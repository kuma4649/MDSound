using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// ---------------------------------------------------------------------------
//	OPN/A/B interface with ADPCM support
//	Copyright (C) cisc 1998, 2003.
// ---------------------------------------------------------------------------
//	$Id: opna.h,v 1.33 2003/06/12 13:14:37 cisc Exp $

// ---------------------------------------------------------------------------
//	class OPN/OPNA
//	OPN/OPNA に良く似た音を生成する音源ユニット
//	
//	interface:
//	bool Init(uint clock, uint rate, bool, const char* path);
//		初期化．このクラスを使用する前にかならず呼んでおくこと．
//		OPNA の場合はこの関数でリズムサンプルを読み込む
//
//		clock:	OPN/OPNA/OPNB のクロック周波数(Hz)
//
//		rate:	生成する PCM の標本周波数(Hz)
//
//		path:	リズムサンプルのパス(OPNA のみ有効)
//				省略時はカレントディレクトリから読み込む
//				文字列の末尾には '\' や '/' などをつけること
//
//		返り値	初期化に成功すれば true
//
//	bool LoadRhythmSample(const char* path)
//		(OPNA ONLY)
//		Rhythm サンプルを読み直す．
//		path は Init の path と同じ．
//		
//	bool SetRate(uint clock, uint rate, bool)
//		クロックや PCM レートを変更する
//		引数等は Init を参照のこと．
//	
//	void Mix(FM_SAMPLETYPE* dest, int nsamples)
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
//	uint GetReg(uint reg)
//		音源のレジスタ reg の内容を読み出す
//		読み込むことが出来るレジスタは PSG, ADPCM の一部，ID(0xff) とか
//	
//	uint ReadStatus()/ReadStatusEx()
//		音源のステータスレジスタを読み出す
//		ReadStatusEx は拡張ステータスレジスタの読み出し(OPNA)
//		busy フラグは常に 0
//	
//	bool Count(uint32 t)
//		音源のタイマーを t [μ秒] 進める．
//		音源の内部状態に変化があった時(timer オーバーフロー)
//		true を返す
//
//	uint32 GetNextEvent()
//		音源のタイマーのどちらかがオーバーフローするまでに必要な
//		時間[μ秒]を返す
//		タイマーが停止している場合は ULONG_MAX を返す… と思う
//	
//	void SetVolumeFM(int db)/SetVolumePSG(int db) ...
//		各音源の音量を＋－方向に調節する．標準値は 0.
//		単位は約 1/2 dB，有効範囲の上限は 20 (10dB)
//

namespace MDSound.fmgen
{

    //	OPN Base -------------------------------------------------------
    public class OPNBase : Timer
    {
        public OPNBase()
        {
            prescale = 0;
            psg = new PSG();
            chip = new fmgen.Chip();
        }

        //	初期化
        public bool Init(uint c, uint r)
        {
            clock = c;
            psgrate = r;

            return true;
        }

        public new void Reset()
        {
            status = 0;
            SetPrescaler(0);
            base.Reset();
            psg.Reset();
        }

        //	音量設定
        public void SetVolumeFM(int db)
        {
            db = Math.Min(db, 20);
            if (db > -192)
                fmvolume = (int)(16384.0 * Math.Pow(10.0, db / 40.0));
            else
                fmvolume = 0;
        }

        public void SetVolumePSG(int db)
        {
            psg.SetVolume(db);
        }

        public void SetLPFCutoff(uint freq)
        {
        }    // obsolete

        protected void SetParameter(fmgen.Channel4 ch, uint addr, uint data)
        {
            uint[] slottable=new uint[4]{ 0, 2, 1, 3 };
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
                        op.SetTL(data & 0x7f, ((regtc & 0x80)!=0) && (csmch == ch));
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

        protected void SetPrescaler(uint p)
        {
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
                chip.SetRatio(ratio);
                psg.SetClock((int)(clock / table[p][1]), (int)psgrate);

                for (int i = 0; i < 8; i++)
                {
                    lfotable[i] = (ratio << (2 + fmgen.FM_LFOCBITS - fmgen.FM_RATIOBITS)) / table2[i];
                }
            }
        }

