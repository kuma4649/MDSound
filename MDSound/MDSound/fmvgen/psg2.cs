using System;
using System.Diagnostics;

namespace MDSound.fmvgen
{
    public class PSG2 : fmgen.PSG
    {

        protected byte[] panpot = new byte[3];
        protected byte[] phaseReset = new byte[3];
        protected bool[] phaseResetBefore = new bool[3];
        protected byte[] duty = new byte[3];
        private reverb reverb;
        private distortion distortion;
        private chorus chorus;
        private effect.HPFLPF hpflpf;
        private effect.ReversePhase reversePhase;
        private effect.Compressor compressor;
        private int efcStartCh;
        private byte[][] user = new byte[6][] { new byte[64], new byte[64], new byte[64], new byte[64], new byte[64], new byte[64] };
        private int userDefCounter = 0;
        private int userDefNum = 0;
        private Func<int, uint, int>[] tblGetSample;
        private int num;
        protected double ncountDbl;
        private const double ncountDiv = 32.0;

        public PSG2(int num,reverb reverb, distortion distortion,chorus chorus, effect.HPFLPF hpflpf, effect.ReversePhase reversePhase,effect.Compressor compressor, int efcStartCh)
        {
            this.num = num;
            this.reverb = reverb;
            this.distortion = distortion;
            this.chorus = chorus;
            this.hpflpf = hpflpf;
            this.reversePhase = reversePhase;
            this.compressor = compressor;
            this.efcStartCh = efcStartCh;
            makeTblGetSample();
        }

        ~PSG2()
        {
        }

        public override void SetReg(uint regnum, byte data)
        {
            if (regnum >= 0x10) return;

            reg[regnum] = data;
            int tmp;
            switch (regnum)
            {
                case 0:     // ChA Fine Tune
                case 1:     // ChA Coarse Tune
                    tmp = ((reg[0] + reg[1] * 256) & 0xfff);
                    speriod[0] = (uint)(tmp != 0 ? tperiodbase / tmp : tperiodbase);
                    duty[0] = (byte)(reg[1] >> 4);
                    duty[0] = (byte)(duty[0] < 8 ? (7 - duty[0]) : duty[0]);
                    break;

                case 2:     // ChB Fine Tune
                case 3:     // ChB Coarse Tune
                    tmp = ((reg[2] + reg[3] * 256) & 0xfff);
                    speriod[1] = (uint)(tmp != 0 ? tperiodbase / tmp : tperiodbase);
                    duty[1] = (byte)(reg[3] >> 4);
                    duty[1] = (byte)(duty[1] < 8 ? (7 - duty[1]) : duty[1]);
                    break;

                case 4:     // ChC Fine Tune
                case 5:     // ChC Coarse Tune
                    tmp = ((reg[4] + reg[5] * 256) & 0xfff);
                    speriod[2] = (uint)(tmp != 0 ? tperiodbase / tmp : tperiodbase);
                    duty[2] = (byte)(reg[5] >> 4);
                    duty[2] = (byte)(duty[2] < 8 ? (7 - duty[2]) : duty[2]);
                    break;

                case 6:     // Noise generator control
                    data &= 0x1f;
                    nperiod = data != 0 ? nperiodbase / data : nperiodbase;
                    break;

                case 7:
                    if ((data & 0x09) == 0) { phaseResetBefore[0] = false; }
                    if ((data & 0x12) == 0) { phaseResetBefore[1] = false; }
                    if ((data & 0x24) == 0) { phaseResetBefore[2] = false; }
                    break;
                case 8:
                    olevel[0] = (uint)((mask & 1) != 0 ? EmitTable[(data & 15) * 2 + 1] : 0);
                    panpot[0] = (byte)(data >> 6);
                    panpot[0] = (byte)(panpot[0] == 0 ? 3 : panpot[0]);
                    phaseReset[0] = (byte)((data & 0x20) != 0 ? 1 : 0);
                    break;

                case 9:
                    olevel[1] = (uint)((mask & 2) != 0 ? EmitTable[(data & 15) * 2 + 1] : 0);
                    panpot[1] = (byte)(data >> 6);
                    panpot[1] = (byte)(panpot[1] == 0 ? 3 : panpot[1]);
                    phaseReset[1] = (byte)((data & 0x20) != 0 ? 1 : 0);
                    break;

                case 10:
                    olevel[2] = (uint)((mask & 4) != 0 ? EmitTable[(data & 15) * 2 + 1] : 0);
                    panpot[2] = (byte)(data >> 6);
                    panpot[2] = (byte)(panpot[2] == 0 ? 3 : panpot[2]);
                    phaseReset[2] = (byte)((data & 0x20) != 0 ? 1 : 0);
                    break;

                case 11:    // Envelop period
                case 12:
                    tmp = ((reg[11] + reg[12] * 256) & 0xffff);
                    eperiod = (uint)(tmp != 0 ? eperiodbase / tmp : eperiodbase * 2);
                    break;

                case 13:    // Envelop shape
                    ecount = 0;
                    envelop = enveloptable[data & 15];
                    if ((data & 0x80) != 0) userDefCounter = 0;
                    userDefNum = ((data & 0x70) >> 4) % 6;
                    break;

                case 14:    // Define Wave Data
                    user[userDefNum][userDefCounter & 63] = data;
                    //Console.WriteLine("{3} : WF {0} {1} {2} ", ((data & 0x70) >> 4) % 6, userDefCounter & 63, (byte)(data & 0xf), data);
                    userDefCounter++;
                    break;
            }

        }

