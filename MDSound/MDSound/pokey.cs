﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MDSound
{
    public class pokey : Instrument
    {
        public override string Name { get => "POKEY"; set => throw new NotImplementedException(); }
        public override string ShortName { get => "POKEY"; set => throw new NotImplementedException(); }

        public pokey()
        {
            visVolume = new int[2][][] {
                new int[1][] { new int[2] { 0, 0 } }
                , new int[1][] { new int[2] { 0, 0 } }
            };
        }

        public override void Reset(byte ChipID)
        {
            device_reset_pokey(ChipID);
        }

        public override uint Start(byte ChipID, uint clock)
        {
            return (uint)device_start_pokey(ChipID, (int)1789772);
        }

        public override uint Start(byte ChipID, uint clock, uint ClockValue, params object[] option)
        {
            return (uint)device_start_pokey(ChipID, (int)ClockValue);
        }

        public override void Stop(byte ChipID)
        {
            device_stop_pokey(ChipID);
        }

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {
            pokey_update(ChipID, outputs, samples);

            visVolume[ChipID][0][0] = outputs[0][0];
            visVolume[ChipID][0][1] = outputs[1][0];
        }

        public override int Write(byte ChipID, int port, int adr, int data)
        {
            pokey_w(ChipID, adr, (byte)data);
            return 0;
        }

        public pokey_state Read(byte ChipID)
        {
            return PokeyData[ChipID];
        }



        /*****************************************************************************
 *
 *  POKEY chip emulator 4.3
 *  Copyright Nicola Salmoria and the MAME Team
 *
 *  Based on original info found in Ron Fries' Pokey emulator,
 *  with additions by Brad Oliver, Eric Smith and Juergen Buchmueller.
 *  paddle (a/d conversion) details from the Atari 400/800 Hardware Manual.
 *  Polynome algorithms according to info supplied by Perry McFarlane.
 *
 *  This code is subject to the MAME license, which besides other
 *  things means it is distributed as is, no warranties whatsoever.
 *  For more details read mame.txt that comes with MAME.
 *
 *****************************************************************************/

        //#pragma once

        //#include "devlegcy.h"

        /* CONSTANT DEFINITIONS */

        /* POKEY WRITE LOGICALS */
        private const int AUDF1_C = 0x00;
        private const int AUDC1_C = 0x01;
        private const int AUDF2_C = 0x02;
        private const int AUDC2_C = 0x03;
        private const int AUDF3_C = 0x04;
        private const int AUDC3_C = 0x05;
        private const int AUDF4_C = 0x06;
        private const int AUDC4_C = 0x07;
        private const int AUDCTL_C = 0x08;
        private const int STIMER_C = 0x09;
        private const int SKREST_C = 0x0A;
        private const int POTGO_C = 0x0B;
        private const int SEROUT_C = 0x0D;
        private const int IRQEN_C = 0x0E;
        private const int SKCTL_C = 0x0F;
        /* POKEY READ LOGICALS */
        private const int POT0_C = 0x00;
        private const int POT1_C = 0x01;
        private const int POT2_C = 0x02;
        private const int POT3_C = 0x03;
        private const int POT4_C = 0x04;
        private const int POT5_C = 0x05;
        private const int POT6_C = 0x06;
        private const int POT7_C = 0x07;
        private const int ALLPOT_C = 0x08;
        private const int KBCODE_C = 0x09;
        private const int RANDOM_C = 0x0A;
        private const int SERIN_C = 0x0D;
        private const int IRQST_C = 0x0E;
        private const int SKSTAT_C = 0x0F;
        /* exact 1.79 MHz clock freq (of the Atari 800 that is) */
        private const int FREQ_17_EXACT = 1789790;





        /*****************************************************************************
         * pot0_r to pot7_r:
         *  Handlers for reading the pot values. Some Atari games use
         *  ALLPOT to return dipswitch settings and other things.
         * serin_r, serout_w, interrupt_cb:
         *  New function pointers for serial input/output and a interrupt callback.
         *****************************************************************************/

        /*typedef struct _pokey_interface pokey_interface;
        struct _pokey_interface
        {
            devcb_read8 pot_r[8];
            devcb_read8 allpot_r;
            devcb_read8 serin_r;
            devcb_write8 serout_w;
            void (*interrupt_cb)(device_t *device, int mask);
        };*/


        //void pokey_update(UINT8 ChipID, stream_sample_t** outputs, int samples);
        //int device_start_pokey(UINT8 ChipID, int clock);
        //void device_stop_pokey(UINT8 ChipID);
        //void device_reset_pokey(UINT8 ChipID);

        ////READ8_DEVICE_HANDLER( pokey_r );
        ////WRITE8_DEVICE_HANDLER( pokey_w );
        //UINT8 pokey_r(UINT8 ChipID, offs_t offset);
        //void pokey_w(UINT8 ChipID, offs_t offset, UINT8 data);

        ///* fix me: eventually this should be a single device with pokey subdevices */
        ////READ8_HANDLER( quad_pokey_r );
        ////WRITE8_HANDLER( quad_pokey_w );

        ///*void pokey_serin_ready (device_t *device, int after);
        //void pokey_break_w (device_t *device, int shift);
        //void pokey_kbcode_w (device_t *device, int kbcode, int make);*/

        //void pokey_set_mute_mask(UINT8 ChipID, UINT32 MuteMask);

        ////DECLARE_LEGACY_SOUND_DEVICE(POKEY, pokey);








        /*****************************************************************************
         *
         *  POKEY chip emulator 4.51
         *  Copyright Nicola Salmoria and the MAME Team
         *
         *  Based on original info found in Ron Fries' Pokey emulator,
         *  with additions by Brad Oliver, Eric Smith and Juergen Buchmueller,
         *  paddle (a/d conversion) details from the Atari 400/800 Hardware Manual.
         *  Polynome algorithms according to info supplied by Perry McFarlane.
         *
         *  This code is subject to the MAME license, which besides other
         *  things means it is distributed as is, no warranties whatsoever.
         *  For more details read mame.txt that comes with MAME.
         *
         *  4.51:
         *  - changed to use the attotime datatype
         *  4.5:
         *  - changed the 9/17 bit polynomial formulas such that the values
         *    required for the Tempest Pokey protection will be found.
         *    Tempest expects the upper 4 bits of the RNG to appear in the
         *    lower 4 bits after four cycles, so there has to be a shift
         *    of 1 per cycle (which was not the case before). Bits #6-#13 of the
         *    new RNG give this expected result now, bits #0-7 of the 9 bit poly.
         *  - reading the RNG returns the shift register contents ^ 0xff.
         *    That way resetting the Pokey with SKCTL (which resets the
         *    polynome shifters to 0) returns the expected 0xff value.
         *  4.4:
         *  - reversed sample values to make OFF channels produce a zero signal.
         *    actually de-reversed them; don't remember that I reversed them ;-/
         *  4.3:
         *  - for POT inputs returning zero, immediately assert the ALLPOT
         *    bit after POTGO is written, otherwise start trigger timer
         *    depending on SK_PADDLE mode, either 1-228 scanlines or 1-2
         *    scanlines, depending on the SK_PADDLE bit of SKCTL.
         *  4.2:
         *  - half volume for channels which are inaudible (this should be
         *    close to the real thing).
         *  4.1:
         *  - default gain increased to closely match the old code.
         *  - random numbers repeat rate depends on POLY9 flag too!
         *  - verified sound output with many, many Atari 800 games,
         *    including the SUPPRESS_INAUDIBLE optimizations.
         *  4.0:
         *  - rewritten from scratch.
         *  - 16bit stream interface.
         *  - serout ready/complete delayed interrupts.
         *  - reworked pot analog/digital conversion timing.
         *  - optional non-indexing pokey update functions.
         *
         *****************************************************************************/

        //# include "mamedef.h"
        ////#include "emu.h"
        //# ifdef _DEBUG
        //# include <stdio.h>
        //#endif
        //# include "pokey.h"

        /*
         * Defining this produces much more (about twice as much)
         * but also more efficient code. Ideally this should be set
         * for processors with big code cache and for healthy compilers :)
         */
        //#ifndef BIG_SWITCH
        //#ifndef HEAVY_MACRO_USAGE
        private const int HEAVY_MACRO_USAGE = 1;
        //#endif
        //#else
        //#define HEAVY_MACRO_USAGE	BIG_SWITCH
        //#endif

        private const int SUPPRESS_INAUDIBLE = 1;

        /* Four channels with a range of 0..32767 and volume 0..15 */
        //#define POKEY_DEFAULT_GAIN (32767/15/4)

        /*
         * But we raise the gain and risk clipping, the old Pokey did
         * this too. It defined POKEY_DEFAULT_GAIN 6 and this was
         * 6 * 15 * 4 = 360, 360/256 = 1.40625
         * I use 15/11 = 1.3636, so this is a little lower.
         */
        private const int POKEY_DEFAULT_GAIN = (32767 / 11 / 4);
        private const int VERBOSE = 0;
        private const int VERBOSE_SOUND = 0;
        private const int VERBOSE_TIMER = 0;
        private const int VERBOSE_POLY = 0;
        private const int VERBOSE_RAND = 0;

        private void logerror(string x) { }
        private void LOG(string x) { do { if (VERBOSE != 0) { logerror(x); } } while (false); }

        private void LOG_SOUND(string x) { do { if (VERBOSE_SOUND != 0) { logerror(x); } } while (false); }

        private void LOG_TIMER(string x) { do { if (VERBOSE_TIMER != 0) { logerror(x); } } while (false); }

        private void LOG_POLY(string x) { do { if (VERBOSE_POLY != 0) { logerror(x); } } while (false); }

        private void LOG_RAND(string x) { do { if (VERBOSE_RAND != 0) { logerror(x); } } while (false); }

        private const int CHAN1 = 0;
        private const int CHAN2 = 1;
        private const int CHAN3 = 2;
        private const int CHAN4 = 3;

        private const int TIMER1 = 0;
        private const int TIMER2 = 1;
        private const int TIMER4 = 2;

        /* values to add to the divisors for the different modes */
        private const int DIVADD_LOCLK = 1;
        private const int DIVADD_HICLK = 4;
        private const int DIVADD_HICLK_JOINED = 7;

        /* AUDCx */
        private const int NOTPOLY5 = 0x80;  /* selects POLY5 or direct CLOCK */
        private const int POLY4 = 0x40; /* selects POLY4 or POLY17 */
        private const int PURE = 0x20;  /* selects POLY4/17 or PURE tone */
        private const int VOLUME_ONLY = 0x10;   /* selects VOLUME OUTPUT ONLY */
        private const int VOLUME_MASK = 0x0f;  /* volume mask */

        /* AUDCTL */
        private const int POLY9 = 0x80;	/* selects POLY9 or POLY17 */
        private const int CH1_HICLK = 0x40;	/* selects 1.78979 MHz for Ch 1 */
        private const int CH3_HICLK = 0x20;	/* selects 1.78979 MHz for Ch 3 */
        private const int CH12_JOINED = 0x10;	/* clocks channel 1 w/channel 2 */
        private const int CH34_JOINED = 0x08;	/* clocks channel 3 w/channel 4 */
        private const int CH1_FILTER = 0x04;	/* selects channel 1 high pass filter */
        private const int CH2_FILTER = 0x02;	/* selects channel 2 high pass filter */
        private const int CLK_15KHZ = 0x01; /* selects 15.6999 kHz or 63.9211 kHz */

        /* IRQEN (D20E) */
        private const int IRQ_BREAK = 0x80;	/* BREAK key pressed interrupt */
        private const int IRQ_KEYBD = 0x40;	/* keyboard data ready interrupt */
        private const int IRQ_SERIN = 0x20;	/* serial input data ready interrupt */
        private const int IRQ_SEROR = 0x10;	/* serial output register ready interrupt */
        private const int IRQ_SEROC = 0x08;	/* serial output complete interrupt */
        private const int IRQ_TIMR4 = 0x04;	/* timer channel #4 interrupt */
        private const int IRQ_TIMR2 = 0x02;	/* timer channel #2 interrupt */
        private const int IRQ_TIMR1 = 0x01;	/* timer channel #1 interrupt */

        /* SKSTAT (R/D20F) */
        private const int SK_FRAME = 0x80;	/* serial framing error */
        private const int SK_OVERRUN = 0x40;	/* serial overrun error */
        private const int SK_KBERR = 0x20;	/* keyboard overrun error */
        private const int SK_SERIN = 0x10;	/* serial input high */
        private const int SK_SHIFT = 0x08;	/* shift key pressed */
        private const int SK_KEYBD = 0x04;	/* keyboard key pressed */
        private const int SK_SEROUT = 0x02;	/* serial output active */

        /* SKCTL (W/D20F) */
        private const int SK_BREAK = 0x80;	/* serial out break signal */
        private const int SK_BPS = 0x70;	/* bits per second */
        private const int SK_FM = 0x08;	/* FM mode */
        private const int SK_PADDLE = 0x04;	/* fast paddle a/d conversion */
        private const int SK_RESET = 0x03;	/* reset serial/keyboard interface */

        private const int DIV_64 = 28;		 /* divisor for 1.78979 MHz clock to 63.9211 kHz */
        private const int DIV_15 = 114; 	 /* divisor for 1.78979 MHz clock to 15.6999 kHz */

        //typedef struct _pokey_state pokey_state;
        public class pokey_state
        {
            public Int32[] counter = new int[4];       /* channel counter */
            public Int32[] divisor = new int[4];       /* channel divisor (modulo value) */
            public UInt32[] volume = new uint[4];       /* channel volume - derived */
            public byte[] output = new byte[4];        /* channel output signal (1 active, 0 inactive) */
            public byte[] audible = new byte[4];       /* channel plays an audible tone/effect */
            public byte[] Muted = new byte[4];
            public UInt32 samplerate_24_8; /* sample rate in 24.8 format */
            public UInt32 samplepos_fract; /* sample position fractional part */
            public UInt32 samplepos_whole; /* sample position whole part */
            public UInt32 polyadjust;      /* polynome adjustment */
            public UInt32 p4;              /* poly4 index */
            public UInt32 p5;              /* poly5 index */
            public UInt32 p9;              /* poly9 index */
            public UInt32 p17;             /* poly17 index */
            public UInt32 r9;              /* rand9 index */
            public UInt32 r17;             /* rand17 index */
            public UInt32 clockmult;       /* clock multiplier */
            //device_t *device;
            //sound_stream * channel; /* streams channel */
            //emu_timer *timer[3];	/* timers for channel 1,2 and 4 events */
            //attotime timer_period[3];	/* computed periods for these timers */
            //Int timer_param[3];		/* computed parameters for these timers */
            //emu_timer *rtimer;     /* timer for calculating the random offset */
            //emu_timer *ptimer[8];	/* pot timers */
            //devcb_resolved_read8 pot_r[8];
            //devcb_resolved_read8 allpot_r;
            //devcb_resolved_read8 serin_r;
            //devcb_resolved_write8 serout_w;
            //void (*Interrupt_cb)(device_t *device, Int mask);
            public byte[] AUDF = new byte[4];          /* AUDFx (D200, D202, D204, D206) */
            public byte[] AUDC = new byte[4];          /* AUDCx (D201, D203, D205, D207) */
            public byte[] POTx = new byte[8];          /* POTx   (R/D200-D207) */
            public byte AUDCTL;           /* AUDCTL (W/D208) */
            public byte ALLPOT;           /* ALLPOT (R/D208) */
            public byte KBCODE;           /* KBCODE (R/D209) */
            public byte RANDOM;           /* RANDOM (R/D20A) */
            public byte SERIN;            /* SERIN  (R/D20D) */
            public byte SEROUT;           /* SEROUT (W/D20D) */
            public byte IRQST;            /* IRQST  (R/D20E) */
            public byte IRQEN;            /* IRQEN  (W/D20E) */
            public byte SKSTAT;           /* SKSTAT (R/D20F) */
            public byte SKCTL;            /* SKCTL  (W/D20F) */
            //pokey_Interface Intf;
            //attotime clock_period;
            public double clock_period;
            //attotime ad_time_fast;
            //attotime ad_time_slow;

            public byte[] poly4 = new byte[0x0f];
            public byte[] poly5 = new byte[0x1f];
            public byte[] poly9 = new byte[0x1ff];
            public byte[] poly17 = new byte[0x1ffff];

            public byte[] rand9 = new byte[0x1ff];
            public byte[] rand17 = new byte[0x1ffff];
        };


        private byte P4(pokey_state chip) { return chip.poly4[chip.p4]; }
        private byte P5(pokey_state chip) { return chip.poly5[chip.p5]; }
        private byte P9(pokey_state chip) { return chip.poly9[chip.p9]; }
        private byte P17(pokey_state chip) { return chip.poly17[chip.p17]; }

        //private TIMER_CALLBACK pokey_timer_expire;
        //private TIMER_CALLBACK pokey_pot_trigger;


        private const int SAMPLE = -1;

        private void ADJUST_EVENT(pokey_state chip, uint _event)
        {
            chip.counter[CHAN1] -= (int)_event;
            chip.counter[CHAN2] -= (int)_event;
            chip.counter[CHAN3] -= (int)_event;
            chip.counter[CHAN4] -= (int)_event;
            chip.samplepos_whole -= (uint)_event;
            chip.polyadjust += (uint)_event;
        }

        //#if SUPPRESS_INAUDIBLE

        private void PROCESS_CHANNEL(pokey_state chip, int ch, uint _event, ref uint sum)
        {
            int toggle = 0;
            ADJUST_EVENT(chip, _event);
            /* reset the channel counter */
            if (chip.audible[ch] != 0)
                chip.counter[ch] = chip.divisor[ch];

            else
                chip.counter[ch] = 0x7fffffff;
            chip.p4 = (chip.p4 + chip.polyadjust) % 0x0000f;
            chip.p5 = (chip.p5 + chip.polyadjust) % 0x0001f;
            chip.p9 = (chip.p9 + chip.polyadjust) % 0x001ff;
            chip.p17 = (chip.p17 + chip.polyadjust) % 0x1ffff;
            chip.polyadjust = 0;
            if ((chip.AUDC[ch] & NOTPOLY5) != 0 || P5(chip) != 0)
            {
                if ((chip.AUDC[ch] & PURE) != 0)
                    toggle = 1;

                else
                if ((chip.AUDC[ch] & POLY4) != 0)
                    toggle = ((chip.output[ch] != 0 && P4(chip) == 0) || (chip.output[ch] == 0 && P4(chip) != 0)) ? 1 : 0;

                else
                if ((chip.AUDCTL & POLY9) != 0)
                    toggle = ((chip.output[ch] != 0 && P9(chip) == 0) || (chip.output[ch] == 0 && P9(chip) != 0)) ? 1 : 0;

                else
                    toggle = ((chip.output[ch] != 0 && P17(chip) == 0) || (chip.output[ch] == 0 && P17(chip) != 0)) ? 1 : 0;
            }
            if (toggle != 0)
            {
                if (chip.audible[ch] != 0 && chip.Muted[ch] == 0)
                {
                    if (chip.output[ch] != 0)
                        sum -= chip.volume[ch];

                    else
                        sum += chip.volume[ch];
                }
                chip.output[ch] ^= 1;
            }
            /* is this a filtering channel (3/4) and is the filter active? */
            if ((chip.AUDCTL & ((CH1_FILTER | CH2_FILTER) & (0x10 >> ch))) != 0)
            {
                if (chip.output[ch - 2] != 0)
                {
                    chip.output[ch - 2] = 0;
                    if (chip.audible[ch] != 0 && chip.Muted[ch] == 0)
                        sum -= chip.volume[ch - 2];
                }
            }
        }
        //#else

        //        private void PROCESS_CHANNEL(pokey_state chip, int ch, uint _event, uint sum)
        //        {
        //            int toggle = 0;
        //            ADJUST_EVENT(chip, _event);
        //            /* reset the channel counter */
        //            chip.counter[ch] = p[chip].divisor[ch];
        //            chip.p4 = (chip.p4 + chip.polyadjust) % 0x0000f;
        //            chip.p5 = (chip.p5 + chip.polyadjust) % 0x0001f;
        //            chip.p9 = (chip.p9 + chip.polyadjust) % 0x001ff;
        //            chip.p17 = (chip.p17 + chip.polyadjust) % 0x1ffff;
        //            chip.polyadjust = 0;
        //            if ((chip.AUDC[ch] & NOTPOLY5) != 0 || P5(chip) != 0)
        //            {
        //                if ((chip.AUDC[ch] & PURE) != 0)
        //                    toggle = 1;
        //                else
        //                if ((chip.AUDC[ch] & POLY4) != 0)
        //                    toggle = ((chip.output[ch] != 0 && P4(chip) == 0) || (chip.output[ch] == 0 && P4(chip) != 0)) ? 1 : 0;
        //                else
        //                if ((chip.AUDCTL & POLY9) != 0)
        //                    toggle = ((chip.output[ch] != 0 && P9(chip) == 0) || (chip.output[ch] == 0 && P9(chip) != 0)) ? 1 : 0;
        //                else
        //                    toggle = ((chip.output[ch] != 0 && P17(chip) == 0) || (chip.output[ch] == 0 && P17(chip) != 0)) ? 1 : 0;
        //            }
        //            if (toggle != 0 && chip.Muted[ch] == 0)
        //            {
        //                if (chip.output[ch] != 0)
        //                    sum -= chip.volume[ch];
        //                else
        //                    sum += chip.volume[ch];
        //                chip.output[ch] ^= 1;
        //            }
        //            /* is this a filtering channel (3/4) and is the filter active? */
        //            if ((chip.AUDCTL & ((CH1_FILTER | CH2_FILTER) & (0x10 >> ch))) != 0)
        //            {
        //                if (chip.output[ch - 2] != 0 && chip.Muted[ch] == 0)
        //                {
        //                    chip.output[ch - 2] = 0;
        //                    sum -= chip.volume[ch - 2];
        //                }
        //            }
        //        }
        //#endif

        private void PROCESS_SAMPLE(pokey_state chip, ref int samples, int[] bufL, int[] bufR, ref int bufPtr, uint _event, uint sum)
        {
            ADJUST_EVENT(chip, _event);
            /* adjust the sample position */
            chip.samplepos_whole++;
            /* store sum of output signals into the buffer */
            /* *buffer++ = (sum > 0x7fff) ? 0x7fff : sum; */
            bufL[bufPtr] = bufR[bufPtr] = (int)sum;
            bufPtr++;
            samples--;
        }

        //#if HEAVY_MACRO_USAGE

        /*
         * This version of PROCESS_POKEY repeats the search for the minimum
         * event value without using an index to the channel. That way the
         * PROCESS_CHANNEL macros can be called with fixed values and expand
         * to much more efficient code
         */

        private void PROCESS_POKEY(pokey_state chip, int samples, int[] bufL, int[] bufR, int bufPtr)
        {
            uint sum = 0;
            if (chip.output[CHAN1] != 0 && chip.Muted[CHAN1] == 0)
                sum += chip.volume[CHAN1];
            if (chip.output[CHAN2] != 0 && chip.Muted[CHAN2] == 0)
                sum += chip.volume[CHAN2];
            if (chip.output[CHAN3] != 0 && chip.Muted[CHAN3] == 0)
                sum += chip.volume[CHAN3];
            if (chip.output[CHAN4] != 0 && chip.Muted[CHAN4] == 0)
                sum += chip.volume[CHAN4];
            while (samples > 0)
            {
                if (chip.counter[CHAN1] < chip.samplepos_whole)
                {
                    if (chip.counter[CHAN2] < chip.counter[CHAN1])
                    {
                        if (chip.counter[CHAN3] < chip.counter[CHAN2])
                        {
                            if (chip.counter[CHAN4] < chip.counter[CHAN3])
                            {
                                uint _event = (uint)chip.counter[CHAN4];
                                PROCESS_CHANNEL(chip, CHAN4, _event, ref sum);
                            }
                            else
                            {
                                uint _event = (uint)chip.counter[CHAN3];
                                PROCESS_CHANNEL(chip, CHAN3, _event, ref sum);
                            }
                        }
                        else
                        if (chip.counter[CHAN4] < chip.counter[CHAN2])
                        {
                            uint _event = (uint)chip.counter[CHAN4];
                            PROCESS_CHANNEL(chip, CHAN4, _event, ref sum);
                        }

                        else
                        {
                            uint _event = (uint)chip.counter[CHAN2];
                            PROCESS_CHANNEL(chip, CHAN2, _event, ref sum);
                        }
                    }
                    else
                    if (chip.counter[CHAN3] < chip.counter[CHAN1])
                    {
                        if (chip.counter[CHAN4] < chip.counter[CHAN3])
                        {
                            uint _event = (uint)chip.counter[CHAN4];
                            PROCESS_CHANNEL(chip, CHAN4, _event, ref sum);
                        }
                        else
                        {
                            uint _event = (uint)chip.counter[CHAN3];
                            PROCESS_CHANNEL(chip, CHAN3, _event, ref sum);
                        }
                    }
                    else
                    if (chip.counter[CHAN4] < chip.counter[CHAN1])
                    {
                        uint _event = (uint)chip.counter[CHAN4];
                        PROCESS_CHANNEL(chip, CHAN4, _event, ref sum);
                    }

                    else
                    {
                        uint _event = (uint)chip.counter[CHAN1];
                        PROCESS_CHANNEL(chip, CHAN1, _event, ref sum);
                    }
                }
                else
                if (chip.counter[CHAN2] < chip.samplepos_whole)
                {
                    if (chip.counter[CHAN3] < chip.counter[CHAN2])
                    {
                        if (chip.counter[CHAN4] < chip.counter[CHAN3])
                        {
                            uint _event = (uint)chip.counter[CHAN4];
                            PROCESS_CHANNEL(chip, CHAN4, _event, ref sum);
                        }
                        else
                        {
                            uint _event = (uint)chip.counter[CHAN3];
                            PROCESS_CHANNEL(chip, CHAN3, _event, ref sum);
                        }
                    }
                    else
                    if (chip.counter[CHAN4] < chip.counter[CHAN2])
                    {
                        uint _event = (uint)chip.counter[CHAN4];
                        PROCESS_CHANNEL(chip, CHAN4, _event, ref sum);
                    }
                    else
                    {
                        uint _event = (uint)chip.counter[CHAN2];
                        PROCESS_CHANNEL(chip, CHAN2, _event, ref sum);
                    }
                }
                else
                if (chip.counter[CHAN3] < chip.samplepos_whole)
                {
                    if (chip.counter[CHAN4] < chip.counter[CHAN3])
                    {
                        uint _event = (uint)chip.counter[CHAN4];
                        PROCESS_CHANNEL(chip, CHAN4, _event, ref sum);
                    }
                    else
                    {
                        uint _event = (uint)chip.counter[CHAN3];
                        PROCESS_CHANNEL(chip, CHAN3, _event, ref sum);
                    }
                }
                else
                if (chip.counter[CHAN4] < chip.samplepos_whole)
                {
                    uint _event = (uint)chip.counter[CHAN4];
                    PROCESS_CHANNEL(chip, CHAN4, _event, ref sum);
                }
                else
                {
                    uint _event = chip.samplepos_whole;
                    PROCESS_SAMPLE(chip, ref samples, bufL, bufR, ref bufPtr, _event, sum);
                }
            }/*																
        	chip->rtimer->adjust(attotime::never)*/
        }
        //#else
        //        /* no HEAVY_MACRO_USAGE */
        //        /*
        //         * And this version of PROCESS_POKEY uses event and channel variables
        //         * so that the PROCESS_CHANNEL macro needs to index memory at runtime.
        //         */

        //        private void PROCESS_POKEY(pokey_state chip, int samples, int[] bufL, int[] bufR, int bufPtr)
        //        {
        //            uint sum = 0;
        //            if (chip.output[CHAN1] != 0 && chip.Muted[CHAN1] == 0)
        //                sum += chip.volume[CHAN1];
        //            if (chip.output[CHAN2] != 0 && chip.Muted[CHAN2] == 0)
        //                sum += chip.volume[CHAN2];
        //            if (chip.output[CHAN3] != 0 && chip.Muted[CHAN3] == 0)
        //                sum += chip.volume[CHAN3];
        //            if (chip.output[CHAN4] != 0 && chip.Muted[CHAN4] == 0)
        //                sum += chip.volume[CHAN4];
        //            while (samples > 0)
        //            {
        //                uint _event = chip.samplepos_whole;
        //                uint channel = unchecked((uint)SAMPLE);
        //                if (chip.counter[CHAN1] < _event)
        //                {

        //                    _event = (uint)chip.counter[CHAN1];
        //                    channel = CHAN1;
        //                }
        //                if (chip.counter[CHAN2] < _event)
        //                {

        //                    _event = (uint)chip.counter[CHAN2];
        //                    channel = CHAN2;
        //                }
        //                if (chip.counter[CHAN3] < _event)
        //                {

        //                    _event = (uint)chip.counter[CHAN3];
        //                    channel = CHAN3;
        //                }
        //                if (chip.counter[CHAN4] < _event)
        //                {

        //                    _event = (uint)chip.counter[CHAN4];
        //                    channel = CHAN4;
        //                }
        //                if (channel == unchecked((uint)SAMPLE))
        //                {
        //                    PROCESS_SAMPLE(chip, ref samples, bufL, bufR, ref bufPtr, _event, sum);
        //                }
        //                else
        //                {
        //                    PROCESS_CHANNEL(chip, (int)channel, _event, sum);
        //                }
        //            }
        //            /*        	chip.rtimer.adjust(attotime::never)*/
        //        }
        //#endif


        /*INLINE pokey_state *get_safe_token(device_t *device)
        {
        	assert(device != NULL);
        	assert(device->type() == POKEY);
        	return (pokey_state *)downcast<legacy_device_base *>(device)->token();
        }*/


        private const int MAX_CHIPS = 0x02;
        private pokey_state[] PokeyData = new pokey_state[MAX_CHIPS];

        //static STREAM_UPDATE( pokey_update )
        private void pokey_update(byte ChipID, int[][] outputs, int samples)
        {
            //pokey_state *chip = (pokey_state *)param;
            pokey_state chip = PokeyData[ChipID];
            //stream_sample_t *buffer = outputs[0];
            int[] bufL = outputs[0];
            int[] bufR = outputs[1];
            int bufPtr = 0;
            PROCESS_POKEY(chip, samples, bufL, bufR, bufPtr);
        }


        private void poly_init(byte[] poly, int size, int left, int right, int add)
        {
            int mask = (1 << size) - 1;
            int i, x = 0;

            LOG_POLY(string.Format("poly {0}\n", size));
            for (i = 0; i < mask; i++)
            {
                poly[i] = (byte)(x & 1);
                LOG_POLY(string.Format("{0:x05}: {1}\n", x, x & 1));
                /* calculate next bit */
                x = ((x << left) + (x >> right) + add) & mask;
            }
        }

        private void rand_init(byte[] rng, int size, int left, int right, int add)
        {
            int mask = (1 << size) - 1;
            int i, x = 0;

            LOG_RAND(string.Format("rand {0}\n", size));
            for (i = 0; i < mask; i++)
            {
                if (size == 17)
                    rng[i] = (byte)(x >> 6);  /* use bits 6..13 */
                else
                    rng[i] = (byte)x;		/* use bits 0..7 */
                LOG_RAND(string.Format("{0:x05}: {1:x02}\n", x, rng[i]));
                //rng++;
                /* calculate next bit */
                x = ((x << left) + (x >> right) + add) & mask;
            }
        }


        /*static void register_for_save(pokey_state *chip, device_t *device)
        {
        	device->save_item(NAME(chip->counter));
        	device->save_item(NAME(chip->divisor));
        	device->save_item(NAME(chip->volume));
        	device->save_item(NAME(chip->output));
        	device->save_item(NAME(chip->audible));
        	device->save_item(NAME(chip->samplepos_fract));
        	device->save_item(NAME(chip->samplepos_whole));
        	device->save_item(NAME(chip->polyadjust));
        	device->save_item(NAME(chip->p4));
        	device->save_item(NAME(chip->p5));
        	device->save_item(NAME(chip->p9));
        	device->save_item(NAME(chip->p17));
        	device->save_item(NAME(chip->r9));
        	device->save_item(NAME(chip->r17));
        	device->save_item(NAME(chip->clockmult));
        	device->save_item(NAME(chip->timer_period[0]));
        	device->save_item(NAME(chip->timer_period[1]));
        	device->save_item(NAME(chip->timer_period[2]));
        	device->save_item(NAME(chip->timer_param));
        	device->save_item(NAME(chip->AUDF));
        	device->save_item(NAME(chip->AUDC));
        	device->save_item(NAME(chip->POTx));
        	device->save_item(NAME(chip->AUDCTL));
        	device->save_item(NAME(chip->ALLPOT));
        	device->save_item(NAME(chip->KBCODE));
        	device->save_item(NAME(chip->RANDOM));
        	device->save_item(NAME(chip->SERIN));
        	device->save_item(NAME(chip->SEROUT));
        	device->save_item(NAME(chip->IRQST));
        	device->save_item(NAME(chip->IRQEN));
        	device->save_item(NAME(chip->SKSTAT));
        	device->save_item(NAME(chip->SKCTL));
        }*/


        //static DEVICE_START( pokey )
        private int device_start_pokey(byte ChipID, int clock)
        {
            //pokey_state *chip = get_safe_token(device);
            pokey_state chip;
            //int sample_rate = device.clock();
            int sample_rate = clock;
            //int i;

            if (ChipID >= MAX_CHIPS)
                return 0;

            if (PokeyData[ChipID] == null)
            {
                PokeyData[ChipID] = new pokey_state();
            }

            chip = PokeyData[ChipID];

            //if (device.static_config())
            //	memcpy(&chip.intf, device.static_config(), sizeof(pokey_interface));
            //chip.device = device;
            //chip.clock_period = attotime::from_hz(device.clock());
            chip.clock_period = 1.0 / clock;

            /* calculate the A/D times
             * In normal, slow mode (SKCTL bit SK_PADDLE is clear) the conversion
             * takes N scanlines, where N is the paddle value. A single scanline
             * takes approximately 64us to finish (1.78979MHz clock).
             * In quick mode (SK_PADDLE set) the conversion is done very fast
             * (takes two scanlines) but the result is not as accurate.
             */
            //chip.ad_time_fast = (attotime::from_nsec(64000*2/228) * FREQ_17_EXACT) / device.clock();
            //chip.ad_time_slow = (attotime::from_nsec(64000      ) * FREQ_17_EXACT) / device.clock();

            /* initialize the poly counters */
            poly_init(chip.poly4, 4, 3, 1, 0x00004);
            poly_init(chip.poly5, 5, 3, 2, 0x00008);
            poly_init(chip.poly9, 9, 8, 1, 0x00180);
            poly_init(chip.poly17, 17, 16, 1, 0x1c000);

            /* initialize the random arrays */
            rand_init(chip.rand9, 9, 8, 1, 0x00180);
            rand_init(chip.rand17, 17, 16, 1, 0x1c000);

            //chip.samplerate_24_8 = (device.clock() << 8) / sample_rate;
            chip.samplerate_24_8 = (uint)((clock << 8) / sample_rate);
            chip.divisor[CHAN1] = 4;
            chip.divisor[CHAN2] = 4;
            chip.divisor[CHAN3] = 4;
            chip.divisor[CHAN4] = 4;
            chip.clockmult = DIV_64;
            chip.KBCODE = 0x09;         /* Atari 800 'no key' */
            chip.SKCTL = SK_RESET;  /* let the RNG run after reset */
            //chip.rtimer = device.machine().scheduler().timer_alloc(FUNC_NULL);

            //chip.timer[0] = device.machine().scheduler().timer_alloc(FUNC(pokey_timer_expire), chip);
            //chip.timer[1] = device.machine().scheduler().timer_alloc(FUNC(pokey_timer_expire), chip);
            //chip.timer[2] = device.machine().scheduler().timer_alloc(FUNC(pokey_timer_expire), chip);

            /*for (i=0; i<8; i++)
        	{
        		chip.ptimer[i] = device.machine().scheduler().timer_alloc(FUNC(pokey_pot_trigger), chip);
        		chip.pot_r[i].resolve(chip.intf.pot_r[i], *device);
        	}
        	chip.allpot_r.resolve(chip.intf.allpot_r, *device);
        	chip.serin_r.resolve(chip.intf.serin_r, *device);
        	chip.serout_w.resolve(chip.intf.serout_w, *device);
        	chip.interrupt_cb = chip.intf.interrupt_cb;*/

            //chip.channel = device.machine().sound().stream_alloc(*device, 0, 1, sample_rate, chip, pokey_update);

            //register_for_save(chip, device);

            return sample_rate;
        }

        private void device_stop_pokey(byte ChipID)
        {
            pokey_state chip = PokeyData[ChipID];

            return;
        }

        private void device_reset_pokey(byte ChipID)
        {
            pokey_state chip = PokeyData[ChipID];
            byte CurChn;

            for (CurChn = 0; CurChn < 4; CurChn++)
            {
                chip.counter[CurChn] = 0;
                chip.divisor[CurChn] = 4;
                chip.volume[CurChn] = 0;
                chip.output[CurChn] = 0;
                chip.audible[CurChn] = 0;
            }
            chip.samplepos_fract = 0;
            chip.samplepos_whole = 0;
            chip.polyadjust = 0;
            chip.p4 = 0;
            chip.p5 = 0;
            chip.p9 = 0;
            chip.p17 = 0;
            chip.r9 = 0;
            chip.r17 = 0;
            chip.clockmult = DIV_64;

            return;
        }

        /*static TIMER_CALLBACK( pokey_timer_expire )
        {
        	pokey_state *p = (pokey_state *)ptr;
        	int timers = param;

        	LOG_TIMER(("POKEY #%p timer %d with IRQEN $%02x\n", p, timers, p->IRQEN));

            // check if some of the requested timer interrupts are enabled //
        	timers &= p->IRQEN;

            if( timers )
            {
        		// set the enabled timer irq status bits //
        		p->IRQST |= timers;
                // call back an application supplied function to handle the interrupt //
        		if( p->interrupt_cb )
        			(*p->interrupt_cb)(p->device, timers);
            }
        }*/

        /*static char *audc2str(int val)
        {
        	static char buff[80];
        	if( val & NOTPOLY5 )
        	{
        		if( val & PURE )
        			strcpy(buff,"pure");
        		else
        		if( val & POLY4 )
        			strcpy(buff,"poly4");
        		else
        			strcpy(buff,"poly9/17");
        	}
        	else
        	{
        		if( val & PURE )
        			strcpy(buff,"poly5");
        		else
        		if( val & POLY4 )
        			strcpy(buff,"poly4+poly5");
        		else
        			strcpy(buff,"poly9/17+poly5");
            }
        	return buff;
        }

        static char *audctl2str(int val)
        {
        	static char buff[80];
        	if( val & POLY9 )
        		strcpy(buff,"poly9");
        	else
        		strcpy(buff,"poly17");
        	if( val & CH1_HICLK )
        		strcat(buff,"+ch1hi");
        	if( val & CH3_HICLK )
        		strcat(buff,"+ch3hi");
        	if( val & CH12_JOINED )
        		strcat(buff,"+ch1/2");
        	if( val & CH34_JOINED )
        		strcat(buff,"+ch3/4");
        	if( val & CH1_FILTER )
        		strcat(buff,"+ch1filter");
        	if( val & CH2_FILTER )
        		strcat(buff,"+ch2filter");
        	if( val & CLK_15KHZ )
        		strcat(buff,"+clk15");
            return buff;
        }*/

        /*static TIMER_CALLBACK( pokey_serin_ready_cb )
        {
        	pokey_state *p = (pokey_state *)ptr;
            if( p->IRQEN & IRQ_SERIN )
        	{
        		// set the enabled timer irq status bits //
        		p->IRQST |= IRQ_SERIN;
        		// call back an application supplied function to handle the interrupt //
        		if( p->interrupt_cb )
        			(*p->interrupt_cb)(p->device, IRQ_SERIN);
        	}
        }

        static TIMER_CALLBACK( pokey_serout_ready_cb )
        {
        	pokey_state *p = (pokey_state *)ptr;
            if( p->IRQEN & IRQ_SEROR )
        	{
        		p->IRQST |= IRQ_SEROR;
        		if( p->interrupt_cb )
        			(*p->interrupt_cb)(p->device, IRQ_SEROR);
        	}
        }

        static TIMER_CALLBACK( pokey_serout_complete )
        {
        	pokey_state *p = (pokey_state *)ptr;
            if( p->IRQEN & IRQ_SEROC )
        	{
        		p->IRQST |= IRQ_SEROC;
        		if( p->interrupt_cb )
        			(*p->interrupt_cb)(p->device, IRQ_SEROC);
        	}
        }

        static TIMER_CALLBACK( pokey_pot_trigger )
        {
        	pokey_state *p = (pokey_state *)ptr;
        	int pot = param;
        	LOG(("POKEY #%p POT%d triggers after %dus\n", p, pot, (int)(1000000 * p->ptimer[pot]->elapsed().as_double())));
        	p->ALLPOT &= ~(1 << pot);	// set the enabled timer irq status bits //
        }*/

        /*#define AD_TIME  ((p->SKCTL & SK_PADDLE) ? p->ad_time_fast : p->ad_time_slow)

        static void pokey_potgo(pokey_state *p)
        {
            int pot;

        	LOG(("POKEY #%p pokey_potgo\n", p));

            p->ALLPOT = 0xff;

            for( pot = 0; pot < 8; pot++ )
        	{
        		p->POTx[pot] = 0xff;
        		if( !p->pot_r[pot].isnull() )
        		{
        			int r = p->pot_r[pot](pot);

        			LOG(("POKEY %s pot_r(%d) returned $%02x\n", p->device->tag(), pot, r));
        			if( r != -1 )
        			{
        				if (r > 228)
                            r = 228;

                        // final value //
                        p->POTx[pot] = r;
        				p->ptimer[pot]->adjust(AD_TIME * r, pot);
        			}
        		}
        	}
        }*/

        //READ8_DEVICE_HANDLER( pokey_r )
        private byte pokey_r(byte ChipID, int offset)
        {
            //pokey_state *p = get_safe_token(device);
            pokey_state p = PokeyData[ChipID];
            int data = 0, pot;
            uint adjust = 0;

            switch (offset & 15)
            {
                case POT0_C:
                case POT1_C:
                case POT2_C:
                case POT3_C:
                case POT4_C:
                case POT5_C:
                case POT6_C:
                case POT7_C:
                    pot = offset & 7;
                    /*if( !p->pot_r[pot].isnull() )
                    {
                        //
                         * If the conversion is not yet finished (ptimer running),
                         * get the current value by the linear interpolation of
                         * the final value using the elapsed time.
                         //
                        if( p->ALLPOT & (1 << pot) )
                        {
                            //data = p->ptimer[pot]->elapsed().attoseconds / AD_TIME.attoseconds;
                            data = p->POTx[pot];
                            LOG(("POKEY '%s' read POT%d (interpolated) $%02x\n", p->device->tag(), pot, data));
                        }
                        else
                        {
                            data = p->POTx[pot];
                            LOG(("POKEY '%s' read POT%d (final value)  $%02x\n", p->device->tag(), pot, data));
                        }
                    }
                    else
                        logerror("%s: warning - read '%s' POT%d\n", p->device->machine().describe_context(), p->device->tag(), pot);*/
                    break;

                case ALLPOT_C:
                    /****************************************************************
                     * If the 2 least significant bits of SKCTL are 0, the ALLPOTs
                     * are disabled (SKRESET). Thanks to MikeJ for pointing this out.
                     ****************************************************************/
                    /*if( (p->SKCTL & SK_RESET) == 0)
                    {
                        data = 0;
                        LOG(("POKEY '%s' ALLPOT internal $%02x (reset)\n", p->device->tag(), data));
                    }
                    else if( !p->allpot_r.isnull() )
                    {
                        data = p->allpot_r(offset);
                        LOG(("POKEY '%s' ALLPOT callback $%02x\n", p->device->tag(), data));
                    }
                    else
                    {
                        data = p->ALLPOT;
                        LOG(("POKEY '%s' ALLPOT internal $%02x\n", p->device->tag(), data));
                    }*/
                    break;

                case KBCODE_C:
                    data = p.KBCODE;
                    break;

                case RANDOM_C:
                    /****************************************************************
                     * If the 2 least significant bits of SKCTL are 0, the random
                     * number generator is disabled (SKRESET). Thanks to Eric Smith
                     * for pointing out this critical bit of info! If the random
                     * number generator is enabled, get a new random number. Take
                     * the time gone since the last read into account and read the
                     * new value from an appropriate offset in the rand17 table.
                     ****************************************************************/
                    if ((p.SKCTL & SK_RESET) != 0)
                    {
                        //adjust = p.rtimer.elapsed().as_double() / p.clock_period.as_double();
                        adjust = 0;
                        p.r9 = (p.r9 + adjust) % 0x001ff;
                        p.r17 = (p.r17 + adjust) % 0x1ffff;
                    }
                    else
                    {
                        adjust = 1;
                        p.r9 = 0;
                        p.r17 = 0;
                        //LOG_RAND(("POKEY '%s' rand17 frozen (SKCTL): $%02x\n", p.device.tag(), p.RANDOM));
                    }
                    if ((p.AUDCTL & POLY9) != 0)
                    {
                        p.RANDOM = p.rand9[p.r9];
                        //LOG_RAND(("POKEY '%s' adjust %u rand9[$%05x]: $%02x\n", p.device.tag(), adjust, p.r9, p.RANDOM));
                    }
                    else
                    {
                        p.RANDOM = p.rand17[p.r17];
                        //LOG_RAND(("POKEY '%s' adjust %u rand17[$%05x]: $%02x\n", p.device.tag(), adjust, p.r17, p.RANDOM));
                    }
                    //if (adjust > 0)
                    //	p.rtimer.adjust(attotime::never);
                    data = p.RANDOM ^ 0xff;
                    break;

                case SERIN_C:
                    //if( !p.serin_r.isnull() )
                    //	p.SERIN = p.serin_r(offset);
                    data = p.SERIN;
                    //LOG(("POKEY '%s' SERIN  $%02x\n", p.device.tag(), data));
                    break;

                case IRQST_C:
                    /* IRQST is an active low input port; we keep it active high */
                    /* internally to ease the (un-)masking of bits */
                    data = p.IRQST ^ 0xff;
                    //LOG(("POKEY '%s' IRQST  $%02x\n", p.device.tag(), data));
                    break;

                case SKSTAT_C:
                    /* SKSTAT is also an active low input port */
                    data = p.SKSTAT ^ 0xff;
                    //LOG(("POKEY '%s' SKSTAT $%02x\n", p.device.tag(), data));
                    break;

                default:
                    //LOG(("POKEY '%s' register $%02x\n", p->device->tag(), offset));
                    break;
            }

            return (byte)data;
        }

        /*READ8_HANDLER( quad_pokey_r )
        {
        	static const char *const devname[4] = { "pokey1", "pokey2", "pokey3", "pokey4" };
        	int pokey_num = (offset >> 3) & ~0x04;
        	int control = (offset & 0x20) >> 2;
        	int pokey_reg = (offset % 8) | control;

        	return pokey_r(space->machine().device(devname[pokey_num]), pokey_reg);
        }*/


        //WRITE8_DEVICE_HANDLER( pokey_w )
        private void pokey_w(byte ChipID, int offset, byte data)
        {
            //pokey_state *p = get_safe_token(device);
            pokey_state p = PokeyData[ChipID];
            int ch_mask = 0, new_val;

            //p.channel.update();

            /* determine which address was changed */
            switch (offset & 15)
            {
                case AUDF1_C:
                    if (data == p.AUDF[CHAN1])
                        return;
                    //LOG_SOUND(("POKEY '%s' AUDF1  $%02x\n", p.device.tag(), data));
                    p.AUDF[CHAN1] = data;
                    ch_mask = 1 << CHAN1;
                    if ((p.AUDCTL & CH12_JOINED) != 0)        /* if ch 1&2 tied together */
                        ch_mask |= 1 << CHAN2;    /* then also change on ch2 */
                    break;

                case AUDC1_C:
                    if (data == p.AUDC[CHAN1])
                        return;
                    //LOG_SOUND(("POKEY '%s' AUDC1  $%02x (%s)\n", p.device.tag(), data, audc2str(data)));
                    p.AUDC[CHAN1] = data;
                    ch_mask = 1 << CHAN1;
                    break;

                case AUDF2_C:
                    if (data == p.AUDF[CHAN2])
                        return;
                    //LOG_SOUND(("POKEY '%s' AUDF2  $%02x\n", p.device.tag(), data));
                    p.AUDF[CHAN2] = data;
                    ch_mask = 1 << CHAN2;
                    break;

                case AUDC2_C:
                    if (data == p.AUDC[CHAN2])
                        return;
                    //LOG_SOUND(("POKEY '%s' AUDC2  $%02x (%s)\n", p.device.tag(), data, audc2str(data)));
                    p.AUDC[CHAN2] = data;
                    ch_mask = 1 << CHAN2;
                    break;

                case AUDF3_C:
                    if (data == p.AUDF[CHAN3])
                        return;
                    //LOG_SOUND(("POKEY '%s' AUDF3  $%02x\n", p.device.tag(), data));
                    p.AUDF[CHAN3] = data;
                    ch_mask = 1 << CHAN3;

                    if ((p.AUDCTL & CH34_JOINED) != 0)    /* if ch 3&4 tied together */
                        ch_mask |= 1 << CHAN4;  /* then also change on ch4 */
                    break;

                case AUDC3_C:
                    if (data == p.AUDC[CHAN3])
                        return;
                    //LOG_SOUND(("POKEY '%s' AUDC3  $%02x (%s)\n", p.device.tag(), data, audc2str(data)));
                    p.AUDC[CHAN3] = data;
                    ch_mask = 1 << CHAN3;
                    break;

                case AUDF4_C:
                    if (data == p.AUDF[CHAN4])
                        return;
                    //LOG_SOUND(("POKEY '%s' AUDF4  $%02x\n", p.device.tag(), data));
                    p.AUDF[CHAN4] = data;
                    ch_mask = 1 << CHAN4;
                    break;

                case AUDC4_C:
                    if (data == p.AUDC[CHAN4])
                        return;
                    //LOG_SOUND(("POKEY '%s' AUDC4  $%02x (%s)\n", p.device.tag(), data, audc2str(data)));
                    p.AUDC[CHAN4] = data;
                    ch_mask = 1 << CHAN4;
                    break;

                case AUDCTL_C:
                    if (data == p.AUDCTL)
                        return;
                    //LOG_SOUND(("POKEY '%s' AUDCTL $%02x (%s)\n", p.device.tag(), data, audctl2str(data)));
                    p.AUDCTL = data;
                    ch_mask = 15;       /* all channels */
                    /* determine the base multiplier for the 'div by n' calculations */
                    p.clockmult = (uint)((p.AUDCTL & CLK_15KHZ) != 0 ? DIV_15 : DIV_64);
                    break;

                case STIMER_C:
                    /*// first remove any existing timers //
                    LOG_TIMER(("POKEY '%s' STIMER $%02x\n", p.device.tag(), data));

                    p.timer[TIMER1].adjust(attotime::never, p.timer_param[TIMER1]);
                    p.timer[TIMER2].adjust(attotime::never, p.timer_param[TIMER2]);
                    p.timer[TIMER4].adjust(attotime::never, p.timer_param[TIMER4]);

                    // reset all counters to zero (side effect) //
                    p.polyadjust = 0;
                    p.counter[CHAN1] = 0;
                    p.counter[CHAN2] = 0;
                    p.counter[CHAN3] = 0;
                    p.counter[CHAN4] = 0;

                    // joined chan#1 and chan#2 ? //
                    if( p.AUDCTL & CH12_JOINED )
                    {
                        if( p.divisor[CHAN2] > 4 )
                        {
                            LOG_TIMER(("POKEY '%s' timer1+2 after %d clocks\n", p.device.tag(), p.divisor[CHAN2]));
                            // set timer #1 _and_ #2 event after timer_div clocks of joined CHAN1+CHAN2 //
                            p.timer_period[TIMER2] = p.clock_period * p.divisor[CHAN2];
                            p.timer_param[TIMER2] = IRQ_TIMR2|IRQ_TIMR1;
                            p.timer[TIMER2].adjust(p.timer_period[TIMER2], p.timer_param[TIMER2], p.timer_period[TIMER2]);
                        }
                    }
                    else
                    {
                        if( p.divisor[CHAN1] > 4 )
                        {
                            LOG_TIMER(("POKEY '%s' timer1 after %d clocks\n", p.device.tag(), p.divisor[CHAN1]));
                            // set timer #1 event after timer_div clocks of CHAN1 //
                            p.timer_period[TIMER1] = p.clock_period * p.divisor[CHAN1];
                            p.timer_param[TIMER1] = IRQ_TIMR1;
                            p.timer[TIMER1].adjust(p.timer_period[TIMER1], p.timer_param[TIMER1], p.timer_period[TIMER1]);
                        }

                        if( p.divisor[CHAN2] > 4 )
                        {
                            LOG_TIMER(("POKEY '%s' timer2 after %d clocks\n", p.device.tag(), p.divisor[CHAN2]));
                            // set timer #2 event after timer_div clocks of CHAN2 //
                            p.timer_period[TIMER2] = p.clock_period * p.divisor[CHAN2];
                            p.timer_param[TIMER2] = IRQ_TIMR2;
                            p.timer[TIMER2].adjust(p.timer_period[TIMER2], p.timer_param[TIMER2], p.timer_period[TIMER2]);
                        }
                    }

                    // Note: p[chip] does not have a timer #3 //

                    if( p.AUDCTL & CH34_JOINED )
                    {
                        // not sure about this: if audc4 == 0000xxxx don't start timer 4 ? //
                        if( p.AUDC[CHAN4] & 0xf0 )
                        {
                            if( p.divisor[CHAN4] > 4 )
                            {
                                LOG_TIMER(("POKEY '%s' timer4 after %d clocks\n", p.device.tag(), p.divisor[CHAN4]));
                                // set timer #4 event after timer_div clocks of CHAN4 //
                                p.timer_period[TIMER4] = p.clock_period * p.divisor[CHAN4];
                                p.timer_param[TIMER4] = IRQ_TIMR4;
                                p.timer[TIMER4].adjust(p.timer_period[TIMER4], p.timer_param[TIMER4], p.timer_period[TIMER4]);
                            }
                        }
                    }
                    else
                    {
                        if( p.divisor[CHAN4] > 4 )
                        {
                            LOG_TIMER(("POKEY '%s' timer4 after %d clocks\n", p.device.tag(), p.divisor[CHAN4]));
                            // set timer #4 event after timer_div clocks of CHAN4 //
                            p.timer_period[TIMER4] = p.clock_period * p.divisor[CHAN4];
                            p.timer_param[TIMER4] = IRQ_TIMR4;
                            p.timer[TIMER4].adjust(p.timer_period[TIMER4], p.timer_param[TIMER4], p.timer_period[TIMER4]);
                        }
                    }

                    p.timer[TIMER1].enable(p.IRQEN & IRQ_TIMR1);
                    p.timer[TIMER2].enable(p.IRQEN & IRQ_TIMR2);
                    p.timer[TIMER4].enable(p.IRQEN & IRQ_TIMR4);*/
                    break;

                case SKREST_C:
                    /* reset SKSTAT */
                    //LOG(("POKEY '%s' SKREST $%02x\n", p.device.tag(), data));
                    p.SKSTAT &= unchecked((byte)~(SK_FRAME | SK_OVERRUN | SK_KBERR));
                    break;

                case POTGO_C:
                    //LOG(("POKEY '%s' POTGO  $%02x\n", p.device.tag(), data));
                    //pokey_potgo(p);
                    break;

                case SEROUT_C:
                    //LOG(("POKEY '%s' SEROUT $%02x\n", p.device.tag(), data));
                    //p.serout_w(offset, data);
                    //p.SKSTAT |= SK_SEROUT;
                    /*
                     * These are arbitrary values, tested with some custom boot
                     * loaders from Ballblazer and Escape from Fractalus
                     * The real times are unknown
                     */
                    //device.machine().scheduler().timer_set(attotime::from_usec(200), FUNC(pokey_serout_ready_cb), 0, p);
                    /* 10 bits (assumption 1 start, 8 data and 1 stop bit) take how long? */
                    //device.machine().scheduler().timer_set(attotime::from_usec(2000), FUNC(pokey_serout_complete), 0, p);
                    break;

                case IRQEN_C:
                    //LOG(("POKEY '%s' IRQEN  $%02x\n", p.device.tag(), data));

                    /* acknowledge one or more IRQST bits ? */
                    if ((p.IRQST & ~data) != 0)
                    {
                        /* reset IRQST bits that are masked now */
                        p.IRQST &= data;
                    }
                    else
                    {
                        /* enable/disable timers now to avoid unneeded
                           breaking of the CPU cores for masked timers */
                        /*if( p.timer[TIMER1] && ((p.IRQEN^data) & IRQ_TIMR1) )
                            p.timer[TIMER1].enable(data & IRQ_TIMR1);
                        if( p.timer[TIMER2] && ((p.IRQEN^data) & IRQ_TIMR2) )
                            p.timer[TIMER2].enable(data & IRQ_TIMR2);
                        if( p.timer[TIMER4] && ((p.IRQEN^data) & IRQ_TIMR4) )
                            p.timer[TIMER4].enable(data & IRQ_TIMR4);*/
                    }
                    /* store irq enable */
                    p.IRQEN = data;
                    break;

                case SKCTL_C:
                    if (data == p.SKCTL)
                        return;
                    //LOG(("POKEY '%s' SKCTL  $%02x\n", p.device.tag(), data));
                    p.SKCTL = data;
                    if ((data & SK_RESET) == 0)
                    {
                        pokey_w(ChipID, IRQEN_C, 0);
                        pokey_w(ChipID, SKREST_C, 0);
                    }
                    break;
            }

            /************************************************************
             * As defined in the manual, the exact counter values are
             * different depending on the frequency and resolution:
             *    64 kHz or 15 kHz - AUDF + 1
             *    1.79 MHz, 8-bit  - AUDF + 4
             *    1.79 MHz, 16-bit - AUDF[CHAN1]+256*AUDF[CHAN2] + 7
             ************************************************************/

            /* only reset the channels that have changed */

            if ((ch_mask & (1 << CHAN1)) != 0)
            {
                /* process channel 1 frequency */
                if ((p.AUDCTL & CH1_HICLK) != 0)
                    new_val = p.AUDF[CHAN1] + DIVADD_HICLK;
                else
                    new_val = (int)((p.AUDF[CHAN1] + DIVADD_LOCLK) * p.clockmult);

                //LOG_SOUND(("POKEY '%s' chan1 %d\n", p.device.tag(), new_val));

                p.volume[CHAN1] = (uint)((p.AUDC[CHAN1] & VOLUME_MASK) * POKEY_DEFAULT_GAIN);
                p.divisor[CHAN1] = new_val;
                if (new_val < p.counter[CHAN1])
                    p.counter[CHAN1] = new_val;
                //if( p.interrupt_cb && p.timer[TIMER1] )
                //	p.timer[TIMER1].adjust(p.clock_period * new_val, p.timer_param[TIMER1], p.timer_period[TIMER1]);
                p.audible[CHAN1] = (byte)(
                    (
                        (p.AUDC[CHAN1] & VOLUME_ONLY) != 0 ||
                        (p.AUDC[CHAN1] & VOLUME_MASK) == 0 ||
                        ((p.AUDC[CHAN1] & PURE) != 0 && new_val < (p.samplerate_24_8 >> 8))
                    )
                    ? 0 : 1
                    );
                if (p.audible[CHAN1] == 0)
                {
                    p.output[CHAN1] = 1;
                    p.counter[CHAN1] = 0x7fffffff;
                    /* 50% duty cycle should result in half volume */
                    p.volume[CHAN1] >>= 1;
                }
            }

            if ((ch_mask & (1 << CHAN2)) != 0)
            {
                /* process channel 2 frequency */
                if ((p.AUDCTL & CH12_JOINED) != 0)
                {
                    if ((p.AUDCTL & CH1_HICLK) != 0)
                        new_val = p.AUDF[CHAN2] * 256 + p.AUDF[CHAN1] + DIVADD_HICLK_JOINED;
                    else
                        new_val = (int)((p.AUDF[CHAN2] * 256 + p.AUDF[CHAN1] + DIVADD_LOCLK) * p.clockmult);
                    //LOG_SOUND(("POKEY '%s' chan1+2 %d\n", p.device.tag(), new_val));
                }
                else
                {
                    new_val = (int)((p.AUDF[CHAN2] + DIVADD_LOCLK) * p.clockmult);
                    //LOG_SOUND(("POKEY '%s' chan2 %d\n", p.device.tag(), new_val));
                }

                p.volume[CHAN2] = (uint)((p.AUDC[CHAN2] & VOLUME_MASK) * POKEY_DEFAULT_GAIN);
                p.divisor[CHAN2] = new_val;
                if (new_val < p.counter[CHAN2])
                    p.counter[CHAN2] = new_val;
                //if( p.interrupt_cb && p.timer[TIMER2] )
                //	p.timer[TIMER2].adjust(p.clock_period * new_val, p.timer_param[TIMER2], p.timer_period[TIMER2]);
                p.audible[CHAN2] = (byte)(
                    (
                        (p.AUDC[CHAN2] & VOLUME_ONLY) != 0 ||
                        (p.AUDC[CHAN2] & VOLUME_MASK) == 0 ||
                        ((p.AUDC[CHAN2] & PURE) != 0 && new_val < (p.samplerate_24_8 >> 8))
                    )
                    ? 0 : 1
                    );
                if (p.audible[CHAN2] == 0)
                {
                    p.output[CHAN2] = 1;
                    p.counter[CHAN2] = 0x7fffffff;
                    /* 50% duty cycle should result in half volume */
                    p.volume[CHAN2] >>= 1;
                }
            }

            if ((ch_mask & (1 << CHAN3)) != 0)
            {
                /* process channel 3 frequency */
                if ((p.AUDCTL & CH3_HICLK) != 0)
                    new_val = p.AUDF[CHAN3] + DIVADD_HICLK;
                else
                    new_val = (int)((p.AUDF[CHAN3] + DIVADD_LOCLK) * p.clockmult);

                //LOG_SOUND(("POKEY '%s' chan3 %d\n", p.device.tag(), new_val));

                p.volume[CHAN3] = (uint)((p.AUDC[CHAN3] & VOLUME_MASK) * POKEY_DEFAULT_GAIN);
                p.divisor[CHAN3] = new_val;
                if (new_val < p.counter[CHAN3])
                    p.counter[CHAN3] = new_val;
                /* channel 3 does not have a timer associated */
                p.audible[CHAN3] = (byte)(
                        (
                            !(
                                (p.AUDC[CHAN3] & VOLUME_ONLY) != 0 ||
                                (p.AUDC[CHAN3] & VOLUME_MASK) == 0 ||
                                ((p.AUDC[CHAN3] & PURE) != 0 && new_val < (p.samplerate_24_8 >> 8))
                            )
                            ||
                            (p.AUDCTL & CH1_FILTER) != 0
                        ) ? 1 : 0
                    );
                if (p.audible[CHAN3] == 0)
                {
                    p.output[CHAN3] = 1;
                    p.counter[CHAN3] = 0x7fffffff;
                    /* 50% duty cycle should result in half volume */
                    p.volume[CHAN3] >>= 1;
                }
            }

            if ((ch_mask & (1 << CHAN4)) != 0)
            {
                /* process channel 4 frequency */
                if ((p.AUDCTL & CH34_JOINED) != 0)
                {
                    if ((p.AUDCTL & CH3_HICLK) != 0)
                        new_val = p.AUDF[CHAN4] * 256 + p.AUDF[CHAN3] + DIVADD_HICLK_JOINED;
                    else
                        new_val = (int)((p.AUDF[CHAN4] * 256 + p.AUDF[CHAN3] + DIVADD_LOCLK) * p.clockmult);
                    //LOG_SOUND(("POKEY '%s' chan3+4 %d\n", p.device.tag(), new_val));
                }
                else
                {
                    new_val = (int)((p.AUDF[CHAN4] + DIVADD_LOCLK) * p.clockmult);
                    //LOG_SOUND(("POKEY '%s' chan4 %d\n", p.device.tag(), new_val));
                }

                p.volume[CHAN4] = (uint)((p.AUDC[CHAN4] & VOLUME_MASK) * POKEY_DEFAULT_GAIN);
                p.divisor[CHAN4] = new_val;
                if (new_val < p.counter[CHAN4])
                    p.counter[CHAN4] = new_val;
                //if( p.interrupt_cb && p.timer[TIMER4] )
                //	p.timer[TIMER4].adjust(p.clock_period * new_val, p.timer_param[TIMER4], p.timer_period[TIMER4]);
                p.audible[CHAN4] = (byte)(
                    (
                        !(
                            (p.AUDC[CHAN4] & VOLUME_ONLY) != 0 ||
                            (p.AUDC[CHAN4] & VOLUME_MASK) == 0 ||
                            (
                                (p.AUDC[CHAN4] & PURE) != 0 && new_val < (p.samplerate_24_8 >> 8)
                            )
                        ) ||
                        (p.AUDCTL & CH2_FILTER) != 0
                        ) ? 1 : 0
                    );

                if (p.audible[CHAN4] == 0)
                {
                    p.output[CHAN4] = 1;
                    p.counter[CHAN4] = 0x7fffffff;
                    /* 50% duty cycle should result in half volume */
                    p.volume[CHAN4] >>= 1;
                }
            }
        }

        /*WRITE8_HANDLER( quad_pokey_w )
        {
        	static const char *const devname[4] = { "pokey1", "pokey2", "pokey3", "pokey4" };
            int pokey_num = (offset >> 3) & ~0x04;
            int control = (offset & 0x20) >> 2;
            int pokey_reg = (offset % 8) | control;

            pokey_w(space->machine().device(devname[pokey_num]), pokey_reg, data);
        }

        void pokey_serin_ready(device_t *device, int after)
        {
        	pokey_state *p = get_safe_token(device);
        	device->machine().scheduler().timer_set(p->clock_period * after, FUNC(pokey_serin_ready_cb), 0, p);
        }

        void pokey_break_w(device_t *device, int shift)
        {
        	//pokey_state *p = get_safe_token(device);
        	if( shift )                     // shift code ? //
        		p->SKSTAT |= SK_SHIFT;
        	else
        		p->SKSTAT &= ~SK_SHIFT;
        	// check if the break IRQ is enabled //
        	if( p->IRQEN & IRQ_BREAK )
        	{
        		// set break IRQ status and call back the interrupt handler //
        		p->IRQST |= IRQ_BREAK;
        		if( p->interrupt_cb )
        			(*p->interrupt_cb)(device, IRQ_BREAK);
        	}
        }

        void pokey_kbcode_w(device_t *device, int kbcode, int make)
        {
        	pokey_state *p = get_safe_token(device);
            // make code ? //
        	if( make )
        	{
        		p->KBCODE = kbcode;
        		p->SKSTAT |= SK_KEYBD;
        		if( kbcode & 0x40 ) 		// shift code ? //
        			p->SKSTAT |= SK_SHIFT;
        		else
        			p->SKSTAT &= ~SK_SHIFT;

        		if( p->IRQEN & IRQ_KEYBD )
        		{
        			// last interrupt not acknowledged ? //
        			if( p->IRQST & IRQ_KEYBD )
        				p->SKSTAT |= SK_KBERR;
        			p->IRQST |= IRQ_KEYBD;
        			if( p->interrupt_cb )
        				(*p->interrupt_cb)(device, IRQ_KEYBD);
        		}
        	}
        	else
        	{
        		p->KBCODE = kbcode;
        		p->SKSTAT &= ~SK_KEYBD;
            }
        }*/


        private void pokey_set_mute_mask(byte ChipID, uint MuteMask)
        {
            pokey_state chip = PokeyData[ChipID];
            byte CurChn;

            for (CurChn = 0; CurChn < 4; CurChn++)
                chip.Muted[CurChn] = (byte)((MuteMask >> CurChn) & 0x01);

            return;
        }




        /**************************************************************************
         * Generic get_info
         **************************************************************************/

        /*DEVICE_GET_INFO( pokey )
        {
        	switch (state)
        	{
        		// --- the following bits of info are returned as 64-bit signed integers --- //
        		case DEVINFO_INT_TOKEN_BYTES:					info->i = sizeof(pokey_state);		break;

        		// --- the following bits of info are returned as pointers to data or functions --- //
        		case DEVINFO_FCT_START:							info->start = DEVICE_START_NAME( pokey );			break;
        		case DEVINFO_FCT_STOP:							// Nothing //									break;
        		case DEVINFO_FCT_RESET:							// Nothing //									break;

        		// --- the following bits of info are returned as NULL-terminated strings --- //
        		case DEVINFO_STR_NAME:							strcpy(info->s, "POKEY");						break;
        		case DEVINFO_STR_FAMILY:					strcpy(info->s, "Atari custom");				break;
        		case DEVINFO_STR_VERSION:					strcpy(info->s, "4.51");						break;
        		case DEVINFO_STR_SOURCE_FILE:						strcpy(info->s, __FILE__);						break;
        		case DEVINFO_STR_CREDITS:					strcpy(info->s, "Copyright Nicola Salmoria and the MAME Team"); break;
        	}
        }


        DEFINE_LEGACY_SOUND_DEVICE(POKEY, pokey);*/




    }
}
