using System;

// ---------------------------------------------------------------------------
//	class OPM
//	OPM に良く似た(?)音を生成する音源ユニット
//	
//	interface:
//	bool Init(uint clock, uint rate, bool);
//		初期化．このクラスを使用する前にかならず呼んでおくこと．
//		注意: 線形補完モードは廃止されました
//
//		clock:	OPM のクロック周波数(Hz)
//
//		rate:	生成する PCM の標本周波数(Hz)
//
//				
//		返値	初期化に成功すれば true
//
//	bool SetRate(uint clock, uint rate, bool)
//		クロックや PCM レートを変更する
//		引数等は Init と同様．
//	
//	void Mix(Sample* dest, int nsamples)
//		Stereo PCM データを nsamples 分合成し， dest で始まる配列に
//		加える(加算する)
//		・dest には sample*2 個分の領域が必要
//		・格納形式は L, R, L, R... となる．
//		・あくまで加算なので，あらかじめ配列をゼロクリアする必要がある
//		・FM_SAMPLETYPE が short 型の場合クリッピングが行われる.
//		・この関数は音源内部のタイマーとは独立している．
//		  Timer は Count と GetNextEvent で操作する必要がある．
//	
//	void Reset()
//		音源をリセット(初期化)する
//
//	void SetReg(uint reg, uint data)
//		音源のレジスタ reg に data を書き込む
//	
//	uint ReadStatus()
//		音源のステータスレジスタを読み出す
//		busy フラグは常に 0
//	
//	bool Count(uint32 t)
//		音源のタイマーを t [10^(-6) 秒] 進める．
//		音源の内部状態に変化があった時(timer オーバーフロー)
//		true を返す
//
//	uint32 GetNextEvent()
//		音源のタイマーのどちらかがオーバーフローするまでに必要な
//		時間[μ秒]を返す
//		タイマーが停止している場合は 0 を返す．
//	
//	void SetVolume(int db)
//		各音源の音量を＋－方向に調節する．標準値は 0.
//		単位は約 1/2 dB，有効範囲の上限は 20 (10dB)
//
//	仮想関数:
//	virtual void Intr(bool irq)
//		IRQ 出力に変化があった場合呼ばれる．
//		irq = true:  IRQ 要求が発生
//		irq = false: IRQ 要求が消える
//
namespace MDSound.fmgen
{
    public class OPM : Timer
    {
        private const int OPM_LFOENTS = 512;
        private int fmvolume;

        private uint clock;
        private uint rate;
        private uint pcmrate;

        private uint pmd;
        private uint amd;
        private uint lfocount;
        private uint lfodcount;

        private uint lfo_count_;
        private uint lfo_count_diff_;
        private uint lfo_step_;
        private uint lfo_count_prev_;

        private uint lfowaveform;
        private uint rateratio;
        private uint noise;
        private int noisecount;
        private uint noisedelta;

        private bool interpolation;
        private byte lfofreq;
        private new byte status;
        private byte reg01;

        private byte[] kc = new byte[8];
        private byte[] kf = new byte[8];
        private byte[] pan = new byte[8];

        private fmgen.Channel4[] ch = new fmgen.Channel4[8];
        private fmgen.Chip chip = new fmgen.Chip();

        private static int[][] amtable = new int[4][] { new int[OPM_LFOENTS], new int[OPM_LFOENTS], new int[OPM_LFOENTS], new int[OPM_LFOENTS] };
        private static int[][] pmtable = new int[4][] { new int[OPM_LFOENTS], new int[OPM_LFOENTS], new int[OPM_LFOENTS], new int[OPM_LFOENTS] };

        private static Random rand = new Random();

        public OPM()
        {
            amtable[0][0] = -1;

            lfo_count_ = 0;
            lfo_count_prev_ = ~(uint)(0);
            BuildLFOTable();
            for (int i = 0; i < 8; i++)
            {
                ch[i] = new fmgen.Channel4();
                ch[i].SetChip(chip);
                ch[i].SetType(fmgen.OpType.typeM);
            }
        }

        ~OPM()
        {
        }

        public bool Init(uint c, uint rf, bool ip = false)
        {
            if (!SetRate(c, rf, ip))
                return false;

            Reset();

            SetVolume(0);
            SetChannelMask(0);
            return true;
        }

        public bool SetRate(uint c, uint r, bool ip)
        {
            clock = c;
            pcmrate = r;
            rate = r;

            RebuildTimeTable();

            return true;
        }

        public void SetChannelMask(uint mask)
        {
            for (int i = 0; i < 8; i++)
                ch[i].Mute(!!((mask & (1 << i))!=0));
        }

