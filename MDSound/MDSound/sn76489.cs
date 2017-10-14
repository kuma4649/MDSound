using System;
using System.Collections.ObjectModel;

namespace MDSound
{

    public class sn76489 :Instrument
    {

        public class SN76489_Context
        {
            public int Mute; // per-channel muting
            public int BoostNoise; // double noise volume when non-zero

            /* Variables */
            public float Clock;
            public float dClock;
            public int PSGStereo;
            public int NumClocksForSample;
            public sn76489.feedback_patterns WhiteNoiseFeedback;
            public sn76489.sr_widths SRWidth;

            /* PSG registers: */
            public int[] Registers = new int[8];        /* Tone, vol x4 */
            public int LatchedRegister;
            public int NoiseShiftRegister;
            public int NoiseFreq;            /* Noise channel signal generator frequency */

            /* Output calculation variables */
            public int[] ToneFreqVals = new int[4];      /* Frequency register values (counters) */
            public int[] ToneFreqPos = new int[4];        /* Frequency channel flip-flops */
            public int[] Channels = new int[4];          /* Value of each channel, before stereo is applied */
            public float[] IntermediatePos = new float[4];   /* intermediate values used at boundaries between + and - (does not need double accuracy)*/

            public float[][] panning = new float[4][] { new float[2], new float[2], new float[2], new float[2] };            /* fake stereo */
            public int[][] volume = new int[4][] { new int[2], new int[2], new int[2], new int[2] };

            public int NgpFlags;       /* bit 7 - NGP Mode on/off, bit 0 - is 2nd NGP chip */

            public SN76489_Context NgpChip2;
        }

        public enum feedback_patterns
        {
            FB_BBCMICRO = 0x8005, /* Texas Instruments TMS SN76489N (original) from BBC Micro computer */
            FB_SC3000 = 0x0006, /* Texas Instruments TMS SN76489AN (rev. A) from SC-3000H computer */
            FB_SEGAVDP = 0x0009, /* SN76489 clone in Sega's VDP chips (315-5124, 315-5246, 315-5313, Game Gear) */
        };

        public enum sr_widths
        {
            SRW_SC3000BBCMICRO = 15,
            SRW_SEGAVDP = 16
        };

        public enum volume_modes
        {
            VOL_TRUNC = 0,      /* Volume levels 13-15 are identical */
            VOL_FULL = 1,      /* Volume levels 13-15 are unique */
        };

        public enum mute_values : int
        {
            MUTE_ALLOFF = 0,      /* All channels muted */
            MUTE_TONE1 = 1,      /* Tone 1 mute control */
            MUTE_TONE2 = 2,      /* Tone 2 mute control */
            MUTE_TONE3 = 4,      /* Tone 3 mute control */
            MUTE_NOISE = 8,      /* Noise mute control */
            MUTE_ALLON = 15,     /* All channels enabled */
        };


        private const double PI = 3.14159265359;
        private const double SQRT2 = 1.414213562;
        private const double RANGE = 512;
        private const uint DefaultPSGClockValue = 3579545;

        private const int NoiseInitialState = 0x8000;  /* Initial state of shift register */
        private const int PSG_CUTOFF = 0x6;     /* Value below which PSG does not output */

        private readonly ReadOnlyCollection<int> PSGVolumeValues = Array.AsReadOnly(new int[]{
            /* These values are taken from a real SMS2's output */
            /*	{892,892,892,760,623,497,404,323,257,198,159,123,96,75,60,0}, /* I can't remember why 892... :P some scaling I did at some point */
            /* these values are true volumes for 2dB drops at each step (multiply previous by 10^-0.1) */
            /*1516,1205,957,760,603,479,381,303,240,191,152,120,96,76,60,0*/
            // The MAME core uses 0x2000 as maximum volume (0x1000 for bipolar output)
            4096, 3254, 2584, 2053, 1631, 1295, 1029, 817, 649, 516, 410, 325, 258, 205, 163, 0
        });

        /*static SN76489_Context SN76489[MAX_SN76489];*/
        private const int MAX_CHIPS = 2;

        private SN76489_Context[] LastChipInit = new SN76489_Context[MAX_CHIPS] { null, null };
        public SN76489_Context[] SN76489_Chip = new SN76489_Context[MAX_CHIPS] { new SN76489_Context(), new SN76489_Context() };

