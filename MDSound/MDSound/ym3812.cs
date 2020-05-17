using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDSound
{
    public class ym3812 : Instrument
    {
        public override string Name { get { return "YM3812"; } set { } }
        public override string ShortName { get { return "OPL2"; } set { } }

        public override void Reset(byte ChipID)
        {
            device_reset_ym3812(ChipID);
            visVolume = new int[2][][] {
                new int[1][] { new int[2] { 0, 0 } }
                , new int[1][] { new int[2] { 0, 0 } }
            };
        }

        private const uint DefaultYM3812ClockValue = 3579545;

        public override uint Start(byte ChipID, uint clock)
        {
            return Start(ChipID, DefaultYM3812ClockValue, 44100, null);
        }

        public override uint Start(byte ChipID, uint clock, uint ClockValue, params object[] option)
        {
            return (uint)device_start_ym3812(ChipID, (int)ClockValue);
        }

        public override void Stop(byte ChipID)
        {
            device_stop_ym3812(ChipID);
        }

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {
            ym3812_stream_update(ChipID, outputs, samples);

            visVolume[ChipID][0][0] = outputs[0][0];
            visVolume[ChipID][0][1] = outputs[1][0];
        }

        public override int Write(byte ChipID, int port, int adr, int data)
        {
            ym3812_control_port_w(ChipID, 0, (byte)adr);
            ym3812_write_port_w(ChipID, 0, (byte)data);
            return 0;
        }


        //3812intf.h
        //#pragma once

        //        /*typedef struct _ym3812_interface ym3812_interface;
        //        struct _ym3812_interface
        //        {
        //            //void (*handler)(const device_config *device, int linestate);
        //            void (*handler)(int linestate);
        //        };*/

        //        /*READ8_DEVICE_HANDLER( ym3812_r );
        //        WRITE8_DEVICE_HANDLER( ym3812_w );
        //        READ8_DEVICE_HANDLER( ym3812_status_port_r );
        //        READ8_DEVICE_HANDLER( ym3812_read_port_r );
        //        WRITE8_DEVICE_HANDLER( ym3812_control_port_w );
        //        WRITE8_DEVICE_HANDLER( ym3812_write_port_w );
        //        DEVICE_GET_INFO( ym3812 );
        //        #define SOUND_YM3812 DEVICE_GET_INFO_NAME( ym3812 )*/

        //        void ym3812_stream_update(UINT8 ChipID, stream_sample_t** outputs, int samples);
        //        int device_start_ym3812(UINT8 ChipID, int clock);
        //        void device_stop_ym3812(UINT8 ChipID);
        //        void device_reset_ym3812(UINT8 ChipID);

        //        UINT8 ym3812_r(UINT8 ChipID, offs_t offset);
        //        void ym3812_w(UINT8 ChipID, offs_t offset, UINT8 data);

        //        UINT8 ym3812_status_port_r(UINT8 ChipID, offs_t offset);
        //        UINT8 ym3812_read_port_r(UINT8 ChipID, offs_t offset);
        //        void ym3812_control_port_w(UINT8 ChipID, offs_t offset, UINT8 data);
        //        void ym3812_write_port_w(UINT8 ChipID, offs_t offset, UINT8 data);

        //        void ym3812_set_emu_core(UINT8 Emulator);
        //        void ym3812_set_mute_mask(UINT8 ChipID, UINT32 MuteMask);



        //3812intf.c
        /******************************************************************************
        * FILE
        *   Yamaha 3812 emulator interface - MAME VERSION
        *
        * CREATED BY
        *   Ernesto Corvi
        *
        * UPDATE LOG
        *   JB  28-04-2002  Fixed simultaneous usage of all three different chip types.
        *                       Used real sample rate when resample filter is active.
        *       AAT 12-28-2001  Protected Y8950 from accessing unmapped port and keyboard handlers.
        *   CHS 1999-01-09  Fixes new ym3812 emulation interface.
        *   CHS 1998-10-23  Mame streaming sound chip update
        *   EC  1998        Created Interface
        *
        * NOTES
        *
        ******************************************************************************/
        //# include <stddef.h>	// for NULL
        //# include "mamedef.h"
        //#include "attotime.h"
        //#include "sndintrf.h"
        //#include "streams.h"
        //#include "cpuintrf.h"
        //# include "3812intf.h"
        //# ifdef ENABLE_ALL_CORES
        //# include "fmopl.h"
        //#endif

        //#define OPLTYPE_IS_OPL2
        //# include "adlibemu.h"


        private const int EC_DBOPL = 0x00;	// DosBox OPL (AdLibEmu)
        //# ifdef ENABLE_ALL_CORES
        private const int EC_MAME = 0x01;	// YM3826 core from MAME
        //#endif

        //typedef struct _ym3812_state ym3812_state;
        //struct _ym3812_state
        public class ym3812_state
        {
            //sound_stream *	stream;
            //emu_timer *		timer[2];
            public OPL_DATA chip;
            //const ym3812_interface *intf;
            //const device_config *device;
        }


        //public byte CHIP_SAMPLING_MODE;
        //public Int32 CHIP_SAMPLE_RATE;
        private byte EMU_CORE = 0x00;

        private const int MAX_CHIPS = 0x02;
        private ym3812_state[] YM3812Data = new ym3812_state[MAX_CHIPS] { new ym3812_state(), new ym3812_state() };

        /*INLINE ym3812_state *get_safe_token(const device_config *device)
        {
            assert(device != NULL);
            assert(device->token != NULL);
            assert(device->type == SOUND);
            assert(sound_get_type(device) == SOUND_YM3812);
            return (ym3812_state *)device->token;
        }*/

        public delegate void ADL_UPDATEHANDLER(ym3812_state param);


        private void IRQHandler(ym3812_state param, int irq)
        {
            ym3812_state info = param;
            //if (info->intf->handler) (info->intf->handler)(info->device, irq ? ASSERT_LINE : CLEAR_LINE);
            //if (info->intf->handler) (info->intf->handler)(irq ? ASSERT_LINE : CLEAR_LINE);
        }

        /*static TIMER_CALLBACK( timer_callback_0 )
        {
            ym3812_state *info = (ym3812_state *)ptr;
            ym3812_timer_over(info->chip,0);
        }

        static TIMER_CALLBACK( timer_callback_1 )
        {
            ym3812_state *info = (ym3812_state *)ptr;
            ym3812_timer_over(info->chip,1);
        }*/

        //static void TimerHandler(void *param,int c,attotime period)
        private void TimerHandler(ym3812_state param, int c, int period)
        {
            ym3812_state info = param;
            //if( attotime_compare(period, attotime_zero) == 0 )
            if (period == 0)
            {   /* Reset FM Timer */
                //timer_enable(info->timer[c], 0);
            }
            else
            {   /* Start FM Timer */
                //timer_adjust_oneshot(info->timer[c], period, 0);
            }
        }


        //static STREAM_UPDATE( ym3812_stream_update )
        private void ym3812_stream_update(byte ChipID, int[][] outputs, int samples)
        {
            //ym3812_state *info = (ym3812_state *)param;
            ym3812_state info = YM3812Data[ChipID];
            switch (EMU_CORE)
            {
                //# ifdef ENABLE_ALL_CORES
                case EC_MAME:
                    //ym3812_update_one(info.chip, outputs, samples);
                    break;
                //#endif
                case EC_DBOPL:
                    //adlib_OPL2_getsample(info.chip, outputs, samples);
                    ADLIBEMU_getsample(info.chip, outputs, samples);
                    break;
            }
        }

        private int[][] DUMMYBUF = new int[2][] { null, null };

        private void _stream_update(ym3812_state param/*, int interval*/)
        {
            ym3812_state info = param;
            //stream_update(info->stream);

            switch (EMU_CORE)
            {
                //# ifdef ENABLE_ALL_CORES
                case EC_MAME:
                    //ym3812_update_one(info.chip, DUMMYBUF, 0);
                    break;
                //#endif
                case EC_DBOPL:
                    //adlib_OPL2_getsample(info.chip, DUMMYBUF, 0);
                    ADLIBEMU_getsample(info.chip, DUMMYBUF, 0);
                    break;
            }
        }


        //static DEVICE_START( ym3812 )
        private int device_start_ym3812(byte ChipID, int clock)
        {
            //static const ym3812_interface dummy = { 0 };
            //ym3812_state *info = get_safe_token(device);
            ym3812_state info;
            int rate;

            if (ChipID >= MAX_CHIPS)
                return 0;

            info = YM3812Data[ChipID];
            rate = (clock & 0x7FFFFFFF) / 72;
            if ((CHIP_SAMPLING_MODE == 0x01 && rate < CHIP_SAMPLE_RATE) ||
                CHIP_SAMPLING_MODE == 0x02)
                rate = CHIP_SAMPLE_RATE;
            //info->intf = device->static_config ? (const ym3812_interface *)device->static_config : &dummy;
            //info->intf = &dummy;
            //info->device = device;

            /* stream system initialize */
            switch (EMU_CORE)
            {
                //# ifdef ENABLE_ALL_CORES
                case EC_MAME:
                    //info.chip = ym3812_init(clock & 0x7FFFFFFF, rate);
                    ////assert_always(info->chip != NULL, "Error creating YM3812 chip");

                    ////info->stream = stream_create(device,0,1,rate,info,ym3812_stream_update);

                    ///* YM3812 setup */
                    //ym3812_set_timer_handler(info.chip, TimerHandler, info);
                    //ym3812_set_irq_handler(info.chip, IRQHandler, info);
                    //ym3812_set_update_handler(info.chip, _stream_update, info);

                    ////info->timer[0] = timer_alloc(device->machine, timer_callback_0, info);
                    ////info->timer[1] = timer_alloc(device->machine, timer_callback_1, info);
                    break;
                //#endif
                case EC_DBOPL:
                    //info.chip = adlib_OPL2_init(clock & 0x7FFFFFFF, rate, _stream_update, info);
                    info.chip = ADLIBEMU_init((uint)(clock & 0x7FFFFFFF), (uint)rate, _stream_update, info);
                    break;
            }

            return rate;
        }

        //static DEVICE_STOP( ym3812 )
        private void device_stop_ym3812(byte ChipID)
        {
            //ym3812_state *info = get_safe_token(device);
            ym3812_state info = YM3812Data[ChipID];
            switch (EMU_CORE)
            {
                //# ifdef ENABLE_ALL_CORES
                case EC_MAME:
                    //ym3812_shutdown(info.chip);
                    break;
                //#endif
                case EC_DBOPL:
                    //adlib_OPL2_stop(info.chip);
                    ADLIBEMU_stop(info.chip);
                    break;
            }
        }

        //static DEVICE_RESET( ym3812 )
        private void device_reset_ym3812(byte ChipID)
        {
            //ym3812_state *info = get_safe_token(device);
            ym3812_state info = YM3812Data[ChipID];
            switch (EMU_CORE)
            {
                //# ifdef ENABLE_ALL_CORES
                case EC_MAME:
                    //ym3812_reset_chip(info.chip);
                    break;
                //#endif
                case EC_DBOPL:
                    //adlib_OPL2_reset(info.chip);
                    ADLIBEMU_reset(info.chip);
                    break;
            }
        }


        //READ8_DEVICE_HANDLER( ym3812_r )
        private byte ym3812_r(byte ChipID, int offset)
        {
            //ym3812_state *info = get_safe_token(device);
            ym3812_state info = YM3812Data[ChipID];
            switch (EMU_CORE)
            {
                //# ifdef ENABLE_ALL_CORES
                case EC_MAME:
                    //return ym3812_read(info.chip, offset & 1);
                //#endif
                case EC_DBOPL:
                    //return adlib_OPL2_reg_read(info.chip, offset & 0x01);
                    return (byte)ADLIBEMU_reg_read(info.chip, (uint)(offset & 0x01));
                default:
                    return 0x00;
            }
        }

        //WRITE8_DEVICE_HANDLER( ym3812_w )
        private void ym3812_w(byte ChipID, int offset, byte data)
        {
            //ym3812_state *info = get_safe_token(device);
            ym3812_state info = YM3812Data[ChipID];
            if (info == null || info.chip == null) return;

            switch (EMU_CORE)
            {
                //# ifdef ENABLE_ALL_CORES
                case EC_MAME:
                    //ym3812_write(info.chip, offset & 1, data);
                    break;
                //#endif
                case EC_DBOPL:
                    //adlib_OPL2_writeIO(info.chip, offset & 1, data);
                    ADLIBEMU_writeIO(info.chip, (uint)(offset & 1), data);
                    break;
            }
        }

        //READ8_DEVICE_HANDLER( ym3812_status_port_r )
        private byte ym3812_status_port_r(byte ChipID, int offset)
        {
            return ym3812_r(ChipID, 0);
        }
        //READ8_DEVICE_HANDLER( ym3812_read_port_r )
        private byte ym3812_read_port_r(byte ChipID, int offset)
        {
            return ym3812_r(ChipID, 1);
        }
        //WRITE8_DEVICE_HANDLER( ym3812_control_port_w )
        private void ym3812_control_port_w(byte ChipID, int offset, byte data)
        {
            ym3812_w(ChipID, 0, data);
        }
        //WRITE8_DEVICE_HANDLER( ym3812_write_port_w )
        private void ym3812_write_port_w(byte ChipID, int offset, byte data)
        {
            ym3812_w(ChipID, 1, data);
        }


        public void ym3812_set_emu_core(byte Emulator)
        {
            //# ifdef ENABLE_ALL_CORES
            EMU_CORE = (byte)((Emulator < 0x02) ? Emulator : 0x00);
            //#else
            //EMU_CORE = EC_DBOPL;
            //#endif

            return;
        }

        private void ym3812_set_mute_mask(byte ChipID, UInt32 MuteMask)
        {
            ym3812_state info = YM3812Data[ChipID];
            switch (EMU_CORE)
            {
                //# ifdef ENABLE_ALL_CORES
                case EC_MAME:
                    //opl_set_mute_mask(info.chip, MuteMask);
                    break;
                //#endif
                case EC_DBOPL:
                    //adlib_OPL2_set_mute_mask(info.chip, MuteMask);
                    ADLIBEMU_set_mute_mask(info.chip, MuteMask);
                    break;
            }

            return;
        }


        /**************************************************************************
         * Generic get_info
         **************************************************************************/

        /*DEVICE_GET_INFO( ym3812 )
        {
            switch (state)
            {
                // --- the following bits of info are returned as 64-bit signed integers ---
                case DEVINFO_INT_TOKEN_BYTES:					info->i = sizeof(ym3812_state);				break;
                // --- the following bits of info are returned as pointers to data or functions ---
                case DEVINFO_FCT_START:							info->start = DEVICE_START_NAME( ym3812 );				break;
                case DEVINFO_FCT_STOP:							info->stop = DEVICE_STOP_NAME( ym3812 );				break;
                case DEVINFO_FCT_RESET:							info->reset = DEVICE_RESET_NAME( ym3812 );				break;
                // --- the following bits of info are returned as NULL-terminated strings ---
                case DEVINFO_STR_NAME:							strcpy(info->s, "YM3812");							break;
                case DEVINFO_STR_FAMILY:					strcpy(info->s, "Yamaha FM");						break;
                case DEVINFO_STR_VERSION:					strcpy(info->s, "1.0");								break;
                case DEVINFO_STR_SOURCE_FILE:						strcpy(info->s, __FILE__);							break;
                case DEVINFO_STR_CREDITS:					strcpy(info->s, "Copyright Nicola Salmoria and the MAME Team"); break;
            }
        }*/






        //adlibemu.h
        //#if defined(OPLTYPE_IS_OPL2)
        //#define ADLIBEMU(name)			adlib_OPL2_##name
        //#elif defined(OPLTYPE_IS_OPL3)
        //#define ADLIBEMU(name)			adlib_OPL3_##name
        //#endif

        //        typedef void (* ADL_UPDATEHANDLER) (void* param);

        //void* ADLIBEMU(init)(UINT32 clock, UINT32 samplerate,
        //                     ADL_UPDATEHANDLER UpdateHandler, void* param);
        //void ADLIBEMU(stop)(void* chip);
        //void ADLIBEMU(reset)(void* chip);

        //void ADLIBEMU(writeIO)(void* chip, UINT32 addr, UINT8 val);
        //void ADLIBEMU(getsample)(void* chip, INT32** sndptr, INT32 numsamples);

        //UINT32 ADLIBEMU(reg_read)(void* chip, UINT32 port);
        //void ADLIBEMU(write_index)(void* chip, UINT32 port, UINT8 val);

        //void ADLIBEMU(set_mute_mask)(void* chip, UINT32 MuteMask);





        //adlibemu_opl2.c
        //# include "mamedef.h"
        //#define OPLTYPE_IS_OPL2
        //# include "adlibemu.h"
        //# include "opl.c"




        //opl.h
        /*
 *  Copyright (C) 2002-2010  The DOSBox Team
 *  OPL2/OPL3 emulation library
 *
 *  This library is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public
 *  License as published by the Free Software Foundation; either
 *  version 2.1 of the License, or (at your option) any later version.
 * 
 *  This library is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 *  Lesser General Public License for more details.
 * 
 *  You should have received a copy of the GNU Lesser General Public
 *  License along with this library; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 */


        /*
         * Originally based on ADLIBEMU.C, an AdLib/OPL2 emulation library by Ken Silverman
         * Copyright (C) 1998-2001 Ken Silverman
         * Ken Silverman's official web site: "http://www.advsys.net/ken"
         */


        //#define double double

        /*
            define Int32, UInt32, Int32, UInt32, Int16, Bit16u, Bit8s, byte here
        */
        /*
        #include <stdint.h>
        typedef uintptr_t	UInt32;
        typedef intptr_t	Int32;
        typedef uint32_t	UInt32;
        typedef int32_t		Int32;
        typedef uint16_t	Bit16u;
        typedef int16_t		Int16;
        typedef uint8_t		byte;
        typedef int8_t		Bit8s;
        */

        //typedef UINT32		UInt32;
        //typedef INT32		Int32;
        //typedef UINT32		UInt32;
        //typedef INT32		Int32;
        //typedef UINT16		Bit16u;
        //typedef INT16		Int16;
        //typedef UINT8		byte;
        //typedef INT8		Bit8s;

        /*
            define attribution that inlines/forces inlining of a function (optional)
        */
        //#define OPL_INLINE INLINE


        //#undef NUM_CHANNELS
        //#if defined(OPLTYPE_IS_OPL3)
        //#define NUM_CHANNELS	18
        //#else
        private const int NUM_CHANNELS = 9;
        //#endif

        private const int MAXOPERATORS = (NUM_CHANNELS * 2);


        private const double FL05 = ((double)0.5);
        private const double FL2 = ((double)2.0);
        private const double PI = ((double)3.1415926535897932384626433832795);


        private const int FIXEDPT = 0x10000;        // fixed-point calculations using 16+16
        private const int FIXEDPT_LFO = 0x1000000;  // fixed-point calculations using 8+24

        private const int WAVEPREC = 1024;      // waveform precision (10 bits)

        //#define INTFREQU		((double)(14318180.0 / 288.0))		// clocking of the chip
        //#if defined(OPLTYPE_IS_OPL3)
        //#define INTFREQU		((double)(OPL.chip_clock / 288.0))		// clocking of the chip
        //#else
        private double INTFREQU(double n)
        {
            return (double)(n / 72.0);
            // ((double)(OPL.chip_clock / 72.0));      // clocking of the chip
        }
                                                                                //#endif


        private const int OF_TYPE_ATT = 0;
        private const int OF_TYPE_DEC = 1;
        private const int OF_TYPE_REL = 2;
        private const int OF_TYPE_SUS = 3;
        private const int OF_TYPE_SUS_NOKEEP = 4;
        private const int OF_TYPE_OFF = 5;

        private const int ARC_CONTROL = 0x00;
        private const int ARC_TVS_KSR_MUL = 0x20;
        private const int ARC_KSL_OUTLEV = 0x40;
        private const int ARC_ATTR_DECR = 0x60;
        private const int ARC_SUSL_RELR = 0x80;
        private const int ARC_FREQ_NUM = 0xa0;
        private const int ARC_KON_BNUM = 0xb0;
        private const int ARC_PERC_MODE = 0xbd;
        private const int ARC_FEEDBACK = 0xc0;
        private const int ARC_WAVE_SEL = 0xe0;

        private const int ARC_SECONDSET = 0x100;    // second operator set for OPL3


        private const int OP_ACT_OFF = 0x00;
        private const int OP_ACT_NORMAL = 0x01; // regular channel activated (bitmasked)
        private const int OP_ACT_PERC = 0x02;   // percussion channel activated (bitmasked)

        private const int BLOCKBUF_SIZE = 512;


        // vibrato constants
        private const int VIBTAB_SIZE = 8;
        private const double VIBFAC = 70 / 50000;       // no braces, integer mul/div

        // tremolo constants and table
        private const int TREMTAB_SIZE = 53;
        private const double TREM_FREQ = ((double)(3.7));           // tremolo at 3.7hz


        /* operator struct definition
             For OPL2 all 9 channels consist of two operators each, carrier and modulator.
             Channel x has operators x as modulator and operators (9+x) as carrier.
             For OPL3 all 18 channels consist either of two operators (2op mode) or four
             operators (4op mode) which is determined through register4 of the second
             adlib register set.
             Only the channels 0,1,2 (first set) and 9,10,11 (second set) can act as
             4op channels. The two additional operators for a channel y come from the
             2op channel y+3 so the operatorss y, (9+y), y+3, (9+y)+3 make up a 4op
             channel.
        */
        public class op_type
        {//operator_struct
            public Int32 cval, lastcval;           // current output/last output (used for feedback)
            public UInt32 tcount, wfpos, tinc;      // time (position in waveform) and time increment
            public double amp, step_amp;            // and amplification (envelope)
            public double vol;                      // volume
            public double sustain_level;            // sustain level
            public Int32 mfbi;                 // feedback amount
            public double a0, a1, a2, a3;           // attack rate function coefficients
            public double decaymul, releasemul; // decay/release rate functions
            public UInt32 op_state;             // current state of operator (attack/decay/sustain/release/off)
            public UInt32 toff;
            public Int32 freq_high;                // highest three bits of the frequency, used for vibrato calculations
            public Int16[] cur_wform;               // start of selected waveform
            public uint cur_wform_ptr;               // start of selected waveform
            public UInt32 cur_wmask;                // mask for selected waveform
            public UInt32 act_state;                // activity state (regular, percussion)
            public bool sus_keep;                   // keep sustain level when decay finished
            public bool vibrato, tremolo;            // vibrato/tremolo enable bits

            // variables used to provide non-continuous envelopes
            public UInt32 generator_pos;            // for non-standard sample rates we need to determine how many samples have passed
            public Int32 cur_env_step;               // current (standardized) sample position
            public Int32 env_step_a, env_step_d, env_step_r;   // number of std samples of one step (for attack/decay/release mode)
            public byte step_skip_pos_a;           // position of 8-cyclic step skipping (always 2^x to check against mask)
            public Int32 env_step_skip_a;            // bitmask that determines if a step is skipped (respective bit is zero then)

            //#if defined(OPLTYPE_IS_OPL3)
            //bool is_4op,is_4op_attached;	// base of a 4op channel/part of a 4op channel
            //Int32 left_pan,right_pan;		// opl3 stereo panning amount
            //#endif
        }

        public class OPL_DATA
        {            //opl_chip
            // per-chip variables
            //UInt32 chip_num;
            public op_type[] op = new op_type[MAXOPERATORS];
            public byte[] MuteChn = new byte[NUM_CHANNELS + 5];
            public UInt32 chip_clock;

            public Int32 int_samplerate;

            public byte status;
            public UInt32 opl_index;
            public Int32 opl_addr;
            //#if defined(OPLTYPE_IS_OPL3)
            //byte adlibreg[512];	// adlib register set (including second set)
            //byte wave_sel[44];		// waveform selection
            //#else
            public byte[] adlibreg = new byte[256]; // adlib register set
            public byte[] wave_sel = new byte[22];      // waveform selection
                                                        //#endif


            // vibrato/tremolo increment/counter
            public UInt32 vibtab_pos;
            public UInt32 vibtab_add;
            public UInt32 tremtab_pos;
            public UInt32 tremtab_add;

            public UInt32 generator_add;    // should be a chip parameter

            public double recipsamp;    // inverse of sampling rate
            public double[] frqmul = new double[16];

            public ADL_UPDATEHANDLER UpdateHandler; // stream update handler
            public ym3812_state UpdateParam; //void*                  // stream update parameter
        }


        // enable an operator
        //static void enable_operator(OPL_DATA* chip, UInt32 regbase, op_type* op_pt, UInt32 act_type);

        // functions to change parameters of an operator
        //static void change_frequency(OPL_DATA* chip, UInt32 chanbase, UInt32 regbase, op_type* op_pt);

        //static void change_attackrate(OPL_DATA* chip, UInt32 regbase, op_type* op_pt);
        //static void change_decayrate(OPL_DATA* chip, UInt32 regbase, op_type* op_pt);
        //static void change_releaserate(OPL_DATA* chip, UInt32 regbase, op_type* op_pt);
        //static void change_sustainlevel(OPL_DATA* chip, UInt32 regbase, op_type* op_pt);
        //static void change_waveform(OPL_DATA* chip, UInt32 regbase, op_type* op_pt);
        //static void change_keepsustain(OPL_DATA* chip, UInt32 regbase, op_type* op_pt);
        //static void change_vibrato(OPL_DATA* chip, UInt32 regbase, op_type* op_pt);
        //static void change_feedback(OPL_DATA* chip, UInt32 chanbase, op_type* op_pt);

        // general functions
        /*void* adlib_init(UInt32 clock, UInt32 samplerate);
        void adlib_writeIO(void *chip, UInt32 addr, byte val);
        void adlib_write(void *chip, UInt32 idx, byte val);
        //void adlib_getsample(Int16* sndptr, Int32 numsamples);
        void adlib_getsample(void *chip, Int32** sndptr, Int32 numsamples);
        UInt32 adlib_reg_read(void *chip, UInt32 port);
        void adlib_write_index(void *chip, UInt32 port, byte val);*/
        //static void adlib_write(void *chip, UInt32 idx, byte val);








        //opl.c
        // IMPORTANT: This file is not meant to be compiled. It's included in adlibemu_opl?.c.

        /*
         *  Copyright (C) 2002-2010  The DOSBox Team
         *  OPL2/OPL3 emulation library
         *
         *  This library is free software; you can redistribute it and/or
         *  modify it under the terms of the GNU Lesser General Public
         *  License as published by the Free Software Foundation; either
         *  version 2.1 of the License, or (at your option) any later version.
         * 
         *  This library is distributed in the hope that it will be useful,
         *  but WITHOUT ANY WARRANTY; without even the implied warranty of
         *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
         *  Lesser General Public License for more details.
         * 
         *  You should have received a copy of the GNU Lesser General Public
         *  License along with this library; if not, write to the Free Software
         *  Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
         */


        /*
         * Originally based on ADLIBEMU.C, an AdLib/OPL2 emulation library by Ken Silverman
         * Copyright (C) 1998-2001 Ken Silverman
         * Ken Silverman's official web site: "http://www.advsys.net/ken"
         */


        //# include <math.h>
        //# include <stdlib.h> // rand
        //# include <string.h>	// for memset
        //#include "dosbox.h"
        //# include "../stdbool.h"
        //# include "opl.h"


        //static double recipsamp;	// inverse of sampling rate		// moved to OPL_DATA
        private Int16[] wavtable = new Int16[WAVEPREC * 3];   // wave form table

        // vibrato/tremolo tables
        private Int32[] vib_table = new Int32[VIBTAB_SIZE];
        private Int32[] trem_table = new Int32[TREMTAB_SIZE * 2];

        private Int32[] vibval_const = new Int32[BLOCKBUF_SIZE];
        private Int32[] tremval_const = new Int32[BLOCKBUF_SIZE];

        // vibrato value tables (used per-operator)
        private Int32[] vibval_var1 = new Int32[BLOCKBUF_SIZE];
        private Int32[] vibval_var2 = new Int32[BLOCKBUF_SIZE];
        //static Int32 vibval_var3[BLOCKBUF_SIZE];
        //static Int32 vibval_var4[BLOCKBUF_SIZE];

        // vibrato/trmolo value table pointers
        //static Int32 *vibval1, *vibval2, *vibval3, *vibval4;
        //static Int32 *tremval1, *tremval2, *tremval3, *tremval4;
        // moved to adlib_getsample


        // key scale level lookup table
        private double[] kslmul = new double[4]{
            0.0, 0.5, 0.25, 1.0     // -> 0, 3, 1.5, 6 dB/oct
        };

        // frequency multiplicator lookup table
        private double[] frqmul_tab = new double[16]{
            0.5,1,2,3,4,5,6,7,8,9,10,10,12,12,15,15
        };

        // calculated frequency multiplication values (depend on sampling rate)
        //static double frqmul[16];	// moved to OPL_DATA

        // key scale levels
        private byte[][] kslev = new byte[8][]{
            new byte[16], new byte[16], new byte[16], new byte[16],
            new byte[16], new byte[16], new byte[16], new byte[16]
        };

        // map a channel number to the register offset of the modulator (=register base)
        private byte[] modulatorbase = new byte[9]{
            0,1,2,
            8,9,10,
            16,17,18
        };

        // map a register base to a modulator operator number or operator number
        //#if defined(OPLTYPE_IS_OPL3)
        //static const byte regbase2modop[44] = {
        //	0,1,2,0,1,2,0,0,3,4,5,3,4,5,0,0,6,7,8,6,7,8,					// first set
        //	18,19,20,18,19,20,0,0,21,22,23,21,22,23,0,0,24,25,26,24,25,26	// second set
        //};
        //static const byte regbase2op[44] = {
        //	0,1,2,9,10,11,0,0,3,4,5,12,13,14,0,0,6,7,8,15,16,17,			// first set
        //	18,19,20,27,28,29,0,0,21,22,23,30,31,32,0,0,24,25,26,33,34,35	// second set
        //};
        //#else
        private byte[] regbase2modop = new byte[22] {
            0,1,2,0,1,2,0,0,3,4,5,3,4,5,0,0,6,7,8,6,7,8
        };
        private byte[] regbase2op = new byte[22] {
            0,1,2,9,10,11,0,0,3,4,5,12,13,14,0,0,6,7,8,15,16,17
        };
        //#endif


        // start of the waveform
        private UInt32[] waveform = new UInt32[8]{
            WAVEPREC,
            WAVEPREC>>1,
            WAVEPREC,
            (WAVEPREC*3)>>2,
            0,
            0,
            (WAVEPREC*5)>>2,
            WAVEPREC<<1
        };

        // length of the waveform as mask
        private UInt32[] wavemask = new UInt32[8]{
            WAVEPREC-1,
            WAVEPREC-1,
            (WAVEPREC>>1)-1,
            (WAVEPREC>>1)-1,
            WAVEPREC-1,
            ((WAVEPREC*3)>>2)-1,
            WAVEPREC>>1,
            WAVEPREC-1
        };

        // where the first entry resides
        private UInt32[] wavestart = new UInt32[8] {
            0,
            WAVEPREC>>1,
            0,
            WAVEPREC>>2,
            0,
            0,
            0,
            WAVEPREC>>3
        };

        // envelope generator function constants
        private double[] attackconst = new double[4] {
            (double)(1/2.82624),
            (double)(1/2.25280),
            (double)(1/1.88416),
            (double)(1/1.59744)
        };

        private double[] decrelconst = new double[4]{
            (double)(1/39.28064),
            (double)(1/31.41608),
            (double)(1/26.17344),
            (double)(1/22.44608)
        };


        private void operator_advance(OPL_DATA chip, op_type op_pt, Int32 vib)
        {
            op_pt.wfpos = op_pt.tcount;                       // waveform position

            // advance waveform time
            op_pt.tcount += op_pt.tinc;
            op_pt.tcount += (UInt32)(op_pt.tinc * vib / FIXEDPT);

            op_pt.generator_pos += chip.generator_add;
        }

        private static Random rand = new Random();

        private void operator_advance_drums(OPL_DATA chip, op_type op_pt1, Int32 vib1, op_type op_pt2, Int32 vib2, op_type op_pt3, Int32 vib3)
        {
            UInt32 c1 = op_pt1.tcount / FIXEDPT;
            UInt32 c3 = op_pt3.tcount / FIXEDPT;
            UInt32 phasebit = (UInt32)((((c1 & 0x88) ^ ((c1 << 5) & 0x80)) | ((c3 ^ (c3 << 2)) & 0x20)) != 0 ? 0x02 : 0x00);

            UInt32 noisebit = (uint)(rand.Next() & 1);

            UInt32 snare_phase_bit = (((UInt32)((op_pt1.tcount / FIXEDPT) / 0x100)) & 1);

            //Hihat
            UInt32 inttm = (UInt32)((phasebit << 8) | (uint)(0x34 << (int)(phasebit ^ (noisebit << 1))));
            op_pt1.wfpos = inttm * FIXEDPT;                // waveform position
                                                           // advance waveform time
            op_pt1.tcount += op_pt1.tinc;
            op_pt1.tcount += (UInt32)(op_pt1.tinc * vib1 / FIXEDPT);
            op_pt1.generator_pos += chip.generator_add;

            //Snare
            inttm = ((1 + snare_phase_bit) ^ noisebit) << 8;
            op_pt2.wfpos = inttm * FIXEDPT;                // waveform position
                                                           // advance waveform time
            op_pt2.tcount += op_pt2.tinc;
            op_pt2.tcount += (UInt32)(op_pt2.tinc * vib2 / FIXEDPT);
            op_pt2.generator_pos += chip.generator_add;

            //Cymbal
            inttm = (1 + phasebit) << 8;
            op_pt3.wfpos = inttm * FIXEDPT;                // waveform position
                                                           // advance waveform time
            op_pt3.tcount += op_pt3.tinc;
            op_pt3.tcount += (UInt32)(op_pt3.tinc * vib3 / FIXEDPT);
            op_pt3.generator_pos += chip.generator_add;
        }


        // output level is sustained, mode changes only when operator is turned off (->release)
        // or when the keep-sustained bit is turned off (->sustain_nokeep)
        private void operator_output(op_type op_pt, Int32 modulator, Int32 trem)
        {
            if (op_pt.op_state != OF_TYPE_OFF)
            {
                UInt32 i;
                op_pt.lastcval = op_pt.cval;
                i = (UInt32)((op_pt.wfpos + modulator) / FIXEDPT);

                // wform: -16384 to 16383 (0x4000)
                // trem :  32768 to 65535 (0x10000)
                // step_amp: 0.0 to 1.0
                // vol  : 1/2^14 to 1/2^29 (/0x4000; /1../0x8000)

                op_pt.cval = (Int32)(op_pt.step_amp * op_pt.vol * op_pt.cur_wform[op_pt.cur_wform_ptr + (i & op_pt.cur_wmask)] * trem / 16.0);
            }
        }


        // no action, operator is off
        private static void operator_off(op_type op_pt)
        {
        }

        // output level is sustained, mode changes only when operator is turned off (->release)
        // or when the keep-sustained bit is turned off (->sustain_nokeep)
        private static void operator_sustain(op_type op_pt)
        {
            UInt32 num_steps_add = op_pt.generator_pos / FIXEDPT;  // number of (standardized) samples
            UInt32 ct;
            for (ct = 0; ct < num_steps_add; ct++)
            {
                op_pt.cur_env_step++;
            }
            op_pt.generator_pos -= num_steps_add * FIXEDPT;
        }

        // operator in release mode, if output level reaches zero the operator is turned off
        private static void operator_release(op_type op_pt)
        {
            UInt32 num_steps_add;
            UInt32 ct;

            // ??? boundary?
            if (op_pt.amp > 0.00000001)
            {
                // release phase
                op_pt.amp *= op_pt.releasemul;
            }

            num_steps_add = op_pt.generator_pos / FIXEDPT; // number of (standardized) samples
            for (ct = 0; ct < num_steps_add; ct++)
            {
                op_pt.cur_env_step++;                      // sample counter
                if ((op_pt.cur_env_step & op_pt.env_step_r) == 0)
                {
                    if (op_pt.amp <= 0.00000001)
                    {
                        // release phase finished, turn off this operator
                        op_pt.amp = 0.0;
                        if (op_pt.op_state == OF_TYPE_REL)
                        {
                            op_pt.op_state = OF_TYPE_OFF;
                        }
                    }
                    op_pt.step_amp = op_pt.amp;
                }
            }
            op_pt.generator_pos -= num_steps_add * FIXEDPT;
        }

        // operator in decay mode, if sustain level is reached the output level is either
        // kept (sustain level keep enabled) or the operator is switched into release mode
        private static void operator_decay(op_type op_pt)
        {
            UInt32 num_steps_add;
            UInt32 ct;

            if (op_pt.amp > op_pt.sustain_level)
            {
                // decay phase
                op_pt.amp *= op_pt.decaymul;
            }

            num_steps_add = op_pt.generator_pos / FIXEDPT; // number of (standardized) samples
            for (ct = 0; ct < num_steps_add; ct++)
            {
                op_pt.cur_env_step++;
                if ((op_pt.cur_env_step & op_pt.env_step_d) == 0)
                {
                    if (op_pt.amp <= op_pt.sustain_level)
                    {
                        // decay phase finished, sustain level reached
                        if (op_pt.sus_keep)
                        {
                            // keep sustain level (until turned off)
                            op_pt.op_state = OF_TYPE_SUS;
                            op_pt.amp = op_pt.sustain_level;
                        }
                        else
                        {
                            // next: release phase
                            op_pt.op_state = OF_TYPE_SUS_NOKEEP;
                        }
                    }
                    op_pt.step_amp = op_pt.amp;
                }
            }
            op_pt.generator_pos -= num_steps_add * FIXEDPT;
        }

        // operator in attack mode, if full output level is reached,
        // the operator is switched into decay mode
        private static void operator_attack(op_type op_pt)
        {
            UInt32 num_steps_add;
            UInt32 ct;

            op_pt.amp = ((op_pt.a3 * op_pt.amp + op_pt.a2) * op_pt.amp + op_pt.a1) * op_pt.amp + op_pt.a0;

            num_steps_add = op_pt.generator_pos / FIXEDPT;     // number of (standardized) samples
            for (ct = 0; ct < num_steps_add; ct++)
            {
                op_pt.cur_env_step++;  // next sample
                if ((op_pt.cur_env_step & op_pt.env_step_a) == 0)
                {       // check if next step already reached
                    if (op_pt.amp > 1.0)
                    {
                        // attack phase finished, next: decay
                        op_pt.op_state = OF_TYPE_DEC;
                        op_pt.amp = 1.0;
                        op_pt.step_amp = 1.0;
                    }
                    op_pt.step_skip_pos_a <<= 1;
                    if (op_pt.step_skip_pos_a == 0) op_pt.step_skip_pos_a = 1;
                    if ((op_pt.step_skip_pos_a & op_pt.env_step_skip_a) != 0)
                    {   // check if required to skip next step
                        op_pt.step_amp = op_pt.amp;
                    }
                }
            }
            op_pt.generator_pos -= num_steps_add * FIXEDPT;
        }

        private void operator_eg_attack_check(op_type op_pt)
        {
            if (((op_pt.cur_env_step + 1) & op_pt.env_step_a) == 0)
            {
                // check if next step already reached
                if (op_pt.a0 >= 1.0)
                {
                    // attack phase finished, next: decay
                    op_pt.op_state = OF_TYPE_DEC;
                    op_pt.amp = 1.0;
                    op_pt.step_amp = 1.0;
                }
            }
        }


        private delegate void optype_fptr(op_type a);

        private optype_fptr[] opfuncs = new optype_fptr[6]{
            operator_attack,
            operator_decay,
            operator_release,
            operator_sustain,	// sustain phase (keeping level)
	        operator_release,	// sustain_nokeep phase (release-style)
	        operator_off
        };

        private byte[] step_skip_mask = new byte[5] { 0xff, 0xfe, 0xee, 0xba, 0xaa };

        private void change_attackrate(OPL_DATA chip, UInt32 regbase, op_type op_pt)
        {
            Int32 attackrate = chip.adlibreg[ARC_ATTR_DECR + regbase] >> 4;
            if (attackrate != 0)
            {
                //byte[] step_skip_mask = new byte[5] { 0xff, 0xfe, 0xee, 0xba, 0xaa };
                Int32 step_skip;
                Int32 steps;
                Int32 step_num;

                double f = (double)(Math.Pow(FL2, (double)attackrate + (op_pt.toff >> 2) - 1) * attackconst[op_pt.toff & 3] * chip.recipsamp);
                // attack rate coefficients
                op_pt.a0 = (double)(0.0377 * f);
                op_pt.a1 = (double)(10.73 * f + 1);
                op_pt.a2 = (double)(-17.57 * f);
                op_pt.a3 = (double)(7.42 * f);

                step_skip = (int)(attackrate * 4 + op_pt.toff);
                steps = step_skip >> 2;
                op_pt.env_step_a = (1 << (steps <= 12 ? 12 - steps : 0)) - 1;

                step_num = (step_skip <= 48) ? (4 - (step_skip & 3)) : 0;
                op_pt.env_step_skip_a = step_skip_mask[step_num];

                //#if defined(OPLTYPE_IS_OPL3)
                //if (step_skip>=60)
                //#else
                if (step_skip >= 62)
                //#endif
                {
                    op_pt.a0 = (double)(2.0);  // something that triggers an immediate transition to amp:=1.0
                    op_pt.a1 = (double)(0.0);
                    op_pt.a2 = (double)(0.0);
                    op_pt.a3 = (double)(0.0);
                }
            }
            else
            {
                // attack disabled
                op_pt.a0 = 0.0;
                op_pt.a1 = 1.0;
                op_pt.a2 = 0.0;
                op_pt.a3 = 0.0;
                op_pt.env_step_a = 0;
                op_pt.env_step_skip_a = 0;
            }
        }

        private void change_decayrate(OPL_DATA chip, UInt32 regbase, op_type op_pt)
        {
            Int32 decayrate = chip.adlibreg[ARC_ATTR_DECR + regbase] & 15;
            // decaymul should be 1.0 when decayrate==0
            if (decayrate != 0)
            {
                Int32 steps;

                double f = (double)(-7.4493 * decrelconst[op_pt.toff & 3] * chip.recipsamp);
                op_pt.decaymul = (double)(Math.Pow(FL2, f * Math.Pow(FL2, (double)(decayrate + (op_pt.toff >> 2)))));
                steps = (int)((decayrate * 4 + op_pt.toff) >> 2);
                op_pt.env_step_d = (1 << (steps <= 12 ? 12 - steps : 0)) - 1;
            }
            else
            {
                op_pt.decaymul = 1.0;
                op_pt.env_step_d = 0;
            }
        }

        private void change_releaserate(OPL_DATA chip, UInt32 regbase, op_type op_pt)
        {
            Int32 releaserate = chip.adlibreg[ARC_SUSL_RELR + regbase] & 15;
            // releasemul should be 1.0 when releaserate==0
            if (releaserate != 0)
            {
                Int32 steps;

                double f = (double)(-7.4493 * decrelconst[op_pt.toff & 3] * chip.recipsamp);
                op_pt.releasemul = (double)(Math.Pow(FL2, f * Math.Pow(FL2, (double)(releaserate + (op_pt.toff >> 2)))));
                steps = (int)((releaserate * 4 + op_pt.toff) >> 2);
                op_pt.env_step_r = (1 << (steps <= 12 ? 12 - steps : 0)) - 1;
            }
            else
            {
                op_pt.releasemul = 1.0;
                op_pt.env_step_r = 0;
            }
        }

        private void change_sustainlevel(OPL_DATA chip, UInt32 regbase, op_type op_pt)
        {
            Int32 sustainlevel = chip.adlibreg[ARC_SUSL_RELR + regbase] >> 4;
            // sustainlevel should be 0.0 when sustainlevel==15 (max)
            if (sustainlevel < 15)
            {
                op_pt.sustain_level = (double)(Math.Pow(FL2, (double)sustainlevel * (-FL05)));
            }
            else
            {
                op_pt.sustain_level = 0.0;
            }
        }

        private void change_waveform(OPL_DATA chip, UInt32 regbase, op_type op_pt)
        {
            //#if defined(OPLTYPE_IS_OPL3)
            //if (regbase>=ARC_SECONDSET) regbase -= (ARC_SECONDSET-22);	// second set starts at 22
            //#endif
            // waveform selection
            op_pt.cur_wmask = wavemask[chip.wave_sel[regbase]];
            op_pt.cur_wform = wavtable;
            op_pt.cur_wform_ptr = waveform[chip.wave_sel[regbase]];
            // (might need to be adapted to waveform type here...)
        }

        private void change_keepsustain(OPL_DATA chip, UInt32 regbase, op_type op_pt)
        {
            op_pt.sus_keep = (chip.adlibreg[ARC_TVS_KSR_MUL + regbase] & 0x20) > 0;
            if (op_pt.op_state == OF_TYPE_SUS)
            {
                if (!op_pt.sus_keep)
                    op_pt.op_state = OF_TYPE_SUS_NOKEEP;
            }
            else if (op_pt.op_state == OF_TYPE_SUS_NOKEEP)
            {
                if (op_pt.sus_keep)
                    op_pt.op_state = OF_TYPE_SUS;
            }
        }

        // enable/disable vibrato/tremolo LFO effects
        private void change_vibrato(OPL_DATA chip, UInt32 regbase, op_type op_pt)
        {
            op_pt.vibrato = (chip.adlibreg[ARC_TVS_KSR_MUL + regbase] & 0x40) != 0;
            op_pt.tremolo = (chip.adlibreg[ARC_TVS_KSR_MUL + regbase] & 0x80) != 0;
        }

        // change amount of self-feedback
        private void change_feedback(OPL_DATA chip, UInt32 chanbase, op_type op_pt)
        {
            Int32 feedback = chip.adlibreg[ARC_FEEDBACK + chanbase] & 14;
            if (feedback != 0)
                op_pt.mfbi = (Int32)(Math.Pow(FL2, (double)((feedback >> 1) + 8)));
            else
                op_pt.mfbi = 0;
        }

        private void change_frequency(OPL_DATA chip, UInt32 chanbase, UInt32 regbase, op_type op_pt)
        {
            UInt32 frn;
            UInt32 oct;
            UInt32 note_sel;
            double vol_in;

            // frequency
            frn = ((((UInt32)chip.adlibreg[ARC_KON_BNUM + chanbase]) & 3) << 8) + (UInt32)chip.adlibreg[ARC_FREQ_NUM + chanbase];
            // block number/octave
            oct = ((((UInt32)chip.adlibreg[ARC_KON_BNUM + chanbase]) >> 2) & 7);
            op_pt.freq_high = (Int32)((frn >> 7) & 7);

            // keysplit
            note_sel = (uint)((chip.adlibreg[8] >> 6) & 1);
            op_pt.toff = ((frn >> 9) & (note_sel ^ 1)) | ((frn >> 8) & note_sel);
            op_pt.toff += (oct << 1);

            // envelope scaling (KSR)
            if ((chip.adlibreg[ARC_TVS_KSR_MUL + regbase] & 0x10) == 0) op_pt.toff >>= 2;

            // 20+a0+b0:
            op_pt.tinc = (UInt32)((((double)(frn << (int)oct)) * chip.frqmul[chip.adlibreg[ARC_TVS_KSR_MUL + regbase] & 15]));
            // 40+a0+b0:
            vol_in = (double)((double)(chip.adlibreg[ARC_KSL_OUTLEV + regbase] & 63) +
                                    kslmul[chip.adlibreg[ARC_KSL_OUTLEV + regbase] >> 6] * kslev[oct][frn >> 6]);
            op_pt.vol = (double)(Math.Pow(FL2, (double)(vol_in * -0.125 - 14)));

            // operator frequency changed, care about features that depend on it
            change_attackrate(chip, regbase, op_pt);
            change_decayrate(chip, regbase, op_pt);
            change_releaserate(chip, regbase, op_pt);
        }

        private void enable_operator(OPL_DATA chip, UInt32 regbase, op_type op_pt, UInt32 act_type)
        {
            // check if this is really an off-on transition
            if (op_pt.act_state == OP_ACT_OFF)
            {
                Int32 wselbase = (Int32)regbase;
                if (wselbase >= ARC_SECONDSET)
                    wselbase -= (ARC_SECONDSET - 22);   // second set starts at 22

                op_pt.tcount = wavestart[chip.wave_sel[wselbase]] * FIXEDPT;

                // start with attack mode
                op_pt.op_state = OF_TYPE_ATT;
                op_pt.act_state |= act_type;
            }
        }

        private void disable_operator(op_type op_pt, UInt32 act_type)
        {
            // check if this is really an on-off transition
            if (op_pt.act_state != OP_ACT_OFF)
            {
                op_pt.act_state &= (~act_type);
                if (op_pt.act_state == OP_ACT_OFF)
                {
                    if (op_pt.op_state != OF_TYPE_OFF)
                        op_pt.op_state = OF_TYPE_REL;
                }
            }
        }

        private UInt32 initfirstime = 0;
        //void adlib_init(UInt32 samplerate)
        private OPL_DATA ADLIBEMU_init(UInt32 clock, UInt32 samplerate, ADL_UPDATEHANDLER UpdateHandler, ym3812_state param)
        {
            OPL_DATA OPL;
            //op_type* op;

            Int32 i, j, oct;
            //Int32 trem_table_int[TREMTAB_SIZE];

            OPL = new OPL_DATA();//(OPL_DATA) malloc(sizeof(OPL_DATA));
            OPL.chip_clock = clock;
            OPL.int_samplerate = (int)samplerate;
            OPL.UpdateHandler = UpdateHandler;
            OPL.UpdateParam = param;

            OPL.generator_add = (UInt32)(INTFREQU(OPL.chip_clock) * FIXEDPT / OPL.int_samplerate);


            /*memset(OPL.adlibreg,0,sizeof(OPL.adlibreg));
            memset(OPL.op,0,sizeof(op_type)*MAXOPERATORS);
            memset(OPL.wave_sel,0,sizeof(OPL.wave_sel));
            for (i=0;i<MAXOPERATORS;i++)
            {
                op = &OPL.op[i];

                op.op_state = OF_TYPE_OFF;
                op.act_state = OP_ACT_OFF;
                op.amp = 0.0;
                op.step_amp = 0.0;
                op.vol = 0.0;
                op.tcount = 0;
                op.tinc = 0;
                op.toff = 0;
                op.cur_wmask = wavemask[0];
                op.cur_wform = &wavtable[waveform[0]];
                op.freq_high = 0;
                op.generator_pos = 0;
                op.cur_env_step = 0;
                op.env_step_a = 0;
                op.env_step_d = 0;
                op.env_step_r = 0;
                op.step_skip_pos_a = 0;
                op.env_step_skip_a = 0;
        #if defined(OPLTYPE_IS_OPL3)
                op.is_4op = false;
                op.is_4op_attached = false;
                op.left_pan = 1;
                op.right_pan = 1;
        #endif
            }*/

            OPL.recipsamp = 1.0 / (double)OPL.int_samplerate;
            for (i = 15; i >= 0; i--)
            {
                OPL.frqmul[i] = (double)(frqmul_tab[i] * INTFREQU(OPL.chip_clock) / (double)WAVEPREC * (double)FIXEDPT * OPL.recipsamp);
            }

            //OPL.status = 0;
            //OPL.opl_index = 0;


            if (initfirstime == 0)
            {
                // create vibrato table
                vib_table[0] = 8;
                vib_table[1] = 4;
                vib_table[2] = 0;
                vib_table[3] = -4;
                for (i = 4; i < VIBTAB_SIZE; i++) vib_table[i] = vib_table[i - 4] * -1;
            }

            // vibrato at ~6.1 ?? (opl3 docs say 6.1, opl4 docs say 6.0, y8950 docs say 6.4)
            OPL.vibtab_add = (UInt32)(VIBTAB_SIZE * FIXEDPT_LFO / 8192 * INTFREQU(OPL.chip_clock) / OPL.int_samplerate);
            OPL.vibtab_pos = 0;

            if (initfirstime == 0)
            {
                Int32[] trem_table_int = new Int32[TREMTAB_SIZE];

                for (i = 0; i < BLOCKBUF_SIZE; i++) vibval_const[i] = 0;


                // create tremolo table
                for (i = 0; i < 14; i++) trem_table_int[i] = i - 13;        // upwards (13 to 26 -> -0.5/6 to 0)
                for (i = 14; i < 41; i++) trem_table_int[i] = -i + 14;      // downwards (26 to 0 -> 0 to -1/6)
                for (i = 41; i < 53; i++) trem_table_int[i] = i - 40 - 26;  // upwards (1 to 12 -> -1/6 to -0.5/6)

                for (i = 0; i < TREMTAB_SIZE; i++)
                {
                    // 0.0 .. -26/26*4.8/6 == [0.0 .. -0.8], 4/53 steps == [1 .. 0.57]
                    double trem_val1 = (double)(((double)trem_table_int[i]) * 4.8 / 26.0 / 6.0);                // 4.8db
                    double trem_val2 = (double)((double)((Int32)(trem_table_int[i] / 4)) * 1.2 / 6.0 / 6.0);       // 1.2db (larger stepping)

                    trem_table[i] = (Int32)(Math.Pow(FL2, trem_val1) * FIXEDPT);
                    trem_table[TREMTAB_SIZE + i] = (Int32)(Math.Pow(FL2, trem_val2) * FIXEDPT);
                }
            }

            // tremolo at 3.7hz
            OPL.tremtab_add = (UInt32)((double)TREMTAB_SIZE * TREM_FREQ * FIXEDPT_LFO / (double)OPL.int_samplerate);
            OPL.tremtab_pos = 0;

            if (initfirstime == 0)
            {
                initfirstime = 1;

                for (i = 0; i < BLOCKBUF_SIZE; i++) tremval_const[i] = FIXEDPT;


                // create waveform tables
                for (i = 0; i < (WAVEPREC >> 1); i++)
                {
                    wavtable[(i << 1) + WAVEPREC] = (Int16)(16384 * Math.Sin((double)((i << 1)) * PI * 2 / WAVEPREC));
                    wavtable[(i << 1) + 1 + WAVEPREC] = (Int16)(16384 * Math.Sin((double)((i << 1) + 1) * PI * 2 / WAVEPREC));
                    wavtable[i] = wavtable[(i << 1) + WAVEPREC];
                    // alternative: (zero-less)
                    /*			wavtable[(i<<1)  +WAVEPREC]	= (Int16)(16384*sin((double)((i<<2)+1)*PI/WAVEPREC));
                                wavtable[(i<<1)+1+WAVEPREC]	= (Int16)(16384*sin((double)((i<<2)+3)*PI/WAVEPREC));
                                wavtable[i]					= wavtable[(i<<1)-1+WAVEPREC]; */
                }
                for (i = 0; i < (WAVEPREC >> 3); i++)
                {
                    wavtable[i + (WAVEPREC << 1)] = (Int16)(wavtable[i + (WAVEPREC >> 3)] - 16384);
                    wavtable[i + ((WAVEPREC * 17) >> 3)] = (Int16)(wavtable[i + (WAVEPREC >> 2)] + 16384);
                }

                // key scale level table verified ([table in book]*8/3)
                kslev[7][0] = 0; kslev[7][1] = 24; kslev[7][2] = 32; kslev[7][3] = 37;
                kslev[7][4] = 40; kslev[7][5] = 43; kslev[7][6] = 45; kslev[7][7] = 47;
                kslev[7][8] = 48;
                for (i = 9; i < 16; i++) kslev[7][i] = (byte)(i + 41);
                for (j = 6; j >= 0; j--)
                {
                    for (i = 0; i < 16; i++)
                    {
                        oct = (Int32)kslev[j + 1][i] - 8;
                        if (oct < 0) oct = 0;
                        kslev[j][i] = (byte)oct;
                    }
                }
            }

            return OPL;
        }

        private void ADLIBEMU_stop(OPL_DATA chip)
        {
            //free(chip);

            return;
        }

        private void ADLIBEMU_reset(OPL_DATA chip)
        {
            OPL_DATA OPL = (OPL_DATA)chip;
            Int32 i;
            op_type op;

            //memset(OPL.adlibreg, 0x00, sizeof(OPL.adlibreg));
            //memset(OPL.op, 0x00, sizeof(op_type) * MAXOPERATORS);
            //memset(OPL.wave_sel, 0x00, sizeof(OPL.wave_sel));
            for (int ind = 0; ind < OPL.adlibreg.Length; ind++) OPL.adlibreg[ind] = 0;
            for (int ind = 0; ind < OPL.op.Length; ind++) OPL.op[ind] = new op_type();
            for (int ind = 0; ind < OPL.wave_sel.Length; ind++) OPL.wave_sel[ind] = 0;

            for (i = 0; i < MAXOPERATORS; i++)
            {
                op = OPL.op[i];

                op.op_state = OF_TYPE_OFF;
                op.act_state = OP_ACT_OFF;
                op.amp = 0.0;
                op.step_amp = 0.0;
                op.vol = 0.0;
                op.tcount = 0;
                op.tinc = 0;
                op.toff = 0;
                op.cur_wmask = wavemask[0];
                op.cur_wform = wavtable;
                op.cur_wform_ptr = waveform[0];
                op.freq_high = 0;

                op.generator_pos = 0;
                op.cur_env_step = 0;
                op.env_step_a = 0;
                op.env_step_d = 0;
                op.env_step_r = 0;
                op.step_skip_pos_a = 0;
                op.env_step_skip_a = 0;

                //#if defined(OPLTYPE_IS_OPL3)
                //op.is_4op = false;
                //op.is_4op_attached = false;
                //op.left_pan = 1;
                //op.right_pan = 1;
                //#endif
            }

            OPL.status = 0;
            OPL.opl_index = 0;
            OPL.opl_addr = 0;

            return;
        }



        private void ADLIBEMU_writeIO(OPL_DATA chip, UInt32 addr, byte val)
        {
            OPL_DATA OPL = (OPL_DATA)chip;

            if ((addr & 1) != 0)
                adlib_write(OPL, (uint)OPL.opl_addr, val);
            else
                //#if defined(OPLTYPE_IS_OPL3)
                //OPL.opl_addr = val | ((addr & 2) << 7);
                //#else
                OPL.opl_addr = val;
            //#endif
        }

        private void adlib_write(OPL_DATA chip, UInt32 idx, byte val)
        {
            OPL_DATA OPL = (OPL_DATA)chip;

            UInt32 second_set = idx & 0x100;
            OPL.adlibreg[idx] = val;

            switch (idx & 0xf0)
            {
                case ARC_CONTROL:
                    // here we check for the second set registers, too:
                    switch (idx)
                    {
                        case 0x02:  // timer1 counter
                        case 0x03:  // timer2 counter
                            break;
                        case 0x04:
                            // IRQ reset, timer mask/start
                            if ((val & 0x80) != 0)
                            {
                                // clear IRQ bits in status register
                                OPL.status &= 0x9f;// ~0x60;
                            }
                            else
                            {
                                OPL.status = 0;
                            }
                            break;
                        //#if defined(OPLTYPE_IS_OPL3)
                        //		case 0x04|ARC_SECONDSET:
                        //			// 4op enable/disable switches for each possible channel
                        //			OPL.op[0].is_4op = (val&1)>0;
                        //			OPL.op[3].is_4op_attached = OPL.op[0].is_4op;
                        //			OPL.op[1].is_4op = (val&2)>0;
                        //			OPL.op[4].is_4op_attached = OPL.op[1].is_4op;
                        //			OPL.op[2].is_4op = (val&4)>0;
                        //			OPL.op[5].is_4op_attached = OPL.op[2].is_4op;
                        //			OPL.op[18].is_4op = (val&8)>0;
                        //			OPL.op[21].is_4op_attached = OPL.op[18].is_4op;
                        //			OPL.op[19].is_4op = (val&16)>0;
                        //			OPL.op[22].is_4op_attached = OPL.op[19].is_4op;
                        //			OPL.op[20].is_4op = (val&32)>0;
                        //			OPL.op[23].is_4op_attached = OPL.op[20].is_4op;
                        //			break;
                        //		case 0x05|ARC_SECONDSET:
                        //			break;
                        //#endif
                        case 0x08:
                            // CSW, note select
                            break;
                        default:
                            break;
                    }
                    break;
                case ARC_TVS_KSR_MUL:
                case ARC_TVS_KSR_MUL + 0x10:
                    {
                        // tremolo/vibrato/sustain keeping enabled; key scale rate; frequency multiplication
                        int num = (int)(idx & 7);
                        UInt32 base_ = (idx - ARC_TVS_KSR_MUL) & 0xff;
                        if ((num < 6) && (base_ < 22))
                        {
                            UInt32 modop = regbase2modop[second_set!=0 ? (base_ + 22) : base_];
                            UInt32 regbase = base_ + second_set;
                            UInt32 chanbase = second_set != 0 ? (modop - 18 + ARC_SECONDSET) : modop;

                            // change tremolo/vibrato and sustain keeping of this operator
                            op_type op_ptr = OPL.op[modop + ((num < 3) ? 0 : 9)];
                            change_keepsustain(chip, regbase, op_ptr);
                            change_vibrato(chip, regbase, op_ptr);

                            // change frequency calculations of this operator as
                            // key scale rate and frequency multiplicator can be changed
                            //#if defined(OPLTYPE_IS_OPL3)
                            //			if ((OPL.adlibreg[0x105]&1) && (OPL.op[modop].is_4op_attached))
                            //			{
                            //				// operator uses frequency of channel
                            //				change_frequency(chip, chanbase-3,regbase,op_ptr);
                            //			}
                            //			else
                            //			{
                            //				change_frequency(chip, chanbase,regbase,op_ptr);
                            //			}
                            //#else
                            change_frequency(chip, chanbase, base_, op_ptr);
                            //#endif
                        }
                    }
                    break;
                case ARC_KSL_OUTLEV:
                case ARC_KSL_OUTLEV + 0x10:
                    {
                        // key scale level; output rate
                        int num = (int)(idx & 7);
                        UInt32 base_ = (idx - ARC_KSL_OUTLEV) & 0xff;
                        if ((num < 6) && (base_ < 22))
                        {
                            UInt32 modop = regbase2modop[second_set != 0 ? (base_ + 22) : base_];
                            UInt32 chanbase = second_set != 0 ? (modop - 18 + ARC_SECONDSET) : modop;

                            // change frequency calculations of this operator as
                            // key scale level and output rate can be changed
                            op_type op_ptr = OPL.op[modop + ((num < 3) ? 0 : 9)];
                            //#if defined(OPLTYPE_IS_OPL3)
                            //			UInt32 regbase = base+second_set;
                            //			if ((OPL.adlibreg[0x105]&1) && (OPL.op[modop].is_4op_attached))
                            //			{
                            //				// operator uses frequency of channel
                            //				change_frequency(chip, chanbase-3,regbase,op_ptr);
                            //			}
                            //			else
                            //			{
                            //				change_frequency(chip, chanbase,regbase,op_ptr);
                            //			}
                            //#else
                            change_frequency(chip, chanbase, base_, op_ptr);
                            //#endif
                        }
                    }
                    break;
                case ARC_ATTR_DECR:
                case ARC_ATTR_DECR + 0x10:
                    {
                        // attack/decay rates
                        int num = (int)(idx & 7);
                        UInt32 base_ = (idx - ARC_ATTR_DECR) & 0xff;
                        if ((num < 6) && (base_ < 22))
                        {
                            UInt32 regbase = base_ + second_set;

                            // change attack rate and decay rate of this operator
                            op_type op_ptr = OPL.op[regbase2op[second_set != 0 ? (base_ + 22) : base_]];
                            change_attackrate(chip, regbase, op_ptr);
                            change_decayrate(chip, regbase, op_ptr);
                        }
                    }
                    break;
                case ARC_SUSL_RELR:
                case ARC_SUSL_RELR + 0x10:
                    {
                        // sustain level; release rate
                        int num = (int)(idx & 7);
                        UInt32 base_ = (idx - ARC_SUSL_RELR) & 0xff;
                        if ((num < 6) && (base_ < 22))
                        {
                            UInt32 regbase = base_ + second_set;

                            // change sustain level and release rate of this operator
                            op_type op_ptr = OPL.op[regbase2op[second_set != 0 ? (base_ + 22) : base_]];
                            change_releaserate(chip, regbase, op_ptr);
                            change_sustainlevel(chip, regbase, op_ptr);
                        }
                    }
                    break;
                case ARC_FREQ_NUM:
                    {
                        // 0xa0-0xa8 low8 frequency
                        UInt32 base_ = (idx - ARC_FREQ_NUM) & 0xff;
                        if (base_ < 9)
                        {
                            Int32 opbase = (Int32)(second_set != 0 ? (base_ + 18) : base_);
                            Int32 modbase;
                            UInt32 chanbase;
                            //#if defined(OPLTYPE_IS_OPL3)
                            //if ((OPL.adlibreg[0x105]&1) && OPL.op[opbase].is_4op_attached) break;
                            //#endif
                            // regbase of modulator:
                            modbase = (int)(modulatorbase[base_] + second_set);

                            chanbase = base_ + second_set;

                            change_frequency(chip, chanbase, (uint)modbase, OPL.op[opbase]);
                            change_frequency(chip, chanbase, (uint)(modbase + 3), OPL.op[opbase + 9]);
                            //#if defined(OPLTYPE_IS_OPL3)
                            //			// for 4op channels all four operators are modified to the frequency of the channel
                            //			if ((OPL.adlibreg[0x105]&1) && OPL.op[second_set?(base+18):base].is_4op)
                            //			{
                            //				change_frequency(chip, chanbase,modbase+8,&OPL.op[opbase+3]);
                            //				change_frequency(chip, chanbase,modbase+3+8,&OPL.op[opbase+3+9]);
                            //			}
                            //#endif
                        }
                    }
                    break;
                case ARC_KON_BNUM:
                    {
                        UInt32 base_;
                        if (OPL.UpdateHandler != null) // hack for DOSBox logs
                            OPL.UpdateHandler(OPL.UpdateParam);
                        if (idx == ARC_PERC_MODE)
                        {
                            //#if defined(OPLTYPE_IS_OPL3)
                            //if (second_set) return;
                            //#endif

                            if ((val & 0x30) == 0x30)
                            {       // BassDrum active
                                enable_operator(chip, 16, OPL.op[6], OP_ACT_PERC);
                                change_frequency(chip, 6, 16, OPL.op[6]);
                                enable_operator(chip, 16 + 3, OPL.op[6 + 9], OP_ACT_PERC);
                                change_frequency(chip, 6, 16 + 3, OPL.op[6 + 9]);
                            }
                            else
                            {
                                disable_operator(OPL.op[6], OP_ACT_PERC);
                                disable_operator(OPL.op[6 + 9], OP_ACT_PERC);
                            }
                            if ((val & 0x28) == 0x28)
                            {       // Snare active
                                enable_operator(chip, 17 + 3, OPL.op[16], OP_ACT_PERC);
                                change_frequency(chip, 7, 17 + 3, OPL.op[16]);
                            }
                            else
                            {
                                disable_operator(OPL.op[16], OP_ACT_PERC);
                            }
                            if ((val & 0x24) == 0x24)
                            {       // TomTom active
                                enable_operator(chip, 18, OPL.op[8], OP_ACT_PERC);
                                change_frequency(chip, 8, 18, OPL.op[8]);
                            }
                            else
                            {
                                disable_operator(OPL.op[8], OP_ACT_PERC);
                            }
                            if ((val & 0x22) == 0x22)
                            {       // Cymbal active
                                enable_operator(chip, 18 + 3, OPL.op[8 + 9], OP_ACT_PERC);
                                change_frequency(chip, 8, 18 + 3, OPL.op[8 + 9]);
                            }
                            else
                            {
                                disable_operator(OPL.op[8 + 9], OP_ACT_PERC);
                            }
                            if ((val & 0x21) == 0x21)
                            {       // Hihat active
                                enable_operator(chip, 17, OPL.op[7], OP_ACT_PERC);
                                change_frequency(chip, 7, 17, OPL.op[7]);
                            }
                            else
                            {
                                disable_operator(OPL.op[7], OP_ACT_PERC);
                            }

                            break;
                        }
                        // regular 0xb0-0xb8
                        base_ = (idx - ARC_KON_BNUM) & 0xff;
                        if (base_ < 9)
                        {
                            Int32 opbase = (Int32)(second_set != 0 ? (base_ + 18) : base_);
                            // regbase of modulator:
                            Int32 modbase = (Int32)(modulatorbase[base_] + second_set);
                            UInt32 chanbase;

                            //#if defined(OPLTYPE_IS_OPL3)
                            //if ((OPL.adlibreg[0x105]&1) && OPL.op[opbase].is_4op_attached) break;
                            //#endif
                            if ((val & 32) != 0)
                            {
                                // operator switched on
                                enable_operator(chip, (UInt32)modbase, OPL.op[opbase], OP_ACT_NORMAL);        // modulator (if 2op)
                                enable_operator(chip, (UInt32)(modbase + 3), OPL.op[opbase + 9], OP_ACT_NORMAL);    // carrier (if 2op)
                                                                                                                    //#if defined(OPLTYPE_IS_OPL3)
                                                                                                                    //				// for 4op channels all four operators are switched on
                                                                                                                    //				if ((OPL.adlibreg[0x105]&1) && OPL.op[opbase].is_4op)
                                                                                                                    //				{
                                                                                                                    //					// turn on chan+3 operators as well
                                                                                                                    //					enable_operator(chip, modbase+8,&OPL.op[opbase+3],OP_ACT_NORMAL);
                                                                                                                    //					enable_operator(chip, modbase+3+8,&OPL.op[opbase+3+9],OP_ACT_NORMAL);
                                                                                                                    //				}
                                                                                                                    //#endif
                            }
                            else
                            {
                                // operator switched off
                                disable_operator(OPL.op[opbase], OP_ACT_NORMAL);
                                disable_operator(OPL.op[opbase + 9], OP_ACT_NORMAL);
                                //#if defined(OPLTYPE_IS_OPL3)
                                //				// for 4op channels all four operators are switched off
                                //				if ((OPL.adlibreg[0x105]&1) && OPL.op[opbase].is_4op)
                                //				{
                                //					// turn off chan+3 operators as well
                                //					disable_operator(&OPL.op[opbase+3],OP_ACT_NORMAL);
                                //					disable_operator(&OPL.op[opbase+3+9],OP_ACT_NORMAL);
                                //				}
                                //#endif
                            }

                            chanbase = base_ + second_set;

                            // change frequency calculations of modulator and carrier (2op) as
                            // the frequency of the channel has changed
                            change_frequency(chip, chanbase, (UInt32)modbase, OPL.op[opbase]);
                            change_frequency(chip, chanbase, (UInt32)(modbase + 3), OPL.op[opbase + 9]);
                            //#if defined(OPLTYPE_IS_OPL3)
                            //			// for 4op channels all four operators are modified to the frequency of the channel
                            //			if ((OPL.adlibreg[0x105]&1) && OPL.op[second_set?(base+18):base].is_4op)
                            //			{
                            //				// change frequency calculations of chan+3 operators as well
                            //				change_frequency(chip, chanbase,modbase+8,&OPL.op[opbase+3]);
                            //				change_frequency(chip, chanbase,modbase+3+8,&OPL.op[opbase+3+9]);
                            //			}
                            //#endif
                        }
                    }
                    break;
                case ARC_FEEDBACK:
                    {
                        // 0xc0-0xc8 feedback/modulation type (AM/FM)
                        UInt32 base_ = (idx - ARC_FEEDBACK) & 0xff;
                        if (base_ < 9)
                        {
                            Int32 opbase = (Int32)(second_set != 0 ? (base_ + 18) : base_);
                            UInt32 chanbase = base_ + second_set;
                            change_feedback(chip, chanbase, OPL.op[opbase]);
                            //#if defined(OPLTYPE_IS_OPL3)
                            //			// OPL3 panning
                            //			OPL.op[opbase].left_pan = ((val&0x10)>>4);
                            //			OPL.op[opbase].right_pan = ((val&0x20)>>5);
                            //			OPL.op[opbase].left_pan += ((val&0x40)>>6);
                            //			OPL.op[opbase].right_pan += ((val&0x80)>>7);
                            //#endif
                        }
                    }
                    break;
                case ARC_WAVE_SEL:
                case ARC_WAVE_SEL + 0x10:
                    {
                        int num = (Int32)(idx & 7);
                        UInt32 base_ = (idx - ARC_WAVE_SEL) & 0xff;
                        if ((num < 6) && (base_ < 22))
                        {
                            //#if defined(OPLTYPE_IS_OPL3)
                            //			Int32 wselbase = second_set?(base+22):base;	// for easier mapping onto wave_sel[]
                            //			op_type* op_ptr;
                            //			// change waveform
                            //			if (OPL.adlibreg[0x105]&1) OPL.wave_sel[wselbase] = val&7;	// opl3 mode enabled, all waveforms accessible
                            //			else OPL.wave_sel[wselbase] = val&3;
                            //			op_ptr = &OPL.op[regbase2modop[wselbase]+((num<3) ? 0 : 9)];
                            //			change_waveform(chip, wselbase,op_ptr);
                            //#else
                            if ((OPL.adlibreg[0x01] & 0x20) != 0)
                            {
                                op_type op_ptr;

                                // wave selection enabled, change waveform
                                OPL.wave_sel[base_] = (byte)(val & 3);
                                op_ptr = OPL.op[regbase2modop[base_] + ((num < 3) ? 0 : 9)];
                                change_waveform(chip, base_, op_ptr);
                            }
                            //#endif
                        }
                    }
                    break;
                default:
                    break;
            }
        }


        private UInt32 ADLIBEMU_reg_read(OPL_DATA chip, UInt32 port)
        {
            OPL_DATA OPL = (OPL_DATA)chip;

            //#if defined(OPLTYPE_IS_OPL3)
            //	// opl3-detection routines require ret&6 to be zero
            //	if ((port&1)==0)
            //	{
            //		return OPL.status;
            //	}
            //	return 0x00;
            //#else
            // opl2-detection routines require ret&6 to be 6
            if ((port & 1) == 0)
            {
                return (UInt32)(OPL.status | 6);
            }
            return 0xff;
            //#endif
        }

        private void ADLIBEMU_write_index(OPL_DATA chip, UInt32 port, byte val)
        {
            OPL_DATA OPL = (OPL_DATA)chip;

            OPL.opl_index = val;
            //#if defined(OPLTYPE_IS_OPL3)
            //	if ((port&3)!=0)
            //	{
            //		// possibly second set
            //		if (((OPL.adlibreg[0x105]&1)!=0) || (OPL.opl_index==5)) OPL.opl_index |= ARC_SECONDSET;
            //	}
            //#endif
        }

        /*static void OPL_INLINE clipit16(Int32 ival, Int16* outval)
        {
            if (ival<32768)
            {
                if (ival>-32769)
                {
                    *outval=(Int16)ival;
                }
                else
                {
                    *outval = -32768;
                }
            }
            else
            {
                *outval = 32767;
            }
        }*/



        // be careful with this
        // uses cptr and chanval, outputs into outbufl(/outbufr)
        // for opl3 check if opl3-mode is enabled (which uses stereo panning)
        // 
        // Changes by Valley Bell:
        //	- Changed to always output to both channels
        //	- added parameter "chn" to fix panning for 4-op channels and the Rhythm Cymbal
        //#undef CHANVAL_OUT
        //#if defined(OPLTYPE_IS_OPL3)
        //#define CHANVAL_OUT(chn)								\
        //	if (OPL.adlibreg[0x105]&1) {						\
        //		outbufl[i] += chanval*cptr[chn].left_pan;		\
        //		outbufr[i] += chanval*cptr[chn].right_pan;	\
        //	} else {										\
        //		outbufl[i] += chanval;						\
        //		outbufr[i] += chanval;						\
        //	}
        //#else
        private void CHANVAL_OUT(int chn, Int32[] outbufl, Int32[] outbufr, int i, int chanval)
        {
            outbufl[i] += chanval;
            outbufr[i] += chanval;
        }
        //#endif

        private Int32[] vib_lut = new Int32[BLOCKBUF_SIZE];
        private Int32[] trem_lut = new Int32[BLOCKBUF_SIZE];

        //void adlib_getsample(Int16* sndptr, Int32 numsamples)
        private void ADLIBEMU_getsample(OPL_DATA chip, Int32[][] sndptr, Int32 numsamples)
        {
            OPL_DATA OPL = (OPL_DATA)chip;

            Int32 i, endsamples;
            op_type[] cptr;
            int cptr_ptr;

            //Int32 outbufl[BLOCKBUF_SIZE];
            //#if defined(OPLTYPE_IS_OPL3)
            //	// second output buffer (right channel for opl3 stereo)
            //	//Int32 outbufr[BLOCKBUF_SIZE];
            //#endif
            Int32[] outbufl = sndptr[0];
            Int32[] outbufr = sndptr[1];

            // vibrato/tremolo lookup tables (global, to possibly be used by all operators)
            //Int32[] vib_lut = new Int32[BLOCKBUF_SIZE];
            //Int32[] trem_lut = new Int32[BLOCKBUF_SIZE];

            Int32 samples_to_process = numsamples;

            Int32 cursmp;
            Int32 vib_tshift;
            UInt32 max_channel = NUM_CHANNELS;
            Int32 cur_ch;

            Int32[] vibval1, vibval2, vibval3, vibval4;
            Int32[] tremval1, tremval2, tremval3, tremval4;

            //#if defined(OPLTYPE_IS_OPL3)
            //	if ((OPL.adlibreg[0x105]&1)==0) max_channel = NUM_CHANNELS/2;
            //#endif

            if (samples_to_process == 0)
            {
                for (cur_ch = 0; cur_ch < max_channel; cur_ch++)
                {
                    if ((OPL.adlibreg[ARC_PERC_MODE] & 0x20) != 0 && (cur_ch >= 6 && cur_ch < 9))
                        continue;

                    //#if defined(OPLTYPE_IS_OPL3)
                    //			if (cur_ch < 9)
                    //				cptr = &OPL.op[cur_ch];
                    //			else
                    //				cptr = &OPL.op[cur_ch+9];	// second set is operator18-operator35
                    //			if (cptr->is_4op_attached)
                    //				continue;
                    //#else
                    cptr = OPL.op;
                    cptr_ptr = cur_ch;
                    //#endif

                    if (cptr[cptr_ptr + 0].op_state == OF_TYPE_ATT)
                        operator_eg_attack_check(cptr[cptr_ptr + 0]);
                    if (cptr[cptr_ptr + 9].op_state == OF_TYPE_ATT)
                        operator_eg_attack_check(cptr[cptr_ptr + 9]);
                }

                return;
            }

            for (cursmp = 0; cursmp < samples_to_process; cursmp += endsamples)
            {
                endsamples = samples_to_process - cursmp;
                //if (endsamples>BLOCKBUF_SIZE) endsamples = BLOCKBUF_SIZE;

                //memset(outbufl, 0, endsamples * sizeof(Int32));
                //#if defined(OPLTYPE_IS_OPL3)
                //		// clear second output buffer (opl3 stereo)
                //		//if (adlibreg[0x105]&1)
                //memset(outbufr, 0, endsamples * sizeof(Int32));
                //#endif
                for (int ind = 0; ind < endsamples; ind++)
                {
                    outbufl[ind] = 0;
                    outbufr[ind] = 0;
                }

                // calculate vibrato/tremolo lookup tables
                vib_tshift = ((OPL.adlibreg[ARC_PERC_MODE] & 0x40) == 0) ? 1 : 0;   // 14cents/7cents switching
                for (i = 0; i < endsamples; i++)
                {
                    // cycle through vibrato table
                    OPL.vibtab_pos += OPL.vibtab_add;
                    if (OPL.vibtab_pos / FIXEDPT_LFO >= VIBTAB_SIZE)
                        OPL.vibtab_pos -= VIBTAB_SIZE * FIXEDPT_LFO;
                    vib_lut[i] = vib_table[OPL.vibtab_pos / FIXEDPT_LFO] >> vib_tshift;     // 14cents (14/100 of a semitone) or 7cents

                    // cycle through tremolo table
                    OPL.tremtab_pos += OPL.tremtab_add;
                    if (OPL.tremtab_pos / FIXEDPT_LFO >= TREMTAB_SIZE)
                        OPL.tremtab_pos -= TREMTAB_SIZE * FIXEDPT_LFO;
                    if ((OPL.adlibreg[ARC_PERC_MODE] & 0x80) != 0)
                        trem_lut[i] = trem_table[OPL.tremtab_pos / FIXEDPT_LFO];
                    else
                        trem_lut[i] = trem_table[TREMTAB_SIZE + OPL.tremtab_pos / FIXEDPT_LFO];
                }

                if ((OPL.adlibreg[ARC_PERC_MODE] & 0x20) != 0)
                {
                    if ((OPL.MuteChn[NUM_CHANNELS + 0]) == 0)
                    {
                        //BassDrum
                        cptr = OPL.op;
                        cptr_ptr = 6;
                        if ((OPL.adlibreg[ARC_FEEDBACK + 6] & 1) != 0)
                        {
                            // additive synthesis
                            if (cptr[cptr_ptr + 9].op_state != OF_TYPE_OFF)
                            {
                                if (cptr[cptr_ptr + 9].vibrato)
                                {
                                    vibval1 = vibval_var1;
                                    for (i = 0; i < endsamples; i++)
                                        vibval1[i] = (Int32)((vib_lut[i] * cptr[cptr_ptr + 9].freq_high / 8) * FIXEDPT * VIBFAC);
                                }
                                else
                                    vibval1 = vibval_const;
                                if (cptr[cptr_ptr + 9].tremolo)
                                    tremval1 = trem_lut;    // tremolo enabled, use table
                                else
                                    tremval1 = tremval_const;

                                // calculate channel output
                                for (i = 0; i < endsamples; i++)
                                {
                                    Int32 chanval;

                                    operator_advance(OPL, cptr[cptr_ptr + 9], vibval1[i]);
                                    opfuncs[cptr[cptr_ptr + 9].op_state](cptr[cptr_ptr + 9]);
                                    operator_output(cptr[cptr_ptr + 9], 0, tremval1[i]);

                                    chanval = cptr[cptr_ptr + 9].cval * 2;
                                    CHANVAL_OUT(0, outbufl, outbufr, i, chanval);


                                }
                            }
                        }
                        else
                        {
                            // frequency modulation
                            if ((cptr[cptr_ptr + 9].op_state != OF_TYPE_OFF) || (cptr[cptr_ptr + 0].op_state != OF_TYPE_OFF))
                            {
                                if ((cptr[cptr_ptr + 0].vibrato) && (cptr[cptr_ptr + 0].op_state != OF_TYPE_OFF))
                                {
                                    vibval1 = vibval_var1;
                                    for (i = 0; i < endsamples; i++)
                                        vibval1[i] = (Int32)((vib_lut[i] * cptr[cptr_ptr + 0].freq_high / 8) * FIXEDPT * VIBFAC);
                                }
                                else
                                    vibval1 = vibval_const;
                                if ((cptr[cptr_ptr + 9].vibrato) && (cptr[cptr_ptr + 9].op_state != OF_TYPE_OFF))
                                {
                                    vibval2 = vibval_var2;
                                    for (i = 0; i < endsamples; i++)
                                        vibval2[i] = (Int32)((vib_lut[i] * cptr[cptr_ptr + 9].freq_high / 8) * FIXEDPT * VIBFAC);
                                }
                                else
                                    vibval2 = vibval_const;
                                if (cptr[cptr_ptr + 0].tremolo)
                                    tremval1 = trem_lut;    // tremolo enabled, use table
                                else
                                    tremval1 = tremval_const;
                                if (cptr[cptr_ptr + 9].tremolo)
                                    tremval2 = trem_lut;    // tremolo enabled, use table
                                else
                                    tremval2 = tremval_const;

                                // calculate channel output
                                for (i = 0; i < endsamples; i++)
                                {
                                    Int32 chanval;

                                    operator_advance(OPL, cptr[cptr_ptr + 0], vibval1[i]);
                                    opfuncs[cptr[cptr_ptr + 0].op_state](cptr[cptr_ptr + 0]);
                                    operator_output(cptr[cptr_ptr + 0], (cptr[cptr_ptr + 0].lastcval + cptr[cptr_ptr + 0].cval) * cptr[cptr_ptr + 0].mfbi / 2, tremval1[i]);

                                    operator_advance(OPL, cptr[cptr_ptr + 9], vibval2[i]);
                                    opfuncs[cptr[cptr_ptr + 9].op_state](cptr[cptr_ptr + 9]);
                                    operator_output(cptr[cptr_ptr + 9], cptr[cptr_ptr + 0].cval * FIXEDPT, tremval2[i]);

                                    chanval = cptr[cptr_ptr + 9].cval * 2;
                                    CHANVAL_OUT(0, outbufl, outbufr, i, chanval);


                                }
                            }
                        }
                    }   // end if (! Muted)

                    //TomTom (j=8)
                    if ((OPL.MuteChn[NUM_CHANNELS + 2]) == 0 && OPL.op[8].op_state != OF_TYPE_OFF)
                    {
                        cptr = OPL.op;
                        cptr_ptr = 8;
                        if (cptr[cptr_ptr + 0].vibrato)
                        {
                            vibval3 = vibval_var1;
                            for (i = 0; i < endsamples; i++)
                                vibval3[i] = (Int32)((vib_lut[i] * cptr[cptr_ptr + 0].freq_high / 8) * FIXEDPT * VIBFAC);
                        }
                        else
                            vibval3 = vibval_const;

                        if (cptr[cptr_ptr + 0].tremolo)
                            tremval3 = trem_lut;    // tremolo enabled, use table
                        else
                            tremval3 = tremval_const;

                        // calculate channel output
                        for (i = 0; i < endsamples; i++)
                        {
                            Int32 chanval;

                            operator_advance(OPL, cptr[cptr_ptr + 0], vibval3[i]);
                            opfuncs[cptr[cptr_ptr + 0].op_state](cptr[cptr_ptr + 0]);     //TomTom
                            operator_output(cptr[cptr_ptr + 0], 0, tremval3[i]);
                            chanval = cptr[cptr_ptr + 0].cval * 2;
                            CHANVAL_OUT(0, outbufl, outbufr, i, chanval);


                        }
                    }

                    //Snare/Hihat (j=7), Cymbal (j=8)
                    if ((OPL.op[7].op_state != OF_TYPE_OFF) || (OPL.op[16].op_state != OF_TYPE_OFF) ||
                        (OPL.op[17].op_state != OF_TYPE_OFF))
                    {
                        cptr = OPL.op;
                        cptr_ptr = 7;
                        if ((cptr[cptr_ptr + 0].vibrato) && (cptr[cptr_ptr + 0].op_state != OF_TYPE_OFF))
                        {
                            vibval1 = vibval_var1;
                            for (i = 0; i < endsamples; i++)
                                vibval1[i] = (Int32)((vib_lut[i] * cptr[cptr_ptr + 0].freq_high / 8) * FIXEDPT * VIBFAC);
                        }
                        else
                            vibval1 = vibval_const;
                        if ((cptr[cptr_ptr + 9].vibrato) && (cptr[cptr_ptr + 9].op_state == OF_TYPE_OFF))
                        {
                            vibval2 = vibval_var2;
                            for (i = 0; i < endsamples; i++)
                                vibval2[i] = (Int32)((vib_lut[i] * cptr[cptr_ptr + 9].freq_high / 8) * FIXEDPT * VIBFAC);
                        }
                        else
                            vibval2 = vibval_const;

                        if (cptr[cptr_ptr + 0].tremolo)
                            tremval1 = trem_lut;    // tremolo enabled, use table
                        else
                            tremval1 = tremval_const;
                        if (cptr[cptr_ptr + 9].tremolo)
                            tremval2 = trem_lut;    // tremolo enabled, use table
                        else
                            tremval2 = tremval_const;

                        cptr = OPL.op;
                        cptr_ptr = 8;
                        if ((cptr[cptr_ptr + 9].vibrato) && (cptr[cptr_ptr + 9].op_state == OF_TYPE_OFF))
                        {
                            vibval4 = vibval_var2;
                            for (i = 0; i < endsamples; i++)
                                vibval4[i] = (Int32)((vib_lut[i] * cptr[cptr_ptr + 9].freq_high / 8) * FIXEDPT * VIBFAC);
                        }
                        else
                            vibval4 = vibval_const;

                        if (cptr[cptr_ptr + 9].tremolo) tremval4 = trem_lut;   // tremolo enabled, use table
                        else tremval4 = tremval_const;

                        // calculate channel output
                        cptr = OPL.op;   // set cptr to something useful (else it stays at op[8])
                        cptr_ptr = 0;
                        for (i = 0; i < endsamples; i++)
                        {
                            Int32 chanval;

                            operator_advance_drums(OPL, OPL.op[7], vibval1[i], OPL.op[7 + 9], vibval2[i], OPL.op[8 + 9], vibval4[i]);

                            if ((OPL.MuteChn[NUM_CHANNELS + 4]) == 0)
                            {
                                opfuncs[OPL.op[7].op_state](OPL.op[7]);         //Hihat
                                operator_output(OPL.op[7], 0, tremval1[i]);
                            }
                            else
                                OPL.op[7].cval = 0;

                            if ((OPL.MuteChn[NUM_CHANNELS + 1]) == 0)
                            {
                                opfuncs[OPL.op[7 + 9].op_state](OPL.op[7 + 9]);     //Snare
                                operator_output(OPL.op[7 + 9], 0, tremval2[i]);
                            }
                            else
                                OPL.op[7 + 9].cval = 0;

                            if ((OPL.MuteChn[NUM_CHANNELS + 3]) == 0)
                            {
                                opfuncs[OPL.op[8 + 9].op_state](OPL.op[8 + 9]);     //Cymbal
                                operator_output(OPL.op[8 + 9], 0, tremval4[i]);
                            }
                            else
                                OPL.op[8 + 9].cval = 0;

                            //chanval = (OPL.op[7].cval + OPL.op[7+9].cval + OPL.op[8+9].cval)*2;
                            //CHANVAL_OUT(0)
                            // fix panning of the snare -Valley Bell
                            chanval = (OPL.op[7].cval + OPL.op[7 + 9].cval) * 2;
                            CHANVAL_OUT(7, outbufl, outbufr, i, chanval);

                            chanval = OPL.op[8 + 9].cval * 2;
                            CHANVAL_OUT(8, outbufl, outbufr, i, chanval);

                        }
                    }
                }

                for (cur_ch = (int)(max_channel - 1); cur_ch >= 0; cur_ch--)
                {
                    UInt32 k;

                    if (OPL.MuteChn[cur_ch] != 0)
                        continue;

                    // skip drum/percussion operators
                    if ((OPL.adlibreg[ARC_PERC_MODE] & 0x20) != 0 && (cur_ch >= 6) && (cur_ch < 9)) continue;

                    k = (uint)cur_ch;
                    //#if defined(OPLTYPE_IS_OPL3)
                    //			if (cur_ch < 9)
                    //			{
                    //				cptr = &OPL.op[cur_ch];
                    //			}
                    //			else
                    //			{
                    //				cptr = &OPL.op[cur_ch+9];	// second set is operator18-operator35
                    //				k += (-9+256);		// second set uses registers 0x100 onwards
                    //			}
                    //			// check if this operator is part of a 4-op
                    //			//if ((OPL.adlibreg[0x105]&1) && cptr->is_4op_attached) continue;
                    //			if (cptr->is_4op_attached) continue;	// this is more correct
                    //#else
                    cptr = OPL.op;
                    cptr_ptr = cur_ch;
                    //#endif

                    // check for FM/AM
                    if ((OPL.adlibreg[ARC_FEEDBACK + k] & 1) != 0)
                    {
                        //#if defined(OPLTYPE_IS_OPL3)
                        //				//if ((OPL.adlibreg[0x105]&1) && cptr->is_4op)
                        //				if (cptr->is_4op)	// this is more correct
                        //				{
                        //					if (OPL.adlibreg[ARC_FEEDBACK+k+3]&1)
                        //					{
                        //						// AM-AM-style synthesis (op1[fb] + (op2 * op3) + op4)
                        //						if (cptr[0].op_state != OF_TYPE_OFF)
                        //						{
                        //							if (cptr[0].vibrato)
                        //							{
                        //								vibval1 = vibval_var1;
                        //								for (i=0;i<endsamples;i++)
                        //									vibval1[i] = (Int32)((vib_lut[i]*cptr[0].freq_high/8)*FIXEDPT*VIBFAC);
                        //							}
                        //							else
                        //								vibval1 = vibval_const;
                        //							if (cptr[0].tremolo)
                        //								tremval1 = trem_lut;	// tremolo enabled, use table
                        //							else
                        //								tremval1 = tremval_const;

                        //							// calculate channel output
                        //							for (i=0;i<endsamples;i++)
                        //							{
                        //								Int32 chanval;

                        //								operator_advance(OPL, &cptr[0],vibval1[i]);
                        //								opfuncs[cptr[0].op_state](&cptr[0]);
                        //								operator_output(&cptr[0],(cptr[0].lastcval+cptr[0].cval)*cptr[0].mfbi/2,tremval1[i]);

                        //								chanval = cptr[0].cval;
                        //								CHANVAL_OUT(3)	// Note: Op 1 of 4, so it needs to use the panning bits of Op 4 (Ch+3)
                        //							}
                        //						}

                        //						if ((cptr[3].op_state != OF_TYPE_OFF) || (cptr[9].op_state != OF_TYPE_OFF))
                        //						{
                        //							if ((cptr[9].vibrato) && (cptr[9].op_state != OF_TYPE_OFF))
                        //							{
                        //								vibval1 = vibval_var1;
                        //								for (i=0;i<endsamples;i++)
                        //									vibval1[i] = (Int32)((vib_lut[i]*cptr[9].freq_high/8)*FIXEDPT*VIBFAC);
                        //							}
                        //							else
                        //								vibval1 = vibval_const;
                        //							if (cptr[9].tremolo)
                        //								tremval1 = trem_lut;	// tremolo enabled, use table
                        //							else
                        //								tremval1 = tremval_const;
                        //							if (cptr[3].tremolo)
                        //								tremval2 = trem_lut;	// tremolo enabled, use table
                        //							else
                        //								tremval2 = tremval_const;

                        //							// calculate channel output
                        //							for (i=0;i<endsamples;i++)
                        //							{
                        //								Int32 chanval;

                        //								operator_advance(OPL, &cptr[9],vibval1[i]);
                        //								opfuncs[cptr[9].op_state](&cptr[9]);
                        //								operator_output(&cptr[9],0,tremval1[i]);

                        //								operator_advance(OPL, &cptr[3],0);
                        //								opfuncs[cptr[3].op_state](&cptr[3]);
                        //								operator_output(&cptr[3],cptr[9].cval*FIXEDPT,tremval2[i]);

                        //								chanval = cptr[3].cval;
                        //								CHANVAL_OUT(3)
                        //							}
                        //						}

                        //						if (cptr[3+9].op_state != OF_TYPE_OFF)
                        //						{
                        //							if (cptr[3+9].tremolo)
                        //								tremval1 = trem_lut;	// tremolo enabled, use table
                        //							else
                        //								tremval1 = tremval_const;

                        //							// calculate channel output
                        //							for (i=0;i<endsamples;i++)
                        //							{
                        //								Int32 chanval;

                        //								operator_advance(OPL, &cptr[3+9],0);
                        //								opfuncs[cptr[3+9].op_state](&cptr[3+9]);
                        //								operator_output(&cptr[3+9],0,tremval1[i]);

                        //								chanval = cptr[3+9].cval;
                        //								CHANVAL_OUT(3)
                        //							}
                        //						}
                        //					}
                        //					else
                        //					{
                        //						// AM-FM-style synthesis (op1[fb] + (op2 * op3 * op4))
                        //						if (cptr[0].op_state != OF_TYPE_OFF)
                        //						{
                        //							if (cptr[0].vibrato)
                        //							{
                        //								vibval1 = vibval_var1;
                        //								for (i=0;i<endsamples;i++)
                        //									vibval1[i] = (Int32)((vib_lut[i]*cptr[0].freq_high/8)*FIXEDPT*VIBFAC);
                        //							}
                        //							else
                        //								vibval1 = vibval_const;
                        //							if (cptr[0].tremolo)
                        //								tremval1 = trem_lut;	// tremolo enabled, use table
                        //							else
                        //								tremval1 = tremval_const;

                        //							// calculate channel output
                        //							for (i=0;i<endsamples;i++)
                        //							{
                        //								Int32 chanval;

                        //								operator_advance(OPL, &cptr[0],vibval1[i]);
                        //								opfuncs[cptr[0].op_state](&cptr[0]);
                        //								operator_output(&cptr[0],(cptr[0].lastcval+cptr[0].cval)*cptr[0].mfbi/2,tremval1[i]);

                        //								chanval = cptr[0].cval;
                        //								CHANVAL_OUT(3)
                        //							}
                        //						}

                        //						if ((cptr[9].op_state != OF_TYPE_OFF) || (cptr[3].op_state != OF_TYPE_OFF) || (cptr[3+9].op_state != OF_TYPE_OFF))
                        //						{
                        //							if ((cptr[9].vibrato) && (cptr[9].op_state != OF_TYPE_OFF)) {
                        //								vibval1 = vibval_var1;
                        //								for (i=0;i<endsamples;i++)
                        //									vibval1[i] = (Int32)((vib_lut[i]*cptr[9].freq_high/8)*FIXEDPT*VIBFAC);
                        //							}
                        //							else
                        //								vibval1 = vibval_const;
                        //							if (cptr[9].tremolo)
                        //								tremval1 = trem_lut;	// tremolo enabled, use table
                        //							else
                        //								tremval1 = tremval_const;
                        //							if (cptr[3].tremolo)
                        //								tremval2 = trem_lut;	// tremolo enabled, use table
                        //							else
                        //								tremval2 = tremval_const;
                        //							if (cptr[3+9].tremolo)
                        //								tremval3 = trem_lut;	// tremolo enabled, use table
                        //							else
                        //								tremval3 = tremval_const;

                        //							// calculate channel output
                        //							for (i=0;i<endsamples;i++)
                        //							{
                        //								Int32 chanval;

                        //								operator_advance(OPL, &cptr[9],vibval1[i]);
                        //								opfuncs[cptr[9].op_state](&cptr[9]);
                        //								operator_output(&cptr[9],0,tremval1[i]);

                        //								operator_advance(OPL, &cptr[3],0);
                        //								opfuncs[cptr[3].op_state](&cptr[3]);
                        //								operator_output(&cptr[3],cptr[9].cval*FIXEDPT,tremval2[i]);

                        //								operator_advance(OPL, &cptr[3+9],0);
                        //								opfuncs[cptr[3+9].op_state](&cptr[3+9]);
                        //								operator_output(&cptr[3+9],cptr[3].cval*FIXEDPT,tremval3[i]);

                        //								chanval = cptr[3+9].cval;
                        //								CHANVAL_OUT(3)
                        //							}
                        //						}
                        //					}
                        //					continue;
                        //				}
                        //#endif
                        // 2op additive synthesis
                        if ((cptr[cptr_ptr + 9].op_state == OF_TYPE_OFF) && (cptr[cptr_ptr + 0].op_state == OF_TYPE_OFF)) continue;
                        if ((cptr[cptr_ptr + 0].vibrato) && (cptr[cptr_ptr + 0].op_state != OF_TYPE_OFF))
                        {
                            vibval1 = vibval_var1;
                            for (i = 0; i < endsamples; i++)
                                vibval1[i] = (Int32)((vib_lut[i] * cptr[cptr_ptr + 0].freq_high / 8) * FIXEDPT * VIBFAC);
                        }
                        else
                            vibval1 = vibval_const;
                        if ((cptr[cptr_ptr + 9].vibrato) && (cptr[cptr_ptr + 9].op_state != OF_TYPE_OFF))
                        {
                            vibval2 = vibval_var2;
                            for (i = 0; i < endsamples; i++)
                                vibval2[i] = (Int32)((vib_lut[i] * cptr[cptr_ptr + 9].freq_high / 8) * FIXEDPT * VIBFAC);
                        }
                        else
                            vibval2 = vibval_const;
                        if (cptr[cptr_ptr + 0].tremolo)
                            tremval1 = trem_lut;    // tremolo enabled, use table
                        else
                            tremval1 = tremval_const;
                        if (cptr[cptr_ptr + 9].tremolo)
                            tremval2 = trem_lut;    // tremolo enabled, use table
                        else
                            tremval2 = tremval_const;

                        // calculate channel output
                        for (i = 0; i < endsamples; i++)
                        {
                            Int32 chanval;

                            // carrier1
                            operator_advance(OPL, cptr[cptr_ptr + 0], vibval1[i]);
                            opfuncs[cptr[cptr_ptr + 0].op_state](cptr[cptr_ptr + 0]);
                            operator_output(cptr[cptr_ptr + 0], (cptr[cptr_ptr + 0].lastcval + cptr[cptr_ptr + 0].cval) * cptr[cptr_ptr + 0].mfbi / 2, tremval1[i]);

                            // carrier2
                            operator_advance(OPL, cptr[cptr_ptr + 9], vibval2[i]);
                            opfuncs[cptr[cptr_ptr + 9].op_state](cptr[cptr_ptr + 9]);
                            operator_output(cptr[cptr_ptr + 9], 0, tremval2[i]);

                            chanval = cptr[cptr_ptr + 9].cval + cptr[cptr_ptr + 0].cval;
                            CHANVAL_OUT(0, outbufl, outbufr, i, chanval);
                        }
                    }
                    else
                    {
                        //#if defined(OPLTYPE_IS_OPL3)
                        //				//if ((OPL.adlibreg[0x105]&1) && cptr->is_4op)
                        //				if (cptr->is_4op)	// this is more correct
                        //				{
                        //					if (OPL.adlibreg[ARC_FEEDBACK+k+3]&1)
                        //					{
                        //						// FM-AM-style synthesis ((op1[fb] * op2) + (op3 * op4))
                        //						if ((cptr[0].op_state != OF_TYPE_OFF) || (cptr[9].op_state != OF_TYPE_OFF))
                        //						{
                        //							if ((cptr[0].vibrato) && (cptr[0].op_state != OF_TYPE_OFF))
                        //							{
                        //								vibval1 = vibval_var1;
                        //								for (i=0;i<endsamples;i++)
                        //									vibval1[i] = (Int32)((vib_lut[i]*cptr[0].freq_high/8)*FIXEDPT*VIBFAC);
                        //							}
                        //							else
                        //								vibval1 = vibval_const;
                        //							if ((cptr[9].vibrato) && (cptr[9].op_state != OF_TYPE_OFF))
                        //							{
                        //								vibval2 = vibval_var2;
                        //								for (i=0;i<endsamples;i++)
                        //									vibval2[i] = (Int32)((vib_lut[i]*cptr[9].freq_high/8)*FIXEDPT*VIBFAC);
                        //							}
                        //							else
                        //								vibval2 = vibval_const;
                        //							if (cptr[0].tremolo)
                        //								tremval1 = trem_lut;	// tremolo enabled, use table
                        //							else
                        //								tremval1 = tremval_const;
                        //							if (cptr[9].tremolo)
                        //								tremval2 = trem_lut;	// tremolo enabled, use table
                        //							else
                        //								tremval2 = tremval_const;

                        //							// calculate channel output
                        //							for (i=0;i<endsamples;i++)
                        //							{
                        //								Int32 chanval;

                        //								operator_advance(OPL, &cptr[0],vibval1[i]);
                        //								opfuncs[cptr[0].op_state](&cptr[0]);
                        //								operator_output(&cptr[0],(cptr[0].lastcval+cptr[0].cval)*cptr[0].mfbi/2,tremval1[i]);

                        //								operator_advance(OPL, &cptr[9],vibval2[i]);
                        //								opfuncs[cptr[9].op_state](&cptr[9]);
                        //								operator_output(&cptr[9],cptr[0].cval*FIXEDPT,tremval2[i]);

                        //								chanval = cptr[9].cval;
                        //								CHANVAL_OUT(3)
                        //							}
                        //						}

                        //						if ((cptr[3].op_state != OF_TYPE_OFF) || (cptr[3+9].op_state != OF_TYPE_OFF))
                        //						{
                        //							if (cptr[3].tremolo)
                        //								tremval1 = trem_lut;	// tremolo enabled, use table
                        //							else
                        //								tremval1 = tremval_const;
                        //							if (cptr[3+9].tremolo)
                        //								tremval2 = trem_lut;	// tremolo enabled, use table
                        //							else
                        //								tremval2 = tremval_const;

                        //							// calculate channel output
                        //							for (i=0;i<endsamples;i++)
                        //							{
                        //								Int32 chanval;

                        //								operator_advance(OPL, &cptr[3],0);
                        //								opfuncs[cptr[3].op_state](&cptr[3]);
                        //								operator_output(&cptr[3],0,tremval1[i]);

                        //								operator_advance(OPL, &cptr[3+9],0);
                        //								opfuncs[cptr[3+9].op_state](&cptr[3+9]);
                        //								operator_output(&cptr[3+9],cptr[3].cval*FIXEDPT,tremval2[i]);

                        //								chanval = cptr[3+9].cval;
                        //								CHANVAL_OUT(3)
                        //							}
                        //						}

                        //					}
                        //					else
                        //					{
                        //						// FM-FM-style synthesis (op1[fb] * op2 * op3 * op4)
                        //						if ((cptr[0].op_state != OF_TYPE_OFF) || (cptr[9].op_state != OF_TYPE_OFF) || 
                        //							(cptr[3].op_state != OF_TYPE_OFF) || (cptr[3+9].op_state != OF_TYPE_OFF))
                        //						{
                        //							if ((cptr[0].vibrato) && (cptr[0].op_state != OF_TYPE_OFF))
                        //							{
                        //								vibval1 = vibval_var1;
                        //								for (i=0;i<endsamples;i++)
                        //									vibval1[i] = (Int32)((vib_lut[i]*cptr[0].freq_high/8)*FIXEDPT*VIBFAC);
                        //							}
                        //							else
                        //								vibval1 = vibval_const;
                        //							if ((cptr[9].vibrato) && (cptr[9].op_state != OF_TYPE_OFF))
                        //							{
                        //								vibval2 = vibval_var2;
                        //								for (i=0;i<endsamples;i++)
                        //									vibval2[i] = (Int32)((vib_lut[i]*cptr[9].freq_high/8)*FIXEDPT*VIBFAC);
                        //							}
                        //							else
                        //								vibval2 = vibval_const;
                        //							if (cptr[0].tremolo)
                        //								tremval1 = trem_lut;	// tremolo enabled, use table
                        //							else
                        //								tremval1 = tremval_const;
                        //							if (cptr[9].tremolo)
                        //								tremval2 = trem_lut;	// tremolo enabled, use table
                        //							else
                        //								tremval2 = tremval_const;
                        //							if (cptr[3].tremolo)
                        //								tremval3 = trem_lut;	// tremolo enabled, use table
                        //							else
                        //								tremval3 = tremval_const;
                        //							if (cptr[3+9].tremolo)
                        //								tremval4 = trem_lut;	// tremolo enabled, use table
                        //							else
                        //								tremval4 = tremval_const;

                        //							// calculate channel output
                        //							for (i=0;i<endsamples;i++)
                        //							{
                        //								Int32 chanval;

                        //								operator_advance(OPL, &cptr[0],vibval1[i]);
                        //								opfuncs[cptr[0].op_state](&cptr[0]);
                        //								operator_output(&cptr[0],(cptr[0].lastcval+cptr[0].cval)*cptr[0].mfbi/2,tremval1[i]);

                        //								operator_advance(OPL, &cptr[9],vibval2[i]);
                        //								opfuncs[cptr[9].op_state](&cptr[9]);
                        //								operator_output(&cptr[9],cptr[0].cval*FIXEDPT,tremval2[i]);

                        //								operator_advance(OPL, &cptr[3],0);
                        //								opfuncs[cptr[3].op_state](&cptr[3]);
                        //								operator_output(&cptr[3],cptr[9].cval*FIXEDPT,tremval3[i]);

                        //								operator_advance(OPL, &cptr[3+9],0);
                        //								opfuncs[cptr[3+9].op_state](&cptr[3+9]);
                        //								operator_output(&cptr[3+9],cptr[3].cval*FIXEDPT,tremval4[i]);

                        //								chanval = cptr[3+9].cval;
                        //								CHANVAL_OUT(3)
                        //							}
                        //						}
                        //					}
                        //					continue;
                        //				}
                        //#endif
                        // 2op frequency modulation
                        if ((cptr[cptr_ptr + 9].op_state == OF_TYPE_OFF) && (cptr[cptr_ptr + 0].op_state == OF_TYPE_OFF)) continue;
                        if ((cptr[cptr_ptr + 0].vibrato) && (cptr[cptr_ptr + 0].op_state != OF_TYPE_OFF))
                        {
                            vibval1 = vibval_var1;
                            for (i = 0; i < endsamples; i++)
                                vibval1[i] = (Int32)((vib_lut[i] * cptr[cptr_ptr + 0].freq_high / 8) * FIXEDPT * VIBFAC);
                        }
                        else
                            vibval1 = vibval_const;
                        if ((cptr[cptr_ptr + 9].vibrato) && (cptr[cptr_ptr + 9].op_state != OF_TYPE_OFF))
                        {
                            vibval2 = vibval_var2;
                            for (i = 0; i < endsamples; i++)
                                vibval2[i] = (Int32)((vib_lut[i] * cptr[cptr_ptr + 9].freq_high / 8) * FIXEDPT * VIBFAC);
                        }
                        else
                            vibval2 = vibval_const;
                        if (cptr[cptr_ptr + 0].tremolo)
                            tremval1 = trem_lut;    // tremolo enabled, use table
                        else
                            tremval1 = tremval_const;
                        if (cptr[cptr_ptr + 9].tremolo)
                            tremval2 = trem_lut;    // tremolo enabled, use table
                        else
                            tremval2 = tremval_const;

                        // calculate channel output
                        for (i = 0; i < endsamples; i++)
                        {
                            Int32 chanval;

                            // modulator
                            operator_advance(OPL, cptr[cptr_ptr + 0], vibval1[i]);
                            opfuncs[cptr[cptr_ptr + 0].op_state](cptr[cptr_ptr + 0]);
                            operator_output(cptr[cptr_ptr + 0], (cptr[cptr_ptr + 0].lastcval + cptr[cptr_ptr + 0].cval) * cptr[cptr_ptr + 0].mfbi / 2, tremval1[i]);

                            // carrier
                            operator_advance(OPL, cptr[cptr_ptr + 9], vibval2[i]);
                            opfuncs[cptr[cptr_ptr + 9].op_state](cptr[cptr_ptr + 9]);
                            operator_output(cptr[cptr_ptr + 9], cptr[cptr_ptr + 0].cval * FIXEDPT, tremval2[i]);

                            chanval = cptr[cptr_ptr + 9].cval;
                            CHANVAL_OUT(0, outbufl, outbufr, i, chanval);

                        }
                    }
                }

                /*#if defined(OPLTYPE_IS_OPL3)
                        if (adlibreg[0x105]&1)
                        {
                            // convert to 16bit samples (stereo)
                            for (i=0;i<endsamples;i++)
                            {
                                clipit16(outbufl[i],sndptr++);
                                clipit16(outbufr[i],sndptr++);
                            }
                        }
                        else
                        {
                            // convert to 16bit samples (mono)
                            for (i=0;i<endsamples;i++)
                            {
                                clipit16(outbufl[i],sndptr++);
                                clipit16(outbufl[i],sndptr++);
                            }
                        }
                #else
                        // convert to 16bit samples
                        for (i=0;i<endsamples;i++)
                            clipit16(outbufl[i],sndptr++);
                #endif*/

            }

        }

        private void ADLIBEMU_set_mute_mask(OPL_DATA chip, UInt32 MuteMask)
        {
            OPL_DATA OPL = (OPL_DATA)chip;

            byte CurChn;

            for (CurChn = 0; CurChn < NUM_CHANNELS + 5; CurChn++)
                OPL.MuteChn[CurChn] = (byte)((MuteMask >> CurChn) & 0x01);

            return;
        }
    }
}