        private byte[] chenable = new byte[3];
        private byte[] nenable = new byte[3];
        private uint?[] p = new uint?[3];

        public override void Mix(int[] dest, int nsamples)
        {
            byte r7 = (byte)~reg[7];

            if (((r7 & 0x3f) | ((reg[8] | reg[9] | reg[10]) & 0x1f)) != 0)
            {
                chenable[0] = (byte)((((r7 & 0x01) != 0) && (speriod[0] <= (uint)(1 << toneshift))) ? 15 : 0);
                chenable[1] = (byte)((((r7 & 0x02) != 0) && (speriod[1] <= (uint)(1 << toneshift))) ? 15 : 0);
                chenable[2] = (byte)((((r7 & 0x04) != 0) && (speriod[2] <= (uint)(1 << toneshift))) ? 15 : 0);
                nenable[0] = (byte)((r7 & 0x08) != 0 ? 1 : 0);
                nenable[1] = (byte)((r7 & 0x10) != 0 ? 1 : 0);
                nenable[2] = (byte)((r7 & 0x20) != 0 ? 1 : 0);
                p[0] = ((mask & 1) != 0 && (reg[8] & 0x10) != 0) ? (uint?)null : 0;
                p[1] = ((mask & 2) != 0 && (reg[9] & 0x10) != 0) ? (uint?)null : 1;
                p[2] = ((mask & 4) != 0 && (reg[10] & 0x10) != 0) ? (uint?)null : 2;
                if (!phaseResetBefore[0] && phaseReset[0] != 0 && (r7 & 0x09) != 0) { scount[0] = 0; phaseResetBefore[0] = true; }
                if (!phaseResetBefore[1] && phaseReset[1] != 0 && (r7 & 0x12) != 0) { scount[1] = 0; phaseResetBefore[1] = true; }
                if (!phaseResetBefore[2] && phaseReset[2] != 0 && (r7 & 0x24) != 0) { scount[2] = 0; phaseResetBefore[2] = true; }

                int noise, sample, sampleL, sampleR, revSampleL, revSampleR;
                uint env;
                int nv = 0;

                if (p[0] != null && p[1] != null && p[2] != null)
                {
                    // エンベロープ無し
                    if ((r7 & 0x38) == 0)
                    {
                        int ptrDest = 0;
                        // ノイズ無し
                        for (int i = 0; i < nsamples; i++)
                        {
                            sampleL = 0;
                            sampleR = 0;
                            revSampleL = 0;
                            revSampleR = 0;

                            for (int j = 0; j < (1 << oversampling); j++)
                            {
                                for (int k = 0; k < 3; k++)
                                {
                                    sample = tblGetSample[duty[k]](k, olevel[k]);
                                    int L = sample;
                                    int R = sample;
                                    distortion.Mix(efcStartCh + k, ref L, ref R);
                                    chorus.Mix(efcStartCh + k, ref L, ref R);
                                    hpflpf.Mix(efcStartCh + k, ref L, ref R);
                                    compressor.Mix(efcStartCh + k, ref L, ref R);
                                    L = (panpot[k] & 2) != 0 ? L : 0;
                                    R = (panpot[k] & 1) != 0 ? R : 0;
                                    L *= reversePhase.SSG[num][k][0];
                                    R *= reversePhase.SSG[num][k][1];
                                    revSampleL += (int)(L * reverb.SendLevel[efcStartCh + k] * 0.6);
                                    revSampleR += (int)(R * reverb.SendLevel[efcStartCh + k] * 0.6);
                                    sampleL += L;
                                    sampleR += R;
                                    scount[k] += speriod[k];
                                }

                            }
                            sampleL /= (1 << oversampling);
                            sampleR /= (1 << oversampling);
                            revSampleL /= (1 << oversampling);
                            revSampleR /= (1 << oversampling);

                            StoreSample(ref dest[ptrDest + 0], sampleL);
                            StoreSample(ref dest[ptrDest + 1], sampleR);
                            reverb.StoreDataC(revSampleL, revSampleR);
                            ptrDest += 2;

                            visVolume = sampleL;

                        }
                    }
                    else
                    {
                        int ptrDest = 0;
                        // ノイズ有り
                        for (int i = 0; i < nsamples; i++)
                        {
                            sampleL = 0;
                            sampleR = 0;
                            revSampleL = 0;
                            revSampleR = 0;
                            sample = 0;
                            for (int j = 0; j < (1 << oversampling); j++)
                            {
                                noise = (int)(noisetable[((uint)ncountDbl >> (int)((noiseshift + oversampling + 6)) & (noisetablesize - 1))]
                                    >> (int)((uint)ncountDbl >> (noiseshift + oversampling + 1)));

                                ncountDbl += ((double)nperiod / ((reg[6] & 0x20) != 0 ? ncountDiv : 1.0));

                                for (int k = 0; k < 3; k++)
                                {
                                    sample = tblGetSample[duty[k]](k, olevel[k]);
                                    int L = sample;
                                    int R = sample;

                                    //ノイズ
                                    nv = ((int)(scount[k] >> (toneshift + oversampling)) & 0 | (nenable[k] & noise)) - 1;
                                    sample = (int)((olevel[k] + nv) ^ nv);
                                    L += sample;
                                    R += sample;

                                    distortion.Mix(efcStartCh + k, ref L, ref R);
                                    chorus.Mix(efcStartCh + k, ref L, ref R);
                                    hpflpf.Mix(efcStartCh + k, ref L, ref R);
                                    compressor.Mix(efcStartCh + k, ref L, ref R);
                                    L = (panpot[k] & 2) != 0 ? L : 0;
                                    R = (panpot[k] & 1) != 0 ? R : 0;
                                    L *= reversePhase.SSG[num][k][0];
                                    R *= reversePhase.SSG[num][k][1];
                                    revSampleL += (int)(L * reverb.SendLevel[efcStartCh + k] * 0.6);
                                    revSampleR += (int)(R * reverb.SendLevel[efcStartCh + k] * 0.6);
                                    sampleL += L;
                                    sampleR += R;
                                    scount[k] += speriod[k];
                                }
                            }

                            sampleL /= (1 << oversampling);
                            sampleR /= (1 << oversampling);
                            StoreSample(ref dest[ptrDest + 0], sampleL);
                            StoreSample(ref dest[ptrDest + 1], sampleR);
                            reverb.StoreDataC(revSampleL, revSampleR);
                            ptrDest += 2;

                            visVolume = sampleL;

                        }
                    }

                    // エンベロープの計算をさぼった帳尻あわせ
                    ecount = (uint)((ecount >> 8) + (eperiod >> (8 - oversampling)) * nsamples);
                    if (ecount >= (1 << (envshift + 6 + oversampling - 8)))
                    {
                        if ((reg[0x0d] & 0x0b) != 0x0a)
                            ecount |= (1 << (envshift + 5 + oversampling - 8));
                        ecount &= (1 << (envshift + 6 + oversampling - 8)) - 1;
                    }
                    ecount <<= 8;
                }
                else
                {
                    int ptrDest = 0;
                    // エンベロープあり
                    for (int i = 0; i < nsamples; i++)
                    {
                        sampleL = 0;
                        sampleR = 0;
                        revSampleL = 0;
                        revSampleR = 0;

                        for (int j = 0; j < (1 << oversampling); j++)
                        {
                            env = envelop[ecount >> (envshift + oversampling)];
                            ecount += eperiod;
                            if (ecount >= (1 << (envshift + 6 + oversampling)))
                            {
                                if ((reg[0x0d] & 0x0b) != 0x0a)
                                    ecount |= (1 << (envshift + 5 + oversampling));
                                ecount &= (1 << (envshift + 6 + oversampling)) - 1;
                            }
                            noise = (int)(noisetable[((uint)ncountDbl >> (int)((noiseshift + oversampling + 6)) & (noisetablesize - 1))]
                                >> (int)((uint)ncountDbl >> (noiseshift + oversampling + 1)));
                            ncountDbl += (nperiod / ((reg[6] & 0x20) != 0 ? ncountDiv : 1.0));

                            for (int k = 0; k < 3; k++)
                            {
                                uint lv = (p[k] == null ? env : olevel[k]);
                                sample = tblGetSample[duty[k]](k, lv);
                                int L = sample;
                                int R = sample;

                                //ノイズ
                                nv = ((int)(scount[k] >> (toneshift + oversampling)) & 0 | (nenable[k] & noise)) - 1;
                                sample = (int)((lv + nv) ^ nv);
                                L += sample;
                                R += sample;

                                distortion.Mix(efcStartCh + k, ref L, ref R);
                                chorus.Mix(efcStartCh + k, ref L, ref R);
                                hpflpf.Mix(efcStartCh + k, ref L, ref R);
                                compressor.Mix(efcStartCh + k, ref L, ref R);
                                L = (panpot[k] & 2) != 0 ? L : 0;
                                R = (panpot[k] & 1) != 0 ? R : 0;
                                L *= reversePhase.SSG[num][k][0];
                                R *= reversePhase.SSG[num][k][1];
                                revSampleL += (int)(L * reverb.SendLevel[efcStartCh + k] * 0.6);
                                revSampleR += (int)(R * reverb.SendLevel[efcStartCh + k] * 0.6);
                                sampleL += L;
                                sampleR += R;
                                scount[k] += speriod[k];
                            }

                        }
                        sampleL /= (1 << oversampling);
                        sampleR /= (1 << oversampling);
                        revSampleL /= (1 << oversampling);
                        revSampleR /= (1 << oversampling);

                        StoreSample(ref dest[ptrDest + 0], sampleL);
                        StoreSample(ref dest[ptrDest + 1], sampleR);
                        reverb.StoreDataC(revSampleL, revSampleR);
                        ptrDest += 2;

                        visVolume = sampleL;

                    }
                }
            }
        }