        protected void RebuildTimeTable()
        {
            int p = prescale;
            prescale = 0xff;//-1;
            SetPrescaler((uint)p);
        }



        protected int fmvolume;

        protected uint clock;             // OPN クロック
        protected uint rate;              // FM 音源合成レート
        protected uint psgrate;           // FMGen  出力レート
        protected new uint status;
        protected fmgen.Channel4 csmch;


        protected uint[] lfotable = new uint[8];

        //	タイマー時間処理
        private void TimerA()
        {
            if ((regtc & 0x80)!=0)
            {
                csmch.KeyControl(0x00);
                csmch.KeyControl(0x0f);
            }
        }

        private byte prescale;

        protected fmgen.Chip chip;
        protected PSG psg;

    }

    //	OPN2 Base ------------------------------------------------------
    public class OPNABase : OPNBase
    {
        public OPNABase()
        {
        }

        ~OPNABase()
        {
        }

        public uint ReadStatus()
        {
            return status & 0x03;
        }

        public uint ReadStatusEx()
        {
            return 0;
        }

        public void SetChannelMask(uint mask)
        {
        }

        private void Intr(bool f)
        {
        }

        private void MakeTable2()
        {
        }

        protected bool Init(uint c, uint r, bool f)
        {
            return false;
        }

        protected bool SetRate(uint c, uint r, bool f)
        {
            return false;
        }

        protected new void Reset()
        {
        }

        protected void SetReg(uint addr, uint data)
        {
        }

        protected void SetADPCMBReg(uint reg, uint data)
        {
        }

        protected uint GetReg(uint addr)
        {
            return 0;
        }

        protected void FMMix(int buffer, int nsamples)
        {
        }

        protected void Mix6(int buffer, int nsamples, int activech)
        {
        }

        protected void MixSubS(int activech, int s)
        {
        }

        protected void MixSubSL(int activech, int s)
        {
        }

        protected new void SetStatus(uint bit)
        {
        }

        protected new void ResetStatus(uint bit)
        {
        }

        protected void UpdateStatus()
        {
        }

        protected void LFO()
        {
        }


        protected void DecodeADPCMB()
        {
        }

        protected void ADPCMBMix(int dest, uint count)
        {
        }

        protected void WriteRAM(uint data)
        {
        }

        protected uint ReadRAM()
        {
            return 0;
        }

        protected int ReadRAMN()
        {
            return -1;
        }

        protected int DecodeADPCMBSample(uint s)
        {
            return -1;
        }


        // FM 音源関係
        protected byte[] pan = new byte[6];
        protected byte[] fnum2 = new byte[9];

        protected byte reg22;
        protected uint reg29;     // OPNA only?

        protected uint stmask;
        protected uint statusnext;

        protected uint lfocount;
        protected uint lfodcount;

        protected uint[] fnum = new uint[6];
        protected uint[] fnum3 = new uint[3];

        // ADPCM 関係
        protected byte[] adpcmbuf;        // ADPCM RAM
        protected uint adpcmmask;     // メモリアドレスに対するビットマスク
        protected uint adpcmnotice;   // ADPCM 再生終了時にたつビット
        protected uint startaddr;     // Start address
        protected uint stopaddr;      // Stop address
        protected uint memaddr;       // 再生中アドレス
        protected uint limitaddr;     // Limit address/mask
        protected int adpcmlevel;     // ADPCM 音量
        protected int adpcmvolume;
        protected int adpcmvol;
        protected uint deltan;            // ⊿N
        protected int adplc;          // 周波数変換用変数
        protected int adpld;          // 周波数変換用変数差分値
        protected uint adplbase;      // adpld の元
        protected int adpcmx;         // ADPCM 合成用 x
        protected int adpcmd;         // ADPCM 合成用 ⊿
        protected int adpcmout;       // ADPCM 合成後の出力
        protected int apout0;         // out(t-2)+out(t-1)
        protected int apout1;         // out(t-1)+out(t)

        protected uint adpcmreadbuf;  // ADPCM リード用バッファ
        protected bool adpcmplay;     // ADPCM 再生中
        protected sbyte granuality;
        protected bool adpcmmask_;