        //static unsigned short int FNumLimit;

        private void SN76489_Config(SN76489_Context chip, /*int mute,*/ feedback_patterns feedback, sr_widths sr_width, int boost_noise)
        {
            //chip->Mute = mute;
            chip.WhiteNoiseFeedback = feedback;
            chip.SRWidth = sr_width;
        }

        public void SN76489_GGStereoWrite(byte ChipID, int data)
        {
            SN76489_Context chip = SN76489_Chip[ChipID];
            chip.PSGStereo = data;
            //Console.WriteLine("WrPSGStereo:0:{0}", SN76489_Chip[0].PSGStereo);
            //Console.WriteLine("WrPSGStereo:1:{0}", SN76489_Chip[1].PSGStereo);
        }

        /*void SN76489_UpdateOne(SN76489_Context* chip, int *l, int *r)
        {
          INT16 tl,tr;
          INT16 *buff[2] = { &tl, &tr };
          SN76489_Update( chip, buff, 1 );
          *l = tl;
          *r = tr;
        }*/

        /*int  SN76489_GetMute(SN76489_Context* chip)
        {
          return chip->Mute;
        }*/

        private void SN76489_SetPanning(SN76489_Context chip, int ch0, int ch1, int ch2, int ch3)
        {
            calc_panning(chip.panning[0], ch0);
            calc_panning(chip.panning[1], ch1);
            calc_panning(chip.panning[2], ch2);
            calc_panning(chip.panning[3], ch3);
        }

        private void calc_panning(float[] channels, int position)
        {
            if (position > RANGE / 2)
                position = (int)(RANGE / 2);
            else if (position < -RANGE / 2)
                position = -(int)(RANGE / 2);
            position += (int)(RANGE / 2);  // make -256..0..256 -> 0..256..512

            // Equal power law: equation is
            // right = sin( position / range * pi / 2) * sqrt( 2 )
            // left is equivalent to right with position = range - position
            // position is in the range 0 .. RANGE
            // RANGE / 2 = centre, result = 1.0f
            channels[1] = (float)(Math.Sin((double)position / RANGE * Math.PI / 2) * SQRT2);
            position = (int)RANGE - position;
            channels[0] = (float)(Math.Sin((double)position / RANGE * Math.PI / 2) * SQRT2);
        }

        //-----------------------------------------------------------------
        // Reset the panning values to the centre position
        //-----------------------------------------------------------------
        private void centre_panning(float[] channels)
        {
            channels[0] = channels[1] = 1.0f;
        }


        public new const string Name = "SN76489";

        public sn76489()
        {
            visVolume = new int[2][][] {
                new int[1][] { new int[2] { 0, 0 } }
                , new int[1][] { new int[2] { 0, 0 } }
            };
            //0..Main
        }

        public override uint Start(byte ChipID, uint clock)
        {
            return Start(ChipID, DefaultPSGClockValue, clock);
        }

        public uint Start(byte ChipID, uint SamplingRate, uint PSGClockValue,params object[] option)
        {
            int i;
            SN76489_Chip[ChipID] = new SN76489_Context();
            SN76489_Context chip = SN76489_Chip[ChipID];

            if (chip != null)
            {
                chip.dClock = (float)(PSGClockValue & 0x7FFFFFF) / 16 / SamplingRate;

                SN76489_SetMute(ChipID, 15);
                SN76489_Config(chip, /*MUTE_ALLON,*/ feedback_patterns.FB_SEGAVDP, sr_widths.SRW_SEGAVDP, 1);

                for (i = 0; i <= 3; i++)
                    centre_panning(chip.panning[i]);
                //SN76489_Reset(chip);

                if ((PSGClockValue & 0x80000000) > 0 && LastChipInit != null)
                {
                    // Activate special NeoGeoPocket Mode
                    LastChipInit[ChipID].NgpFlags = 0x80 | 0x00;
                    chip.NgpFlags = 0x80 | 0x01;
                    chip.NgpChip2 = LastChipInit[ChipID];
                    LastChipInit[ChipID].NgpChip2 = chip;
                    LastChipInit[ChipID] = null;
                }
                else
                {
                    chip.NgpFlags = 0x00;
                    chip.NgpChip2 = null;
                    LastChipInit[ChipID] = chip;
                }
            }

            return SamplingRate;
        }

