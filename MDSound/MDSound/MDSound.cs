using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDSound
{
    public class MDSound
    {

        private const uint DefaultSamplingRate = 44100;
        private const uint DefaultSamplingBuffer = 512;

        private uint SamplingRate = DefaultSamplingRate;
        private uint SamplingBuffer = DefaultSamplingBuffer;
        private int[][] StreamBufs = null;

        private Chip[] insts = null;
        private Dictionary<enmInstrumentType, Instrument[]> dicInst = new Dictionary<enmInstrumentType, Instrument[]>();

        private int[][] buffer = null;
        private int[][] buff = new int[2][] { new int[1], new int[1] };

        private List<int[]> sn76489Mask = new List<int[]>(new int[][] { new int[] { 15, 15 } });// psgはmuteを基準にしているのでビットが逆です
        private List<int[]> ym2612Mask = new List<int[]>(new int[][] { new int[] { 0, 0 } });
        private List<int[]> ym2203Mask = new List<int[]>(new int[][] { new int[] { 0, 0 } });
        private List<uint[]> segapcmMask = new List<uint[]>(new uint[][] { new uint[] { 0, 0 } });
        private List<uint[]> okim6295Mask = new List<uint[]>(new uint[][] { new uint[] { 0, 0 } });
        private List<uint[]> c140Mask = new List<uint[]>(new uint[][] { new uint[] { 0, 0 } });
        private List<int[]> ay8910Mask = new List<int[]>(new int[][] { new int[] { 0, 0 } });
        private List<int[]> huc6280Mask = new List<int[]>(new int[][] { new int[] { 0, 0 } });
        private List<uint[]> nesMask = new List<uint[]>(new uint[][] { new uint[] { 0, 0 } });

        private int[][][] rf5c164Vol = new int[][][] {
            new int[8][] { new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2] }
            ,new int[8][] { new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2] }
        };

        private int[][] ym2612Key = new int[][] { new int[6], new int[6] };
        private int[][] ym2151Key = new int[][] { new int[8], new int[8] };
        private int[][] ym2203Key = new int[][] { new int[6], new int[6] };
        private int[][] ym2608Key = new int[][] { new int[11], new int[11] };
        private int[][] ym2609Key = new int[][] { new int[12 + 12 + 3 + 1], new int[28] };
        private int[][] ym2610Key = new int[][] { new int[11], new int[11] };

        private bool incFlag = false;
        private object lockobj = new object();
        private byte ResampleMode = 0;

        private const uint FIXPNT_BITS = 11;
        private const uint FIXPNT_FACT = (1 << (int)FIXPNT_BITS);
        private const uint FIXPNT_MASK = (FIXPNT_FACT - 1);

        private int[][] tempSample = new int[2][] { new int[1], new int[1] };

        public static int np_nes_apu_volume;
        public static int np_nes_dmc_volume;
        public static int np_nes_fds_volume;
        public static int np_nes_fme7_volume;
        public static int np_nes_mmc5_volume;
        public static int np_nes_n106_volume;
        public static int np_nes_vrc6_volume;
        public static int np_nes_vrc7_volume;

#if DEBUG
        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