        protected byte control1;     // ADPCM コントロールレジスタ１
        protected byte control2;     // ADPCM コントロールレジスタ２
        protected byte[] adpcmreg = new byte[8];  // ADPCM レジスタの一部分

        protected int rhythmmask_;

        protected fmgen.Channel4[] ch = new fmgen.Channel4[6];

        protected static void BuildLFOTable()
        {
        }

        protected static int[] amtable = new int[fmgen.FM_LFOENTS];
        protected static int[] pmtable = new int[fmgen.FM_LFOENTS];
        protected static int[] tltable = new int[fmgen.FM_TLENTS + fmgen.FM_TLPOS];
        protected static bool tablehasmade;
    };

    //	YM2203(OPN) ----------------------------------------------------
    public class OPN : OPNBase
    {
        public OPN()
        {
            SetVolumeFM(0);
            SetVolumePSG(0);

            csmch = ch[2];

            for (int i = 0; i < 3; i++)
            {
                ch[i].SetChip(chip);
                ch[i].SetType(fmgen.OpType.typeN);
            }
        }

        ~OPN()
        {
        }

        //	初期化
        public bool Init(uint c, uint r, bool ip = false, string s = "")
        {
            if (!SetRate(c, r, ip))
                return false;

            Reset();

            SetVolumeFM(0);
            SetVolumePSG(0);
            SetChannelMask(0);
            return true;
        }

        //	サンプリングレート変更
        public bool SetRate(uint c, uint r, bool f = false)
        {
            base.Init(c, r);
            RebuildTimeTable();
            return true;
        }

        //	リセット
        public new void Reset()
        {
            uint i;
            for (i = 0x20; i < 0x28; i++) SetReg(i, 0);
            for (i = 0x30; i < 0xc0; i++) SetReg(i, 0);
            base.Reset();
            ch[0].Reset();
            ch[1].Reset();
            ch[2].Reset();
        }

        //	合成(2ch)
        public void Mix(int[] buffer, int nsamples)
        {

            psg.Mix(buffer, nsamples);

            // Set F-Number
            ch[0].SetFNum(fnum[0]);
            ch[1].SetFNum(fnum[1]);
            if ((regtc & 0xc0)==0)
                ch[2].SetFNum(fnum[2]);
            else
            {   // 効果音
                ch[2].op[0].SetFNum(fnum3[1]);
                ch[2].op[1].SetFNum(fnum3[2]);
                ch[2].op[2].SetFNum(fnum3[0]);
                ch[2].op[3].SetFNum(fnum[2]);
            }

            int actch = (((ch[2].Prepare() << 2) | ch[1].Prepare()) << 2) | ch[0].Prepare();
            if ((actch & 0x15)!=0)
            {
                //Sample* limit = buffer + nsamples * 2;
                int limit = nsamples * 2;
                //for (Sample* dest = buffer; dest < limit; dest += 2)
                for (int dest = 0; dest < limit; dest += 2)
                {
                    int s = 0;
                    if ((actch & 0x01)!=0) s = ch[0].Calc();
                    if ((actch & 0x04)!=0) s += ch[1].Calc();
                    if ((actch & 0x10)!=0) s += ch[2].Calc();
                    s = ((fmgen.Limit(s, 0x7fff, -0x8000) * fmvolume) >> 14);
                    fmgen.StoreSample(ref buffer[dest + 0], s);
                    fmgen.StoreSample(ref buffer[dest + 1], s);
                }
            }
        }