        public override void Reset(byte ChipID)
        {
            int i;
            SN76489_Context chip = SN76489_Chip[ChipID];

            chip.PSGStereo = 0xFF;

            for (i = 0; i <= 3; i++)
            {
                /* Initialise PSG state */
                chip.Registers[2 * i] = 1;      /* tone freq=1 */
                chip.Registers[2 * i + 1] = 0xf;    /* vol=off */
                chip.NoiseFreq = 0x10;

                /* Set counters to 0 */
                chip.ToneFreqVals[i] = 0;

                /* Set flip-flops to 1 */
                chip.ToneFreqPos[i] = 1;

                /* Set intermediate positions to do-not-use value */
                chip.IntermediatePos[i] = float.MinValue;

                /* Set panning to centre */
                //centre_panning( chip.panning[i] );
            }

            chip.LatchedRegister = 0;

            /* Initialise noise generator */
            chip.NoiseShiftRegister = NoiseInitialState;

            /* Zero clock */
            chip.Clock = 0;
        }

        public override void Stop(byte ChipID)
        {
            SN76489_Chip[ChipID] = null;
        }

        public override void Update(byte ChipID, int[][] buffer, int length)
        {
            SN76489_Context chip = SN76489_Chip[ChipID];
            //Console.WriteLine("PSGStereo:0:{0}", SN76489_Chip[0].PSGStereo);
            //Console.WriteLine("PSGStereo:1:{0}", SN76489_Chip[1].PSGStereo);

            int i, j;
            int NGPMode;
            SN76489_Context chip2 = null;
            SN76489_Context chip_t = null;
            SN76489_Context chip_n = null;

            NGPMode = (chip.NgpFlags >> 7) & 0x01;
            if (NGPMode == 0)
            {
                chip_t = chip_n = chip;
            }
            else
            {
                chip2 = (SN76489_Context)chip.NgpChip2;
                if ((chip.NgpFlags & 0x01) == 0)
                {
                    chip_t = chip;
                    chip_n = chip2;
                }
                else
                {
                    chip_t = chip2;
                    chip_n = chip;
                }
            }

            for (j = 0; j < length; j++)
            {
                /* Tone channels */
                for (i = 0; i <= 2; ++i)
                    if ((chip_t.Mute >> i & 1) > 0)
                    {
                        if (chip_t.IntermediatePos[i] != float.MinValue)
                            /* Intermediate position (antialiasing) */
                            chip.Channels[i] = (short)(PSGVolumeValues[chip.Registers[2 * i + 1]] * chip_t.IntermediatePos[i]);
                        else
                            /* Flat (no antialiasing needed) */
                            chip.Channels[i] = PSGVolumeValues[chip.Registers[2 * i + 1]] * chip_t.ToneFreqPos[i];
                    }
                    else
                        /* Muted channel */
                        chip.Channels[i] = 0;

                /* Noise channel */
                if ((chip_n.Mute >> 3 & 1) > 0)
                {
                    //chip->Channels[3] = PSGVolumeValues[chip->Registers[7]] * ( chip_n->NoiseShiftRegister & 0x1 ) * 2; /* double noise volume */
                    // Now the noise is bipolar, too. -Valley Bell
                    chip.Channels[3] = PSGVolumeValues[chip.Registers[7]] * ((chip_n.NoiseShiftRegister & 0x1) * 2 - 1);
                    // due to the way the white noise works here, it seems twice as loud as it should be
                    if ((chip.Registers[6] & 0x4) > 0)
                        chip.Channels[3] >>= 1;
                }
                else
                    chip.Channels[i] = 0;

                // Build stereo result into buffer
                buffer[0][j] = 0;
                buffer[1][j] = 0;
                int bl = 0;
                int br = 0;
                if (chip.NgpFlags == 0)
                {
                    // For all 4 channels
                    for (i = 0; i <= 3; ++i)
                    {
                        if (((chip.PSGStereo >> i) & 0x11) == 0x11)
                        {
                            //Console.WriteLine("ggpan1");
                            // no GG stereo for this channel
                            if (chip.panning[i][0] == 1.0f)
                            {
                                bl = chip.Channels[i]; // left
                                br = chip.Channels[i]; // right

                            }
                            else
                            {
                                bl = (int)(chip.panning[i][0] * chip.Channels[i]); // left
                                br = (int)(chip.panning[i][1] * chip.Channels[i]); // right

                            }
                        }
                        else
                        {
                            //Console.WriteLine("ggpan2");
                            // GG stereo overrides panning
                            bl = ((chip.PSGStereo >> (i + 4)) & 0x1) * chip.Channels[i]; // left
                            br = ((chip.PSGStereo >> i) & 0x1) * chip.Channels[i]; // right
                            //Console.WriteLine("Ch:bl:br:{0}:{1}:{2}:{3}",i,bl,br, chip.Channels[i]);
                        }

                        buffer[0][j] += bl;
                        buffer[1][j] += br;
                        chip.volume[i][0] = Math.Abs(bl);// Math.Max(bl, chip.volume[i][0]);
                        chip.volume[i][1] = Math.Abs(br);// Math.Max(br, chip.volume[i][1]);
                    }
                }
                else
                {
                    if ((chip.NgpFlags & 0x01) == 0)
                    {
                        // For all 3 tone channels
                        for (i = 0; i < 3; i++)
                        {
                            bl = (chip.PSGStereo >> (i + 4) & 0x1) * chip.Channels[i]; // left
                            br = (chip.PSGStereo >> i & 0x1) * chip2.Channels[i]; // right
                            buffer[0][j] += bl;
                            buffer[1][j] += br;
                            chip.volume[i][0] = Math.Abs(bl);// Math.Max(bl, chip.volume[i][0]);
                            chip.volume[i][1] = Math.Abs(br);// Math.Max(br, chip.volume[i][1]);
                        }
                    }
                    else
                    {
                        // noise channel
                        i = 3;
                        bl = (chip.PSGStereo >> (i + 4) & 0x1) * chip2.Channels[i]; // left
                        br = (chip.PSGStereo >> i & 0x1) * chip.Channels[i]; // right
                        buffer[0][j] += bl;
                        buffer[1][j] += br;
                        chip.volume[i][0] = Math.Abs(bl);// Math.Max(bl, chip.volume[i][0]);
                        chip.volume[i][1] = Math.Abs(br);// Math.Max(br, chip.volume[i][1]);
                    }
                }


                /* Increment clock by 1 sample length */
                chip.Clock += chip.dClock;
                chip.NumClocksForSample = (int)chip.Clock;  /* truncate */
                chip.Clock -= chip.NumClocksForSample;      /* remove integer part */

                /* Decrement tone channel counters */
                for (i = 0; i <= 2; ++i)
                    chip.ToneFreqVals[i] -= chip.NumClocksForSample;

                /* Noise channel: match to tone2 or decrement its counter */
                if (chip.NoiseFreq == 0x80)
                    chip.ToneFreqVals[3] = chip.ToneFreqVals[2];
                else
                    chip.ToneFreqVals[3] -= chip.NumClocksForSample;

                /* Tone channels: */
                for (i = 0; i <= 2; ++i)
                {
                    if (chip.ToneFreqVals[i] <= 0)
                    {   /* If the counter gets below 0... */
                        if (chip.Registers[i * 2] >= PSG_CUTOFF)
                        {
                            /* For tone-generating values, calculate how much of the sample is + and how much is - */
                            /* This is optimised into an even more confusing state than it was in the first place... */
                            chip.IntermediatePos[i] = (chip.NumClocksForSample - chip.Clock + 2 * chip.ToneFreqVals[i]) * chip.ToneFreqPos[i] / (chip.NumClocksForSample + chip.Clock);
                            /* Flip the flip-flop */
                            chip.ToneFreqPos[i] = -chip.ToneFreqPos[i];
                        }
                        else
                        {
                            /* stuck value */
                            chip.ToneFreqPos[i] = 1;
                            chip.IntermediatePos[i] = float.MinValue;
                        }
                        chip.ToneFreqVals[i] += chip.Registers[i * 2] * (chip.NumClocksForSample / chip.Registers[i * 2] + 1);
                    }
                    else
                        /* signal no antialiasing needed */
                        chip.IntermediatePos[i] = float.MinValue;
                }

                /* Noise channel */
                if (chip.ToneFreqVals[3] <= 0)
                {
                    /* If the counter gets below 0... */
                    /* Flip the flip-flop */
                    chip.ToneFreqPos[3] = -chip.ToneFreqPos[3];
                    if (chip.NoiseFreq != 0x80)
                        /* If not matching tone2, decrement counter */
                        chip.ToneFreqVals[3] += chip.NoiseFreq * (chip.NumClocksForSample / chip.NoiseFreq + 1);
                    if (chip.ToneFreqPos[3] == 1)
                    {
                        /* On the positive edge of the square wave (only once per cycle) */
                        int Feedback;
                        if ((chip.Registers[6] & 0x4) > 0)
                        {
                            /* White noise */
                            /* Calculate parity of fed-back bits for feedback */
                            switch (chip.WhiteNoiseFeedback)
                            {
                                /* Do some optimised calculations for common (known) feedback values */
                                //case 0x0003: /* SC-3000, BBC %00000011 */
                                case feedback_patterns.FB_SEGAVDP: /* SMS, GG, MD  %00001001 */
                                                                   /* If two bits fed back, I can do Feedback=(nsr & fb) && (nsr & fb ^ fb) */
                                                                   /* since that's (one or more bits set) && (not all bits set) */
                                    Feedback = chip.NoiseShiftRegister & (int)chip.WhiteNoiseFeedback;
                                    Feedback = (Feedback > 0) && (((chip.NoiseShiftRegister & (int)chip.WhiteNoiseFeedback) ^ (int)chip.WhiteNoiseFeedback) > 0) ? 1 : 0;
                                    break;
                                default:
                                    /* Default handler for all other feedback values */
                                    /* XOR fold bits into the final bit */
                                    Feedback = chip.NoiseShiftRegister & (int)chip.WhiteNoiseFeedback;
                                    Feedback ^= Feedback >> 8;
                                    Feedback ^= Feedback >> 4;
                                    Feedback ^= Feedback >> 2;
                                    Feedback ^= Feedback >> 1;
                                    Feedback &= 1;
                                    break;
                            }
                        }
                        else      /* Periodic noise */
                            Feedback = chip.NoiseShiftRegister & 1;

                        chip.NoiseShiftRegister = (chip.NoiseShiftRegister >> 1) | (Feedback << ((int)chip.SRWidth - 1));
                    }
                }
            }

            visVolume[ChipID][0][0] = chip.volume[0][0];
            visVolume[ChipID][0][1] = chip.volume[0][1];

        }

