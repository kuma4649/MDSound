using System;
using System.Diagnostics;

namespace MDSound.fmvgen
{
    public class PSG2 : fmgen.PSG
    {

        protected byte[] panpot = new byte[3];
        protected byte[] duty = new byte[3];
        private reverb reverb;
        private distortion distortion;
        private chorus chorus;
        private int efcStartCh;
        private byte[][] user = new byte[6][] { new byte[64], new byte[64], new byte[64], new byte[64], new byte[64], new byte[64] };
        private int userDefCounter = 0;

        public PSG2(reverb reverb, distortion distortion,chorus chorus, int efcStartCh)
        {
            this.reverb = reverb;
            this.distortion = distortion;
            this.chorus = chorus;
            this.efcStartCh = efcStartCh;
        }

        ~PSG2()
        {
        }

        public override void SetReg(uint regnum, byte data)
        {
            if (regnum < 0x10)
            {
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

                    case 8:
                        olevel[0] = (uint)((mask & 1) != 0 ? EmitTable[(data & 15) * 2 + 1] : 0);
                        panpot[0] = (byte)(data >> 6);
                        panpot[0] = (byte)(panpot[0] == 0 ? 3 : panpot[0]);
                        break;

                    case 9:
                        olevel[1] = (uint)((mask & 2) != 0 ? EmitTable[(data & 15) * 2 + 1] : 0);
                        panpot[1] = (byte)(data >> 6);
                        panpot[1] = (byte)(panpot[1] == 0 ? 3 : panpot[1]);
                        break;

                    case 10:
                        olevel[2] = (uint)((mask & 4) != 0 ? EmitTable[(data & 15) * 2 + 1] : 0);
                        panpot[2] = (byte)(data >> 6);
                        panpot[2] = (byte)(panpot[2] == 0 ? 3 : panpot[2]);
                        break;

                    case 11:    // Envelop period
                    case 12:
                        tmp = ((reg[11] + reg[12] * 256) & 0xffff);
                        eperiod = (uint)(tmp != 0 ? eperiodbase / tmp : eperiodbase * 2);
                        break;

                    case 13:    // Envelop shape
                        ecount = 0;
                        envelop = enveloptable[data & 15];
                        break;

                    case 14:    // Define Wave Data
                        if ((data & 0x80) != 0) userDefCounter = 0;
                        user[((data & 0x70) >> 4) % 6][userDefCounter & 63] = (byte)(data & 0xf);
                        //Console.WriteLine("{3} : WF {0} {1} {2} ", ((data & 0x70) >> 4) % 6, userDefCounter & 63, (byte)(data & 0xf), data);
                        userDefCounter++;
                        break;
                }
            }
        }