        //	レジスタアレイにデータを設定
        public void SetReg(uint addr, uint data)
        {
            //	LOG2("reg[%.2x] <- %.2x\n", addr, data);
            if (addr >= 0x100)
                return;

            int c = (int)(addr & 3);
            switch (addr)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                case 4:
                case 5:
                case 6:
                case 7:
                case 8:
                case 9:
                case 10:
                case 11:
                case 12:
                case 13:
                case 14:
                case 15:
                    psg.SetReg(addr, (byte)data);
                    break;

                case 0x24:
                case 0x25:
                    SetTimerA(addr, data);
                    break;

                case 0x26:
                    SetTimerB(data);
                    break;

                case 0x27:
                    SetTimerControl(data);
                    break;

                case 0x28:      // Key On/Off
                    if ((data & 3) < 3)
                        ch[data & 3].KeyControl(data >> 4);
                    break;

                case 0x2d:
                case 0x2e:
                case 0x2f:
                    SetPrescaler(addr - 0x2d);
                    break;

                // F-Number
                case 0xa0:
                case 0xa1:
                case 0xa2:
                    fnum[c] = (uint)(data + fnum2[c] * 0x100);
                    break;

                case 0xa4:
                case 0xa5:
                case 0xa6:
                    fnum2[c] = (byte)(data);
                    break;

                case 0xa8:
                case 0xa9:
                case 0xaa:
                    fnum3[c] = (uint)(data + fnum2[c + 3] * 0x100);
                    break;

                case 0xac:
                case 0xad:
                case 0xae:
                    fnum2[c + 3] = (byte)(data);
                    break;

                case 0xb0:
                case 0xb1:
                case 0xb2:
                    ch[c].SetFB((data >> 3) & 7);
                    ch[c].SetAlgorithm(data & 7);
                    break;

                default:
                    if (c < 3)
                    {
                        if ((addr & 0xf0) == 0x60)
                            data &= 0x1f;
                        base.SetParameter(ch[c], addr, data);
                    }
                    break;
            }
        }

        //	レジスタ読み込み
        public uint GetReg(uint addr)
        {
            if (addr < 0x10)
                return psg.GetReg(addr);
            else
                return 0;
        }

        public uint ReadStatus()
        {
            return status & 0x03;
        }

        public uint ReadStatusEx()
        {
            return 0xff;
        }

        //	マスク設定
        public void SetChannelMask(uint mask)
        {
            for (int i = 0; i < 3; i++)
                ch[i].Mute(!!((mask & (1 << i)) != 0));
            psg.SetChannelMask((int)(mask >> 6));
        }

        public int dbgGetOpOut(int c, int s)
        {
            return ch[c].op[s].dbgopout_;
        }

        public int dbgGetPGOut(int c, int s)
        {
            return ch[c].op[s].dbgpgout_;
        }

        public fmgen.Channel4 dbgGetCh(int c)
        {
            return ch[c];
        }

        private void Intr(bool f)
        {
        }

        //	ステータスフラグ設定
        private new void SetStatus(uint bits)
        {
            if ((status & bits)==0)
            {
                status |= bits;
                Intr(true);
            }
        }

        private new void ResetStatus(uint bit)
        {
            status &= ~bit;
            if (status==0)
                Intr(false);
        }

        private uint[] fnum = new uint[3];
        private uint[] fnum3 = new uint[3];
        private byte[] fnum2 = new byte[6];