        private void makeTblGetSample()
        {
            tblGetSample = new Func<int, uint, int>[] {
                GetSampleFromDuty,
                GetSampleFromDuty,
                GetSampleFromDuty,
                GetSampleFromDuty,
                GetSampleFromDuty,
                GetSampleFromDuty,
                GetSampleFromDuty,
                GetSampleFromDuty,
                GetSampleFromTriangle,
                GetSampleFromSaw,
                GetSampleFromUserDef,
                GetSampleFromUserDef,
                GetSampleFromUserDef,
                GetSampleFromUserDef,
                GetSampleFromUserDef,
                GetSampleFromUserDef
            };
        }

        private int GetSampleFromUserDef(int k, uint lv)
        {
            if (chenable[k] == 0) return 0;

            //ユーザー定義
            uint pos = (scount[k] >> (toneshift + oversampling - 3 - 2)) & 63;
            int n = user[duty[k] - 10][pos];
            int x = n - 128;
            return (int)((lv * x) >> 7);
        }

        private int GetSampleFromSaw(int k, uint lv)
        {
            if (chenable[k] == 0) return 0;

            int n = ((int)(scount[k] >> (toneshift + oversampling - 3)) & chenable[k]);
            //のこぎり波
            int x = n < 7 ? n : (n - 16);
            return (int)((lv * x) >> 2);
        }

        private int GetSampleFromTriangle(int k, uint lv)
        {
            if (chenable[k] == 0) return 0;

            int n = ((int)(scount[k] >> (toneshift + oversampling - 3)) & chenable[k]);
            //三角波
            int x = n < 8 ? (n - 4) : (15 - 4 - n);
            return (int)((lv * x) >> 1);
        }

        private int GetSampleFromDuty(int k, uint lv)
        {
            if (chenable[k] == 0) return 0;

            int n = ((int)(scount[k] >> (toneshift + oversampling - 3)) & chenable[k]);
            //矩形波
            int x = n > duty[k] ? 0 : -1;
            return (int)((lv + x) ^ x);
        }
    }
}
