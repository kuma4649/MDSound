using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDSound.fmgen
{
    //	YM2609(OPNA2) ---------------------------------------------------
    public class OPNA2 : OPNABase
    {
        // リズム音源関係
        private Rhythm[] rhythm = new Rhythm[6] { new Rhythm(), new Rhythm(), new Rhythm(), new Rhythm(), new Rhythm(), new Rhythm() };

        private sbyte rhythmtl;      // リズム全体の音量
        private int rhythmtvol;
        private byte rhythmkey;     // リズムのキー

        protected FM6[] fm6 = new FM6[2] { new FM6(), new FM6() };
        protected new PSG2[] psg = new PSG2[4] { new PSG2(), new PSG2(), new PSG2(), new PSG2() };
        protected ADPCMB[] adpcmb = new ADPCMB[3] { new ADPCMB(), new ADPCMB(), new ADPCMB() };

        protected new byte prescale;

        public class FM6
        {
            public OPNA2 parent = null;

            //protected OPNA2 parent;
            public int fmvolume;
            protected fmgen.Channel4 csmch;
            protected uint[] fnum = new uint[6];
            protected uint[] fnum3 = new uint[3];
            public fmgen.Channel4[] ch = new fmgen.Channel4[6];

            protected byte[] fnum2 = new byte[9];

            protected byte reg22;
            protected uint reg29;     // OPNA only?
            protected byte[] pan = new byte[6];
            protected uint lfocount;
            protected uint lfodcount;
            public int[] visVolume = new int[2] { 0, 0 };
            protected byte regtc;
            public fmgen.Chip chip;

            protected uint[] lfotable = new uint[8];

            public FM6()
            {
                chip = new fmgen.Chip();

                for (int i = 0; i < 6; i++)
                {
                    ch[i] = new fmgen.Channel4();
                    ch[i].SetChip(chip);
                    ch[i].SetType(fmgen.OpType.typeN);
                }

                csmch = ch[2];

            }

            // ---------------------------------------------------------------------------
            //	レジスタアレイにデータを設定
            //
            public void SetReg(uint addr, uint data)
            {
                if (addr < 0x20) return;
                if (addr >= 0x100 && addr < 0x120) return;

                int c = (int)(addr & 3);
                uint modified;

                switch (addr)
                {

                    // Timer -----------------------------------------------------------------
                    case 0x24:
                    case 0x25:
                        parent.SetTimerA(addr, data);
                        break;

                    case 0x26:
                        parent.SetTimerB(data);
                        break;

                    case 0x27:
                        parent.SetTimerControl(data);
                        break;

                    // Misc ------------------------------------------------------------------
                    case 0x28:      // Key On/Off
                        if ((data & 3) < 3)
                        {
                            c = (int)((data & 3) + ((data & 4) != 0 ? 3 : 0));
                            ch[c].KeyControl(data >> 4);
                        }
                        break;

                    // Status Mask -----------------------------------------------------------
                    case 0x29:
                        reg29 = data;
                        //		UpdateStatus(); //?
                        break;

                    // Prescaler -------------------------------------------------------------
                    case 0x2d:
                    case 0x2e:
                    case 0x2f:
                        parent.SetPrescaler(addr - 0x2d);
                        break;

                    // F-Number --------------------------------------------------------------
                    case 0x1a0:
                    case 0x1a1:
                    case 0x1a2:
                        c += 3;
                        fnum[c] = (uint)(data + fnum2[c] * 0x100);
                        ch[c].SetFNum(fnum[c]);
                        break;
                    case 0xa0:
                    case 0xa1:
                    case 0xa2:
                        fnum[c] = (uint)(data + fnum2[c] * 0x100);
                        ch[c].SetFNum(fnum[c]);
                        break;

                    case 0x1a4:
                    case 0x1a5:
                    case 0x1a6:
                        c += 3;
                        fnum2[c] = (byte)(data);
                        break;
                    case 0xa4:
                    case 0xa5:
                    case 0xa6:
                        fnum2[c] = (byte)(data);
                        break;

                    case 0xa8:
                    case 0xa9:
                    case 0xaa:
                        fnum3[c] = (uint)(data + fnum2[c + 6] * 0x100);
                        break;

                    case 0xac:
                    case 0xad:
                    case 0xae:
                        fnum2[c + 6] = (byte)(data);
                        break;

                    // Algorithm -------------------------------------------------------------

                    case 0x1b0:
                    case 0x1b1:
                    case 0x1b2:
                        c += 3;
                        ch[c].SetFB((data >> 3) & 7);
                        ch[c].SetAlgorithm(data & 7);
                        break;
                    case 0xb0:
                    case 0xb1:
                    case 0xb2:
                        ch[c].SetFB((data >> 3) & 7);
                        ch[c].SetAlgorithm(data & 7);
                        break;

                    case 0x1b4:
                    case 0x1b5:
                    case 0x1b6:
                        c += 3;
                        pan[c] = (byte)((data >> 6) & 3);
                        ch[c].SetMS(data);
                        break;
                    case 0xb4:
                    case 0xb5:
                    case 0xb6:
                        pan[c] = (byte)((data >> 6) & 3);
                        ch[c].SetMS(data);
                        break;

                    // LFO -------------------------------------------------------------------
                    case 0x22:
                        modified = reg22 ^ data;
                        reg22 = (byte)data;
                        if ((modified & 0x8) != 0)
                            lfocount = 0;
                        lfodcount = (reg22 & 8) != 0 ? lfotable[reg22 & 7] : 0;
                        break;

                        // 音色 ------------------------------------------------------------------
                    default:
                        if (c < 3)
                        {
                            if ((addr & 0x100) != 0)
                                c += 3;
                            SetParameter(ch[c], addr, data);
                        }
                        break;
                }
            }

            protected void SetParameter(fmgen.Channel4 ch, uint addr, uint data)
            {
                uint[] slottable = new uint[4] { 0, 2, 1, 3 };
                byte[] sltable = new byte[16]{
              0,   4,   8,  12,  16,  20,  24,  28,
             32,  36,  40,  44,  48,  52,  56, 124
            };

                if ((addr & 3) < 3)
                {
                    uint slot = slottable[(addr >> 2) & 3];
                    fmgen.Operator op = ch.op[slot];

                    switch ((addr >> 4) & 15)
                    {
                        case 3: // 30-3E DT/MULTI
                            op.SetDT((data >> 4) & 0x07);
                            op.SetMULTI(data & 0x0f);
                            break;

                        case 4: // 40-4E TL
                            op.SetTL(data & 0x7f, ((regtc & 0x80) != 0) && (csmch == ch));
                            break;

                        case 5: // 50-5E KS/AR
                            op.SetKS((data >> 6) & 3);
                            op.SetAR((data & 0x1f) * 2);
                            break;

                        case 6: // 60-6E DR/AMON
                            op.SetDR((data & 0x1f) * 2);
                            op.SetAMON((data & 0x80) != 0);
                            break;

                        case 7: // 70-7E SR
                            op.SetSR((data & 0x1f) * 2);
                            break;

                        case 8: // 80-8E SL/RR
                            op.SetSL(sltable[(data >> 4) & 15]);
                            op.SetRR((data & 0x0f) * 4 + 2);
                            break;

                        case 9: // 90-9E SSG-EC
                            op.SetSSGEC(data & 0x0f);
                            break;
                    }
                }
            }

            public void Mix(int[] buffer, int nsamples,byte regtc)
            {
                if (fmvolume > 0)
                {
                    this.regtc = regtc;
                    // 準備
                    // Set F-Number
                    if ((regtc & 0xc0) == 0)
                        csmch.SetFNum(fnum[2]);// csmch - ch]);
                    else
                    {
                        // 効果音モード
                        csmch.op[0].SetFNum(fnum3[1]);
                        csmch.op[1].SetFNum(fnum3[2]);
                        csmch.op[2].SetFNum(fnum3[0]);
                        csmch.op[3].SetFNum(fnum[2]);
                    }

                    int act = (((ch[2].Prepare() << 2) | ch[1].Prepare()) << 2) | ch[0].Prepare();
                    if ((reg29 & 0x80) != 0)
                        act |= (ch[3].Prepare() | ((ch[4].Prepare() | (ch[5].Prepare() << 2)) << 2)) << 6;
                    if ((reg22 & 0x08) == 0)
                        act &= 0x555;

                    if ((act & 0x555) != 0)
                    {
                        Mix6(buffer, nsamples, act);
                    }
                }
            }

            protected void Mix6(int[] buffer, int nsamples, int activech)
            {
                // Mix
                int[] ibuf = new int[4];
                int[] idest = new int[6];
                idest[0] = pan[0];
                idest[1] = pan[1];
                idest[2] = pan[2];
                idest[3] = pan[3];
                idest[4] = pan[4];
                idest[5] = pan[5];

                int limit = nsamples * 2;
                for (int dest = 0; dest < limit; dest += 2)
                {
                    ibuf[1] = ibuf[2] = ibuf[3] = 0;
                    if ((activech & 0xaaa) != 0)
                    {
                        LFO();
                        MixSubSL(activech, idest, ibuf);
                    }
                    else
                    {
                        MixSubS(activech, idest, ibuf);
                    }

                    int v = ((fmgen.Limit(ibuf[2] + ibuf[3], 0x7fff, -0x8000) * fmvolume) >> 14);
                    fmgen.StoreSample(ref buffer[dest + 0], v);// ((fmgen.Limit(ibuf[2] + ibuf[3], 0x7fff, -0x8000) * fmvolume) >> 14));
                    visVolume[0] = v;

                    v = ((fmgen.Limit(ibuf[1] + ibuf[3], 0x7fff, -0x8000) * fmvolume) >> 14);
                    fmgen.StoreSample(ref buffer[dest + 1], v);// ((fmgen.Limit(ibuf[1] + ibuf[3], 0x7fff, -0x8000) * fmvolume) >> 14));
                    visVolume[1] = v;
                }
            }

            protected void MixSubS(int activech, int[] dest, int[] buf)
            {
                if ((activech & 0x001) != 0) buf[dest[0]] = ch[0].Calc();
                if ((activech & 0x004) != 0) buf[dest[1]] += ch[1].Calc();
                if ((activech & 0x010) != 0) buf[dest[2]] += ch[2].Calc();
                if ((activech & 0x040) != 0) buf[dest[3]] += ch[3].Calc();
                if ((activech & 0x100) != 0) buf[dest[4]] += ch[4].Calc();
                if ((activech & 0x400) != 0) buf[dest[5]] += ch[5].Calc();
            }

            protected void MixSubSL(int activech, int[] dest, int[] buf)
            {
                if ((activech & 0x001) != 0) buf[dest[0]] = ch[0].CalcL();
                if ((activech & 0x004) != 0) buf[dest[1]] += ch[1].CalcL();
                if ((activech & 0x010) != 0) buf[dest[2]] += ch[2].CalcL();
                if ((activech & 0x040) != 0) buf[dest[3]] += ch[3].CalcL();
                if ((activech & 0x100) != 0) buf[dest[4]] += ch[4].CalcL();
                if ((activech & 0x400) != 0) buf[dest[5]] += ch[5].CalcL();
            }

            protected void LFO()
            {
                //	LOG3("%4d - %8d, %8d\n", c, lfocount, lfodcount);

                //	Operator::SetPML(pmtable[(lfocount >> (FM_LFOCBITS+1)) & 0xff]);
                //	Operator::SetAML(amtable[(lfocount >> (FM_LFOCBITS+1)) & 0xff]);
                chip.SetPML((uint)(pmtable[(lfocount >> (fmgen.FM_LFOCBITS + 1)) & 0xff]));
                chip.SetAML((uint)(amtable[(lfocount >> (fmgen.FM_LFOCBITS + 1)) & 0xff]));
                lfocount += lfodcount;
            }

            public void Reset()
            {
                uint i;
                for (i = 0x20; i < 0x28; i++) SetReg(i, 0);
                for (i = 0x30; i < 0xc0; i++) SetReg(i, 0);
                for (i = 0x130; i < 0x1c0; i++) SetReg(i, 0);
                for (i = 0; i < 6; i++)
                {
                    pan[i] = 3;
                    ch[i].Reset();
                }
            }

        }

        public class ADPCMB
        {
            public OPNA2 parent = null;

            public bool NO_BITTYPE_EMULATION = false;

            public uint statusnext;

            //public byte[] adpcmbuf;        // ADPCM RAM
            public uint adpcmmask;     // メモリアドレスに対するビットマスク
            public uint adpcmnotice;   // ADPCM 再生終了時にたつビット
            protected uint startaddr;     // Start address
            protected uint stopaddr;      // Stop address
            public uint memaddr;       // 再生中アドレス
            protected uint limitaddr;     // Limit address/mask
            public int adpcmlevel;     // ADPCM 音量
            public int adpcmvolume;
            public int adpcmvol;
            public uint deltan;            // ⊿N
            public int adplc;          // 周波数変換用変数
            public int adpld;          // 周波数変換用変数差分値
            public uint adplbase;      // adpld の元
            public int adpcmx;         // ADPCM 合成用 x
            public int adpcmd;         // ADPCM 合成用 ⊿
            protected int adpcmout;       // ADPCM 合成後の出力
            protected int apout0;         // out(t-2)+out(t-1)
            protected int apout1;         // out(t-1)+out(t)

            protected uint adpcmreadbuf;  // ADPCM リード用バッファ
            public bool adpcmplay;     // ADPCM 再生中
            protected sbyte granuality;
            public bool adpcmmask_;

            protected byte control1;     // ADPCM コントロールレジスタ１
            public byte control2;     // ADPCM コントロールレジスタ２
            protected byte[] adpcmreg = new byte[8];  // ADPCM レジスタの一部分

            public void Mix(int[] dest, int count)
            {
                uint maskl = (uint)((control2 & 0x80) != 0 ? -1 : 0);
                uint maskr = (uint)((control2 & 0x40) != 0 ? -1 : 0);
                if (adpcmmask_)
                {
                    maskl = maskr = 0;
                }

                if (adpcmplay)
                {
                    int ptrDest = 0;
                    //		LOG2("ADPCM Play: %d   DeltaN: %d\n", adpld, deltan);
                    if (adpld <= 8192)      // fplay < fsamp
                    {
                        for (; count > 0; count--)
                        {
                            if (adplc < 0)
                            {
                                adplc += 8192;
                                DecodeADPCMB();
                                if (!adpcmplay)
                                    break;
                            }
                            int s = (adplc * apout0 + (8192 - adplc) * apout1) >> 13;
                            fmgen.StoreSample(ref dest[ptrDest + 0], (int)(s & maskl));
                            fmgen.StoreSample(ref dest[ptrDest + 1], (int)(s & maskr));
                            //visAPCMVolume[0] = (int)(s & maskl);
                            //visAPCMVolume[1] = (int)(s & maskr);
                            ptrDest += 2;
                            adplc -= adpld;
                        }
                        for (; count > 0 && apout0 != 0; count--)
                        {
                            if (adplc < 0)
                            {
                                apout0 = apout1;
                                apout1 = 0;
                                adplc += 8192;
                            }
                            int s = (adplc * apout1) >> 13;
                            fmgen.StoreSample(ref dest[ptrDest + 0], (int)(s & maskl));
                            fmgen.StoreSample(ref dest[ptrDest + 1], (int)(s & maskr));
                            //visAPCMVolume[0] = (int)(s & maskl);
                            //visAPCMVolume[1] = (int)(s & maskr);
                            ptrDest += 2;
                            adplc -= adpld;
                        }
                    }
                    else    // fplay > fsamp	(adpld = fplay/famp*8192)
                    {
                        int t = (-8192 * 8192) / adpld;
                        for (; count > 0; count--)
                        {
                            int s = apout0 * (8192 + adplc);
                            while (adplc < 0)
                            {
                                DecodeADPCMB();
                                if (!adpcmplay)
                                    goto stop;
                                s -= apout0 * Math.Max(adplc, t);
                                adplc -= t;
                            }
                            adplc -= 8192;
                            s >>= 13;
                            fmgen.StoreSample(ref dest[ptrDest + 0], (int)(s & maskl));
                            fmgen.StoreSample(ref dest[ptrDest + 1], (int)(s & maskr));
                            //visAPCMVolume[0] = (int)(s & maskl);
                            //visAPCMVolume[1] = (int)(s & maskr);
                            ptrDest += 2;
                        }
                        stop:
                        ;
                    }
                }
                if (!adpcmplay)
                {
                    apout0 = apout1 = adpcmout = 0;
                    adplc = 0;
                }
            }

            public void SetADPCMBReg(uint addr, uint data)
            {
                switch (addr)
                {
                    case 0x00:      // Control Register 1
                        if (((data & 0x80) != 0) && !adpcmplay)
                        {
                            adpcmplay = true;
                            memaddr = startaddr;
                            adpcmx = 0;
                            adpcmd = 127;
                            adplc = 0;
                        }
                        if ((data & 1) != 0)
                        {
                            adpcmplay = false;
                        }
                        control1 = (byte)data;
                        break;

                    case 0x01:      // Control Register 2
                        control2 = (byte)data;
                        granuality = (sbyte)((control2 & 2) != 0 ? 1 : 4);
                        break;

                    case 0x02:      // Start Address L
                    case 0x03:      // Start Address H
                        adpcmreg[addr - 0x02 + 0] = (byte)data;
                        startaddr = (uint)((adpcmreg[1] * 256 + adpcmreg[0]) << 6);
                        memaddr = startaddr;
                        //		LOG1("  startaddr %.6x", startaddr);
                        break;

                    case 0x04:      // Stop Address L
                    case 0x05:      // Stop Address H
                        adpcmreg[addr - 0x04 + 2] = (byte)data;
                        stopaddr = (uint)((adpcmreg[3] * 256 + adpcmreg[2] + 1) << 6);
                        //		LOG1("  stopaddr %.6x", stopaddr);
                        break;

                    case 0x08:      // ADPCM data
                        if ((control1 & 0x60) == 0x60)
                        {
                            //			LOG2("  Wr [0x%.5x] = %.2x", memaddr, data);
                            WriteRAM(data);
                        }
                        break;

                    case 0x09:      // delta-N L
                    case 0x0a:      // delta-N H
                        adpcmreg[addr - 0x09 + 4] = (byte)data;
                        deltan = (uint)(adpcmreg[5] * 256 + adpcmreg[4]);
                        deltan = Math.Max(256, deltan);
                        adpld = (int)(deltan * adplbase >> 16);
                        break;

                    case 0x0b:      // Level Control
                        adpcmlevel = (byte)data;
                        adpcmvolume = (adpcmvol * adpcmlevel) >> 12;
                        break;

                    case 0x0c:      // Limit Address L
                    case 0x0d:      // Limit Address H
                        adpcmreg[addr - 0x0c + 6] = (byte)data;
                        limitaddr = (uint)((adpcmreg[7] * 256 + adpcmreg[6] + 1) << 6);
                        //		LOG1("  limitaddr %.6x", limitaddr);
                        break;

                    case 0x10:      // Flag Control
                        if ((data & 0x80) != 0)
                        {
                            parent.status = 0;
                            parent.UpdateStatus();
                        }
                        else
                        {
                            parent.stmask = ~(data & 0x1f);
                            //			UpdateStatus();					//???
                        }
                        break;
                }
            }

            // ---------------------------------------------------------------------------
            //	ADPCM RAM への書込み操作
            //
            protected void WriteRAM(uint data)
            {
                if (NO_BITTYPE_EMULATION)
                {
                    if ((control2 & 2) == 0)
                    {
                        // 1 bit mode
                        parent.adpcmbuf[(memaddr >> 4) & 0x3ffff] = (byte)data;
                        memaddr += 16;
                    }
                    else
                    {
                        // 8 bit mode
                        //uint8* p = &adpcmbuf[(memaddr >> 4) & 0x7fff];
                        int p = (int)((memaddr >> 4) & 0x7fff);
                        uint bank = (memaddr >> 1) & 7;
                        byte mask = (byte)(1 << (int)bank);
                        data <<= (int)bank;

                        parent.adpcmbuf[p + 0x00000] = (byte)((parent.adpcmbuf[p + 0x00000] & ~mask) | ((byte)(data) & mask));
                        data >>= 1;
                        parent.adpcmbuf[p + 0x08000] = (byte)((parent.adpcmbuf[p + 0x08000] & ~mask) | ((byte)(data) & mask));
                        data >>= 1;
                        parent.adpcmbuf[p + 0x10000] = (byte)((parent.adpcmbuf[p + 0x10000] & ~mask) | ((byte)(data) & mask));
                        data >>= 1;
                        parent.adpcmbuf[p + 0x18000] = (byte)((parent.adpcmbuf[p + 0x18000] & ~mask) | ((byte)(data) & mask));
                        data >>= 1;
                        parent.adpcmbuf[p + 0x20000] = (byte)((parent.adpcmbuf[p + 0x20000] & ~mask) | ((byte)(data) & mask));
                        data >>= 1;
                        parent.adpcmbuf[p + 0x28000] = (byte)((parent.adpcmbuf[p + 0x28000] & ~mask) | ((byte)(data) & mask));
                        data >>= 1;
                        parent.adpcmbuf[p + 0x30000] = (byte)((parent.adpcmbuf[p + 0x30000] & ~mask) | ((byte)(data) & mask));
                        data >>= 1;
                        parent.adpcmbuf[p + 0x38000] = (byte)((parent.adpcmbuf[p + 0x38000] & ~mask) | ((byte)(data) & mask));
                        memaddr += 2;
                    }
                }
                else
                {
                    parent.adpcmbuf[(memaddr >> granuality) & 0x3ffff] = (byte)data;
                    memaddr += (uint)(1 << granuality);
                }

                if (memaddr == stopaddr)
                {
                    parent.SetStatus(4);
                    statusnext = 0x04;  // EOS
                    memaddr &= 0x3fffff;
                }
                if (memaddr == limitaddr)
                {
                    //		LOG1("Limit ! (%.8x)\n", limitaddr);
                    memaddr = 0;
                }
                parent.SetStatus(8);
            }

            // ---------------------------------------------------------------------------
            //	ADPCM 展開
            //
            protected void DecodeADPCMB()
            {
                apout0 = apout1;
                int n = (ReadRAMN() * adpcmvolume) >> 13;
                apout1 = adpcmout + n;
                adpcmout = n;
            }

            // ---------------------------------------------------------------------------
            //	ADPCM RAM からの nibble 読み込み及び ADPCM 展開
            //
            protected int ReadRAMN()
            {
                uint data;
                if (granuality > 0)
                {
                    if (NO_BITTYPE_EMULATION)
                    {
                        if ((control2 & 2) == 0)
                        {
                            data = parent.adpcmbuf[(memaddr >> 4) & 0x3ffff];
                            memaddr += 8;
                            if ((memaddr & 8) != 0)
                                return DecodeADPCMBSample(data >> 4);
                            data &= 0x0f;
                        }
                        else
                        {
                            //uint8* p = &adpcmbuf[(memaddr >> 4) & 0x7fff] + ((~memaddr & 1) << 17);
                            int p = (int)(((memaddr >> 4) & 0x7fff) + ((~memaddr & 1) << 17));
                            uint bank = (memaddr >> 1) & 7;
                            byte mask = (byte)(1 << (int)bank);

                            data = (uint)(parent.adpcmbuf[p + 0x18000] & mask);
                            data = (uint)(data * 2 + (parent.adpcmbuf[p + 0x10000] & mask));
                            data = (uint)(data * 2 + (parent.adpcmbuf[p + 0x08000] & mask));
                            data = (uint)(data * 2 + (parent.adpcmbuf[p + 0x00000] & mask));
                            data >>= (int)bank;
                            memaddr++;
                            if ((memaddr & 1) != 0)
                                return DecodeADPCMBSample(data);
                        }
                    }
                    else
                    {
                        data = parent.adpcmbuf[(memaddr >> granuality) & adpcmmask];
                        memaddr += (uint)(1 << (granuality - 1));
                        if ((memaddr & (1 << (granuality - 1))) != 0)
                            return DecodeADPCMBSample(data >> 4);
                        data &= 0x0f;
                    }
                }
                else
                {
                    data = parent.adpcmbuf[(memaddr >> 1) & adpcmmask];
                    ++memaddr;
                    if ((memaddr & 1) != 0)
                        return DecodeADPCMBSample(data >> 4);
                    data &= 0x0f;
                }

                DecodeADPCMBSample(data);

                // check
                if (memaddr == stopaddr)
                {
                    if ((control1 & 0x10) != 0)
                    {
                        memaddr = startaddr;
                        data = (uint)adpcmx;
                        adpcmx = 0;
                        adpcmd = 127;
                        return (int)data;
                    }
                    else
                    {
                        memaddr &= adpcmmask;   //0x3fffff;
                        parent.SetStatus(adpcmnotice);
                        adpcmplay = false;
                    }
                }

                if (memaddr == limitaddr)
                    memaddr = 0;

                return adpcmx;
            }

            protected int DecodeADPCMBSample(uint data)
            {
                int[] table1 = new int[16]
                {
          1,   3,   5,   7,   9,  11,  13,  15,
         -1,  -3,  -5,  -7,  -9, -11, -13, -15,
                };

                int[] table2 = new int[16]
                {
         57,  57,  57,  57,  77, 102, 128, 153,
         57,  57,  57,  57,  77, 102, 128, 153,
                };

                adpcmx = fmgen.Limit(adpcmx + table1[data] * adpcmd / 8, 32767, -32768);
                adpcmd = fmgen.Limit(adpcmd * table2[data] / 64, 24576, 127);
                return adpcmx;
            }
        }

        public class Rhythm
        {
            public byte pan;      // ぱん
            public sbyte level;     // おんりょう
            public int volume;     // おんりょうせってい
            public int[] sample;      // さんぷる
            public uint size;      // さいず
            public uint pos;       // いち
            public uint step;      // すてっぷち
            public uint rate;      // さんぷるのれーと
        };

        public class whdr
        {
            public uint chunksize;
            public uint tag;
            public uint nch;
            public uint rate;
            public uint avgbytes;
            public uint align;
            public uint bps;
            public uint size;
        }


        // ---------------------------------------------------------------------------
        //	構築
        //
        public OPNA2()
        {
            for (int i = 0; i < 6; i++)
            {
                rhythm[i].sample = null;
                rhythm[i].pos = 0;
                rhythm[i].size = 0;
                rhythm[i].volume = 0;
            }
            rhythmtvol = 0;

            for (int i = 0; i < 2; i++)
            {
                fm6[i].parent = this;
            }

            for (int i = 0; i < 3; i++)
            {
                adpcmb[i].adpcmmask = 0x3ffff;
                adpcmb[i].adpcmnotice = 4;
                adpcmb[i].deltan = 256;
                adpcmb[i].adpcmvol = 0;
                adpcmb[i].control2 = 0;
                adpcmb[i].parent = this;
            }

            csmch = ch[2];

        }

        ~OPNA2()
        {
            adpcmbuf = null;
            for (int i = 0; i < 6; i++)
            {
                rhythm[i].sample = null;
            }
        }

        public bool Init(uint c, uint r, bool ipflag = false, string path = "")
        {
            rate = 8000;
            LoadRhythmSample(path);

            if (adpcmbuf == null)
                adpcmbuf = new byte[0x40000];
            if (adpcmbuf == null)
                return false;

            if (!SetRate(c, r, ipflag))
                return false;
            if (!base.Init(c, r, ipflag))
                return false;

            Reset();

            SetVolumeFM(0);
            SetVolumePSG(0);
            SetVolumeADPCM(0);
            SetVolumeRhythmTotal(0);
            for (int i = 0; i < 6; i++)
                SetVolumeRhythm(0, 0);
            SetChannelMask(0);

            return true;
        }

        // ---------------------------------------------------------------------------
        //	サンプリングレート変更
        //
        public new bool SetRate(uint c, uint r, bool ipflag = false)
        {
            if (!base.SetRate(c, r, ipflag))
                return false;

            RebuildTimeTable();
            for (int i = 0; i < 6; i++)
            {
                rhythm[i].step = rhythm[i].rate * 1024 / r;
            }

            for (int i = 0; i < 3; i++)
            {
                adpcmb[i].adplbase = (uint)((int)(8192.0 * (clock / 72.0) / r));
                adpcmb[i].adpld = (int)(adpcmb[i].deltan * adpcmb[i].adplbase >> 16);
            }

            return true;
        }

        // ---------------------------------------------------------------------------
        //	合成
        //	in:		buffer		合成先
        //			nsamples	合成サンプル数
        //
        public void Mix(int[] buffer, int nsamples)
        {

            fm6[0].Mix(buffer, nsamples, regtc);
            fm6[1].Mix(buffer, nsamples, regtc);
            psg[0].Mix(buffer, nsamples);
            psg[1].Mix(buffer, nsamples);
            psg[2].Mix(buffer, nsamples);
            psg[3].Mix(buffer, nsamples);
            adpcmb[0].Mix(buffer, nsamples);
            adpcmb[1].Mix(buffer, nsamples);
            adpcmb[2].Mix(buffer, nsamples);
            RhythmMix(buffer, nsamples);

        }

        // ---------------------------------------------------------------------------
        //	リセット
        //
        public new void Reset()
        {
            reg29 = 0x1f;
            rhythmkey = 0;
            limitaddr = 0x3ffff;
            base.Reset();

            SetPrescaler(0);

            fm6[0].Reset();
            fm6[1].Reset();

            psg[0].Reset();
            psg[1].Reset();
            psg[2].Reset();
            psg[3].Reset();

            for (int i = 0; i < 3; i++)
            {
                adpcmb[i].statusnext = 0;
                adpcmb[i].memaddr = 0;
                adpcmb[i].adpcmd = 127;
                adpcmb[i].adpcmx = 0;
                adpcmb[i].adpcmplay = false;
                adpcmb[i].adplc = 0;
                adpcmb[i].adpld = 0x100;
            }
        }

        protected new void RebuildTimeTable()
        {
            base.RebuildTimeTable();

            int p = prescale;
            prescale = 0xff;//-1;
            SetPrescaler((uint)p);
        }

        protected new void SetPrescaler(uint p)
        {
            base.SetPrescaler(p);

            sbyte[][] table = new sbyte[3][] { new sbyte[2] { 6, 4 }, new sbyte[2] { 3, 2 }, new sbyte[2] { 2, 1 } };
            byte[] table2 = new byte[8] { 108, 77, 71, 67, 62, 44, 8, 5 };
            // 512
            if (prescale != p)
            {
                prescale = (byte)p;
                //assert(0 <= prescale && prescale< 3);

                uint fmclock = (uint)(clock / table[p][0] / 12);

                rate = psgrate;

                // 合成周波数と出力周波数の比
                //assert(fmclock< (0x80000000 >> FM_RATIOBITS));
                uint ratio = ((fmclock << fmgen.FM_RATIOBITS) + rate / 2) / rate;

                SetTimerBase(fmclock);
                //		MakeTimeTable(ratio);
                fm6[0].chip.SetRatio(ratio);
                fm6[1].chip.SetRatio(ratio);

                psg[0].SetClock((int)(clock / table[p][1]), (int)psgrate);
                psg[1].SetClock((int)(clock / table[p][1]), (int)psgrate);
                psg[2].SetClock((int)(clock / table[p][1]), (int)psgrate);
                psg[3].SetClock((int)(clock / table[p][1]), (int)psgrate);

                for (int i = 0; i < 8; i++)
                {
                    lfotable[i] = (ratio << (2 + fmgen.FM_LFOCBITS - fmgen.FM_RATIOBITS)) / table2[i];
                }
            }
        }

        // ---------------------------------------------------------------------------
        //	レジスタアレイにデータを設定
        //
        public new void SetReg(uint addr, uint data)
        {
            addr &= 0x3ff;

            if (addr < 0x10)
            {
                psg[0].SetReg(addr, (byte)data);
                return;
            }
            else if (addr >= 0x10 && addr < 0x20)
            {
                RhythmSetReg(addr, (byte)data);
                return;
            }
            else if (addr >= 0x100 && addr < 0x110)
            {
                AdpcmbSetReg(0, addr - 0x100, (byte)data);
                return;
            }
            else if (addr >= 0x110 && addr < 0x120)
            {
                return;
            }
            else if (addr >= 0x120 && addr < 0x130)
            {
                psg[1].SetReg(addr - 0x120, (byte)data);
                return;
            }
            else if (addr >= 0x200 && addr < 0x210)
            {
                psg[2].SetReg(addr - 0x200, (byte)data);
                return;
            }
            else if (addr >= 0x210 && addr < 0x220)
            {
                return;
            }
            else if (addr >= 0x300 && addr < 0x310)
            {
                AdpcmbSetReg(1, addr - 0x300, (byte)data);
                return;
            }
            else if (addr >= 0x310 && addr < 0x320)
            {
                AdpcmbSetReg(2, addr - 0x310, (byte)data);
                return;
            }
            else if (addr >= 0x320 && addr < 0x330)
            {
                psg[3].SetReg(addr - 0x320, (byte)data);
                return;
            }

            if (addr < 0x200)
            {
                FmSetReg(0, addr, (byte)data);
            }
            else
            {
                FmSetReg(1, addr - 0x200, (byte)data);
            }

        }

        public void FmSetReg(int ch, uint addr, byte data)
        {
            fm6[ch].SetReg(addr, data);
        }

        public void AdpcmbSetReg(int ch, uint addr, byte data)
        {
            adpcmb[ch].SetADPCMBReg(addr, data);
        }

        public void RhythmSetReg(uint addr, byte data)
        {
            switch (addr)
            {
                // Rhythm ----------------------------------------------------------------
                case 0x10:          // DM/KEYON
                    if ((data & 0x80) == 0)  // KEY ON
                    {
                        rhythmkey |= (byte)(data & 0x3f);
                        if ((data & 0x01) != 0) rhythm[0].pos = 0;
                        if ((data & 0x02) != 0) rhythm[1].pos = 0;
                        if ((data & 0x04) != 0) rhythm[2].pos = 0;
                        if ((data & 0x08) != 0) rhythm[3].pos = 0;
                        if ((data & 0x10) != 0) rhythm[4].pos = 0;
                        if ((data & 0x20) != 0) rhythm[5].pos = 0;
                    }
                    else
                    {                   // DUMP
                        rhythmkey &= (byte)(~(byte)data);
                    }
                    break;

                case 0x11:
                    rhythmtl = (sbyte)(~data & 63);
                    break;

                case 0x18:      // Bass Drum
                case 0x19:      // Snare Drum
                case 0x1a:      // Top Cymbal
                case 0x1b:      // Hihat
                case 0x1c:      // Tom-tom
                case 0x1d:      // Rim shot
                    rhythm[addr & 7].pan = (byte)((data >> 6) & 3);
                    rhythm[addr & 7].level = (sbyte)(~data & 31);
                    break;
            }
        }


        public new uint GetReg(uint addr)
        {
            return 0;
        }

        //	音量設定
        public new void SetVolumeFM(int db)
        {
            db = Math.Min(db, 20);
            if (db > -192)
            {
                fm6[0].fmvolume = (int)(16384.0 * Math.Pow(10.0, db / 40.0));
                fm6[1].fmvolume = (int)(16384.0 * Math.Pow(10.0, db / 40.0));
            }
            else
            {
                fm6[0].fmvolume = 0;
                fm6[1].fmvolume = 0;
            }
        }

        public new void SetVolumePSG(int db)
        {
            psg[0].SetVolume(db);
            psg[1].SetVolume(db);
            psg[2].SetVolume(db);
            psg[3].SetVolume(db);
        }

        public void SetVolumeADPCM(int db)
        {
            db = Math.Min(db, 20);
            if (db > -192)
            {
                adpcmb[0].adpcmvol = (int)(65536.0 * Math.Pow(10.0, db / 40.0));
                adpcmb[1].adpcmvol = (int)(65536.0 * Math.Pow(10.0, db / 40.0));
                adpcmb[2].adpcmvol = (int)(65536.0 * Math.Pow(10.0, db / 40.0));
            }
            else
            {
                adpcmb[0].adpcmvol = 0;
                adpcmb[1].adpcmvol = 0;
                adpcmb[2].adpcmvol = 0;
            }

            adpcmb[0].adpcmvolume = (adpcmb[0].adpcmvol * adpcmb[0].adpcmlevel) >> 12;
            adpcmb[1].adpcmvolume = (adpcmb[1].adpcmvol * adpcmb[1].adpcmlevel) >> 12;
            adpcmb[2].adpcmvolume = (adpcmb[2].adpcmvol * adpcmb[2].adpcmlevel) >> 12;
        }

        // ---------------------------------------------------------------------------
        //	チャンネルマスクの設定
        //
        public new void SetChannelMask(uint mask)
        {
            for (int i = 0; i < 6; i++)
            {
                fm6[0].ch[i].Mute(!((mask & (1 << i)) == 0));
                fm6[1].ch[i].Mute(!((mask & (1 << i)) == 0));
            }

            psg[0].SetChannelMask((int)(mask >> 6));
            psg[1].SetChannelMask((int)(mask >> 6));
            psg[2].SetChannelMask((int)(mask >> 6));
            psg[3].SetChannelMask((int)(mask >> 6));

            adpcmb[0].adpcmmask_ = (mask & (1 << 9)) != 0;
            adpcmb[1].adpcmmask_ = (mask & (1 << 9)) != 0;
            adpcmb[2].adpcmmask_ = (mask & (1 << 9)) != 0;

            rhythmmask_ = (int)((mask >> 10) & ((1 << 6) - 1));
        }

        public byte[] GetADPCMBuffer()
        {
            return adpcmbuf;
        }

        // ---------------------------------------------------------------------------
        //	リズム合成
        //
        private void RhythmMix(int[] buffer, int count)
        {
            if (rhythmtvol < 128 && rhythm[0].sample != null && ((rhythmkey & 0x3f) != 0))
            {
                int limit = (int)count * 2;
                visRtmVolume[0] = 0;
                visRtmVolume[1] = 0;
                for (int i = 0; i < 6; i++)
                {
                    Rhythm r = rhythm[i];
                    if ((rhythmkey & (1 << i)) != 0 && (byte)r.level < 128)
                    {
                        int db = fmgen.Limit(rhythmtl + rhythmtvol + r.level + r.volume, 127, -31);
                        int vol = tltable[fmgen.FM_TLPOS + (db << (fmgen.FM_TLBITS - 7))] >> 4;
                        int maskl = -((r.pan >> 1) & 1);
                        int maskr = -(r.pan & 1);

                        if ((rhythmmask_ & (1 << i)) != 0)
                        {
                            maskl = maskr = 0;
                        }

                        for (int dest = 0; dest < limit && r.pos < r.size; dest += 2)
                        {
                            int sample = (r.sample[r.pos / 1024] * vol) >> 12;
                            r.pos += r.step;
                            fmgen.StoreSample(ref buffer[dest + 0], sample & maskl);
                            fmgen.StoreSample(ref buffer[dest + 1], sample & maskr);
                            visRtmVolume[0] += sample & maskl;
                            visRtmVolume[1] += sample & maskr;
                        }
                    }
                }
            }
        }

        // ---------------------------------------------------------------------------
        //	リズム音を読みこむ
        //
        public bool LoadRhythmSample(string path)
        {
            string[] rhythmname = new string[6]
            {
                "BD", "SD", "TOP", "HH", "TOM", "RIM",
            };

            int i;
            for (i = 0; i < 6; i++)
                rhythm[i].pos = ~(uint)0;

            for (i = 0; i < 6; i++)
            {
                byte[] buf = null;
                int filePtr = 0;
                uint fsize;
                string fileName = "";
                if (path != null && path != "")
                    fileName = path;
                fileName = fileName + "2608_";
                fileName = fileName + rhythmname[i];
                fileName = fileName + ".WAV";

                try
                {
                    buf = System.IO.File.ReadAllBytes(fileName);
                }
                catch
                {
                    buf = null;
                }

                if (buf == null)
                {
                    if (i != 5)
                        break;
                    if (path != null && path != "")
                        fileName = path;
                    fileName = fileName + "2608_RYM.WAV";
                    try
                    {
                        buf = System.IO.File.ReadAllBytes(fileName);
                    }
                    catch
                    {
                        break;
                    }
                }

                whdr whdr = new whdr();

                filePtr = 0x10;
                byte[] bufWhdr = new byte[4 + 2 + 2 + 4 + 4 + 2 + 2 + 2];
                for (int ind = 0; ind < bufWhdr.Length; ind++)
                {
                    bufWhdr[ind] = buf[filePtr+ind];
                }

                whdr.chunksize = (uint)(bufWhdr[0] + bufWhdr[1] * 0x100 + bufWhdr[2] * 0x10000 + bufWhdr[3] * 0x10000);
                whdr.tag = (uint)(bufWhdr[4] + bufWhdr[5] * 0x100);
                whdr.nch = (uint)(bufWhdr[6] + bufWhdr[7] * 0x100);
                whdr.rate = (uint)(bufWhdr[8] + bufWhdr[9] * 0x100 + bufWhdr[10] * 0x10000 + bufWhdr[11] * 0x10000);
                whdr.avgbytes = (uint)(bufWhdr[12] + bufWhdr[13] * 0x100 + bufWhdr[14] * 0x10000 + bufWhdr[15] * 0x10000);
                whdr.align = (uint)(bufWhdr[16] + bufWhdr[17] * 0x100);
                whdr.bps = (uint)(bufWhdr[18] + bufWhdr[19] * 0x100);
                whdr.size = (uint)(bufWhdr[20] + bufWhdr[21] * 0x100);

                byte[] subchunkname = new byte[4];
                fsize = 4 + whdr.chunksize;
                do
                {
                    filePtr += (int)fsize;
                    for (int ind = 0; ind < 4; ind++)
                    {
                        subchunkname[ind] = buf[filePtr++];
                    }
                    for (int ind = 0; ind < 4; ind++)
                    {
                        bufWhdr[ind] = buf[filePtr++];
                    }
                    fsize = (uint)(bufWhdr[0] + bufWhdr[1] * 0x100 + bufWhdr[2] * 0x10000 + bufWhdr[3] * 0x10000);
                } while ('d' != subchunkname[0] && 'a' != subchunkname[1] && 't' != subchunkname[2] && 'a' != subchunkname[3]);

                fsize /= 2;
                if (fsize >= 0x100000 || whdr.tag != 1 || whdr.nch != 1)
                    break;
                fsize = (uint)Math.Max(fsize, (1 << 31) / 1024);

                rhythm[i].sample = null;
                rhythm[i].sample = new int[fsize];
                if (rhythm[i].sample == null)
                    break;
                byte[] bufSample = new byte[fsize * 2];
                for (int ind = 0; ind < (fsize * 2); ind++)
                {
                    bufSample[ind] = buf[filePtr++];
                }
                for (int si = 0; si < fsize; si++)
                {
                    rhythm[i].sample[si] = (short)(bufSample[si * 2] + bufSample[si * 2 + 1] * 0x100);
                }

                rhythm[i].rate = whdr.rate;
                rhythm[i].step = rhythm[i].rate * 1024 / rate;
                rhythm[i].pos = rhythm[i].size = fsize * 1024;
            }
            if (i != 6)
            {
                for (i = 0; i < 6; i++)
                {
                    rhythm[i].sample = null;
                }
                return false;
            }
            return true;
        }

        // ---------------------------------------------------------------------------
        //	音量設定
        //
        public void SetVolumeRhythmTotal(int db)
        {
            db = Math.Min(db, 20);
            rhythmtvol = -(db * 2 / 3);
        }

        public void SetVolumeRhythm(int index, int db)
        {
            db = Math.Min(db, 20);
            rhythm[index].volume = -(db * 2 / 3);
        }



    };
}