        private fmgen.Channel4[] ch = new fmgen.Channel4[3] { new fmgen.Channel4(), new fmgen.Channel4(), new fmgen.Channel4() };

    };

    //	YM2608(OPNA) ---------------------------------------------------
    public class OPNA : OPNABase
    {
        public OPNA()
        {
        }

        ~OPNA()
        {
        }

        public bool Init(uint c, uint r, bool f = false, string rhythmpath = "")
        {
            return false;
        }

        public bool LoadRhythmSample(string s)
        {
            return false;
        }

        public new bool SetRate(uint c, uint r, bool f = false)
        {
            return false;
        }

        public void Mix(int[] buffer, int nsamples)
        {
        }

        public new void Reset()
        {
        }

        public new void SetReg(uint addr, uint data)
        {
        }

        public new uint GetReg(uint addr)
        {
            return 0;
        }

        public void SetVolumeADPCM(int db)
        {
        }

        public void SetVolumeRhythmTotal(int db)
        {
        }

        public void SetVolumeRhythm(int index, int db)
        {
        }


        public byte[] GetADPCMBuffer()
        {
            return adpcmbuf;
        }

        public int dbgGetOpOut(int c, int s)
        {
            return ch[c].op[s].dbgopout_;
        }

        public int dbgGetPGOut(int c, int s)
        {
            return ch[c].op[s].dbgpgout_;
        }

        public fmgen.Channel4 dbgGetCh(int c)
        {
            return ch[c];
        }


        private class Rhythm
        {
            byte pan;      // ぱん
            sbyte level;     // おんりょう
            int volume;     // おんりょうせってい
            int[] sample;      // さんぷる
            uint size;      // さいず
            uint pos;       // いち
            uint step;      // すてっぷち
            uint rate;      // さんぷるのれーと
        };

        private void RhythmMix(int[] buffer, uint count)
        {
        }

        // リズム音源関係
        private Rhythm[] rhythm = new Rhythm[6];

        private sbyte rhythmtl;      // リズム全体の音量
        private int rhythmtvol;
        private byte rhythmkey;     // リズムのキー
    };

    //	YM2610/B(OPNB) ---------------------------------------------------
    public class OPNB : OPNABase
    {
        public OPNB()
        {
        }

        ~OPNB()
        {
        }

        public bool Init(uint c, uint r, bool f = false,
                     byte[] _adpcma = null, int _adpcma_size = 0,
                     byte[] _adpcmb = null, int _adpcmb_size = 0)
        {
            return false;
        }

        public new bool SetRate(uint c, uint r, bool f = false)
        {
            return false;
        }

        public void Mix(int[] buffer, int nsamples)
        {
        }

        public new void Reset()
        {
        }

        public new void SetReg(uint addr, uint data)
        {
        }

        public new uint GetReg(uint addr)
        {
            return 0;
        }

        public new uint ReadStatusEx()
        {
            return 0;
        }

        public void SetVolumeADPCMATotal(int db)
        {
        }

        public void SetVolumeADPCMA(int index, int db)
        {
        }

        public void SetVolumeADPCMB(int db)
        {
        }

        //		void	SetChannelMask(uint mask);

        public class ADPCMA
        {
            byte pan;      // ぱん
            sbyte level;     // おんりょう
            int volume;     // おんりょうせってい
            uint pos;       // いち
            uint step;      // すてっぷち

            uint start;     // 開始
            uint stop;      // 終了
            uint nibble;        // 次の 4 bit
            int adpcmx;     // 変換用
            int adpcmd;     // 変換用
        };

        private int DecodeADPCMASample(uint a)
        {
            return -1;
        }

        public void ADPCMAMix(int[] buffer, uint count)
        {
        }

        public static void InitADPCMATable()
        {
        }

        // ADPCMA 関係
        public byte[] adpcmabuf;       // ADPCMA ROM
        public int adpcmasize;
        public ADPCMA[] adpcma = new ADPCMA[6];
        public sbyte adpcmatl;      // ADPCMA 全体の音量
        public int adpcmatvol;
        public byte adpcmakey;        // ADPCMA のキー
        public int adpcmastep;
        public byte[] adpcmareg = new byte[32];

        public static int[] jedi_table = new int[(48 + 1) * 16];

        public new fmgen.Channel4[] ch = new fmgen.Channel4[6];
    };

    //	YM2612/3438(OPN2) ----------------------------------------------------
    public class OPN2 : OPNBase
    {
        public OPN2()
        {
        }

        ~OPN2()
        {
        }

        public bool Init(uint c, uint r, bool f = false, string s = null)
        {
            return false;
        }

        public bool SetRate(uint c, uint r, bool f)
        {
            return false;
        }

        public new void Reset()
        {
        }

        public void Mix(int[] buffer, int nsamples)
        {
        }

        public void SetReg(uint addr, uint data)
        {
        }

        public uint GetReg(uint addr)
        {
            return 0;
        }

        public uint ReadStatus()
        {
            return status & 0x03;
        }

        public uint ReadStatusEx()
        {
            return 0xff;
        }

        public void SetChannelMask(uint mask)
        {
        }

        private void Intr(bool f)
        {
        }

        private new void SetStatus(uint bit)
        {
        }

        private new void ResetStatus(uint bit)
        {
        }

        private uint[] fnum = new uint[3];
        private uint[] fnum3 = new uint[3];
        private byte[] fnum2 = new byte[6];

        // 線形補間用ワーク
        private int mixc, mixc1;

        private fmgen.Channel4[] ch = new fmgen.Channel4[3];
    };

}




