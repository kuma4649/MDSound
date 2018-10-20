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
        private Dictionary<enmInstrumentType, Instrument> dicInst = new Dictionary<enmInstrumentType, Instrument>();

        private int[][] buffer = null;
        private int[][] buff = new int[2][] { new int[1], new int[1] };

        private int[] sn76489Mask = new int[] { 15, 15 };// psgはmuteを基準にしているのでビットが逆です
        private int[] ym2612Mask = new int[] { 0, 0 };
        private int[] ym2203Mask = new int[] { 0, 0 };
        private uint[] segapcmMask = new uint[] { 0, 0 };
        private uint[] c140Mask = new uint[] { 0, 0 };
        private int[] ay8910Mask = new int[] { 0, 0 };
        private int[] huc6280Mask = new int[] { 0, 0 };
        private uint[] nesMask = new uint[] { 0, 0 };

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
            YM3812
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

                sn76489Mask[0] = 15;
                ym2203Mask[0] = 0;
                ym2612Mask[0] = 0;
                segapcmMask[0] = 0;
                c140Mask[0] = 0;
                ay8910Mask[0] = 0;

                sn76489Mask[1] = 15;
                ym2203Mask[1] = 0;
                ym2612Mask[1] = 0;
                segapcmMask[1] = 0;
                c140Mask[1] = 0;
                ay8910Mask[1] = 0;

                incFlag = false;

                if (insts == null) return;

                dicInst.Clear();
                foreach (Chip inst in insts)
                {
                    inst.SamplingRate = inst.Start(inst.ID, inst.SamplingRate, inst.Clock, inst.Option);
                    inst.Reset(inst.ID);

                    if (dicInst.ContainsKey(inst.type)) dicInst.Remove(inst.type);
                    dicInst.Add(inst.type, inst.Instrument);

                    SetupResampler(inst);
                }

            }
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


        public int Update(short[] buf, int offset, int sampleCount, Action frame)
        {
            lock (lockobj)
            {
                int a, b;

                //for (int chipID = 0; chipID < 2; chipID++)
                //{
                //    for (int i = 0; i < 8; i++)
                //    {
                //        rf5c164Vol[chipID][i][0] = 0;
                //        rf5c164Vol[chipID][i][1] = 0;
                //    }
                //}

                for (int i = 0; i < sampleCount ; i+=2)
                {

                    if (frame != null) frame();

                    a = 0;
                    b = 0;

                    //sw.Reset();
                    //sw.Start();

                    buffer[0][0] = 0;
                    buffer[1][0] = 0;
                    ResampleChipStream(insts, buffer, 1);
                    a += buffer[0][0];
                    b += buffer[1][0];

                    //Console.WriteLine(sw.Elapsed);

                    if (incFlag)
                    {
                        a += buf[offset + i + 0];
                        b += buf[offset + i + 1];
                    }

                    Clip(ref a, ref b);

                    buf[offset + i + 0] = (short)a;
                    buf[offset + i + 1] = (short)b;

                    //if (dicInst.ContainsKey(enmInstrumentType.RF5C164))
                    //{
                    //    for (int chipID = 0; chipID < 2; chipID++)
                    //    {
                    //        if (((scd_pcm)(dicInst[enmInstrumentType.RF5C164])).PCM_Chip[chipID] == null) continue;
                    //        for (int ch = 0; ch < 8; ch++)
                    //        {
                    //            rf5c164Vol[chipID][ch][0] = Math.Max(rf5c164Vol[chipID][ch][0]
                    //                , (int)(((scd_pcm)(dicInst[enmInstrumentType.RF5C164])).PCM_Chip[chipID].Channel[ch].Data 
                    //                * ((scd_pcm)(dicInst[enmInstrumentType.RF5C164])).PCM_Chip[chipID].Channel[ch].MUL_L));

                    //            rf5c164Vol[chipID][ch][1] = Math.Max(rf5c164Vol[chipID][ch][1]
                    //                , (int)(((scd_pcm)(dicInst[enmInstrumentType.RF5C164])).PCM_Chip[chipID].Channel[ch].Data 
                    //                * ((scd_pcm)(dicInst[enmInstrumentType.RF5C164])).PCM_Chip[chipID].Channel[ch].MUL_R));
                    //        }
                    //    }
                    //}
                }

                return sampleCount;

            }
        }

        private void Clip(ref int a,ref int b)
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

        private void ResampleChipStream(Chip[] insts, int[][] RetSample, uint Length)
        {
            if (insts == null || insts.Length < 1) return;
            if (Length > tempSample[0].Length)
            {
                tempSample = new int[2][] { new int[Length], new int[Length] };
            }

            Chip inst;
            int[] CurBufL;
            int[] CurBufR;
            int[][] StreamPnt = new int[0x02][] { new int[0x100], new int[0x100] };
            uint InBase;
            uint InPos;
            uint InPosNext;
            uint OutPos;
            uint SmpFrc;  // Sample Friction
            uint InPre=0;
            uint InNow;
            uint InPosL;
            long TempSmpL;
            long TempSmpR;
            int TempS32L;
            int TempS32R;
            int SmpCnt;   // must be signed, else I'm getting calculation errors
            int CurSmpl;
            ulong ChipSmpRate;

            for (int i = 0; i < 0x100; i++)
            {
                StreamBufs[0][i] = 0;
                StreamBufs[1][i] = 0;
            }
            CurBufL = StreamBufs[0x00];
            CurBufR = StreamBufs[0x01];

            // This Do-While-Loop gets and resamples the chip output of one or more chips.
            // It's a loop to support the AY8910 paired with the YM2203/YM2608/YM2610.
            for (int i = 0; i < insts.Length; i++)
            {
                inst = insts[i];
                //double volume = inst.Volume/100.0;
                int mul = (int)(16384.0 * Math.Pow(10.0, inst.Volume / 40.0));

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
                                inst.Update(inst.ID, buff, 1);

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
                            inst.Update(inst.ID, buff, 1);

                            StreamPnt[0][ind] += (short)((Limit(buff[0][0], 0x7fff, -0x8000) * mul) >> 14 );
                            StreamPnt[1][ind] += (short)((Limit(buff[1][0], 0x7fff, -0x8000) * mul) >> 14 );
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
                            inst.Update(inst.ID, buff, 1);

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
                            if (SmpFrc!=0)
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
                            if (SmpFrc!=0)
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

                if (inst.AdditionalUpdate != null)
                {
                    inst.AdditionalUpdate(inst, inst.ID, tempSample, (int)Length);
                }

                for (int j = 0; j < Length; j++)
                {
                    RetSample[0][j] += tempSample[0][j];
                    RetSample[1][j] += tempSample[1][j];
                }

            }

            return;
        }




        public void WriteAY8910(byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.AY8910)) return;

                ((ay8910)(dicInst[enmInstrumentType.AY8910])).Write(ChipID, 0, Adr, Data);
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
                ay8910Mask[chipID] |= ch;
                if (!dicInst.ContainsKey(enmInstrumentType.AY8910)) return;
                ((ay8910)(dicInst[enmInstrumentType.AY8910])).AY8910_SetMute((byte)chipID, ay8910Mask[chipID]);
            }
        }

        public void resetAY8910Mask(int chipID, int ch)
        {
            lock (lockobj)
            {
                ay8910Mask[chipID] &= ~ch;
                if (!dicInst.ContainsKey(enmInstrumentType.AY8910)) return;
                ((ay8910)(dicInst[enmInstrumentType.AY8910])).AY8910_SetMute((byte)chipID, ay8910Mask[chipID]);
            }
        }

        public int[][][] getAY8910VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.AY8910)) return null;
            return ((ay8910)dicInst[enmInstrumentType.AY8910]).visVolume;
        }


        public void WriteSN76489(byte ChipID, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.SN76489)) return;

                dicInst[enmInstrumentType.SN76489].Write(ChipID, 0, 0, Data);
            }
        }

        public void WriteSN76489GGPanning(byte ChipID, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.SN76489)) return;

                ((sn76489)(dicInst[enmInstrumentType.SN76489])).SN76489_GGStereoWrite(ChipID, Data);
            }
        }

        public void WriteYM2612(byte ChipID, byte Port, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2612)) return;

                dicInst[enmInstrumentType.YM2612].Write(ChipID, 0, (byte)(0 + (Port & 1) * 2), Adr);
                dicInst[enmInstrumentType.YM2612].Write(ChipID, 0, (byte)(1 + (Port & 1) * 2), Data);
            }
        }

        public void WriteYM3438(byte ChipID, byte Port, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM3438)) return;

                dicInst[enmInstrumentType.YM3438].Write(ChipID, 0, (byte)(0 + (Port & 1) * 2), Adr);
                dicInst[enmInstrumentType.YM3438].Write(ChipID, 0, (byte)(1 + (Port & 1) * 2), Data);
            }
        }

        public void WritePWM(byte ChipID, byte Adr, uint Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.PWM)) return;

                dicInst[enmInstrumentType.PWM].Write(ChipID, 0, Adr, (int)Data);
                // (byte)((adr & 0xf0)>>4),(uint)((adr & 0xf)*0x100+data));
            }
        }

        public void WriteRF5C164(byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.RF5C164)) return;

                dicInst[enmInstrumentType.RF5C164].Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteRF5C164PCMData(byte ChipID, uint RAMStartAdr, uint RAMDataLength, byte[] SrcData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.RF5C164)) return;

                ((scd_pcm)(dicInst[enmInstrumentType.RF5C164])).rf5c164_write_ram2(ChipID, RAMStartAdr, RAMDataLength, SrcData, SrcStartAdr);
            }
        }

        public void WriteRF5C164MemW(byte ChipID, uint Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.RF5C164)) return;

                ((scd_pcm)(dicInst[enmInstrumentType.RF5C164])).rf5c164_mem_w(ChipID, Adr, Data);
            }
        }

        public void WriteRF5C68(byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.RF5C68)) return;

                dicInst[enmInstrumentType.RF5C68].Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteRF5C68PCMData(byte ChipID, uint RAMStartAdr, uint RAMDataLength, byte[] SrcData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.RF5C68)) return;

                ((rf5c68)(dicInst[enmInstrumentType.RF5C68])).rf5c68_write_ram2(ChipID, (int)RAMStartAdr, (int)RAMDataLength, SrcData, SrcStartAdr);
            }
        }

        public void WriteRF5C68MemW(byte ChipID, uint Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.RF5C68)) return;

                ((rf5c68)(dicInst[enmInstrumentType.RF5C68])).rf5c68_mem_w(ChipID, (int)Adr, Data);
            }
        }


        public void WriteC140(byte ChipID, uint Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.C140)) return;

                dicInst[enmInstrumentType.C140].Write(ChipID, 0, (int)Adr, Data);
            }
        }

        public void WriteC140PCMData(byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.C140)) return;

                ((c140)(dicInst[enmInstrumentType.C140])).c140_write_rom2(ChipID, ROMSize, DataStart, DataLength, ROMData, SrcStartAdr);
            }
        }

        public void WriteYM3812(int ChipID, int rAdr, int rDat)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM3812)) return;

                ((ym3812)(dicInst[enmInstrumentType.YM3812])).Write((byte)ChipID, 0, rAdr, rDat);
            }
        }

        public void WriteC352(byte ChipID, uint Adr, uint Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.C352)) return;

                dicInst[enmInstrumentType.C352].Write(ChipID, 0, (int)Adr, (ushort)Data);
            }
        }

        public void WriteC352PCMData(byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.C352)) return;

                ((c352)(dicInst[enmInstrumentType.C352])).c352_write_rom2(ChipID, ROMSize, (int)DataStart, (int)DataLength, ROMData, SrcStartAdr);
            }
        }

        public void WriteYMF271PCMData(byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YMF271)) return;

                ((ymf271)(dicInst[enmInstrumentType.YMF271])).ymf271_write_rom(ChipID, ROMSize, DataStart, DataLength, ROMData, (int)SrcStartAdr);
            }
        }

        public void WriteYMF278BPCMData(byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YMF278B)) return;

                ((ymf278b)(dicInst[enmInstrumentType.YMF278B])).ymf278b_write_rom(ChipID, (int)ROMSize, (int)DataStart, (int)DataLength, ROMData, (int)SrcStartAdr);
            }
        }

        public void WriteYMZ280BPCMData(byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YMZ280B)) return;

                ((ymz280b)(dicInst[enmInstrumentType.YMZ280B])).ymz280b_write_rom(ChipID, (int)ROMSize, (int)DataStart, (int)DataLength, ROMData, (int)SrcStartAdr);
            }
        }

        public void WriteY8950PCMData(byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.Y8950)) return;

                ((y8950)(dicInst[enmInstrumentType.Y8950])).y8950_write_data_pcmrom(ChipID, (int)ROMSize, (int)DataStart, (int)DataLength, ROMData, (int)SrcStartAdr);
            }
        }

        public void WriteOKIM6258(byte ChipID, byte Port, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.OKIM6258)) return;

                ((okim6258)(dicInst[enmInstrumentType.OKIM6258])).Write(ChipID, 0, Port, Data);
            }
        }

        public void WriteOKIM6295(byte ChipID, byte Port, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.OKIM6295)) return;

                ((okim6295)(dicInst[enmInstrumentType.OKIM6295])).Write(ChipID, 0, Port, Data);
            }
        }

        public void WriteOKIM6295PCMData(byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.OKIM6295)) return;

                ((okim6295)(dicInst[enmInstrumentType.OKIM6295])).okim6295_write_rom2(ChipID, (int)ROMSize, (int)DataStart, (int)DataLength, ROMData, SrcStartAdr);
            }
        }

        public void WriteSEGAPCM(byte ChipID, int Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.SEGAPCM)) return;

                ((segapcm)(dicInst[enmInstrumentType.SEGAPCM])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteSEGAPCMPCMData(byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.SEGAPCM)) return;

                ((segapcm)(dicInst[enmInstrumentType.SEGAPCM])).sega_pcm_write_rom2(ChipID, (int)ROMSize, (int)DataStart, (int)DataLength, ROMData, SrcStartAdr);
            }
        }

        public void WriteYM2151(byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2151)) return;

                ((dicInst[enmInstrumentType.YM2151])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteYM2151mame(byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2151mame)) return;

                ((dicInst[enmInstrumentType.YM2151mame])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteYM2151x68sound(byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2151x68sound)) return;

                ((dicInst[enmInstrumentType.YM2151x68sound])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteYM2203(byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2203)) return;

                ((ym2203)(dicInst[enmInstrumentType.YM2203])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteYM2608(byte ChipID, byte Port, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2608)) return;

                ((ym2608)(dicInst[enmInstrumentType.YM2608])).Write(ChipID, 0, (Port * 0x100 + Adr), Data);
            }
        }

        public void WriteYM2609(byte ChipID, byte Port, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2609)) return;

                ((ym2609)(dicInst[enmInstrumentType.YM2609])).Write(ChipID, 0, (Port * 0x100 + Adr), Data);
            }
        }

        public void WriteYM2610(byte ChipID, byte Port, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2610)) return;

                ((ym2610)(dicInst[enmInstrumentType.YM2610])).Write(ChipID,0, (Port * 0x100 + Adr), Data);
            }
        }

        public void WriteYM2610_SetAdpcmA(byte ChipID, byte[] Buf)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2610)) return;

                ((ym2610)(dicInst[enmInstrumentType.YM2610])).YM2610_setAdpcmA(ChipID, Buf, Buf.Length);
            }
        }

        public void WriteYM2610_SetAdpcmB(byte ChipID, byte[] Buf)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2610)) return;

                ((ym2610)(dicInst[enmInstrumentType.YM2610])).YM2610_setAdpcmB(ChipID, Buf, Buf.Length);
            }
        }

        public void WriteYMF262(byte ChipID, byte Port, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YMF262)) return;

                ((ymf262)(dicInst[enmInstrumentType.YMF262])).Write(ChipID,0, (Port * 0x100 + Adr), Data);
            }
        }

        public void WriteYMF271(byte ChipID, byte Port, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YMF271)) return;

                ((ymf271)(dicInst[enmInstrumentType.YMF271])).Write(ChipID, Port, Adr, Data);
            }
        }

        public void WriteYMF278B(byte ChipID, byte Port, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YMF278B)) return;
                ((ymf278b)(dicInst[enmInstrumentType.YMF278B])).Write(ChipID, Port, Adr, Data);
            }
        }

        public void WriteY8950(byte ChipID,byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.Y8950)) return;

                ((y8950)(dicInst[enmInstrumentType.Y8950])).Write(ChipID,0, Adr, Data);
            }
        }

        public void WriteYMZ280B(byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YMZ280B)) return;

                ((ymz280b)(dicInst[enmInstrumentType.YMZ280B])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteHuC6280(byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.HuC6280)) return;

                ((Ootake_PSG)(dicInst[enmInstrumentType.HuC6280])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteK053260(byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.K053260)) return;

                ((K053260)(dicInst[enmInstrumentType.K053260])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteGA20(byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.GA20)) return;

                ((iremga20)(dicInst[enmInstrumentType.GA20])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteYM2413(byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2413)) return;

                ((ym2413)(dicInst[enmInstrumentType.YM2413])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteK051649(byte ChipID, int Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.K051649)) return;

                ((K051649)(dicInst[enmInstrumentType.K051649])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteK054539(byte ChipID, int Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.K054539)) return;

                ((K054539)(dicInst[enmInstrumentType.K054539])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteK053260PCMData(byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.K053260)) return;

                ((K053260)(dicInst[enmInstrumentType.K053260])).k053260_write_rom(ChipID, (int)ROMSize, (int)DataStart, (int)DataLength, ROMData, (int)SrcStartAdr);
            }
        }

        public void WriteK054539PCMData(byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.K054539)) return;

                ((K054539)(dicInst[enmInstrumentType.K054539])).k054539_write_rom2(ChipID, (int)ROMSize, (int)DataStart, (int)DataLength, ROMData, (int)SrcStartAdr);
            }
        }

        public void WriteMultiPCMPCMData(byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.MultiPCM)) return;

                ((multipcm)(dicInst[enmInstrumentType.MultiPCM])).multipcm_write_rom2(ChipID, (int)ROMSize, (int)DataStart, (int)DataLength, ROMData, (int)SrcStartAdr);
            }
        }

        public void WriteQSoundPCMData(byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.QSound)) return;

                ((qsound)(dicInst[enmInstrumentType.QSound])).qsound_write_rom(ChipID, (int)ROMSize, (int)DataStart, (int)DataLength, ROMData, (int)SrcStartAdr);
            }
        }

        public void WriteGA20PCMData(byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.GA20)) return;

                ((iremga20)(dicInst[enmInstrumentType.GA20])).iremga20_write_rom(ChipID, (int)ROMSize, (int)DataStart, (int)DataLength, ROMData, (int)SrcStartAdr);
            }
        }

        public void WriteDMG(byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.DMG)) return;

                ((gb)(dicInst[enmInstrumentType.DMG])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteNES(byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.Nes)) return;

                ((nes_intf)(dicInst[enmInstrumentType.Nes])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteMultiPCM(byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.MultiPCM)) return;

                ((multipcm)(dicInst[enmInstrumentType.MultiPCM])).Write(ChipID, 0, Adr, Data);
            }
        }

        public void WriteMultiPCMSetBank(byte ChipID, byte Ch, int Adr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.MultiPCM)) return;

                ((multipcm)(dicInst[enmInstrumentType.MultiPCM])).multipcm_bank_write(ChipID, Ch, (UInt16)Adr);
            }
        }

        public void WriteQSound(byte ChipID, Int32 adr, byte dat)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.QSound)) return;

                ((qsound)(dicInst[enmInstrumentType.QSound])).qsound_w(ChipID, adr, dat);
            }
        }

        public void WriteNESRam(byte ChipID, Int32 DataStart, Int32 DataLength,byte[] RAMData,Int32 RAMDataStartAdr)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.Nes)) return;

                ((nes_intf)(dicInst[enmInstrumentType.Nes])).nes_write_ram(ChipID, DataStart, DataLength, RAMData, RAMDataStartAdr);
            }
        }

        public byte[] ReadNES(byte ChipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.Nes)) return null;

                return ((nes_intf)(dicInst[enmInstrumentType.Nes])).nes_r(ChipID);
            }
        }

        public np.np_nes_fds.NES_FDS ReadFDS(byte ChipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.Nes)) return null;

                return ((nes_intf)(dicInst[enmInstrumentType.Nes])).nes_r_fds(ChipID);
            }
        }




        public void SetVolumeYM2151(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2151)) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YM2151) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeYM2151mame(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2151mame)) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YM2151mame) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeYM2151x68sound(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2151x68sound)) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YM2151x68sound) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeYM2203(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2203)) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YM2203) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeYM2203FM(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2203)) return;

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

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YM2413) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void setVolumeHuC6280(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.HuC6280)) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.HuC6280) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeYM2608(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2608)) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YM2608) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeYM2608FM(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2608)) return;

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

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YM2610) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeYM2610FM(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2610)) return;

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

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YM2612) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeYM3438(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM3438)) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YM3438) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeSN76489(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.SN76489)) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.SN76489) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeRF5C164(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.RF5C164)) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.RF5C164) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumePWM(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.PWM)) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.PWM) continue;
                c.Volume = Math.Max(Math.Min(vol, 20),-192);
            }
        }

        public void SetVolumeOKIM6258(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.OKIM6258)) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.OKIM6258) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeMpcmX68k(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.mpcmX68k)) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.mpcmX68k) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeOKIM6295(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.OKIM6295)) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.OKIM6295) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeC140(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.C140)) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.C140) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeC352(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.C352)) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.C352) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetRearMute(byte flag)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.C352)) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.C352) continue;
                ((c352)dicInst[enmInstrumentType.C352]).c352_set_options(flag);
            }
        }

        public void SetVolumeK051649(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.K051649)) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.K051649) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeK054539(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.K054539)) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.K054539) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeQSound(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.QSound)) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.QSound) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeDMG(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.DMG)) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.DMG) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeGA20(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.GA20)) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.GA20) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeYMZ280B(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YMZ280B)) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YMZ280B) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeYMF271(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YMF271)) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YMF271) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeYMF262(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YMF262)) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YMF262) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeYMF278B(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YMF278B)) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.YMF278B) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeMultiPCM(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.MultiPCM)) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.MultiPCM) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeSegaPCM(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.SEGAPCM)) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.SEGAPCM) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeNES(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.Nes)) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.Nes) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeDMC(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.DMC)) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.DMC) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeFDS(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.FDS)) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.FDS) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeMMC5(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.MMC5)) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.MMC5) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeN160(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.N160)) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.N160) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeVRC6(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.VRC6)) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.VRC6) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeVRC7(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.VRC7)) return;

            foreach (Chip c in insts)
            {
                if (c.type != enmInstrumentType.VRC7) continue;
                c.Volume = Math.Max(Math.Min(vol, 20), -192);
            }
        }

        public void SetVolumeFME7(int vol)
        {
            if (!dicInst.ContainsKey(enmInstrumentType.FME7)) return;

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
                return ((sn76489)(dicInst[enmInstrumentType.SN76489])).SN76489_Chip[0].Registers;
            }
        }

        public int[][] ReadYM2612Register(byte chipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.YM2612)) return null;
                return ((ym2612)(dicInst[enmInstrumentType.YM2612])).YM2612_Chip[chipID].REG;
            }
        }

        public scd_pcm.pcm_chip_ ReadRf5c164Register(int chipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.RF5C164)) return null;
                if (((scd_pcm)(dicInst[enmInstrumentType.RF5C164])).PCM_Chip == null || ((scd_pcm)(dicInst[enmInstrumentType.RF5C164])).PCM_Chip.Length < 1) return null;
                return ((scd_pcm)(dicInst[enmInstrumentType.RF5C164])).PCM_Chip[chipID];
            }
        }

        public c140.c140_state ReadC140Register(int cur)
        {
            lock(lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.C140)) return null;
                return ((c140)dicInst[enmInstrumentType.C140]).C140Data[cur];
            }
        }

        public okim6258.okim6258_state ReadOKIM6258Status(int chipID)
        {
            lock(lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.OKIM6258)) return null;
                return okim6258.OKIM6258Data[chipID];
            }
        }

        public okim6295.okim6295_state ReadOKIM6295Status(int chipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.OKIM6295)) return null;
                return okim6295.OKIM6295Data[chipID];
            }
        }

        public segapcm.segapcm_state ReadSegaPCMStatus(int chipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.SEGAPCM)) return null;
                return ((segapcm)dicInst[enmInstrumentType.SEGAPCM]).SPCMData[chipID];
            }
        }

        public Ootake_PSG.huc6280_state ReadHuC6280Status(int chipID)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.HuC6280)) return null;
                return ((Ootake_PSG)dicInst[enmInstrumentType.HuC6280]).GetState((byte)chipID);
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
                int[] keys = new int[((ym2612)(dicInst[enmInstrumentType.YM2612])).YM2612_Chip[chipID].CHANNEL.Length];
                for (int i = 0; i < keys.Length; i++)
                    keys[i] = ((ym2612)(dicInst[enmInstrumentType.YM2612])).YM2612_Chip[chipID].CHANNEL[i].KeyOn;
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




        public void setSN76489Mask(int chipID,int ch)
        {
            lock (lockobj)
            {
                sn76489Mask[chipID] &= ~ch;
                if (!dicInst.ContainsKey(enmInstrumentType.SN76489)) return;
                ((sn76489)(dicInst[enmInstrumentType.SN76489])).SN76489_SetMute((byte)chipID, sn76489Mask[chipID]);
            }
        }

        public void setYM2612Mask(int chipID,int ch)
        {
            lock (lockobj)
            {
                ym2612Mask[chipID] |= ch;
                if (!dicInst.ContainsKey(enmInstrumentType.YM2612)) return;
                ((ym2612)(dicInst[enmInstrumentType.YM2612])).YM2612_SetMute((byte)chipID, ym2612Mask[chipID]);
            }
        }

        public void setYM2203Mask(int chipID, int ch)
        {
            lock (lockobj)
            {
                ym2203Mask[chipID] |= ch;
                if (!dicInst.ContainsKey(enmInstrumentType.YM2203)) return;
                ((ym2203)(dicInst[enmInstrumentType.YM2203])).YM2203_SetMute((byte)chipID, ym2203Mask[chipID]);
            }
        }

        public void setRf5c164Mask(int chipID,int ch)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.RF5C164)) return;
                ((scd_pcm)(dicInst[enmInstrumentType.RF5C164])).PCM_Chip[chipID].Channel[ch].Muted = 1;
            }
        }

        public void setC140Mask(int chipID, int ch)
        {
            lock (lockobj)
            {
                c140Mask[chipID] |= (uint)ch;
                if (!dicInst.ContainsKey(enmInstrumentType.C140)) return;
                ((c140)(dicInst[enmInstrumentType.C140])).c140_set_mute_mask((byte)chipID, c140Mask[chipID]);
            }
        }

        public void setSegaPcmMask(int chipID, int ch)
        {
            lock (lockobj)
            {
                segapcmMask[chipID] |= (uint)ch;
                if (!dicInst.ContainsKey(enmInstrumentType.SEGAPCM)) return;
                ((segapcm)(dicInst[enmInstrumentType.SEGAPCM])).segapcm_set_mute_mask((byte)chipID, segapcmMask[chipID]);
            }
        }

        public void setHuC6280Mask(int chipID, int ch)
        {
            lock (lockobj)
            {
                huc6280Mask[chipID] |= ch;
                if (!dicInst.ContainsKey(enmInstrumentType.HuC6280)) return;
                ((Ootake_PSG)(dicInst[enmInstrumentType.HuC6280])).HuC6280_SetMute((byte)chipID, huc6280Mask[chipID]);
            }
        }

        public void setNESMask(int chipID, int ch)
        {
            lock (lockobj)
            {
                nesMask[chipID] |= (uint)(0x1 << ch);
                if (!dicInst.ContainsKey(enmInstrumentType.Nes)) return;
                ((nes_intf)(dicInst[enmInstrumentType.Nes])).nes_set_mute_mask((byte)chipID, nesMask[chipID]);
            }
        }

        public void setFDSMask(int chipID)
        {
            lock (lockobj)
            {
                nesMask[chipID] |= 0x20;
                if (!dicInst.ContainsKey(enmInstrumentType.Nes)) return;
                ((nes_intf)(dicInst[enmInstrumentType.Nes])).nes_set_mute_mask((byte)chipID, nesMask[chipID]);
            }
        }



        public void resetSN76489Mask(int chipID, int ch)
        {
            lock (lockobj)
            {
                sn76489Mask[chipID] |= ch;
                if (!dicInst.ContainsKey(enmInstrumentType.SN76489)) return;
                ((sn76489)(dicInst[enmInstrumentType.SN76489])).SN76489_SetMute((byte)chipID, sn76489Mask[chipID]);
            }
        }

        public void resetYM2612Mask(int chipID, int ch)
        {
            lock (lockobj)
            {
                ym2612Mask[chipID] &= ~ch;
                if (!dicInst.ContainsKey(enmInstrumentType.YM2612)) return;
                ((ym2612)(dicInst[enmInstrumentType.YM2612])).YM2612_SetMute((byte)chipID, ym2612Mask[chipID]);
            }
        }

        public void resetYM2203Mask(int chipID, int ch)
        {
            lock (lockobj)
            {
                ym2203Mask[chipID] &= ~ch;
                if (!dicInst.ContainsKey(enmInstrumentType.YM2203)) return;
                ((ym2203)(dicInst[enmInstrumentType.YM2203])).YM2203_SetMute((byte)chipID, ym2203Mask[chipID]);
            }
        }

        public void resetRf5c164Mask(int chipID, int ch)
        {
            lock (lockobj)
            {
                if (!dicInst.ContainsKey(enmInstrumentType.RF5C164)) return;
                ((scd_pcm)(dicInst[enmInstrumentType.RF5C164])).PCM_Chip[chipID].Channel[ch].Muted = 0;
            }
        }

        public void resetC140Mask(int chipID, int ch)
        {
            lock (lockobj)
            {
                c140Mask[chipID] &= ~(uint)ch;
                if (!dicInst.ContainsKey(enmInstrumentType.C140)) return;
                ((c140)(dicInst[enmInstrumentType.C140])).c140_set_mute_mask((byte)chipID, c140Mask[chipID]);
            }
        }

        public void resetSegaPcmMask(int chipID, int ch)
        {
            lock (lockobj)
            {
                segapcmMask[chipID] &= ~(uint)ch;
                if (!dicInst.ContainsKey(enmInstrumentType.SEGAPCM)) return;
                 ((segapcm)(dicInst[enmInstrumentType.SEGAPCM])).segapcm_set_mute_mask((byte)chipID, segapcmMask[chipID]);
            }
        }

        public void resetHuC6280Mask(int chipID, int ch)
        {
            lock (lockobj)
            {
                huc6280Mask[chipID] &= ~ch;
                if (!dicInst.ContainsKey(enmInstrumentType.HuC6280)) return;
                ((Ootake_PSG)(dicInst[enmInstrumentType.HuC6280])).HuC6280_SetMute((byte)chipID, huc6280Mask[chipID]);
            }
        }

        public void resetNESMask(int chipID, int ch)
        {
            lock (lockobj)
            {
                nesMask[chipID] &= (uint)~(0x1 << ch);
                if (!dicInst.ContainsKey(enmInstrumentType.Nes)) return;
                ((nes_intf)(dicInst[enmInstrumentType.Nes])).nes_set_mute_mask((byte)chipID, nesMask[chipID]);
            }
        }

        public void resetFDSMask(int chipID)
        {
            lock (lockobj)
            {
                nesMask[chipID] &= ~(uint)0x20;
                if (!dicInst.ContainsKey(enmInstrumentType.Nes)) return;
                ((nes_intf)(dicInst[enmInstrumentType.Nes])).nes_set_mute_mask((byte)chipID, nesMask[chipID]);
            }
        }



        public int[][][] getYM2151VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2151)) return null;
            return (dicInst[enmInstrumentType.YM2151]).visVolume;
        }

        public int[][][] getYM2203VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2203)) return null;
            return  ((ym2203)dicInst[enmInstrumentType.YM2203]).visVolume;
        }

        public int[][][] getYM2413VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2413)) return null;
            return ((ym2413)dicInst[enmInstrumentType.YM2413]).visVolume;
        }

        public int[][][] getYM2608VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2608)) return null;
            return ((ym2608)dicInst[enmInstrumentType.YM2608]).visVolume;
        }

        public int[][][] getYM2609VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2609)) return null;
            return ((ym2608)dicInst[enmInstrumentType.YM2609]).visVolume;
        }

        public int[][][] getYM2610VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2610)) return null;
            return ((ym2610)dicInst[enmInstrumentType.YM2610]).visVolume;
        }

        public int[][][] getYM2612VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.YM2612)) return null;
            return ((ym2612)dicInst[enmInstrumentType.YM2612]).visVolume;
        }

        public int[][][] getSN76489VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.SN76489)) return null;
            return ((sn76489)dicInst[enmInstrumentType.SN76489]).visVolume;
        }

        public int[][][] getHuC6280VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.HuC6280)) return null;
            return ((Ootake_PSG)dicInst[enmInstrumentType.HuC6280]).visVolume;
        }

        public int[][][] getRF5C164VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.RF5C164)) return null;
            return ((scd_pcm)dicInst[enmInstrumentType.RF5C164]).visVolume;
        }

        public int[][][] getPWMVisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.PWM)) return null;
            return ((pwm)dicInst[enmInstrumentType.PWM]).visVolume;
        }

        public int[][][] getOKIM6258VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.OKIM6258)) return null;
            return ((okim6258)dicInst[enmInstrumentType.OKIM6258]).visVolume;
        }

        public int[][][] getOKIM6295VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.OKIM6295)) return null;
            return ((okim6295)dicInst[enmInstrumentType.OKIM6295]).visVolume;
        }

        public int[][][] getC140VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.C140)) return null;
            return ((c140)dicInst[enmInstrumentType.C140]).visVolume;
        }

        public int[][][] getSegaPCMVisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.SEGAPCM)) return null;
            return ((segapcm)dicInst[enmInstrumentType.SEGAPCM]).visVolume;
        }

        public int[][][] getC352VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.C352)) return null;
            return ((c352)dicInst[enmInstrumentType.C352]).visVolume;
        }

        public int[][][] getK051649VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.K051649)) return null;
            return ((K051649)dicInst[enmInstrumentType.K051649]).visVolume;
        }

        public int[][][] getK054539VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.K054539)) return null;
            return ((K054539)dicInst[enmInstrumentType.K054539]).visVolume;
        }

        public int[][][] getNESVisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.Nes)) return null;
            return null;
        }

        public int[][][] getDMCVisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.DMC)) return null;
            return null;
        }

        public int[][][] getFDSVisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.FDS)) return null;
            return null;
        }

        public int[][][] getMMC5VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.MMC5)) return null;
            return null;
        }

        public int[][][] getN160VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.N160)) return null;
            return null;
        }

        public int[][][] getVRC6VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.VRC6)) return null;
            return null;
        }

        public int[][][] getVRC7VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.VRC7)) return null;
            return null;
        }

        public int[][][] getFME7VisVolume()
        {
            if (!dicInst.ContainsKey(enmInstrumentType.FME7)) return null;
            return null;
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