        public override void Mix(int[] dest, int nsamples)
        {
            byte[] chenable = new byte[3];
            byte[] nenable = new byte[3];
            uint?[] p = new uint?[3];
            byte r7 = (byte)~reg[7];

            if (((r7 & 0x3f) | ((reg[8] | reg[9] | reg[10]) & 0x1f)) != 0)
            {
                chenable[0] = (byte)((((r7 & 0x01) != 0) && (speriod[0] <= (uint)(1 << toneshift))) ? 15 : 0);
                chenable[1] = (byte)((((r7 & 0x02) != 0) && (speriod[1] <= (uint)(1 << toneshift))) ? 15 : 0);
                chenable[2] = (byte)((((r7 & 0x04) != 0) && (speriod[2] <= (uint)(1 << toneshift))) ? 15 : 0);
                nenable[0] = (byte)(((r7 >> 3) & 1) != 0 ? 1 : 0);
                nenable[1] = (byte)(((r7 >> 4) & 1) != 0 ? 1 : 0);
                nenable[2] = (byte)(((r7 >> 5) & 1) != 0 ? 1 : 0);
                p[0] = ((mask & 1) != 0 && (reg[8] & 0x10) != 0) ? (uint?)null : 0;
                p[1] = ((mask & 2) != 0 && (reg[9] & 0x10) != 0) ? (uint?)null : 1;
                p[2] = ((mask & 4) != 0 && (reg[10] & 0x10) != 0) ? (uint?)null : 2;

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
                                int x, n;

                                for (int k = 0; k < 3; k++)
                                {
                                    if (duty[k] < 8)
                                    {
                                        n = ((int)(scount[k] >> (toneshift + oversampling - 3)) & chenable[k]);
                                        //矩形波
                                        x = n > duty[k] ? 0 : -1;
                                        sample = (int)((olevel[k] + x) ^ x);
                                    }
                                    else if (duty[k] == 8)
                                    {
                                        n = ((int)(scount[k] >> (toneshift + oversampling - 3)) & chenable[k]);
                                        //三角波
                                        x = n < 8 ? (n - 4) : (15 - 4 - n);
                                        sample = (int)((olevel[k] * x) >> 1);
                                    }
                                    else if (duty[k] == 9)
                                    {
                                        n = ((int)(scount[k] >> (toneshift + oversampling - 3)) & chenable[k]);
                                        //のこぎり波
                                        x = n - 8;
                                        sample = (int)((olevel[k] * x) >> 2);
                                    }
                                    else 
                                    {
                                        //ユーザー定義
                                        uint pos = (scount[k] >> (toneshift + oversampling - 3 - 2)) & 63;
                                        n = ((int)user[duty[k] - 10][pos] & chenable[k]);
                                        x = n - 8;
                                        sample = (int)(((int)olevel[k] * x) >> 2);
                                        //if (k == 0) Console.WriteLine("{0} {1} {2}  ", pos, n, sample);
                                    }

                                    int L = (panpot[k] & 2) != 0 ? sample : 0;
                                    int R = (panpot[k] & 1) != 0 ? sample : 0;
                                    distortion.Mix(efcStartCh + k, ref L, ref R);
                                    chorus.Mix(efcStartCh + k, ref L, ref R);
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
                            reverb.StoreData(0, revSampleL);
                            reverb.StoreData(1, revSampleR);
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
                                noise = (int)(noisetable[(ncount >> (int)((noiseshift + oversampling + 6)) & (noisetablesize - 1))]
                                    >> (int)(ncount >> (noiseshift + oversampling + 1)));
                                ncount += nperiod;

                                int x, n;

                                for (int k = 0; k < 3; k++)
                                {
                                    n = ((int)(scount[k] >> (toneshift + oversampling - 3)) & chenable[k]);
                                    if (duty[k] < 8)
                                    {
                                        //矩形波
                                        x = n > duty[k] ? 0 : -1;
                                        sample = (int)((olevel[k] + x) ^ x);
                                    }
                                    else if (duty[k] == 8)
                                    {
                                        //三角波
                                        x = n < 8 ? (n - 4) : (15 - 4 - n);
                                        sample = (int)((olevel[k] * x) >> 1);
                                    }
                                    else if (duty[k] == 9)
                                    {
                                        //のこぎり波
                                        x = n - 7;
                                        sample = (int)((olevel[k] * x) >> 2);
                                    }

                                    int L = (panpot[k] & 2) != 0 ? sample : 0;
                                    int R = (panpot[k] & 1) != 0 ? sample : 0;

                                    //ノイズ
                                    nv = ((int)(scount[k] >> (toneshift + oversampling)) & 0 | (nenable[k] & noise)) - 1;
                                    sample = (int)((olevel[k] + nv) ^ nv);
                                    L += (panpot[k] & 2) != 0 ? sample : 0;
                                    R += (panpot[k] & 1) != 0 ? sample : 0;

                                    distortion.Mix(efcStartCh + k, ref L, ref R);
                                    chorus.Mix(efcStartCh + k, ref L, ref R);
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
                        sample = 0;
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
                            noise = (int)(noisetable[(ncount >> (int)((noiseshift + oversampling + 6)) & (noisetablesize - 1))]
                                >> (int)(ncount >> (noiseshift + oversampling + 1)));
                            ncount += nperiod;

                            int x, n;

                            for (int k = 0; k < 3; k++)
                            {
                                n = ((int)(scount[k] >> (toneshift + oversampling - 3)) & chenable[k]);
                                uint lv = (p[k] == null ? env : olevel[k]);

                                if (duty[k] < 8)
                                {
                                    //矩形波
                                    x = n > duty[k] ? 0 : -1;
                                    sample = (int)((lv + x) ^ x);
                                }
                                else if (duty[k] == 8)
                                {
                                    //三角波
                                    x = n < 8 ? (n - 4) : (15 - 4 - n);
                                    sample = (int)((lv * x) >> 1);
                                }
                                else if (duty[k] == 9)
                                {
                                    //のこぎり波
                                    x = n - 7;
                                    sample = (int)((lv * x) >> 2);
                                }

                                int L = (panpot[k] & 2) != 0 ? sample : 0;
                                int R = (panpot[k] & 1) != 0 ? sample : 0;

                                //ノイズ
                                nv = ((int)(scount[k] >> (toneshift + oversampling)) & 0 | (nenable[k] & noise)) - 1;
                                sample = (int)((lv + nv) ^ nv);
                                L += (panpot[k] & 2) != 0 ? sample : 0;
                                R += (panpot[k] & 1) != 0 ? sample : 0;
                                distortion.Mix(efcStartCh + k, ref L, ref R);
                                chorus.Mix(efcStartCh + k, ref L, ref R);
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
                        reverb.StoreData(0, revSampleL);
                        reverb.StoreData(1, revSampleR);
                        ptrDest += 2;

                        visVolume = sampleL;

                    }
                }
            }
        }

    }
}