#endif

        private uint getfriction(uint x) { return ((x) & FIXPNT_MASK); }
        private uint getnfriction(uint x) { return ((FIXPNT_FACT - (x)) & FIXPNT_MASK); }
        private uint fpi_floor(uint x) { return (uint)((x) & ~FIXPNT_MASK); }
        private uint fpi_ceil(uint x) { return (uint)((x + FIXPNT_MASK) & ~FIXPNT_MASK); }
        private uint fp2i_floor(uint x) { return ((x) / FIXPNT_FACT); }
        private uint fp2i_ceil(uint x) { return ((x + FIXPNT_MASK) / FIXPNT_FACT); }


        public enum enmInstrumentType : int
        {
            None = 0,
            YM2612,
            SN76489,
            RF5C164,
            PWM,
            C140,
            OKIM6258,
            OKIM6295,
            SEGAPCM,
            YM2151,
            YM2203,
            YM2608,
            YM2610,
            AY8910,
            YM2413,
            HuC6280,
            C352,
            K054539,
            YM2609,
            K051649,
            Nes,
            DMC,
            FDS,
            MMC5,
            N160,
            VRC6,
            VRC7,
            FME7,
            MultiPCM,
            YMF262,
            YMF271,
            YMF278B,
            YMZ280B,
            DMG,
            QSound,
            GA20,
            K053260,
            Y8950,
            RF5C68,
            YM2151mame,
            YM2151x68sound,
            YM3438,
            mpcmX68k,
            YM3812,
            YM3526,
            QSoundCtr,
            PPZ8,
            PPSDRV
        }

        public class Chip
        {
            public delegate void dlgUpdate(byte ChipID, int[][] Buffer, int Length);
            public delegate uint dlgStart(byte ChipID, uint SamplingRate, uint FMClockValue, params object[] Option);
            public delegate void dlgStop(byte ChipID);
            public delegate void dlgReset(byte ChipID);
            public delegate void dlgAdditionalUpdate(Chip sender, byte ChipID, int[][] Buffer, int Length);

            public Instrument Instrument = null;
            public dlgUpdate Update = null;
            public dlgStart Start = null;
            public dlgStop Stop = null;
            public dlgReset Reset = null;
            public dlgAdditionalUpdate AdditionalUpdate = null;

            public enmInstrumentType type = enmInstrumentType.None;
            public byte ID = 0;
            public uint SamplingRate = 0;
            public uint Clock = 0;
            public int Volume = 0;
            public int VisVolume = 0;

            public byte Resampler;
            public uint SmpP;
            public uint SmpLast;
            public uint SmpNext;
            public int[] LSmpl;
            public int[] NSmpl;

            public object[] Option = null;
        }

        public MDSound()
        {
            Init(DefaultSamplingRate, DefaultSamplingBuffer, null);
        }

        public MDSound(uint SamplingRate, uint SamplingBuffer, Chip[] insts)
        {
            Init(SamplingRate, SamplingBuffer, insts);
        }

        public void Init(uint SamplingRate, uint SamplingBuffer, Chip[] insts)
        {
            lock (lockobj)
            {
                this.SamplingRate = SamplingRate;
                this.SamplingBuffer = SamplingBuffer;
                this.insts = insts;

                buffer = new int[2][] { new int[1], new int[1] };
                StreamBufs = new int[2][] { new int[0x100], new int[0x100] };

                incFlag = false;

                if (insts == null) return;

                dicInst.Clear();
                foreach (Chip inst in insts)
                {
                    inst.SamplingRate = inst.Start(inst.ID, inst.SamplingRate, inst.Clock, inst.Option);
                    inst.Reset(inst.ID);

                    if (dicInst.ContainsKey(inst.type))
                    {
                        List<Instrument> lst = dicInst[inst.type].ToList();
                        lst.Add(inst.Instrument);
                        dicInst[inst.type] = lst.ToArray();
                    }
                    else
                    {
                        dicInst.Add(inst.type, new Instrument[] { inst.Instrument });
                    }

                    SetupResampler(inst);
                }

                sn76489Mask = new List<int[]>();
                if (dicInst.ContainsKey(enmInstrumentType.SN76489)) for (int i = 0; i < dicInst[enmInstrumentType.SN76489].Length; i++) sn76489Mask.Add(new int[] { 15, 15 });
                ym2203Mask = new List<int[]>();
                if (dicInst.ContainsKey(enmInstrumentType.YM2203)) for (int i = 0; i < dicInst[enmInstrumentType.YM2203].Length; i++) ym2203Mask.Add(new int[] { 0, 0 });
                ym2612Mask = new List<int[]>();
                if (dicInst.ContainsKey(enmInstrumentType.YM2612)) for (int i = 0; i < dicInst[enmInstrumentType.YM2612].Length; i++) ym2612Mask.Add(new int[] { 0, 0 });
                segapcmMask = new List<uint[]>();
                if (dicInst.ContainsKey(enmInstrumentType.SEGAPCM)) for (int i = 0; i < dicInst[enmInstrumentType.SEGAPCM].Length; i++) segapcmMask.Add(new uint[] { 0, 0 });
                c140Mask = new List<uint[]>();
                if (dicInst.ContainsKey(enmInstrumentType.C140)) for (int i = 0; i < dicInst[enmInstrumentType.C140].Length; i++) c140Mask.Add(new uint[] { 0, 0 });
                ay8910Mask = new List<int[]>();
                if (dicInst.ContainsKey(enmInstrumentType.AY8910)) for (int i = 0; i < dicInst[enmInstrumentType.AY8910].Length; i++) ay8910Mask.Add(new int[] { 0, 0 });
            }
        }

        public string GetDebugMsg()
        {
            return debugMsg;
        }

        private void SetupResampler(Chip chip)
        {
            if (chip.SamplingRate == 0)
            {
                chip.Resampler = 0xff;
                return;
            }

            if (chip.SamplingRate < SamplingRate)
            {
                chip.Resampler = 0x01;
            }
            else if (chip.SamplingRate == SamplingRate)
            {
                chip.Resampler = 0x02;
            }
            else if (chip.SamplingRate > SamplingRate)
            {
                chip.Resampler = 0x03;
            }
            if (chip.Resampler == 0x01 || chip.Resampler == 0x03)
            {
                if (ResampleMode == 0x02 || (ResampleMode == 0x01 && chip.Resampler == 0x03))
                    chip.Resampler = 0x00;
            }

            chip.SmpP = 0x00;
            chip.SmpLast = 0x00;
            chip.SmpNext = 0x00;
            chip.LSmpl = new int[2];
            chip.LSmpl[0] = 0x00;
            chip.LSmpl[1] = 0x00;
            chip.NSmpl = new int[2];
            if (chip.Resampler == 0x01)
            {
                // Pregenerate first Sample (the upsampler is always one too late)
                int[][] buf = new int[2][] { new int[1], new int[1] };
                chip.Update(chip.ID, buf, 1);
                chip.NSmpl[0] = buf[0x00][0x00];
                chip.NSmpl[1] = buf[0x01][0x00];
            }
            else
            {
                chip.NSmpl[0] = 0x00;
                chip.NSmpl[1] = 0x00;
            }

        }


        private int a, b, i;

        public int Update(short[] buf, int offset, int sampleCount, Action frame)
        {
            lock (lockobj)
            {
        
                for (i = 0; i < sampleCount && offset + i < buf.Length; i += 2)
                {

                    frame?.Invoke();

                    a = 0;
                    b = 0;

                    buffer[0][0] = 0;
                    buffer[1][0] = 0;
                    ResampleChipStream(insts, buffer, 1);
                    a += buffer[0][0];
                    b += buffer[1][0];

                    if (incFlag)
                    {
                        a += buf[offset + i + 0];
                        b += buf[offset + i + 1];
                    }

                    Clip(ref a, ref b);

                    buf[offset + i + 0] = (short)a;
                    buf[offset + i + 1] = (short)b;

                }

                return Math.Min(i, sampleCount);

            }
        }


        //public int Update(short[] buf, int offset, int sampleCount, Action frame)
        //{
        //    lock (lockobj)
        //    {
        //        int a, b;

        //        for (int i = 0; i < sampleCount; i += 2)
        //        {

        //            frame?.Invoke();

        //            a = 0;
        //            b = 0;

        //            buffer[0][0] = 0;
        //            buffer[1][0] = 0;
        //            ResampleChipStream(insts, buffer, 1);

        //            if (insts != null && insts.Length > 0)
        //            {
        //                for (int j = 0; j < insts.Length; j++)
        //                {
        //                    buff[0][0] = 0;
        //                    buff[1][0] = 0;

        //                    int mul = insts[j].Volume;
        //                    mul = (int)(16384.0 * Math.Pow(10.0, mul / 40.0));

        //                    insts[j].Update?.Invoke(insts[j].ID, buff, 1);

        //                    buffer[0][0] += (short)((Limit(buff[0][0], 0x7fff, -0x8000) * mul) >> 14);
        //                    buffer[1][0] += (short)((Limit(buff[1][0], 0x7fff, -0x8000) * mul) >> 14);
        //                }
        //            }

        //            a += buffer[0][0];
        //            b += buffer[1][0];

        //            if (incFlag)
        //            {
        //                a += buf[offset + i + 0];
        //                b += buf[offset + i + 1];
        //            }

        //            Clip(ref a, ref b);

        //            buf[offset + i + 0] = (short)a;
        //            buf[offset + i + 1] = (short)b;

        //        }

        //        return sampleCount;

        //    }
        //}

        private void Clip(ref int a, ref int b)
        {
            if ((uint)(a + 32767) > (uint)(32767 * 2))
            {
                if ((int)(a + 32767) >= (int)(32767 * 2))
                {
                    a = 32767;
                }
                else
                {
                    a = -32767;
                }
            }
            if ((uint)(b + 32767) > (uint)(32767 * 2))
            {
                if ((int)(b + 32767) >= (int)(32767 * 2))
                {
                    b = 32767;
                }
                else
                {
                    b = -32767;
                }
            }
        }

        public static int Limit(int v, int max, int min)
        {
            return v > max ? max : (v < min ? min : v);
        }

        int[][] StreamPnt = new int[0x02][] { new int[0x100], new int[0x100] };
        public static string debugMsg;

        private unsafe void ResampleChipStream(Chip[] insts, int[][] RetSample, uint Length)
        {
            if (insts == null || insts.Length < 1) return;
            if (Length > tempSample[0].Length)
            {
                tempSample = new int[2][] { new int[Length], new int[Length] };
            }

            Chip inst;
            int[] CurBufL;
            int[] CurBufR;
            //int[][] StreamPnt = new int[0x02][] { new int[0x100], new int[0x100] };
            uint InBase;
            uint InPos;
            uint InPosNext;
            uint OutPos;
            uint SmpFrc;  // Sample Friction
            uint InPre = 0;
            uint InNow;
            uint InPosL;
            long TempSmpL;
            long TempSmpR;
            int TempS32L;
            int TempS32R;
            int SmpCnt;   // must be signed, else I'm getting calculation errors
            int CurSmpl;
            ulong ChipSmpRate;

            //for (int i = 0; i < 0x100; i++)
            //{
            //    StreamBufs[0][i] = 0;
            //    StreamBufs[1][i] = 0;
            //}
            Array.Clear(StreamBufs[0], 0, 0x100);
            Array.Clear(StreamBufs[1], 0, 0x100);
            CurBufL = StreamBufs[0x00];
            CurBufR = StreamBufs[0x01];

            // This Do-While-Loop gets and resamples the chip output of one or more chips.
            // It's a loop to support the AY8910 paired with the YM2203/YM2608/YM2610.
            for (int i = 0; i < insts.Length; i++)
            {
                inst = insts[i];
                //double volume = inst.Volume/100.0;
                int mul = inst.Volume;
                if (inst.type == enmInstrumentType.Nes) mul = 0;
                mul = (int)(16384.0 * Math.Pow(10.0, mul / 40.0));

                //Console.WriteLine("{0} {1}", inst.type, inst.Resampler);
                //Console.WriteLine("{0}", inst.Resampler);
                switch (inst.Resampler)
                {
                    case 0x00:  // old, but very fast resampler
                        inst.SmpLast = inst.SmpNext;
                        inst.SmpP += Length;
                        inst.SmpNext = (uint)((ulong)inst.SmpP * inst.SamplingRate / SamplingRate);
                        if (inst.SmpLast >= inst.SmpNext)
                        {
                            tempSample[0][0] = (short)((Limit(inst.LSmpl[0], 0x7fff, -0x8000) * mul) >> 14);
                            tempSample[1][0] = (short)((Limit(inst.LSmpl[1], 0x7fff, -0x8000) * mul) >> 14);

                            //RetSample[0][0] += (int)(inst.LSmpl[0] * volume);
                            //RetSample[1][0] += (int)(inst.LSmpl[1] * volume);
                        }
                        else
                        {
                            SmpCnt = (int)(inst.SmpNext - inst.SmpLast);

                            //inst.Update(inst.ID, StreamBufs, SmpCnt);
                            for (int ind = 0; ind < SmpCnt; ind++)
                            {
                                buff[0][0] = 0;
                                buff[1][0] = 0;
                                inst.Update?.Invoke(inst.ID, buff, 1);

                                StreamBufs[0][ind] += (short)((Limit(buff[0][0], 0x7fff, -0x8000) * mul) >> 14);
                                StreamBufs[1][ind] += (short)((Limit(buff[1][0], 0x7fff, -0x8000) * mul) >> 14);

                                //StreamBufs[0][ind] += (int)(buff[0][0] * volume);
                                //StreamBufs[1][ind] += (int)(buff[1][0] * volume);
                            }

                            if (SmpCnt == 1)
                            {
                                tempSample[0][0] = (short)((Limit(CurBufL[0x00], 0x7fff, -0x8000) * mul) >> 14);
                                tempSample[1][0] = (short)((Limit(CurBufR[0x00], 0x7fff, -0x8000) * mul) >> 14);

                                //RetSample[0][0] += (int)(CurBufL[0x00] * volume);
                                //RetSample[1][0] += (int)(CurBufR[0x00] * volume);
                                inst.LSmpl[0] = CurBufL[0x00];
                                inst.LSmpl[1] = CurBufR[0x00];
                            }
                            else if (SmpCnt == 2)
                            {
                                tempSample[0][0] = (short)(((Limit((CurBufL[0x00] + CurBufL[0x01]), 0x7fff, -0x8000) * mul) >> 14) >> 1);
                                tempSample[1][0] = (short)(((Limit((CurBufR[0x00] + CurBufR[0x01]), 0x7fff, -0x8000) * mul) >> 14) >> 1);

                                //RetSample[0][0] += (int)((int)((CurBufL[0x00] + CurBufL[0x01]) * volume) >> 1);
                                //RetSample[1][0] += (int)((int)((CurBufR[0x00] + CurBufR[0x01]) * volume) >> 1);
                                inst.LSmpl[0] = CurBufL[0x01];
                                inst.LSmpl[1] = CurBufR[0x01];
                            }
                            else
                            {
                                TempS32L = CurBufL[0x00];
                                TempS32R = CurBufR[0x00];
                                for (CurSmpl = 0x01; CurSmpl < SmpCnt; CurSmpl++)
                                {
                                    TempS32L += CurBufL[CurSmpl];
                                    TempS32R += CurBufR[CurSmpl];
                                }
                                tempSample[0][0] = (short)(((Limit(TempS32L, 0x7fff, -0x8000) * mul) >> 14) / SmpCnt);
                                tempSample[1][0] = (short)(((Limit(TempS32R, 0x7fff, -0x8000) * mul) >> 14) / SmpCnt);

                                //RetSample[0][0] += (int)(TempS32L * volume / SmpCnt);
                                //RetSample[1][0] += (int)(TempS32R * volume / SmpCnt);
                                inst.LSmpl[0] = CurBufL[SmpCnt - 1];
                                inst.LSmpl[1] = CurBufR[SmpCnt - 1];
                            }
                        }
                        break;
                    case 0x01:  // Upsampling
                        ChipSmpRate = inst.SamplingRate;
                        InPosL = (uint)(FIXPNT_FACT * inst.SmpP * ChipSmpRate / SamplingRate);
                        InPre = (uint)fp2i_floor(InPosL);
                        InNow = (uint)fp2i_ceil(InPosL);

                        //if (inst.type == enmInstrumentType.YM2612)
                        //{
                        //    System.Console.WriteLine("InPosL={0} , InPre={1} , InNow={2} , inst.SmpNext={3}", InPosL, InPre, InNow, inst.SmpNext);
                        //}

                        CurBufL[0x00] = inst.LSmpl[0];
                        CurBufR[0x00] = inst.LSmpl[1];
                        CurBufL[0x01] = inst.NSmpl[0];
                        CurBufR[0x01] = inst.NSmpl[1];
                        for (int ind = 0; ind < (int)(InNow - inst.SmpNext); ind++)
                        {
                            StreamPnt[0x00][ind] = CurBufL[0x02 + ind];
                            StreamPnt[0x01][ind] = CurBufR[0x02 + ind];
                        }
                        //inst.Update(inst.ID, StreamPnt, (int)(InNow - inst.SmpNext));
                        for (int ind = 0; ind < (int)(InNow - inst.SmpNext); ind++)
                        {
                            buff[0][0] = 0;
                            buff[1][0] = 0;
                            inst.Update?.Invoke(inst.ID, buff, 1);

                            StreamPnt[0][ind] += (short)((Limit(buff[0][0], 0x7fff, -0x8000) * mul) >> 14);
                            StreamPnt[1][ind] += (short)((Limit(buff[1][0], 0x7fff, -0x8000) * mul) >> 14);
                            //StreamPnt[0][ind] += (int)(buff[0][0] * volume);
                            //StreamPnt[1][ind] += (int)(buff[1][0] * volume);
                        }
                        for (int ind = 0; ind < (int)(InNow - inst.SmpNext); ind++)
                        {
                            CurBufL[0x02 + ind] = StreamPnt[0x00][ind];
                            CurBufR[0x02 + ind] = StreamPnt[0x01][ind];
                        }

                        InBase = FIXPNT_FACT + (uint)(InPosL - (uint)inst.SmpNext * FIXPNT_FACT);
                        SmpCnt = (int)FIXPNT_FACT;
                        inst.SmpLast = InPre;
                        inst.SmpNext = InNow;
                        for (OutPos = 0x00; OutPos < Length; OutPos++)
                        {
                            InPos = InBase + (uint)(FIXPNT_FACT * OutPos * ChipSmpRate / SamplingRate);

                            InPre = fp2i_floor(InPos);
                            InNow = fp2i_ceil(InPos);
                            SmpFrc = getfriction(InPos);

                            // Linear interpolation
                            TempSmpL = ((long)CurBufL[InPre] * (FIXPNT_FACT - SmpFrc)) +
                                        ((long)CurBufL[InNow] * SmpFrc);
                            TempSmpR = ((long)CurBufR[InPre] * (FIXPNT_FACT - SmpFrc)) +
                                        ((long)CurBufR[InNow] * SmpFrc);
                            //RetSample[0][OutPos] += (int)(TempSmpL * volume / SmpCnt);
                            //RetSample[1][OutPos] += (int)(TempSmpR * volume / SmpCnt);
                            tempSample[0][OutPos] = (int)(TempSmpL / SmpCnt);
                            tempSample[1][OutPos] = (int)(TempSmpR / SmpCnt);
                        }
                        inst.LSmpl[0] = CurBufL[InPre];
                        inst.LSmpl[1] = CurBufR[InPre];
                        inst.NSmpl[0] = CurBufL[InNow];
                        inst.NSmpl[1] = CurBufR[InNow];
                        inst.SmpP += Length;
                        break;
                    case 0x02:  // Copying
                        inst.SmpNext = inst.SmpP * inst.SamplingRate / SamplingRate;
                        //inst.Update(inst.ID, StreamBufs, (int)Length);
                        for (int ind = 0; ind < Length; ind++)
                        {
                            buff[0][0] = 0;
                            buff[1][0] = 0;
                            inst.Update?.Invoke(inst.ID, buff, 1);

                            StreamBufs[0][ind] = (short)((Limit(buff[0][0], 0x7fff, -0x8000) * mul) >> 14);
                            StreamBufs[1][ind] = (short)((Limit(buff[1][0], 0x7fff, -0x8000) * mul) >> 14);

                            //StreamBufs[0][ind] = (int)(buff[0][0] * volume);
                            //StreamBufs[1][ind] = (int)(buff[1][0] * volume);
                        }
                        for (OutPos = 0x00; OutPos < Length; OutPos++)
                        {
                            tempSample[0][OutPos] = (int)(CurBufL[OutPos]);
                            tempSample[1][OutPos] = (int)(CurBufR[OutPos]);
                        }
                        inst.SmpP += Length;
                        inst.SmpLast = inst.SmpNext;
                        break;
                    case 0x03:  // Downsampling
                        ChipSmpRate = inst.SamplingRate;
                        InPosL = (uint)(FIXPNT_FACT * (inst.SmpP + Length) * ChipSmpRate / SamplingRate);
                        inst.SmpNext = (uint)fp2i_ceil(InPosL);

                        CurBufL[0x00] = inst.LSmpl[0];
                        CurBufR[0x00] = inst.LSmpl[1];

                        for (int ind = 0; ind < (int)(inst.SmpNext - inst.SmpLast); ind++)
                        {
                            StreamPnt[0x00][ind] = CurBufL[0x01 + ind];
                            StreamPnt[0x01][ind] = CurBufR[0x01 + ind];
                        }
                        //inst.Update(inst.ID, StreamPnt, (int)(inst.SmpNext - inst.SmpLast));
                        for (int ind = 0; ind < (int)(inst.SmpNext - inst.SmpLast); ind++)
                        {
                            buff[0][0] = 0;
                            buff[1][0] = 0;
                            inst.Update(inst.ID, buff, 1);

                            StreamPnt[0][ind] += (short)((Limit(buff[0][0], 0x7fff, -0x8000) * mul) >> 14);
                            StreamPnt[1][ind] += (short)((Limit(buff[1][0], 0x7fff, -0x8000) * mul) >> 14);
                            //StreamPnt[0][ind] += (int)(buff[0][0] * volume);
                            //StreamPnt[1][ind] += (int)(buff[1][0] * volume);
                        }
                        for (int ind = 0; ind < (int)(inst.SmpNext - inst.SmpLast); ind++)
                        {
                            CurBufL[0x01 + ind] = StreamPnt[0x00][ind];
                            CurBufR[0x01 + ind] = StreamPnt[0x01][ind];
                        }

                        InPosL = (uint)(FIXPNT_FACT * inst.SmpP * ChipSmpRate / SamplingRate);
                        // I'm adding 1.0 to avoid negative indexes
                        InBase = FIXPNT_FACT + (uint)(InPosL - (uint)inst.SmpLast * FIXPNT_FACT);
                        InPosNext = InBase;
                        for (OutPos = 0x00; OutPos < Length; OutPos++)
                        {
                            //InPos = InBase + (UINT32)(FIXPNT_FACT * OutPos * ChipSmpRate / SampleRate);
                            InPos = InPosNext;
                            InPosNext = InBase + (uint)(FIXPNT_FACT * (OutPos + 1) * ChipSmpRate / SamplingRate);

                            // first frictional Sample
                            SmpFrc = getnfriction(InPos);
                            if (SmpFrc != 0)
                            {
                                InPre = fp2i_floor(InPos);
                                TempSmpL = (long)CurBufL[InPre] * SmpFrc;
                                TempSmpR = (long)CurBufR[InPre] * SmpFrc;
                            }
                            else
                            {
                                TempSmpL = TempSmpR = 0x00;
                            }
                            SmpCnt = (int)SmpFrc;

                            // last frictional Sample
                            SmpFrc = getfriction(InPosNext);
                            InPre = fp2i_floor(InPosNext);
                            if (SmpFrc != 0)
                            {
                                TempSmpL += (long)CurBufL[InPre] * SmpFrc;
                                TempSmpR += (long)CurBufR[InPre] * SmpFrc;
                                SmpCnt += (int)SmpFrc;
                            }

                            // whole Samples in between
                            //InPre = fp2i_floor(InPosNext);
                            InNow = fp2i_ceil(InPos);
                            SmpCnt += (int)((InPre - InNow) * FIXPNT_FACT);    // this is faster
                            while (InNow < InPre)
                            {
                                TempSmpL += (long)CurBufL[InNow] * FIXPNT_FACT;
                                TempSmpR += (long)CurBufR[InNow] * FIXPNT_FACT;
                                //SmpCnt ++;
                                InNow++;
                            }

                            //RetSample[0][OutPos] += (int)(TempSmpL * volume / SmpCnt);
                            //RetSample[1][OutPos] += (int)(TempSmpR * volume / SmpCnt);
                            tempSample[0][OutPos] = (int)(TempSmpL / SmpCnt);
                            tempSample[1][OutPos] = (int)(TempSmpR / SmpCnt);
                        }

                        inst.LSmpl[0] = CurBufL[InPre];
                        inst.LSmpl[1] = CurBufR[InPre];
                        inst.SmpP += Length;
                        inst.SmpLast = inst.SmpNext;
                        break;
                    default:
                        inst.SmpP += SamplingRate;
                        break;  // do absolutely nothing
                }

                if (inst.SmpLast >= inst.SamplingRate)
                {
                    inst.SmpLast -= inst.SamplingRate;
                    inst.SmpNext -= inst.SamplingRate;
                    inst.SmpP -= SamplingRate;
                }

                inst.AdditionalUpdate?.Invoke(inst, inst.ID, tempSample, (int)Length);
                for (int j = 0; j < Length; j++)
                {
                    RetSample[0][j] += tempSample[0][j];
                    RetSample[1][j] += tempSample[1][j];
                }

            }

            return;
        }



        #region AY8910

        public void WriteAY8910(byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.AY8910)) return;

                ((ay8910)(dicInst[enmInstrumentType.AY8910][0])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteAY8910(int ChipIndex, byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.AY8910)) return;

                ((ay8910)(dicInst[enmInstrumentType.AY8910][ChipIndex])).Write(ChipID, 0, Adr, Data);
            }
        }


        public void setVolumeAY8910(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.AY8910)) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.AY8910) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void setAY8910Mask(int chipID, int ch)
        {
            lock (lockobj)
            {
                ay8910Mask[0][chipID] |= ch;
                if (!dicInst.ContainsKey(enmInstrumentType.AY8910)) return;
                ((ay8910)(dicInst[enmInstrumentType.AY8910][0])).AY8910_SetMute((byte)chipID, ay8910Mask[0][chipID]);
            }
        }

        public void setAY8910Mask(int ChipIndex, int chipID, int ch)
        {
            lock (lockobj)
            {
                ay8910Mask[ChipIndex][chipID] |= ch;
                if (!dicInst.ContainsKey(enmInstrumentType.AY8910)) return;
                ((ay8910)(dicInst[enmInstrumentType.AY8910][ChipIndex])).AY8910_SetMute((byte)chipID, ay8910Mask[ChipIndex][chipID]);
            }
        }

        public void resetAY8910Mask(int chipID, int ch)
        {
            lock (lockobj)
            {
                ay8910Mask[0][chipID] &= ~ch;
                if (!dicInst.ContainsKey(enmInstrumentType.AY8910)) return;
                ((ay8910)(dicInst[enmInstrumentType.AY8910][0])).AY8910_SetMute((byte)chipID, ay8910Mask[0][chipID]);
            }
        }

        public void resetAY8910Mask(int ChipIndex, int chipID, int ch)
        {
            lock (lockobj)
            {
                ay8910Mask[ChipIndex][chipID] &= ~ch;
                if (!dicInst.ContainsKey(enmInstrumentType.AY8910)) return;
                ((ay8910)(dicInst[enmInstrumentType.AY8910][ChipIndex])).AY8910_SetMute((byte)chipID, ay8910Mask[ChipIndex][chipID]);
            }
        }

        public int[][][] getAY8910VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.AY8910)) return null;
            return ((ay8910)dicInst[enmInstrumentType.AY8910][0]).visVolume;
        }

        public int[][][] getAY8910VisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.AY8910)) return null;
            return ((ay8910)dicInst[enmInstrumentType.AY8910][ChipIndex]).visVolume;
        }

        #endregion


        #region SN76489

        public void WriteSN76489(byte ChipID, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.SN76489)) return;

                dicInst[enmInstrumentType.SN76489][0].Write(ChipID, 0, 0, Data);
            }
        }

        public void WriteSN76489(int ChipIndex,byte ChipID, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.SN76489)) return;

                dicInst[enmInstrumentType.SN76489][ChipIndex].Write(ChipID, 0, 0, Data);
            }
        }

        public void WriteSN76489GGPanning(byte ChipID, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.SN76489)) return;

                ((sn76489)(dicInst[enmInstrumentType.SN76489][0])).SN76489_GGStereoWrite(ChipID, Data);
            }
        }

        public void WriteSN76489GGPanning(int ChipIndex, byte ChipID, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.SN76489)) return;

                ((sn76489)(dicInst[enmInstrumentType.SN76489][ChipIndex])).SN76489_GGStereoWrite(ChipID, Data);
            }
        }

        #endregion


        #region YM2612

        public void WriteYM2612(byte ChipID, byte Port, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2612)) return;

                dicInst[enmInstrumentType.YM2612][0].Write(ChipID, 0, (byte)(0 + (Port & 1) * 2), Adr);
                dicInst[enmInstrumentType.YM2612][0].Write(ChipID, 0, (byte)(1 + (Port & 1) * 2), Data);
            }
        }

        public void WriteYM2612(int ChipIndex, byte ChipID, byte Port, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2612)) return;

                dicInst[enmInstrumentType.YM2612][ChipIndex].Write(ChipID, 0, (byte)(0 + (Port & 1) * 2), Adr);
                dicInst[enmInstrumentType.YM2612][ChipIndex].Write(ChipID, 0, (byte)(1 + (Port & 1) * 2), Data);
            }
        }

        #endregion


        #region YM3438

        public void WriteYM3438(byte ChipID, byte Port, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM3438)) return;

                dicInst[enmInstrumentType.YM3438][0].Write(ChipID, 0, (byte)(0 + (Port & 1) * 2), Adr);
                dicInst[enmInstrumentType.YM3438][0].Write(ChipID, 0, (byte)(1 + (Port & 1) * 2), Data);
            }
        }

        public void WriteYM3438(int ChipIndex,byte ChipID, byte Port, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM3438)) return;

                dicInst[enmInstrumentType.YM3438][ChipIndex].Write(ChipID, 0, (byte)(0 + (Port & 1) * 2), Adr);
                dicInst[enmInstrumentType.YM3438][ChipIndex].Write(ChipID, 0, (byte)(1 + (Port & 1) * 2), Data);
            }
        }

        #endregion


        #region PWM

        public void WritePWM(byte ChipID, byte Adr, uint Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.PWM)) return;

                dicInst[enmInstrumentType.PWM][0].Write(ChipID, 0, Adr, (int)Data);
                // (byte)((adr & 0xf0)>>4),(uint)((adr & 0xf)*0x100+data));
            }
        }

        public void WritePWM(int ChipIndex,byte ChipID, byte Adr, uint Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.PWM)) return;

                dicInst[enmInstrumentType.PWM][ChipIndex].Write(ChipID, 0, Adr, (int)Data);
            }
        }

        #endregion


        #region RF5C164

        public void WriteRF5C164(byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.RF5C164)) return;

                dicInst[enmInstrumentType.RF5C164][0].Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteRF5C164(int ChipIndex,byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.RF5C164)) return;

                dicInst[enmInstrumentType.RF5C164][ChipIndex].Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteRF5C164PCMData(byte ChipID, uint RAMStartAdr, uint RAMDataLength, byte[] SrcData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.RF5C164)) return;

                ((scd_pcm)(dicInst[enmInstrumentType.RF5C164][0])).rf5c164_write_ram2(ChipID, RAMStartAdr, RAMDataLength, SrcData, SrcStartAdr);
            }
        }

        public void WriteRF5C164PCMData(int ChipIndex,byte ChipID, uint RAMStartAdr, uint RAMDataLength, byte[] SrcData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.RF5C164)) return;

                ((scd_pcm)(dicInst[enmInstrumentType.RF5C164][ChipIndex])).rf5c164_write_ram2(ChipID, RAMStartAdr, RAMDataLength, SrcData, SrcStartAdr);
            }
        }

        public void WriteRF5C164MemW(byte ChipID, uint Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.RF5C164)) return;

                ((scd_pcm)(dicInst[enmInstrumentType.RF5C164][0])).rf5c164_mem_w(ChipID, Adr, Data);
            }
        }

        public void WriteRF5C164MemW(int ChipIndex,byte ChipID, uint Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.RF5C164)) return;

                ((scd_pcm)(dicInst[enmInstrumentType.RF5C164][ChipIndex])).rf5c164_mem_w(ChipID, Adr, Data);
            }
        }

        #endregion


        #region RF5C68

        public void WriteRF5C68(byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.RF5C68)) return;

                dicInst[enmInstrumentType.RF5C68][0].Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteRF5C68(int ChipIndex, byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.RF5C68)) return;

                dicInst[enmInstrumentType.RF5C68][ChipIndex].Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteRF5C68PCMData(byte ChipID, uint RAMStartAdr, uint RAMDataLength, byte[] SrcData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.RF5C68)) return;

                ((rf5c68)(dicInst[enmInstrumentType.RF5C68][0])).rf5c68_write_ram2(ChipID, (int)RAMStartAdr, (int)RAMDataLength, SrcData, SrcStartAdr);
            }
        }

        public void WriteRF5C68PCMData(int ChipIndex, byte ChipID, uint RAMStartAdr, uint RAMDataLength, byte[] SrcData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.RF5C68)) return;

                ((rf5c68)(dicInst[enmInstrumentType.RF5C68][ChipIndex])).rf5c68_write_ram2(ChipID, (int)RAMStartAdr, (int)RAMDataLength, SrcData, SrcStartAdr);
            }
        }

        public void WriteRF5C68MemW(byte ChipID, uint Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.RF5C68)) return;

                ((rf5c68)(dicInst[enmInstrumentType.RF5C68][0])).rf5c68_mem_w(ChipID, (int)Adr, Data);
            }
        }

        public void WriteRF5C68MemW(int ChipIndex, byte ChipID, uint Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.RF5C68)) return;

                ((rf5c68)(dicInst[enmInstrumentType.RF5C68][ChipIndex])).rf5c68_mem_w(ChipID, (int)Adr, Data);
            }
        }

        #endregion


        #region C140

        public void WriteC140(byte ChipID, uint Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.C140)) return;

                dicInst[enmInstrumentType.C140][0].Write(ChipID, 0, (int)Adr, Data);
            }
        }

        public void WriteC140(int ChipIndex, byte ChipID, uint Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.C140)) return;

                dicInst[enmInstrumentType.C140][ChipIndex].Write(ChipID, 0, (int)Adr, Data);
            }
        }

        public void WriteC140PCMData(byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.C140)) return;

                ((c140)(dicInst[enmInstrumentType.C140][0])).c140_write_rom2(ChipID, ROMSize, DataStart, DataLength, ROMData, SrcStartAdr);
            }
        }

        public void WriteC140PCMData(int ChipIndex, byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.C140)) return;

                ((c140)(dicInst[enmInstrumentType.C140][ChipIndex])).c140_write_rom2(ChipID, ROMSize, DataStart, DataLength, ROMData, SrcStartAdr);
            }
        }

        #endregion


        #region YM3812

        public void WriteYM3812(int ChipID, int rAdr, int rDat)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM3812)) return;

                ((ym3812)(dicInst[enmInstrumentType.YM3812][0])).Write((byte)ChipID, 0, rAdr, rDat);
            }
        }

        public void WriteYM3812(int ChipIndex,int ChipID, int rAdr, int rDat)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM3812)) return;

                ((ym3812)(dicInst[enmInstrumentType.YM3812][ChipIndex])).Write((byte)ChipID, 0, rAdr, rDat);
            }
        }

        #endregion


        #region C352

        public void WriteC352(byte ChipID, uint Adr, uint Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.C352)) return;

                dicInst[enmInstrumentType.C352][0].Write(ChipID, 0, (int)Adr, (ushort)Data);
            }
        }

        public void WriteC352(int ChipIndex,byte ChipID, uint Adr, uint Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.C352)) return;

                dicInst[enmInstrumentType.C352][ChipIndex].Write(ChipID, 0, (int)Adr, (ushort)Data);
            }
        }

        public void WriteC352PCMData(byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.C352)) return;

                ((c352)(dicInst[enmInstrumentType.C352][0])).c352_write_rom2(ChipID, ROMSize, (int)DataStart, (int)DataLength, ROMData, SrcStartAdr);
            }
        }

        public void WriteC352PCMData(int ChipIndex,byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.C352)) return;

                ((c352)(dicInst[enmInstrumentType.C352][ChipIndex])).c352_write_rom2(ChipID, ROMSize, (int)DataStart, (int)DataLength, ROMData, SrcStartAdr);
            }
        }

        #endregion


        #region YMF271

        public void WriteYMF271PCMData(byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YMF271)) return;

                ((ymf271)(dicInst[enmInstrumentType.YMF271][0])).ymf271_write_rom(ChipID, ROMSize, DataStart, DataLength, ROMData, (int)SrcStartAdr);
            }
        }

        public void WriteYMF271PCMData(int ChipIndex, byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YMF271)) return;

                ((ymf271)(dicInst[enmInstrumentType.YMF271][ChipIndex])).ymf271_write_rom(ChipID, ROMSize, DataStart, DataLength, ROMData, (int)SrcStartAdr);
            }
        }

        #endregion


        #region YMF278B

        public void WriteYMF278BPCMData(byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YMF278B)) return;

                ((ymf278b)(dicInst[enmInstrumentType.YMF278B][0])).ymf278b_write_rom(ChipID, (int)ROMSize, (int)DataStart, (int)DataLength, ROMData, (int)SrcStartAdr);
            }
        }

        public void WriteYMF278BPCMData(int ChipIndex, byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YMF278B)) return;

                ((ymf278b)(dicInst[enmInstrumentType.YMF278B][ChipIndex])).ymf278b_write_rom(ChipID, (int)ROMSize, (int)DataStart, (int)DataLength, ROMData, (int)SrcStartAdr);
            }
        }

        #endregion


        #region YMZ280B

        public void WriteYMZ280BPCMData(byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YMZ280B)) return;

                ((ymz280b)(dicInst[enmInstrumentType.YMZ280B][0])).ymz280b_write_rom(ChipID, (int)ROMSize, (int)DataStart, (int)DataLength, ROMData, (int)SrcStartAdr);
            }
        }

        public void WriteYMZ280BPCMData(int ChipIndex, byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YMZ280B)) return;

                ((ymz280b)(dicInst[enmInstrumentType.YMZ280B][ChipIndex])).ymz280b_write_rom(ChipID, (int)ROMSize, (int)DataStart, (int)DataLength, ROMData, (int)SrcStartAdr);
            }
        }

        #endregion


        #region Y8950

        public void WriteY8950PCMData(byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.Y8950)) return;

                ((y8950)(dicInst[enmInstrumentType.Y8950][0])).y8950_write_data_pcmrom(ChipID, (int)ROMSize, (int)DataStart, (int)DataLength, ROMData, (int)SrcStartAdr);
            }
        }

        public void WriteY8950PCMData(int ChipIndex, byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.Y8950)) return;

                ((y8950)(dicInst[enmInstrumentType.Y8950][ChipIndex])).y8950_write_data_pcmrom(ChipID, (int)ROMSize, (int)DataStart, (int)DataLength, ROMData, (int)SrcStartAdr);
            }
        }

        #endregion


        #region OKIM6258

        public void WriteOKIM6258(byte ChipID, byte Port, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.OKIM6258)) return;

                ((okim6258)(dicInst[enmInstrumentType.OKIM6258][0])).Write(ChipID, 0, Port, Data);
            }
        }

        public void WriteOKIM6258(int ChipIndex,byte ChipID, byte Port, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.OKIM6258)) return;

                ((okim6258)(dicInst[enmInstrumentType.OKIM6258][ChipIndex])).Write(ChipID, 0, Port, Data);
            }
        }

        #endregion


        #region OKIM6295

        public void WriteOKIM6295(byte ChipID, byte Port, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.OKIM6295)) return;

                ((okim6295)(dicInst[enmInstrumentType.OKIM6295][0])).Write(ChipID, 0, Port, Data);
            }
        }

        public void WriteOKIM6295(int ChipIndex, byte ChipID, byte Port, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.OKIM6295)) return;

                ((okim6295)(dicInst[enmInstrumentType.OKIM6295][ChipIndex])).Write(ChipID, 0, Port, Data);
            }
        }

        public void WriteOKIM6295PCMData(byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.OKIM6295)) return;

                ((okim6295)(dicInst[enmInstrumentType.OKIM6295][0])).okim6295_write_rom2(ChipID, (int)ROMSize, (int)DataStart, (int)DataLength, ROMData, SrcStartAdr);
            }
        }

        public void WriteOKIM6295PCMData(int ChipIndex,byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.OKIM6295)) return;

                ((okim6295)(dicInst[enmInstrumentType.OKIM6295][ChipIndex])).okim6295_write_rom2(ChipID, (int)ROMSize, (int)DataStart, (int)DataLength, ROMData, SrcStartAdr);
            }
        }

        #endregion


        #region SEGAPCM

        public void WriteSEGAPCM(byte ChipID, int Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.SEGAPCM)) return;

                ((segapcm)(dicInst[enmInstrumentType.SEGAPCM][0])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteSEGAPCM(int ChipIndex, byte ChipID, int Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.SEGAPCM)) return;

                ((segapcm)(dicInst[enmInstrumentType.SEGAPCM][ChipIndex])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteSEGAPCMPCMData(byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.SEGAPCM)) return;

                ((segapcm)(dicInst[enmInstrumentType.SEGAPCM][0])).sega_pcm_write_rom2(ChipID, (int)ROMSize, (int)DataStart, (int)DataLength, ROMData, SrcStartAdr);
            }
        }

        public void WriteSEGAPCMPCMData(int ChipIndex,byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.SEGAPCM)) return;

                ((segapcm)(dicInst[enmInstrumentType.SEGAPCM][ChipIndex])).sega_pcm_write_rom2(ChipID, (int)ROMSize, (int)DataStart, (int)DataLength, ROMData, SrcStartAdr);
            }
        }

        #endregion


        #region YM2151

        public void WriteYM2151(byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2151)) return;

                ((dicInst[enmInstrumentType.YM2151][0])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteYM2151(int ChipIndex,byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2151)) return;

                ((dicInst[enmInstrumentType.YM2151][ChipIndex])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteYM2151mame(byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2151mame)) return;

                ((dicInst[enmInstrumentType.YM2151mame][0])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteYM2151mame(int ChipIndex,byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2151mame)) return;

                ((dicInst[enmInstrumentType.YM2151mame][ChipIndex])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteYM2151x68sound(byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2151x68sound)) return;

                ((dicInst[enmInstrumentType.YM2151x68sound][0])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteYM2151x68sound(int ChipIndex,byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2151x68sound)) return;

                ((dicInst[enmInstrumentType.YM2151x68sound][ChipIndex])).Write(ChipID, 0, Adr, Data);
            }
        }

        #endregion


        #region YM2203

        public void WriteYM2203(byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2203)) return;

                ((ym2203)(dicInst[enmInstrumentType.YM2203][0])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteYM2203(int ChipIndex,byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2203)) return;

                ((ym2203)(dicInst[enmInstrumentType.YM2203][ChipIndex])).Write(ChipID, 0, Adr, Data);
            }
        }

        #endregion


        #region YM2608

        public void WriteYM2608(byte ChipID, byte Port, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2608)) return;

                ((ym2608)(dicInst[enmInstrumentType.YM2608][0])).Write(ChipID, 0, (Port * 0x100 + Adr), Data);
            }
        }

        public void WriteYM2608(int ChipIndex,byte ChipID, byte Port, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2608)) return;

                ((ym2608)(dicInst[enmInstrumentType.YM2608][ChipIndex])).Write(ChipID, 0, (Port * 0x100 + Adr), Data);
            }
        }

        public byte[] GetADPCMBufferYM2608(byte ChipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2608)) return null;

                return ((ym2608)(dicInst[enmInstrumentType.YM2608][0])).GetADPCMBuffer(ChipID);
            }
        }

        public byte[] GetADPCMBufferYM2608(int ChipIndex,byte ChipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2608)) return null;

                return ((ym2608)(dicInst[enmInstrumentType.YM2608][ChipIndex])).GetADPCMBuffer(ChipID);
            }
        }

        public uint ReadStatusExYM2608(byte ChipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2608)) throw new Exception();

                return ((ym2608)(dicInst[enmInstrumentType.YM2608][0])).ReadStatusEx(ChipID);
            }
        }

        public uint ReadStatusExYM2608(int ChipIndex,byte ChipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2608)) throw new Exception();

                return ((ym2608)(dicInst[enmInstrumentType.YM2608][ChipIndex])).ReadStatusEx(ChipID);
            }
        }

        #endregion


        #region YM2609

        public void WriteYM2609(byte ChipID, byte Port, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2609)) return;

                ((ym2609)(dicInst[enmInstrumentType.YM2609][0])).Write(ChipID, 0, (Port * 0x100 + Adr), Data);
            }
        }

        public void WriteYM2609(int ChipIndex, byte ChipID, byte Port, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2609)) return;

                ((ym2609)(dicInst[enmInstrumentType.YM2609][ChipIndex])).Write(ChipID, 0, (Port * 0x100 + Adr), Data);
            }
        }

        public void WriteYM2609_SetAdpcmA(byte ChipID, byte[] Buf)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2609)) return;

                ((ym2609)(dicInst[enmInstrumentType.YM2609][0])).SetAdpcmA(ChipID, Buf, Buf.Length);
            }
        }

        public void WriteYM2609_SetAdpcmA(int ChipIndex, byte ChipID, byte[] Buf)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2609)) return;

                ((ym2609)(dicInst[enmInstrumentType.YM2609][ChipIndex])).SetAdpcmA(ChipID, Buf, Buf.Length);
            }
        }

        #endregion


        #region YM2610

        public void WriteYM2610(byte ChipID, byte Port, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2610)) return;

                ((ym2610)(dicInst[enmInstrumentType.YM2610][0])).Write(ChipID, 0, (Port * 0x100 + Adr), Data);
            }
        }

        public void WriteYM2610(int ChipIndex, byte ChipID, byte Port, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2610)) return;

                ((ym2610)(dicInst[enmInstrumentType.YM2610][ChipIndex])).Write(ChipID, 0, (Port * 0x100 + Adr), Data);
            }
        }

        public void WriteYM2610_SetAdpcmA(byte ChipID, byte[] Buf)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2610)) return;

                ((ym2610)(dicInst[enmInstrumentType.YM2610][0])).YM2610_setAdpcmA(ChipID, Buf, Buf.Length);
            }
        }

        public void WriteYM2610_SetAdpcmA(int ChipIndex, byte ChipID, byte[] Buf)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2610)) return;

                ((ym2610)(dicInst[enmInstrumentType.YM2610][ChipIndex])).YM2610_setAdpcmA(ChipID, Buf, Buf.Length);
            }
        }

        public void WriteYM2610_SetAdpcmB(byte ChipID, byte[] Buf)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2610)) return;

                ((ym2610)(dicInst[enmInstrumentType.YM2610][0])).YM2610_setAdpcmB(ChipID, Buf, Buf.Length);
            }
        }

        public void WriteYM2610_SetAdpcmB(int ChipIndex, byte ChipID, byte[] Buf)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2610)) return;

                ((ym2610)(dicInst[enmInstrumentType.YM2610][ChipIndex])).YM2610_setAdpcmB(ChipID, Buf, Buf.Length);
            }
        }

        #endregion


        #region YMF262

        public void WriteYMF262(byte ChipID, byte Port, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YMF262)) return;

                ((ymf262)(dicInst[enmInstrumentType.YMF262][0])).Write(ChipID, 0, (Port * 0x100 + Adr), Data);
            }
        }

        public void WriteYMF262(int ChipIndex,byte ChipID, byte Port, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YMF262)) return;

                ((ymf262)(dicInst[enmInstrumentType.YMF262][ChipIndex])).Write(ChipID, 0, (Port * 0x100 + Adr), Data);
            }
        }

        #endregion


        #region YMF271

        public void WriteYMF271(byte ChipID, byte Port, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YMF271)) return;

                ((ymf271)(dicInst[enmInstrumentType.YMF271][0])).Write(ChipID, Port, Adr, Data);
            }
        }

        public void WriteYMF271(int ChipIndex,byte ChipID, byte Port, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YMF271)) return;

                ((ymf271)(dicInst[enmInstrumentType.YMF271][ChipIndex])).Write(ChipID, Port, Adr, Data);
            }
        }

        #endregion


        #region YMF278B

        public void WriteYMF278B(byte ChipID, byte Port, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YMF278B)) return;
                ((ymf278b)(dicInst[enmInstrumentType.YMF278B][0])).Write(ChipID, Port, Adr, Data);
            }
        }

        public void WriteYMF278B(int ChipIndex, byte ChipID, byte Port, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YMF278B)) return;
                ((ymf278b)(dicInst[enmInstrumentType.YMF278B][ChipIndex])).Write(ChipID, Port, Adr, Data);
            }
        }

        #endregion


        #region YM3526

        public void WriteYM3526(byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM3526)) return;

                ((ym3526)(dicInst[enmInstrumentType.YM3526][0])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteYM3526(int ChipIndex, byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM3526)) return;

                ((ym3526)(dicInst[enmInstrumentType.YM3526][ChipIndex])).Write(ChipID, 0, Adr, Data);
            }
        }

        #endregion


        #region Y8950

        public void WriteY8950(byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.Y8950)) return;

                ((y8950)(dicInst[enmInstrumentType.Y8950][0])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteY8950(int ChipIndex,byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.Y8950)) return;

                ((y8950)(dicInst[enmInstrumentType.Y8950][ChipIndex])).Write(ChipID, 0, Adr, Data);
            }
        }

        #endregion


        #region YMZ280B

        public void WriteYMZ280B(byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YMZ280B)) return;

                ((ymz280b)(dicInst[enmInstrumentType.YMZ280B][0])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteYMZ280B(int ChipIndex, byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YMZ280B)) return;

                ((ymz280b)(dicInst[enmInstrumentType.YMZ280B][ChipIndex])).Write(ChipID, 0, Adr, Data);
            }
        }

        #endregion


        #region HuC6280

        public void WriteHuC6280(byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.HuC6280)) return;

                ((Ootake_PSG)(dicInst[enmInstrumentType.HuC6280][0])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteHuC6280(int ChipIndex, byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.HuC6280)) return;

                ((Ootake_PSG)(dicInst[enmInstrumentType.HuC6280][ChipIndex])).Write(ChipID, 0, Adr, Data);
            }
        }

        #endregion


        #region GA20

        public void WriteGA20(byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.GA20)) return;

                ((iremga20)(dicInst[enmInstrumentType.GA20][0])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteGA20(int ChipIndex, byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.GA20)) return;

                ((iremga20)(dicInst[enmInstrumentType.GA20][ChipIndex])).Write(ChipID, 0, Adr, Data);
            }
        }

        #endregion


        #region YM2413

        public void WriteYM2413(byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2413)) return;

                ((ym2413)(dicInst[enmInstrumentType.YM2413][0])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteYM2413(int ChipIndex, byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2413)) return;

                ((ym2413)(dicInst[enmInstrumentType.YM2413][ChipIndex])).Write(ChipID, 0, Adr, Data);
            }
        }

        #endregion


        #region K051649

        public void WriteK051649(byte ChipID, int Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.K051649)) return;

                ((K051649)(dicInst[enmInstrumentType.K051649][0])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteK051649(int ChipIndex,byte ChipID, int Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.K051649)) return;

                ((K051649)(dicInst[enmInstrumentType.K051649][ChipIndex])).Write(ChipID, 0, Adr, Data);
            }
        }

        #endregion


        #region K053260

        public void WriteK053260(byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.K053260)) return;

                ((K053260)(dicInst[enmInstrumentType.K053260][0])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteK053260(int ChipIndex, byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.K053260)) return;

                ((K053260)(dicInst[enmInstrumentType.K053260][ChipIndex])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteK053260PCMData(byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.K053260)) return;

                ((K053260)(dicInst[enmInstrumentType.K053260][0])).k053260_write_rom(ChipID, (int)ROMSize, (int)DataStart, (int)DataLength, ROMData, (int)SrcStartAdr);
            }
        }

        public void WriteK053260PCMData(int ChipIndex, byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.K053260)) return;

                ((K053260)(dicInst[enmInstrumentType.K053260][ChipIndex])).k053260_write_rom(ChipID, (int)ROMSize, (int)DataStart, (int)DataLength, ROMData, (int)SrcStartAdr);
            }
        }

        #endregion


        #region K054539

        public void WriteK054539(byte ChipID, int Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.K054539)) return;

                ((K054539)(dicInst[enmInstrumentType.K054539][0])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteK054539(int ChipIndex,byte ChipID, int Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.K054539)) return;

                ((K054539)(dicInst[enmInstrumentType.K054539][ChipIndex])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteK054539PCMData(byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.K054539)) return;

                ((K054539)(dicInst[enmInstrumentType.K054539][0])).k054539_write_rom2(ChipID, (int)ROMSize, (int)DataStart, (int)DataLength, ROMData, (int)SrcStartAdr);
            }
        }

        public void WriteK054539PCMData(int ChipIndex, byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.K054539)) return;

                ((K054539)(dicInst[enmInstrumentType.K054539][ChipIndex])).k054539_write_rom2(ChipID, (int)ROMSize, (int)DataStart, (int)DataLength, ROMData, (int)SrcStartAdr);
            }
        }

        #endregion


        #region PPZ8

        public void WritePPZ8(byte ChipID, int port, int address, int data, byte[] addtionalData)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.PPZ8)) return;

                //if (port == 0x03)
                //{
                //    ((PPZ8)(dicInst[enmInstrumentType.PPZ8][0])).LoadPcm(ChipID, (byte)address, (byte)data, addtionalData);
                //}
                //else
                //{
                   ((PPZ8)(dicInst[enmInstrumentType.PPZ8][0])).Write(ChipID, port, address, data);
                //}
            }
        }

        public void WritePPZ8(int ChipIndex, byte ChipID, int port, int address, int data, byte[] addtionalData)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.PPZ8)) return;

                //if (port == 0x03)
                //{
                //    ((PPZ8)(dicInst[enmInstrumentType.PPZ8][0])).LoadPcm(ChipID, (byte)address, (byte)data, addtionalData);
                //}
                //else
                //{
                ((PPZ8)(dicInst[enmInstrumentType.PPZ8][ChipIndex])).Write(ChipID, port, address, data);
                //}
            }
        }

        public void WritePPZ8PCMData(byte ChipID, int address, int data, byte[] PCMData)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.PPZ8)) return;

                ((PPZ8)(dicInst[enmInstrumentType.PPZ8][0])).LoadPcm(ChipID, (byte)address, (byte)data, PCMData);
            }
        }

        public void WritePPZ8PCMData(int ChipIndex, byte ChipID, int address, int data, byte[] PCMData)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.PPZ8)) return;

                ((PPZ8)(dicInst[enmInstrumentType.PPZ8][ChipIndex])).LoadPcm(ChipID, (byte)address, (byte)data, PCMData);
            }
        }

        #endregion


        #region PPSDRV

        public void WritePPSDRV(byte ChipID, int port, int address, int data, byte[] addtionalData)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.PPSDRV)) return;

                ((PPSDRV)(dicInst[enmInstrumentType.PPSDRV][0])).Write(ChipID, port, address, data);
            }
        }

        public void WritePPSDRV(int ChipIndex, byte ChipID, int port, int address, int data, byte[] addtionalData)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.PPSDRV)) return;

                ((PPSDRV)(dicInst[enmInstrumentType.PPSDRV][ChipIndex])).Write(ChipID, port, address, data);
            }
        }

        public void WritePPSDRVPCMData(byte ChipID, byte[] PCMData)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.PPSDRV)) return;

                ((PPSDRV)(dicInst[enmInstrumentType.PPSDRV][0])).Load(ChipID, PCMData);
            }
        }

        public void WritePPSDRVPCMData(int ChipIndex, byte ChipID, byte[] PCMData)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.PPSDRV)) return;

                ((PPSDRV)(dicInst[enmInstrumentType.PPSDRV][ChipIndex])).Load(ChipID, PCMData);
            }
        }

        #endregion


        #region QSound

        public void WriteQSound(byte ChipID, Int32 adr, byte dat)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.QSound)) return;

                ((qsound)(dicInst[enmInstrumentType.QSound][0])).qsound_w(ChipID, adr, dat);
            }
        }

        public void WriteQSound(int ChipIndex, byte ChipID, Int32 adr, byte dat)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.QSound)) return;

                ((qsound)(dicInst[enmInstrumentType.QSound][ChipIndex])).qsound_w(ChipID, adr, dat);
            }
        }

        public void WriteQSoundPCMData(byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.QSound)) return;

                ((qsound)(dicInst[enmInstrumentType.QSound][0])).qsound_write_rom(ChipID, (int)ROMSize, (int)DataStart, (int)DataLength, ROMData, (int)SrcStartAdr);
            }
        }

        public void WriteQSoundPCMData(int ChipIndex, byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.QSound)) return;

                ((qsound)(dicInst[enmInstrumentType.QSound][ChipIndex])).qsound_write_rom(ChipID, (int)ROMSize, (int)DataStart, (int)DataLength, ROMData, (int)SrcStartAdr);
            }
        }

        #endregion


        #region QSoundCtr

        public void WriteQSoundCtr(byte ChipID, Int32 adr, byte dat)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.QSoundCtr)) return;

                //((qsound)(dicInst[enmInstrumentType.QSound][0])).qsound_w(ChipID, adr, dat);
                ((Qsound_ctr)(dicInst[enmInstrumentType.QSoundCtr][0])).qsound_w(ChipID, adr, dat);
            }
        }

        public void WriteQSoundCtr(int ChipIndex, byte ChipID, Int32 adr, byte dat)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.QSoundCtr)) return;

                //((qsound)(dicInst[enmInstrumentType.QSound][ChipIndex])).qsound_w(ChipID, adr, dat);
                ((Qsound_ctr)(dicInst[enmInstrumentType.QSoundCtr][ChipIndex])).qsound_w(ChipID, adr, dat);
            }
        }

        public void WriteQSoundCtrPCMData(byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.QSoundCtr)) return;

                //((qsound)(dicInst[enmInstrumentType.QSound][0])).qsound_write_rom(ChipID, (int)ROMSize, (int)DataStart, (int)DataLength, ROMData, (int)SrcStartAdr);
                ((Qsound_ctr)(dicInst[enmInstrumentType.QSoundCtr][0])).qsound_write_rom(ChipID, (int)ROMSize, (int)DataStart, (int)DataLength, ROMData, (int)SrcStartAdr);
            }
        }

        public void WriteQSoundCtrPCMData(int ChipIndex, byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.QSoundCtr)) return;

                //((qsound)(dicInst[enmInstrumentType.QSound][ChipIndex])).qsound_write_rom(ChipID, (int)ROMSize, (int)DataStart, (int)DataLength, ROMData, (int)SrcStartAdr);
                ((Qsound_ctr)(dicInst[enmInstrumentType.QSoundCtr][ChipIndex])).qsound_write_rom(ChipID, (int)ROMSize, (int)DataStart, (int)DataLength, ROMData, (int)SrcStartAdr);
            }
        }

        #endregion


        #region GA20

        public void WriteGA20PCMData(byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.GA20)) return;

                ((iremga20)(dicInst[enmInstrumentType.GA20][0])).iremga20_write_rom(ChipID, (int)ROMSize, (int)DataStart, (int)DataLength, ROMData, (int)SrcStartAdr);
            }
        }

        public void WriteGA20PCMData(int ChipIndex, byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.GA20)) return;

                ((iremga20)(dicInst[enmInstrumentType.GA20][ChipIndex])).iremga20_write_rom(ChipID, (int)ROMSize, (int)DataStart, (int)DataLength, ROMData, (int)SrcStartAdr);
            }
        }

        #endregion


        #region DMG

        public void WriteDMG(byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.DMG)) return;

                ((gb)(dicInst[enmInstrumentType.DMG][0])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteDMG(int ChipIndex,byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.DMG)) return;

                ((gb)(dicInst[enmInstrumentType.DMG][ChipIndex])).Write(ChipID, 0, Adr, Data);
            }
        }

        #endregion


        #region NES

        public void WriteNES(byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.Nes)) return;

                ((nes_intf)(dicInst[enmInstrumentType.Nes][0])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteNES(int ChipIndex,byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.Nes)) return;

                ((nes_intf)(dicInst[enmInstrumentType.Nes][ChipIndex])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteNESRam(byte ChipID, Int32 DataStart, Int32 DataLength, byte[] RAMData, Int32 RAMDataStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.Nes)) return;

                ((nes_intf)(dicInst[enmInstrumentType.Nes][0])).nes_write_ram(ChipID, DataStart, DataLength, RAMData, RAMDataStartAdr);
            }
        }

        public void WriteNESRam(int ChipIndex, byte ChipID, Int32 DataStart, Int32 DataLength, byte[] RAMData, Int32 RAMDataStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.Nes)) return;

                ((nes_intf)(dicInst[enmInstrumentType.Nes][ChipIndex])).nes_write_ram(ChipID, DataStart, DataLength, RAMData, RAMDataStartAdr);
            }
        }

        public byte[] ReadNES(byte ChipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.Nes)) return null;

                return ((nes_intf)(dicInst[enmInstrumentType.Nes][0])).nes_r(ChipID);
            }
        }

        public byte[] ReadNES(int ChipIndex, byte ChipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.Nes)) return null;

                return ((nes_intf)(dicInst[enmInstrumentType.Nes][ChipIndex])).nes_r(ChipID);
            }
        }

        #endregion


        #region MultiPCM

        public void WriteMultiPCM(byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.MultiPCM)) return;

                ((multipcm)(dicInst[enmInstrumentType.MultiPCM][0])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteMultiPCM(int ChipIndex,byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.MultiPCM)) return;

                ((multipcm)(dicInst[enmInstrumentType.MultiPCM][ChipIndex])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteMultiPCMSetBank(byte ChipID, byte Ch, int Adr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.MultiPCM)) return;

                ((multipcm)(dicInst[enmInstrumentType.MultiPCM][0])).multipcm_bank_write(ChipID, Ch, (UInt16)Adr);
            }
        }

        public void WriteMultiPCMSetBank(int ChipIndex,byte ChipID, byte Ch, int Adr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.MultiPCM)) return;

                ((multipcm)(dicInst[enmInstrumentType.MultiPCM][ChipIndex])).multipcm_bank_write(ChipID, Ch, (UInt16)Adr);
            }
        }

        public void WriteMultiPCMPCMData(byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.MultiPCM)) return;

                ((multipcm)(dicInst[enmInstrumentType.MultiPCM][0])).multipcm_write_rom2(ChipID, (int)ROMSize, (int)DataStart, (int)DataLength, ROMData, (int)SrcStartAdr);
            }
        }

        public void WriteMultiPCMPCMData(int ChipIndex, byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.MultiPCM)) return;

                ((multipcm)(dicInst[enmInstrumentType.MultiPCM][ChipIndex])).multipcm_write_rom2(ChipID, (int)ROMSize, (int)DataStart, (int)DataLength, ROMData, (int)SrcStartAdr);
            }
        }

        #endregion


        #region FDS

        public np.np_nes_fds.NES_FDS ReadFDS(byte ChipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.Nes)) return null;

                return ((nes_intf)(dicInst[enmInstrumentType.Nes][0])).nes_r_fds(ChipID);
            }
        }

        public np.np_nes_fds.NES_FDS ReadFDS(int ChipIndex, byte ChipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.Nes)) return null;

                return ((nes_intf)(dicInst[enmInstrumentType.Nes][ChipIndex])).nes_r_fds(ChipID);
            }
        }

        #endregion



        public void SetVolumeYM2151(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2151)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YM2151) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeYM2151mame(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2151mame)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YM2151mame) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeYM2151x68sound(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2151x68sound)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YM2151x68sound) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeYM2203(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2203)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YM2203) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeYM2203FM(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2203)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YM2203) continue;
                ((ym2203)c.Instrument).SetFMVolume(0, vol);
                ((ym2203)c.Instrument).SetFMVolume(1, vol);
            }
        }

        public void SetVolumeYM2203PSG(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2203)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YM2203) continue;
                ((ym2203)c.Instrument).SetPSGVolume(0, vol);
                ((ym2203)c.Instrument).SetPSGVolume(1, vol);
            }
        }

        public void SetVolumeYM2413(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2413)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YM2413) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void setVolumeHuC6280(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.HuC6280)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.HuC6280) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeYM2608(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2608)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YM2608) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeYM2608FM(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2608)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YM2608) continue;
                ((ym2608)c.Instrument).SetFMVolume(0, vol);
                ((ym2608)c.Instrument).SetFMVolume(1, vol);
            }
        }

        public void SetVolumeYM2608PSG(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2608)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YM2608) continue;
                ((ym2608)c.Instrument).SetPSGVolume(0, vol);
                ((ym2608)c.Instrument).SetPSGVolume(1, vol);
            }
        }

        public void SetVolumeYM2608Rhythm(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2608)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YM2608) continue;
                ((ym2608)c.Instrument).SetRhythmVolume(0, vol);
                ((ym2608)c.Instrument).SetRhythmVolume(1, vol);
            }
        }

        public void SetVolumeYM2608Adpcm(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2608)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YM2608) continue;
                ((ym2608)c.Instrument).SetAdpcmVolume(0, vol);
                ((ym2608)c.Instrument).SetAdpcmVolume(1, vol);
            }
        }

        public void SetVolumeYM2609FM(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2609)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YM2609) continue;
                ((ym2609)c.Instrument).SetFMVolume(0, vol);
                ((ym2609)c.Instrument).SetFMVolume(1, vol);
            }
        }

        public void SetVolumeYM2609PSG(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2609)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YM2609) continue;
                ((ym2609)c.Instrument).SetPSGVolume(0, vol);
                ((ym2609)c.Instrument).SetPSGVolume(1, vol);
            }
        }

        public void SetVolumeYM2609Rhythm(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2609)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YM2609) continue;
                ((ym2609)c.Instrument).SetRhythmVolume(0, vol);
                ((ym2609)c.Instrument).SetRhythmVolume(1, vol);
            }
        }

        public void SetVolumeYM2609Adpcm(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2609)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YM2609) continue;
                ((ym2609)c.Instrument).SetAdpcmVolume(0, vol);
                ((ym2609)c.Instrument).SetAdpcmVolume(1, vol);
            }
        }

        public void SetVolumeYM2610(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2610)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YM2610) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeYM2610FM(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2610)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YM2610) continue;
                ((ym2610)c.Instrument).SetFMVolume(0, vol);
                ((ym2610)c.Instrument).SetFMVolume(1, vol);
            }
        }

        public void SetVolumeYM2610PSG(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2610)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YM2610) continue;
                ((ym2610)c.Instrument).SetPSGVolume(0, vol);
                ((ym2610)c.Instrument).SetPSGVolume(1, vol);
            }
        }

        public void SetVolumeYM2610AdpcmA(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2610)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YM2610) continue;
                ((ym2610)c.Instrument).SetAdpcmAVolume(0, vol);
                ((ym2610)c.Instrument).SetAdpcmAVolume(1, vol);
            }
        }

        public void SetVolumeYM2610AdpcmB(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2610)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YM2610) continue;
                ((ym2610)c.Instrument).SetAdpcmBVolume(0, vol);
                ((ym2610)c.Instrument).SetAdpcmBVolume(1, vol);
            }
        }

        public void SetVolumeYM2612(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2612)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YM2612) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeYM3438(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM3438)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YM3438) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeSN76489(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.SN76489)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.SN76489) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeRF5C164(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.RF5C164)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.RF5C164) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumePWM(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.PWM)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.PWM) continue;
                c.Volume = Math.Max(Math.Min(vol, 20),-192);
            }
        }

        public void SetVolumeOKIM6258(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.OKIM6258)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.OKIM6258) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeMpcmX68k(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.mpcmX68k)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.mpcmX68k) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeOKIM6295(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.OKIM6295)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.OKIM6295) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeC140(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.C140)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.C140) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeC352(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.C352)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.C352) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetRearMute(byte flag)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.C352)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.C352) continue;
                for (int i = 0; i < dicInst[enmInstrumentType.C352].Length; i++)
                    ((c352)dicInst[enmInstrumentType.C352][i]).c352_set_options(flag);
            }
        }

        public void SetVolumeK051649(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.K051649)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.K051649) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeK053260(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.K053260)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.K053260) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeRF5C68(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.RF5C68)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.RF5C68) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeYM3812(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM3812)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YM3812) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeY8950(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.Y8950)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.Y8950) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeYM3526(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM3526)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YM3526) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeK054539(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.K054539)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.K054539) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeQSound(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.QSound)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.QSound) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeQSoundCtr(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.QSoundCtr)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.QSoundCtr) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeDMG(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.DMG)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.DMG) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeGA20(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.GA20)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.GA20) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeYMZ280B(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YMZ280B)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YMZ280B) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeYMF271(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YMF271)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YMF271) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeYMF262(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YMF262)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YMF262) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeYMF278B(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YMF278B)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YMF278B) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeMultiPCM(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.MultiPCM)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.MultiPCM) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeSegaPCM(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.SEGAPCM)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.SEGAPCM) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeNES(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.Nes)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type == enmInstrumentType.Nes)
                {
                    c.Volume = Math.Max(Math.Min(vol, 20), -192);
                    ((nes_intf)c.Instrument).SetVolumeAPU(vol);
                }
            }
        }

        public void SetVolumeDMC(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.DMC)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type == enmInstrumentType.DMC)
                {
                    c.Volume = Math.Max(Math.Min(vol, 20), -192);
                    ((nes_intf)c.Instrument).SetVolumeDMC(vol);
                }
            }
        }

        public void SetVolumeFDS(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.FDS)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type == enmInstrumentType.FDS)
                {
                    c.Volume = Math.Max(Math.Min(vol, 20), -192);
                    ((nes_intf)c.Instrument).SetVolumeFDS(vol);
                }
            }
        }

        public void SetVolumeMMC5(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.MMC5)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.MMC5) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeN160(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.N160)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.N160) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeVRC6(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.VRC6)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.VRC6) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeVRC7(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.VRC7)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.VRC7) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeFME7(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.FME7)) return;

            if (insts == null) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.FME7) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }



        public int[] ReadSN76489Register()
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.SN76489)) return null;
                return ((sn76489)(dicInst[enmInstrumentType.SN76489][0])).SN76489_Chip[0].Registers;
            }
        }

        public int[] ReadSN76489Register(int ChipIndex)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.SN76489)) return null;
                return ((sn76489)(dicInst[enmInstrumentType.SN76489][ChipIndex])).SN76489_Chip[0].Registers;
            }
        }

        public int[][] ReadYM2612Register(byte chipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2612)) return null;
                return ((ym2612)(dicInst[enmInstrumentType.YM2612][0])).YM2612_Chip[chipID].REG;
            }
        }

        public int[][] ReadYM2612Register(int ChipIndex,byte chipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2612)) return null;
                return ((ym2612)(dicInst[enmInstrumentType.YM2612][ChipIndex])).YM2612_Chip[chipID].REG;
            }
        }

        public scd_pcm.pcm_chip_ ReadRf5c164Register(int chipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.RF5C164)) return null;
                if (((scd_pcm)(dicInst[enmInstrumentType.RF5C164][0])).PCM_Chip == null || ((scd_pcm)(dicInst[enmInstrumentType.RF5C164][0])).PCM_Chip.Length < 1) return null;
                return ((scd_pcm)(dicInst[enmInstrumentType.RF5C164][0])).PCM_Chip[chipID];
            }
        }

        public scd_pcm.pcm_chip_ ReadRf5c164Register(int ChipIndex,int chipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.RF5C164)) return null;
                if (((scd_pcm)(dicInst[enmInstrumentType.RF5C164][ChipIndex])).PCM_Chip == null || ((scd_pcm)(dicInst[enmInstrumentType.RF5C164][ChipIndex])).PCM_Chip.Length < 1) return null;
                return ((scd_pcm)(dicInst[enmInstrumentType.RF5C164][ChipIndex])).PCM_Chip[chipID];
            }
        }

        public c140.c140_state ReadC140Register(int cur)
        {
            lock(lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.C140)) return null;
                return ((c140)dicInst[enmInstrumentType.C140][0]).C140Data[cur];
            }
        }

        public c140.c140_state ReadC140Register(int ChipIndex,int cur)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.C140)) return null;
                return ((c140)dicInst[enmInstrumentType.C140][ChipIndex]).C140Data[cur];
            }
        }

        public ushort[] ReadC352Flag(int chipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.C352)) return null;
                return ((c352)dicInst[enmInstrumentType.C352][0]).flags[chipID];
            }
        }

        public ushort[] ReadC352Flag(int ChipIndex,int chipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.C352)) return null;
                return ((c352)dicInst[enmInstrumentType.C352][ChipIndex]).flags[chipID];
            }
        }

        public multipcm._MultiPCM ReadMultiPCMRegister(int chipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.MultiPCM)) return null;
                return ((multipcm)(dicInst[enmInstrumentType.MultiPCM][0])).multipcm_r(chipID);
            }
        }

        public multipcm._MultiPCM ReadMultiPCMRegister(int ChipIndex,int chipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.MultiPCM)) return null;
                return ((multipcm)(dicInst[enmInstrumentType.MultiPCM][ChipIndex])).multipcm_r(chipID);
            }
        }

        public okim6258.okim6258_state ReadOKIM6258Status(int chipID)
        {
            lock(lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.OKIM6258)) return null;
                return ((okim6258)dicInst[enmInstrumentType.OKIM6258][0]).OKIM6258Data[chipID];
            }
        }

        public okim6258.okim6258_state ReadOKIM6258Status(int ChipIndex,int chipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.OKIM6258)) return null;
                return ((okim6258)dicInst[enmInstrumentType.OKIM6258][ChipIndex]).OKIM6258Data[chipID];
            }
        }

        public okim6295.okim6295_state ReadOKIM6295Status(int chipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.OKIM6295)) return null;
                return ((okim6295)dicInst[enmInstrumentType.OKIM6295][0]).OKIM6295Data[chipID];
            }
        }

        public okim6295.okim6295_state ReadOKIM6295Status(int ChipIndex, int chipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.OKIM6295)) return null;
                return ((okim6295)dicInst[enmInstrumentType.OKIM6295][ChipIndex]).OKIM6295Data[chipID];
            }
        }

        public segapcm.segapcm_state ReadSegaPCMStatus(int chipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.SEGAPCM)) return null;
                return ((segapcm)dicInst[enmInstrumentType.SEGAPCM][0]).SPCMData[chipID];
            }
        }

        public segapcm.segapcm_state ReadSegaPCMStatus(int ChipIndex,int chipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.SEGAPCM)) return null;
                return ((segapcm)dicInst[enmInstrumentType.SEGAPCM][ChipIndex]).SPCMData[chipID];
            }
        }


        public Ootake_PSG.huc6280_state ReadHuC6280Status(int chipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.HuC6280)) return null;
                return ((Ootake_PSG)dicInst[enmInstrumentType.HuC6280][0]).GetState((byte)chipID);
            }
        }

        public Ootake_PSG.huc6280_state ReadHuC6280Status(int ChipIndex,int chipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.HuC6280)) return null;
                return ((Ootake_PSG)dicInst[enmInstrumentType.HuC6280][ChipIndex]).GetState((byte)chipID);
            }
        }

        public K051649.k051649_state ReadK051649Status(int chipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.K051649)) return null;
                return ((K051649)dicInst[enmInstrumentType.K051649][0]).GetK051649_State((byte)chipID);
            }
        }

        public K051649.k051649_state ReadK051649Status(int ChipIndex,int chipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.K051649)) return null;
                return ((K051649)dicInst[enmInstrumentType.K051649][ChipIndex]).GetK051649_State((byte)chipID);
            }
        }

        public int[][] ReadRf5c164Volume(int chipID)
        {
            lock (lockobj)
            {
                return rf5c164Vol[chipID];
            }
        }

        public int[] ReadYM2612KeyOn(byte chipID)
        {
            lock (lockobj)
            {
                int[] keys = new int[((ym2612)(dicInst[enmInstrumentType.YM2612][0])).YM2612_Chip[chipID].CHANNEL.Length];
                for (int i = 0; i < keys.Length; i++)
                    keys[i] = ((ym2612)(dicInst[enmInstrumentType.YM2612][0])).YM2612_Chip[chipID].CHANNEL[i].KeyOn;
                return keys;
            }
        }

        public int[] ReadYM2612KeyOn(int ChipIndex,byte chipID)
        {
            lock (lockobj)
            {
                int[] keys = new int[((ym2612)(dicInst[enmInstrumentType.YM2612][ChipIndex])).YM2612_Chip[chipID].CHANNEL.Length];
                for (int i = 0; i < keys.Length; i++)
                    keys[i] = ((ym2612)(dicInst[enmInstrumentType.YM2612][ChipIndex])).YM2612_Chip[chipID].CHANNEL[i].KeyOn;
                return keys;
            }
        }

        public int[] ReadYM2151KeyOn(int chipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2151)) return null;
                for (int i = 0; i < 8; i++)
                {
                    //ym2151Key[chipID][i] = ((ym2151)(iYM2151)).YM2151_Chip[chipID].CHANNEL[i].KeyOn;
                }
                return ym2151Key[chipID];
            }
        }

        public int[] ReadYM2203KeyOn(int chipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2203)) return null;
                for (int i = 0; i < 6; i++)
                {
                    //ym2203Key[chipID][i] = ((ym2203)(iYM2203)).YM2203_Chip[chipID].CHANNEL[i].KeyOn;
                }
                return ym2203Key[chipID];
            }
        }

        public int[] ReadYM2608KeyOn(int chipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2608)) return null;
                for (int i = 0; i < 11; i++)
                {
                    //ym2608Key[chipID][i] = ((ym2608)(iYM2608)).YM2608_Chip[chipID].CHANNEL[i].KeyOn;
                }
                return ym2608Key[chipID];
            }
        }

        public int[] ReadYM2609KeyOn(int chipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2609)) return null;
                for (int i = 0; i < 11; i++)
                {
                    //ym2608Key[chipID][i] = ((ym2608)(iYM2608)).YM2608_Chip[chipID].CHANNEL[i].KeyOn;
                }
                return ym2609Key[chipID];
            }
        }

        public int[] ReadYM2610KeyOn(int chipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2610)) return null;
                for (int i = 0; i < 11; i++)
                {
                    //ym2610Key[chipID][i] = ((ym2610)(iYM2610)).YM2610_Chip[chipID].CHANNEL[i].KeyOn;
                }
                return ym2610Key[chipID];
            }
        }




        public void setSN76489Mask(int chipID, int ch)
        {
            lock (lockobj)
            {
                sn76489Mask[0][chipID] &= ~ch;
                if (!dicInst.ContainsKey(enmInstrumentType.SN76489)) return;
                ((sn76489)(dicInst[enmInstrumentType.SN76489][0])).SN76489_SetMute((byte)chipID, sn76489Mask[0][chipID]);
            }
        }

        public void setSN76489Mask(int ChipIndex,int chipID, int ch)
        {
            lock (lockobj)
            {
                sn76489Mask[ChipIndex][chipID] &= ~ch;
                if (!dicInst.ContainsKey(enmInstrumentType.SN76489)) return;
                ((sn76489)(dicInst[enmInstrumentType.SN76489][ChipIndex])).SN76489_SetMute((byte)chipID, sn76489Mask[ChipIndex][chipID]);
            }
        }

        public void setYM2612Mask(int chipID,int ch)
        {
            lock (lockobj)
            {
                ym2612Mask[0][chipID] |= ch;
                if (!dicInst.ContainsKey(enmInstrumentType.YM2612)) return;
                ((ym2612)(dicInst[enmInstrumentType.YM2612][0])).YM2612_SetMute((byte)chipID, ym2612Mask[0][chipID]);
            }
        }

        public void setYM2612Mask(int ChipIndex,int chipID, int ch)
        {
            lock (lockobj)
            {
                ym2612Mask[ChipIndex][chipID] |= ch;
                if (!dicInst.ContainsKey(enmInstrumentType.YM2612)) return;
                ((ym2612)(dicInst[enmInstrumentType.YM2612][ChipIndex])).YM2612_SetMute((byte)chipID, ym2612Mask[ChipIndex][chipID]);
            }
        }

        public void setYM2203Mask(int chipID, int ch)
        {
            lock (lockobj)
            {
                ym2203Mask[0][chipID] |= ch;
                if (!dicInst.ContainsKey(enmInstrumentType.YM2203)) return;
                ((ym2203)(dicInst[enmInstrumentType.YM2203][0])).YM2203_SetMute((byte)chipID, ym2203Mask[0][chipID]);
            }
        }
        public void setYM2203Mask(int ChipIndex,int chipID, int ch)
        {
            lock (lockobj)
            {
                ym2203Mask[ChipIndex][chipID] |= ch;
                if (!dicInst.ContainsKey(enmInstrumentType.YM2203)) return;
                ((ym2203)(dicInst[enmInstrumentType.YM2203][ChipIndex])).YM2203_SetMute((byte)chipID, ym2203Mask[ChipIndex][chipID]);
            }
        }

        public void setRf5c164Mask(int chipID, int ch)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.RF5C164)) return;
                ((scd_pcm)(dicInst[enmInstrumentType.RF5C164][0])).PCM_Chip[chipID].Channel[ch].Muted = 1;
            }
        }

        public void setRf5c164Mask(int ChipIndex, int chipID, int ch)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.RF5C164)) return;
                ((scd_pcm)(dicInst[enmInstrumentType.RF5C164][ChipIndex])).PCM_Chip[chipID].Channel[ch].Muted = 1;
            }
        }

        public void setC140Mask(int chipID, int ch)
        {
            lock (lockobj)
            {
                c140Mask[0][chipID] |= (uint)ch;
                if (!dicInst.ContainsKey(enmInstrumentType.C140)) return;
                ((c140)(dicInst[enmInstrumentType.C140][0])).c140_set_mute_mask((byte)chipID, c140Mask[0][chipID]);
            }
        }

        public void setC140Mask(int ChipIndex,int chipID, int ch)
        {
            lock (lockobj)
            {
                c140Mask[ChipIndex][chipID] |= (uint)ch;
                if (!dicInst.ContainsKey(enmInstrumentType.C140)) return;
                ((c140)(dicInst[enmInstrumentType.C140][ChipIndex])).c140_set_mute_mask((byte)chipID, c140Mask[ChipIndex][chipID]);
            }
        }

        public void setSegaPcmMask(int chipID, int ch)
        {
            lock (lockobj)
            {
                segapcmMask[0][chipID] |= (uint)ch;
                if (!dicInst.ContainsKey(enmInstrumentType.SEGAPCM)) return;
                ((segapcm)(dicInst[enmInstrumentType.SEGAPCM][0])).segapcm_set_mute_mask((byte)chipID, segapcmMask[0][chipID]);
            }
        }

        public void setSegaPcmMask(int ChipIndex, int chipID, int ch)
        {
            lock (lockobj)
            {
                segapcmMask[ChipIndex][chipID] |= (uint)ch;
                if (!dicInst.ContainsKey(enmInstrumentType.SEGAPCM)) return;
                ((segapcm)(dicInst[enmInstrumentType.SEGAPCM][ChipIndex])).segapcm_set_mute_mask((byte)chipID, segapcmMask[ChipIndex][chipID]);
            }
        }

        public void setOKIM6295Mask(int ChipIndex, int chipID, int ch)
        {
            lock (lockobj)
            {
                okim6295Mask[ChipIndex][chipID] |= (uint)ch;
                if (!dicInst.ContainsKey(enmInstrumentType.OKIM6295)) return;
                ((okim6295)(dicInst[enmInstrumentType.OKIM6295][ChipIndex])).okim6295_set_mute_mask((byte)chipID, okim6295Mask[ChipIndex][chipID]);
            }
        }

        public okim6295.okim6295Info GetOKIM6295Info(int ChipIndex, int chipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.OKIM6295)) return null;
                return ((okim6295)(dicInst[enmInstrumentType.OKIM6295][ChipIndex])).ReadChInfo((byte)chipID);
            }
        }

        public void setHuC6280Mask(int chipID, int ch)
        {
            lock (lockobj)
            {
                huc6280Mask[0][chipID] |= ch;
                if (!dicInst.ContainsKey(enmInstrumentType.HuC6280)) return;
                ((Ootake_PSG)(dicInst[enmInstrumentType.HuC6280][0])).HuC6280_SetMute((byte)chipID, huc6280Mask[0][chipID]);
            }
        }

        public void setHuC6280Mask(int ChipIndex,int chipID, int ch)
        {
            lock (lockobj)
            {
                huc6280Mask[ChipIndex][chipID] |= ch;
                if (!dicInst.ContainsKey(enmInstrumentType.HuC6280)) return;
                ((Ootake_PSG)(dicInst[enmInstrumentType.HuC6280][ChipIndex])).HuC6280_SetMute((byte)chipID, huc6280Mask[ChipIndex][chipID]);
            }
        }

        public void setNESMask(int chipID, int ch)
        {
            lock (lockobj)
            {
                nesMask[0][chipID] |= (uint)(0x1 << ch);
                if (!dicInst.ContainsKey(enmInstrumentType.Nes)) return;
                ((nes_intf)(dicInst[enmInstrumentType.Nes][0])).nes_set_mute_mask((byte)chipID, nesMask[0][chipID]);
            }
        }

        public void setNESMask(int ChipIndex,int chipID, int ch)
        {
            lock (lockobj)
            {
                nesMask[ChipIndex][chipID] |= (uint)(0x1 << ch);
                if (!dicInst.ContainsKey(enmInstrumentType.Nes)) return;
                ((nes_intf)(dicInst[enmInstrumentType.Nes][ChipIndex])).nes_set_mute_mask((byte)chipID, nesMask[ChipIndex][chipID]);
            }
        }

        public void setFDSMask(int chipID)
        {
            lock (lockobj)
            {
                nesMask[0][chipID] |= 0x20;
                if (!dicInst.ContainsKey(enmInstrumentType.Nes)) return;
                ((nes_intf)(dicInst[enmInstrumentType.Nes][0])).nes_set_mute_mask((byte)chipID, nesMask[0][chipID]);
            }
        }

        public void setFDSMask(int ChipIndex,int chipID)
        {
            lock (lockobj)
            {
                nesMask[ChipIndex][chipID] |= 0x20;
                if (!dicInst.ContainsKey(enmInstrumentType.Nes)) return;
                ((nes_intf)(dicInst[enmInstrumentType.Nes][ChipIndex])).nes_set_mute_mask((byte)chipID, nesMask[ChipIndex][chipID]);
            }
        }



        public void resetSN76489Mask(int chipID, int ch)
        {
            lock (lockobj)
            {
                sn76489Mask[0][chipID] |= ch;
                if (!dicInst.ContainsKey(enmInstrumentType.SN76489)) return;
                ((sn76489)(dicInst[enmInstrumentType.SN76489][0])).SN76489_SetMute((byte)chipID, sn76489Mask[0][chipID]);
            }
        }

        public void resetSN76489Mask(int ChipIndex,int chipID, int ch)
        {
            lock (lockobj)
            {
                sn76489Mask[ChipIndex][chipID] |= ch;
                if (!dicInst.ContainsKey(enmInstrumentType.SN76489)) return;
                ((sn76489)(dicInst[enmInstrumentType.SN76489][ChipIndex])).SN76489_SetMute((byte)chipID, sn76489Mask[ChipIndex][chipID]);
            }
        }


        public void resetYM2612Mask(int chipID, int ch)
        {
            lock (lockobj)
            {
                ym2612Mask[0][chipID] &= ~ch;
                if (!dicInst.ContainsKey(enmInstrumentType.YM2612)) return;
                ((ym2612)(dicInst[enmInstrumentType.YM2612][0])).YM2612_SetMute((byte)chipID, ym2612Mask[0][chipID]);
            }
        }

        public void resetYM2612Mask(int ChipIndex,int chipID, int ch)
        {
            lock (lockobj)
            {
                ym2612Mask[ChipIndex][chipID] &= ~ch;
                if (!dicInst.ContainsKey(enmInstrumentType.YM2612)) return;
                ((ym2612)(dicInst[enmInstrumentType.YM2612][ChipIndex])).YM2612_SetMute((byte)chipID, ym2612Mask[ChipIndex][chipID]);
            }
        }

        public void resetYM2203Mask(int chipID, int ch)
        {
            lock (lockobj)
            {
                ym2203Mask[0][chipID] &= ~ch;
                if (!dicInst.ContainsKey(enmInstrumentType.YM2203)) return;
                ((ym2203)(dicInst[enmInstrumentType.YM2203][0])).YM2203_SetMute((byte)chipID, ym2203Mask[0][chipID]);
            }
        }

        public void resetYM2203Mask(int ChipIndex,int chipID, int ch)
        {
            lock (lockobj)
            {
                ym2203Mask[ChipIndex][chipID] &= ~ch;
                if (!dicInst.ContainsKey(enmInstrumentType.YM2203)) return;
                ((ym2203)(dicInst[enmInstrumentType.YM2203][ChipIndex])).YM2203_SetMute((byte)chipID, ym2203Mask[ChipIndex][chipID]);
            }
        }

        public void resetRf5c164Mask(int chipID, int ch)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.RF5C164)) return;
                ((scd_pcm)(dicInst[enmInstrumentType.RF5C164][0])).PCM_Chip[chipID].Channel[ch].Muted = 0;
            }
        }

        public void resetRf5c164Mask(int ChipIndex,int chipID, int ch)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.RF5C164)) return;
                ((scd_pcm)(dicInst[enmInstrumentType.RF5C164][ChipIndex])).PCM_Chip[chipID].Channel[ch].Muted = 0;
            }
        }

        public void resetC140Mask(int chipID, int ch)
        {
            lock (lockobj)
            {
                c140Mask[0][chipID] &= ~(uint)ch;
                if (!dicInst.ContainsKey(enmInstrumentType.C140)) return;
                ((c140)(dicInst[enmInstrumentType.C140][0])).c140_set_mute_mask((byte)chipID, c140Mask[0][chipID]);
            }
        }

        public void resetC140Mask(int ChipIndex,int chipID, int ch)
        {
            lock (lockobj)
            {
                c140Mask[ChipIndex][chipID] &= ~(uint)ch;
                if (!dicInst.ContainsKey(enmInstrumentType.C140)) return;
                ((c140)(dicInst[enmInstrumentType.C140][ChipIndex])).c140_set_mute_mask((byte)chipID, c140Mask[ChipIndex][chipID]);
            }
        }

        public void resetSegaPcmMask(int chipID, int ch)
        {
            lock (lockobj)
            {
                segapcmMask[0][chipID] &= ~(uint)ch;
                if (!dicInst.ContainsKey(enmInstrumentType.SEGAPCM)) return;
                 ((segapcm)(dicInst[enmInstrumentType.SEGAPCM][0])).segapcm_set_mute_mask((byte)chipID, segapcmMask[0][chipID]);
            }
        }

        public void resetSegaPcmMask(int ChipIndex,int chipID, int ch)
        {
            lock (lockobj)
            {
                segapcmMask[ChipIndex][chipID] &= ~(uint)ch;
                if (!dicInst.ContainsKey(enmInstrumentType.SEGAPCM)) return;
                ((segapcm)(dicInst[enmInstrumentType.SEGAPCM][ChipIndex])).segapcm_set_mute_mask((byte)chipID, segapcmMask[ChipIndex][chipID]);
            }
        }

        public void resetOKIM6295Mask(int ChipIndex, int chipID, int ch)
        {
            lock (lockobj)
            {
                okim6295Mask[ChipIndex][chipID] &= ~(uint)ch;
                if (!dicInst.ContainsKey(enmInstrumentType.OKIM6295)) return;
                ((okim6295)(dicInst[enmInstrumentType.OKIM6295][ChipIndex])).okim6295_set_mute_mask((byte)chipID, okim6295Mask[ChipIndex][chipID]);
            }
        }

        public void resetHuC6280Mask(int chipID, int ch)
        {
            lock (lockobj)
            {
                huc6280Mask[0][chipID] &= ~ch;
                if (!dicInst.ContainsKey(enmInstrumentType.HuC6280)) return;
                ((Ootake_PSG)(dicInst[enmInstrumentType.HuC6280][0])).HuC6280_SetMute((byte)chipID, huc6280Mask[0][chipID]);
            }
        }

        public void resetHuC6280Mask(int ChipIndex,int chipID, int ch)
        {
            lock (lockobj)
            {
                huc6280Mask[ChipIndex][chipID] &= ~ch;
                if (!dicInst.ContainsKey(enmInstrumentType.HuC6280)) return;
                ((Ootake_PSG)(dicInst[enmInstrumentType.HuC6280][ChipIndex])).HuC6280_SetMute((byte)chipID, huc6280Mask[ChipIndex][chipID]);
            }
        }

        public void resetNESMask(int chipID, int ch)
        {
            lock (lockobj)
            {
                nesMask[0][chipID] &= (uint)~(0x1 << ch);
                if (!dicInst.ContainsKey(enmInstrumentType.Nes)) return;
                ((nes_intf)(dicInst[enmInstrumentType.Nes][0])).nes_set_mute_mask((byte)chipID, nesMask[0][chipID]);
            }
        }

        public void resetNESMask(int ChipIndex, int chipID, int ch)
        {
            lock (lockobj)
            {
                nesMask[ChipIndex][chipID] &= (uint)~(0x1 << ch);
                if (!dicInst.ContainsKey(enmInstrumentType.Nes)) return;
                ((nes_intf)(dicInst[enmInstrumentType.Nes][ChipIndex])).nes_set_mute_mask((byte)chipID, nesMask[ChipIndex][chipID]);
            }
        }

        public void resetFDSMask(int chipID)
        {
            lock (lockobj)
            {
                nesMask[0][chipID] &= ~(uint)0x20;
                if (!dicInst.ContainsKey(enmInstrumentType.Nes)) return;
                ((nes_intf)(dicInst[enmInstrumentType.Nes][0])).nes_set_mute_mask((byte)chipID, nesMask[0][chipID]);
            }
        }

        public void resetFDSMask(int ChipIndex,int chipID)
        {
            lock (lockobj)
            {
                nesMask[ChipIndex][chipID] &= ~(uint)0x20;
                if (!dicInst.ContainsKey(enmInstrumentType.Nes)) return;
                ((nes_intf)(dicInst[enmInstrumentType.Nes][ChipIndex])).nes_set_mute_mask((byte)chipID, nesMask[ChipIndex][chipID]);
            }
        }



        public int[][][] getYM2151VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2151)) return null;
            return (dicInst[enmInstrumentType.YM2151][0]).visVolume;
        }

        public int[][][] getYM2151VisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2151)) return null;
            return (dicInst[enmInstrumentType.YM2151][ChipIndex]).visVolume;
        }

        public int[][][] getYM2203VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2203)) return null;
            return ((ym2203)dicInst[enmInstrumentType.YM2203][0]).visVolume;
        }

        public int[][][] getYM2203VisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2203)) return null;
            return ((ym2203)dicInst[enmInstrumentType.YM2203][ChipIndex]).visVolume;
        }

        public int[][][] getYM2413VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2413)) return null;
            return ((ym2413)dicInst[enmInstrumentType.YM2413][0]).visVolume;
        }

        public int[][][] getYM2413VisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2413)) return null;
            return ((ym2413)dicInst[enmInstrumentType.YM2413][ChipIndex]).visVolume;
        }

        public int[][][] getYM2608VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2608)) return null;
            return ((ym2608)dicInst[enmInstrumentType.YM2608][0]).visVolume;
        }

        public int[][][] getYM2608VisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2608)) return null;
            return ((ym2608)dicInst[enmInstrumentType.YM2608][ChipIndex]).visVolume;
        }

        public int[][][] getYM2609VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2609)) return null;
            return ((ym2608)dicInst[enmInstrumentType.YM2609][0]).visVolume;
        }

        public int[][][] getYM2609VisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2609)) return null;
            return ((ym2608)dicInst[enmInstrumentType.YM2609][ChipIndex]).visVolume;
        }

        public int[][][] getYM2610VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2610)) return null;
            return ((ym2610)dicInst[enmInstrumentType.YM2610][0]).visVolume;
        }

        public int[][][] getYM2610VisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2610)) return null;
            return ((ym2610)dicInst[enmInstrumentType.YM2610][ChipIndex]).visVolume;
        }

        public int[][][] getYM2612VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2612)) return null;
            return (dicInst[enmInstrumentType.YM2612][0]).visVolume;
        }

        public int[][][] getYM2612VisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2612)) return null;
            return (dicInst[enmInstrumentType.YM2612][ChipIndex]).visVolume;
        }

        public int[][][] getSN76489VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.SN76489)) return null;
            return ((sn76489)dicInst[enmInstrumentType.SN76489][0]).visVolume;
        }

        public int[][][] getSN76489VisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.SN76489)) return null;
            return ((sn76489)dicInst[enmInstrumentType.SN76489][ChipIndex]).visVolume;
        }

        public int[][][] getHuC6280VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.HuC6280)) return null;
            return ((Ootake_PSG)dicInst[enmInstrumentType.HuC6280][0]).visVolume;
        }

        public int[][][] getHuC6280VisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.HuC6280)) return null;
            return ((Ootake_PSG)dicInst[enmInstrumentType.HuC6280][ChipIndex]).visVolume;
        }

        public int[][][] getRF5C164VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.RF5C164)) return null;
            return ((scd_pcm)dicInst[enmInstrumentType.RF5C164][0]).visVolume;
        }

        public int[][][] getRF5C164VisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.RF5C164)) return null;
            return ((scd_pcm)dicInst[enmInstrumentType.RF5C164][ChipIndex]).visVolume;
        }

        public int[][][] getPWMVisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.PWM)) return null;
            return ((pwm)dicInst[enmInstrumentType.PWM][0]).visVolume;
        }

        public int[][][] getPWMVisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.PWM)) return null;
            return ((pwm)dicInst[enmInstrumentType.PWM][ChipIndex]).visVolume;
        }

        public int[][][] getOKIM6258VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.OKIM6258)) return null;
            return ((okim6258)dicInst[enmInstrumentType.OKIM6258][0]).visVolume;
        }

        public int[][][] getOKIM6258VisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.OKIM6258)) return null;
            return ((okim6258)dicInst[enmInstrumentType.OKIM6258][ChipIndex]).visVolume;
        }

        public int[][][] getOKIM6295VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.OKIM6295)) return null;
            return ((okim6295)dicInst[enmInstrumentType.OKIM6295][0]).visVolume;
        }

        public int[][][] getOKIM6295VisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.OKIM6295)) return null;
            return ((okim6295)dicInst[enmInstrumentType.OKIM6295][ChipIndex]).visVolume;
        }

        public int[][][] getC140VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.C140)) return null;
            return ((c140)dicInst[enmInstrumentType.C140][0]).visVolume;
        }

        public int[][][] getC140VisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.C140)) return null;
            return ((c140)dicInst[enmInstrumentType.C140][ChipIndex]).visVolume;
        }

        public int[][][] getSegaPCMVisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.SEGAPCM)) return null;
            return ((segapcm)dicInst[enmInstrumentType.SEGAPCM][0]).visVolume;
        }

        public int[][][] getSegaPCMVisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.SEGAPCM)) return null;
            return ((segapcm)dicInst[enmInstrumentType.SEGAPCM][ChipIndex]).visVolume;
        }

        public int[][][] getC352VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.C352)) return null;
            return ((c352)dicInst[enmInstrumentType.C352][0]).visVolume;
        }

        public int[][][] getC352VisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.C352)) return null;
            return ((c352)dicInst[enmInstrumentType.C352][ChipIndex]).visVolume;
        }

        public int[][][] getK051649VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.K051649)) return null;
            return ((K051649)dicInst[enmInstrumentType.K051649][0]).visVolume;
        }

        public int[][][] getK051649VisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.K051649)) return null;
            return ((K051649)dicInst[enmInstrumentType.K051649][ChipIndex]).visVolume;
        }

        public int[][][] getK054539VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.K054539)) return null;
            return ((K054539)dicInst[enmInstrumentType.K054539][0]).visVolume;
        }

        public int[][][] getK054539VisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.K054539)) return null;
            return ((K054539)dicInst[enmInstrumentType.K054539][ChipIndex]).visVolume;
        }




        public int[][][] getNESVisVolume()
        {
            return null;
            //if (!dicInst.ContainsKey(enmInstrumentType.Nes)) return null;
            //return dicInst[enmInstrumentType.Nes].visVolume;
        }

        public int[][][] getDMCVisVolume()
        {
            return null;
            //if (!dicInst.ContainsKey(enmInstrumentType.DMC)) return null;
            //return dicInst[enmInstrumentType.DMC].visVolume;
        }

        public int[][][] getFDSVisVolume()
        {
            return null;
            //if (!dicInst.ContainsKey(enmInstrumentType.FDS)) return null;
            //return dicInst[enmInstrumentType.FDS].visVolume;
        }

        public int[][][] getMMC5VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.MMC5)) return null;
            return dicInst[enmInstrumentType.MMC5][0].visVolume;
        }

        public int[][][] getMMC5VisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.MMC5)) return null;
            return dicInst[enmInstrumentType.MMC5][ChipIndex].visVolume;
        }

        public int[][][] getN160VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.N160)) return null;
            return dicInst[enmInstrumentType.N160][0].visVolume;
        }

        public int[][][] getN160VisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.N160)) return null;
            return dicInst[enmInstrumentType.N160][ChipIndex].visVolume;
        }

        public int[][][] getVRC6VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.VRC6)) return null;
            return dicInst[enmInstrumentType.VRC6][0].visVolume;
        }

        public int[][][] getVRC6VisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.VRC6)) return null;
            return dicInst[enmInstrumentType.VRC6][ChipIndex].visVolume;
        }

        public int[][][] getVRC7VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.VRC7)) return null;
            return dicInst[enmInstrumentType.VRC7][0].visVolume;
        }

        public int[][][] getVRC7VisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.VRC7)) return null;
            return dicInst[enmInstrumentType.VRC7][ChipIndex].visVolume;
        }

        public int[][][] getFME7VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.FME7)) return null;
            return dicInst[enmInstrumentType.FME7][0].visVolume;
        }

        public int[][][] getFME7VisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.FME7)) return null;
            return dicInst[enmInstrumentType.FME7][ChipIndex].visVolume;
        }

        public int[][][] getYM3526VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM3526)) return null;
            return dicInst[enmInstrumentType.YM3526][0].visVolume;
        }

        public int[][][] getYM3526VisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM3526)) return null;
            return dicInst[enmInstrumentType.YM3526][ChipIndex].visVolume;
        }

        public int[][][] getY8950VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.Y8950)) return null;
            return dicInst[enmInstrumentType.Y8950][0].visVolume;
        }

        public int[][][] getY8950VisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.Y8950)) return null;
            return dicInst[enmInstrumentType.Y8950][ChipIndex].visVolume;
        }

        public int[][][] getYM3812VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM3812)) return null;
            return dicInst[enmInstrumentType.YM3812][0].visVolume;
        }

        public int[][][] getYM3812VisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM3812)) return null;
            return dicInst[enmInstrumentType.YM3812][ChipIndex].visVolume;
        }

        public int[][][] getYMF262VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YMF262)) return null;
            return dicInst[enmInstrumentType.YMF262][0].visVolume;
        }

        public int[][][] getYMF262VisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YMF262)) return null;
            return dicInst[enmInstrumentType.YMF262][ChipIndex].visVolume;
        }

        public int[][][] getYMF278BVisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YMF278B)) return null;
            return dicInst[enmInstrumentType.YMF278B][0].visVolume;
        }

        public int[][][] getYMF278BVisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YMF278B)) return null;
            return dicInst[enmInstrumentType.YMF278B][ChipIndex].visVolume;
        }

        public int[][][] getYMZ280BVisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YMZ280B)) return null;
            return dicInst[enmInstrumentType.YMZ280B][0].visVolume;
        }

        public int[][][] getYMZ280BVisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YMZ280B)) return null;
            return dicInst[enmInstrumentType.YMZ280B][ChipIndex].visVolume;
        }

        public int[][][] getYMF271VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YMF271)) return null;
            return dicInst[enmInstrumentType.YMF271][0].visVolume;
        }

        public int[][][] getYMF271VisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YMF271)) return null;
            return dicInst[enmInstrumentType.YMF271][ChipIndex].visVolume;
        }

        public int[][][] getRF5C68VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.RF5C68)) return null;
            return dicInst[enmInstrumentType.RF5C68][0].visVolume;
        }

        public int[][][] getRF5C68VisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.RF5C68)) return null;
            return dicInst[enmInstrumentType.RF5C68][ChipIndex].visVolume;
        }

        public int[][][] getMultiPCMVisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.MultiPCM)) return null;
            return dicInst[enmInstrumentType.MultiPCM][0].visVolume;
        }
        public int[][][] getMultiPCMVisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.MultiPCM)) return null;
            return dicInst[enmInstrumentType.MultiPCM][ChipIndex].visVolume;
        }

        public int[][][] getK053260VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.K053260)) return null;
            return dicInst[enmInstrumentType.K053260][0].visVolume;
        }
        public int[][][] getK053260VisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.K053260)) return null;
            return dicInst[enmInstrumentType.K053260][ChipIndex].visVolume;
        }

        public int[][][] getQSoundVisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.QSound)) return null;
            return dicInst[enmInstrumentType.QSound][0].visVolume;
        }
        public int[][][] getQSoundVisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.QSound)) return null;
            return dicInst[enmInstrumentType.QSound][ChipIndex].visVolume;
        }

        public int[][][] getQSoundCtrVisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.QSoundCtr)) return null;
            return dicInst[enmInstrumentType.QSoundCtr][0].visVolume;
        }
        public int[][][] getQSoundCtrVisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.QSoundCtr)) return null;
            return dicInst[enmInstrumentType.QSoundCtr][ChipIndex].visVolume;
        }

        public int[][][] getGA20VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.GA20)) return null;
            return dicInst[enmInstrumentType.GA20][0].visVolume;
        }
        public int[][][] getGA20VisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.GA20)) return null;
            return dicInst[enmInstrumentType.GA20][ChipIndex].visVolume;
        }

        public int[][][] getDMGVisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.DMG)) return null;
            return dicInst[enmInstrumentType.DMG][0].visVolume;
        }

        public int[][][] getDMGVisVolume(int ChipIndex)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.DMG)) return null;
            return dicInst[enmInstrumentType.DMG][ChipIndex].visVolume;
        }



        /// <summary>
        /// Left全体ボリュームの取得(視覚効果向け)
        /// </summary>
        public int getTotalVolumeL()
        {
            lock (lockobj)
            {
                int v = 0;
                for (int i = 0; i < buffer[0].Length; i++)
                {
                    v = Math.Max(v,Math.Abs(buffer[0][i]));
                }
                return v;
            }
        }

        /// <summary>
        /// Right全体ボリュームの取得(視覚効果向け)
        /// </summary>
        public int getTotalVolumeR()
        {
            lock (lockobj)
            {
                int v = 0;
                for (int i = 0; i < buffer[1].Length; i++)
                {
                    v = Math.Max(v,Math.Abs(buffer[1][i]));
                }
                return v;
            }
        }



        public void setIncFlag()
        {
            lock (lockobj)
            {
                incFlag = true;
            }
        }

        public void resetIncFlag()
        {
            lock (lockobj)
            {
                incFlag = false;
            }
        }

    }
}
