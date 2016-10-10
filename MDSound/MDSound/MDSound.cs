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
        private Instrument iSN76489 = null;
        private Instrument iYM2612 = null;
        private Instrument iRF5C164 = null;
        private Instrument iPWM = null;
        private Instrument iC140 = null;
        private Instrument iOKIM6258 = null;
        private Instrument iOKIM6295 = null;
        private Instrument iSEGAPCM = null;
        private Instrument iYM2151 = null;
        private Instrument iYM2203 = null;
        private Instrument iYM2608 = null;
        private Instrument iYM2610 = null;

        private int[][] buffer = null;
        private int[][] buffer2 = null;
        private int[][] buff = new int[2][] { new int[1], new int[1] };

        private int sn76489Mask = 15;// psgはmuteを基準にしているのでビットが逆です
        private int ym2612Mask = 0;
        private uint segapcmMask = 0;

        private int[][] ym2612Vol = new int[6][] { new int[2], new int[2], new int[2], new int[2], new int[2], new int[2] };
        private int[] ym2612Ch3SlotVol = new int[4];
        private int[][] sn76489Vol = new int[4][] { new int[2], new int[2], new int[2], new int[2] };
        private int[][] rf5c164Vol = new int[8][] { new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2] };

        private int[] ym2612Key = new int[6];
        private int[] ym2151Key = new int[8];
        private int[] ym2203Key = new int[6];
        private int[] ym2608Key = new int[11];
        private int[] ym2610Key = new int[11];

        private bool incFlag = false;
        private object lockobj = new object();
        private byte ResampleMode=0;

        private const uint FIXPNT_BITS = 11;
        private const uint FIXPNT_FACT = (1 << (int)FIXPNT_BITS);
        private const uint FIXPNT_MASK = (FIXPNT_FACT - 1);

        private uint getfriction(uint x) { return ((x) & FIXPNT_MASK); }
        private uint getnfriction(uint x) { return ((FIXPNT_FACT - (x)) & FIXPNT_MASK); }
        private uint fpi_floor(uint x) { return (uint)((x) & ~FIXPNT_MASK); }
        private uint fpi_ceil(uint x) { return (uint)((x + FIXPNT_MASK) & ~FIXPNT_MASK); }
        private uint fp2i_floor(uint x) { return ((x) / FIXPNT_FACT); }
        private uint fp2i_ceil(uint x) { return ((x + FIXPNT_MASK) / FIXPNT_FACT); }


        public enum enmInstrumentType : int
        {
            None=0,
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
            YM2610
        }

        public class Chip
        {
            public delegate void dlgUpdate(byte ChipID, int[][] Buffer, int Length);
            public delegate uint dlgStart(byte ChipID, uint SamplingRate, uint FMClockValue, params object[] Option);
            public delegate void dlgStop(byte ChipID);
            public delegate void dlgReset(byte ChipID);

            public Instrument Instrument = null;
            public dlgUpdate Update = null;
            public dlgStart Start = null;
            public dlgStop Stop = null;
            public dlgReset Reset = null;

            public enmInstrumentType type = enmInstrumentType.None;
            public byte ID = 0;
            public uint SamplingRate = 0;
            public uint Clock = 0;
            public uint Volume = 0;

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

                buffer = new int[2][] { new int[SamplingBuffer], new int[SamplingBuffer] };
                buffer2 = new int[2][] { new int[1], new int[1] };
                StreamBufs = new int[2][] { new int[0x100], new int[0x100] };

                sn76489Mask = 15;
                ym2612Mask = 0;
                segapcmMask = 0;

                incFlag = false;

                if (insts == null) return;

                foreach (Chip inst in insts)
                {
                    inst.SamplingRate = inst.Start(inst.ID, inst.SamplingRate, inst.Clock, inst.Option);
                    inst.Reset(inst.ID);

                    switch (inst.type)
                    {
                        case enmInstrumentType.SN76489:
                            iSN76489 = inst.Instrument;
                            break;
                        case enmInstrumentType.YM2612:
                            iYM2612 = inst.Instrument;
                            break;
                        case enmInstrumentType.RF5C164:
                            iRF5C164 = inst.Instrument;
                            break;
                        case enmInstrumentType.PWM:
                            iPWM = inst.Instrument;
                            break;
                        case enmInstrumentType.C140:
                            iC140 = inst.Instrument;
                            break;
                        case enmInstrumentType.OKIM6258:
                            iOKIM6258 = inst.Instrument;
                            break;
                        case enmInstrumentType.OKIM6295:
                            iOKIM6295 = inst.Instrument;
                            break;
                        case enmInstrumentType.SEGAPCM:
                            iSEGAPCM = inst.Instrument;
                            break;
                        case enmInstrumentType.YM2151:
                            iYM2151 = inst.Instrument;
                            break;
                        case enmInstrumentType.YM2203:
                            iYM2203 = inst.Instrument;
                            break;
                        case enmInstrumentType.YM2608:
                            iYM2608 = inst.Instrument;
                            break;
                        case enmInstrumentType.YM2610:
                            iYM2610 = inst.Instrument;
                            break;
                    }

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

                for (int i = 0; i < 6; i++)
                {
                    ym2612Vol[i][0] = 0;
                    ym2612Vol[i][1] = 0;
                }
                for (int i = 0; i < 4; i++)
                {
                    ym2612Ch3SlotVol[i] = 0;
                }
                for (int i = 0; i < 4; i++)
                {
                    sn76489Vol[i][0] = 0;
                    sn76489Vol[i][1] = 0;
                }
                for (int i = 0; i < 8; i++)
                {
                    rf5c164Vol[i][0] = 0;
                    rf5c164Vol[i][1] = 0;
                }

                for (int i = 0; i < sampleCount / 2; i++)
                {

                    if (frame != null) frame();

                    a = 0;
                    b = 0;

                    buffer2[0][0] = 0;
                    buffer2[1][0] = 0;
                    ResampleChipStream(insts, buffer2, 1);
                    a += buffer2[0][0];
                    b += buffer2[1][0];


                    if (incFlag)
                    {
                        buf[offset + i * 2 + 0] += (short)Math.Max(Math.Min(a, short.MaxValue), short.MinValue);
                        buf[offset + i * 2 + 1] += (short)Math.Max(Math.Min(b, short.MaxValue), short.MinValue);
                    }
                    else
                    {
                        buf[offset + i * 2 + 0] = (short)Math.Max(Math.Min(a, short.MaxValue), short.MinValue);
                        buf[offset + i * 2 + 1] = (short)Math.Max(Math.Min(b, short.MaxValue), short.MinValue);
                    }

                    if (iYM2612 != null)
                    {
                        for (int ch = 0; ch < 6; ch++)
                        {
                            ym2612Vol[ch][0] = Math.Max(ym2612Vol[ch][0], ((ym2612)(iYM2612)).YM2612_Chip[0].CHANNEL[ch].fmVol[0]);
                            ym2612Vol[ch][1] = Math.Max(ym2612Vol[ch][1], ((ym2612)(iYM2612)).YM2612_Chip[0].CHANNEL[ch].fmVol[1]);
                        }

                        for (int slot = 0; slot < 4; slot++)
                        {
                            ym2612Ch3SlotVol[slot] = Math.Max(ym2612Ch3SlotVol[slot], ((ym2612)(iYM2612)).YM2612_Chip[0].CHANNEL[2].fmSlotVol[slot]);
                        }
                    }

                    if (iSN76489 != null)
                    {
                        for (int ch = 0; ch < 4; ch++)
                        {
                            sn76489Vol[ch][0] = Math.Max(sn76489Vol[ch][0], ((sn76489)(iSN76489)).SN76489_Chip[0].volume[ch][0]);
                            sn76489Vol[ch][1] = Math.Max(sn76489Vol[ch][1], ((sn76489)(iSN76489)).SN76489_Chip[0].volume[ch][0]);
                        }
                    }

                    if (iRF5C164 != null)
                    {
                        for (int ch = 0; ch < 8; ch++)
                        {
                            rf5c164Vol[ch][0] = Math.Max(rf5c164Vol[ch][0], (int)(((scd_pcm)(iRF5C164)).PCM_Chip[0].Channel[ch].Data * ((scd_pcm)(iRF5C164)).PCM_Chip[0].Channel[ch].MUL_L));
                            rf5c164Vol[ch][1] = Math.Max(rf5c164Vol[ch][1], (int)(((scd_pcm)(iRF5C164)).PCM_Chip[0].Channel[ch].Data * ((scd_pcm)(iRF5C164)).PCM_Chip[0].Channel[ch].MUL_R));
                        }
                    }
                }

                return sampleCount;

            }
        }

        private void ResampleChipStream(Chip[] insts, int[][] RetSample, uint Length)
        {
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
                double volume = inst.Volume/100.0;

                switch (inst.Resampler)
                {
                    case 0x00:  // old, but very fast resampler
                        inst.SmpLast = inst.SmpNext;
                        inst.SmpP += Length;
                        inst.SmpNext = (uint)((ulong)inst.SmpP * inst.SamplingRate / SamplingRate);
                        if (inst.SmpLast >= inst.SmpNext)
                        {
                            RetSample[0][0] += (int)(inst.LSmpl[0] * volume);
                            RetSample[1][0] += (int)(inst.LSmpl[1] * volume);
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
                                StreamBufs[0][ind] += (int)(buff[0][0] * volume);
                                StreamBufs[1][ind] += (int)(buff[1][0] * volume);
                            }

                            if (SmpCnt == 1)
                            {
                                RetSample[0][0] += (int)(CurBufL[0x00] * volume);
                                RetSample[1][0] += (int)(CurBufR[0x00] * volume);
                                inst.LSmpl[0] = CurBufL[0x00];
                                inst.LSmpl[1] = CurBufR[0x00];
                            }
                            else if (SmpCnt == 2)
                            {
                                RetSample[0][0] += (int)((int)((CurBufL[0x00] + CurBufL[0x01]) * volume) >> 1);
                                RetSample[1][0] += (int)((int)((CurBufR[0x00] + CurBufR[0x01]) * volume) >> 1);
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
                                RetSample[0][0] += (int)(TempS32L * volume / SmpCnt);
                                RetSample[1][0] += (int)(TempS32R * volume / SmpCnt);
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
                            StreamPnt[0][ind] += (int)(buff[0][0] * volume);
                            StreamPnt[1][ind] += (int)(buff[1][0] * volume);
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
                            RetSample[0][OutPos] += (int)(TempSmpL / SmpCnt);
                            RetSample[1][OutPos] += (int)(TempSmpR / SmpCnt);
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
                            StreamBufs[0][ind] = (int)(buff[0][0] * volume);
                            StreamBufs[1][ind] = (int)(buff[1][0] * volume);
                        }
                        for (OutPos = 0x00; OutPos < Length; OutPos++)
                        {
                            RetSample[0][OutPos] += (int)(CurBufL[OutPos]);
                            RetSample[1][OutPos] += (int)(CurBufR[OutPos]);
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
                            StreamPnt[0][ind] += (int)(buff[0][0] * volume);
                            StreamPnt[1][ind] += (int)(buff[1][0] * volume);
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
                            RetSample[0][OutPos] += (int)(TempSmpL / SmpCnt);
                            RetSample[1][OutPos] += (int)(TempSmpR / SmpCnt);
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

            }

            return;
        }


        public void WriteSN76489(byte Data)
        {
            lock (lockobj)
            {
                if (iSN76489 == null) return;

                ((sn76489)(iSN76489)).SN76489_Write(0, Data);
            }
        }

        public void WriteYM2612(byte ChipID,byte Port, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (iYM2612 == null) return;

                ((ym2612)(iYM2612)).YM2612_Write(ChipID, (byte)(0 + (Port & 1) * 2), Adr);
                ((ym2612)(iYM2612)).YM2612_Write(ChipID, (byte)(1 + (Port & 1) * 2), Data);
            }
        }

        public void WritePWM(byte ChipID, byte Adr, uint Data)
        {
            lock (lockobj)
            {
                if (iPWM == null) return;

                ((pwm)(iPWM)).pwm_chn_w(ChipID, Adr, Data);// (byte)((adr & 0xf0)>>4),(uint)((adr & 0xf)*0x100+data));
            }
        }

        public void WriteRF5C164(byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (iRF5C164 == null) return;

                ((scd_pcm)(iRF5C164)).rf5c164_w(ChipID, Adr, Data);
            }
        }

        public void WriteRF5C164PCMData(byte ChipID, uint RAMStartAdr, uint RAMDataLength, byte[] SrcData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (iRF5C164 == null) return;

                ((scd_pcm)(iRF5C164)).rf5c164_write_ram2(ChipID, RAMStartAdr, RAMDataLength, SrcData, SrcStartAdr);
            }
        }

        public void WriteRF5C164MemW(byte ChipID, uint Adr, byte Data)
        {
            lock (lockobj)
            {
                if (iRF5C164 == null) return;

                ((scd_pcm)(iRF5C164)).rf5c164_mem_w(ChipID, Adr, Data);
            }
        }

        public void WriteC140(byte ChipID, uint Adr, byte Data)
        {
            lock (lockobj)
            {
                if (iC140 == null) return;

                ((c140)(iC140)).c140_w(ChipID, Adr, Data);
            }
        }

        public void WriteC140PCMData(byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (iC140 == null) return;

                ((c140)(iC140)).c140_write_rom2(ChipID, ROMSize, DataStart, DataLength, ROMData, SrcStartAdr);
            }
        }

        public void WriteOKIM6258(byte ChipID, byte Port, byte Data)
        {
            lock (lockobj)
            {
                if (iOKIM6258 == null) return;
                ((okim6258)(iOKIM6258)).okim6258_write(ChipID, Port, Data);
            }
        }

        public void WriteOKIM6295(byte ChipID, byte Port, byte Data)
        {
            lock (lockobj)
            {
                if (iOKIM6295 == null) return;
                ((okim6295)(iOKIM6295)).okim6295_w(ChipID, Port, Data);
            }
        }

        public void WriteOKIM6295PCMData(byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (iOKIM6295 == null) return;

                ((okim6295)(iOKIM6295)).okim6295_write_rom2(ChipID, (int)ROMSize, (int)DataStart, (int)DataLength, ROMData, SrcStartAdr);
            }
        }

        public void WriteSEGAPCM(byte ChipID, int Adr, byte Data)
        {
            lock (lockobj)
            {
                if (iSEGAPCM == null) return;
                ((segapcm)(iSEGAPCM)).sega_pcm_w(ChipID, Adr, Data);
            }
        }

        public void WriteSEGAPCMPCMData(byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (iSEGAPCM == null) return;

                ((segapcm)(iSEGAPCM)).sega_pcm_write_rom2(ChipID, (int)ROMSize, (int)DataStart, (int)DataLength, ROMData, SrcStartAdr);
            }
        }

        public void WriteYM2151(byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (iYM2151 == null) return;

                ((ym2151)(iYM2151)).YM2151_Write(ChipID, Adr, Data);
            }
        }

        public void WriteYM2203(byte ChipID, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (iYM2203 == null) return;

                ((ym2203)(iYM2203)).YM2203_Write(ChipID, Adr, Data);
            }
        }

        public void WriteYM2608(byte ChipID, byte Port, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (iYM2608 == null) return;

                ((ym2608)(iYM2608)).YM2608_Write(ChipID, (uint)(Port * 0x100 + Adr), Data);
            }
        }

        public void WriteYM2610(byte ChipID, byte Port, byte Adr, byte Data)
        {
            lock (lockobj)
            {
                if (iYM2610 == null) return;

                ((ym2610)(iYM2610)).YM2610_Write(ChipID, (uint)(Port * 0x100 + Adr), Data);
            }
        }

        public void WriteYM2610_SetAdpcmA(byte ChipID, byte[] Buf)
        {
            lock (lockobj)
            {
                if (iYM2610 == null) return;

                ((ym2610)(iYM2610)).YM2610_setAdpcmA(ChipID, Buf, Buf.Length);
            }
        }

        public void WriteYM2610_SetAdpcmB(byte ChipID, byte[] Buf)
        {
            lock (lockobj)
            {
                if (iYM2610 == null) return;

                ((ym2610)(iYM2610)).YM2610_setAdpcmB(ChipID, Buf, Buf.Length);
            }
        }



        public int[] ReadSN76489Register()
        {
            lock (lockobj)
            {
                if (iSN76489 == null) return null;
                return ((sn76489)(iSN76489)).SN76489_Chip[0].Registers;
            }
        }

        public int[][] ReadYM2612Register()
        {
            lock (lockobj)
            {
                if (iYM2612 == null) return null;
                return ((ym2612)(iYM2612)).YM2612_Chip[0].REG;
            }
        }

        public scd_pcm.pcm_chip_ ReadRf5c164Register()
        {
            lock (lockobj)
            {
                if (iRF5C164 == null || ((scd_pcm)(iRF5C164)).PCM_Chip == null || ((scd_pcm)(iRF5C164)).PCM_Chip.Length < 1) return null;
                return ((scd_pcm)(iRF5C164)).PCM_Chip[0];
            }
        }

        public c140.c140_state ReadC140Register(int cur)
        {
            lock(lockobj)
            {
                if (iC140 == null) return null;
                return ((c140)iC140).C140Data[cur];
            }
        }

        public okim6258.okim6258_state ReadOKIM6258Status(int chipID)
        {
            lock(lockobj)
            {
                if (iOKIM6258 == null) return null;
                return okim6258.OKIM6258Data[chipID];
            }
        }

        public okim6295.okim6295_state ReadOKIM6295Status(int chipID)
        {
            lock (lockobj)
            {
                if (iOKIM6295 == null) return null;
                return okim6295.OKIM6295Data[chipID];
            }
        }

        public segapcm.segapcm_state ReadSegaPCMStatus(int chipID)
        {
            lock (lockobj)
            {
                if (iSEGAPCM == null) return null;
                return ((segapcm)iSEGAPCM).SPCMData[chipID];
            }
        }


        public int[][] ReadRf5c164Volume()
        {
            lock (lockobj)
            {
                return rf5c164Vol;
            }
        }

        public int[][] ReadYM2612Volume()
        {
            lock (lockobj)
            {
                return ym2612Vol;
            }
        }

        public int[] ReadYM2612Ch3SlotVolume()
        {
            lock (lockobj)
            {
                return ym2612Ch3SlotVol;
            }
        }

        public int[][] ReadSN76489Volume()
        {
            lock (lockobj)
            {
                return sn76489Vol;
            }
        }

        public int[] ReadYM2612KeyOn()
        {
            lock (lockobj)
            {
                if (iYM2612 == null) return null;
                for (int i = 0; i < 6; i++)
                {
                    ym2612Key[i] = ((ym2612)(iYM2612)).YM2612_Chip[0].CHANNEL[i].KeyOn;
                }
                return ym2612Key;
            }
        }

        public int[] ReadYM2151KeyOn()
        {
            lock (lockobj)
            {
                if (iYM2151 == null) return null;
                for (int i = 0; i < 8; i++)
                {
                    //ym2151Key[i] = ((ym2151)(iYM2151)).YM2151_Chip[0].CHANNEL[i].KeyOn;
                }
                return ym2151Key;
            }
        }

        public int[] ReadYM2203KeyOn()
        {
            lock (lockobj)
            {
                if (iYM2203 == null) return null;
                for (int i = 0; i < 6; i++)
                {
                    //ym2203Key[i] = ((ym2203)(iYM2203)).YM2203_Chip[0].CHANNEL[i].KeyOn;
                }
                return ym2203Key;
            }
        }

        public int[] ReadYM2608KeyOn()
        {
            lock (lockobj)
            {
                if (iYM2608 == null) return null;
                for (int i = 0; i < 11; i++)
                {
                    //ym2608Key[i] = ((ym2608)(iYM2608)).YM2608_Chip[0].CHANNEL[i].KeyOn;
                }
                return ym2608Key;
            }
        }

        public int[] ReadYM2610KeyOn()
        {
            lock (lockobj)
            {
                if (iYM2610 == null) return null;
                for (int i = 0; i < 11; i++)
                {
                    //ym2610Key[i] = ((ym2610)(iYM2610)).YM2610_Chip[0].CHANNEL[i].KeyOn;
                }
                return ym2610Key;
            }
        }


        public void setSN76489Mask(int ch)
        {
            lock (lockobj)
            {
                sn76489Mask &= ~ch;
                if (iSN76489 != null) ((sn76489)(iSN76489)).SN76489_SetMute(0,sn76489Mask);
            }
        }

        public void setYM2612Mask(int ch)
        {
            lock (lockobj)
            {
                ym2612Mask |= ch;
                if (iYM2612 != null) ((ym2612)(iYM2612)).YM2612_SetMute(0, ym2612Mask);
            }
        }

        public void setRf5c164Mask(int ch)
        {
            lock (lockobj)
            {
                if (iRF5C164 != null) ((scd_pcm)(iRF5C164)).PCM_Chip[0].Channel[ch].Muted = 1;
            }
        }

        public void setSegaPcmMask(int ch)
        {
            lock (lockobj)
            {
                segapcmMask |= (uint)ch;
                if (iSEGAPCM != null) ((segapcm)(iSEGAPCM)).segapcm_set_mute_mask(0, segapcmMask);
            }
        }

        public void resetSN76489Mask(int ch)
        {
            lock (lockobj)
            {
                sn76489Mask |= ch;
                if (iSN76489 != null) ((sn76489)(iSN76489)).SN76489_SetMute(0, sn76489Mask);
            }
        }

        public void resetYM2612Mask(int ch)
        {
            lock (lockobj)
            {
                ym2612Mask &= ~ch;
                if (iYM2612 != null) ((ym2612)(iYM2612)).YM2612_SetMute(0, ym2612Mask);
            }
        }

        public void resetRf5c164Mask(int ch)
        {
            lock (lockobj)
            {
                if (iRF5C164 != null) ((scd_pcm)(iRF5C164)).PCM_Chip[0].Channel[ch].Muted = 0;
            }
        }

        public void resetSegaPcmMask(int ch)
        {
            lock (lockobj)
            {
                segapcmMask &= ~(uint)ch;
                if (iSEGAPCM != null) ((segapcm)(iSEGAPCM)).segapcm_set_mute_mask(0, segapcmMask);
            }
        }


        public int getTotalVolumeL()
        {
            lock (lockobj)
            {
                int v = 0;
                for (int i = 0; i < buffer[0].Length; i++)
                {
                    v = Math.Max(v, abs(buffer[0][i]));
                }
                return v;
            }
        }

        public int getTotalVolumeR()
        {
            lock (lockobj)
            {
                int v = 0;
                for (int i = 0; i < buffer[1].Length; i++)
                {
                    v = Math.Max(v, abs(buffer[1][i]));
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

        private int abs(int n)
        {
            return (n > 0) ? n : -n;
        }

    }
}
