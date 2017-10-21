using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDSound
{
    public class ymf262 : Instrument
    {
        public override void Reset(byte ChipID)
        {
            device_reset_ymf262(ChipID);
        }

        public override uint Start(byte ChipID, uint clock)
        {
            return (UInt32)device_start_ymf262(ChipID, (Int32)14318180);
        }

        public uint Start(byte ChipID, uint clock, uint FMClockValue, params object[] option)
        {

            return (UInt32)device_start_ymf262(ChipID, (Int32)FMClockValue);
        }

        public override void Stop(byte ChipID)
        {
            device_stop_ymf262(ChipID);
        }

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {
            ymf262_stream_update(ChipID, outputs, samples);   
        }

        public int YMF262_Write(byte ChipID, uint adr, byte data)
        {
            ymf262_w(ChipID, (adr & 0x100) != 0 ? 0x02 : 0x00, (byte)(adr & 0xff));
            ymf262_w(ChipID, (adr & 0x100) != 0 ? 0x03 : 0x01, data);
            return 0;
        }




        //#pragma once


        /*typedef struct _ymf262_interface ymf262_interface;
        struct _ymf262_interface
        {
            //void (*handler)(const device_config *device, int irq);
            void (*handler)(int irq);
        };*/


        /*READ8_DEVICE_HANDLER( ymf262_r );
        WRITE8_DEVICE_HANDLER( ymf262_w );

        READ8_DEVICE_HANDLER ( ymf262_status_r );
        WRITE8_DEVICE_HANDLER( ymf262_register_a_w );
        WRITE8_DEVICE_HANDLER( ymf262_register_b_w );
        WRITE8_DEVICE_HANDLER( ymf262_data_a_w );
        WRITE8_DEVICE_HANDLER( ymf262_data_b_w );


        DEVICE_GET_INFO( ymf262 );
        #define SOUND_YMF262 DEVICE_GET_INFO_NAME( ymf262 )*/

        //public void ymf262_stream_update(byte ChipID, Int32[][] outputs, Int32 samples) { }
        //public Int32 device_start_ymf262(byte ChipID, Int32 clock) { return 0; }
        //public void device_stop_ymf262(byte ChipID) { }
        //public void device_reset_ymf262(byte ChipID) { }
        //public byte ymf262_r(byte ChipID, Int32 offset) { return 0; }
        //public void ymf262_w(byte ChipID, Int32 offset, byte data) { }

        //public byte ymf262_status_r(byte ChipID, Int32 offset) { return 0; }
        //public void ymf262_register_a_w(byte ChipID, Int32 offset, byte data) { }
        //public void ymf262_register_b_w(byte ChipID, Int32 offset, byte data) { }
        //public void ymf262_data_a_w(byte ChipID, Int32 offset, byte data) { }
        //public void ymf262_data_b_w(byte ChipID, Int32 offset, byte data) { }

        //public void ymf262_set_emu_core(byte Emulator) { }
        //public void ymf262_set_mute_mask(byte ChipID, UInt32 MuteMask) { }




        /***************************************************************************

  262intf.c

  MAME interface for YMF262 (OPL3) emulator

***************************************************************************/
        //# include "mamedef.h"
        //#include "attotime.h"
        //#include "sndintrf.h"
        //#include "streams.h"
        //# include "262intf.h"
        //# ifdef ENABLE_ALL_CORES
        //# include "ymf262.h"
        //#endif

        private const bool OPLTYPE_IS_OPL3 = true;
        //# include "adlibemu.h"


        private const byte EC_DBOPL = 0x00;  // DosBox OPL (AdLibEmu)
                                             //# ifdef ENABLE_ALL_CORES
        private const byte EC_MAME = 0x01;   // YMF262 core from MAME
                                             //#endif

        public class ymf262_state
        //struct _ymf262_state
        {
            //sound_stream *	stream;
            //emu_timer *		timer[2];
            //public object chip;
            public OPL_DATA chip;
            //const ymf262_interface *intf;
            //const device_config *device;
        }


        //extern UINT8 CHIP_SAMPLING_MODE;
        //extern INT32 CHIP_SAMPLE_RATE;
        private byte EMU_CORE = 0x00;

        private const Int32 MAX_CHIPS = 0x02;
        private ymf262_state[] YMF262Data = new ymf262_state[MAX_CHIPS] {new ymf262_state(),new ymf262_state() };
        private Int32[][] DUMMYBUF=new Int32[0x02][]{ null, null };

        /*INLINE ymf262_state *get_safe_token(const device_config *device)
        {
            assert(device != NULL);
            assert(device->token != NULL);
            assert(device->type == SOUND);
            assert(sound_get_type(device) == SOUND_YMF262);
            return (ymf262_state *)device->token;
        }*/

        private void IRQHandler_262(ymf262_state param, Int32 irq)
        {
            ymf262_state info = (ymf262_state)param;
            //if (info->intf->handler) (info->intf->handler)(info->device, irq);
        }

        /*static TIMER_CALLBACK( timer_callback_262_0 )
        {
            ymf262_state *info = (ymf262_state *)ptr;
            ymf262_timer_over(info->chip, 0);
        }

        static TIMER_CALLBACK( timer_callback_262_1 )
        {
            ymf262_state *info = (ymf262_state *)ptr;
            ymf262_timer_over(info->chip, 1);
        }*/

        //static void timer_handler_262(void *param,int timer, attotime period)
        private void timer_handler_262(ymf262_state param, Int32 timer, Int32 period)
        {
            ymf262_state info = (ymf262_state)param;
            if (period == 0)
            {   /* Reset FM Timer */
                //timer_enable(info->timer[timer], 0);
            }
            else
            {   /* Start FM Timer */
                //timer_adjust_oneshot(info->timer[timer], period, 0);
            }
        }

        //static STREAM_UPDATE( ymf262_stream_update )
        public void ymf262_stream_update(byte ChipID, Int32[][] outputs, Int32 samples)
        {
            //ymf262_state *info = (ymf262_state *)param;
            ymf262_state info = YMF262Data[ChipID];
            switch (EMU_CORE)
            {
                //# ifdef ENABLE_ALL_CORES
                case EC_MAME:
                    //ymf262_update_one(info.chip, outputs, samples);
                    break;
                //#endif
                case EC_DBOPL:
                    adlib_OPL3_getsample(info.chip, outputs, samples);
                    break;
            }
        }

        private void _stream_update(ymf262_state param/*, int interval*/)
        {
            ymf262_state info = (ymf262_state)param;
            //stream_update(info->stream);

            switch (EMU_CORE)
            {
                //# ifdef ENABLE_ALL_CORES
                case EC_MAME:
                    //ymf262_update_one(info.chip, DUMMYBUF, 0);
                    break;
                //#endif
                case EC_DBOPL:
                    adlib_OPL3_getsample(info.chip, DUMMYBUF, 0);
                    break;
            }
        }


        //static DEVICE_START( ymf262 )
        public Int32 device_start_ymf262(byte ChipID, Int32 clock)
        {
            //static const ymf262_interface dummy = { 0 };
            //ymf262_state *info = get_safe_token(device);
            ymf262_state info;
            Int32 rate;

            if (ChipID >= MAX_CHIPS)
                return 0;

            info = YMF262Data[ChipID];
            rate = clock / 288;
            if ((CHIP_SAMPLING_MODE == 0x01 && rate < CHIP_SAMPLE_RATE) ||
                CHIP_SAMPLING_MODE == 0x02)
                rate = CHIP_SAMPLE_RATE;

            //info->intf = device->static_config ? (const ymf262_interface *)device->static_config : &dummy;
            //info->intf = &dummy;
            //info->device = device;

            /* stream system initialize */
            switch (EMU_CORE)
            {
                //# ifdef ENABLE_ALL_CORES
                case EC_MAME:
                    //info.chip = ymf262_init(clock, rate);
                    //assert_always(info->chip != NULL, "Error creating YMF262 chip");

                    //info->stream = stream_create(device,0,4,rate,info,ymf262_stream_update);

                    /* YMF262 setup */
                    ymf262_set_timer_handler(info.chip, timer_handler_262, info);
                    ymf262_set_irq_handler(info.chip, IRQHandler_262, info);
                    ymf262_set_update_handler(info.chip, _stream_update, info);

                    //info->timer[0] = timer_alloc(device->machine, timer_callback_262_0, info);
                    //info->timer[1] = timer_alloc(device->machine, timer_callback_262_1, info);
                    break;
                //#endif
                case EC_DBOPL:
                    info.chip = adlib_OPL3_init((UInt32)clock, (UInt32)rate, _stream_update, info);
                    break;
            }

            return rate;
        }

        //static DEVICE_STOP( ymf262 )
        public void device_stop_ymf262(byte ChipID)
        {
            //ymf262_state *info = get_safe_token(device);
            ymf262_state info = YMF262Data[ChipID];
            switch (EMU_CORE)
            {
                //# ifdef ENABLE_ALL_CORES
                case EC_MAME:
                    ymf262_shutdown(info.chip);
                    break;
                //#endif
                case EC_DBOPL:
                    adlib_OPL3_stop(info.chip);
                    break;
            }
        }

        /* reset */
        //static DEVICE_RESET( ymf262 )
        public void device_reset_ymf262(byte ChipID)
        {
            //ymf262_state *info = get_safe_token(device);
            ymf262_state info = YMF262Data[ChipID];
            switch (EMU_CORE)
            {
                //# ifdef ENABLE_ALL_CORES
                case EC_MAME:
                    ymf262_reset_chip(info.chip);
                    break;
                //#endif
                case EC_DBOPL:
                    adlib_OPL3_reset(info.chip);
                    break;
            }
        }


        //READ8_DEVICE_HANDLER( ymf262_r )
        public byte ymf262_r(byte ChipID, Int32 offset)
        {
            //ymf262_state *info = get_safe_token(device);
            ymf262_state info = YMF262Data[ChipID];
            switch (EMU_CORE)
            {
                //# ifdef ENABLE_ALL_CORES
                case EC_MAME:
                    return ymf262_read(info.chip, offset & 3);
                //#endif
                case EC_DBOPL:
                    return (byte)adlib_OPL3_reg_read(info.chip, (UInt32)(offset & 0x03));
                default:
                    return 0x00;
            }
        }

        //WRITE8_DEVICE_HANDLER( ymf262_w )
        public void ymf262_w(byte ChipID, Int32 offset, byte data)
        {
            //ymf262_state *info = get_safe_token(device);
            ymf262_state info = YMF262Data[ChipID];

            switch (EMU_CORE)
            {
                //# ifdef ENABLE_ALL_CORES
                case EC_MAME:
                    ymf262_write(info.chip, offset & 3, data);
                    break;
                //#endif
                case EC_DBOPL:
                    adlib_OPL3_writeIO(info.chip, (UInt32)(offset & 3), data);
                    break;
            }
        }

        //READ8_DEVICE_HANDLER ( ymf262_status_r )
        public byte ymf262_status_r(byte ChipID, Int32 offset)
        {
            return ymf262_r(ChipID, 0);
        }

        //WRITE8_DEVICE_HANDLER( ymf262_register_a_w )
        public void ymf262_register_a_w(byte ChipID, Int32 offset, byte data)
        {
            ymf262_w(ChipID, 0, data);
        }
        //WRITE8_DEVICE_HANDLER( ymf262_register_b_w )
        public void ymf262_register_b_w(byte ChipID, Int32 offset, byte data)
        {
            ymf262_w(ChipID, 2, data);
        }
        //WRITE8_DEVICE_HANDLER( ymf262_data_a_w )
        public void ymf262_data_a_w(byte ChipID, Int32 offset, byte data)
        {
            ymf262_w(ChipID, 1, data);
        }
        //WRITE8_DEVICE_HANDLER( ymf262_data_b_w )
        public void ymf262_data_b_w(byte ChipID, Int32 offset, byte data)
        {
            ymf262_w(ChipID, 3, data);
        }


        public void ymf262_set_emu_core(byte Emulator)
        {
            //# ifdef ENABLE_ALL_CORES
            EMU_CORE = (byte)((Emulator < 0x02) ? Emulator : 0x00);
            //#else
            //            EMU_CORE = EC_DBOPL;
            //#endif

            return;
        }

        public void ymf262_set_mute_mask(byte ChipID, UInt32 MuteMask)
        {
            ymf262_state info = YMF262Data[ChipID];
            switch (EMU_CORE)
            {
                //# ifdef ENABLE_ALL_CORES
                case EC_MAME:
                    ymf262_set_mutemask(info.chip, MuteMask);
                    break;
                //#endif
                case EC_DBOPL:
                    adlib_OPL3_set_mute_mask(info.chip, MuteMask);
                    break;
            }

            return;
        }


        /**************************************************************************
         * Generic get_info
         **************************************************************************/

        /*DEVICE_GET_INFO( ymf262 )
        {
            switch (state)
            {
                // --- the following bits of info are returned as 64-bit signed integers ---
                case DEVINFO_INT_TOKEN_BYTES:					info->i = sizeof(ymf262_state);				break;

                // --- the following bits of info are returned as pointers to data or functions ---
                case DEVINFO_FCT_START:							info->start = DEVICE_START_NAME( ymf262 );				break;
                case DEVINFO_FCT_STOP:							info->stop = DEVICE_STOP_NAME( ymf262 );				break;
                case DEVINFO_FCT_RESET:							info->reset = DEVICE_RESET_NAME( ymf262 );				break;

                // --- the following bits of info are returned as NULL-terminated strings ---
                case DEVINFO_STR_NAME:							strcpy(info->s, "YMF262");							break;
                case DEVINFO_STR_FAMILY:					strcpy(info->s, "Yamaha FM");						break;
                case DEVINFO_STR_VERSION:					strcpy(info->s, "1.0");								break;
                case DEVINFO_STR_SOURCE_FILE:						strcpy(info->s, __FILE__);							break;
                case DEVINFO_STR_CREDITS:					strcpy(info->s, "Copyright Nicola Salmoria and the MAME Team"); break;
            }
        }*/





        //#pragma once

        //#include "attotime.h"

        /* select number of output bits: 8 or 16 */
        //private Int32 OPL3_SAMPLE_BITS = 16;

        /* compiler dependence */
        //#ifndef __OSDCOMM_H__
        //#define __OSDCOMM_H__
        /*typedef unsigned char	UINT8;   // unsigned  8bit
        typedef unsigned short	UINT16;  // unsigned 16bit
        typedef unsigned int	UINT32;  // unsigned 32bit
        typedef signed char		INT8;    // signed  8bit
        typedef signed short	INT16;   // signed 16bit
        typedef signed int		INT32;   // signed 32bit*/
        //#endif

        //typedef stream_sample_t OPL3SAMPLE;
        /*
        #if (OPL3_SAMPLE_BITS==16)
        typedef INT16 OPL3SAMPLE;
        #endif
        #if (OPL3_SAMPLE_BITS==8)
        typedef INT8 OPL3SAMPLE;
        #endif
        */

        //typedef void (*OPL3_TIMERHANDLER)(void *param,int timer,attotime period);
        //typedef void (* OPL3_TIMERHANDLER) (void* param, int timer, int period);
        public delegate void OPL3_TIMERHANDLER(ymf262_state param, Int32 timer, Int32 period);
        //typedef void (* OPL3_IRQHANDLER) (void* param, int irq);
        public delegate void OPL3_IRQHANDLER(ymf262_state param, Int32 irq);
        //typedef void (* OPL3_UPDATEHANDLER) (void* param/*,int min_interval_us*/);
        public delegate void OPL3_UPDATEHANDLER(ymf262_state param);


        //public void ymf262_init(Int32 clock, Int32 rate) { }
        public void ymf262_shutdown(object chip) { }
        public void ymf262_reset_chip(object chip) { }
        public Int32 ymf262_write(object chip, Int32 a, Int32 v) { return 0; }
        public byte ymf262_read(object chip, Int32 a) { return 0; }
        public Int32 ymf262_timer_over(object chip, Int32 c) { return 0; }
        //public void ymf262_update_one(object chip, Int32[][] buffers, Int32 length) { }

        public void ymf262_set_timer_handler(object chip, OPL3_TIMERHANDLER TimerHandler, object param) { }
        public void ymf262_set_irq_handler(object chip, OPL3_IRQHANDLER IRQHandler, object param) { }
        public void ymf262_set_update_handler(object chip, OPL3_UPDATEHANDLER UpdateHandler, object param) { }

        //public void ymf262_set_emu_core(byte Emulator) { }
        public void ymf262_set_mutemask(object chip, UInt32 MuteMask) { }





        /*
**
** File: ymf262.c - software implementation of YMF262
**                  FM sound generator type OPL3
**
** Copyright Jarek Burczynski
**
** Version 0.2
**

Revision History:

03-03-2003: initial release
 - thanks to Olivier Galibert and Chris Hardy for YMF262 and YAC512 chips
 - thanks to Stiletto for the datasheets

   Features as listed in 4MF262A6 data sheet:
    1. Registers are compatible with YM3812 (OPL2) FM sound source.
    2. Up to six sounds can be used as four-operator melody sounds for variety.
    3. 18 simultaneous melody sounds, or 15 melody sounds with 5 rhythm sounds (with two operators).
    4. 6 four-operator melody sounds and 6 two-operator melody sounds, or 6 four-operator melody
       sounds, 3 two-operator melody sounds and 5 rhythm sounds (with four operators).
    5. 8 selectable waveforms.
    6. 4-channel sound output.
    7. YMF262 compabile DAC (YAC512) is available.
    8. LFO for vibrato and tremolo effedts.
    9. 2 programable timers.
   10. Shorter register access time compared with YM3812.
   11. 5V single supply silicon gate CMOS process.
   12. 24 Pin SOP Package (YMF262-M), 48 Pin SQFP Package (YMF262-S).


differences between OPL2 and OPL3 not documented in Yamaha datahasheets:
- sinus table is a little different: the negative part is off by one...

- in order to enable selection of four different waveforms on OPL2
  one must set bit 5 in register 0x01(test).
  on OPL3 this bit is ignored and 4-waveform select works *always*.
  (Don't confuse this with OPL3's 8-waveform select.)

- Envelope Generator: all 15 x rates take zero time on OPL3
  (on OPL2 15 0 and 15 1 rates take some time while 15 2 and 15 3 rates
  take zero time)

- channel calculations: output of operator 1 is in perfect sync with
  output of operator 2 on OPL3; on OPL and OPL2 output of operator 1
  is always delayed by one sample compared to output of operator 2


differences between OPL2 and OPL3 shown in datasheets:
- YMF262 does not support CSM mode


*/

        //# include <math.h>
        //# include "mamedef.h"
        //# include <stdlib.h>
        //# include <string.h>	// for memset
        //# include <stddef.h>	// for NULL
        //#include "sndintrf.h"
        //# include "ymf262.h"


        /* output final shift */
        //#if (OPL3_SAMPLE_BITS ==16)
        private Int32 FINAL_SH = (0);
        //private Int32 MAXOUT = (+32767);
        //private Int32 MINOUT = (-32768);
        //#else
        //#define FINAL_SH	(8)
        //#define MAXOUT		(+127)
        //#define MINOUT		(-128)
        //#endif


        private const Int32 FREQ_SH = 16;  /* 16.16 fixed point (frequency calculations) */
        private Int32 EG_SH = 16;  /* 16.16 fixed point (EG timing)              */
        private Int32 LFO_SH = 24;  /*  8.24 fixed point (LFO calculations)       */
        //private Int32 TIMER_SH = 16;  /* 16.16 fixed point (timers calculations)    */

        private Int32 FREQ_MASK = ((1 << FREQ_SH) - 1);

        /* envelope output entries */
        private static Int32 ENV_BITS = 10;
        private static Int32 ENV_LEN = (1 << ENV_BITS);
        private static double ENV_STEP = (128.0 / ENV_LEN);

        private Int32 MAX_ATT_INDEX = ((1 << (ENV_BITS - 1)) - 1); /*511*/
        private Int32 MIN_ATT_INDEX = (0);

        /* sinwave entries */
        private static Int32 SIN_BITS = 10;
        private static Int32 SIN_LEN = (1 << SIN_BITS);
        private static Int32 SIN_MASK = (SIN_LEN - 1);

        private const Int32 TL_RES_LEN = (256);   /* 8 bits addressing (real chip) */

        /* register number to channel number , slot offset */
        private Int32 SLOT1 = 0;
        private Int32 SLOT2 = 1;

        /* Envelope Generator phases */

        private const byte EG_ATT = 4;
        private const byte EG_DEC = 3;
        private const byte EG_SUS = 2;
        private const byte EG_REL = 1;
        private const byte EG_OFF = 0;

        /* save output as raw 16-bit sample */

        /*#define SAVE_SAMPLE*/

        //# ifdef SAVE_SAMPLE
        //        static FILE* sample[1];
        //	#if 1	/*save to MONO file */
        //		#define SAVE_ALL_CHANNELS \
        //		{	signed int pom = a; \
        //			fputc((unsigned short)pom&0xff,sample[0]); \
        //			fputc(((unsigned short)pom>>8)&0xff,sample[0]); \
        //		}
        //	#else	/*save to STEREO file */
        //		#define SAVE_ALL_CHANNELS \
        //		{	signed int pom = a; \

        //            fputc((unsigned short)pom&0xff,sample[0]); \

        //            fputc(((unsigned short)pom>>8)&0xff,sample[0]); \
        //			pom = b; \

        //            fputc((unsigned short)pom&0xff,sample[0]); \

        //            fputc(((unsigned short)pom>>8)&0xff,sample[0]); \
        //		}
        //#endif
        //#endif

        //#define LOG_CYM_FILE 0
        //static FILE * cymfile = NULL;

        private Int32 OPL3_TYPE_YMF262 = (0);   /* 36 operators, 8 waveforms */


        public class OPL3_SLOT
        {

            public UInt32 ar;          /* attack rate: AR<<2           */
            public UInt32 dr;          /* decay rate:  DR<<2           */
            public UInt32 rr;          /* release rate:RR<<2           */
            public byte KSR;      /* key scale rate               */
            public byte ksl;      /* keyscale level               */
            public byte ksr;      /* key scale rate: kcode>>KSR   */
            public byte mul;      /* multiple: mul_tab[ML]        */

            /* Phase Generator */
            public UInt32 Cnt;     /* frequency counter            */
            public UInt32 Incr;        /* frequency counter step       */
            public byte FB;           /* feedback shift value         */
            public Int32 connect; /* slot output pointer          */
            public Int32[] op1_out = new Int32[2];   /* slot1 output for feedback    */
            public byte CON;      /* connection (algorithm) type  */

            /* Envelope Generator */
            public byte eg_type;  /* percussive/non-percussive mode */
            public byte state;        /* phase type                   */
            public UInt32 TL;          /* total level: TL << 2         */
            public Int32 TLL;      /* adjusted now TL              */
            public Int32 volume;       /* envelope counter             */
            public UInt32 sl;          /* sustain level: sl_tab[SL]    */

            public UInt32 eg_m_ar; /* (attack state)               */
            public byte eg_sh_ar; /* (attack state)               */
            public byte eg_sel_ar;    /* (attack state)               */
            public UInt32 eg_m_dr; /* (decay state)                */
            public byte eg_sh_dr; /* (decay state)                */
            public byte eg_sel_dr;    /* (decay state)                */
            public UInt32 eg_m_rr; /* (release state)              */
            public byte eg_sh_rr; /* (release state)              */
            public byte eg_sel_rr;    /* (release state)              */

            public UInt32 key;     /* 0 = KEY OFF, >0 = KEY ON     */

            /* LFO */
            public UInt32 AMmask;      /* LFO Amplitude Modulation enable mask */
            public byte vib;      /* LFO Phase Modulation enable flag (active high)*/

            /* waveform select */
            public byte waveform_number;
            public UInt32 wavetable;

            //unsigned char reserved[128-84];//speedup: pump up the struct size to power of 2
            public byte[] reserved = new byte[128 - 100];//speedup: pump up the struct size to power of 2

        }

        public class OPL3_CH
        {

            public OPL3_SLOT[] SLOT = new OPL3_SLOT[2] { new OPL3_SLOT(), new OPL3_SLOT() };

            public UInt32 block_fnum;  /* block+fnum                   */
            public UInt32 fc;          /* Freq. Increment base         */
            public UInt32 ksl_base;    /* KeyScaleLevel Base step      */
            public byte kcode;        /* key code (for key scaling)   */

            /*
               there are 12 2-operator channels which can be combined in pairs
               to form six 4-operator channel, they are:
                0 and 3,
                1 and 4,
                2 and 5,
                9 and 12,
                10 and 13,
                11 and 14
            */
            public byte extended; /* set to 1 if this channel forms up a 4op channel with another channel(only used by first of pair of channels, ie 0,1,2 and 9,10,11) */
            public byte Muted;

            public byte[] reserved = new byte[512 - 272];//speedup:pump up the struct size to power of 2

        }

        /* OPL3 state */
        public class OPL3
        {

            public OPL3_CH[] P_CH = new OPL3_CH[18] {
                new OPL3_CH(), new OPL3_CH(), new OPL3_CH(), new OPL3_CH(),
                new OPL3_CH(), new OPL3_CH(), new OPL3_CH(), new OPL3_CH(),
                new OPL3_CH(), new OPL3_CH(), new OPL3_CH(), new OPL3_CH(),
                new OPL3_CH(), new OPL3_CH(), new OPL3_CH(), new OPL3_CH(),
                new OPL3_CH(), new OPL3_CH()
            };               /* OPL3 chips have 18 channels  */

            public UInt32[] pan = new UInt32[18 * 4];             /* channels output masks (0xffffffff = enable); 4 masks per one channel */
            public UInt32[] pan_ctrl_value = new UInt32[18];      /* output control values 1 per one channel (1 value contains 4 masks) */
            public byte[] MuteSpc = new byte[5];               /* for the 5 Rhythm Channels */

            public Int32[] chanout = new Int32[18];         /* 18 channels */
            public Int32 phase_modulation;    /* phase modulation input (SLOT 2) */
            public Int32 phase_modulation2;   /* phase modulation input (SLOT 3 in 4 operator channels) */

            public UInt32 eg_cnt;                  /* global envelope generator counter    */
            public UInt32 eg_timer;                /* global envelope generator counter works at frequency = chipclock/288 (288=8*36) */
            public UInt32 eg_timer_add;            /* step of eg_timer                     */
            public UInt32 eg_timer_overflow;       /* envelope generator timer overlfows every 1 sample (on real chip) */

            public UInt32[] fn_tab = new UInt32[1024];            /* fnumber->increment counter   */

            /* LFO */
            public UInt32 LFO_AM;
            public Int32 LFO_PM;
            public byte lfo_am_depth;
            public byte lfo_pm_depth_range;
            public UInt32 lfo_am_cnt;
            public UInt32 lfo_am_inc;
            public UInt32 lfo_pm_cnt;
            public UInt32 lfo_pm_inc;

            public UInt32 noise_rng;               /* 23 bit noise shift register  */
            public UInt32 noise_p;             /* current noise 'phase'        */
            public UInt32 noise_f;             /* current noise period         */

            public byte OPL3_mode;                /* OPL3 extension enable flag   */

            public byte rhythm;                   /* Rhythm mode                  */

            public Int32[] T = new Int32[2];                   /* timer counters               */
            public byte[] st = new byte[2];                    /* timer enable                 */

            public UInt32 address;             /* address register             */
            public byte status;                   /* status flag                  */
            public byte statusmask;               /* status mask                  */

            public byte nts;                  /* NTS (note select)            */

            /* external event callback handlers */
            public OPL3_TIMERHANDLER timer_handler;/* TIMER handler                */
            public object TimerParam;                   /* TIMER parameter              */
            public OPL3_IRQHANDLER IRQHandler; /* IRQ handler                  */
            public ymf262_state IRQParam;                 /* IRQ parameter                */
            public OPL3_UPDATEHANDLER UpdateHandler;/* stream update handler       */
            public ymf262_state UpdateParam;              /* stream update parameter      */

            public byte type;                     /* chip type                    */
            public Int32 clock;                      /* master clock  (Hz)           */
            public Int32 rate;                       /* sampling rate (Hz)           */
            public double freqbase;             /* frequency base               */
                                                //attotime TimerBase;			/* Timer base time (==sampling time)*/
        }



        /* mapping of register number (offset) to slot number used by the emulator */
        private Int32[] slot_array = new Int32[32]
        {
             0, 2, 4, 1, 3, 5,-1,-1,
            6, 8,10, 7, 9,11,-1,-1,
            12,14,16,13,15,17,-1,-1,
            -1,-1,-1,-1,-1,-1,-1,-1
        };

        /* key scale level */
        /* table is 3dB/octave , DV converts this into 6dB/octave */
        /* 0.1875 is bit 0 weight of the envelope counter (volume) expressed in the 'decibel' scale */
        private static double DV = (0.1875 / 2.0);
        private static UInt32[] ksl_tab = new UInt32[8 * 16]
        {
	        /* OCT 0 */
	        (UInt32)( 0.000/DV),(UInt32)( 0.000/DV),(UInt32)( 0.000/DV),(UInt32)( 0.000/DV),
            (UInt32)( 0.000/DV),(UInt32)( 0.000/DV),(UInt32)( 0.000/DV),(UInt32)( 0.000/DV),
            (UInt32)( 0.000/DV),(UInt32)( 0.000/DV),(UInt32)( 0.000/DV),(UInt32)( 0.000/DV),
            (UInt32)( 0.000/DV),(UInt32)( 0.000/DV),(UInt32)( 0.000/DV),(UInt32)( 0.000/DV),
	        /* OCT 1 */
            (UInt32)( 0.000/DV),(UInt32)( 0.000/DV),(UInt32)( 0.000/DV),(UInt32)( 0.000/DV),
            (UInt32)( 0.000/DV),(UInt32)( 0.000/DV),(UInt32)( 0.000/DV),(UInt32)( 0.000/DV),
            (UInt32)( 0.000/DV),(UInt32)( 0.750/DV),(UInt32)( 1.125/DV),(UInt32)( 1.500/DV),
            (UInt32)( 1.875/DV),(UInt32)( 2.250/DV),(UInt32)( 2.625/DV),(UInt32)( 3.000/DV),
            /* OCT 2 */
            (UInt32)( 0.000/DV),(UInt32)( 0.000/DV),(UInt32)( 0.000/DV),(UInt32)( 0.000/DV),
            (UInt32)( 0.000/DV),(UInt32)( 1.125/DV),(UInt32)( 1.875/DV),(UInt32)( 2.625/DV),
            (UInt32)( 3.000/DV),(UInt32)( 3.750/DV),(UInt32)( 4.125/DV),(UInt32)( 4.500/DV),
            (UInt32)( 4.875/DV),(UInt32)( 5.250/DV),(UInt32)( 5.625/DV),(UInt32)( 6.000/DV),
            /* OCT 3 */
            (UInt32)( 0.000/DV),(UInt32)( 0.000/DV),(UInt32)( 0.000/DV),(UInt32)( 1.875/DV),
            (UInt32)( 3.000/DV),(UInt32)( 4.125/DV),(UInt32)( 4.875/DV),(UInt32)( 5.625/DV),
            (UInt32)( 6.000/DV),(UInt32)( 6.750/DV),(UInt32)( 7.125/DV),(UInt32)( 7.500/DV),
            (UInt32)( 7.875/DV),(UInt32)( 8.250/DV),(UInt32)( 8.625/DV),(UInt32)( 9.000/DV),
            /* OCT 4 */
            (UInt32)( 0.000/DV),(UInt32)( 0.000/DV),(UInt32)( 3.000/DV),(UInt32)( 4.875/DV),
            (UInt32)( 6.000/DV),(UInt32)( 7.125/DV),(UInt32)( 7.875/DV),(UInt32)( 8.625/DV),
            (UInt32)( 9.000/DV),(UInt32)( 9.750/DV),(UInt32)(10.125/DV),(UInt32)(10.500/DV),
            (UInt32)(10.875/DV),(UInt32)(11.250/DV),(UInt32)(11.625/DV),(UInt32)(12.000/DV),
            /* OCT 5 */
            (UInt32)( 0.000/DV),(UInt32)( 3.000/DV),(UInt32)( 6.000/DV),(UInt32)( 7.875/DV),
            (UInt32)( 9.000/DV),(UInt32)(10.125/DV),(UInt32)(10.875/DV),(UInt32)(11.625/DV),
            (UInt32)(12.000/DV),(UInt32)(12.750/DV),(UInt32)(13.125/DV),(UInt32)(13.500/DV),
            (UInt32)(13.875/DV),(UInt32)(14.250/DV),(UInt32)(14.625/DV),(UInt32)(15.000/DV),
            /* OCT 6 */
            (UInt32)( 0.000/DV),(UInt32)( 6.000/DV),(UInt32)( 9.000/DV),(UInt32)(10.875/DV),
            (UInt32)(12.000/DV),(UInt32)(13.125/DV),(UInt32)(13.875/DV),(UInt32)(14.625/DV),
            (UInt32)(15.000/DV),(UInt32)(15.750/DV),(UInt32)(16.125/DV),(UInt32)(16.500/DV),
            (UInt32)(16.875/DV),(UInt32)(17.250/DV),(UInt32)(17.625/DV),(UInt32)(18.000/DV),
            /* OCT 7 */
            (UInt32)( 0.000/DV),(UInt32)( 9.000/DV),(UInt32)(12.000/DV),(UInt32)(13.875/DV),
            (UInt32)(15.000/DV),(UInt32)(16.125/DV),(UInt32)(16.875/DV),(UInt32)(17.625/DV),
            (UInt32)(18.000/DV),(UInt32)(18.750/DV),(UInt32)(19.125/DV),(UInt32)(19.500/DV),
            (UInt32)(19.875/DV),(UInt32)(20.250/DV),(UInt32)(20.625/DV),(UInt32)(21.000/DV)
        };
        //#undef DV

        /* 0 / 3.0 / 1.5 / 6.0 dB/OCT */
        private UInt32[] ksl_shift = new UInt32[4] { 31, 1, 2, 0 };


        /* sustain level table (3dB per step) */
        /* 0 - 15: 0, 3, 6, 9,12,15,18,21,24,27,30,33,36,39,42,93 (dB)*/
        private static UInt32 SC(double db) { return (UInt32)(db * (2.0 / ENV_STEP)); }
        private UInt32[] sl_tab = new UInt32[16]{
             SC( 0),SC( 1),SC( 2),SC(3 ),SC(4 ),SC(5 ),SC(6 ),SC( 7),
             SC( 8),SC( 9),SC(10),SC(11),SC(12),SC(13),SC(14),SC(31)
        };
        //#undef SC


        private const Int32 RATE_STEPS = (8);
        private static byte[] eg_inc = new byte[15 * RATE_STEPS]{
            /*cycle:0 1  2 3  4 5  6 7*/
            /* 0 */ 0,1, 0,1, 0,1, 0,1, /* rates 00..12 0 (increment by 0 or 1) */
            /* 1 */ 0,1, 0,1, 1,1, 0,1, /* rates 00..12 1 */
            /* 2 */ 0,1, 1,1, 0,1, 1,1, /* rates 00..12 2 */
            /* 3 */ 0,1, 1,1, 1,1, 1,1, /* rates 00..12 3 */
            
            /* 4 */ 1,1, 1,1, 1,1, 1,1, /* rate 13 0 (increment by 1) */
            /* 5 */ 1,1, 1,2, 1,1, 1,2, /* rate 13 1 */
            /* 6 */ 1,2, 1,2, 1,2, 1,2, /* rate 13 2 */
            /* 7 */ 1,2, 2,2, 1,2, 2,2, /* rate 13 3 */
            
            /* 8 */ 2,2, 2,2, 2,2, 2,2, /* rate 14 0 (increment by 2) */
            /* 9 */ 2,2, 2,4, 2,2, 2,4, /* rate 14 1 */
            /*10 */ 2,4, 2,4, 2,4, 2,4, /* rate 14 2 */
            /*11 */ 2,4, 4,4, 2,4, 4,4, /* rate 14 3 */
            
            /*12 */ 4,4, 4,4, 4,4, 4,4, /* rates 15 0, 15 1, 15 2, 15 3 for decay */
            /*13 */ 8,8, 8,8, 8,8, 8,8, /* rates 15 0, 15 1, 15 2, 15 3 for attack (zero time) */
            /*14 */ 0,0, 0,0, 0,0, 0,0, /* infinity rates for attack and decay(s) */
        };


        private static byte O(Int32 a) { return (byte)((byte)a * RATE_STEPS); }

        /* note that there is no O(13) in this table - it's directly in the code */
        private static byte[] eg_rate_select = new byte[16 + 64 + 16]{	/* Envelope Generator rates (16 + 64 rates + 16 RKS) */
            /* 16 infinite time rates */
            O(14),O(14),O(14),O(14),O(14),O(14),O(14),O(14),
            O(14),O(14),O(14),O(14),O(14),O(14),O(14),O(14),
            /* rates 00-12 */
            O( 0),O( 1),O( 2),O( 3),
            O( 0),O( 1),O( 2),O( 3),
            O( 0),O( 1),O( 2),O( 3),
            O( 0),O( 1),O( 2),O( 3),
            O( 0),O( 1),O( 2),O( 3),
            O( 0),O( 1),O( 2),O( 3),
            O( 0),O( 1),O( 2),O( 3),
            O( 0),O( 1),O( 2),O( 3),
            O( 0),O( 1),O( 2),O( 3),
            O( 0),O( 1),O( 2),O( 3),
            O( 0),O( 1),O( 2),O( 3),
            O( 0),O( 1),O( 2),O( 3),
            O( 0),O( 1),O( 2),O( 3),
            /* rate 13 */
            O( 4),O( 5),O( 6),O( 7),
            /* rate 14 */
            O( 8),O( 9),O(10),O(11),
            /* rate 15 */
            O(12),O(12),O(12),O(12),
            /* 16 dummy rates (same as 15 3) */
            O(12),O(12),O(12),O(12),O(12),O(12),O(12),O(12),
            O(12),O(12),O(12),O(12),O(12),O(12),O(12),O(12),
        };
        //#undef O

        /*rate  0,    1,    2,    3,   4,   5,   6,  7,  8,  9,  10, 11, 12, 13, 14, 15 */
        /*shift 12,   11,   10,   9,   8,   7,   6,  5,  4,  3,  2,  1,  0,  0,  0,  0  */
        /*mask  4095, 2047, 1023, 511, 255, 127, 63, 31, 15, 7,  3,  1,  0,  0,  0,  0  */

        private static byte O1(Int32 a) { return (byte)((byte)a * 1); }
        private static byte[] eg_rate_shift = new byte[16 + 64 + 16]{	/* Envelope Generator counter shifts (16 + 64 rates + 16 RKS) */
            /* 16 infinite time rates */
            O1(0),O1(0),O1(0),O1(0),O1(0),O1(0),O1(0),O1(0),
            O1(0),O1(0),O1(0),O1(0),O1(0),O1(0),O1(0),O1(0),
            /* rates 00-12 */
            O1(12),O1(12),O1(12),O1(12),
            O1(11),O1(11),O1(11),O1(11),
            O1(10),O1(10),O1(10),O1(10),
            O1( 9),O1( 9),O1( 9),O1( 9),
            O1( 8),O1( 8),O1( 8),O1( 8),
            O1( 7),O1( 7),O1( 7),O1( 7),
            O1( 6),O1( 6),O1( 6),O1( 6),
            O1( 5),O1( 5),O1( 5),O1( 5),
            O1( 4),O1( 4),O1( 4),O1( 4),
            O1( 3),O1( 3),O1( 3),O1( 3),
            O1( 2),O1( 2),O1( 2),O1( 2),
            O1( 1),O1( 1),O1( 1),O1( 1),
            O1( 0),O1( 0),O1( 0),O1( 0),
            /* rate 13 */
            O1( 0),O1( 0),O1( 0),O1( 0),
            /* rate 14 */
            O1( 0),O1( 0),O1( 0),O1( 0),
            /* rate 15 */
            O1( 0),O1( 0),O1( 0),O1( 0),
            /* 16 dummy rates (same as 15 3) */
            O1( 0),O1( 0),O1( 0),O1( 0),O1( 0),O1( 0),O1( 0),O1( 0),
            O1( 0),O1( 0),O1( 0),O1( 0),O1( 0),O1( 0),O1( 0),O1( 0),
        };
        //#undef O


        /* multiple table */
        private static Int32 ML = 2;
        private static byte[] mul_tab = new byte[16]{
            /* 1/2, 1, 2, 3, 4, 5, 6, 7, 8, 9,10,10,12,12,15,15 */
            (byte)(0.50*ML),(byte)( 1.00*ML),(byte)( 2.00*ML),(byte)( 3.00*ML),(byte)( 4.00*ML),(byte)( 5.00*ML),(byte)( 6.00*ML),(byte)( 7.00*ML),
            (byte)(8.00*ML),(byte)( 9.00*ML),(byte)(10.00*ML),(byte)(10.00*ML),(byte)(12.00*ML),(byte)(12.00*ML),(byte)(15.00*ML),(byte)(15.00*ML)
        };
        //#undef ML

        /*  TL_TAB_LEN is calculated as:

        *   (12+1)=13 - sinus amplitude bits     (Y axis)
        *   additional 1: to compensate for calculations of negative part of waveform
        *   (if we don't add it then the greatest possible _negative_ value would be -2
        *   and we really need -1 for waveform #7)
        *   2  - sinus sign bit           (Y axis)
        *   TL_RES_LEN - sinus resolution (X axis)
        */
        private static Int32 TL_TAB_LEN = (13 * 2 * TL_RES_LEN);
        private static Int32[] tl_tab = new Int32[TL_TAB_LEN];

        private static Int32 ENV_QUIET = (TL_TAB_LEN >> 4);

        /* sin waveform table in 'decibel' scale */
        /* there are eight waveforms on OPL3 chips */
        private static UInt32[] sin_tab = new UInt32[SIN_LEN * 8];

        /* LFO Amplitude Modulation table (verified on real YM3812)
           27 output levels (triangle waveform); 1 level takes one of: 192, 256 or 448 samples

           Length: 210 elements.

            Each of the elements has to be repeated
            exactly 64 times (on 64 consecutive samples).
            The whole table takes: 64 * 210 = 13440 samples.

            When AM = 1 data is used directly
            When AM = 0 data is divided by 4 before being used (losing precision is important)
        */

        private const Int32 LFO_AM_TAB_ELEMENTS = 210;

        private static byte[] lfo_am_table = new byte[LFO_AM_TAB_ELEMENTS]{
            0,0,0,0,0,0,0,
            1,1,1,1,
            2,2,2,2,
            3,3,3,3,
            4,4,4,4,
            5,5,5,5,
            6,6,6,6,
            7,7,7,7,
            8,8,8,8,
            9,9,9,9,
            10,10,10,10,
            11,11,11,11,
            12,12,12,12,
            13,13,13,13,
            14,14,14,14,
            15,15,15,15,
            16,16,16,16,
            17,17,17,17,
            18,18,18,18,
            19,19,19,19,
            20,20,20,20,
            21,21,21,21,
            22,22,22,22,
            23,23,23,23,
            24,24,24,24,
            25,25,25,25,
            26,26,26,
            25,25,25,25,
            24,24,24,24,
            23,23,23,23,
            22,22,22,22,
            21,21,21,21,
            20,20,20,20,
            19,19,19,19,
            18,18,18,18,
            17,17,17,17,
            16,16,16,16,
            15,15,15,15,
            14,14,14,14,
            13,13,13,13,
            12,12,12,12,
            11,11,11,11,
            10,10,10,10,
            9,9,9,9,
            8,8,8,8,
            7,7,7,7,
            6,6,6,6,
            5,5,5,5,
            4,4,4,4,
            3,3,3,3,
            2,2,2,2,
            1,1,1,1
        };

        /* LFO Phase Modulation table (verified on real YM3812) */
        private static sbyte[] lfo_pm_table = new sbyte[8 * 8 * 2]{
            /* FNUM2/FNUM = 00 0xxxxxxx (0x0000) */
            0, 0, 0, 0, 0, 0, 0, 0,	/*LFO PM depth = 0*/
            0, 0, 0, 0, 0, 0, 0, 0,	/*LFO PM depth = 1*/
            /* FNUM2/FNUM = 00 1xxxxxxx (0x0080) */
            0, 0, 0, 0, 0, 0, 0, 0,	/*LFO PM depth = 0*/
            1, 0, 0, 0,-1, 0, 0, 0,	/*LFO PM depth = 1*/
            /* FNUM2/FNUM = 01 0xxxxxxx (0x0100) */
            1, 0, 0, 0,-1, 0, 0, 0,	/*LFO PM depth = 0*/
            2, 1, 0,-1,-2,-1, 0, 1,	/*LFO PM depth = 1*/
            /* FNUM2/FNUM = 01 1xxxxxxx (0x0180) */
            1, 0, 0, 0,-1, 0, 0, 0,	/*LFO PM depth = 0*/
            3, 1, 0,-1,-3,-1, 0, 1,	/*LFO PM depth = 1*/
            /* FNUM2/FNUM = 10 0xxxxxxx (0x0200) */
            2, 1, 0,-1,-2,-1, 0, 1,	/*LFO PM depth = 0*/
            4, 2, 0,-2,-4,-2, 0, 2,	/*LFO PM depth = 1*/
            /* FNUM2/FNUM = 10 1xxxxxxx (0x0280) */
            2, 1, 0,-1,-2,-1, 0, 1,	/*LFO PM depth = 0*/
            5, 2, 0,-2,-5,-2, 0, 2,	/*LFO PM depth = 1*/
            /* FNUM2/FNUM = 11 0xxxxxxx (0x0300) */
            3, 1, 0,-1,-3,-1, 0, 1,	/*LFO PM depth = 0*/
            6, 3, 0,-3,-6,-3, 0, 3,	/*LFO PM depth = 1*/
            /* FNUM2/FNUM = 11 1xxxxxxx (0x0380) */
            3, 1, 0,-1,-3,-1, 0, 1,	/*LFO PM depth = 0*/
            7, 3, 0,-3,-7,-3, 0, 3	/*LFO PM depth = 1*/
        };


        /* lock level of common table */
        private static Int32 num_lock = 0;

        /* work table */
        private OPL3_SLOT SLOT7_1(OPL3 chip) { return chip.P_CH[7].SLOT[SLOT1]; }
        private OPL3_SLOT SLOT7_2(OPL3 chip) { return chip.P_CH[7].SLOT[SLOT2]; }
        private OPL3_SLOT SLOT8_1(OPL3 chip) { return chip.P_CH[8].SLOT[SLOT1]; }
        private OPL3_SLOT SLOT8_2(OPL3 chip) { return chip.P_CH[8].SLOT[SLOT2]; }

        /*INLINE int limit( int val, int max, int min ) {
            if ( val > max )
                val = max;
            else if ( val < min )
                val = min;

            return val;
        }*/

        /* status set and IRQ handling */
        private void OPL3_STATUS_SET(OPL3 chip, Int32 flag)
        {
            /* set status flag masking out disabled IRQs */
            chip.status |= (byte)(flag & chip.statusmask);
            if ((chip.status & 0x80) == 0)
            {
                if ((chip.status & 0x7f) != 0)
                {   /* IRQ on */
                    chip.status |= 0x80;
                    /* callback user interrupt handler (IRQ is OFF to ON) */
                    if (chip.IRQHandler != null) chip.IRQHandler(chip.IRQParam, 1);
                }
            }
        }

        /* status reset and IRQ handling */
        private void OPL3_STATUS_RESET(OPL3 chip, Int32 flag)
        {
            /* reset status flag */
            chip.status &= (byte)~flag;
            if ((chip.status & 0x80) != 0)
            {
                if ((chip.status & 0x7f) == 0)
                {
                    chip.status &= 0x7f;
                    /* callback user interrupt handler (IRQ is ON to OFF) */
                    if (chip.IRQHandler != null) chip.IRQHandler(chip.IRQParam, 0);
                }
            }
        }

        /* IRQ mask set */
        private void OPL3_STATUSMASK_SET(OPL3 chip, Int32 flag)
        {
            chip.statusmask = (byte)flag;
            /* IRQ handling check */
            OPL3_STATUS_SET(chip, 0);
            OPL3_STATUS_RESET(chip, 0);
        }

        /* advance LFO to next sample */
        private void advance_lfo(OPL3 chip)
        {
            byte tmp;

            /* LFO */
            chip.lfo_am_cnt += chip.lfo_am_inc;
            if (chip.lfo_am_cnt >= ((UInt32)LFO_AM_TAB_ELEMENTS << LFO_SH))    /* lfo_am_table is 210 elements long */
                chip.lfo_am_cnt -= ((UInt32)LFO_AM_TAB_ELEMENTS << LFO_SH);

            tmp = lfo_am_table[chip.lfo_am_cnt >> LFO_SH];

            if (chip.lfo_am_depth != 0)
                chip.LFO_AM = tmp;
            else
                chip.LFO_AM = (byte)(tmp >> 2);

            chip.lfo_pm_cnt += chip.lfo_pm_inc;
            chip.LFO_PM = (Int32)(((chip.lfo_pm_cnt >> LFO_SH) & 7) | chip.lfo_pm_depth_range);
        }

        /* advance to next sample */
        private void advance(OPL3 chip)
        {
            OPL3_CH CH;
            OPL3_SLOT op;
            Int32 i;

            chip.eg_timer += chip.eg_timer_add;

            while (chip.eg_timer >= chip.eg_timer_overflow)
            {
                chip.eg_timer -= chip.eg_timer_overflow;

                chip.eg_cnt++;

                for (i = 0; i < 9 * 2 * 2; i++)
                {
                    CH = chip.P_CH[i / 2];
                    op = CH.SLOT[i & 1];
                    //#if 1
                    /* Envelope Generator */
                    switch (op.state)
                    {
                        case EG_ATT:    /* attack phase */
                            //              if ( !(chip->eg_cnt & ((1<<op->eg_sh_ar)-1) ) )
                            if ((chip.eg_cnt & op.eg_m_ar) == 0)
                            {
                                op.volume += (~op.volume *
                                           (eg_inc[op.eg_sel_ar + ((chip.eg_cnt >> op.eg_sh_ar) & 7)])
                                          ) >> 3;

                                if (op.volume <= MIN_ATT_INDEX)
                                {
                                    op.volume = MIN_ATT_INDEX;
                                    op.state = EG_DEC;
                                }

                            }
                            break;

                        case EG_DEC:    /* decay phase */
                            //              if ( !(chip->eg_cnt & ((1<<op->eg_sh_dr)-1) ) )
                            if ((chip.eg_cnt & op.eg_m_dr) == 0)
                            {
                                op.volume += eg_inc[op.eg_sel_dr + ((chip.eg_cnt >> op.eg_sh_dr) & 7)];

                                if (op.volume >= op.sl)
                                    op.state = EG_SUS;

                            }
                            break;

                        case EG_SUS:    /* sustain phase */

                            /* this is important behaviour:
                            one can change percusive/non-percussive modes on the fly and
                            the chip will remain in sustain phase - verified on real YM3812 */

                            if (op.eg_type != 0)        /* non-percussive mode */
                            {
                                /* do nothing */
                            }
                            else                /* percussive mode */
                            {
                                /* during sustain phase chip adds Release Rate (in percussive mode) */
                                //                  if ( !(chip->eg_cnt & ((1<<op->eg_sh_rr)-1) ) )
                                if ((chip.eg_cnt & op.eg_m_rr) == 0)
                                {
                                    op.volume += eg_inc[op.eg_sel_rr + ((chip.eg_cnt >> op.eg_sh_rr) & 7)];

                                    if (op.volume >= MAX_ATT_INDEX)
                                        op.volume = MAX_ATT_INDEX;
                                }
                                /* else do nothing in sustain phase */
                            }
                            break;

                        case EG_REL:    /* release phase */
                            //              if ( !(chip->eg_cnt & ((1<<op->eg_sh_rr)-1) ) )
                            if ((chip.eg_cnt & op.eg_m_rr) == 0)
                            {
                                op.volume += eg_inc[op.eg_sel_rr + ((chip.eg_cnt >> op.eg_sh_rr) & 7)];

                                if (op.volume >= MAX_ATT_INDEX)
                                {
                                    op.volume = MAX_ATT_INDEX;
                                    op.state = EG_OFF;
                                }

                            }
                            break;

                        default:
                            break;
                    }
                    //#endif
                }
            }

            for (i = 0; i < 9 * 2 * 2; i++)
            {
                CH = chip.P_CH[i / 2];
                op = CH.SLOT[i & 1];

                /* Phase Generator */
                if (op.vib != 0)
                {
                    byte block;
                    UInt32 block_fnum = CH.block_fnum;

                    UInt32 fnum_lfo = (block_fnum & 0x0380) >> 7;

                    Int32 lfo_fn_table_index_offset = lfo_pm_table[chip.LFO_PM + 16 * fnum_lfo];

                    if (lfo_fn_table_index_offset != 0)  /* LFO phase modulation active */
                    {
                        block_fnum += (UInt32)lfo_fn_table_index_offset;
                        block = (byte)((block_fnum & 0x1c00) >> 10);
                        op.Cnt += (chip.fn_tab[block_fnum & 0x03ff] >> (7 - block)) * op.mul;
                    }
                    else    /* LFO phase modulation  = zero */
                    {
                        op.Cnt += op.Incr;
                    }
                }
                else    /* LFO phase modulation disabled for this operator */
                {
                    op.Cnt += op.Incr;
                }
            }

            /*  The Noise Generator of the YM3812 is 23-bit shift register.
            *   Period is equal to 2^23-2 samples.
            *   Register works at sampling frequency of the chip, so output
            *   can change on every sample.
            *
            *   Output of the register and input to the bit 22 is:
            *   bit0 XOR bit14 XOR bit15 XOR bit22
            *
            *   Simply use bit 22 as the noise output.
            */

            chip.noise_p += chip.noise_f;
            i = (Int32)(chip.noise_p >> FREQ_SH);       /* number of events (shifts of the shift register) */
            chip.noise_p &= (UInt32)FREQ_MASK;
            while (i != 0)
            {
                /*
                UINT32 j;
                j = ( (chip->noise_rng) ^ (chip->noise_rng>>14) ^ (chip->noise_rng>>15) ^ (chip->noise_rng>>22) ) & 1;
                chip->noise_rng = (j<<22) | (chip->noise_rng>>1);
                */

                /*
                    Instead of doing all the logic operations above, we
                    use a trick here (and use bit 0 as the noise output).
                    The difference is only that the noise bit changes one
                    step ahead. This doesn't matter since we don't know
                    what is real state of the noise_rng after the reset.
                */

                if ((chip.noise_rng & 1) != 0) chip.noise_rng ^= 0x800302;
                chip.noise_rng >>= 1;

                i--;
            }
        }


        private Int32 op_calc(UInt32 phase, UInt32 env, Int32 pm, UInt32 wave_tab)
        {
            UInt32 p;

            p = (env << 4) + sin_tab[wave_tab + ((((Int32)((phase & ~FREQ_MASK) + (pm << 16))) >> FREQ_SH) & SIN_MASK)];

            if (p >= TL_TAB_LEN)
                return 0;
            return tl_tab[p];
        }

        private Int32 op_calc1(UInt32 phase, UInt32 env, Int32 pm, UInt32 wave_tab)
        {
            UInt32 p;

            p = (env << 4) + sin_tab[wave_tab + ((((Int32)((phase & ~FREQ_MASK) + pm)) >> FREQ_SH) & SIN_MASK)];

            if (p >= TL_TAB_LEN)
                return 0;
            return tl_tab[p];
        }


        private UInt32 volume_calc(OPL3 chip, OPL3_SLOT OP) { return (UInt32)(OP.TLL + ((UInt32)OP.volume) + (chip.LFO_AM & OP.AMmask)); }

        /* calculate output of a standard 2 operator channel
         (or 1st part of a 4-op channel) */
        private void chan_calc(OPL3 chip, OPL3_CH CH)
        {
            OPL3_SLOT SLOT;
            UInt32 env;
            Int32 _out;

            if (CH.Muted != 0) return;

            chip.phase_modulation = 0;
            chip.phase_modulation2 = 0;

            /* SLOT 1 */
            SLOT = CH.SLOT[SLOT1];
            env = volume_calc(chip, SLOT);
            _out = SLOT.op1_out[0] + SLOT.op1_out[1];
            SLOT.op1_out[0] = SLOT.op1_out[1];
            SLOT.op1_out[1] = 0;
            if (env < ENV_QUIET)
            {
                if (SLOT.FB == 0)
                    _out = 0;
                SLOT.op1_out[1] = op_calc1(SLOT.Cnt, env, (_out << SLOT.FB), SLOT.wavetable);
            }
            SLOT.connect += SLOT.op1_out[1];
            //logerror("out0=%5i vol0=%4i ", SLOT->op1_out[1], env );

            /* SLOT 2 */
            //SLOT++;
            SLOT = CH.SLOT[SLOT2];
            env = volume_calc(chip, SLOT);
            if (env < ENV_QUIET)
                SLOT.connect += op_calc(SLOT.Cnt, env, chip.phase_modulation, SLOT.wavetable);

            //logerror("out1=%5i vol1=%4i\n", op_calc(SLOT->Cnt, env, chip->phase_modulation, SLOT->wavetable), env );

        }

        /* calculate output of a 2nd part of 4-op channel */
        private void chan_calc_ext(OPL3 chip, OPL3_CH CH)
        {
            OPL3_SLOT SLOT;
            UInt32 env;

            if (CH.Muted != 0)
                return;

            chip.phase_modulation = 0;

            /* SLOT 1 */
            SLOT = CH.SLOT[SLOT1];
            env = volume_calc(chip, SLOT);
            if (env < ENV_QUIET)
                SLOT.connect += op_calc(SLOT.Cnt, env, chip.phase_modulation2, SLOT.wavetable);

            /* SLOT 2 */
            //SLOT++;
            SLOT = CH.SLOT[SLOT2];
            env = volume_calc(chip, SLOT);
            if (env < ENV_QUIET)
                SLOT.connect += op_calc(SLOT.Cnt, env, chip.phase_modulation, SLOT.wavetable);

        }

        /*
            operators used in the rhythm sounds generation process:

            Envelope Generator:

        channel  operator  register number   Bass  High  Snare Tom  Top
        / slot   number    TL ARDR SLRR Wave Drum  Hat   Drum  Tom  Cymbal
         6 / 0   12        50  70   90   f0  +
         6 / 1   15        53  73   93   f3  +
         7 / 0   13        51  71   91   f1        +
         7 / 1   16        54  74   94   f4              +
         8 / 0   14        52  72   92   f2                    +
         8 / 1   17        55  75   95   f5                          +

            Phase Generator:

        channel  operator  register number   Bass  High  Snare Tom  Top
        / slot   number    MULTIPLE          Drum  Hat   Drum  Tom  Cymbal
         6 / 0   12        30                +
         6 / 1   15        33                +
         7 / 0   13        31                      +     +           +
         7 / 1   16        34                -----  n o t  u s e d -----
         8 / 0   14        32                                  +
         8 / 1   17        35                      +                 +

        channel  operator  register number   Bass  High  Snare Tom  Top
        number   number    BLK/FNUM2 FNUM    Drum  Hat   Drum  Tom  Cymbal
           6     12,15     B6        A6      +

           7     13,16     B7        A7            +     +           +

           8     14,17     B8        A8            +           +     +

        */

        /* calculate rhythm */

        private void chan_calc_rhythm(OPL3 chip, OPL3_CH[] CH, Int32 num, UInt32 noise)
        {
            OPL3_SLOT SLOT;
            Int32[] chanout = chip.chanout;
            Int32 _out;
            UInt32 env;


            /* Bass Drum (verified on real YM3812):
              - depends on the channel 6 'connect' register:
                  when connect = 0 it works the same as in normal (non-rhythm) mode (op1->op2->out)
                  when connect = 1 _only_ operator 2 is present on output (op2->out), operator 1 is ignored
              - output sample always is multiplied by 2
            */

            chip.phase_modulation = 0;

            /* SLOT 1 */
            SLOT = CH[6 + num].SLOT[SLOT1];
            env = volume_calc(chip, SLOT);

            _out = SLOT.op1_out[0] + SLOT.op1_out[1];
            SLOT.op1_out[0] = SLOT.op1_out[1];

            if (SLOT.CON == 0)
                chip.phase_modulation = SLOT.op1_out[0];
            //else ignore output of operator 1

            SLOT.op1_out[1] = 0;
            if (env < ENV_QUIET)
            {
                if (SLOT.FB == 0)
                    _out = 0;
                SLOT.op1_out[1] = op_calc1(SLOT.Cnt, env, (_out << SLOT.FB), SLOT.wavetable);
            }

            /* SLOT 2 */
            //SLOT++;
            SLOT = CH[6 + num].SLOT[SLOT2];
            env = volume_calc(chip, SLOT);
            if (env < ENV_QUIET && chip.MuteSpc[0] == 0)
                chanout[6] += op_calc(SLOT.Cnt, env, chip.phase_modulation, SLOT.wavetable) * 2;


            /* Phase generation is based on: */
            // HH  (13) channel 7->slot 1 combined with channel 8->slot 2 (same combination as TOP CYMBAL but different output phases)
            // SD  (16) channel 7->slot 1
            // TOM (14) channel 8->slot 1
            // TOP (17) channel 7->slot 1 combined with channel 8->slot 2 (same combination as HIGH HAT but different output phases)

            /* Envelope generation based on: */
            // HH  channel 7->slot1
            // SD  channel 7->slot2
            // TOM channel 8->slot1
            // TOP channel 8->slot2


            /* The following formulas can be well optimized.
               I leave them in direct form for now (in case I've missed something).
            */

            /* High Hat (verified on real YM3812) */
            env = volume_calc(chip, SLOT7_1(chip));
            if (env < ENV_QUIET && chip.MuteSpc[4] == 0)
            {

                /* high hat phase generation:
                    phase = d0 or 234 (based on frequency only)
                    phase = 34 or 2d0 (based on noise)
                */

                /* base frequency derived from operator 1 in channel 7 */
                byte bit7 = (byte)(((SLOT7_1(chip).Cnt >> FREQ_SH) >> 7) & 1);
                byte bit3 = (byte)(((SLOT7_1(chip).Cnt >> FREQ_SH) >> 3) & 1);
                byte bit2 = (byte)(((SLOT7_1(chip).Cnt >> FREQ_SH) >> 2) & 1);

                byte res1 = (byte)((bit2 ^ bit7) | bit3);

                /* when res1 = 0 phase = 0x000 | 0xd0; */
                /* when res1 = 1 phase = 0x200 | (0xd0>>2); */
                UInt32 phase = (UInt32)(res1 != 0 ? (0x200 | (0xd0 >> 2)) : 0xd0);

                /* enable gate based on frequency of operator 2 in channel 8 */
                byte bit5e = (byte)(((SLOT8_2(chip).Cnt >> FREQ_SH) >> 5) & 1);
                byte bit3e = (byte)(((SLOT8_2(chip).Cnt >> FREQ_SH) >> 3) & 1);

                byte res2 = (byte)(bit3e ^ bit5e);

                /* when res2 = 0 pass the phase from calculation above (res1); */
                /* when res2 = 1 phase = 0x200 | (0xd0>>2); */
                if (res2 != 0)
                    phase = (0x200 | (0xd0 >> 2));


                /* when phase & 0x200 is set and noise=1 then phase = 0x200|0xd0 */
                /* when phase & 0x200 is set and noise=0 then phase = 0x200|(0xd0>>2), ie no change */
                if ((phase & 0x200) != 0)
                {
                    if (noise != 0)
                        phase = 0x200 | 0xd0;
                }
                else
                /* when phase & 0x200 is clear and noise=1 then phase = 0xd0>>2 */
                /* when phase & 0x200 is clear and noise=0 then phase = 0xd0, ie no change */
                {
                    if (noise != 0)
                        phase = 0xd0 >> 2;
                }

                chanout[7] += op_calc(phase << FREQ_SH, env, 0, SLOT7_1(chip).wavetable) * 2;
            }

            /* Snare Drum (verified on real YM3812) */
            env = volume_calc(chip, SLOT7_2(chip));
            if (env < ENV_QUIET && chip.MuteSpc[1] == 0)
            {
                /* base frequency derived from operator 1 in channel 7 */
                byte bit8 = (byte)(((SLOT7_1(chip).Cnt >> FREQ_SH) >> 8) & 1);

                /* when bit8 = 0 phase = 0x100; */
                /* when bit8 = 1 phase = 0x200; */
                UInt32 phase = (UInt32)(bit8 != 0 ? 0x200 : 0x100);

                /* Noise bit XOR'es phase by 0x100 */
                /* when noisebit = 0 pass the phase from calculation above */
                /* when noisebit = 1 phase ^= 0x100; */
                /* in other words: phase ^= (noisebit<<8); */
                if (noise != 0)
                    phase ^= 0x100;

                chanout[7] += op_calc(phase << FREQ_SH, env, 0, SLOT7_2(chip).wavetable) * 2;
            }

            /* Tom Tom (verified on real YM3812) */
            env = volume_calc(chip, SLOT8_1(chip));
            if (env < ENV_QUIET && chip.MuteSpc[2] == 0)
                chanout[8] += op_calc(SLOT8_1(chip).Cnt, env, 0, SLOT8_1(chip).wavetable) * 2;

            /* Top Cymbal (verified on real YM3812) */
            env = volume_calc(chip, SLOT8_2(chip));
            if (env < ENV_QUIET && chip.MuteSpc[3] == 0)
            {
                /* base frequency derived from operator 1 in channel 7 */
                byte bit7 = (byte)(((SLOT7_1(chip).Cnt >> FREQ_SH) >> 7) & 1);
                byte bit3 = (byte)(((SLOT7_1(chip).Cnt >> FREQ_SH) >> 3) & 1);
                byte bit2 = (byte)(((SLOT7_1(chip).Cnt >> FREQ_SH) >> 2) & 1);
                byte res1 = (byte)((bit2 ^ bit7) | bit3);

                /* when res1 = 0 phase = 0x000 | 0x100; */
                /* when res1 = 1 phase = 0x200 | 0x100; */
                UInt32 phase = (UInt32)(res1 != 0 ? 0x300 : 0x100);

                /* enable gate based on frequency of operator 2 in channel 8 */
                byte bit5e = (byte)(((SLOT8_2(chip).Cnt >> FREQ_SH) >> 5) & 1);
                byte bit3e = (byte)(((SLOT8_2(chip).Cnt >> FREQ_SH) >> 3) & 1);

                byte res2 = (byte)(bit3e ^ bit5e);
                /* when res2 = 0 pass the phase from calculation above (res1); */
                /* when res2 = 1 phase = 0x200 | 0x100; */
                if (res2 != 0)
                    phase = 0x300;

                chanout[8] += op_calc(phase << FREQ_SH, env, 0, SLOT8_2(chip).wavetable) * 2;
            }

        }


        /* generic table initialize */
        private Int32 init_tables()
        {
            Int32 i, x;
            Int32 n;
            double o, m;


            for (x = 0; x < TL_RES_LEN; x++)
            {
                m = (1 << 16) / Math.Pow(2, (x + 1) * (ENV_STEP / 4.0) / 8.0);
                m = Math.Floor(m);

                /* we never reach (1<<16) here due to the (x+1) */
                /* result fits within 16 bits at maximum */

                n = (Int32)m;     /* 16 bits here */
                n >>= 4;        /* 12 bits here */
                if ((n & 1) != 0)      /* round to nearest */
                    n = (n >> 1) + 1;
                else
                    n = n >> 1;
                /* 11 bits here (rounded) */
                n <<= 1;        /* 12 bits here (as in real chip) */
                tl_tab[x * 2 + 0] = n;
                tl_tab[x * 2 + 1] = ~tl_tab[x * 2 + 0]; /* this *is* different from OPL2 (verified on real YMF262) */

                for (i = 1; i < 13; i++)
                {
                    tl_tab[x * 2 + 0 + i * 2 * TL_RES_LEN] = tl_tab[x * 2 + 0] >> i;
                    tl_tab[x * 2 + 1 + i * 2 * TL_RES_LEN] = ~tl_tab[x * 2 + 0 + i * 2 * TL_RES_LEN];  /* this *is* different from OPL2 (verified on real YMF262) */
                }
                //#if 0
                //			logerror("tl %04i", x*2);
                //			for (i=0; i<13; i++)
                //				logerror(", [%02i] %5i", i*2, tl_tab[ x*2 +0 + i*2*TL_RES_LEN ] ); /* positive */
                //			logerror("\n");

                //			logerror("tl %04i", x*2);
                //			for (i=0; i<13; i++)
                //				logerror(", [%02i] %5i", i*2, tl_tab[ x*2 +1 + i*2*TL_RES_LEN ] ); /* negative */
                //			logerror("\n");
                //#endif
            }

            for (i = 0; i < SIN_LEN; i++)
            {
                /* non-standard sinus */
                m = Math.Sin(((i * 2) + 1) * Math.PI / SIN_LEN); /* checked against the real chip */

                /* we never reach zero here due to ((i*2)+1) */

                if (m > 0.0)
                    o = 8 * Math.Log(1.0 / m) / Math.Log(2.0);    /* convert to 'decibels' */
                else
                    o = 8 * Math.Log(-1.0 / m) / Math.Log(2.0);   /* convert to 'decibels' */

                o = o / (ENV_STEP / 4);

                n = (int)(2.0 * o);
                if ((n & 1) != 0)                      /* round to nearest */
                    n = (n >> 1) + 1;
                else
                    n = n >> 1;

                sin_tab[i] = (UInt32)(n * 2 + (m >= 0.0 ? 0 : 1));

                /*logerror("YMF262.C: sin [%4i (hex=%03x)]= %4i (tl_tab value=%5i)\n", i, i, sin_tab[i], tl_tab[sin_tab[i]] );*/
            }

            for (i = 0; i < SIN_LEN; i++)
            {
                /* these 'pictures' represent _two_ cycles */
                /* waveform 1:  __      __     */
                /*             /  \____/  \____*/
                /* output only first half of the sinus waveform (positive one) */

                if ((i & (1 << (SIN_BITS - 1))) != 0)
                    sin_tab[1 * SIN_LEN + i] = (UInt32)TL_TAB_LEN;
                else
                    sin_tab[1 * SIN_LEN + i] = sin_tab[i];

                /* waveform 2:  __  __  __  __ */
                /*             /  \/  \/  \/  \*/
                /* abs(sin) */

                sin_tab[2 * SIN_LEN + i] = sin_tab[i & (SIN_MASK >> 1)];

                /* waveform 3:  _   _   _   _  */
                /*             / |_/ |_/ |_/ |_*/
                /* abs(output only first quarter of the sinus waveform) */

                if ((i & (1 << (SIN_BITS - 2))) != 0)
                    sin_tab[3 * SIN_LEN + i] = (UInt32)TL_TAB_LEN;
                else
                    sin_tab[3 * SIN_LEN + i] = sin_tab[i & (SIN_MASK >> 2)];

                /* waveform 4:                 */
                /*             /\  ____/\  ____*/
                /*               \/      \/    */
                /* output whole sinus waveform in half the cycle(step=2) and output 0 on the other half of cycle */

                if ((i & (1 << (SIN_BITS - 1))) != 0)
                    sin_tab[4 * SIN_LEN + i] = (UInt32)TL_TAB_LEN;
                else
                    sin_tab[4 * SIN_LEN + i] = sin_tab[i * 2];

                /* waveform 5:                 */
                /*             /\/\____/\/\____*/
                /*                             */
                /* output abs(whole sinus) waveform in half the cycle(step=2) and output 0 on the other half of cycle */

                if ((i & (1 << (SIN_BITS - 1))) != 0)
                    sin_tab[5 * SIN_LEN + i] = (UInt32)TL_TAB_LEN;
                else
                    sin_tab[5 * SIN_LEN + i] = sin_tab[(i * 2) & (SIN_MASK >> 1)];

                /* waveform 6: ____    ____    */
                /*                             */
                /*                 ____    ____*/
                /* output maximum in half the cycle and output minimum on the other half of cycle */

                if ((i & (1 << (SIN_BITS - 1))) != 0)
                    sin_tab[6 * SIN_LEN + i] = 1;   /* negative */
                else
                    sin_tab[6 * SIN_LEN + i] = 0;   /* positive */

                /* waveform 7:                 */
                /*             |\____  |\____  */
                /*                   \|      \|*/
                /* output sawtooth waveform    */

                if ((i & (1 << (SIN_BITS - 1))) != 0)
                    x = ((SIN_LEN - 1) - i) * 16 + 1;   /* negative: from 8177 to 1 */
                else
                    x = i * 16; /*positive: from 0 to 8176 */

                if (x > TL_TAB_LEN)
                    x = TL_TAB_LEN; /* clip to the allowed range */

                sin_tab[7 * SIN_LEN + i] = (UInt32)x;

                //logerror("YMF262.C: sin1[%4i]= %4i (tl_tab value=%5i)\n", i, sin_tab[1*SIN_LEN+i], tl_tab[sin_tab[1*SIN_LEN+i]] );
                //logerror("YMF262.C: sin2[%4i]= %4i (tl_tab value=%5i)\n", i, sin_tab[2*SIN_LEN+i], tl_tab[sin_tab[2*SIN_LEN+i]] );
                //logerror("YMF262.C: sin3[%4i]= %4i (tl_tab value=%5i)\n", i, sin_tab[3*SIN_LEN+i], tl_tab[sin_tab[3*SIN_LEN+i]] );
                //logerror("YMF262.C: sin4[%4i]= %4i (tl_tab value=%5i)\n", i, sin_tab[4*SIN_LEN+i], tl_tab[sin_tab[4*SIN_LEN+i]] );
                //logerror("YMF262.C: sin5[%4i]= %4i (tl_tab value=%5i)\n", i, sin_tab[5*SIN_LEN+i], tl_tab[sin_tab[5*SIN_LEN+i]] );
                //logerror("YMF262.C: sin6[%4i]= %4i (tl_tab value=%5i)\n", i, sin_tab[6*SIN_LEN+i], tl_tab[sin_tab[6*SIN_LEN+i]] );
                //logerror("YMF262.C: sin7[%4i]= %4i (tl_tab value=%5i)\n", i, sin_tab[7*SIN_LEN+i], tl_tab[sin_tab[7*SIN_LEN+i]] );
            }
            /*logerror("YMF262.C: ENV_QUIET= %08x (dec*8=%i)\n", ENV_QUIET, ENV_QUIET*8 );*/

            //# ifdef SAVE_SAMPLE
            //    sample[0] = fopen("sampsum.pcm", "wb");
            //#endif

            return 1;
        }

        private void OPLCloseTable()
        {
            //# ifdef SAVE_SAMPLE
            //    fclose(sample[0]);
            //#endif
        }

        private void OPL3_initalize(OPL3 chip)
        {
            Int32 i;

            /* frequency base */
            chip.freqbase = (chip.rate) != 0 ? ((double)chip.clock / (8.0 * 36)) / chip.rate : 0;
            //#if 0
            //	chip->rate = (double)chip->clock / (8.0*36);
            //	chip->freqbase  = 1.0;
            //#endif

            /* logerror("YMF262: freqbase=%f\n", chip->freqbase); */

            /* Timer base time */
            //chip->TimerBase = attotime_mul(ATTOTIME_IN_HZ(chip->clock), 8*36);

            /* make fnumber -> increment counter table */
            for (i = 0; i < 1024; i++)
            {
                /* opn phase increment counter = 20bit */
                chip.fn_tab[i] = (UInt32)((double)i * 64 * chip.freqbase * (1 << (FREQ_SH - 10))); /* -10 because chip works with 10.10 fixed point, while we use 16.16 */
                                                                                                   //#if 0
                                                                                                   //		logerror("YMF262.C: fn_tab[%4i] = %08x (dec=%8i)\n",
                                                                                                   //				 i, chip->fn_tab[i]>>6, chip->fn_tab[i]>>6 );
                                                                                                   //#endif
            }

            //#if 0
            //	for( i=0 ; i < 16 ; i++ )
            //	{
            //		logerror("YMF262.C: sl_tab[%i] = %08x\n",
            //			i, sl_tab[i] );
            //	}
            //	for( i=0 ; i < 8 ; i++ )
            //	{
            //		int j;
            //		logerror("YMF262.C: ksl_tab[oct=%2i] =",i);
            //		for (j=0; j<16; j++)
            //		{
            //			logerror("%08x ", ksl_tab[i*16+j] );
            //		}
            //		logerror("\n");
            //	}
            //#endif


            /* Amplitude modulation: 27 output levels (triangle waveform); 1 level takes one of: 192, 256 or 448 samples */
            /* One entry from LFO_AM_TABLE lasts for 64 samples */
            chip.lfo_am_inc = (UInt32)((1.0 / 64.0) * (1 << LFO_SH) * chip.freqbase);

            /* Vibrato: 8 output levels (triangle waveform); 1 level takes 1024 samples */
            chip.lfo_pm_inc = (UInt32)((1.0 / 1024.0) * (1 << LFO_SH) * chip.freqbase);

            /*logerror ("chip->lfo_am_inc = %8x ; chip->lfo_pm_inc = %8x\n", chip->lfo_am_inc, chip->lfo_pm_inc);*/

            /* Noise generator: a step takes 1 sample */
            chip.noise_f = (UInt32)((1.0 / 1.0) * (1 << FREQ_SH) * chip.freqbase);

            chip.eg_timer_add = (UInt32)((1 << EG_SH) * chip.freqbase);
            chip.eg_timer_overflow = (UInt32)((1) * (1 << EG_SH));
            /*logerror("YMF262init eg_timer_add=%8x eg_timer_overflow=%8x\n", chip->eg_timer_add, chip->eg_timer_overflow);*/

        }

        private void FM_KEYON(OPL3_SLOT SLOT, UInt32 key_set)
        {
            if (SLOT.key == 0)
            {
                /* restart Phase Generator */
                SLOT.Cnt = 0;
                /* phase -> Attack */
                SLOT.state = EG_ATT;
            }
            SLOT.key |= key_set;
        }

        private void FM_KEYOFF(OPL3_SLOT SLOT, UInt32 key_clr)
        {
            if (SLOT.key != 0)
            {
                SLOT.key &= key_clr;

                if (SLOT.key == 0)
                {
                    /* phase -> Release */
                    if (SLOT.state > EG_REL)
                        SLOT.state = (byte)EG_REL;
                }
            }
        }

        /* update phase increment counter of operator (also update the EG rates if necessary) */
        private void CALC_FCSLOT(OPL3_CH CH, OPL3_SLOT SLOT)
        {
            Int32 ksr;

            /* (frequency) phase increment counter */
            SLOT.Incr = CH.fc * SLOT.mul;
            ksr = CH.kcode >> SLOT.KSR;

            if (SLOT.ksr != ksr)
            {
                SLOT.ksr = (byte)ksr;

                /* calculate envelope generator rates */
                if ((SLOT.ar + SLOT.ksr) < 16 + 60)
                {
                    SLOT.eg_sh_ar = eg_rate_shift[SLOT.ar + SLOT.ksr];
                    SLOT.eg_m_ar = (UInt32)((1 << SLOT.eg_sh_ar) - 1);
                    SLOT.eg_sel_ar = eg_rate_select[SLOT.ar + SLOT.ksr];
                }
                else
                {
                    SLOT.eg_sh_ar = 0;
                    SLOT.eg_m_ar = (UInt32)((1 << SLOT.eg_sh_ar) - 1);
                    SLOT.eg_sel_ar = 13 * RATE_STEPS;
                }
                SLOT.eg_sh_dr = eg_rate_shift[SLOT.dr + SLOT.ksr];
                SLOT.eg_m_dr = (UInt32)((1 << SLOT.eg_sh_dr) - 1);
                SLOT.eg_sel_dr = eg_rate_select[SLOT.dr + SLOT.ksr];
                SLOT.eg_sh_rr = eg_rate_shift[SLOT.rr + SLOT.ksr];
                SLOT.eg_m_rr = (UInt32)((1 << SLOT.eg_sh_rr) - 1);
                SLOT.eg_sel_rr = eg_rate_select[SLOT.rr + SLOT.ksr];
            }
        }

        /* set multi,am,vib,EG-TYP,KSR,mul */
        private void set_mul(OPL3 chip, Int32 slot, Int32 v)
        {
            OPL3_CH CH = chip.P_CH[slot / 2];
            OPL3_CH CH_3 = ((slot / 2 - 3) >= 0 && (slot / 2 - 3) < chip.P_CH.Length) ? chip.P_CH[slot / 2 - 3] : null;
            OPL3_SLOT SLOT = CH.SLOT[slot & 1];

            SLOT.mul = mul_tab[v & 0x0f];
            SLOT.KSR = (byte)((v & 0x10) != 0 ? 0 : 2);
            SLOT.eg_type = (byte)(v & 0x20);
            SLOT.vib = (byte)(v & 0x40);
            SLOT.AMmask = (byte)((v & 0x80) != 0 ? ~0 : 0);

            if ((chip.OPL3_mode & 1) != 0)
            {
                Int32 chan_no = slot / 2;

                /* in OPL3 mode */
                //DO THIS:
                //if this is one of the slots of 1st channel forming up a 4-op channel
                //do normal operation
                //else normal 2 operator function
                //OR THIS:
                //if this is one of the slots of 2nd channel forming up a 4-op channel
                //update it using channel data of 1st channel of a pair
                //else normal 2 operator function
                switch (chan_no)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 9:
                    case 10:
                    case 11:
                        if (CH.extended != 0)
                        {
                            /* normal */
                            CALC_FCSLOT(CH, SLOT);
                        }
                        else
                        {
                            /* normal */
                            CALC_FCSLOT(CH, SLOT);
                        }
                        break;
                    case 3:
                    case 4:
                    case 5:
                    case 12:
                    case 13:
                    case 14:
                        if (CH_3.extended != 0)
                        {
                            /* update this SLOT using frequency data for 1st channel of a pair */
                            CALC_FCSLOT(CH_3, SLOT);
                        }
                        else
                        {
                            /* normal */
                            CALC_FCSLOT(CH, SLOT);
                        }
                        break;
                    default:
                        /* normal */
                        CALC_FCSLOT(CH, SLOT);
                        break;
                }
            }
            else
            {
                /* in OPL2 mode */
                CALC_FCSLOT(CH, SLOT);
            }
        }

        /* set ksl & tl */
        private void set_ksl_tl(OPL3 chip, int slot, int v)
        {
            OPL3_CH CH = chip.P_CH[slot / 2];
            //OPL3_CH CH_3 = chip.P_CH[slot / 2 - 3];
            OPL3_CH CH_3 = ((slot/2 - 3) >= 0 && (slot/2 - 3) < chip.P_CH.Length) ? chip.P_CH[slot/2 - 3] : null;
            OPL3_SLOT SLOT = CH.SLOT[slot & 1];

            SLOT.ksl = (byte)ksl_shift[v >> 6];
            SLOT.TL = (UInt32)((v & 0x3f) << (ENV_BITS - 1 - 7)); /* 7 bits TL (bit 6 = always 0) */

            if ((chip.OPL3_mode & 1) != 0)
            {
                Int32 chan_no = slot / 2;

                /* in OPL3 mode */
                //DO THIS:
                //if this is one of the slots of 1st channel forming up a 4-op channel
                //do normal operation
                //else normal 2 operator function
                //OR THIS:
                //if this is one of the slots of 2nd channel forming up a 4-op channel
                //update it using channel data of 1st channel of a pair
                //else normal 2 operator function
                switch (chan_no)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 9:
                    case 10:
                    case 11:
                        if (CH.extended != 0)
                        {
                            /* normal */
                            SLOT.TLL = (Int32)(SLOT.TL + (CH.ksl_base >> SLOT.ksl));
                        }
                        else
                        {
                            /* normal */
                            SLOT.TLL = (Int32)(SLOT.TL + (CH.ksl_base >> SLOT.ksl));
                        }
                        break;
                    case 3:
                    case 4:
                    case 5:
                    case 12:
                    case 13:
                    case 14:
                        if (CH_3.extended != 0)
                        {
                            /* update this SLOT using frequency data for 1st channel of a pair */
                            SLOT.TLL = (Int32)(SLOT.TL + (CH_3.ksl_base >> SLOT.ksl));
                        }
                        else
                        {
                            /* normal */
                            SLOT.TLL = (Int32)(SLOT.TL + (CH.ksl_base >> SLOT.ksl));
                        }
                        break;
                    default:
                        /* normal */
                        SLOT.TLL = (Int32)(SLOT.TL + (CH.ksl_base >> SLOT.ksl));
                        break;
                }
            }
            else
            {
                /* in OPL2 mode */
                SLOT.TLL = (Int32)(SLOT.TL + (CH.ksl_base >> SLOT.ksl));
            }

        }

        /* set attack rate & decay rate  */
        private void set_ar_dr(OPL3 chip, Int32 slot, Int32 v)
        {
            OPL3_CH CH = chip.P_CH[slot / 2];
            OPL3_SLOT SLOT = CH.SLOT[slot & 1];

            SLOT.ar = (UInt32)((v >> 4) != 0 ? 16 + ((v >> 4) << 2) : 0);

            if ((SLOT.ar + SLOT.ksr) < 16 + 60) /* verified on real YMF262 - all 15 x rates take "zero" time */
            {
                SLOT.eg_sh_ar = eg_rate_shift[SLOT.ar + SLOT.ksr];
                SLOT.eg_m_ar = (UInt32)((1 << SLOT.eg_sh_ar) - 1);
                SLOT.eg_sel_ar = eg_rate_select[SLOT.ar + SLOT.ksr];
            }
            else
            {
                SLOT.eg_sh_ar = 0;
                SLOT.eg_m_ar = (UInt32)((1 << SLOT.eg_sh_ar) - 1);
                SLOT.eg_sel_ar = 13 * RATE_STEPS;
            }

            SLOT.dr = (UInt32)((v & 0x0f) != 0 ? 16 + ((v & 0x0f) << 2) : 0);
            SLOT.eg_sh_dr = eg_rate_shift[SLOT.dr + SLOT.ksr];
            SLOT.eg_m_dr = (UInt32)((1 << SLOT.eg_sh_dr) - 1);
            SLOT.eg_sel_dr = eg_rate_select[SLOT.dr + SLOT.ksr];
        }

        /* set sustain level & release rate */
        private void set_sl_rr(OPL3 chip, Int32 slot, Int32 v)
        {
            OPL3_CH CH = chip.P_CH[slot / 2];
            OPL3_SLOT SLOT = CH.SLOT[slot & 1];

            SLOT.sl = sl_tab[v >> 4];

            SLOT.rr = (UInt32)((v & 0x0f) != 0 ? 16 + ((v & 0x0f) << 2) : 0);
            SLOT.eg_sh_rr = eg_rate_shift[SLOT.rr + SLOT.ksr];
            SLOT.eg_m_rr = (UInt32)((1 << SLOT.eg_sh_rr) - 1);
            SLOT.eg_sel_rr = eg_rate_select[SLOT.rr + SLOT.ksr];
        }


        private void update_channels(OPL3 chip, OPL3_CH CH)
        {
            /* update channel passed as a parameter and a channel at CH+=3; */
            if (CH.extended != 0)
            {   /* we've just switched to combined 4 operator mode */

            }
            else
            {   /* we've just switched to normal 2 operator mode */

            }

        }

        /* write a value v to register r on OPL chip */
        private void OPL3WriteReg(OPL3 chip, Int32 r, Int32 v)
        {
            OPL3_CH CH;
            OPL3_CH CH_P3;
            OPL3_CH CH_M3;
            Int32[] chanout = chip.chanout;
            UInt32 ch_offset = 0;
            Int32 slot;
            Int32 block_fnum;



            /*if (LOG_CYM_FILE && (cymfile) && ((r&255)!=0) && (r!=255) )
            {
                if (r>0xff)
                    fputc( (unsigned char)0xff, cymfile );//mark writes to second register set

                fputc( (unsigned char)r&0xff, cymfile );
                fputc( (unsigned char)v, cymfile );
            }*/

            if ((r & 0x100) != 0)
            {
                switch (r)
                {
                    case 0x101: /* test register */
                        return;

                    case 0x104: /* 6 channels enable */
                        {
                            byte prev;

                            CH = chip.P_CH[0];    /* channel 0 */
                            prev = CH.extended;
                            CH.extended = (byte)((v >> 0) & 1);
                            if (prev != CH.extended)
                                update_channels(chip, CH);
                            //CH++;                   /* channel 1 */
                            CH = chip.P_CH[1];
                            prev = CH.extended;
                            CH.extended = (byte)((v >> 1) & 1);
                            if (prev != CH.extended)
                                update_channels(chip, CH);
                            //CH++;                   /* channel 2 */
                            CH = chip.P_CH[2];
                            prev = CH.extended;
                            CH.extended = (byte)((v >> 2) & 1);
                            if (prev != CH.extended)
                                update_channels(chip, CH);


                            CH = chip.P_CH[9];    /* channel 9 */
                            prev = CH.extended;
                            CH.extended = (byte)((v >> 3) & 1);
                            if (prev != CH.extended)
                                update_channels(chip, CH);
                            //CH++;                   /* channel 10 */
                            CH = chip.P_CH[10];
                            prev = CH.extended;
                            CH.extended = (byte)((v >> 4) & 1);
                            if (prev != CH.extended)
                                update_channels(chip, CH);
                            //CH++;                   /* channel 11 */
                            CH = chip.P_CH[11];
                            prev = CH.extended;
                            CH.extended = (byte)((v >> 5) & 1);
                            if (prev != CH.extended)
                                update_channels(chip, CH);

                        }
                        return;

                    case 0x105: /* OPL3 extensions enable register */

                        chip.OPL3_mode = (byte)(v & 0x01); /* OPL3 mode when bit0=1 otherwise it is OPL2 mode */

                        /* following behaviour was tested on real YMF262,
                        switching OPL3/OPL2 modes on the fly:
                         - does not change the waveform previously selected (unless when ....)
                         - does not update CH.A, CH.B, CH.C and CH.D output selectors (registers c0-c8) (unless when ....)
                         - does not disable channels 9-17 on OPL3->OPL2 switch
                         - does not switch 4 operator channels back to 2 operator channels
                        */

                        return;

                    default:
                        //# ifdef _DEBUG
                        //                if (r < 0x120)
                        //                    logerror("YMF262: write to unknown register (set#2): %03x value=%02x\n", r, v);
                        //#endif
                        break;
                }

                ch_offset = 9;  /* register page #2 starts from channel 9 (counting from 0) */
            }

            /* adjust bus to 8 bits */
            r &= 0xff;
            v &= 0xff;


            switch (r & 0xe0)
            {
                case 0x00:  /* 00-1f:control */
                    switch (r & 0x1f)
                    {
                        case 0x01:  /* test register */
                            break;
                        case 0x02:  /* Timer 1 */
                            chip.T[0] = (256 - v) * 4;
                            break;
                        case 0x03:  /* Timer 2 */
                            chip.T[1] = (256 - v) * 16;
                            break;
                        case 0x04:  /* IRQ clear / mask and Timer enable */
                            if ((v & 0x80) != 0)
                            {   /* IRQ flags clear */
                                OPL3_STATUS_RESET(chip, 0x60);
                            }
                            else
                            {   /* set IRQ mask ,timer enable */
                                byte st1 = (byte)(v & 1);
                                byte st2 = (byte)((v >> 1) & 1);

                                /* IRQRST,T1MSK,t2MSK,x,x,x,ST2,ST1 */
                                OPL3_STATUS_RESET(chip, v & 0x60);
                                OPL3_STATUSMASK_SET(chip, (~v) & 0x60);

                                /* timer 2 */
                                if (chip.st[1] != st2)
                                {
                                    //attotime period = st2 ? attotime_mul(chip->TimerBase, chip->T[1]) : attotime_zero;
                                    chip.st[1] = st2;
                                    //if (chip->timer_handler) (chip->timer_handler)(chip->TimerParam,1,period);
                                }
                                /* timer 1 */
                                if (chip.st[0] != st1)
                                {
                                    //attotime period = st1 ? attotime_mul(chip->TimerBase, chip->T[0]) : attotime_zero;
                                    chip.st[0] = st1;
                                    //if (chip->timer_handler) (chip->timer_handler)(chip->TimerParam,0,period);
                                }
                            }
                            break;
                        case 0x08:  /* x,NTS,x,x, x,x,x,x */
                            chip.nts = (byte)v;
                            break;

                        default:
                            //# ifdef _DEBUG
                            //                    logerror("YMF262: write to unknown register: %02x value=%02x\n", r, v);
                            //#endif
                            break;
                    }
                    break;
                case 0x20:  /* am ON, vib ON, ksr, eg_type, mul */
                    slot = slot_array[r & 0x1f];
                    if (slot < 0) return;
                    set_mul(chip, (Int32)(slot + ch_offset * 2), v);
                    break;
                case 0x40:
                    slot = slot_array[r & 0x1f];
                    if (slot < 0) return;
                    set_ksl_tl(chip, (Int32)(slot + ch_offset * 2), v);
                    break;
                case 0x60:
                    slot = slot_array[r & 0x1f];
                    if (slot < 0) return;
                    set_ar_dr(chip, (Int32)(slot + ch_offset * 2), v);
                    break;
                case 0x80:
                    slot = slot_array[r & 0x1f];
                    if (slot < 0) return;
                    set_sl_rr(chip, (Int32)(slot + ch_offset * 2), v);
                    break;
                case 0xa0:
                    if (r == 0xbd)          /* am depth, vibrato depth, r,bd,sd,tom,tc,hh */
                    {
                        if (ch_offset != 0) /* 0xbd register is present in set #1 only */
                            return;

                        chip.lfo_am_depth = (byte)(v & 0x80);
                        chip.lfo_pm_depth_range = (byte)((v & 0x40) != 0 ? 8 : 0);

                        chip.rhythm = (byte)(v & 0x3f);

                        if ((chip.rhythm & 0x20) != 0)
                        {
                            /* BD key on/off */
                            if ((v & 0x10) != 0)
                            {
                                FM_KEYON(chip.P_CH[6].SLOT[SLOT1], 2);
                                FM_KEYON(chip.P_CH[6].SLOT[SLOT2], 2);
                            }
                            else
                            {
                                FM_KEYOFF(chip.P_CH[6].SLOT[SLOT1], ~(UInt32)2);
                                FM_KEYOFF(chip.P_CH[6].SLOT[SLOT2], ~(UInt32)2);
                            }
                            /* HH key on/off */
                            if ((v & 0x01) != 0) FM_KEYON(chip.P_CH[7].SLOT[SLOT1], 2);
                            else FM_KEYOFF(chip.P_CH[7].SLOT[SLOT1], ~(UInt32)2);
                            /* SD key on/off */
                            if ((v & 0x08) != 0) FM_KEYON(chip.P_CH[7].SLOT[SLOT2], 2);
                            else FM_KEYOFF(chip.P_CH[7].SLOT[SLOT2], ~(UInt32)2);
                            /* TOM key on/off */
                            if ((v & 0x04) != 0) FM_KEYON(chip.P_CH[8].SLOT[SLOT1], 2);
                            else FM_KEYOFF(chip.P_CH[8].SLOT[SLOT1], ~(UInt32)2);
                            /* TOP-CY key on/off */
                            if ((v & 0x02) != 0) FM_KEYON(chip.P_CH[8].SLOT[SLOT2], 2);
                            else FM_KEYOFF(chip.P_CH[8].SLOT[SLOT2], ~(UInt32)2);
                        }
                        else
                        {
                            /* BD key off */
                            FM_KEYOFF(chip.P_CH[6].SLOT[SLOT1], ~(UInt32)2);
                            FM_KEYOFF(chip.P_CH[6].SLOT[SLOT2], ~(UInt32)2);
                            /* HH key off */
                            FM_KEYOFF(chip.P_CH[7].SLOT[SLOT1], ~(UInt32)2);
                            /* SD key off */
                            FM_KEYOFF(chip.P_CH[7].SLOT[SLOT2], ~(UInt32)2);
                            /* TOM key off */
                            FM_KEYOFF(chip.P_CH[8].SLOT[SLOT1], ~(UInt32)2);
                            /* TOP-CY off */
                            FM_KEYOFF(chip.P_CH[8].SLOT[SLOT2], ~(UInt32)2);
                        }
                        return;
                    }

                    /* keyon,block,fnum */
                    if ((r & 0x0f) > 8) return;
                    CH = chip.P_CH[(r & 0x0f) + ch_offset];
                    CH_P3 = (((r & 0xf) + ch_offset + 3) >= 0 && ((r & 0xf) + ch_offset + 3) < chip.P_CH.Length) ? chip.P_CH[(r & 0xf) + ch_offset + 3] : null;
                    CH_M3 = (((r & 0xf) + ch_offset - 3) >= 0 && ((r & 0xf) + ch_offset - 3) < chip.P_CH.Length) ? chip.P_CH[(r & 0xf) + ch_offset - 3] : null;

                    if ((r & 0x10) == 0)
                    {   /* a0-a8 */
                        block_fnum = (Int32)((CH.block_fnum & 0x1f00) | (UInt32)v);
                    }
                    else
                    {   /* b0-b8 */
                        block_fnum = (Int32)((UInt32)((v & 0x1f) << 8) | (CH.block_fnum & 0xff));

                        if ((chip.OPL3_mode & 1) != 0)
                        {
                            Int32 chan_no = (Int32)((r & 0x0f) + ch_offset);

                            /* in OPL3 mode */
                            //DO THIS:
                            //if this is 1st channel forming up a 4-op channel
                            //ALSO keyon/off slots of 2nd channel forming up 4-op channel
                            //else normal 2 operator function keyon/off
                            //OR THIS:
                            //if this is 2nd channel forming up 4-op channel just do nothing
                            //else normal 2 operator function keyon/off
                            switch (chan_no)
                            {
                                case 0:
                                case 1:
                                case 2:
                                case 9:
                                case 10:
                                case 11:
                                    if (CH.extended != 0)
                                    {
                                        //if this is 1st channel forming up a 4-op channel
                                        //ALSO keyon/off slots of 2nd channel forming up 4-op channel
                                        if ((v & 0x20) != 0)
                                        {
                                            FM_KEYON(CH.SLOT[SLOT1], 1);
                                            FM_KEYON(CH.SLOT[SLOT2], 1);
                                            FM_KEYON(CH_P3.SLOT[SLOT1], 1);
                                            FM_KEYON(CH_P3.SLOT[SLOT2], 1);
                                        }
                                        else
                                        {
                                            FM_KEYOFF(CH.SLOT[SLOT1], ~(UInt32)1);
                                            FM_KEYOFF(CH.SLOT[SLOT2], ~(UInt32)1);
                                            FM_KEYOFF(CH_P3.SLOT[SLOT1], ~(UInt32)1);
                                            FM_KEYOFF(CH_P3.SLOT[SLOT2], ~(UInt32)1);
                                        }
                                    }
                                    else
                                    {
                                        //else normal 2 operator function keyon/off
                                        if ((v & 0x20) != 0)
                                        {
                                            FM_KEYON(CH.SLOT[SLOT1], 1);
                                            FM_KEYON(CH.SLOT[SLOT2], 1);
                                        }
                                        else
                                        {
                                            FM_KEYOFF(CH.SLOT[SLOT1], ~(UInt32)1);
                                            FM_KEYOFF(CH.SLOT[SLOT2], ~(UInt32)1);
                                        }
                                    }
                                    break;

                                case 3:
                                case 4:
                                case 5:
                                case 12:
                                case 13:
                                case 14:
                                    if (CH_M3.extended != 0)
                                    {
                                        //if this is 2nd channel forming up 4-op channel just do nothing
                                    }
                                    else
                                    {
                                        //else normal 2 operator function keyon/off
                                        if ((v & 0x20) != 0)
                                        {
                                            FM_KEYON(CH.SLOT[SLOT1], 1);
                                            FM_KEYON(CH.SLOT[SLOT2], 1);
                                        }
                                        else
                                        {
                                            FM_KEYOFF(CH.SLOT[SLOT1], ~(UInt32)1);
                                            FM_KEYOFF(CH.SLOT[SLOT2], ~(UInt32)1);
                                        }
                                    }
                                    break;

                                default:
                                    if ((v & 0x20) != 0)
                                    {
                                        FM_KEYON(CH.SLOT[SLOT1], 1);
                                        FM_KEYON(CH.SLOT[SLOT2], 1);
                                    }
                                    else
                                    {
                                        FM_KEYOFF(CH.SLOT[SLOT1], ~(UInt32)1);
                                        FM_KEYOFF(CH.SLOT[SLOT2], ~(UInt32)1);
                                    }
                                    break;
                            }
                        }
                        else
                        {
                            if ((v & 0x20) != 0)
                            {
                                FM_KEYON(CH.SLOT[SLOT1], 1);
                                FM_KEYON(CH.SLOT[SLOT2], 1);
                            }
                            else
                            {
                                FM_KEYOFF(CH.SLOT[SLOT1], ~(UInt32)1);
                                FM_KEYOFF(CH.SLOT[SLOT2], ~(UInt32)1);
                            }
                        }
                    }
                    /* update */
                    if (CH.block_fnum != block_fnum)
                    {
                        byte block = (byte)(block_fnum >> 10);

                        CH.block_fnum = (UInt32)block_fnum;

                        CH.ksl_base = ksl_tab[block_fnum >> 6];
                        CH.fc = chip.fn_tab[block_fnum & 0x03ff] >> (7 - block);

                        /* BLK 2,1,0 bits -> bits 3,2,1 of kcode */
                        CH.kcode = (byte)((CH.block_fnum & 0x1c00) >> 9);

                        /* the info below is actually opposite to what is stated in the Manuals (verifed on real YMF262) */
                        /* if notesel == 0 -> lsb of kcode is bit 10 (MSB) of fnum  */
                        /* if notesel == 1 -> lsb of kcode is bit 9 (MSB-1) of fnum */
                        if ((chip.nts & 0x40) != 0)
                            CH.kcode |= (byte)((CH.block_fnum & 0x100) >> 8); /* notesel == 1 */
                        else
                            CH.kcode |= (byte)((CH.block_fnum & 0x200) >> 9); /* notesel == 0 */

                        if ((chip.OPL3_mode & 1) != 0)
                        {
                            Int32 chan_no = (Int32)((r & 0x0f) + ch_offset);
                            /* in OPL3 mode */
                            //DO THIS:
                            //if this is 1st channel forming up a 4-op channel
                            //ALSO update slots of 2nd channel forming up 4-op channel
                            //else normal 2 operator function keyon/off
                            //OR THIS:
                            //if this is 2nd channel forming up 4-op channel just do nothing
                            //else normal 2 operator function keyon/off
                            switch (chan_no)
                            {
                                case 0:
                                case 1:
                                case 2:
                                case 9:
                                case 10:
                                case 11:
                                    if (CH.extended != 0)
                                    {
                                        //if this is 1st channel forming up a 4-op channel
                                        //ALSO update slots of 2nd channel forming up 4-op channel

                                        /* refresh Total Level in FOUR SLOTs of this channel and channel+3 using data from THIS channel */
                                        CH.SLOT[SLOT1].TLL = (Int32)(CH.SLOT[SLOT1].TL + (CH.ksl_base >> CH.SLOT[SLOT1].ksl));
                                        CH.SLOT[SLOT2].TLL = (Int32)(CH.SLOT[SLOT2].TL + (CH.ksl_base >> CH.SLOT[SLOT2].ksl));
                                        CH_P3.SLOT[SLOT1].TLL = (Int32)(CH_P3.SLOT[SLOT1].TL + (CH.ksl_base >> CH_P3.SLOT[SLOT1].ksl));
                                        CH_P3.SLOT[SLOT2].TLL = (Int32)(CH_P3.SLOT[SLOT2].TL + (CH.ksl_base >> CH_P3.SLOT[SLOT2].ksl));

                                        /* refresh frequency counter in FOUR SLOTs of this channel and channel+3 using data from THIS channel */
                                        CALC_FCSLOT(CH, CH.SLOT[SLOT1]);
                                        CALC_FCSLOT(CH, CH.SLOT[SLOT2]);
                                        CALC_FCSLOT(CH, CH_P3.SLOT[SLOT1]);
                                        CALC_FCSLOT(CH, CH_P3.SLOT[SLOT2]);
                                    }
                                    else
                                    {
                                        //else normal 2 operator function
                                        /* refresh Total Level in both SLOTs of this channel */
                                        CH.SLOT[SLOT1].TLL = (Int32)(CH.SLOT[SLOT1].TL + (CH.ksl_base >> CH.SLOT[SLOT1].ksl));
                                        CH.SLOT[SLOT2].TLL = (Int32)(CH.SLOT[SLOT2].TL + (CH.ksl_base >> CH.SLOT[SLOT2].ksl));

                                        /* refresh frequency counter in both SLOTs of this channel */
                                        CALC_FCSLOT(CH, CH.SLOT[SLOT1]);
                                        CALC_FCSLOT(CH, CH.SLOT[SLOT2]);
                                    }
                                    break;

                                case 3:
                                case 4:
                                case 5:
                                case 12:
                                case 13:
                                case 14:
                                    if (CH_M3.extended != 0)
                                    {
                                        //if this is 2nd channel forming up 4-op channel just do nothing
                                    }
                                    else
                                    {
                                        //else normal 2 operator function
                                        /* refresh Total Level in both SLOTs of this channel */
                                        CH.SLOT[SLOT1].TLL = (Int32)(CH.SLOT[SLOT1].TL + (CH.ksl_base >> CH.SLOT[SLOT1].ksl));
                                        CH.SLOT[SLOT2].TLL = (Int32)(CH.SLOT[SLOT2].TL + (CH.ksl_base >> CH.SLOT[SLOT2].ksl));

                                        /* refresh frequency counter in both SLOTs of this channel */
                                        CALC_FCSLOT(CH, CH.SLOT[SLOT1]);
                                        CALC_FCSLOT(CH, CH.SLOT[SLOT2]);
                                    }
                                    break;

                                default:
                                    /* refresh Total Level in both SLOTs of this channel */
                                    CH.SLOT[SLOT1].TLL = (Int32)(CH.SLOT[SLOT1].TL + (CH.ksl_base >> CH.SLOT[SLOT1].ksl));
                                    CH.SLOT[SLOT2].TLL = (Int32)(CH.SLOT[SLOT2].TL + (CH.ksl_base >> CH.SLOT[SLOT2].ksl));

                                    /* refresh frequency counter in both SLOTs of this channel */
                                    CALC_FCSLOT(CH, CH.SLOT[SLOT1]);
                                    CALC_FCSLOT(CH, CH.SLOT[SLOT2]);
                                    break;
                            }
                        }
                        else
                        {
                            /* in OPL2 mode */

                            /* refresh Total Level in both SLOTs of this channel */
                            CH.SLOT[SLOT1].TLL = (Int32)(CH.SLOT[SLOT1].TL + (CH.ksl_base >> CH.SLOT[SLOT1].ksl));
                            CH.SLOT[SLOT2].TLL = (Int32)(CH.SLOT[SLOT2].TL + (CH.ksl_base >> CH.SLOT[SLOT2].ksl));

                            /* refresh frequency counter in both SLOTs of this channel */
                            CALC_FCSLOT(CH, CH.SLOT[SLOT1]);
                            CALC_FCSLOT(CH, CH.SLOT[SLOT2]);
                        }
                    }
                    break;

                case 0xc0:
                    /* CH.D, CH.C, CH.B, CH.A, FB(3bits), C */
                    if ((r & 0xf) > 8) return;

                    CH = chip.P_CH[(r & 0xf) + ch_offset];
                    CH_P3 = (((r & 0xf) + ch_offset + 3) >= 0 && ((r & 0xf) + ch_offset + 3) < chip.P_CH.Length) ? chip.P_CH[(r & 0xf) + ch_offset + 3] : null;
                    CH_M3 = (((r & 0xf) + ch_offset - 3) >= 0 && ((r & 0xf) + ch_offset - 3) < chip.P_CH.Length) ? chip.P_CH[(r & 0xf) + ch_offset - 3] : null;

                    if ((chip.OPL3_mode & 1) != 0)
                    {
                        Int32 _base = (Int32)(((r & 0xf) + ch_offset) * 4);

                        /* OPL3 mode */
                        chip.pan[_base] = (UInt32)((v & 0x10) != 0 ? ~0 : 0);  /* ch.A */
                        chip.pan[_base + 1] = (UInt32)((v & 0x20) != 0 ? ~0 : 0);  /* ch.B */
                        chip.pan[_base + 2] = (UInt32)((v & 0x40) != 0 ? ~0 : 0);  /* ch.C */
                        chip.pan[_base + 3] = (UInt32)((v & 0x80) != 0 ? ~0 : 0);  /* ch.D */
                    }
                    else
                    {
                        Int32 _base = (Int32)(((r & 0xf) + ch_offset) * 4);

                        /* OPL2 mode - always enabled */
                        chip.pan[_base] = ~(UInt32)0;       /* ch.A */
                        chip.pan[_base + 1] = ~(UInt32)0;       /* ch.B */
                        chip.pan[_base + 2] = ~(UInt32)0;       /* ch.C */
                        chip.pan[_base + 3] = ~(UInt32)0;       /* ch.D */
                    }

                    chip.pan_ctrl_value[(r & 0xf) + ch_offset] = (UInt32)v;    /* store control value for OPL3/OPL2 mode switching on the fly */

                    CH.SLOT[SLOT1].FB = (byte)(((v >> 1) & 7) != 0 ? ((v >> 1) & 7) + 7 : 0);
                    CH.SLOT[SLOT1].CON = (byte)(v & 1);

                    if ((chip.OPL3_mode & 1) != 0)
                    {
                        Int32 chan_no = (Int32)((r & 0x0f) + ch_offset);

                        switch (chan_no)
                        {
                            case 0:
                            case 1:
                            case 2:
                            case 9:
                            case 10:
                            case 11:
                                if (CH.extended != 0)
                                {
                                    byte conn = (byte)((CH.SLOT[SLOT1].CON << 1) | (CH_P3.SLOT[SLOT1].CON << 0));
                                    switch (conn)
                                    {
                                        case 0:
                                            /* 1 -> 2 -> 3 -> 4 - out */

                                            CH.SLOT[SLOT1].connect = chip.phase_modulation;
                                            CH.SLOT[SLOT2].connect = chip.phase_modulation2;
                                            CH_P3.SLOT[SLOT1].connect = chip.phase_modulation;
                                            CH_P3.SLOT[SLOT2].connect = chanout[chan_no + 3];
                                            break;
                                        case 1:
                                            /* 1 -> 2 -\
                                               3 -> 4 -+- out */

                                            CH.SLOT[SLOT1].connect = chip.phase_modulation;
                                            CH.SLOT[SLOT2].connect = chanout[chan_no];
                                            CH_P3.SLOT[SLOT1].connect = chip.phase_modulation;
                                            CH_P3.SLOT[SLOT2].connect = chanout[chan_no + 3];
                                            break;
                                        case 2:
                                            /* 1 -----------\
                                               2 -> 3 -> 4 -+- out */

                                            CH.SLOT[SLOT1].connect = chanout[chan_no];
                                            CH.SLOT[SLOT2].connect = chip.phase_modulation2;
                                            CH_P3.SLOT[SLOT1].connect = chip.phase_modulation;
                                            CH_P3.SLOT[SLOT2].connect = chanout[chan_no + 3];
                                            break;
                                        case 3:
                                            /* 1 ------\
                                               2 -> 3 -+- out
                                               4 ------/     */
                                            CH.SLOT[SLOT1].connect = chanout[chan_no];
                                            CH.SLOT[SLOT2].connect = chip.phase_modulation2;
                                            CH_P3.SLOT[SLOT1].connect = chanout[chan_no + 3];
                                            CH_P3.SLOT[SLOT2].connect = chanout[chan_no + 3];
                                            break;
                                    }
                                }
                                else
                                {
                                    /* 2 operators mode */
                                    CH.SLOT[SLOT1].connect = CH.SLOT[SLOT1].CON != 0 ? chanout[(r & 0xf) + ch_offset] : chip.phase_modulation;
                                    CH.SLOT[SLOT2].connect = chanout[(r & 0xf) + ch_offset];
                                }
                                break;

                            case 3:
                            case 4:
                            case 5:
                            case 12:
                            case 13:
                            case 14:
                                if (CH_M3.extended != 0)
                                {
                                    byte conn = (byte)((CH_M3.SLOT[SLOT1].CON << 1) | (CH.SLOT[SLOT1].CON << 0));
                                    switch (conn)
                                    {
                                        case 0:
                                            /* 1 -> 2 -> 3 -> 4 - out */

                                            CH_M3.SLOT[SLOT1].connect = chip.phase_modulation;
                                            CH_M3.SLOT[SLOT2].connect = chip.phase_modulation2;
                                            CH.SLOT[SLOT1].connect = chip.phase_modulation;
                                            CH.SLOT[SLOT2].connect = chanout[chan_no];
                                            break;
                                        case 1:
                                            /* 1 -> 2 -\
                                               3 -> 4 -+- out */

                                            CH_M3.SLOT[SLOT1].connect = chip.phase_modulation;
                                            CH_M3.SLOT[SLOT2].connect = chanout[chan_no - 3];
                                            CH.SLOT[SLOT1].connect = chip.phase_modulation;
                                            CH.SLOT[SLOT2].connect = chanout[chan_no];
                                            break;
                                        case 2:
                                            /* 1 -----------\
                                               2 -> 3 -> 4 -+- out */

                                            CH_M3.SLOT[SLOT1].connect = chanout[chan_no - 3];
                                            CH_M3.SLOT[SLOT2].connect = chip.phase_modulation2;
                                            CH.SLOT[SLOT1].connect = chip.phase_modulation;
                                            CH.SLOT[SLOT2].connect = chanout[chan_no];
                                            break;
                                        case 3:
                                            /* 1 ------\
                                               2 -> 3 -+- out
                                               4 ------/     */
                                            CH_M3.SLOT[SLOT1].connect = chanout[chan_no - 3];
                                            CH_M3.SLOT[SLOT2].connect = chip.phase_modulation2;
                                            CH.SLOT[SLOT1].connect = chanout[chan_no];
                                            CH.SLOT[SLOT2].connect = chanout[chan_no];
                                            break;
                                    }
                                }
                                else
                                {
                                    /* 2 operators mode */
                                    CH.SLOT[SLOT1].connect = CH.SLOT[SLOT1].CON != 0 ? chanout[(r & 0xf) + ch_offset] : chip.phase_modulation;
                                    CH.SLOT[SLOT2].connect = chanout[(r & 0xf) + ch_offset];
                                }
                                break;

                            default:
                                /* 2 operators mode */
                                CH.SLOT[SLOT1].connect = CH.SLOT[SLOT1].CON != 0 ? chanout[(r & 0xf) + ch_offset] : chip.phase_modulation;
                                CH.SLOT[SLOT2].connect = chanout[(r & 0xf) + ch_offset];
                                break;
                        }
                    }
                    else
                    {
                        /* OPL2 mode - always 2 operators mode */
                        CH.SLOT[SLOT1].connect = CH.SLOT[SLOT1].CON != 0 ? chanout[(r & 0xf) + ch_offset] : chip.phase_modulation;
                        CH.SLOT[SLOT2].connect = chanout[(r & 0xf) + ch_offset];
                    }
                    break;

                case 0xe0: /* waveform select */
                    slot = slot_array[r & 0x1f];
                    if (slot < 0) return;

                    slot += (Int32)ch_offset * 2;

                    CH = chip.P_CH[slot / 2];


                    /* store 3-bit value written regardless of current OPL2 or OPL3 mode... (verified on real YMF262) */
                    v &= 7;
                    CH.SLOT[slot & 1].waveform_number = (byte)v;

                    /* ... but select only waveforms 0-3 in OPL2 mode */
                    if ((chip.OPL3_mode & 1) == 0)
                    {
                        v &= 3; /* we're in OPL2 mode */
                    }
                    CH.SLOT[slot & 1].wavetable = (UInt32)(v * SIN_LEN);
                    break;
            }
        }

        /*static TIMER_CALLBACK( cymfile_callback )
        {
            if (cymfile)
            {
                fputc( (unsigned char)0, cymfile );
            }
        }*/

        /* lock/unlock for common table */
        private Int32 OPL3_LockTable()
        {
            num_lock++;
            if (num_lock > 1) return 0;

            /* first time */

            if (init_tables() == 0)
            {
                num_lock--;
                return -1;
            }

            /*if (LOG_CYM_FILE)
            {
                cymfile = fopen("ymf262_.cym","wb");
                if (cymfile)
                    timer_pulse ( device->machine, ATTOTIME_IN_HZ(110), NULL, 0, cymfile_callback); //110 Hz pulse timer
                else
                    logerror("Could not create ymf262_.cym file\n");
            }*/

            return 0;
        }

        private void OPL3_UnLockTable()
        {
            if (num_lock != 0) num_lock--;
            if (num_lock != 0) return;

            /* last time */

            OPLCloseTable();

            /*if (LOG_CYM_FILE)
                fclose (cymfile);
            cymfile = NULL;*/
        }

        private void OPL3ResetChip(OPL3 chip)
        {
            Int32 c, s;

            chip.eg_timer = 0;
            chip.eg_cnt = 0;

            chip.noise_rng = 1;    /* noise shift register */
            chip.nts = 0;  /* note split */
            OPL3_STATUS_RESET(chip, 0x60);

            /* reset with register write */
            OPL3WriteReg(chip, 0x01, 0); /* test register */
            OPL3WriteReg(chip, 0x02, 0); /* Timer1 */
            OPL3WriteReg(chip, 0x03, 0); /* Timer2 */
            OPL3WriteReg(chip, 0x04, 0); /* IRQ mask clear */


            //FIX IT  registers 101, 104 and 105


            //FIX IT (dont change CH.D, CH.C, CH.B and CH.A in C0-C8 registers)
            for (c = 0xff; c >= 0x20; c--)
                OPL3WriteReg(chip, c, 0);
            //FIX IT (dont change CH.D, CH.C, CH.B and CH.A in C0-C8 registers)
            for (c = 0x1ff; c >= 0x120; c--)
                OPL3WriteReg(chip, c, 0);



            /* reset operator parameters */
            for (c = 0; c < 9 * 2; c++)
            {
                OPL3_CH CH = chip.P_CH[c];
                for (s = 0; s < 2; s++)
                {
                    CH.SLOT[s].state = (byte)EG_OFF;
                    CH.SLOT[s].volume = MAX_ATT_INDEX;
                }
            }
        }

        /* Create one of virtual YMF262 */
        /* 'clock' is chip clock in Hz  */
        /* 'rate'  is sampling rate  */
        private OPL3 OPL3Create(Int32 clock, Int32 rate, Int32 type)
        {
            OPL3 chip;

            if (OPL3_LockTable() == -1) return null;

            /* allocate memory block */
            chip = new OPL3();// (OPL3*)malloc(sizeof(OPL3));

            if (chip == null)
                return null;

            /* clear */
            //memset(chip, 0, sizeof(OPL3));

            chip.type = (byte)type;
            chip.clock = clock;
            chip.rate = rate;

            /* init global tables */
            OPL3_initalize(chip);

            /* reset chip */
            OPL3ResetChip(chip);
            return chip;
        }

        /* Destroy one of virtual YMF262 */
        private void OPL3Destroy(OPL3 chip)
        {
            OPL3_UnLockTable();
            //free(chip);
        }


        /* Optional handlers */

        private void OPL3SetTimerHandler(OPL3 chip, OPL3_TIMERHANDLER timer_handler, object param)
        {
            chip.timer_handler = timer_handler;
            chip.TimerParam = param;
        }

        private void OPL3SetIRQHandler(OPL3 chip, OPL3_IRQHANDLER IRQHandler, ymf262_state param)
        {
            chip.IRQHandler = IRQHandler;
            chip.IRQParam = param;
        }

        private void OPL3SetUpdateHandler(OPL3 chip, OPL3_UPDATEHANDLER UpdateHandler, ymf262_state param)
        {
            chip.UpdateHandler = UpdateHandler;
            chip.UpdateParam = param;
        }

        /* YMF262 I/O interface */
        private Int32 OPL3Write(OPL3 chip, Int32 a, Int32 v)
        {
            /* data bus is 8 bits */
            v &= 0xff;

            switch (a & 3)
            {
                case 0: /* address port 0 (register set #1) */
                    chip.address = (UInt32)v;
                    break;

                case 1: /* data port - ignore A1 */
                case 3: /* data port - ignore A1 */
                    if (chip.UpdateHandler != null) chip.UpdateHandler(chip.UpdateParam/*,0*/);
                    OPL3WriteReg(chip, (Int32)chip.address, v);
                    break;

                case 2: /* address port 1 (register set #2) */

                    /* verified on real YMF262:
                     in OPL3 mode:
                       address line A1 is stored during *address* write and ignored during *data* write.

                     in OPL2 mode:
                       register set#2 writes go to register set#1 (ignoring A1)
                       verified on registers from set#2: 0x01, 0x04, 0x20-0xef
                       The only exception is register 0x05.
                    */
                    if ((chip.OPL3_mode & 1) != 0)
                    {
                        /* OPL3 mode */
                        chip.address = (UInt32)(v | 0x100);
                    }
                    else
                    {
                        /* in OPL2 mode the only accessible in set #2 is register 0x05 */
                        if (v == 5)
                            chip.address = (UInt32)(v | 0x100);
                        else
                            chip.address = (UInt32)v;  /* verified range: 0x01, 0x04, 0x20-0xef(set #2 becomes set #1 in opl2 mode) */
                    }
                    break;
            }

            return chip.status >> 7;
        }

        private byte OPL3Read(OPL3 chip, Int32 a)
        {
            if (a == 0)
            {
                /* status port */
                return chip.status;
            }

            return 0x00;    /* verified on real YMF262 */
        }



        private int OPL3TimerOver(OPL3 chip, Int32 c)
        {
            if (c != 0)
            {   /* Timer B */
                OPL3_STATUS_SET(chip, 0x20);
            }
            else
            {   /* Timer A */
                OPL3_STATUS_SET(chip, 0x40);
            }
            /* reload timer */
            //if (chip->timer_handler) (chip->timer_handler)(chip->TimerParam,c,attotime_mul(chip->TimerBase, chip->T[c]));
            return chip.status >> 7;
        }




        public OPL3 ymf262_init(Int32 clock, Int32 rate)
        {
            return OPL3Create(clock, rate, OPL3_TYPE_YMF262);
        }

        private void ymf262_shutdown(OPL3 chip)
        {
            OPL3Destroy((OPL3)chip);
        }

        private void ymf262_reset_chip(OPL3 chip)
        {
            OPL3ResetChip((OPL3)chip);
        }

        private Int32 ymf262_write(OPL3 chip, Int32 a, Int32 v)
        {
            return OPL3Write((OPL3)chip, a, v);
        }

        private byte ymf262_read(OPL3 chip, Int32 a)
        {
            /* Note on status register: */

            /* YM3526(OPL) and YM3812(OPL2) return bit2 and bit1 in HIGH state */

            /* YMF262(OPL3) always returns bit2 and bit1 in LOW state */
            /* which can be used to identify the chip */

            /* YMF278(OPL4) returns bit2 in LOW and bit1 in HIGH state ??? info from manual - not verified */

            return OPL3Read((OPL3)chip, a);
        }

        private Int32 ymf262_timer_over(OPL3 chip, Int32 c)
        {
            return OPL3TimerOver((OPL3)chip, c);
        }

        private void ymf262_set_timer_handler(OPL3 chip, OPL3_TIMERHANDLER timer_handler, object param)
        {
            OPL3SetTimerHandler((OPL3)chip, timer_handler, param);
        }

        private void ymf262_set_irq_handler(OPL3 chip, OPL3_IRQHANDLER IRQHandler, ymf262_state param)
        {
            OPL3SetIRQHandler((OPL3)chip, IRQHandler, param);
        }

        private void ymf262_set_update_handler(OPL3 chip, OPL3_UPDATEHANDLER UpdateHandler, ymf262_state param)
        {
            OPL3SetUpdateHandler((OPL3)chip, UpdateHandler, param);
        }

        private void ymf262_set_mutemask(OPL3 chip, UInt32 MuteMask)
        {
            OPL3 opl3 = (OPL3)chip;
            byte CurChn;

            for (CurChn = 0; CurChn < 18; CurChn++)
                opl3.P_CH[CurChn].Muted = (byte)((MuteMask >> CurChn) & 0x01);
            for (CurChn = 0; CurChn < 5; CurChn++)
                opl3.MuteSpc[CurChn] = (byte)((MuteMask >> (CurChn + 18)) & 0x01);

            return;
        }


        /*
        ** Generate samples for one of the YMF262's
        **
        ** 'which' is the virtual YMF262 number
        ** '**buffers' is table of 4 pointers to the buffers: CH.A, CH.B, CH.C and CH.D
        ** 'length' is the number of samples that should be generated
        */
        public void ymf262_update_one(OPL3 _chip, Int32[][] buffers, Int32 length)
        {
            OPL3 chip = (OPL3)_chip;
            byte rhythm = (byte)(chip.rhythm & 0x20);

            //OPL3SAMPLE ch_a = buffers[0];
            //OPL3SAMPLE ch_b = buffers[1];
            Int32[] ch_a = buffers[0];
            Int32[] ch_b = buffers[1];
            //OPL3SAMPLE	*ch_c = buffers[2];
            //OPL3SAMPLE	*ch_d = buffers[3];

            Int32 i;
            //Int32 chn;

            for (i = 0; i < length; i++)
            {
                Int32 a, b, c, d;


                advance_lfo(chip);

                /* clear channel outputs */
                for (int j = 0; j < 18; j++) chip.chanout[j] = 0;
                //memset(chip->chanout, 0, sizeof(signed int) * 18);

                //#if 1
                /* register set #1 */
                chan_calc(chip, chip.P_CH[0]);          /* extended 4op ch#0 part 1 or 2op ch#0 */
                if (chip.P_CH[0].extended != 0)
                    chan_calc_ext(chip, chip.P_CH[3]);  /* extended 4op ch#0 part 2 */
                else
                    chan_calc(chip, chip.P_CH[3]);      /* standard 2op ch#3 */


                chan_calc(chip, chip.P_CH[1]);          /* extended 4op ch#1 part 1 or 2op ch#1 */
                if (chip.P_CH[1].extended != 0)
                    chan_calc_ext(chip, chip.P_CH[4]);  /* extended 4op ch#1 part 2 */
                else
                    chan_calc(chip, chip.P_CH[4]);      /* standard 2op ch#4 */


                chan_calc(chip, chip.P_CH[2]);          /* extended 4op ch#2 part 1 or 2op ch#2 */
                if (chip.P_CH[2].extended != 0)
                    chan_calc_ext(chip, chip.P_CH[5]);  /* extended 4op ch#2 part 2 */
                else
                    chan_calc(chip, chip.P_CH[5]);      /* standard 2op ch#5 */


                if (rhythm == 0)
                {
                    chan_calc(chip, chip.P_CH[6]);
                    chan_calc(chip, chip.P_CH[7]);
                    chan_calc(chip, chip.P_CH[8]);
                }
                else        /* Rhythm part */
                {
                    chan_calc_rhythm(chip, chip.P_CH, 0, (chip.noise_rng >> 0) & 1);
                }

                /* register set #2 */
                chan_calc(chip, chip.P_CH[9]);
                if (chip.P_CH[9].extended != 0)
                    chan_calc_ext(chip, chip.P_CH[12]);
                else
                    chan_calc(chip, chip.P_CH[12]);


                chan_calc(chip, chip.P_CH[10]);
                if (chip.P_CH[10].extended != 0)
                    chan_calc_ext(chip, chip.P_CH[13]);
                else
                    chan_calc(chip, chip.P_CH[13]);


                chan_calc(chip, chip.P_CH[11]);
                if (chip.P_CH[11].extended != 0)
                    chan_calc_ext(chip, chip.P_CH[14]);
                else
                    chan_calc(chip, chip.P_CH[14]);


                /* channels 15,16,17 are fixed 2-operator channels only */
                chan_calc(chip, chip.P_CH[15]);
                chan_calc(chip, chip.P_CH[16]);
                chan_calc(chip, chip.P_CH[17]);
                //#endif

                /* accumulator register set #1 */
                a = (Int32)(chip.chanout[0] & chip.pan[0]);
                b = (Int32)(chip.chanout[0] & chip.pan[1]);
                c = (Int32)(chip.chanout[0] & chip.pan[2]);
                d = (Int32)(chip.chanout[0] & chip.pan[3]);
                //#if 1
                a += (Int32)(chip.chanout[1] & chip.pan[4]);
                b += (Int32)(chip.chanout[1] & chip.pan[5]);
                c += (Int32)(chip.chanout[1] & chip.pan[6]);
                d += (Int32)(chip.chanout[1] & chip.pan[7]);
                a += (Int32)(chip.chanout[2] & chip.pan[8]);
                b += (Int32)(chip.chanout[2] & chip.pan[9]);
                c += (Int32)(chip.chanout[2] & chip.pan[10]);
                d += (Int32)(chip.chanout[2] & chip.pan[11]);

                a += (Int32)(chip.chanout[3] & chip.pan[12]);
                b += (Int32)(chip.chanout[3] & chip.pan[13]);
                c += (Int32)(chip.chanout[3] & chip.pan[14]);
                d += (Int32)(chip.chanout[3] & chip.pan[15]);
                a += (Int32)(chip.chanout[4] & chip.pan[16]);
                b += (Int32)(chip.chanout[4] & chip.pan[17]);
                c += (Int32)(chip.chanout[4] & chip.pan[18]);
                d += (Int32)(chip.chanout[4] & chip.pan[19]);
                a += (Int32)(chip.chanout[5] & chip.pan[20]);
                b += (Int32)(chip.chanout[5] & chip.pan[21]);
                c += (Int32)(chip.chanout[5] & chip.pan[22]);
                d += (Int32)(chip.chanout[5] & chip.pan[23]);

                a += (Int32)(chip.chanout[6] & chip.pan[24]);
                b += (Int32)(chip.chanout[6] & chip.pan[25]);
                c += (Int32)(chip.chanout[6] & chip.pan[26]);
                d += (Int32)(chip.chanout[6] & chip.pan[27]);
                a += (Int32)(chip.chanout[7] & chip.pan[28]);
                b += (Int32)(chip.chanout[7] & chip.pan[29]);
                c += (Int32)(chip.chanout[7] & chip.pan[30]);
                d += (Int32)(chip.chanout[7] & chip.pan[31]);
                a += (Int32)(chip.chanout[8] & chip.pan[32]);
                b += (Int32)(chip.chanout[8] & chip.pan[33]);
                c += (Int32)(chip.chanout[8] & chip.pan[34]);
                d += (Int32)(chip.chanout[8] & chip.pan[35]);

                /* accumulator register set #2 */
                a += (Int32)(chip.chanout[9] & chip.pan[36]);
                b += (Int32)(chip.chanout[9] & chip.pan[37]);
                c += (Int32)(chip.chanout[9] & chip.pan[38]);
                d += (Int32)(chip.chanout[9] & chip.pan[39]);
                a += (Int32)(chip.chanout[10] & chip.pan[40]);
                b += (Int32)(chip.chanout[10] & chip.pan[41]);
                c += (Int32)(chip.chanout[10] & chip.pan[42]);
                d += (Int32)(chip.chanout[10] & chip.pan[43]);
                a += (Int32)(chip.chanout[11] & chip.pan[44]);
                b += (Int32)(chip.chanout[11] & chip.pan[45]);
                c += (Int32)(chip.chanout[11] & chip.pan[46]);
                d += (Int32)(chip.chanout[11] & chip.pan[47]);

                a += (Int32)(chip.chanout[12] & chip.pan[48]);
                b += (Int32)(chip.chanout[12] & chip.pan[49]);
                c += (Int32)(chip.chanout[12] & chip.pan[50]);
                d += (Int32)(chip.chanout[12] & chip.pan[51]);
                a += (Int32)(chip.chanout[13] & chip.pan[52]);
                b += (Int32)(chip.chanout[13] & chip.pan[53]);
                c += (Int32)(chip.chanout[13] & chip.pan[54]);
                d += (Int32)(chip.chanout[13] & chip.pan[55]);
                a += (Int32)(chip.chanout[14] & chip.pan[56]);
                b += (Int32)(chip.chanout[14] & chip.pan[57]);
                c += (Int32)(chip.chanout[14] & chip.pan[58]);
                d += (Int32)(chip.chanout[14] & chip.pan[59]);

                a += (Int32)(chip.chanout[15] & chip.pan[60]);
                b += (Int32)(chip.chanout[15] & chip.pan[61]);
                c += (Int32)(chip.chanout[15] & chip.pan[62]);
                d += (Int32)(chip.chanout[15] & chip.pan[63]);
                a += (Int32)(chip.chanout[16] & chip.pan[64]);
                b += (Int32)(chip.chanout[16] & chip.pan[65]);
                c += (Int32)(chip.chanout[16] & chip.pan[66]);
                d += (Int32)(chip.chanout[16] & chip.pan[67]);
                a += (Int32)(chip.chanout[17] & chip.pan[68]);
                b += (Int32)(chip.chanout[17] & chip.pan[69]);
                c += (Int32)(chip.chanout[17] & chip.pan[70]);
                d += (Int32)(chip.chanout[17] & chip.pan[71]);
                //#endif
                a >>= FINAL_SH;
                b >>= FINAL_SH;
                c >>= FINAL_SH;
                d >>= FINAL_SH;

                /* limit check */
                //a = limit( a , MAXOUT, MINOUT );
                //b = limit( b , MAXOUT, MINOUT );
                //c = limit( c , MAXOUT, MINOUT );
                //d = limit( d , MAXOUT, MINOUT );

                //# ifdef SAVE_SAMPLE
                //    if (which == 0)
                //    {
                //        SAVE_ALL_CHANNELS

                //        }
                //#endif

                /* store to sound buffer */
                ch_a[i] = a + c;
                ch_b[i] = b + d;
                //ch_c[i] = c;
                //ch_d[i] = d;

                advance(chip);
            }

        }






        //************************************************************************************************************
        // opl.h
        //************************************************************************************************************

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


        //#define fltype double

        /*
            define Bits, Bitu, Bit32s, Bit32u, Bit16s, Bit16u, Bit8s, Bit8u here
        */
        /*
        #include <stdint.h>
        typedef uintptr_t	Bitu;
        typedef intptr_t	Bits;
        typedef uint32_t	Bit32u;
        typedef int32_t		Bit32s;
        typedef uint16_t	Bit16u;
        typedef int16_t		Bit16s;
        typedef uint8_t		Bit8u;
        typedef int8_t		Bit8s;
        */

        //typedef UINT32      Bitu;
        //typedef INT32       Bits;
        //typedef UINT32      Bit32u;
        //typedef INT32       Bit32s;
        //typedef UINT16      Bit16u;
        //typedef INT16       Bit16s;
        //typedef UINT8       Bit8u;
        //typedef INT8        Bit8s;

        /*
            define attribution that inlines/forces inlining of a function (optional)
        */
        //#define OPL_INLINE INLINE


        //#undef NUM_CHANNELS
        //#if defined(OPLTYPE_IS_OPL3)
        //#define NUM_CHANNELS	18
        private const Int32 NUM_CHANNELS = 18;
        //#else
        //#define NUM_CHANNELS	9
        //#endif

        //#define MAXOPERATORS	(NUM_CHANNELS*2)
        private const Int32 MAXOPERATORS = (NUM_CHANNELS * 2);


        //#define FL05	((fltype)0.5)
        private const double FL05 = ((double)0.5);
        //#define FL2		((fltype)2.0)
        private const double FL2 = ((double)2.0);
        //#define PI		((fltype)3.1415926535897932384626433832795)
        private const double PI = ((double)3.1415926535897932384626433832795);


        private const Int32 FIXEDPT = 0x10000;      // fixed-point calculations using 16+16
        private const Int32 FIXEDPT_LFO = 0x1000000;    // fixed-point calculations using 8+24

        private const Int32 WAVEPREC = 1024;        // waveform precision (10 bits)

        //#define INTFREQU		((fltype)(14318180.0 / 288.0))		// clocking of the chip
        //#if defined(OPLTYPE_IS_OPL3)
        private double INTFREQU(OPL_DATA OPL) { return ((double)(OPL.chip_clock / 288.0)); }        // clocking of the chip
                                                                                                    //#else
                                                                                                    //#define INTFREQU		((fltype)(OPL->chip_clock / 72.0))		// clocking of the chip
                                                                                                    //#endif


        private const Int32 OF_TYPE_ATT = 0;
        private const Int32 OF_TYPE_DEC = 1;
        private const Int32 OF_TYPE_REL = 2;
        private const Int32 OF_TYPE_SUS = 3;
        private const Int32 OF_TYPE_SUS_NOKEEP = 4;
        private const Int32 OF_TYPE_OFF = 5;

        private const Int32 ARC_CONTROL = 0x00;
        private const Int32 ARC_TVS_KSR_MUL = 0x20;
        private const Int32 ARC_KSL_OUTLEV = 0x40;
        private const Int32 ARC_ATTR_DECR = 0x60;
        private const Int32 ARC_SUSL_RELR = 0x80;
        private const Int32 ARC_FREQ_NUM = 0xa0;
        private const Int32 ARC_KON_BNUM = 0xb0;
        private const Int32 ARC_PERC_MODE = 0xbd;
        private const Int32 ARC_FEEDBACK = 0xc0;
        private const Int32 ARC_WAVE_SEL = 0xe0;

        private const Int32 ARC_SECONDSET = 0x100;  // second operator set for OPL3


        private const Int32 OP_ACT_OFF = 0x00;
        private const Int32 OP_ACT_NORMAL = 0x01;   // regular channel activated (bitmasked)
        private const Int32 OP_ACT_PERC = 0x02; // percussion channel activated (bitmasked)

        private const Int32 BLOCKBUF_SIZE = 512;


        // vibrato constants
        private const Int32 VIBTAB_SIZE = 8;
        private const Int32 VIBFAC = 70 / 50000;		// no braces, integer mul/div

        // tremolo constants and table
        private const Int32 TREMTAB_SIZE = 53;
        private const double TREM_FREQ = ((double)(3.7));			// tremolo at 3.7hz


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
        {
            public Int32 cval, lastcval;          // current output/last output (used for feedback)
            public UInt32 tcount, wfpos, tinc;     // time (position in waveform) and time increment
            public double amp, step_amp;           // and amplification (envelope)
            public double vol;                     // volume
            public double sustain_level;           // sustain level
            public Int32 mfbi;                    // feedback amount
            public double a0, a1, a2, a3;          // attack rate function coefficients
            public double decaymul, releasemul;    // decay/release rate functions
            public UInt32 op_state;                // current state of operator (attack/decay/sustain/release/off)
            public UInt32 toff;
            public Int32 freq_high;               // highest three bits of the frequency, used for vibrato calculations
            //public Int16[] cur_wform;              // start of selected waveform
            public UInt32 ptrCur_wform;              // start of selected waveform
            public UInt32 cur_wmask;               // mask for selected waveform
            public UInt32 act_state;               // activity state (regular, percussion)
            public bool sus_keep;                  // keep sustain level when decay finished
            public bool vibrato, tremolo;          // vibrato/tremolo enable bits

            // variables used to provide non-continuous envelopes
            public UInt32 generator_pos;           // for non-standard sample rates we need to determine how many samples have passed
            public Int32 cur_env_step;              // current (standardized) sample position
            public Int32 env_step_a, env_step_d, env_step_r;    // number of std samples of one step (for attack/decay/release mode)
            public byte step_skip_pos_a;          // position of 8-cyclic step skipping (always 2^x to check against mask)
            public Int32 env_step_skip_a;           // bitmask that determines if a step is skipped (respective bit is zero then)

            //#if defined(OPLTYPE_IS_OPL3)
            public bool is_4op, is_4op_attached; // base of a 4op channel/part of a 4op channel
            public Int32 left_pan, right_pan;       // opl3 stereo panning amount
                                                    //#endif
        }
        //op_type;

        public class OPL_DATA
        {
            // per-chip variables
            //Bitu chip_num;
            public op_type[] op = new op_type[MAXOPERATORS];
            public byte[] MuteChn = new byte[NUM_CHANNELS + 5];
            public UInt32 chip_clock;

            public Int32 int_samplerate;

            public byte status;
            public UInt32 opl_index;
            public Int32 opl_addr;
            //#if defined(OPLTYPE_IS_OPL3)
            public byte[] adlibreg = new byte[512]; // adlib register set (including second set)
            public byte[] wave_sel = new byte[44];        // waveform selection
                                                          //#else
                                                          //Bit8u adlibreg[256];    // adlib register set
                                                          //Bit8u wave_sel[22];     // waveform selection
                                                          //#endif


            // vibrato/tremolo increment/counter
            public UInt32 vibtab_pos;
            public UInt32 vibtab_add;
            public UInt32 tremtab_pos;
            public UInt32 tremtab_add;

            public UInt32 generator_add;   // should be a chip parameter

            public double recipsamp;   // inverse of sampling rate
            public double[] frqmul = new double[16];

            public ADL_UPDATEHANDLER UpdateHandler;    // stream update handler
            public ymf262_state UpdateParam;                  // stream update parameter
        }
        //OPL_DATA;
        public delegate void ADL_UPDATEHANDLER(ymf262_state param);


        // enable an operator
        //public static void enable_operator(OPL_DATA chip, UInt32 regbase, op_type op_pt, UInt32 act_type) { }

        // functions to change parameters of an operator
        //public static void change_frequency(OPL_DATA chip, UInt32 chanbase, UInt32 regbase, op_type op_pt) { }

        //public static void change_attackrate(OPL_DATA chip, UInt32 regbase, op_type op_pt) { }
        //public static void change_decayrate(OPL_DATA chip, UInt32 regbase, op_type op_pt) { }
        //public static void change_releaserate(OPL_DATA chip, UInt32 regbase, op_type op_pt) { }
        //public static void change_sustainlevel(OPL_DATA chip, UInt32 regbase, op_type op_pt) { }
        //public static void change_waveform(OPL_DATA chip, UInt32 regbase, op_type op_pt) { }
        //public static void change_keepsustain(OPL_DATA chip, UInt32 regbase, op_type op_pt) { }
        //public static void change_vibrato(OPL_DATA chip, UInt32 regbase, op_type op_pt) { }
        //public static void change_feedback(OPL_DATA chip, UInt32 chanbase, op_type op_pt) { }

        // general functions
        /*void* adlib_init(Bitu clock, Bit32u samplerate);
        void adlib_writeIO(void *chip, Bitu addr, Bit8u val);
        void adlib_write(void *chip, Bitu idx, Bit8u val);
        //void adlib_getsample(Bit16s* sndptr, Bits numsamples);
        void adlib_getsample(void *chip, Bit32s** sndptr, Bits numsamples);

        Bitu adlib_reg_read(void *chip, Bitu port);
        void adlib_write_index(void *chip, Bitu port, Bit8u val);*/
        //public static void adlib_write(object chip, UInt32 idx, byte val) { }






        //************************************************************************************************************
        // opl.c
        //************************************************************************************************************
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


        //static fltype recipsamp;	// inverse of sampling rate		// moved to OPL_DATA
        private static Int16[] wavtable = new Int16[WAVEPREC * 3];   // wave form table

        // vibrato/tremolo tables
        private static Int32[] vib_table = new Int32[VIBTAB_SIZE];
        private static Int32[] trem_table = new Int32[TREMTAB_SIZE * 2];

        private static Int32[] vibval_const = new Int32[BLOCKBUF_SIZE];
        private static Int32[] tremval_const = new Int32[BLOCKBUF_SIZE];

        // vibrato value tables (used per-operator)
        private static Int32[] vibval_var1 = new Int32[BLOCKBUF_SIZE];
        private static Int32[] vibval_var2 = new Int32[BLOCKBUF_SIZE];
        //static Bit32s vibval_var3[BLOCKBUF_SIZE];
        //static Bit32s vibval_var4[BLOCKBUF_SIZE];

        // vibrato/trmolo value table pointers
        //static Bit32s *vibval1, *vibval2, *vibval3, *vibval4;
        //static Bit32s *tremval1, *tremval2, *tremval3, *tremval4;
        // moved to adlib_getsample


        // key scale level lookup table
        private static double[] kslmul = new double[4]{
            0.0, 0.5, 0.25, 1.0     // -> 0, 3, 1.5, 6 dB/oct
        };

        // frequency multiplicator lookup table
        private static double[] frqmul_tab = new double[16]{
            0.5,1,2,3,4,5,6,7,8,9,10,10,12,12,15,15
        };
        // calculated frequency multiplication values (depend on sampling rate)
        //static fltype frqmul[16];	// moved to OPL_DATA

        // key scale levels
        private static byte[][] kslev = new byte[8][] { new byte[16], new byte[16], new byte[16], new byte[16], new byte[16], new byte[16], new byte[16], new byte[16] };

        // map a channel number to the register offset of the modulator (=register base)
        private static byte[] modulatorbase = new byte[9]{
            0,1,2,
            8,9,10,
            16,17,18
        };

        // map a register base to a modulator operator number or operator number
        //#if defined(OPLTYPE_IS_OPL3)
        private static byte[] regbase2modop = new byte[44] {
            0,1,2,0,1,2,0,0,3,4,5,3,4,5,0,0,6,7,8,6,7,8,					// first set
	        18,19,20,18,19,20,0,0,21,22,23,21,22,23,0,0,24,25,26,24,25,26	// second set
        };
        private static byte[] regbase2op = new byte[44] {
            0,1,2,9,10,11,0,0,3,4,5,12,13,14,0,0,6,7,8,15,16,17,			// first set
	        18,19,20,27,28,29,0,0,21,22,23,30,31,32,0,0,24,25,26,33,34,35	// second set
        };
        //#else
        //        static const Bit8u regbase2modop[22] = {
        //    0,1,2,0,1,2,0,0,3,4,5,3,4,5,0,0,6,7,8,6,7,8
        //};
        //        static const Bit8u regbase2op[22] = {
        //    0,1,2,9,10,11,0,0,3,4,5,12,13,14,0,0,6,7,8,15,16,17
        //};
        //#endif


        // start of the waveform
        private static UInt32[] waveform = new UInt32[8]{
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
        private static UInt32[] wavemask = new UInt32[8]{
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
        private static UInt32[] wavestart = new UInt32[8]{
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
        private static double[] attackconst = new double[4]{
    (double)(1/2.82624),
    (double)(1/2.25280),
    (double)(1/1.88416),
    (double)(1/1.59744)
        };
        private static double[] decrelconst = new double[4]{
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
            op_pt.tcount += (UInt32)((op_pt.tinc) * vib / FIXEDPT);

            op_pt.generator_pos += chip.generator_add;
        }

        private void operator_advance_drums(OPL_DATA chip, op_type op_pt1, Int32 vib1, op_type op_pt2, Int32 vib2, op_type op_pt3, Int32 vib3)
        {
            UInt32 c1 = op_pt1.tcount / FIXEDPT;
            UInt32 c3 = op_pt3.tcount / FIXEDPT;
            UInt32 phasebit = (UInt32)((((c1 & 0x88) ^ ((c1 << 5) & 0x80)) | ((c3 ^ (c3 << 2)) & 0x20)) != 0 ? 0x02 : 0x00);

            UInt32 noisebit = (UInt32)((new System.Random()).Next() & 1);// rand() & 1;

            UInt32 snare_phase_bit = (((UInt32)((op_pt1.tcount / FIXEDPT) / 0x100)) & 1);

            //Hihat
            UInt32 inttm = (phasebit << 8) | ((UInt32)0x34 << (Int32)(phasebit ^ (noisebit << 1)));
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

                //op_pt.cval = (Int32)(op_pt.step_amp * op_pt.vol * op_pt.cur_wform[i & op_pt.cur_wmask] * trem / 16.0);
                op_pt.cval = (Int32)(op_pt.step_amp * op_pt.vol * wavtable[op_pt.ptrCur_wform+(i & op_pt.cur_wmask)] * trem / 16.0);
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

        private static void operator_eg_attack_check(op_type op_pt)
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


        //typedef void (* optype_fptr) (op_type);
        private delegate void optype_fptr(op_type op);

        private static optype_fptr[] opfuncs = new optype_fptr[6]{
            operator_attack,
            operator_decay,
            operator_release,
            operator_sustain,	// sustain phase (keeping level)
	        operator_release,	// sustain_nokeep phase (release-style)
	        operator_off
        };

        public static void change_attackrate(OPL_DATA chip, UInt32 regbase, op_type op_pt)
        {
            Int32 attackrate = chip.adlibreg[ARC_ATTR_DECR + regbase] >> 4;
            if (attackrate != 0)
            {
                byte[] step_skip_mask = new byte[5] { 0xff, 0xfe, 0xee, 0xba, 0xaa };
                Int32 step_skip;
                Int32 steps;
                Int32 step_num;

                double f = (double)(Math.Pow(FL2, (double)attackrate + (op_pt.toff >> 2) - 1) * attackconst[op_pt.toff & 3] * chip.recipsamp);
                // attack rate coefficients
                op_pt.a0 = (double)(0.0377 * f);
                op_pt.a1 = (double)(10.73 * f + 1);
                op_pt.a2 = (double)(-17.57 * f);
                op_pt.a3 = (double)(7.42 * f);

                step_skip = (Int32)(attackrate * 4 + op_pt.toff);
                steps = step_skip >> 2;
                op_pt.env_step_a = (1 << (steps <= 12 ? 12 - steps : 0)) - 1;

                step_num = (step_skip <= 48) ? (4 - (step_skip & 3)) : 0;
                op_pt.env_step_skip_a = step_skip_mask[step_num];

                //#if defined(OPLTYPE_IS_OPL3)
                if (step_skip >= 60)
                //#else
                //if (step_skip >= 62)
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

        public static void change_decayrate(OPL_DATA chip, UInt32 regbase, op_type op_pt)
        {
            Int32 decayrate = chip.adlibreg[ARC_ATTR_DECR + regbase] & 15;
            // decaymul should be 1.0 when decayrate==0
            if (decayrate != 0)
            {
                Int32 steps;

                double f = (double)(-7.4493 * decrelconst[op_pt.toff & 3] * chip.recipsamp);
                op_pt.decaymul = (double)(Math.Pow(FL2, f * Math.Pow(FL2, (double)(decayrate + (op_pt.toff >> 2)))));
                steps = (Int32)((decayrate * 4 + op_pt.toff) >> 2);
                op_pt.env_step_d = (1 << (steps <= 12 ? 12 - steps : 0)) - 1;
            }
            else
            {
                op_pt.decaymul = 1.0;
                op_pt.env_step_d = 0;
            }
        }

        public static void change_releaserate(OPL_DATA chip, UInt32 regbase, op_type op_pt)
        {
            Int32 releaserate = chip.adlibreg[ARC_SUSL_RELR + regbase] & 15;
            // releasemul should be 1.0 when releaserate==0
            if (releaserate != 0)
            {
                Int32 steps;

                double f = (double)(-7.4493 * decrelconst[op_pt.toff & 3] * chip.recipsamp);
                op_pt.releasemul = (double)(Math.Pow(FL2, f * Math.Pow(FL2, (double)(releaserate + (op_pt.toff >> 2)))));
                steps = (Int32)((releaserate * 4 + op_pt.toff) >> 2);
                op_pt.env_step_r = (1 << (steps <= 12 ? 12 - steps : 0)) - 1;
            }
            else
            {
                op_pt.releasemul = 1.0;
                op_pt.env_step_r = 0;
            }
        }

        public static void change_sustainlevel(OPL_DATA chip, UInt32 regbase, op_type op_pt)
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

        public static void change_waveform(OPL_DATA chip, UInt32 regbase, op_type op_pt)
        {
            //#if defined(OPLTYPE_IS_OPL3)
            if (regbase >= ARC_SECONDSET) regbase -= (ARC_SECONDSET - 22);  // second set starts at 22
                                                                            //#endif
                                                                            // waveform selection
            op_pt.cur_wmask = wavemask[chip.wave_sel[regbase]];
            //op_pt.cur_wform = wavtable[waveform[chip.wave_sel[regbase]]];
            op_pt.ptrCur_wform = (UInt32)waveform[chip.wave_sel[regbase]];
            // (might need to be adapted to waveform type here...)
        }

        public static void change_keepsustain(OPL_DATA chip, UInt32 regbase, op_type op_pt)
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
        public static void change_vibrato(OPL_DATA chip, UInt32 regbase, op_type op_pt)
        {
            op_pt.vibrato = (chip.adlibreg[ARC_TVS_KSR_MUL + regbase] & 0x40) != 0;
            op_pt.tremolo = (chip.adlibreg[ARC_TVS_KSR_MUL + regbase] & 0x80) != 0;
        }

        // change amount of self-feedback
        public static void change_feedback(OPL_DATA chip, UInt32 chanbase, op_type op_pt)
        {
            Int32 feedback = chip.adlibreg[ARC_FEEDBACK + chanbase] & 14;
            if (feedback != 0)
                op_pt.mfbi = (Int32)(Math.Pow(FL2, (double)((feedback >> 1) + 8)));
            else
                op_pt.mfbi = 0;
        }

        public static void change_frequency(OPL_DATA chip, UInt32 chanbase, UInt32 regbase, op_type op_pt)
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
            note_sel = (UInt32)((chip.adlibreg[8] >> 6) & 1);
            op_pt.toff = ((frn >> 9) & (note_sel ^ 1)) | ((frn >> 8) & note_sel);
            op_pt.toff += (oct << 1);

            // envelope scaling (KSR)
            if ((chip.adlibreg[ARC_TVS_KSR_MUL + regbase] & 0x10) == 0) op_pt.toff >>= 2;

            // 20+a0+b0:
            op_pt.tinc = (UInt32)((((double)(frn << (Int32)oct)) * chip.frqmul[chip.adlibreg[ARC_TVS_KSR_MUL + regbase] & 15]));
            // 40+a0+b0:
            vol_in = (double)((double)(chip.adlibreg[ARC_KSL_OUTLEV + regbase] & 63) +
                                    kslmul[chip.adlibreg[ARC_KSL_OUTLEV + regbase] >> 6] * kslev[oct][frn >> 6]);
            op_pt.vol = (double)(Math.Pow(FL2, (double)(vol_in * -0.125 - 14)));

            // operator frequency changed, care about features that depend on it
            change_attackrate(chip, regbase, op_pt);
            change_decayrate(chip, regbase, op_pt);
            change_releaserate(chip, regbase, op_pt);
        }

        public static void enable_operator(OPL_DATA chip, UInt32 regbase, op_type op_pt, UInt32 act_type)
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

        private static void disable_operator(op_type op_pt, UInt32 act_type)
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

        //void adlib_init(UInt32 samplerate)
        private OPL_DATA adlib_OPL3_init(UInt32 clock, UInt32 samplerate, ADL_UPDATEHANDLER UpdateHandler, ymf262_state param)
        {
            //Console.WriteLine("clock:{0} rate:{1}", clock, samplerate);
            OPL_DATA OPL;
            //op_type op;

            Int32 i, j, oct;
            //Int32 trem_table_int[TREMTAB_SIZE];
            UInt32 initfirstime = 0;

            OPL = new OPL_DATA();// (OPL_DATA) malloc(sizeof(OPL_DATA));
            OPL.chip_clock = clock;
            OPL.int_samplerate = (Int32)samplerate;
            OPL.UpdateHandler = UpdateHandler;
            OPL.UpdateParam = param;

            OPL.generator_add = (UInt32)(INTFREQU(OPL) * FIXEDPT / OPL.int_samplerate);


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
                OPL.frqmul[i] = (double)(frqmul_tab[i] * INTFREQU(OPL) / (double)WAVEPREC * (double)FIXEDPT * OPL.recipsamp);
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
            OPL.vibtab_add = (UInt32)(VIBTAB_SIZE * FIXEDPT_LFO / 8192 * INTFREQU(OPL) / OPL.int_samplerate);
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

        private void adlib_OPL3_stop(OPL_DATA chip)
        {

            //free(chip);

            return;
        }

        private void adlib_OPL3_reset(OPL_DATA chip)
        {
            OPL_DATA OPL = (OPL_DATA)chip;
            Int32 i;
            op_type op;

            //memset(OPL.adlibreg, 0x00, sizeof(OPL.adlibreg));
            for (i = 0; i < OPL.adlibreg.Length; i++) OPL.adlibreg[i] = 0x00;

            //memset(OPL.op, 0x00, sizeof(op_type) * MAXOPERATORS);
            for (i = 0; i < MAXOPERATORS; i++) OPL.op[i] = new op_type();

            //memset(OPL.wave_sel, 0x00, sizeof(OPL.wave_sel));
            for (i = 0; i < OPL.wave_sel.Length; i++) OPL.wave_sel[i] = 0x00;

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
                //op.cur_wform = wavtable[waveform[0]];
                op.ptrCur_wform = waveform[0];
                op.freq_high = 0;

                op.generator_pos = 0;
                op.cur_env_step = 0;
                op.env_step_a = 0;
                op.env_step_d = 0;
                op.env_step_r = 0;
                op.step_skip_pos_a = 0;
                op.env_step_skip_a = 0;

                //#if defined(OPLTYPE_IS_OPL3)
                op.is_4op = false;
                op.is_4op_attached = false;
                op.left_pan = 1;
                op.right_pan = 1;
                //#endif
            }

            OPL.status = 0;
            OPL.opl_index = 0;
            OPL.opl_addr = 0;

            return;
        }



        private void adlib_OPL3_writeIO(OPL_DATA chip, UInt32 addr, byte val)
        {
            OPL_DATA OPL = (OPL_DATA)chip;

            if ((addr & 1) != 0)
            {
                //Console.WriteLine("adr={0:x}  dat={1:x}", OPL.opl_addr, val);
                adlib_write(OPL, (UInt32)OPL.opl_addr, val);
            }
            else
                //#if defined(OPLTYPE_IS_OPL3)
                OPL.opl_addr = (Int32)(val | ((addr & 2) << 7));
            //#else
            //OPL.opl_addr = val;
            //#endif
        }

        public static void adlib_write(OPL_DATA chip, UInt32 idx, byte val)
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
                                // clear IRQ Int32 in status register
                                OPL.status &= 0x9f;// ~0x60;
                            }
                            else
                            {
                                OPL.status = 0;
                            }
                            break;
                        //#if defined(OPLTYPE_IS_OPL3)
                        case 0x04 | ARC_SECONDSET:
                            // 4op enable/disable switches for each possible channel
                            OPL.op[0].is_4op = (val & 1) > 0;
                            OPL.op[3].is_4op_attached = OPL.op[0].is_4op;
                            OPL.op[1].is_4op = (val & 2) > 0;
                            OPL.op[4].is_4op_attached = OPL.op[1].is_4op;
                            OPL.op[2].is_4op = (val & 4) > 0;
                            OPL.op[5].is_4op_attached = OPL.op[2].is_4op;
                            OPL.op[18].is_4op = (val & 8) > 0;
                            OPL.op[21].is_4op_attached = OPL.op[18].is_4op;
                            OPL.op[19].is_4op = (val & 16) > 0;
                            OPL.op[22].is_4op_attached = OPL.op[19].is_4op;
                            OPL.op[20].is_4op = (val & 32) > 0;
                            OPL.op[23].is_4op_attached = OPL.op[20].is_4op;
                            break;
                        case 0x05 | ARC_SECONDSET:
                            break;
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
                        Int32 num = (Int32)(idx & 7);
                        UInt32 _base = (idx - ARC_TVS_KSR_MUL) & 0xff;
                        if ((num < 6) && (_base < 22))
                        {
                            UInt32 modop = regbase2modop[second_set != 0 ? (_base + 22) : _base];
                            UInt32 regbase = _base + second_set;
                            UInt32 chanbase = second_set != 0 ? (modop - 18 + ARC_SECONDSET) : modop;

                            // change tremolo/vibrato and sustain keeping of this operator
                            op_type op_ptr = OPL.op[modop + ((num < 3) ? 0 : 9)];
                            change_keepsustain(chip, regbase, op_ptr);
                            change_vibrato(chip, regbase, op_ptr);

                            // change frequency calculations of this operator as
                            // key scale rate and frequency multiplicator can be changed
                            //#if defined(OPLTYPE_IS_OPL3)
                            if ((OPL.adlibreg[0x105] & 1) != 0 && (OPL.op[modop].is_4op_attached))
                            {
                                // operator uses frequency of channel
                                change_frequency(chip, chanbase - 3, regbase, op_ptr);
                            }
                            else
                            {
                                change_frequency(chip, chanbase, regbase, op_ptr);
                            }
                            //#else
                            //change_frequency(chip, chanbase, base, op_ptr);
                            //#endif
                        }
                    }
                    break;
                case ARC_KSL_OUTLEV:
                case ARC_KSL_OUTLEV + 0x10:
                    {
                        // key scale level; output rate
                        Int32 num = (Int32)(idx & 7);
                        UInt32 _base = (idx - ARC_KSL_OUTLEV) & 0xff;
                        if ((num < 6) && (_base < 22))
                        {
                            UInt32 modop = regbase2modop[second_set != 0 ? (_base + 22) : _base];
                            UInt32 chanbase = second_set != 0 ? (modop - 18 + ARC_SECONDSET) : modop;

                            // change frequency calculations of this operator as
                            // key scale level and output rate can be changed
                            op_type op_ptr = OPL.op[modop + ((num < 3) ? 0 : 9)];
                            //#if defined(OPLTYPE_IS_OPL3)
                            UInt32 regbase = _base + second_set;
                            if ((OPL.adlibreg[0x105] & 1) != 0 && (OPL.op[modop].is_4op_attached))
                            {
                                // operator uses frequency of channel
                                change_frequency(chip, chanbase - 3, regbase, op_ptr);
                            }
                            else
                            {
                                change_frequency(chip, chanbase, regbase, op_ptr);
                            }
                            //#else
                            //change_frequency(chip, chanbase, base, op_ptr);
                            //#endif
                        }
                    }
                    break;
                case ARC_ATTR_DECR:
                case ARC_ATTR_DECR + 0x10:
                    {
                        // attack/decay rates
                        Int32 num = (Int32)(idx & 7);
                        UInt32 _base = (idx - ARC_ATTR_DECR) & 0xff;
                        if ((num < 6) && (_base < 22))
                        {
                            UInt32 regbase = _base + second_set;

                            // change attack rate and decay rate of this operator
                            op_type op_ptr = OPL.op[regbase2op[second_set != 0 ? (_base + 22) : _base]];
                            change_attackrate(chip, regbase, op_ptr);
                            change_decayrate(chip, regbase, op_ptr);
                        }
                    }
                    break;
                case ARC_SUSL_RELR:
                case ARC_SUSL_RELR + 0x10:
                    {
                        // sustain level; release rate
                        Int32 num = (Int32)(idx & 7);
                        UInt32 _base = (idx - ARC_SUSL_RELR) & 0xff;
                        if ((num < 6) && (_base < 22))
                        {
                            UInt32 regbase = _base + second_set;

                            // change sustain level and release rate of this operator
                            op_type op_ptr = OPL.op[regbase2op[second_set != 0 ? (_base + 22) : _base]];
                            change_releaserate(chip, regbase, op_ptr);
                            change_sustainlevel(chip, regbase, op_ptr);
                        }
                    }
                    break;
                case ARC_FREQ_NUM:
                    {
                        // 0xa0-0xa8 low8 frequency
                        UInt32 _base = (idx - ARC_FREQ_NUM) & 0xff;
                        if (_base < 9)
                        {
                            Int32 opbase = (Int32)(second_set != 0 ? (_base + 18) : _base);
                            Int32 modbase;
                            UInt32 chanbase;
                            //#if defined(OPLTYPE_IS_OPL3)
                            if ((OPL.adlibreg[0x105] & 1) != 0 && OPL.op[opbase].is_4op_attached) break;
                            //#endif
                            // regbase of modulator:
                            modbase = (Int32)(modulatorbase[_base] + second_set);

                            chanbase = _base + second_set;

                            change_frequency(chip, chanbase, (UInt32)modbase, OPL.op[opbase]);
                            change_frequency(chip, chanbase, (UInt32)(modbase + 3), OPL.op[opbase + 9]);
                            //#if defined(OPLTYPE_IS_OPL3)
                            // for 4op channels all four operators are modified to the frequency of the channel
                            if ((OPL.adlibreg[0x105] & 1) != 0 && OPL.op[second_set != 0 ? (_base + 18) : _base].is_4op)
                            {
                                change_frequency(chip, chanbase, (UInt32)(modbase + 8), OPL.op[opbase + 3]);
                                change_frequency(chip, chanbase, (UInt32)(modbase + 3 + 8), OPL.op[opbase + 3 + 9]);
                            }
                            //#endif
                        }
                    }
                    break;
                case ARC_KON_BNUM:
                    {
                        UInt32 _base;
                        if (OPL.UpdateHandler != null) // hack for DOSBox logs
                            OPL.UpdateHandler(OPL.UpdateParam);
                        if (idx == ARC_PERC_MODE)
                        {
                            //#if defined(OPLTYPE_IS_OPL3)
                            if (second_set != 0) return;
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
                        _base = (idx - ARC_KON_BNUM) & 0xff;
                        if (_base < 9)
                        {
                            Int32 opbase = (Int32)(second_set != 0 ? (_base + 18) : _base);
                            // regbase of modulator:
                            Int32 modbase = (Int32)(modulatorbase[_base] + second_set);
                            UInt32 chanbase;

                            //#if defined(OPLTYPE_IS_OPL3)
                            if ((OPL.adlibreg[0x105] & 1) != 0 && OPL.op[opbase].is_4op_attached) break;
                            //#endif
                            if ((val & 32) != 0)
                            {
                                // operator switched on
                                enable_operator(chip, (UInt32)modbase, OPL.op[opbase], OP_ACT_NORMAL);        // modulator (if 2op)
                                enable_operator(chip, (UInt32)(modbase + 3), OPL.op[opbase + 9], OP_ACT_NORMAL);    // carrier (if 2op)
                                                                                                                    //#if defined(OPLTYPE_IS_OPL3)
                                                                                                                    // for 4op channels all four operators are switched on
                                if ((OPL.adlibreg[0x105] & 1) != 0 && OPL.op[opbase].is_4op)
                                {
                                    // turn on chan+3 operators as well
                                    enable_operator(chip, (UInt32)(modbase + 8), OPL.op[opbase + 3], OP_ACT_NORMAL);
                                    enable_operator(chip, (UInt32)(modbase + 3 + 8), OPL.op[opbase + 3 + 9], OP_ACT_NORMAL);
                                }
                                //#endif
                            }
                            else
                            {
                                // operator switched off
                                disable_operator(OPL.op[opbase], OP_ACT_NORMAL);
                                disable_operator(OPL.op[opbase + 9], OP_ACT_NORMAL);
                                //#if defined(OPLTYPE_IS_OPL3)
                                // for 4op channels all four operators are switched off
                                if ((OPL.adlibreg[0x105] & 1) != 0 && OPL.op[opbase].is_4op)
                                {
                                    // turn off chan+3 operators as well
                                    disable_operator(OPL.op[opbase + 3], OP_ACT_NORMAL);
                                    disable_operator(OPL.op[opbase + 3 + 9], OP_ACT_NORMAL);
                                }
                                //#endif
                            }

                            chanbase = _base + second_set;

                            // change frequency calculations of modulator and carrier (2op) as
                            // the frequency of the channel has changed
                            change_frequency(chip, chanbase, (UInt32)(modbase), OPL.op[opbase]);
                            change_frequency(chip, chanbase, (UInt32)(modbase + 3), OPL.op[opbase + 9]);
                            //#if defined(OPLTYPE_IS_OPL3)
                            // for 4op channels all four operators are modified to the frequency of the channel
                            if ((OPL.adlibreg[0x105] & 1) != 0 && OPL.op[second_set != 0 ? (_base + 18) : _base].is_4op)
                            {
                                // change frequency calculations of chan+3 operators as well
                                change_frequency(chip, chanbase, (UInt32)(modbase + 8), OPL.op[opbase + 3]);
                                change_frequency(chip, chanbase, (UInt32)(modbase + 3 + 8), OPL.op[opbase + 3 + 9]);
                            }
                            //#endif
                        }
                    }
                    break;
                case ARC_FEEDBACK:
                    {
                        // 0xc0-0xc8 feedback/modulation type (AM/FM)
                        UInt32 _base = (idx - ARC_FEEDBACK) & 0xff;
                        if (_base < 9)
                        {
                            Int32 opbase = (Int32)(second_set != 0 ? (_base + 18) : _base);
                            UInt32 chanbase = _base + second_set;
                            change_feedback(chip, chanbase, OPL.op[opbase]);
                            //#if defined(OPLTYPE_IS_OPL3)
                            // OPL3 panning
                            OPL.op[opbase].left_pan = ((val & 0x10) >> 4);
                            OPL.op[opbase].right_pan = ((val & 0x20) >> 5);
                            OPL.op[opbase].left_pan += ((val & 0x40) >> 6);
                            OPL.op[opbase].right_pan += ((val & 0x80) >> 7);
                            //#endif
                        }
                    }
                    break;
                case ARC_WAVE_SEL:
                case ARC_WAVE_SEL + 0x10:
                    {
                        Int32 num = (Int32)(idx & 7);
                        UInt32 _base = (idx - ARC_WAVE_SEL) & 0xff;
                        if ((num < 6) && (_base < 22))
                        {
                            //#if defined(OPLTYPE_IS_OPL3)
                            Int32 wselbase = (Int32)(second_set != 0 ? (_base + 22) : _base);   // for easier mapping onto wave_sel[]
                            op_type op_ptr;
                            // change waveform
                            if ((OPL.adlibreg[0x105] & 1) != 0) OPL.wave_sel[wselbase] = (byte)(val & 7);   // opl3 mode enabled, all waveforms accessible
                            else OPL.wave_sel[wselbase] = (byte)(val & 3);
                            op_ptr = OPL.op[regbase2modop[wselbase] + ((num < 3) ? 0 : 9)];
                            change_waveform(chip, (UInt32)wselbase, op_ptr);
                            //#else
                            //if (OPL.adlibreg[0x01] & 0x20)
                            //{
                            //op_type op_ptr;

                            // wave selection enabled, change waveform
                            //OPL.wave_sel[base] = val & 3;
                            //op_ptr = &OPL.op[regbase2modop[base] + ((num < 3) ? 0 : 9)];
                            //change_waveform(chip, base, op_ptr);
                            //}
                            //#endif
                        }
                    }
                    break;
                default:
                    break;
            }
        }


        private UInt32 adlib_OPL3_reg_read(OPL_DATA chip, UInt32 port)
        {
            OPL_DATA OPL = (OPL_DATA)chip;

            //#if defined(OPLTYPE_IS_OPL3)
            // opl3-detection routines require ret&6 to be zero
            if ((port & 1) == 0)
            {
                return OPL.status;
            }
            return 0x00;
            //#else
            // opl2-detection routines require ret&6 to be 6
            //if ((port&1)==0)
            //{
            //return OPL.status|6;
            //}
            //return 0xff;
            //#endif
        }

        private void adlib_OPL3_write_index(OPL_DATA chip, UInt32 port, byte val)
        {
            OPL_DATA OPL = (OPL_DATA)chip;

            OPL.opl_index = val;
            //#if defined(OPLTYPE_IS_OPL3)
            if ((port & 3) != 0)
            {
                // possibly second set
                if (((OPL.adlibreg[0x105] & 1) != 0) || (OPL.opl_index == 5)) OPL.opl_index |= ARC_SECONDSET;
            }
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
        //private void CHANVAL_OUT(chn) {
        //    if ((OPL.adlibreg[0x105] & 1) != 0)
        //    {
        //        outbufl[i] += chanval * cptr[chn].left_pan;
        //        outbufr[i] += chanval * cptr[chn].right_pan;
        //    }
        //    else
        //    {
        //        outbufl[i] += chanval;
        //        outbufr[i] += chanval;
        //    }
        //}
        //#else
        //#define CHANVAL_OUT(chn)								\
        //	outbufl[i] += chanval;							\
        //	outbufr[i] += chanval;
        //#endif

        Int32[] vib_lut = new Int32[BLOCKBUF_SIZE];
        Int32[] trem_lut = new Int32[BLOCKBUF_SIZE];
        //void adlib_getsample(Int16* sndptr, Int32 numsamples)
        private void adlib_OPL3_getsample(OPL_DATA chip, Int32[][] sndptr, Int32 numsamples)
        {
            OPL_DATA OPL = (OPL_DATA)chip;

            Int32 i, endsamples;
            op_type cptr;

            //Int32 outbufl[BLOCKBUF_SIZE];
            //#if defined(OPLTYPE_IS_OPL3)
            // second output buffer (right channel for opl3 stereo)
            //Int32 outbufr[BLOCKBUF_SIZE];
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

            Int32 ccptr = 0;

            //#if defined(OPLTYPE_IS_OPL3)
            if ((OPL.adlibreg[0x105] & 1) == 0) max_channel = NUM_CHANNELS / 2;
            //#endif

            if (samples_to_process == 0)
            {
                for (cur_ch = 0; cur_ch < max_channel; cur_ch++)
                {
                    if ((OPL.adlibreg[ARC_PERC_MODE] & 0x20) != 0 && (cur_ch >= 6 && cur_ch < 9))
                        continue;

                    //#if defined(OPLTYPE_IS_OPL3)
                    if (cur_ch < 9)
                        //cptr = OPL.op[cur_ch];
                        ccptr = cur_ch;
                    else
                        //cptr = OPL.op[cur_ch + 9];  // second set is operator18-operator35
                        ccptr = cur_ch + 9;  // second set is operator18-operator35
                                             //if (cptr.is_4op_attached)
                    if (OPL.op[ccptr].is_4op_attached)
                        continue;
                    //#else
                    //cptr = &OPL.op[cur_ch];
                    //#endif

                    //if (cptr[0].op_state == OF_TYPE_ATT)
                    //operator_eg_attack_check(cptr[0]);
                    if (OPL.op[ccptr + 0].op_state == OF_TYPE_ATT)
                        operator_eg_attack_check(OPL.op[ccptr + 0]);

                    //if (cptr[9].op_state == OF_TYPE_ATT)
                    //operator_eg_attack_check(cptr[9]);
                    if (OPL.op[ccptr + 9].op_state == OF_TYPE_ATT)
                        operator_eg_attack_check(OPL.op[ccptr + 9]);
                }

                return;
            }

            for (cursmp = 0; cursmp < samples_to_process; cursmp += endsamples)
            {
                endsamples = samples_to_process - cursmp;
                //if (endsamples>BLOCKBUF_SIZE) endsamples = BLOCKBUF_SIZE;

                //memset(outbufl,0, endsamples*sizeof(Int32));
                for (i = 0; i < endsamples; i++) outbufl[i] = 0;
                //#if defined(OPLTYPE_IS_OPL3)
                //		// clear second output buffer (opl3 stereo)
                //		//if (adlibreg[0x105]&1)
                //memset(outbufr,0, endsamples*sizeof(Int32));
                for (i = 0; i < endsamples; i++) outbufr[i] = 0;
                //#endif

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
                        //cptr = OPL.op[6];
                        ccptr = 6;
                        if ((OPL.adlibreg[ARC_FEEDBACK + 6] & 1) != 0)
                        {
                            // additive synthesis
                            //if (cptr[9].op_state != OF_TYPE_OFF)
                            if (OPL.op[ccptr + 9].op_state != OF_TYPE_OFF)
                            {
                                //if (cptr[9].vibrato)
                                if (OPL.op[ccptr + 9].vibrato)
                                {
                                    vibval1 = vibval_var1;
                                    for (i = 0; i < endsamples; i++)
                                        //vibval1[i] = (Int32)((vib_lut[i] * cptr[9].freq_high / 8) * FIXEDPT * VIBFAC);
                                        vibval1[i] = (Int32)((vib_lut[i] * OPL.op[ccptr + 9].freq_high / 8) * FIXEDPT * VIBFAC);
                                }
                                else
                                    vibval1 = vibval_const;
                                //if (cptr[9].tremolo)
                                if (OPL.op[ccptr + 9].tremolo)
                                    tremval1 = trem_lut;    // tremolo enabled, use table
                                else
                                    tremval1 = tremval_const;

                                // calculate channel output
                                for (i = 0; i < endsamples; i++)
                                {
                                    Int32 chanval;

                                    //operator_advance(OPL, cptr[9], vibval1[i]);
                                    //opfuncs[cptr[9].op_state](cptr[9]);
                                    operator_advance(OPL, OPL.op[ccptr + 9], vibval1[i]);
                                    opfuncs[OPL.op[ccptr + 9].op_state](OPL.op[ccptr + 9]);

                                    //operator_output(cptr[9], 0, tremval1[i]);
                                    //chanval = cptr[9].cval * 2;
                                    operator_output(OPL.op[ccptr + 9], 0, tremval1[i]);
                                    chanval = OPL.op[ccptr + 9].cval * 2;

                                    //CHANVAL_OUT(0);
                                    if ((OPL.adlibreg[0x105] & 1) != 0)
                                    {
                                        //outbufl[i] += chanval * cptr.left_pan;
                                        //outbufr[i] += chanval * cptr.right_pan;
                                        outbufl[i] += chanval * OPL.op[ccptr].left_pan;
                                        outbufr[i] += chanval * OPL.op[ccptr].right_pan;
                                    }
                                    else
                                    {
                                        outbufl[i] += chanval;
                                        outbufr[i] += chanval;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // frequency modulation
                            if ((OPL.op[ccptr + 9].op_state != OF_TYPE_OFF) || (OPL.op[ccptr + 0].op_state != OF_TYPE_OFF))
                            {
                                if ((OPL.op[ccptr + 0].vibrato) && (OPL.op[ccptr + 0].op_state != OF_TYPE_OFF))
                                {
                                    vibval1 = vibval_var1;
                                    for (i = 0; i < endsamples; i++)
                                        vibval1[i] = (Int32)((vib_lut[i] * OPL.op[ccptr + 0].freq_high / 8) * FIXEDPT * VIBFAC);
                                }
                                else
                                    vibval1 = vibval_const;
                                if ((OPL.op[ccptr + 9].vibrato) && (OPL.op[ccptr + 9].op_state != OF_TYPE_OFF))
                                {
                                    vibval2 = vibval_var2;
                                    for (i = 0; i < endsamples; i++)
                                        vibval2[i] = (Int32)((vib_lut[i] * OPL.op[ccptr + 9].freq_high / 8) * FIXEDPT * VIBFAC);
                                }
                                else
                                    vibval2 = vibval_const;
                                if (OPL.op[ccptr + 0].tremolo)
                                    tremval1 = trem_lut;    // tremolo enabled, use table
                                else
                                    tremval1 = tremval_const;
                                if (OPL.op[ccptr + 9].tremolo)
                                    tremval2 = trem_lut;    // tremolo enabled, use table
                                else
                                    tremval2 = tremval_const;

                                // calculate channel output
                                for (i = 0; i < endsamples; i++)
                                {
                                    Int32 chanval;


                                    operator_advance(OPL, OPL.op[ccptr + 0], vibval1[i]);
                                    opfuncs[OPL.op[ccptr + 0].op_state](OPL.op[ccptr + 0]);

                                    operator_output(OPL.op[ccptr + 0], (OPL.op[ccptr + 0].lastcval + OPL.op[ccptr + 0].cval) * OPL.op[ccptr + 0].mfbi / 2, tremval1[i]);


                                    operator_advance(OPL, OPL.op[ccptr + 9], vibval2[i]);
                                    opfuncs[OPL.op[ccptr + 9].op_state](OPL.op[ccptr + 9]);

                                    operator_output(OPL.op[ccptr + 9], OPL.op[ccptr + 0].cval * FIXEDPT, tremval2[i]);

                                    chanval = OPL.op[ccptr + 9].cval * 2;

                                    //CHANVAL_OUT(0);
                                    if ((OPL.adlibreg[0x105] & 1) != 0)
                                    {
                                        outbufl[i] += chanval * OPL.op[ccptr + 0].left_pan;
                                        outbufr[i] += chanval * OPL.op[ccptr + 0].right_pan;
                                    }
                                    else
                                    {
                                        outbufl[i] += chanval;
                                        outbufr[i] += chanval;
                                    }

                                }
                            }
                        }
                    }   // end if (! Muted)

                    //TomTom (j=8)
                    if ((OPL.MuteChn[NUM_CHANNELS + 2] == 0) && OPL.op[8].op_state != OF_TYPE_OFF)
                    {
                        cptr = OPL.op[8];
                        ccptr = 8;
                        //if (cptr[0].vibrato)
                        if (cptr.vibrato)
                        {
                            vibval3 = vibval_var1;
                            for (i = 0; i < endsamples; i++)
                                //vibval3[i] = (Int32)((vib_lut[i] * cptr[0].freq_high / 8) * FIXEDPT * VIBFAC);
                                vibval3[i] = (Int32)((vib_lut[i] * cptr.freq_high / 8) * FIXEDPT * VIBFAC);
                        }
                        else
                            vibval3 = vibval_const;

                        //if (cptr[0].tremolo)
                        if (cptr.tremolo)
                            tremval3 = trem_lut;    // tremolo enabled, use table
                        else
                            tremval3 = tremval_const;

                        // calculate channel output
                        for (i = 0; i < endsamples; i++)
                        {
                            Int32 chanval;

                            //operator_advance(OPL, &cptr[0], vibval3[i]);
                            operator_advance(OPL, cptr, vibval3[i]);
                            //opfuncs[cptr[0].op_state](&cptr[0]);        //TomTom
                            opfuncs[cptr.op_state](cptr);		//TomTom

                            //operator_output(&cptr[0], 0, tremval3[i]);
                            operator_output(cptr, 0, tremval3[i]);
                            //chanval = cptr[0].cval * 2;
                            chanval = cptr.cval * 2;

                            //CHANVAL_OUT(0);
                            if ((OPL.adlibreg[0x105] & 1) != 0)
                            {
                                outbufl[i] += chanval * OPL.op[ccptr + 0].left_pan;
                                outbufr[i] += chanval * OPL.op[ccptr + 0].right_pan;
                            }
                            else
                            {
                                outbufl[i] += chanval;
                                outbufr[i] += chanval;
                            }
                        }
                    }

                    //Snare/Hihat (j=7), Cymbal (j=8)
                    if ((OPL.op[7].op_state != OF_TYPE_OFF) || (OPL.op[16].op_state != OF_TYPE_OFF) ||
                        (OPL.op[17].op_state != OF_TYPE_OFF))
                    {
                        cptr = OPL.op[7];
                        //if ((cptr[0].vibrato) && (cptr[0].op_state != OF_TYPE_OFF))
                        if ((cptr.vibrato) && (cptr.op_state != OF_TYPE_OFF))
                        {
                            vibval1 = vibval_var1;
                            for (i = 0; i < endsamples; i++)
                                //vibval1[i] = (Int32)((vib_lut[i] * cptr[0].freq_high / 8) * FIXEDPT * VIBFAC);
                                vibval1[i] = (Int32)((vib_lut[i] * cptr.freq_high / 8) * FIXEDPT * VIBFAC);
                        }
                        else
                            vibval1 = vibval_const;
                        //if ((cptr[9].vibrato) && (cptr[9].op_state == OF_TYPE_OFF))
                        op_type cptr_9 = OPL.op[7 + 9];
                        if ((cptr_9.vibrato) && (cptr_9.op_state == OF_TYPE_OFF))
                        {
                            vibval2 = vibval_var2;
                            for (i = 0; i < endsamples; i++)
                                //vibval2[i] = (Int32)((vib_lut[i] * cptr[9].freq_high / 8) * FIXEDPT * VIBFAC);
                                vibval2[i] = (Int32)((vib_lut[i] * cptr_9.freq_high / 8) * FIXEDPT * VIBFAC);
                        }
                        else
                            vibval2 = vibval_const;

                        //if (cptr[0].tremolo)
                        if (cptr.tremolo)
                            tremval1 = trem_lut;    // tremolo enabled, use table
                        else
                            tremval1 = tremval_const;
                        //if (cptr[9].tremolo)
                        if (cptr_9.tremolo)
                            tremval2 = trem_lut;    // tremolo enabled, use table
                        else
                            tremval2 = tremval_const;

                        cptr = OPL.op[8];
                        cptr_9 = OPL.op[8 + 9];
                        //if ((cptr[9].vibrato) && (cptr[9].op_state == OF_TYPE_OFF))
                        if ((cptr_9.vibrato) && (cptr_9.op_state == OF_TYPE_OFF))
                        {
                            vibval4 = vibval_var2;
                            for (i = 0; i < endsamples; i++)
                                //vibval4[i] = (Int32)((vib_lut[i] * cptr[9].freq_high / 8) * FIXEDPT * VIBFAC);
                                vibval4[i] = (Int32)((vib_lut[i] * cptr_9.freq_high / 8) * FIXEDPT * VIBFAC);
                        }
                        else
                            vibval4 = vibval_const;

                        //if (cptr[9].tremolo) tremval4 = trem_lut;   // tremolo enabled, use table
                        if (cptr_9.tremolo) tremval4 = trem_lut;	// tremolo enabled, use table
                        else tremval4 = tremval_const;

                        // calculate channel output
                        cptr = OPL.op[0];   // set cptr to something useful (else it stays at op[8])
                        ccptr = 0;
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

                            //CHANVAL_OUT(7);
                            if ((OPL.adlibreg[0x105] & 1) != 0)
                            {
                                outbufl[i] += chanval * OPL.op[ccptr + 7].left_pan;
                                outbufr[i] += chanval * OPL.op[ccptr + 7].right_pan;
                            }
                            else
                            {
                                outbufl[i] += chanval;
                                outbufr[i] += chanval;
                            }

                            chanval = OPL.op[8 + 9].cval * 2;

                            //CHANVAL_OUT(8);
                            if ((OPL.adlibreg[0x105] & 1) != 0)
                            {
                                outbufl[i] += chanval * OPL.op[ccptr + 8].left_pan;
                                outbufr[i] += chanval * OPL.op[ccptr + 8].right_pan;
                            }
                            else
                            {
                                outbufl[i] += chanval;
                                outbufr[i] += chanval;
                            }
                        }
                    }
                }

                for (cur_ch = (Int32)(max_channel - 1); cur_ch >= 0; cur_ch--)
                {
                    UInt32 k;

                    if (OPL.MuteChn[cur_ch] != 0)
                        continue;

                    // skip drum/percussion operators
                    if ((OPL.adlibreg[ARC_PERC_MODE] & 0x20) != 0 && (cur_ch >= 6) && (cur_ch < 9)) continue;

                    k = (UInt32)cur_ch;
                    //#if defined(OPLTYPE_IS_OPL3)
                    if (cur_ch < 9)
                    {
                        cptr = OPL.op[cur_ch];
                        ccptr = cur_ch;
                    }
                    else
                    {
                        cptr = OPL.op[cur_ch + 9];  // second set is operator18-operator35
                        ccptr = cur_ch + 9;
                        k += (-9 + 256);        // second set uses registers 0x100 onwards
                    }
                    // check if this operator is part of a 4-op
                    //if ((OPL.adlibreg[0x105]&1) && cptr->is_4op_attached) continue;
                    if (cptr.is_4op_attached) continue; // this is more correct
                                                        //#else
                                                        //cptr = &OPL.op[cur_ch];
                                                        //#endif

                    // check for FM/AM
                    if ((OPL.adlibreg[ARC_FEEDBACK + k] & 1) != 0)
                    {
                        //#if defined(OPLTYPE_IS_OPL3)
                        //if ((OPL.adlibreg[0x105]&1) && cptr->is_4op)
                        if (cptr.is_4op)    // this is more correct
                        {
                            if ((OPL.adlibreg[ARC_FEEDBACK + k + 3] & 1) != 0)
                            {
                                // AM-AM-style synthesis (op1[fb] + (op2 * op3) + op4)
                                //if (cptr[0].op_state != OF_TYPE_OFF)
                                if (cptr.op_state != OF_TYPE_OFF)
                                {
                                    //if (cptr[0].vibrato)
                                    if (cptr.vibrato)
                                    {
                                        vibval1 = vibval_var1;
                                        for (i = 0; i < endsamples; i++)
                                            //vibval1[i] = (Int32)((vib_lut[i] * cptr[0].freq_high / 8) * FIXEDPT * VIBFAC);
                                            vibval1[i] = (Int32)((vib_lut[i] * cptr.freq_high / 8) * FIXEDPT * VIBFAC);
                                    }
                                    else
                                        vibval1 = vibval_const;
                                    //if (cptr[0].tremolo)
                                    if (cptr.tremolo)
                                        tremval1 = trem_lut;    // tremolo enabled, use table
                                    else
                                        tremval1 = tremval_const;

                                    // calculate channel output
                                    for (i = 0; i < endsamples; i++)
                                    {
                                        Int32 chanval;

                                        //operator_advance(OPL, &cptr[0], vibval1[i]);
                                        operator_advance(OPL, cptr, vibval1[i]);
                                        //opfuncs[cptr[0].op_state](&cptr[0]);
                                        opfuncs[cptr.op_state](cptr);
                                        //operator_output(&cptr[0], (cptr[0].lastcval + cptr[0].cval) * cptr[0].mfbi / 2, tremval1[i]);
                                        operator_output(cptr, (cptr.lastcval + cptr.cval) * cptr.mfbi / 2, tremval1[i]);

                                        //chanval = cptr[0].cval;
                                        chanval = cptr.cval;
                                        //CHANVAL_OUT(3); // Note: Op 1 of 4, so it needs to use the panning Int32 of Op 4 (Ch+3)
                                        if ((OPL.adlibreg[0x105] & 1) != 0)
                                        {
                                            //outbufl[i] += chanval * cptr[3].left_pan;
                                            //outbufr[i] += chanval * cptr[3].right_pan;
                                            outbufl[i] += chanval * OPL.op[ccptr + 3].left_pan;
                                            outbufr[i] += chanval * OPL.op[ccptr + 3].right_pan;
                                        }
                                        else
                                        {
                                            outbufl[i] += chanval;
                                            outbufr[i] += chanval;
                                        }
                                    }
                                }

                                if ((OPL.op[ccptr + 3].op_state != OF_TYPE_OFF) || (OPL.op[ccptr + 9].op_state != OF_TYPE_OFF))
                                {
                                    if ((OPL.op[ccptr + 9].vibrato) && (OPL.op[ccptr + 9].op_state != OF_TYPE_OFF))
                                    {
                                        vibval1 = vibval_var1;
                                        for (i = 0; i < endsamples; i++)
                                            vibval1[i] = (Int32)((vib_lut[i] * OPL.op[ccptr + 9].freq_high / 8) * FIXEDPT * VIBFAC);
                                    }
                                    else
                                        vibval1 = vibval_const;
                                    if (OPL.op[ccptr + 9].tremolo)
                                        tremval1 = trem_lut;    // tremolo enabled, use table
                                    else
                                        tremval1 = tremval_const;
                                    if (OPL.op[ccptr + 3].tremolo)
                                        tremval2 = trem_lut;    // tremolo enabled, use table
                                    else
                                        tremval2 = tremval_const;

                                    // calculate channel output
                                    for (i = 0; i < endsamples; i++)
                                    {
                                        Int32 chanval;

                                        operator_advance(OPL, OPL.op[ccptr + 9], vibval1[i]);
                                        opfuncs[OPL.op[ccptr + 9].op_state](OPL.op[ccptr + 9]);
                                        operator_output(OPL.op[ccptr + 9], 0, tremval1[i]);

                                        operator_advance(OPL, OPL.op[ccptr + 3], 0);
                                        opfuncs[OPL.op[ccptr + 3].op_state](OPL.op[ccptr + 3]);
                                        operator_output(OPL.op[ccptr + 3], OPL.op[ccptr + 9].cval * FIXEDPT, tremval2[i]);

                                        chanval = OPL.op[ccptr + 3].cval;
                                        //CHANVAL_OUT(3)
                                        if ((OPL.adlibreg[0x105] & 1) != 0)
                                        {
                                            outbufl[i] += chanval * OPL.op[ccptr + 3].left_pan;
                                            outbufr[i] += chanval * OPL.op[ccptr + 3].right_pan;
                                        }
                                        else
                                        {
                                            outbufl[i] += chanval;
                                            outbufr[i] += chanval;
                                        }

                                    }
                                }

                                if (OPL.op[ccptr + 3 + 9].op_state != OF_TYPE_OFF)
                                {
                                    if (OPL.op[ccptr + 3 + 9].tremolo)
                                        tremval1 = trem_lut;    // tremolo enabled, use table
                                    else
                                        tremval1 = tremval_const;

                                    // calculate channel output
                                    for (i = 0; i < endsamples; i++)
                                    {
                                        Int32 chanval;

                                        operator_advance(OPL, OPL.op[ccptr + 3 + 9], 0);
                                        opfuncs[OPL.op[ccptr + 3 + 9].op_state](OPL.op[ccptr + 3 + 9]);
                                        operator_output(OPL.op[ccptr + 3 + 9], 0, tremval1[i]);

                                        chanval = OPL.op[ccptr + 3 + 9].cval;
                                        //CHANVAL_OUT(3)
                                        if ((OPL.adlibreg[0x105] & 1) != 0)
                                        {
                                            outbufl[i] += chanval * OPL.op[ccptr + 3].left_pan;
                                            outbufr[i] += chanval * OPL.op[ccptr + 3].right_pan;
                                        }
                                        else
                                        {
                                            outbufl[i] += chanval;
                                            outbufr[i] += chanval;
                                        }

                                    }
                                }
                            }
                            else
                            {
                                // AM-FM-style synthesis (op1[fb] + (op2 * op3 * op4))
                                if (OPL.op[ccptr + 0].op_state != OF_TYPE_OFF)
                                {
                                    if (OPL.op[ccptr + 0].vibrato)
                                    {
                                        vibval1 = vibval_var1;
                                        for (i = 0; i < endsamples; i++)
                                            vibval1[i] = (Int32)((vib_lut[i] * OPL.op[ccptr + 0].freq_high / 8) * FIXEDPT * VIBFAC);
                                    }
                                    else
                                        vibval1 = vibval_const;
                                    if (OPL.op[ccptr + 0].tremolo)
                                        tremval1 = trem_lut;    // tremolo enabled, use table
                                    else
                                        tremval1 = tremval_const;

                                    // calculate channel output
                                    for (i = 0; i < endsamples; i++)
                                    {
                                        Int32 chanval;

                                        operator_advance(OPL, OPL.op[ccptr + 0], vibval1[i]);
                                        opfuncs[OPL.op[ccptr + 0].op_state](OPL.op[ccptr + 0]);
                                        operator_output(OPL.op[ccptr + 0], (OPL.op[ccptr + 0].lastcval + OPL.op[ccptr + 0].cval) * OPL.op[ccptr + 0].mfbi / 2, tremval1[i]);

                                        chanval = OPL.op[ccptr + 0].cval;
                                        //CHANVAL_OUT(3)
                                        if ((OPL.adlibreg[0x105] & 1) != 0)
                                        {
                                            outbufl[i] += chanval * OPL.op[ccptr + 3].left_pan;
                                            outbufr[i] += chanval * OPL.op[ccptr + 3].right_pan;
                                        }
                                        else
                                        {
                                            outbufl[i] += chanval;
                                            outbufr[i] += chanval;
                                        }

                                    }
                                }

                                if ((OPL.op[ccptr + 9].op_state != OF_TYPE_OFF) || (OPL.op[ccptr + 3].op_state != OF_TYPE_OFF) || (OPL.op[ccptr + 3 + 9].op_state != OF_TYPE_OFF))
                                {
                                    if ((OPL.op[ccptr + 9].vibrato) && (OPL.op[ccptr + 9].op_state != OF_TYPE_OFF))
                                    {
                                        vibval1 = vibval_var1;
                                        for (i = 0; i < endsamples; i++)
                                            vibval1[i] = (Int32)((vib_lut[i] * OPL.op[ccptr + 9].freq_high / 8) * FIXEDPT * VIBFAC);
                                    }
                                    else
                                        vibval1 = vibval_const;
                                    if (OPL.op[ccptr + 9].tremolo)
                                        tremval1 = trem_lut;    // tremolo enabled, use table
                                    else
                                        tremval1 = tremval_const;
                                    if (OPL.op[ccptr + 3].tremolo)
                                        tremval2 = trem_lut;    // tremolo enabled, use table
                                    else
                                        tremval2 = tremval_const;
                                    if (OPL.op[ccptr + 3 + 9].tremolo)
                                        tremval3 = trem_lut;    // tremolo enabled, use table
                                    else
                                        tremval3 = tremval_const;

                                    // calculate channel output
                                    for (i = 0; i < endsamples; i++)
                                    {
                                        Int32 chanval;

                                        operator_advance(OPL, OPL.op[ccptr + 9], vibval1[i]);
                                        opfuncs[OPL.op[ccptr + 9].op_state](OPL.op[ccptr + 9]);
                                        operator_output(OPL.op[ccptr + 9], 0, tremval1[i]);

                                        operator_advance(OPL, OPL.op[ccptr + 3], 0);
                                        opfuncs[OPL.op[ccptr + 3].op_state](OPL.op[ccptr + 3]);
                                        operator_output(OPL.op[ccptr + 3], OPL.op[ccptr + 9].cval * FIXEDPT, tremval2[i]);

                                        operator_advance(OPL, OPL.op[ccptr + 3 + 9], 0);
                                        opfuncs[OPL.op[ccptr + 3 + 9].op_state](OPL.op[ccptr + 3 + 9]);
                                        operator_output(OPL.op[ccptr + 3 + 9], OPL.op[ccptr + 3].cval * FIXEDPT, tremval3[i]);

                                        chanval = OPL.op[ccptr + 3 + 9].cval;
                                        //CHANVAL_OUT(3)
                                        if ((OPL.adlibreg[0x105] & 1) != 0)
                                        {
                                            outbufl[i] += chanval * OPL.op[ccptr + 3].left_pan;
                                            outbufr[i] += chanval * OPL.op[ccptr + 3].right_pan;
                                        }
                                        else
                                        {
                                            outbufl[i] += chanval;
                                            outbufr[i] += chanval;
                                        }

                                    }
                                }
                            }
                            continue;
                        }
                        //#endif
                        // 2op additive synthesis
                        if ((OPL.op[ccptr + 9].op_state == OF_TYPE_OFF) && (OPL.op[ccptr + 0].op_state == OF_TYPE_OFF)) continue;
                        if ((OPL.op[ccptr + 0].vibrato) && (OPL.op[ccptr + 0].op_state != OF_TYPE_OFF))
                        {
                            vibval1 = vibval_var1;
                            for (i = 0; i < endsamples; i++)
                                vibval1[i] = (Int32)((vib_lut[i] * OPL.op[ccptr + 0].freq_high / 8) * FIXEDPT * VIBFAC);
                        }
                        else
                            vibval1 = vibval_const;
                        if ((OPL.op[ccptr + 9].vibrato) && (OPL.op[ccptr + 9].op_state != OF_TYPE_OFF))
                        {
                            vibval2 = vibval_var2;
                            for (i = 0; i < endsamples; i++)
                                vibval2[i] = (Int32)((vib_lut[i] * OPL.op[ccptr + 9].freq_high / 8) * FIXEDPT * VIBFAC);
                        }
                        else
                            vibval2 = vibval_const;
                        if (OPL.op[ccptr + 0].tremolo)
                            tremval1 = trem_lut;    // tremolo enabled, use table
                        else
                            tremval1 = tremval_const;
                        if (OPL.op[ccptr + 9].tremolo)
                            tremval2 = trem_lut;    // tremolo enabled, use table
                        else
                            tremval2 = tremval_const;

                        // calculate channel output
                        for (i = 0; i < endsamples; i++)
                        {
                            Int32 chanval;

                            // carrier1
                            operator_advance(OPL, OPL.op[ccptr + 0], vibval1[i]);
                            opfuncs[OPL.op[ccptr + 0].op_state](OPL.op[ccptr + 0]);

                            operator_output(OPL.op[ccptr + 0], (OPL.op[ccptr + 0].lastcval + OPL.op[ccptr + 0].cval) * OPL.op[ccptr + 0].mfbi / 2, tremval1[i]);

                            // carrier2
                            operator_advance(OPL, OPL.op[ccptr + 9], vibval2[i]);
                            opfuncs[OPL.op[ccptr + 9].op_state](OPL.op[ccptr + 9]);

                            operator_output(OPL.op[ccptr + 9], 0, tremval2[i]);

                            chanval = OPL.op[ccptr + 9].cval + OPL.op[ccptr + 0].cval;

                            //CHANVAL_OUT(0)
                            if ((OPL.adlibreg[0x105] & 1) != 0)
                            {
                                outbufl[i] += chanval * OPL.op[ccptr + 0].left_pan;
                                outbufr[i] += chanval * OPL.op[ccptr + 0].right_pan;
                            }
                            else
                            {
                                outbufl[i] += chanval;
                                outbufr[i] += chanval;
                            }

                        }
                    }
                    else
                    {
                        //#if defined(OPLTYPE_IS_OPL3)
                        //if ((OPL.adlibreg[0x105]&1) && cptr->is_4op)
                        if (cptr.is_4op)    // this is more correct
                        {
                            if ((OPL.adlibreg[ARC_FEEDBACK + k + 3] & 1) != 0)
                            {
                                // FM-AM-style synthesis ((op1[fb] * op2) + (op3 * op4))
                                if ((OPL.op[ccptr + 0].op_state != OF_TYPE_OFF) || (OPL.op[ccptr + 9].op_state != OF_TYPE_OFF))
                                {
                                    if ((OPL.op[ccptr + 0].vibrato) && (OPL.op[ccptr + 0].op_state != OF_TYPE_OFF))
                                    {
                                        vibval1 = vibval_var1;
                                        for (i = 0; i < endsamples; i++)
                                            vibval1[i] = (Int32)((vib_lut[i] * OPL.op[ccptr + 0].freq_high / 8) * FIXEDPT * VIBFAC);
                                    }
                                    else
                                        vibval1 = vibval_const;
                                    if ((OPL.op[ccptr + 9].vibrato) && (OPL.op[ccptr + 9].op_state != OF_TYPE_OFF))
                                    {
                                        vibval2 = vibval_var2;
                                        for (i = 0; i < endsamples; i++)
                                            vibval2[i] = (Int32)((vib_lut[i] * OPL.op[ccptr + 9].freq_high / 8) * FIXEDPT * VIBFAC);
                                    }
                                    else
                                        vibval2 = vibval_const;
                                    if (OPL.op[ccptr + 0].tremolo)
                                        tremval1 = trem_lut;    // tremolo enabled, use table
                                    else
                                        tremval1 = tremval_const;
                                    if (OPL.op[ccptr + 9].tremolo)
                                        tremval2 = trem_lut;    // tremolo enabled, use table
                                    else
                                        tremval2 = tremval_const;

                                    // calculate channel output
                                    for (i = 0; i < endsamples; i++)
                                    {
                                        Int32 chanval;

                                        operator_advance(OPL, OPL.op[ccptr + 0], vibval1[i]);
                                        opfuncs[OPL.op[ccptr + 0].op_state](OPL.op[ccptr + 0]);
                                        operator_output(OPL.op[ccptr + 0], (OPL.op[ccptr + 0].lastcval + OPL.op[ccptr + 0].cval) * OPL.op[ccptr + 0].mfbi / 2, tremval1[i]);

                                        operator_advance(OPL, OPL.op[ccptr + 9], vibval2[i]);
                                        opfuncs[OPL.op[ccptr + 9].op_state](OPL.op[ccptr + 9]);
                                        operator_output(OPL.op[ccptr + 9], OPL.op[ccptr + 0].cval * FIXEDPT, tremval2[i]);

                                        chanval = OPL.op[ccptr + 9].cval;
                                        //CHANVAL_OUT(3)
                                        if ((OPL.adlibreg[0x105] & 1) != 0)
                                        {
                                            outbufl[i] += chanval * OPL.op[ccptr + 3].left_pan;
                                            outbufr[i] += chanval * OPL.op[ccptr + 3].right_pan;
                                        }
                                        else
                                        {
                                            outbufl[i] += chanval;
                                            outbufr[i] += chanval;
                                        }
                                    }
                                }

                                if ((OPL.op[ccptr + 3].op_state != OF_TYPE_OFF) || (OPL.op[ccptr + 3 + 9].op_state != OF_TYPE_OFF))
                                {
                                    if (OPL.op[ccptr + 3].tremolo)
                                        tremval1 = trem_lut;    // tremolo enabled, use table
                                    else
                                        tremval1 = tremval_const;
                                    if (OPL.op[ccptr + 3 + 9].tremolo)
                                        tremval2 = trem_lut;    // tremolo enabled, use table
                                    else
                                        tremval2 = tremval_const;

                                    // calculate channel output
                                    for (i = 0; i < endsamples; i++)
                                    {
                                        Int32 chanval;

                                        operator_advance(OPL, OPL.op[ccptr + 3], 0);
                                        opfuncs[OPL.op[ccptr + 3].op_state](OPL.op[ccptr + 3]);
                                        operator_output(OPL.op[ccptr + 3], 0, tremval1[i]);

                                        operator_advance(OPL, OPL.op[ccptr + 3 + 9], 0);
                                        opfuncs[OPL.op[ccptr + 3 + 9].op_state](OPL.op[ccptr + 3 + 9]);
                                        operator_output(OPL.op[ccptr + 3 + 9], OPL.op[ccptr + 3].cval * FIXEDPT, tremval2[i]);

                                        chanval = OPL.op[ccptr + 3 + 9].cval;
                                        //CHANVAL_OUT(3)
                                        if ((OPL.adlibreg[0x105] & 1) != 0)
                                        {
                                            outbufl[i] += chanval * OPL.op[ccptr + 3].left_pan;
                                            outbufr[i] += chanval * OPL.op[ccptr + 3].right_pan;
                                        }
                                        else
                                        {
                                            outbufl[i] += chanval;
                                            outbufr[i] += chanval;
                                        }
                                    }
                                }

                            }
                            else
                            {
                                // FM-FM-style synthesis (op1[fb] * op2 * op3 * op4)
                                if ((OPL.op[ccptr + 0].op_state != OF_TYPE_OFF) || (OPL.op[ccptr + 9].op_state != OF_TYPE_OFF) ||
                                    (OPL.op[ccptr + 3].op_state != OF_TYPE_OFF) || (OPL.op[ccptr + 3 + 9].op_state != OF_TYPE_OFF))
                                {
                                    if ((OPL.op[ccptr + 0].vibrato) && (OPL.op[ccptr + 0].op_state != OF_TYPE_OFF))
                                    {
                                        vibval1 = vibval_var1;
                                        for (i = 0; i < endsamples; i++)
                                            vibval1[i] = (Int32)((vib_lut[i] * OPL.op[ccptr + 0].freq_high / 8) * FIXEDPT * VIBFAC);
                                    }
                                    else
                                        vibval1 = vibval_const;
                                    if ((OPL.op[ccptr + 9].vibrato) && (OPL.op[ccptr + 9].op_state != OF_TYPE_OFF))
                                    {
                                        vibval2 = vibval_var2;
                                        for (i = 0; i < endsamples; i++)
                                            vibval2[i] = (Int32)((vib_lut[i] * OPL.op[ccptr + 9].freq_high / 8) * FIXEDPT * VIBFAC);
                                    }
                                    else
                                        vibval2 = vibval_const;
                                    if (OPL.op[ccptr + 0].tremolo)
                                        tremval1 = trem_lut;    // tremolo enabled, use table
                                    else
                                        tremval1 = tremval_const;
                                    if (OPL.op[ccptr + 9].tremolo)
                                        tremval2 = trem_lut;    // tremolo enabled, use table
                                    else
                                        tremval2 = tremval_const;
                                    if (OPL.op[ccptr + 3].tremolo)
                                        tremval3 = trem_lut;    // tremolo enabled, use table
                                    else
                                        tremval3 = tremval_const;
                                    if (OPL.op[ccptr + 3 + 9].tremolo)
                                        tremval4 = trem_lut;    // tremolo enabled, use table
                                    else
                                        tremval4 = tremval_const;

                                    // calculate channel output
                                    for (i = 0; i < endsamples; i++)
                                    {
                                        Int32 chanval;

                                        operator_advance(OPL, OPL.op[ccptr + 0], vibval1[i]);
                                        opfuncs[OPL.op[ccptr + 0].op_state](OPL.op[ccptr + 0]);
                                        operator_output(OPL.op[ccptr + 0], (OPL.op[ccptr + 0].lastcval + OPL.op[ccptr + 0].cval) * OPL.op[ccptr + 0].mfbi / 2, tremval1[i]);

                                        operator_advance(OPL, OPL.op[ccptr + 9], vibval2[i]);
                                        opfuncs[OPL.op[ccptr + 9].op_state](OPL.op[ccptr + 9]);
                                        operator_output(OPL.op[ccptr + 9], OPL.op[ccptr + 0].cval * FIXEDPT, tremval2[i]);

                                        operator_advance(OPL, OPL.op[ccptr + 3], 0);
                                        opfuncs[OPL.op[ccptr + 3].op_state](OPL.op[ccptr + 3]);
                                        operator_output(OPL.op[ccptr + 3], OPL.op[ccptr + 9].cval * FIXEDPT, tremval3[i]);

                                        operator_advance(OPL, OPL.op[ccptr + 3 + 9], 0);
                                        opfuncs[OPL.op[ccptr + 3 + 9].op_state](OPL.op[ccptr + 3 + 9]);
                                        operator_output(OPL.op[ccptr + 3 + 9], OPL.op[ccptr + 3].cval * FIXEDPT, tremval4[i]);

                                        chanval = OPL.op[ccptr + 3 + 9].cval;
                                        //CHANVAL_OUT(3)
                                        if ((OPL.adlibreg[0x105] & 1) != 0)
                                        {
                                            outbufl[i] += chanval * OPL.op[ccptr + 3].left_pan;
                                            outbufr[i] += chanval * OPL.op[ccptr + 3].right_pan;
                                        }
                                        else
                                        {
                                            outbufl[i] += chanval;
                                            outbufr[i] += chanval;
                                        }
                                    }
                                }
                            }
                            continue;
                        }
                        //#endif
                        // 2op frequency modulation
                        if ((OPL.op[ccptr + 9].op_state == OF_TYPE_OFF) && (OPL.op[ccptr + 0].op_state == OF_TYPE_OFF)) continue;
                        if ((OPL.op[ccptr + 0].vibrato) && (OPL.op[ccptr + 0].op_state != OF_TYPE_OFF))
                        {
                            vibval1 = vibval_var1;
                            for (i = 0; i < endsamples; i++)
                                vibval1[i] = (Int32)((vib_lut[i] * OPL.op[ccptr + 0].freq_high / 8) * FIXEDPT * VIBFAC);
                        }
                        else
                            vibval1 = vibval_const;
                        if ((OPL.op[ccptr + 9].vibrato) && (OPL.op[ccptr + 9].op_state != OF_TYPE_OFF))
                        {
                            vibval2 = vibval_var2;
                            for (i = 0; i < endsamples; i++)
                                vibval2[i] = (Int32)((vib_lut[i] * OPL.op[ccptr + 9].freq_high / 8) * FIXEDPT * VIBFAC);
                        }
                        else
                            vibval2 = vibval_const;
                        if (OPL.op[ccptr + 0].tremolo)
                            tremval1 = trem_lut;    // tremolo enabled, use table
                        else
                            tremval1 = tremval_const;
                        if (OPL.op[ccptr + 9].tremolo)
                            tremval2 = trem_lut;    // tremolo enabled, use table
                        else
                            tremval2 = tremval_const;

                        // calculate channel output
                        for (i = 0; i < endsamples; i++)
                        {
                            Int32 chanval;

                            // modulator
                            operator_advance(OPL, OPL.op[ccptr + 0], vibval1[i]);
                            opfuncs[OPL.op[ccptr + 0].op_state](OPL.op[ccptr + 0]);

                            operator_output(OPL.op[ccptr + 0], (OPL.op[ccptr + 0].lastcval + OPL.op[ccptr + 0].cval) * OPL.op[ccptr + 0].mfbi / 2, tremval1[i]);

                            // carrier
                            operator_advance(OPL, OPL.op[ccptr + 9], vibval2[i]);
                            opfuncs[OPL.op[ccptr + 9].op_state](OPL.op[ccptr + 9]);

                            operator_output(OPL.op[ccptr + 9], OPL.op[ccptr + 0].cval * FIXEDPT, tremval2[i]);

                            chanval = OPL.op[ccptr + 9].cval;

                            //CHANVAL_OUT(0)
                            if ((OPL.adlibreg[0x105] & 1) != 0)
                            {
                                outbufl[i] += chanval * OPL.op[ccptr + 0].left_pan;
                                outbufr[i] += chanval * OPL.op[ccptr + 0].right_pan;
                            }
                            else
                            {
                                outbufl[i] += chanval;
                                outbufr[i] += chanval;
                            }

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

                //Console.WriteLine("bufl:{0} bufr:{1}", outbufl[cursmp], outbufr[cursmp]);
            }
        }

        public void adlib_OPL3_set_mute_mask(OPL_DATA chip, UInt32 MuteMask)
        {
            OPL_DATA OPL = (OPL_DATA)chip;

            byte CurChn;

            for (CurChn = 0; CurChn < NUM_CHANNELS + 5; CurChn++)
                OPL.MuteChn[CurChn] = (byte)((MuteMask >> CurChn) & 0x01);

            return;
        }

    }
}