        public new void Reset()
        {
            int i;
            for (i = 0x0; i < 0x100; i++) SetReg((uint)i, 0);
            SetReg(0x19, 0x80);
            base.Reset();

            status = 0;
            noise = 12345;
            noisecount = 0;

            for (i = 0; i < 8; i++)
                ch[i].Reset();
        }

        private void RebuildTimeTable()
        {
            uint fmclock = clock / 64;

            //assert(fmclock < (0x80000000 >> FM_RATIOBITS));
            rateratio = ((fmclock << fmgen.FM_RATIOBITS) + rate / 2) / rate;
            SetTimerBase(fmclock);

            //	FM::MakeTimeTable(rateratio);
            chip.SetRatio(rateratio);

            //	lfo_diff_ = 


            //	lfodcount = (16 + (lfofreq & 15)) << (lfofreq >> 4);
            //	lfodcount = lfodcount * rateratio >> FM_RATIOBITS;
        }

        private void TimerA()
        {
            if ((regtc & 0x80) != 0)
            {
                for (int i = 0; i < 8; i++)
                {
                    ch[i].KeyControl(0);
                    ch[i].KeyControl(0xf);
                }
            }
        }

        public void SetVolume(int db)
        {
            db = Math.Min(db, 20);
            if (db > -192)
                fmvolume = (int)(16384.0 * Math.Pow(10, db / 40.0));
            else
                fmvolume = 0;
        }

        private new void SetStatus(uint bits)
        {
            if ((status & (byte)bits) == 0)
            {
                status |= (byte)bits;
                Intr(true);
            }
        }

        private new void ResetStatus(uint bits)
        {
            if ((status & (byte)bits) != 0)
            {
                status &= (byte)~bits;
                if (status == 0)
                    Intr(false);
            }
        }

        public void SetLPFCutoff(uint freq)
        {
        }

        public void SetReg(uint addr, uint data)
        {
            if (addr >= 0x100)
                return;

            int c = (int)(addr & 7);
            switch (addr & 0xff)
            {
                case 0x01:                  // TEST (lfo restart)
                    if ((data & 2)!=0)
                    {
                        lfo_count_ = 0;
                        lfo_count_prev_ = ~(uint)0;
                    }
                    reg01 = (byte)data;
                    break;

                case 0x08:                  // KEYON
                    if ((regtc & 0x80)==0)
                        ch[data & 7].KeyControl(data >> 3);
                    else
                    {
                        c = (int)(data & 7);
                        if ((data & 0x08)==0) ch[c].op[0].KeyOff();
                        if ((data & 0x10)==0) ch[c].op[1].KeyOff();
                        if ((data & 0x20)==0) ch[c].op[2].KeyOff();
                        if ((data & 0x40)==0) ch[c].op[3].KeyOff();
                    }
                    break;

                case 0x10:
                case 0x11:      // CLKA1, CLKA2
                    SetTimerA(addr, data);
                    break;

                case 0x12:                  // CLKB
                    SetTimerB(data);
                    break;

                case 0x14:                  // CSM, TIMER
                    SetTimerControl(data);
                    break;

                case 0x18:                  // LFRQ(lfo freq)
                    lfofreq = (byte)data;

                    //assert(16 - 4 - FM_RATIOBITS >= 0);
                    lfo_count_diff_ =(uint)
                        (rateratio
                        * ((16 + (lfofreq & 15)) << (16 - 4 - fmgen.FM_RATIOBITS))
                        / (1 << (15 - (lfofreq >> 4))));

                    break;

                case 0x19:                  // PMD/AMD
                    if ((data & 0x80) != 0)
                    {
                        pmd = data & 0x7f;
                    }
                    else
                    {
                        amd = data & 0x7f;
                    }
                    break;

                case 0x1b:                  // CT, W(lfo waveform)
                    lfowaveform = data & 3;
                    break;

                // RL, FB, Connect
                case 0x20:
                case 0x21:
                case 0x22:
                case 0x23:
                case 0x24:
                case 0x25:
                case 0x26:
                case 0x27:
                    ch[c].SetFB((data >> 3) & 7);
                    ch[c].SetAlgorithm(data & 7);
                    pan[c] = (byte)((data >> 6) & 3);
                    break;

                // KC
                case 0x28:
                case 0x29:
                case 0x2a:
                case 0x2b:
                case 0x2c:
                case 0x2d:
                case 0x2e:
                case 0x2f:
                    kc[c] = (byte)data;
                    ch[c].SetKCKF(kc[c], kf[c]);
                    break;

                // KF
                case 0x30:
                case 0x31:
                case 0x32:
                case 0x33:
                case 0x34:
                case 0x35:
                case 0x36:
                case 0x37:
                    kf[c] = (byte)(data >> 2);
                    ch[c].SetKCKF(kc[c], kf[c]);
                    break;

                // PMS, AMS
                case 0x38:
                case 0x39:
                case 0x3a:
                case 0x3b:
                case 0x3c:
                case 0x3d:
                case 0x3e:
                case 0x3f:
                    ch[c].SetMS((data << 4) | (data >> 4));
                    break;

                case 0x0f:          // NE/NFRQ (noise)
                    noisedelta = data;
                    noisecount = 0;
                    break;

                default:
                    if (addr >= 0x40)
                        SetParameter(addr, data);
                    break;
            }
        }