        public void SN76489_Write(byte ChipID, int data)
        {
            SN76489_Context chip = SN76489_Chip[ChipID];

            if ((data & 0x80) > 0)
            {
                /* Latch/data byte  %1 cc t dddd */
                chip.LatchedRegister = (data >> 4) & 0x07;
                chip.Registers[chip.LatchedRegister] =
                    (chip.Registers[chip.LatchedRegister] & 0x3f0) /* zero low 4 bits */
                    | (data & 0xf);                            /* and replace with data */
            }
            else
            {
                /* Data byte        %0 - dddddd */
                if ((chip.LatchedRegister % 2) == 0 && (chip.LatchedRegister < 5))
                    /* Tone register */
                    chip.Registers[chip.LatchedRegister] =
                        (chip.Registers[chip.LatchedRegister] & 0x00f) /* zero high 6 bits */
                        | ((data & 0x3f) << 4);                 /* and replace with data */
                else
                    /* Other register */
                    chip.Registers[chip.LatchedRegister] = data & 0x0f; /* Replace with data */
            }
            switch (chip.LatchedRegister)
            {
                case 0:
                case 2:
                case 4: /* Tone channels */
                    if (chip.Registers[chip.LatchedRegister] == 0)
                        chip.Registers[chip.LatchedRegister] = 1; /* Zero frequency changed to 1 to avoid div/0 */
                    break;
                case 6: /* Noise */
                    chip.NoiseShiftRegister = NoiseInitialState;        /* reset shift register */
                    chip.NoiseFreq = 0x10 << (chip.Registers[6] & 0x3); /* set noise signal generator frequency */
                    break;
            }
        }

        public void SN76489_SetMute(byte ChipID, int val)
        {
            SN76489_Context chip = SN76489_Chip[ChipID];

            chip.Mute = val;
        }

    }

}