using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDSound.fmvgen
{
    public class FM6
    {
        public OPNA2 parent = null;

        //protected OPNA2 parent;
        public int fmvolume;
        protected fmvgen.Channel4 csmch;
        protected uint[] fnum = new uint[6];
        protected uint[] fnum3 = new uint[3];
        public fmvgen.Channel4[] ch = new fmvgen.Channel4[6];

        protected byte[] fnum2 = new byte[9];

        protected byte reg22;
        protected uint reg29;     // OPNA only?
        protected byte[] pan = new byte[6];
        protected float[] panTable = new float[4] { 1.0f, 0.75f, 0.5f, 0.25f };
        protected float[] panL = new float[6];
        protected float[] panR = new float[6];
        protected bool[] ac = new bool[6];
        protected uint lfocount;
        protected uint lfodcount;
        public int[] visVolume = new int[2] { 0, 0 };
        protected byte regtc;
        public fmvgen.Chip chip;

        protected uint[] lfotable = new uint[8];

        public FM6()
        {
            chip = new fmvgen.Chip();

            for (int i = 0; i < 6; i++)
            {
                ch[i] = new fmvgen.Channel4();
                ch[i].SetChip(chip);
                ch[i].SetType(fmvgen.OpType.typeN);
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
                    panL[c] = panTable[(data >> 6) & 3];
                    break;

                case 0x1ac:
                case 0x1ad:
                case 0x1ae:
                    c += 3;
                    panL[c] = panTable[(data >> 6) & 3];
                    break;

                // Algorithm -------------------------------------------------------------

                case 0x1b0:
                case 0x1b1:
                case 0x1b2:
                    c += 3;
                    ch[c].SetFB((data >> 3) & 7);
                    ch[c].SetAlgorithm(data & 7);
                    panR[c] = panTable[(data >> 6) & 3];
                    break;
                case 0xb0:
                case 0xb1:
                case 0xb2:
                    ch[c].SetFB((data >> 3) & 7);
                    ch[c].SetAlgorithm(data & 7);
                    panR[c] = panTable[(data >> 6) & 3];
                    break;

                case 0x1b4:
                case 0x1b5:
                case 0x1b6:
                    c += 3;
                    pan[c] = (byte)((data >> 6) & 3);
                    ch[c].SetMS(data);
                    ac[c] = (data & 0x08) != 0;
                    break;
                case 0xb4:
                case 0xb5:
                case 0xb6:
                    pan[c] = (byte)((data >> 6) & 3);
                    ch[c].SetMS(data);
                    ac[c] = (data & 0x08) != 0;
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

        protected void SetParameter(fmvgen.Channel4 ch, uint addr, uint data)
        {
            uint[] slottable = new uint[4] { 0, 2, 1, 3 };
            byte[] sltable = new byte[16]{
              0,   4,   8,  12,  16,  20,  24,  28,
             32,  36,  40,  44,  48,  52,  56, 124
            };

            if ((addr & 3) < 3)
            {
                uint slot = slottable[(addr >> 2) & 3];
                fmvgen.Operator op = ch.op[slot];

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
                        op.SetDT2((data & 0x60) >> 5);
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

        public void Mix(int[] buffer, int nsamples, byte regtc)
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

                int v = ((fmvgen.Limit(ibuf[2] + ibuf[3], 0x7fff, -0x8000) * fmvolume) >> 14);
                fmvgen.StoreSample(ref buffer[dest + 0], v);// ((fmgen.Limit(ibuf[2] + ibuf[3], 0x7fff, -0x8000) * fmvolume) >> 14));
                visVolume[0] = v;

                v = ((fmvgen.Limit(ibuf[1] + ibuf[3], 0x7fff, -0x8000) * fmvolume) >> 14);
                fmvgen.StoreSample(ref buffer[dest + 1], v);// ((fmgen.Limit(ibuf[1] + ibuf[3], 0x7fff, -0x8000) * fmvolume) >> 14));
                visVolume[1] = v;
            }
        }

        protected void MixSubS(int activech, int[] dest, int[] buf)
        {
            int v;
            if ((activech & 0x001) != 0)
            {
                v = ch[0].Calc();
                buf[2] = (int)((dest[0] >> 1) * v * panL[0]);
                buf[1] = (int)((dest[0] & 0x1) * v * panR[0]);
            }
            if ((activech & 0x004) != 0)
            {
                v = ch[1].Calc();
                buf[2] += (int)((dest[1] >> 1) * v * panL[1]);
                buf[1] += (int)((dest[1] & 0x1) * v * panR[1]);
            }
            if ((activech & 0x010) != 0)
            {
                v = ch[2].Calc();
                buf[2] += (int)((dest[2] >> 1) * v * panL[2]);
                buf[1] += (int)((dest[2] & 0x1) * v * panR[2]);
            }
            if ((activech & 0x040) != 0)
            {
                v = ch[3].Calc();
                buf[2] += (int)((dest[3] >> 1) * v * panL[3]);
                buf[1] += (int)((dest[3] & 0x1) * v * panR[3]);
            }
            if ((activech & 0x100) != 0)
            {
                v = ch[4].Calc();
                buf[2] += (int)((dest[4] >> 1) * v * panL[4]);
                buf[1] += (int)((dest[4] & 0x1) * v * panR[4]);
            }
            if ((activech & 0x400) != 0)
            {
                v = ch[5].Calc();
                buf[2] += (int)((dest[5] >> 1) * v * panL[5]);
                buf[1] += (int)((dest[5] & 0x1) * v * panR[5]);
            }
        }

        protected void MixSubSL(int activech, int[] dest, int[] buf)
        {
            int v;
            if ((activech & 0x001) != 0)
            {
                v = ch[0].CalcL();
                buf[2] = (int)((dest[0] >> 1) * v * panL[0]);
                buf[1] = (int)((dest[0] & 0x1) * v * panR[0]);
            }
            if ((activech & 0x004) != 0)
            {
                v = ch[1].CalcL();
                buf[2] += (int)((dest[1] >> 1) * v * panL[1]);
                buf[1] += (int)((dest[1] & 0x1) * v * panR[1]);
            }
            if ((activech & 0x010) != 0)
            {
                v = ch[2].CalcL();
                buf[2] += (int)((dest[2] >> 1) * v * panL[2]);
                buf[1] += (int)((dest[2] & 0x1) * v * panR[2]);
            }
            if ((activech & 0x040) != 0)
            {
                v = ch[3].CalcL();
                buf[2] += (int)((dest[3] >> 1) * v * panL[3]);
                buf[1] += (int)((dest[3] & 0x1) * v * panR[3]);
            }
            if ((activech & 0x100) != 0)
            {
                v = ch[4].CalcL();
                buf[2] += (int)((dest[4] >> 1) * v * panL[4]);
                buf[1] += (int)((dest[4] & 0x1) * v * panR[4]);
            }
            if ((activech & 0x400) != 0)
            {
                v = ch[5].CalcL();
                buf[2] += (int)((dest[5] >> 1) * v * panL[5]);
                buf[1] += (int)((dest[5] & 0x1) * v * panR[5]);
            }
        }

        protected void LFO()
        {
            //	LOG3("%4d - %8d, %8d\n", c, lfocount, lfodcount);

            //	Operator::SetPML(pmtable[(lfocount >> (FM_LFOCBITS+1)) & 0xff]);
            //	Operator::SetAML(amtable[(lfocount >> (FM_LFOCBITS+1)) & 0xff]);
            chip.SetPML((uint)(OPNA2.pmtable[(lfocount >> (fmvgen.FM_LFOCBITS + 1)) & 0xff]));
            chip.SetAML((uint)(OPNA2.amtable[(lfocount >> (fmvgen.FM_LFOCBITS + 1)) & 0xff]));
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
                panL[i] = panTable[0];
                panR[i] = panTable[0];
                ch[i].Reset();
            }
        }

    }

}