        private void SetParameter(uint addr, uint data)
        {
            byte[] sltable = new byte[16]{
              0,   4,   8,  12,  16,  20,  24,  28,
             32,  36,  40,  44,  48,  52,  56, 124
            };
            byte[] slottable=new byte[4]{ 0, 2, 1, 3 };

            uint slot = slottable[(addr >> 3) & 3];
            fmgen.Operator op = ch[addr & 7].op[slot];

            switch ((addr >> 5) & 7)
            {
                case 2: // 40-5F DT1/MULTI
                    op.SetDT((data >> 4) & 0x07);
                    op.SetMULTI(data & 0x0f);
                    break;

                case 3: // 60-7F TL
                    op.SetTL(data & 0x7f, (regtc & 0x80) != 0);
                    break;

                case 4: // 80-9F KS/AR
                    op.SetKS((data >> 6) & 3);
                    op.SetAR((data & 0x1f) * 2);
                    break;

                case 5: // A0-BF DR/AMON(D1R/AMS-EN)
                    op.SetDR((data & 0x1f) * 2);
                    op.SetAMON((data & 0x80) != 0);
                    break;

                case 6: // C0-DF SR(D2R), DT2
                    op.SetSR((data & 0x1f) * 2);
                    op.SetDT2((data >> 6) & 3);
                    break;

                case 7: // E0-FF SL(D1L)/RR
                    op.SetSL(sltable[(data >> 4) & 15]);
                    op.SetRR((data & 0x0f) * 4 + 2);
                    break;
            }
        }

        private static void BuildLFOTable()
        {
            if (amtable[0][0] != -1)
                return;

            for (int type = 0; type < 4; type++)
            {
                int r = 0;
                for (int c = 0; c < OPM_LFOENTS; c++)
                {
                    int a=0, p=0;

                    switch (type)
                    {
                        case 0:
                            p = (((c + 0x100) & 0x1ff) / 2) - 0x80;
                            a = 0xff - c / 2;
                            break;

                        case 1:
                            a = c < 0x100 ? 0xff : 0;
                            p = c < 0x100 ? 0x7f : -0x80;
                            break;

                        case 2:
                            p = (c + 0x80) & 0x1ff;
                            p = p < 0x100 ? p - 0x80 : 0x17f - p;
                            a = c < 0x100 ? 0xff - c : c - 0x100;
                            break;

                        case 3:
                            if ((c & 3)==0)
                                r = (rand.Next() / 17) & 0xff;
                            a = r;
                            p = r - 0x80;
                            break;
                    }

                    amtable[type][c] = a;
                    pmtable[type][c] = -p - 1;
                    //			printf("%d ", p);
                }
            }
        }

        private void LFO()
        {
            if (lfowaveform != 3)
            {
                //		if ((lfo_count_ ^ lfo_count_prev_) & ~((1 << 15) - 1))
                {
                    int c = (int)((lfo_count_ >> 15) & 0x1fe);
                    //	fprintf(stderr, "%.8x %.2x\n", lfo_count_, c);
                    chip.SetPML((uint)(pmtable[lfowaveform][c] * pmd / 128 + 0x80));
                    chip.SetAML((uint)(amtable[lfowaveform][c] * amd / 128));
                }
            }
            else
            {
                if (((lfo_count_ ^ lfo_count_prev_) & ~((1 << 17) - 1))!=0)
                {
                    int c = (rand.Next() / 17) & 0xff;
                    chip.SetPML((uint)((c - 0x80) * pmd / 128 + 0x80));
                    chip.SetAML((uint)(c * amd / 128));
                }
            }
            lfo_count_prev_ = lfo_count_;
            lfo_step_++;
            if ((lfo_step_ & 7) == 0)
            {
                lfo_count_ += lfo_count_diff_;
            }
        }

        private uint Noise()
        {
            noisecount += (int)(2 * rateratio);
            if (noisecount >= (32 << fmgen.FM_RATIOBITS))
            {
                int n = (int)(32 - (noisedelta & 0x1f));
                if (n == 1)
                    n = 2;

                noisecount = noisecount - (n << fmgen.FM_RATIOBITS);
                if ((noisedelta & 0x1f) == 0x1f)
                    noisecount -= fmgen.FM_RATIOBITS;
                noise = (uint)((noise >> 1) ^ ((noise & 1) != 0 ? 0x8408 : 0));
            }
            return noise;
        }

        private void MixSub(int activech, int[] idest,int[] ibuf)
        {
            if ((activech & 0x4000)!=0)  ibuf[idest[0]] = ch[0].Calc();
            if ((activech & 0x1000)!= 0) ibuf[idest[1]] += ch[1].Calc();
            if ((activech & 0x0400)!= 0) ibuf[idest[2]] += ch[2].Calc();
            if ((activech & 0x0100)!= 0) ibuf[idest[3]] += ch[3].Calc();
            if ((activech & 0x0040)!= 0) ibuf[idest[4]] += ch[4].Calc();
            if ((activech & 0x0010)!= 0) ibuf[idest[5]] += ch[5].Calc();
            if ((activech & 0x0004)!= 0) ibuf[idest[6]] += ch[6].Calc();
            if ((activech & 0x0001)!= 0)
            {
                if ((noisedelta & 0x80) != 0)
                    ibuf[idest[7]] += ch[7].CalcN(Noise());
                else
                    ibuf[idest[7]] += ch[7].Calc();
            }
        }

        private void MixSubL(int activech, int[] idest,int[] ibuf)
        {
            if ((activech & 0x4000) != 0) ibuf[idest[0]] = ch[0].CalcL();
            if ((activech & 0x1000) != 0) ibuf[idest[1]] += ch[1].CalcL();
            if ((activech & 0x0400) != 0) ibuf[idest[2]] += ch[2].CalcL();
            if ((activech & 0x0100) != 0) ibuf[idest[3]] += ch[3].CalcL();
            if ((activech & 0x0040) != 0) ibuf[idest[4]] += ch[4].CalcL();
            if ((activech & 0x0010) != 0) ibuf[idest[5]] += ch[5].CalcL();
            if ((activech & 0x0004) != 0) ibuf[idest[6]] += ch[6].CalcL();
            if ((activech & 0x0001)!=0)
            {
                if ((noisedelta & 0x80) != 0)
                    ibuf[idest[7]] += ch[7].CalcLN(Noise());
                else
                    ibuf[idest[7]] += ch[7].CalcL();
            }
        }

        public void Mix(int[] buffer, int nsamples)
        {
//#define IStoSample(s)	((Limit(s, 0xffff, -0x10000) * fmvolume) >> 14)
            //#define IStoSample(s)	((s * fmvolume) >> 14)

            // odd bits - active, even bits - lfo
            uint activech = 0;
            for (int i = 0; i < 8; i++)
            {
                activech = activech << 2;
                uint pre = (uint)ch[i].Prepare();
                activech = (activech | pre);
            }

            if ((activech & 0x5555)!=0)
            {
                // LFO 波形初期化ビット = 1 ならば LFO はかからない?
                if ((reg01 & 0x02)!=0)
                    activech &= 0x5555;

                // Mix
                int[] ibuf=new int[8];
                int[] idest=new int[8];
                idest[0] = pan[0];
                idest[1] = pan[1];
                idest[2] = pan[2];
                idest[3] = pan[3];
                idest[4] = pan[4];
                idest[5] = pan[5];
                idest[6] = pan[6];
                idest[7] = pan[7];

                int limit = nsamples * 2;
                for (int dest = 0; dest < limit; dest += 2)
                {
                    ibuf[1] = ibuf[2] = ibuf[3] = 0;
                    if ((activech & 0xaaaa) != 0) {
                        LFO();
                        MixSubL((int)activech, idest, ibuf);
                    }
                    else {
                        LFO();
                        MixSub((int)activech, idest, ibuf);
                    }

                    fmgen.StoreSample(ref buffer[dest+0], ((fmgen.Limit((ibuf[1] + ibuf[3]), 0xffff, -0x10000) * fmvolume) >> 14));
                    fmgen.StoreSample(ref buffer[dest+1], ((fmgen.Limit((ibuf[2] + ibuf[3]), 0xffff, -0x10000) * fmvolume) >> 14));
                }
            }
//#undef IStoSample
        }

        public uint GetReg(uint addr)
        {
            return 0;
        }

        public uint ReadStatus()
        {
            return (uint)(status & 0x03);
        }

        private void Intr(bool f)
        {
        }

        public int dbgGetOpOut(int c, int s)
        {
            return ch[c].op[s].dbgopout_;
        }

        public fmgen.Channel4 dbgGetCh(int c)
        {
            return ch[c];
        }

    }
}
