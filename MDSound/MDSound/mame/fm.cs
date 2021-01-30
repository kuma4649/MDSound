using System;
using System.Collections.Generic;
using System.Text;

namespace MDSound
{
    public class fm
    {
        //#define YM2610B_WARNING

        /*
		**
		** File: fm.c -- software implementation of Yamaha FM sound generator
		**
		** Copyright Jarek Burczynski (bujar at mame dot net)
		** Copyright Tatsuyuki Satoh , MultiArcadeMachineEmulator development
		**
		** Version 1.4.2 (final beta)
		**
		*/

        /*
        ** History:
        **
        ** 2006-2008 Eke-Eke (Genesis Plus GX), MAME backport by R. Belmont.
        **  - implemented PG overflow, aka "detune bug" (Ariel, Comix Zone, Shaq Fu, Spiderman,...), credits to Nemesis
        **  - fixed SSG-EG support, credits to Nemesis and additional fixes from Alone Coder
        **  - modified EG rates and frequency, tested by Nemesis on real hardware
        **  - implemented LFO phase update for CH3 special mode (Warlock birds, Alladin bug sound)
        **  - fixed Attack Rate update (Batman & Robin intro)
        **  - fixed attenuation level at the start of Substain (Gynoug explosions)
        **  - fixed EG decay.substain transition to handle special cases, like SL=0 and Decay rate is very slow (Mega Turrican tracks 03,09...)
        **
        ** 06-23-2007 Zsolt Vasvari:
        **  - changed the timing not to require the use of floating point calculations
        **
        ** 03-08-2003 Jarek Burczynski:
        **  - fixed YM2608 initial values (after the reset)
        **  - fixed flag and irqmask handling (YM2608)
        **  - fixed BUFRDY flag handling (YM2608)
        **
        ** 14-06-2003 Jarek Burczynski:
        **  - implemented all of the YM2608 status register flags
        **  - implemented support for external memory read/write via YM2608
        **  - implemented support for deltat memory limit register in YM2608 emulation
        **
        ** 22-05-2003 Jarek Burczynski:
        **  - fixed LFO PM calculations (copy&paste bugfix)
        **
        ** 08-05-2003 Jarek Burczynski:
        **  - fixed SSG support
        **
        ** 22-04-2003 Jarek Burczynski:
        **  - implemented 100% correct LFO generator (verified on real YM2610 and YM2608)
        **
        ** 15-04-2003 Jarek Burczynski:
        **  - added support for YM2608's register 0x110 - status mask
        **
        ** 01-12-2002 Jarek Burczynski:
        **  - fixed register addressing in YM2608, YM2610, YM2610B chips. (verified on real YM2608)
        **    The addressing patch used for early Neo-Geo games can be removed now.
        **
        ** 26-11-2002 Jarek Burczynski, Nicola Salmoria:
        **  - recreated YM2608 ADPCM ROM using data from real YM2608's output which leads to:
        **  - added emulation of YM2608 drums.
        **  - output of YM2608 is two times lower now - same as YM2610 (verified on real YM2608)
        **
        ** 16-08-2002 Jarek Burczynski:
        **  - binary exact Envelope Generator (verified on real YM2203);
        **    identical to YM2151
        **  - corrected 'off by one' error in feedback calculations (when feedback is off)
        **  - corrected connection (algorithm) calculation (verified on real YM2203 and YM2610)
        **
        ** 18-12-2001 Jarek Burczynski:
        **  - added SSG-EG support (verified on real YM2203)
        **
        ** 12-08-2001 Jarek Burczynski:
        **  - corrected sin_tab and tl_tab data (verified on real chip)
        **  - corrected feedback calculations (verified on real chip)
        **  - corrected phase generator calculations (verified on real chip)
        **  - corrected envelope generator calculations (verified on real chip)
        **  - corrected FM volume level (YM2610 and YM2610B).
        **  - changed YMxxxUpdateOne() functions (YM2203, YM2608, YM2610, YM2610B, YM2612) :
        **    this was needed to calculate YM2610 FM channels output correctly.
        **    (Each FM channel is calculated as in other chips, but the output of the channel
        **    gets shifted right by one *before* sending to accumulator. That was impossible to do
        **    with previous implementation).
        **
        ** 23-07-2001 Jarek Burczynski, Nicola Salmoria:
        **  - corrected YM2610 ADPCM type A algorithm and tables (verified on real chip)
        **
        ** 11-06-2001 Jarek Burczynski:
        **  - corrected end of sample bug in ADPCMA_calc_cha().
        **    Real YM2610 checks for equality between current and end addresses (only 20 LSB bits).
        **
        ** 08-12-98 hiro-shi:
        ** rename ADPCMA . ADPCMB, ADPCMB . ADPCMA
        ** move ROM limit check.(CALC_CH? . 2610Write1/2)
        ** test program (ADPCMB_TEST)
        ** move ADPCM A/B end check.
        ** ADPCMB repeat flag(no check)
        ** change ADPCM volume rate (8.16) (32.48).
        **
        ** 09-12-98 hiro-shi:
        ** change ADPCM volume. (8.16, 48.64)
        ** replace ym2610 ch0/3 (YM-2610B)
        ** change ADPCM_SHIFT (10.8) missing bank change 0x4000-0xffff.
        ** add ADPCM_SHIFT_MASK
        ** change ADPCMA_DECODE_MIN/MAX.
        */




        /************************************************************************/
        /*    comment of hiro-shi(Hiromitsu Shioya)                             */
        /*    YM2610(B) = OPN-B                                                 */
        /*    YM2610  : PSG:3ch FM:4ch ADPCM(18.5KHz):6ch DeltaT ADPCM:1ch      */
        /*    YM2610B : PSG:3ch FM:6ch ADPCM(18.5KHz):6ch DeltaT ADPCM:1ch      */
        /************************************************************************/

        //# include <stdio.h>
        //# include <stdlib.h>
        //# include <string.h>
        //# include <stdarg.h>
        //# include <math.h>

        //# include "mamedef.h"
        //		//#ifndef __RAINE__
        //		//#include "sndintrf.h"		/* use M.A.M.E. */
        //		//#else
        //		//#include "deftypes.h"		/* use RAINE */
        //		//#include "support.h"		/* use RAINE */
        //		//#endif
        //# include "fm.h"





        /*
          File: fm.h -- header file for software emulation for FM sound generator
        */

        //#pragma once

        /* --- select emulation chips --- */
        /*
        #define BUILD_YM2203  (HAS_YM2203)		// build YM2203(OPN)   emulator
        #define BUILD_YM2608  (HAS_YM2608)		// build YM2608(OPNA)  emulator
        #define BUILD_YM2610  (HAS_YM2610)		// build YM2610(OPNB)  emulator
        #define BUILD_YM2610B (HAS_YM2610B)		// build YM2610B(OPNB?)emulator
        #define BUILD_YM2612  (HAS_YM2612)		// build YM2612(OPN2)  emulator
        #define BUILD_YM3438  (HAS_YM3438)		// build YM3438(OPN) emulator
        */
        public const int BUILD_YM2203 = 1;
        public const int BUILD_YM2608 = 1;
        public const int BUILD_YM2610 = 1;
        public const int BUILD_YM2610B = 1;
        public const int BUILD_YM2612 = 1;
        public const int BUILD_YM3438 = 1;

        /* select bit size of output : 8 or 16 */
        private const int FM_SAMPLE_BITS = 16;

        /* select timer system internal or external */
        private const int FM_INTERNAL_TIMER = 1;

        /* --- speedup optimize --- */
        /* busy flag enulation , The definition of FM_GET_TIME_NOW() is necessary. */
        //#define FM_BUSY_FLAG_SUPPORT 1

        /* --- external SSG(YM2149/AY-3-8910)emulator interface port */
        /* used by YM2203,YM2608,and YM2610 */
        private _ssg_callbacks ssg_callbacks;
        public class _ssg_callbacks
        {
            public delegate void dlgSet_clock(FM_base param, int clock);
            public dlgSet_clock set_clock;
            public delegate void dlgWrite(FM_base param, int address, int data);
            public dlgWrite write;
            public delegate short dlgRead(FM_base param);
            public dlgRead read;
            public delegate short dlgReset(FM_base param);
            public dlgReset reset;
        };

        /* --- external callback funstions for realtime update --- */

        //#if FM_BUSY_FLAG_SUPPORT
        //#define TIME_TYPE					attotime
        //#define UNDEFINED_TIME				attotime_zero
        //#define FM_GET_TIME_NOW(machine)			timer_get_time(machine)
        //#define ADD_TIMES(t1, t2)   		attotime_add((t1), (t2))
        //#define COMPARE_TIMES(t1, t2)		attotime_compare((t1), (t2))
        //#define MULTIPLY_TIME_BY_INT(t,i)	attotime_mul(t, i)
        //#endif

        //#if BUILD_YM2203
        /* in 2203intf.c */
        public delegate void callBack_update_request(byte ChipID, FM_base param);
        public callBack_update_request ym2203_update_request;
        private void ym2203_update_req(byte ChipID, YM2203 chip) { ym2203_update_request(ChipID, chip); }
        //#endif /* BUILD_YM2203 */

        //#if BUILD_YM2608
        /* in 2608intf.c */
        public callBack_update_request ym2608_update_request;
        private void ym2608_update_req(byte ChipID, YM2608 chip) { ym2608_update_request(ChipID, chip); }
        //#endif /* BUILD_YM2608 */

        //#if (BUILD_YM2610 || BUILD_YM2610B)
        /* in 2610intf.c */
        public callBack_update_request ym2610_update_request;
        private void ym2610_update_req(byte ChipID, YM2610 chip) { ym2610_update_request(ChipID, chip); }
        //#endif /* (BUILD_YM2610||BUILD_YM2610B) */

        //#if (BUILD_YM2612 || BUILD_YM3438)
        /* in 2612intf.c */
        public callBack_update_request ym2612_update_request;
        private void ym2612_update_req(byte ChipID, mame.fm2612.YM2612 chip) { ym2612_update_request(ChipID,chip); }
        //#endif /* (BUILD_YM2612||BUILD_YM3438) */

        /* compiler dependence */
        //#if 0
        //# ifndef OSD_CPU_H
        //#define OSD_CPU_H
        //typedef unsigned char	UINT8;   /* unsigned  8bit */
        //typedef unsigned short	UINT16;  /* unsigned 16bit */
        //typedef unsigned int	UINT32;  /* unsigned 32bit */
        //typedef signed char		INT8;    /* signed  8bit   */
        //typedef signed short	INT16;   /* signed 16bit   */
        //typedef signed int		INT32;   /* signed 32bit   */
        //#endif /* OSD_CPU_H */
        //#endif



        //typedef stream_sample_t FMSAMPLE;
        /*
        #if (FM_SAMPLE_BITS==16)
        typedef INT16 FMSAMPLE;
        #endif
        #if (FM_SAMPLE_BITS==8)
        typedef unsigned char  FMSAMPLE;
        #endif
        */

        //typedef void (* FM_TIMERHANDLER) (void* param, int c, int cnt, int clock);
        //typedef void (* FM_IRQHANDLER) (void* param, int irq);
        /* FM_TIMERHANDLER : Stop or Start timer         */
        /* int n          = chip number                  */
        /* int c          = Channel 0=TimerA,1=TimerB    */
        /* int count      = timer count (0=stop)         */
        /* doube stepTime = step time of one count (sec.)*/

        /* FM_IRQHHANDLER : IRQ level changing sense     */
        /* int n       = chip number                     */
        /* int irq     = IRQ level 0=OFF,1=ON            */

        //#if BUILD_YM2203
        /* -------------------- YM2203(OPN) Interface -------------------- */

        /*
        ** Initialize YM2203 emulator(s).
        **
        ** 'num'           is the number of virtual YM2203's to allocate
        ** 'baseclock'
        ** 'rate'          is sampling rate
        ** 'TimerHandler'  timer callback handler when timer start and clear
        ** 'IRQHandler'    IRQ callback handler when changed IRQ level
        ** return      0 = success
        */
        //void * ym2203_init(void *param, const device_config *device, int baseclock, int rate,
        //               FM_TIMERHANDLER TimerHandler,FM_IRQHANDLER IRQHandler, const ssg_callbacks *ssg);
        //void* ym2203_init(void* param, int baseclock, int rate,
        //               FM_TIMERHANDLER TimerHandler, FM_IRQHANDLER IRQHandler, const ssg_callbacks* ssg);

        //        /*
        //        ** shutdown the YM2203 emulators
        //        */
        //        void ym2203_shutdown(void* chip);

        //        /*
        //        ** reset all chip registers for YM2203 number 'num'
        //        */
        //        void ym2203_reset_chip(void* chip);

        //        /*
        //        ** update one of chip
        //        */
        //        void ym2203_update_one(void* chip, FMSAMPLE** buffer, int length);

        //        /*
        //        ** Write
        //        ** return : InterruptLevel
        //        */
        //        int ym2203_write(void* chip, int a, unsigned char v);

        //        /*
        //        ** Read
        //        ** return : InterruptLevel
        //        */
        //        unsigned char ym2203_read(void* chip, int a);

        //        /*
        //        **  Timer OverFlow
        //        */
        //        int ym2203_timer_over(void* chip, int c);

        //        /*
        //        **  State Save
        //        */
        //        void ym2203_postload(void* chip);

        //        void ym2203_set_mutemask(void* chip, UINT32 MuteMask);
        //#endif /* BUILD_YM2203 */

        //#if BUILD_YM2608
        /* -------------------- YM2608(OPNA) Interface -------------------- */
        //void * ym2608_init(void *param, const device_config *device, int baseclock, int rate,
        //               void *pcmroma,int pcmsizea,
        //               FM_TIMERHANDLER TimerHandler,FM_IRQHANDLER IRQHandler, const ssg_callbacks *ssg);
        //void* ym2608_init(void* param, int baseclock, int rate,
        //               FM_TIMERHANDLER TimerHandler, FM_IRQHANDLER IRQHandler, const ssg_callbacks* ssg);
        //void ym2608_shutdown(void* chip);
        //void ym2608_reset_chip(void* chip);
        //void ym2608_update_one(void* chip, FMSAMPLE** buffer, int length);

        //int ym2608_write(void* chip, int a, unsigned char v);
        //unsigned char ym2608_read(void* chip, int a);
        //int ym2608_timer_over(void* chip, int c);
        //void ym2608_postload(void* chip);
        //void ym2608_write_pcmrom(void* chip, UINT8 rom_id, offs_t ROMSize, offs_t DataStart,
        //                         offs_t DataLength, const UINT8* ROMData);

        //void ym2608_set_mutemask(void* chip, UINT32 MuteMask);
        //#endif /* BUILD_YM2608 */

        //#if (BUILD_YM2610 || BUILD_YM2610B)
        //        /* -------------------- YM2610(OPNB) Interface -------------------- */
        //        //void * ym2610_init(void *param, const device_config *device, int baseclock, int rate,
        //        //               void *pcmroma,int pcmasize,void *pcmromb,int pcmbsize,
        //        //               FM_TIMERHANDLER TimerHandler,FM_IRQHANDLER IRQHandler, const ssg_callbacks *ssg);
        //        void* ym2610_init(void* param, int baseclock, int rate,
        //                       FM_TIMERHANDLER TimerHandler, FM_IRQHANDLER IRQHandler, const ssg_callbacks* ssg);
        //        void ym2610_shutdown(void* chip);
        //        void ym2610_reset_chip(void* chip);
        //        void ym2610_update_one(void* chip, FMSAMPLE** buffer, int length);
        //#if BUILD_YM2610B
        //        void ym2610b_update_one(void* chip, FMSAMPLE** buffer, int length);
        //#endif /* BUILD_YM2610B */

        //        int ym2610_write(void* chip, int a, unsigned char v);
        //        unsigned char ym2610_read(void* chip, int a);
        //        int ym2610_timer_over(void* chip, int c);
        //        void ym2610_postload(void* chip);
        //        void ym2610_write_pcmrom(void* chip, UINT8 rom_id, offs_t ROMSize, offs_t DataStart,
        //                                 offs_t DataLength, const UINT8* ROMData);

        //        void ym2610_set_mutemask(void* chip, UINT32 MuteMask);
        //#endif /* (BUILD_YM2610||BUILD_YM2610B) */

        //#if (BUILD_YM2612 || BUILD_YM3438)
        //        //void * ym2612_init(void *param, const device_config *device, int baseclock, int rate,
        //        //               FM_TIMERHANDLER TimerHandler,FM_IRQHANDLER IRQHandler);
        //        void* ym2612_init(void* param, int baseclock, int rate,
        //                       FM_TIMERHANDLER TimerHandler, FM_IRQHANDLER IRQHandler);
        //        void ym2612_shutdown(void* chip);
        //        void ym2612_reset_chip(void* chip);
        //        void ym2612_update_one(void* chip, FMSAMPLE** buffer, int length);

        //        int ym2612_write(void* chip, int a, unsigned char v);
        //        unsigned char ym2612_read(void* chip, int a);
        //        int ym2612_timer_over(void* chip, int c);
        //        void ym2612_postload(void* chip);

        //        void ym2612_set_mutemask(void* chip, UINT32 MuteMask);
        //        void ym2612_setoptions(UINT8 Flags);
        //#endif /* (BUILD_YM2612||BUILD_YM3438) */









        /* include external DELTA-T unit (when needed) */
        //#if (BUILD_YM2608 || BUILD_YM2610 || BUILD_YM2610B)
        //# include "ymdeltat.h"
        //#endif

        /* shared function building option */
        private const int BUILD_OPN = 1;// (BUILD_YM2203||BUILD_YM2608||BUILD_YM2610||BUILD_YM2610B)
        private const int BUILD_OPN_PRESCALER = 1;// (BUILD_YM2203||BUILD_YM2608)


        /* globals */
        private const int TYPE_SSG = 0x01;      /* SSG support          */
        private const int TYPE_LFOPAN = 0x02;   /* OPN type LFO and PAN */
        private const int TYPE_6CH = 0x04;      /* FM 6CH / 3CH         */
        private const int TYPE_DAC = 0x08;      /* YM2612's DAC device  */
        private const int TYPE_ADPCM = 0x10;    /* two ADPCM units      */
        private const int TYPE_2610 = 0x20;     /* bogus flag to differentiate 2608 from 2610 */


        private const int TYPE_YM2203 = (TYPE_SSG);
        private const int TYPE_YM2608 = (TYPE_SSG | TYPE_LFOPAN | TYPE_6CH | TYPE_ADPCM);
        private const int TYPE_YM2610 = (TYPE_SSG | TYPE_LFOPAN | TYPE_6CH | TYPE_ADPCM | TYPE_2610);



        private const int FREQ_SH = 16;  /* 16.16 fixed point (frequency calculations) */
        private const int EG_SH = 16;  /* 16.16 fixed point (envelope generator timing) */
        private const int LFO_SH = 24;  /*  8.24 fixed point (LFO calculations)       */
        private const int TIMER_SH = 16;  /* 16.16 fixed point (timers calculations)    */

        private const int FREQ_MASK = ((1 << FREQ_SH) - 1);

        private const int ENV_BITS = 10;
        private const int ENV_LEN = (1 << ENV_BITS);
        private const double ENV_STEP = (128.0 / ENV_LEN);

        private const int MAX_ATT_INDEX = (ENV_LEN - 1); /* 1023 */
        private const int MIN_ATT_INDEX = (0);		/* 0 */

        private const int EG_ATT = 4;
        private const int EG_DEC = 3;
        private const int EG_SUS = 2;
        private const int EG_REL = 1;
        private const int EG_OFF = 0;

        private const int SIN_BITS = 10;
        private const int SIN_LEN = (1 << SIN_BITS);
        private const int SIN_MASK = (SIN_LEN - 1);

        private const int TL_RES_LEN = (256); /* 8 bits addressing (real chip) */


        //#if (FM_SAMPLE_BITS ==16)
        private const int FINAL_SH = (0);
        private const int MAXOUT = (+32767);
        private const int MINOUT = (-32768);
        //#else
        //#define FINAL_SH	(8)
        //#define MAXOUT		(+127)
        //#define MINOUT		(-128)
        //#endif


        /*  TL_TAB_LEN is calculated as:
        *   13 - sinus amplitude bits     (Y axis)
        *   2  - sinus sign bit           (Y axis)
        *   TL_RES_LEN - sinus resolution (X axis)
        */
        private const int TL_TAB_LEN = (13 * 2 * TL_RES_LEN);
        private static short[] tl_tab = new short[TL_TAB_LEN];

        private const int ENV_QUIET = (TL_TAB_LEN >> 3);

        /* sin waveform table in 'decibel' scale */
        private static ushort[] sin_tab = new ushort[SIN_LEN];

        /* sustain level table (3dB per step) */
        /* bit0, bit1, bit2, bit3, bit4, bit5, bit6 */
        /* 1,    2,    4,    8,    16,   32,   64   (value)*/
        /* 0.75, 1.5,  3,    6,    12,   24,   48   (dB)*/

        /* 0 - 15: 0, 3, 6, 9,12,15,18,21,24,27,30,33,36,39,42,93 (dB)*/
        private uint SC(int db) { return (uint)(db * (4.0 / ENV_STEP)); }
        private uint[] sl_table = new uint[16];
        //SC( 0),SC( 1),SC( 2),SC(3 ),SC(4 ),SC(5 ),SC(6 ),SC( 7),
        //SC( 8),SC( 9),SC(10),SC(11),SC(12),SC(13),SC(14),SC(31)
        private void initSl_table()
        {
            for (int i = 0; i < 16; i++) sl_table[i] = SC(i == 15 ? 31 : i);
        }


        private const int RATE_STEPS = (8);
        private byte[] eg_inc = new byte[19 * RATE_STEPS]
        {
        /*cycle:0 1  2 3  4 5  6 7*/

        /* 0 */ 0,1, 0,1, 0,1, 0,1, /* rates 00..11 0 (increment by 0 or 1) */
        /* 1 */ 0,1, 0,1, 1,1, 0,1, /* rates 00..11 1 */
        /* 2 */ 0,1, 1,1, 0,1, 1,1, /* rates 00..11 2 */
        /* 3 */ 0,1, 1,1, 1,1, 1,1, /* rates 00..11 3 */

        /* 4 */ 1,1, 1,1, 1,1, 1,1, /* rate 12 0 (increment by 1) */
        /* 5 */ 1,1, 1,2, 1,1, 1,2, /* rate 12 1 */
        /* 6 */ 1,2, 1,2, 1,2, 1,2, /* rate 12 2 */
        /* 7 */ 1,2, 2,2, 1,2, 2,2, /* rate 12 3 */

        /* 8 */ 2,2, 2,2, 2,2, 2,2, /* rate 13 0 (increment by 2) */
        /* 9 */ 2,2, 2,4, 2,2, 2,4, /* rate 13 1 */
        /*10 */ 2,4, 2,4, 2,4, 2,4, /* rate 13 2 */
        /*11 */ 2,4, 4,4, 2,4, 4,4, /* rate 13 3 */

        /*12 */ 4,4, 4,4, 4,4, 4,4, /* rate 14 0 (increment by 4) */
        /*13 */ 4,4, 4,8, 4,4, 4,8, /* rate 14 1 */
        /*14 */ 4,8, 4,8, 4,8, 4,8, /* rate 14 2 */
        /*15 */ 4,8, 8,8, 4,8, 8,8, /* rate 14 3 */

        /*16 */ 8,8, 8,8, 8,8, 8,8, /* rates 15 0, 15 1, 15 2, 15 3 (increment by 8) */
        /*17 */ 16,16,16,16,16,16,16,16, /* rates 15 2, 15 3 for attack */
        /*18 */ 0,0, 0,0, 0,0, 0,0, /* infinity rates for attack and decay(s) */
        };


        private byte O(int a) { return (byte)(a * RATE_STEPS); }

        /*note that there is no O(17) in this table - it's directly in the code */
        private byte[] eg_rate_select = null;	/* Envelope Generator rates (32 + 64 rates + 32 RKS) */
        private void initEg_rate_select()
        {
            eg_rate_select = new byte[32 + 64 + 32]
            {
                /* 32 infinite time rates */
                O(18),O(18),O(18),O(18),O(18),O(18),O(18),O(18),
                O(18),O(18),O(18),O(18),O(18),O(18),O(18),O(18),
                O(18),O(18),O(18),O(18),O(18),O(18),O(18),O(18),
                O(18),O(18),O(18),O(18),O(18),O(18),O(18),O(18),
                /* rates 00-11 */
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

                /* rate 12 */
                O( 4),O( 5),O( 6),O( 7),

                /* rate 13 */
                O( 8),O( 9),O(10),O(11),

                /* rate 14 */
                O(12),O(13),O(14),O(15),

                /* rate 15 */
                O(16),O(16),O(16),O(16),

                /* 32 dummy rates (same as 15 3) */
                O(16),O(16),O(16),O(16),O(16),O(16),O(16),O(16),
                O(16),O(16),O(16),O(16),O(16),O(16),O(16),O(16),
                O(16),O(16),O(16),O(16),O(16),O(16),O(16),O(16),
                O(16),O(16),O(16),O(16),O(16),O(16),O(16),O(16)
            };
        }

        //#undef O

        /*rate  0,    1,    2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, 15*/
        /*shift 11,  10,  9,  8,  7,  6,  5,  4,  3,  2, 1,  0,  0,  0,  0,  0 */
        /*mask  2047, 1023, 511, 255, 127, 63, 31, 15, 7,  3, 1,  0,  0,  0,  0,  0 */

        private byte O2(int a) { return (byte)(a * 1); }
        private byte[] eg_rate_shift = null;
        private void initEg_rate_shift()
        {
            eg_rate_shift = new byte[32 + 64 + 32]{	/* Envelope Generator counter shifts (32 + 64 rates + 32 RKS) */
            /* 32 infinite time rates */
            O2(0),O2(0),O2(0),O2(0),O2(0),O2(0),O2(0),O2(0),
            O2(0),O2(0),O2(0),O2(0),O2(0),O2(0),O2(0),O2(0),
            O2(0),O2(0),O2(0),O2(0),O2(0),O2(0),O2(0),O2(0),
            O2(0),O2(0),O2(0),O2(0),O2(0),O2(0),O2(0),O2(0),

            /* rates 00-11 */
            O2(11),O2(11),O2(11),O2(11),
            O2(10),O2(10),O2(10),O2(10),
            O2( 9),O2( 9),O2( 9),O2( 9),
            O2( 8),O2( 8),O2( 8),O2( 8),
            O2( 7),O2( 7),O2( 7),O2( 7),
            O2( 6),O2( 6),O2( 6),O2( 6),
            O2( 5),O2( 5),O2( 5),O2( 5),
            O2( 4),O2( 4),O2( 4),O2( 4),
            O2( 3),O2( 3),O2( 3),O2( 3),
            O2( 2),O2( 2),O2( 2),O2( 2),
            O2( 1),O2( 1),O2( 1),O2( 1),
            O2( 0),O2( 0),O2( 0),O2( 0),

            /* rate 12 */
            O2( 0),O2( 0),O2( 0),O2( 0),

            /* rate 13 */
            O2( 0),O2( 0),O2( 0),O2( 0),

            /* rate 14 */
            O2( 0),O2( 0),O2( 0),O2( 0),

            /* rate 15 */
            O2( 0),O2( 0),O2( 0),O2( 0),

            /* 32 dummy rates (same as 15 3) */
            O2( 0),O2( 0),O2( 0),O2( 0),O2( 0),O2( 0),O2( 0),O2( 0),
            O2( 0),O2( 0),O2( 0),O2( 0),O2( 0),O2( 0),O2( 0),O2( 0),
            O2( 0),O2( 0),O2( 0),O2( 0),O2( 0),O2( 0),O2( 0),O2( 0),
            O2( 0),O2( 0),O2( 0),O2( 0),O2( 0),O2( 0),O2( 0),O2( 0)

            };
        }
        //#undef O

        private byte[] dt_tab = new byte[4 * 32]{
        /* this is YM2151 and YM2612 phase increment data (in 10.10 fixed point format)*/
        /* FD=0 */
        	0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        /* FD=1 */
        	0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2,
            2, 3, 3, 3, 4, 4, 4, 5, 5, 6, 6, 7, 8, 8, 8, 8,
        /* FD=2 */
        	1, 1, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3, 4, 4, 4, 5,
            5, 6, 6, 7, 8, 8, 9,10,11,12,13,14,16,16,16,16,
        /* FD=3 */
        	2, 2, 2, 2, 2, 3, 3, 3, 4, 4, 4, 5, 5, 6, 6, 7,
            8 , 8, 9,10,11,12,13,14,16,17,19,20,22,22,22,22
        };


        /* OPN key frequency number . key code follow table */
        /* fnum higher 4bit . keycode lower 2bit */
        private byte[] opn_fktable = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 3, 3, 3, 3, 3, 3 };


        /* 8 LFO speed parameters */
        /* each value represents number of samples that one LFO level will last for */
        private uint[] lfo_samples_per_step = new uint[8] { 108, 77, 71, 67, 62, 44, 8, 5 };



        /*There are 4 different LFO AM depths available, they are:
          0 dB, 1.4 dB, 5.9 dB, 11.8 dB
          Here is how it is generated (in EG steps):

          11.8 dB = 0, 2, 4, 6, 8, 10,12,14,16...126,126,124,122,120,118,....4,2,0
           5.9 dB = 0, 1, 2, 3, 4, 5, 6, 7, 8....63, 63, 62, 61, 60, 59,.....2,1,0
           1.4 dB = 0, 0, 0, 0, 1, 1, 1, 1, 2,...15, 15, 15, 15, 14, 14,.....0,0,0

          (1.4 dB is losing precision as you can see)

          It's implemented as generator from 0..126 with step 2 then a shift
          right N times, where N is:
            8 for 0 dB
            3 for 1.4 dB
            1 for 5.9 dB
            0 for 11.8 dB
        */
        private byte[] lfo_ams_depth_shift = new byte[4] { 8, 3, 1, 0 };



        /*There are 8 different LFO PM depths available, they are:
          0, 3.4, 6.7, 10, 14, 20, 40, 80 (cents)

          Modulation level at each depth depends on F-NUMBER bits: 4,5,6,7,8,9,10
          (bits 8,9,10 = FNUM MSB from OCT/FNUM register)

          Here we store only first quarter (positive one) of full waveform.
          Full table (lfo_pm_table) containing all 128 waveforms is build
          at run (init) time.

          One value in table below represents 4 (four) basic LFO steps
          (1 PM step = 4 AM steps).

          For example:
           at LFO SPEED=0 (which is 108 samples per basic LFO step)
           one value from "lfo_pm_output" table lasts for 432 consecutive
           samples (4*108=432) and one full LFO waveform cycle lasts for 13824
           samples (32*432=13824; 32 because we store only a quarter of whole
                    waveform in the table below)
        */
        private byte[][] lfo_pm_output = new byte[7 * 8][]{ /* 7 bits meaningful (of F-NUMBER), 8 LFO output levels per one depth (out of 32), 8 LFO depths */
        /* FNUM BIT 4: 000 0001xxxx */
        /* DEPTH 0 */ new byte[]{0,   0,   0,   0,   0,   0,   0,   0},
        /* DEPTH 1 */ new byte[]{0,   0,   0,   0,   0,   0,   0,   0},
        /* DEPTH 2 */ new byte[]{0,   0,   0,   0,   0,   0,   0,   0},
        /* DEPTH 3 */ new byte[]{0,   0,   0,   0,   0,   0,   0,   0},
        /* DEPTH 4 */ new byte[]{0,   0,   0,   0,   0,   0,   0,   0},
        /* DEPTH 5 */ new byte[]{0,   0,   0,   0,   0,   0,   0,   0},
        /* DEPTH 6 */ new byte[]{0,   0,   0,   0,   0,   0,   0,   0},
        /* DEPTH 7 */ new byte[]{0,   0,   0,   0,   1,   1,   1,   1},
                      
        /* FNUM BIT 5: 000 0010xxxx */
        /* DEPTH 0 */ new byte[]{0,   0,   0,   0,   0,   0,   0,   0},
        /* DEPTH 1 */ new byte[]{0,   0,   0,   0,   0,   0,   0,   0},
        /* DEPTH 2 */ new byte[]{0,   0,   0,   0,   0,   0,   0,   0},
        /* DEPTH 3 */ new byte[]{0,   0,   0,   0,   0,   0,   0,   0},
        /* DEPTH 4 */ new byte[]{0,   0,   0,   0,   0,   0,   0,   0},
        /* DEPTH 5 */ new byte[]{0,   0,   0,   0,   0,   0,   0,   0},
        /* DEPTH 6 */ new byte[]{0,   0,   0,   0,   1,   1,   1,   1},
        /* DEPTH 7 */ new byte[]{0,   0,   1,   1,   2,   2,   2,   3},
                      
        /* FNUM BIT 6: 000 0100xxxx */
        /* DEPTH 0 */ new byte[]{0,   0,   0,   0,   0,   0,   0,   0},
        /* DEPTH 1 */ new byte[]{0,   0,   0,   0,   0,   0,   0,   0},
        /* DEPTH 2 */ new byte[]{0,   0,   0,   0,   0,   0,   0,   0},
        /* DEPTH 3 */ new byte[]{0,   0,   0,   0,   0,   0,   0,   0},
        /* DEPTH 4 */ new byte[]{0,   0,   0,   0,   0,   0,   0,   1},
        /* DEPTH 5 */ new byte[]{0,   0,   0,   0,   1,   1,   1,   1},
        /* DEPTH 6 */ new byte[]{0,   0,   1,   1,   2,   2,   2,   3},
        /* DEPTH 7 */ new byte[]{0,   0,   2,   3,   4,   4,   5,   6},
                      
        /* FNUM BIT 7: 000 1000xxxx */
        /* DEPTH 0 */ new byte[]{0,   0,   0,   0,   0,   0,   0,   0},
        /* DEPTH 1 */ new byte[]{0,   0,   0,   0,   0,   0,   0,   0},
        /* DEPTH 2 */ new byte[]{0,   0,   0,   0,   0,   0,   1,   1},
        /* DEPTH 3 */ new byte[]{0,   0,   0,   0,   1,   1,   1,   1},
        /* DEPTH 4 */ new byte[]{0,   0,   0,   1,   1,   1,   1,   2},
        /* DEPTH 5 */ new byte[]{0,   0,   1,   1,   2,   2,   2,   3},
        /* DEPTH 6 */ new byte[]{0,   0,   2,   3,   4,   4,   5,   6},
        /* DEPTH 7 */ new byte[]{0,   0,   4,   6,   8,   8, 0xa, 0xc},
                      
        /* FNUM BIT 8: 001 0000xxxx */
        /* DEPTH 0 */ new byte[]{0,   0,   0,   0,   0,   0,   0,   0},
        /* DEPTH 1 */ new byte[]{0,   0,   0,   0,   1,   1,   1,   1},
        /* DEPTH 2 */ new byte[]{0,   0,   0,   1,   1,   1,   2,   2},
        /* DEPTH 3 */ new byte[]{0,   0,   1,   1,   2,   2,   3,   3},
        /* DEPTH 4 */ new byte[]{0,   0,   1,   2,   2,   2,   3,   4},
        /* DEPTH 5 */ new byte[]{0,   0,   2,   3,   4,   4,   5,   6},
        /* DEPTH 6 */ new byte[]{0,   0,   4,   6,   8,   8, 0xa, 0xc},
        /* DEPTH 7 */ new byte[]{0,   0,   8, 0xc,0x10,0x10,0x14,0x18},
                      
        /* FNUM BIT 9: 010 0000xxxx */
        /* DEPTH 0 */ new byte[]{0,   0,   0,   0,   0,   0,   0,   0},
        /* DEPTH 1 */ new byte[]{0,   0,   0,   0,   2,   2,   2,   2},
        /* DEPTH 2 */ new byte[]{0,   0,   0,   2,   2,   2,   4,   4},
        /* DEPTH 3 */ new byte[]{0,   0,   2,   2,   4,   4,   6,   6},
        /* DEPTH 4 */ new byte[]{0,   0,   2,   4,   4,   4,   6,   8},
        /* DEPTH 5 */ new byte[]{0,   0,   4,   6,   8,   8, 0xa, 0xc},
        /* DEPTH 6 */ new byte[]{0,   0,   8, 0xc,0x10,0x10,0x14,0x18},
        /* DEPTH 7 */ new byte[]{0,   0,0x10,0x18,0x20,0x20,0x28,0x30},
                      
        /* FNUM BIT10: 100 0000xxxx */
        /* DEPTH 0 */ new byte[]{0,   0,   0,   0,   0,   0,   0,   0},
        /* DEPTH 1 */ new byte[]{0,   0,   0,   0,   4,   4,   4,   4},
        /* DEPTH 2 */ new byte[]{0,   0,   0,   4,   4,   4,   8,   8},
        /* DEPTH 3 */ new byte[]{0,   0,   4,   4,   8,   8, 0xc, 0xc},
        /* DEPTH 4 */ new byte[]{0,   0,   4,   8,   8,   8, 0xc,0x10},
        /* DEPTH 5 */ new byte[]{0,   0,   8, 0xc,0x10,0x10,0x14,0x18},
        /* DEPTH 6 */ new byte[]{0,   0,0x10,0x18,0x20,0x20,0x28,0x30},
        /* DEPTH 7 */ new byte[]{0,   0,0x20,0x30,0x40,0x40,0x50,0x60},

        };

        /* all 128 LFO PM waveforms */
        private static int[] lfo_pm_table = new int[128 * 8 * 32]; /* 128 combinations of 7 bits meaningful (of F-NUMBER), 8 LFO depths, 32 LFO output levels per one depth */





        /* register number to channel number , slot offset */
        private byte OPN_CHAN(int N) { return (byte)(N & 3); }
        private int OPN_SLOT(int N) { return ((N >> 2) & 3); }

        /* slot number */
        private const int SLOT1 = 0;
        private const int SLOT2 = 2;
        private const int SLOT3 = 1;
        private const int SLOT4 = 3;

        /* bit0 = Right enable , bit1 = Left enable */
        private const int OUTD_RIGHT = 1;
        private const int OUTD_LEFT = 2;
        private const int OUTD_CENTER = 3;


        /* save output as raw 16-bit sample */
        /* #define SAVE_SAMPLE */

        //# ifdef SAVE_SAMPLE
        //		static FILE* sample[1];
        //	#if 1	/*save to MONO file */
        //		#define SAVE_ALL_CHANNELS \
        //		{	signed int pom = lt; \
        //			fputc((unsigned short)pom&0xff,sample[0]); \
        //			fputc(((unsigned short)pom>>8)&0xff,sample[0]); \
        //		}
        //	#else	/*save to STEREO file */
        //		#define SAVE_ALL_CHANNELS \
        //		{	signed int pom = lt; \
        //			fputc((unsigned short)pom&0xff,sample[0]); \
        //			fputc(((unsigned short)pom>>8)&0xff,sample[0]); \
        //			pom = rt; \
        //			fputc((unsigned short)pom&0xff,sample[0]); \
        //			fputc(((unsigned short)pom>>8)&0xff,sample[0]); \
        //		}
        //#endif
        //#endif


        /* struct describing a single operator (SLOT) */
        private class FM_SLOT
        {

            public int[] DT;      /* detune          :dt_tab[DT] */
            public byte KSR;      /* key scale rate  :3-KSR */
            public uint ar;          /* attack rate  */
            public uint d1r;     /* decay rate   */
            public uint d2r;     /* sustain rate */
            public uint rr;          /* release rate */
            public byte ksr;      /* key scale rate  :kcode>>(3-KSR) */
            public uint mul;     /* multiple        :ML_TABLE[ML] */

            /* Phase Generator */
            public uint phase;       /* phase counter */
            public int Incr;     /* phase step */

            /* Envelope Generator */
            public byte state;        /* phase type */
            public uint tl;          /* total level: TL << 3 */
            public int volume;       /* envelope counter */
            public uint sl;          /* sustain level:sl_table[SL] */
            public uint vol_out; /* current output from EG circuit (without AM from LFO) */

            public byte eg_sh_ar; /*  (attack state) */
            public byte eg_sel_ar;    /*  (attack state) */
            public byte eg_sh_d1r;    /*  (decay state) */
            public byte eg_sel_d1r;   /*  (decay state) */
            public byte eg_sh_d2r;    /*  (sustain state) */
            public byte eg_sel_d2r;   /*  (sustain state) */
            public byte eg_sh_rr; /*  (release state) */
            public byte eg_sel_rr;    /*  (release state) */

            public byte ssg;      /* SSG-EG waveform */
            public byte ssgn;     /* SSG-EG negated output */

            public uint key;     /* 0=last key was KEY OFF, 1=KEY ON */

            /* LFO */
            public uint AMmask;      /* AM enable flag */

        }

        private class FM_CH
        {

            public FM_SLOT[] SLOT = new FM_SLOT[4];    /* four SLOTs (operators) */

            public byte ALGO;     /* algorithm */
            public byte FB;           /* feedback shift */
            public int[] op1_out = new int[2];   /* op1 output for feedback */

            public int[] connect1;    /* SLOT1 output pointer */
            public int[] connect3;    /* SLOT3 output pointer */
            public int[] connect2;    /* SLOT2 output pointer */
            public int[] connect4;    /* SLOT4 output pointer */
            public int connect1Ptr;    /* SLOT1 output pointer */
            public int connect3Ptr;    /* SLOT3 output pointer */
            public int connect2Ptr;    /* SLOT2 output pointer */
            public int connect4Ptr;    /* SLOT4 output pointer */

            public int[] mem_connect;/* where to put the delayed sample (MEM) */
            public int mem_connectPtr;/* where to put the delayed sample (MEM) */
            public int mem_value;    /* delayed sample (MEM) value */

            public int pms;      /* channel PMS */
            public byte ams;      /* channel AMS */

            public uint fc;          /* fnum,blk:adjusted to sample rate */
            public byte kcode;        /* key code:                        */
            public uint block_fnum;  /* current blk/fnum value for this slot (can be different betweeen slots of one channel in 3slot mode) */
            public byte Muted;
        }


        private class FM_ST
        {
            //const device_config *device;
            public FM_base param;                /* this chip parameter  */
            public int clock;              /* master clock  (Hz)   */
            public int rate;               /* sampling rate (Hz)   */
            public double freqbase;            /* frequency base       */
            public int timer_prescaler;    /* timer prescaler      */
            //#if FM_BUSY_FLAG_SUPPORT
            //public TIME_TYPE busy_expiry_time;	/* expiry time of the busy status */
            //#endif
            public byte address;          /* address register     */
            public byte irq;              /* interrupt level      */
            public byte irqmask;          /* irq mask             */
            public byte status;               /* status flag          */
            public uint mode;                /* mode  CSM / 3SLOT    */
            public byte prescaler_sel;        /* prescaler selector   */
            public byte fn_h;             /* freq latch           */
            public int TA;                   /* timer a              */
            public int TAC;              /* timer a counter      */
            public byte TB;                   /* timer b              */
            public int TBC;              /* timer b counter      */
            /* local time tables */
            public int[][] dt_tab = new int[8][] {
                new int[32], new int[32], new int[32], new int[32],
                new int[32], new int[32], new int[32], new int[32] };        /* DeTune table         */
            /* Extention Timer and IRQ handler */
            public delegate void dlgFM_TIMERHANDLER(object o, int a, int b, int c);
            public dlgFM_TIMERHANDLER timer_handler;
            public delegate void dlgFM_IRQHANDLER(FM_base o, int i);
            public dlgFM_IRQHANDLER IRQ_Handler;
            public _ssg_callbacks SSG;
        }



        ///***********************************************************/
        ///* OPN unit                                                */
        ///***********************************************************/

        ///* OPN 3slot struct */
        private class FM_3SLOT
        {
            public uint[] fc = new uint[3];           /* fnum3,blk3: calculated */
            public byte fn_h;         /* freq3 latch */
            public byte[] kcode = new byte[3];     /* key code */
            public uint[] block_fnum = new uint[3]; /* current fnum value for this slot (can be different betweeen slots of one channel in 3slot mode) */
        }

        /* OPN/A/B common state */
        private class FM_OPN
        {
            public byte type;         /* chip type */
            public FM_ST ST;               /* general state */
            public FM_3SLOT SL3;           /* 3 slot mode state */
            public FM_CH[] P_CH;            /* pointer of CH */
            public uint[] pan = new uint[6 * 2];    /* fm channels output masks (0xffffffff = enable) */

            public uint eg_cnt;          /* global envelope generator counter */
            public uint eg_timer;        /* global envelope generator counter works at frequency = chipclock/64/3 */
            public uint eg_timer_add;    /* step of eg_timer */
            public uint eg_timer_overflow;/* envelope generator timer overlfows every 3 samples (on real chip) */


            /* there are 2048 FNUMs that can be generated using FNUM/BLK registers
                but LFO works with one more bit of a precision so we really need 4096 elements */

            public uint[] fn_table = new uint[4096];  /* fnumber.increment counter */
            public uint fn_max;    /* maximal phase increment (used for phase overflow) */

            /* LFO */
            public uint LFO_AM;          /* runtime LFO calculations helper */
            public int LFO_PM;           /* runtime LFO calculations helper */

            public uint lfo_cnt;
            public uint lfo_inc;

            public uint[] lfo_freq = new uint[8]; /* LFO FREQ table */

            public int m2, c1, c2;       /* Phase Modulation input for operators 2,3,4 */
            public int mem;          /* one sample delay memory */

            public int[] out_fm = new int[8];      /* outputs of working channels */

            //#if (BUILD_YM2608 || BUILD_YM2610 || BUILD_YM2610B)
            public int[] out_adpcm = new int[4];    /* channel output NONE,LEFT,RIGHT or CENTER for YM2608/YM2610 ADPCM */
            public int[] out_delta = new int[4];    /* channel output NONE,LEFT,RIGHT or CENTER for YM2608/YM2610 DELTAT*/
            //#endif
        }



        /* log output level */
        private const int LOG_ERR = 3;      /* ERROR       */
        private const int LOG_WAR = 2;   /* WARNING     */
        private const int LOG_INF = 1;    /* INFORMATION */
        private const int LOG_LEVEL = LOG_INF;

        //# ifndef __RAINE__
        //#define LOG(n,x) do { if( (n)>=LOG_LEVEL ) logerror x; } while (0)
        //#endif

        /* limitter */
        private void Limit(ref int val, int max, int min)
        {
            if (val > max) val = max;
            else if (val < min) val = min;
        }


        /* status set and IRQ handling */
        //INLINE
        private void FM_STATUS_SET(FM_ST ST, int flag)
        {
            /* set status flag */
            ST.status |= (byte)flag;
            if (ST.irq == 0 && ((ST.status & ST.irqmask) != 0))
            {
                ST.irq = 1;
                /* callback user interrupt handler (IRQ is OFF to ON) */
                if (ST.IRQ_Handler != null) ST.IRQ_Handler(ST.param, 1);
            }
        }

        /* status reset and IRQ handling */
        //INLINE
        private void FM_STATUS_RESET(FM_ST ST, int flag)
        {
            /* reset status flag */
            ST.status &= (byte)~flag;
            if ((ST.irq != 0) && ((ST.status & ST.irqmask) == 0))
            {
                ST.irq = 0;
                /* callback user interrupt handler (IRQ is ON to OFF) */
                if (ST.IRQ_Handler != null) ST.IRQ_Handler(ST.param, 0);
            }
        }

        /* IRQ mask set */
        //INLINE
        private void FM_IRQMASK_SET(FM_ST ST, int flag)
        {
            ST.irqmask = (byte)flag;
            /* IRQ handling check */
            FM_STATUS_SET(ST, 0);
            FM_STATUS_RESET(ST, 0);
        }

        /* OPN Mode Register Write */
        //INLINE
        private void set_timers(FM_ST ST, FM_base n, int v)
        {
            /* b7 = CSM MODE */
            /* b6 = 3 slot mode */
            /* b5 = reset b */
            /* b4 = reset a */
            /* b3 = timer enable b */
            /* b2 = timer enable a */
            /* b1 = load b */
            /* b0 = load a */
            ST.mode = (uint)v;

            /* reset Timer b flag */
            if ((v & 0x20) != 0)
                FM_STATUS_RESET(ST, 0x02);
            /* reset Timer a flag */
            if ((v & 0x10) != 0)
                FM_STATUS_RESET(ST, 0x01);
            /* load b */
            if ((v & 0x02) != 0)
            {
                if (ST.TBC == 0)
                {
                    ST.TBC = (256 - ST.TB) << 4;
                    /* External timer handler */
                    if (ST.timer_handler != null) ST.timer_handler(n, 1, ST.TBC * ST.timer_prescaler, ST.clock);
                }
            }
            else
            {   /* stop timer b */
                if (ST.TBC != 0)
                {
                    ST.TBC = 0;
                    if (ST.timer_handler != null) ST.timer_handler(n, 1, 0, ST.clock);
                }
            }
            /* load a */
            if ((v & 0x01) != 0)
            {
                if (ST.TAC == 0)
                {
                    ST.TAC = (1024 - ST.TA);
                    /* External timer handler */
                    if (ST.timer_handler != null) ST.timer_handler(n, 0, ST.TAC * ST.timer_prescaler, ST.clock);
                }
            }
            else
            {   /* stop timer a */
                if (ST.TAC != 0)
                {
                    ST.TAC = 0;
                    if (ST.timer_handler != null) ST.timer_handler(n, 0, 0, ST.clock);
                }
            }
        }


        /* Timer A Overflow */
        //INLINE
        private void TimerAOver(FM_ST ST)
        {
            /* set status (if enabled) */
            if ((ST.mode & 0x04) != 0) FM_STATUS_SET(ST, 0x01);
            /* clear or reload the counter */
            ST.TAC = (1024 - ST.TA);
            if (ST.timer_handler != null) ST.timer_handler(ST.param, 0, ST.TAC * ST.timer_prescaler, ST.clock);
        }
        /* Timer B Overflow */
        //INLINE
        private void TimerBOver(FM_ST ST)
        {
            /* set status (if enabled) */
            if ((ST.mode & 0x08) != 0) FM_STATUS_SET(ST, 0x02);
            /* clear or reload the counter */
            ST.TBC = (256 - ST.TB) << 4;
            if (ST.timer_handler != null) ST.timer_handler(ST.param, 1, ST.TBC * ST.timer_prescaler, ST.clock);
        }


        //#if FM_INTERNAL_TIMER
        /* ----- internal timer mode , update timer */

        /* ---------- calculate timer A ---------- */
        private void INTERNAL_TIMER_A(FM_OPN OPN, FM_ST ST, FM_CH CSM_CH)
        {
            if (ST.TAC != 0 && (ST.timer_handler == null))
                if ((ST.TAC -= (int)(ST.freqbase * 4096)) <= 0)
                {
                    TimerAOver(ST);
                    /* CSM mode total level latch and auto key on */
                    if ((ST.mode & 0x80) != 0)
                        CSMKeyControll(OPN.type, CSM_CH);
                }
        }
        /* ---------- calculate timer B ---------- */
        private void INTERNAL_TIMER_B(FM_ST ST, int step)
        {
            if (ST.TBC != 0 && ST.timer_handler == null)
                if (((ST).TBC -= (int)((ST).freqbase * 4096 * step)) <= 0)
                    TimerBOver(ST);
        }

        //#else /* FM_INTERNAL_TIMER */
        /* external timer mode */
        //#define INTERNAL_TIMER_A(ST,CSM_CH)
        //#define INTERNAL_TIMER_B(ST,step)
        //#endif /* FM_INTERNAL_TIMER */



        //#if FM_BUSY_FLAG_SUPPORT
        //private void FM_BUSY_CLEAR(FM_ST ST) { ST.busy_expiry_time = UNDEFINED_TIME; }
        ////INLINE
        //private byte FM_STATUS_FLAG(FM_ST ST)
        //{
        //    if (COMPARE_TIMES(ST.busy_expiry_time, UNDEFINED_TIME) != 0)
        //    {
        //        if (COMPARE_TIMES(ST.busy_expiry_time, FM_GET_TIME_NOW(ST.device.machine)) > 0)
        //            return (byte)(ST.status | 0x80);    /* with busy */
        //        /* expire */
        //        FM_BUSY_CLEAR(ST);
        //    }
        //    return ST.status;
        //}

        ////INLINE
        //private void FM_BUSY_SET(FM_ST ST, int busyclock)
        //{
        //    TIME_TYPE expiry_period = MULTIPLY_TIME_BY_INT(ATTOTIME_IN_HZ(ST.clock), busyclock * ST.timer_prescaler);
        //    ST.busy_expiry_time = ADD_TIMES(FM_GET_TIME_NOW(ST.device.machine), expiry_period);
        //}
        //#else
        private byte FM_STATUS_FLAG(FM_ST ST) { return ST.status; }
        private void FM_BUSY_SET(FM_ST ST, int bclock) { }
        private void FM_BUSY_CLEAR(FM_ST ST) { }
        //#endif




        //INLINE
        private void FM_KEYON(byte _type, FM_CH CH, int s)
        {
            FM_SLOT SLOT = CH.SLOT[s];
            if (SLOT.key == 0)
            {
                SLOT.key = 1;
                SLOT.phase = 0;        /* restart Phase Generator */
                SLOT.ssgn = (byte)((SLOT.ssg & 0x04) >> 1);
                SLOT.state = EG_ATT;
            }
        }

        //INLINE
        private void FM_KEYOFF(FM_CH CH, int s)
        {
            FM_SLOT SLOT = CH.SLOT[s];
            if (SLOT.key != 0)
            {
                SLOT.key = 0;
                if (SLOT.state > EG_REL)
                    SLOT.state = EG_REL;/* phase . Release */
            }
        }

        /* set algorithm connection */
        private static void setup_connection(FM_OPN OPN, FM_CH CH, int ch)
        {
            int carrier = OPN.out_fm[ch];

            int[] om1 = CH.connect1;
            int om1Ptr = CH.connect1Ptr;
            int[] om2 = CH.connect3;
            int om2Ptr = CH.connect3Ptr;
            int[] oc1 = CH.connect2;
            int oc1Ptr = CH.connect2Ptr;

            int[] memc = CH.mem_connect;
            int memcPtr = CH.mem_connectPtr;

            switch (CH.ALGO)
            {
                case 0:
                    /* M1---C1---MEM---M2---C2---OUT */
                    om1[om1Ptr] = OPN.c1;
                    oc1[oc1Ptr] = OPN.mem;
                    om2[om2Ptr] = OPN.c2;
                    memc[memcPtr] = OPN.m2;
                    break;
                case 1:
                    /* M1------+-MEM---M2---C2---OUT */
                    /*      C1-+                     */
                    om1[om1Ptr] = OPN.mem;
                    oc1[oc1Ptr] = OPN.mem;
                    om2[om2Ptr] = OPN.c2;
                    memc[memcPtr] = OPN.m2;
                    break;
                case 2:
                    /* M1-----------------+-C2---OUT */
                    /*      C1---MEM---M2-+          */
                    om1[om1Ptr] = OPN.c2;
                    oc1[oc1Ptr] = OPN.mem;
                    om2[om2Ptr] = OPN.c2;
                    memc[memcPtr] = OPN.m2;
                    break;
                case 3:
                    /* M1---C1---MEM------+-C2---OUT */
                    /*                 M2-+          */
                    om1[om1Ptr] = OPN.c1;
                    oc1[oc1Ptr] = OPN.mem;
                    om2[om2Ptr] = OPN.c2;
                    memc[memcPtr] = OPN.c2;
                    break;
                case 4:
                    /* M1---C1-+-OUT */
                    /* M2---C2-+     */
                    /* MEM: not used */
                    om1[om1Ptr] = OPN.c1;
                    oc1[oc1Ptr] = carrier;
                    om2[om2Ptr] = OPN.c2;
                    memc[memcPtr] = OPN.mem;  /* store it anywhere where it will not be used */
                    break;
                case 5:
                    /*    +----C1----+     */
                    /* M1-+-MEM---M2-+-OUT */
                    /*    +----C2----+     */
                    om1[om1Ptr] = 0;   /* special mark */
                    oc1[oc1Ptr] = carrier;
                    om2[om2Ptr] = carrier;
                    memc[memcPtr] = OPN.m2;
                    break;
                case 6:
                    /* M1---C1-+     */
                    /*      M2-+-OUT */
                    /*      C2-+     */
                    /* MEM: not used */
                    om1[om1Ptr] = OPN.c1;
                    oc1[oc1Ptr] = carrier;
                    om2[om2Ptr] = carrier;
                    memc[memcPtr] = OPN.mem;  /* store it anywhere where it will not be used */
                    break;
                case 7:
                    /* M1-+     */
                    /* C1-+-OUT */
                    /* M2-+     */
                    /* C2-+     */
                    /* MEM: not used*/
                    om1[om1Ptr] = carrier;
                    oc1[oc1Ptr] = carrier;
                    om2[om2Ptr] = carrier;
                    memc[memcPtr] = OPN.mem;  /* store it anywhere where it will not be used */
                    break;
            }

            CH.connect4[CH.connect4Ptr] = carrier;
        }

        /* set detune & multiple */
        //INLINE
        private void set_det_mul(FM_ST ST, FM_CH CH, FM_SLOT SLOT, int v)
        {
            SLOT.mul = (v & 0x0f) != 0 ? (uint)((v & 0x0f) * 2) : 1;
            SLOT.DT = ST.dt_tab[(v >> 4) & 7];
            CH.SLOT[SLOT1].Incr = -1;
        }

        /* set total level */
        //INLINE
        private void set_tl(FM_CH CH, FM_SLOT SLOT, int v)
        {
            SLOT.tl = (uint)((v & 0x7f) << (ENV_BITS - 7)); /* 7bit TL */
        }

        /* set attack rate & key scale  */
        //INLINE
        private void set_ar_ksr(byte type, FM_CH CH, FM_SLOT SLOT, int v)
        {
            byte old_KSR = SLOT.KSR;

            SLOT.ar = (v & 0x1f) != 0 ? (uint)(32 + ((v & 0x1f) << 1)) : 0;

            SLOT.KSR = (byte)(3 - (v >> 6));
            if (SLOT.KSR != old_KSR)
            {
                CH.SLOT[SLOT1].Incr = -1;
            }

            /* refresh Attack rate */
            if ((SLOT.ar + SLOT.ksr) < 32 + 62)
            {
                SLOT.eg_sh_ar = eg_rate_shift[SLOT.ar + SLOT.ksr];
                SLOT.eg_sel_ar = eg_rate_select[SLOT.ar + SLOT.ksr];
            }
            else
            {
                SLOT.eg_sh_ar = 0;
                SLOT.eg_sel_ar = 17 * RATE_STEPS;
            }
        }

        /* set decay rate */
        //INLINE
        private void set_dr(byte type, FM_SLOT SLOT, int v)
        {
            SLOT.d1r = (v & 0x1f) != 0 ? (uint)(32 + ((v & 0x1f) << 1)) : 0;

            SLOT.eg_sh_d1r = eg_rate_shift[SLOT.d1r + SLOT.ksr];
            SLOT.eg_sel_d1r = eg_rate_select[SLOT.d1r + SLOT.ksr];
        }

        /* set sustain rate */
        //INLINE
        private void set_sr(byte type, FM_SLOT SLOT, int v)
        {
            SLOT.d2r = (v & 0x1f) != 0 ? (uint)(32 + ((v & 0x1f) << 1)) : 0;

            SLOT.eg_sh_d2r = eg_rate_shift[SLOT.d2r + SLOT.ksr];
            SLOT.eg_sel_d2r = eg_rate_select[SLOT.d2r + SLOT.ksr];
        }

        /* set release rate */
        //INLINE
        private void set_sl_rr(byte type, FM_SLOT SLOT, int v)
        {
            SLOT.sl = sl_table[v >> 4];

            SLOT.rr = (uint)(34 + ((v & 0x0f) << 2));

            SLOT.eg_sh_rr = eg_rate_shift[SLOT.rr + SLOT.ksr];
            SLOT.eg_sel_rr = eg_rate_select[SLOT.rr + SLOT.ksr];
        }



        //INLINE
        private int op_calc(uint phase, ushort env, short pm)
        {
            uint p;

            p = (uint)((env << 3) + sin_tab[(((short)((phase & ~FREQ_MASK) + (pm << 15))) >> FREQ_SH) & SIN_MASK]);

            if (p >= TL_TAB_LEN)
                return 0;
            return tl_tab[p];
        }

        //INLINE
        private short op_calc1(uint phase, ushort env, short pm)
        {
            uint p;

            p = (uint)((env << 3) + sin_tab[(((short)((phase & ~FREQ_MASK) + pm)) >> FREQ_SH) & SIN_MASK]);

            if (p >= TL_TAB_LEN)
                return 0;
            return tl_tab[p];
        }

        /* advance LFO to next sample */
        //INLINE
        private void advance_lfo(FM_OPN OPN)
        {
            byte pos;

            if (OPN.lfo_inc != 0)   /* LFO enabled ? */
            {
                OPN.lfo_cnt += OPN.lfo_inc;

                pos = (byte)((OPN.lfo_cnt >> LFO_SH) & 127);


                /* update AM when LFO output changes */

                /* actually I can't optimize is this way without rewriting chan_calc()
                to use chip.lfo_am instead of global lfo_am */
                {

                    /* triangle */
                    /* AM: 0 to 126 step +2, 126 to 0 step -2 */
                    if (pos < 64)
                        OPN.LFO_AM = (uint)((pos & 63) * 2);
                    else
                        OPN.LFO_AM = (uint)(126 - ((pos & 63) * 2));
                }

                /* PM works with 4 times slower clock */
                pos >>= 2;
                /* update PM when LFO output changes */
                /*if (prev_pos != pos)*/ /* can't use global lfo_pm for this optimization, must be chip.lfo_pm instead*/
                {
                    OPN.LFO_PM = pos;
                }

            }
            else
            {
                OPN.LFO_AM = 0;
                OPN.LFO_PM = 0;
            }
        }

        /* changed from INLINE to static here to work around gcc 4.2.1 codegen bug */
        private void advance_eg_channel(FM_OPN OPN, FM_SLOT[] slot)
        {
            ushort _out;
            ushort swap_flag = 0;
            ushort i;

            int slotPtr = 0;
            i = 4; /* four operators per channel */
            do
            {
                FM_SLOT SLOT = slot[slotPtr];

                /* reset SSG-EG swap flag */
                swap_flag = 0;

                switch (SLOT.state)
                {
                    case EG_ATT:        /* attack phase */
                        if ((OPN.eg_cnt & ((1 << SLOT.eg_sh_ar) - 1)) == 0)
                        {
                            SLOT.volume += (~SLOT.volume *
                                              eg_inc[SLOT.eg_sel_ar + ((OPN.eg_cnt >> SLOT.eg_sh_ar) & 7)]
                                            ) >> 4;

                            if (SLOT.volume <= MIN_ATT_INDEX)
                            {
                                SLOT.volume = MIN_ATT_INDEX;
                                SLOT.state = EG_DEC;
                            }
                        }
                        break;

                    case EG_DEC:    /* decay phase */
                        {
                            if ((SLOT.ssg & 0x08) != 0)   /* SSG EG type envelope selected */
                            {
                                if ((OPN.eg_cnt & ((1 << SLOT.eg_sh_d1r) - 1)) == 0)
                                {
                                    SLOT.volume += 4 * eg_inc[SLOT.eg_sel_d1r + ((OPN.eg_cnt >> SLOT.eg_sh_d1r) & 7)];

                                    if (SLOT.volume >= (int)(SLOT.sl))
                                        SLOT.state = EG_SUS;
                                }
                            }
                            else
                            {
                                if ((OPN.eg_cnt & ((1 << SLOT.eg_sh_d1r) - 1)) == 0)
                                {
                                    SLOT.volume += eg_inc[SLOT.eg_sel_d1r + ((OPN.eg_cnt >> SLOT.eg_sh_d1r) & 7)];

                                    if (SLOT.volume >= (int)(SLOT.sl))
                                        SLOT.state = EG_SUS;
                                }
                            }
                        }
                        break;

                    case EG_SUS:    /* sustain phase */
                        if ((SLOT.ssg & 0x08) != 0)   /* SSG EG type envelope selected */
                        {
                            if ((OPN.eg_cnt & ((1 << SLOT.eg_sh_d2r) - 1)) == 0)
                            {

                                SLOT.volume += 4 * eg_inc[SLOT.eg_sel_d2r + ((OPN.eg_cnt >> SLOT.eg_sh_d2r) & 7)];

                                if (SLOT.volume >= ENV_QUIET)
                                {
                                    SLOT.volume = MAX_ATT_INDEX;

                                    if ((SLOT.ssg & 0x01) != 0)   /* bit 0 = hold */
                                    {
                                        if ((SLOT.ssgn & 1) != 0) /* have we swapped once ??? */
                                        {
                                            /* yes, so do nothing, just hold current level */
                                        }
                                        else
                                            swap_flag = (ushort)((SLOT.ssg & 0x02) | 1); /* bit 1 = alternate */

                                    }
                                    else
                                    {
                                        /* same as KEY-ON operation */

                                        /* restart of the Phase Generator should be here */
                                        SLOT.phase = 0;

                                        {
                                            /* phase . Attack */
                                            SLOT.volume = 511;
                                            SLOT.state = EG_ATT;
                                        }

                                        swap_flag = (ushort)(SLOT.ssg & 0x02); /* bit 1 = alternate */
                                    }
                                }
                            }
                        }
                        else
                        {
                            if ((OPN.eg_cnt & ((1 << SLOT.eg_sh_d2r) - 1)) == 0)
                            {
                                SLOT.volume += eg_inc[SLOT.eg_sel_d2r + ((OPN.eg_cnt >> SLOT.eg_sh_d2r) & 7)];

                                if (SLOT.volume >= MAX_ATT_INDEX)
                                {
                                    SLOT.volume = MAX_ATT_INDEX;
                                    /* do not change SLOT.state (verified on real chip) */
                                }
                            }

                        }
                        break;

                    case EG_REL:    /* release phase */
                        if ((OPN.eg_cnt & ((1 << SLOT.eg_sh_rr) - 1)) == 0)
                        {
                            /* SSG-EG affects Release phase also (Nemesis) */
                            SLOT.volume += eg_inc[SLOT.eg_sel_rr + ((OPN.eg_cnt >> SLOT.eg_sh_rr) & 7)];

                            if (SLOT.volume >= MAX_ATT_INDEX)
                            {
                                SLOT.volume = MAX_ATT_INDEX;
                                SLOT.state = EG_OFF;
                            }
                        }
                        break;

                }


                _out = (ushort)((uint)SLOT.volume);

                /* negate output (changes come from alternate bit, init comes from attack bit) */
                if ((SLOT.ssg & 0x08) != 0 && (SLOT.ssgn & 2) != 0 && (SLOT.state > EG_REL))
                    _out ^= MAX_ATT_INDEX;

                /* we need to store the result here because we are going to change ssgn
                    in next instruction */
                SLOT.vol_out = _out + SLOT.tl;

                /* reverse SLOT inversion flag */
                SLOT.ssgn ^= (byte)swap_flag;

                slotPtr++;
                i--;
            } while (i != 0);

        }



        private ushort volume_calc(FM_SLOT OP, uint AM) { return (ushort)(OP.vol_out + (AM & OP.AMmask)); }

        //INLINE
        private void update_phase_lfo_slot(FM_OPN OPN, FM_SLOT SLOT, int pms, uint block_fnum)
        {
            uint fnum_lfo = ((block_fnum & 0x7f0) >> 4) * 32 * 8;
            int lfo_fn_table_index_offset = lfo_pm_table[fnum_lfo + pms + OPN.LFO_PM];

            if (lfo_fn_table_index_offset != 0)    /* LFO phase modulation active */
            {
                byte blk;
                uint fn;
                int kc, fc;

                block_fnum = (uint)(block_fnum * 2 + lfo_fn_table_index_offset);

                blk = (byte)((block_fnum & 0x7000) >> 12);
                fn = block_fnum & 0xfff;

                /* keyscale code */
                kc = (blk << 2) | opn_fktable[fn >> 8];

                /* phase increment counter */
                fc = (int)((OPN.fn_table[fn] >> (7 - blk)) + SLOT.DT[kc]);

                /* detects frequency overflow (credits to Nemesis) */
                if (fc < 0) fc += (int)OPN.fn_max;

                /* update phase */
                SLOT.phase += (uint)((fc * SLOT.mul) >> 1);
            }
            else    /* LFO phase modulation  = zero */
            {
                SLOT.phase += (uint)SLOT.Incr;
            }
        }

        //INLINE
        private void update_phase_lfo_channel(FM_OPN OPN, FM_CH CH)
        {
            uint block_fnum = CH.block_fnum;

            uint fnum_lfo = ((block_fnum & 0x7f0) >> 4) * 32 * 8;
            int lfo_fn_table_index_offset = lfo_pm_table[fnum_lfo + CH.pms + OPN.LFO_PM];

            if (lfo_fn_table_index_offset != 0)    /* LFO phase modulation active */
            {
                byte blk;
                uint fn;
                int kc, fc, finc;

                block_fnum = (uint)(block_fnum * 2 + lfo_fn_table_index_offset);

                blk = (byte)((block_fnum & 0x7000) >> 12);
                fn = block_fnum & 0xfff;

                /* keyscale code */
                kc = (blk << 2) | opn_fktable[fn >> 8];

                /* phase increment counter */
                fc = (int)(OPN.fn_table[fn] >> (7 - blk));

                /* detects frequency overflow (credits to Nemesis) */
                finc = fc + CH.SLOT[SLOT1].DT[kc];

                if (finc < 0) finc += (int)OPN.fn_max;
                CH.SLOT[SLOT1].phase += (uint)((finc * CH.SLOT[SLOT1].mul) >> 1);

                finc = fc + CH.SLOT[SLOT2].DT[kc];
                if (finc < 0) finc += (int)OPN.fn_max;
                CH.SLOT[SLOT2].phase += (uint)((finc * CH.SLOT[SLOT2].mul) >> 1);

                finc = fc + CH.SLOT[SLOT3].DT[kc];
                if (finc < 0) finc += (int)OPN.fn_max;
                CH.SLOT[SLOT3].phase += (uint)((finc * CH.SLOT[SLOT3].mul) >> 1);

                finc = fc + CH.SLOT[SLOT4].DT[kc];
                if (finc < 0) finc += (int)OPN.fn_max;
                CH.SLOT[SLOT4].phase += (uint)((finc * CH.SLOT[SLOT4].mul) >> 1);
            }
            else    /* LFO phase modulation  = zero */
            {
                CH.SLOT[SLOT1].phase += (uint)(CH.SLOT[SLOT1].Incr);
                CH.SLOT[SLOT2].phase += (uint)(CH.SLOT[SLOT2].Incr);
                CH.SLOT[SLOT3].phase += (uint)(CH.SLOT[SLOT3].Incr);
                CH.SLOT[SLOT4].phase += (uint)(CH.SLOT[SLOT4].Incr);
            }
        }

        //INLINE
        private void chan_calc(FM_OPN OPN, FM_CH CH, int chnum)
        {
            ushort eg_out;

            uint AM = OPN.LFO_AM >> CH.ams;

            if (CH.Muted != 0)
                return;


            OPN.m2 = OPN.c1 = OPN.c2 = OPN.mem = 0;

            CH.mem_connect[CH.mem_connectPtr] = CH.mem_value;   /* restore delayed sample (MEM) value to m2 or c2 */

            eg_out = volume_calc(CH.SLOT[SLOT1], AM);
            {
                int _out = CH.op1_out[0] + CH.op1_out[1];
                CH.op1_out[0] = CH.op1_out[1];

                if (CH.connect1[CH.connect1Ptr] == 0)
                {
                    /* algorithm 5  */
                    OPN.mem = OPN.c1 = OPN.c2 = CH.op1_out[0];
                }
                else
                {
                    /* other algorithms */
                    CH.connect1[CH.connect1Ptr] += CH.op1_out[0];
                }

                CH.op1_out[1] = 0;
                if (eg_out < ENV_QUIET) /* SLOT 1 */
                {
                    if (CH.FB == 0)
                        _out = 0;

                    CH.op1_out[1] = op_calc1(CH.SLOT[SLOT1].phase, eg_out, (short)(_out << CH.FB));
                }
            }

            eg_out = volume_calc(CH.SLOT[SLOT3], AM);
            if (eg_out < ENV_QUIET)     /* SLOT 3 */
                CH.connect3[CH.connect3Ptr] += op_calc(CH.SLOT[SLOT3].phase, eg_out, (short)OPN.m2);

            eg_out = volume_calc(CH.SLOT[SLOT2], AM);
            if (eg_out < ENV_QUIET)     /* SLOT 2 */
                CH.connect2[CH.connect2Ptr] += op_calc(CH.SLOT[SLOT2].phase, eg_out, (short)OPN.c1);

            eg_out = volume_calc(CH.SLOT[SLOT4], AM);
            if (eg_out < ENV_QUIET)     /* SLOT 4 */
                CH.connect4[CH.connect4Ptr] += op_calc(CH.SLOT[SLOT4].phase, eg_out, (short)OPN.c2);


            /* store current MEM */
            CH.mem_value = OPN.mem;

            /* update phase counters AFTER output calculations */
            if (CH.pms != 0)
            {
                /* add support for 3 slot mode */
                if ((OPN.ST.mode & 0xC0) != 0 && (chnum == 2))
                {
                    update_phase_lfo_slot(OPN, CH.SLOT[SLOT1], CH.pms, OPN.SL3.block_fnum[1]);
                    update_phase_lfo_slot(OPN, CH.SLOT[SLOT2], CH.pms, OPN.SL3.block_fnum[2]);
                    update_phase_lfo_slot(OPN, CH.SLOT[SLOT3], CH.pms, OPN.SL3.block_fnum[0]);
                    update_phase_lfo_slot(OPN, CH.SLOT[SLOT4], CH.pms, CH.block_fnum);
                }
                else update_phase_lfo_channel(OPN, CH);
            }
            else    /* no LFO phase modulation */
            {
                CH.SLOT[SLOT1].phase += (uint)CH.SLOT[SLOT1].Incr;
                CH.SLOT[SLOT2].phase += (uint)CH.SLOT[SLOT2].Incr;
                CH.SLOT[SLOT3].phase += (uint)CH.SLOT[SLOT3].Incr;
                CH.SLOT[SLOT4].phase += (uint)CH.SLOT[SLOT4].Incr;
            }
        }

        /* update phase increment and envelope generator */
        //INLINE
        private void refresh_fc_eg_slot(FM_OPN OPN, FM_SLOT SLOT, int fc, int kc)
        {
            int ksr = kc >> SLOT.KSR;

            fc += SLOT.DT[kc];

            /* detects frequency overflow (credits to Nemesis) */
            if (fc < 0) fc += (int)OPN.fn_max;

            /* (frequency) phase increment counter */
            SLOT.Incr = (int)((fc * SLOT.mul) >> 1);

            if (SLOT.ksr != ksr)
            {
                SLOT.ksr = (byte)ksr;

                /* calculate envelope generator rates */
                if ((SLOT.ar + SLOT.ksr) < 32 + 62)
                {
                    SLOT.eg_sh_ar = eg_rate_shift[SLOT.ar + SLOT.ksr];
                    SLOT.eg_sel_ar = eg_rate_select[SLOT.ar + SLOT.ksr];
                }
                else
                {
                    SLOT.eg_sh_ar = 0;
                    SLOT.eg_sel_ar = 17 * RATE_STEPS;
                }

                SLOT.eg_sh_d1r = eg_rate_shift[SLOT.d1r + SLOT.ksr];
                SLOT.eg_sh_d2r = eg_rate_shift[SLOT.d2r + SLOT.ksr];
                SLOT.eg_sh_rr = eg_rate_shift[SLOT.rr + SLOT.ksr];

                SLOT.eg_sel_d1r = eg_rate_select[SLOT.d1r + SLOT.ksr];
                SLOT.eg_sel_d2r = eg_rate_select[SLOT.d2r + SLOT.ksr];
                SLOT.eg_sel_rr = eg_rate_select[SLOT.rr + SLOT.ksr];
            }
        }

        /* update phase increment counters */
        /* Changed from INLINE to static to work around gcc 4.2.1 codegen bug */
        private void refresh_fc_eg_chan(FM_OPN OPN, FM_CH CH)
        {
            if (CH.SLOT[SLOT1].Incr == -1)
            {
                int fc = (int)CH.fc;
                int kc = CH.kcode;
                refresh_fc_eg_slot(OPN, CH.SLOT[SLOT1], fc, kc);
                refresh_fc_eg_slot(OPN, CH.SLOT[SLOT2], fc, kc);
                refresh_fc_eg_slot(OPN, CH.SLOT[SLOT3], fc, kc);
                refresh_fc_eg_slot(OPN, CH.SLOT[SLOT4], fc, kc);
            }
        }

        /* initialize time tables */
        private void init_timetables(FM_ST ST, byte[] dttable)
        {

            int i, d;
            double rate;

            //#if 0
            //        	logerror("FM.C: samplerate=%8i chip clock=%8i  freqbase=%f  \n",
            //ST.rate, ST.clock, ST.freqbase );
            //#endif

            /* DeTune table */
            for (d = 0; d <= 3; d++)
            {
                for (i = 0; i <= 31; i++)
                {
                    rate = ((double)dttable[d * 32 + i]) * SIN_LEN * ST.freqbase * (1 << FREQ_SH) / ((double)(1 << 20));
                    ST.dt_tab[d][i] = (int)rate;
                    ST.dt_tab[d + 4][i] = -ST.dt_tab[d][i];
                    //#if 0
                    //logerror("FM.C: DT [%2i %2i] = %8x  \n", d, i, ST.dt_tab[d][i] );
                    //#endif
                }
            }

        }


        private void reset_channels(FM_ST ST, FM_CH[] CH, int num)
        {
            int c, s;

            ST.mode = 0;   /* normal mode */
            ST.TA = 0;
            ST.TAC = 0;
            ST.TB = 0;
            ST.TBC = 0;

            for (c = 0; c < num; c++)
            {
                //memset(&CH[c], 0x00, sizeof(FM_CH));
                CH[c].mem_value = 0;
                CH[c].op1_out[0] = 0;
                CH[c].op1_out[1] = 0;
                CH[c].fc = 0;
                for (s = 0; s < 4; s++)
                {
                    //memset(&CH[c].SLOT[s], 0x00, sizeof(FM_SLOT));
                    CH[c].SLOT[s].Incr = -1;
                    CH[c].SLOT[s].key = 0;
                    CH[c].SLOT[s].phase = 0;
                    CH[c].SLOT[s].ssg = 0;
                    CH[c].SLOT[s].ssgn = 0;
                    CH[c].SLOT[s].state = EG_OFF;
                    CH[c].SLOT[s].volume = MAX_ATT_INDEX;
                    CH[c].SLOT[s].vol_out = MAX_ATT_INDEX;
                }
            }
        }

        /* initialize generic tables */
        private int init_tables()
        {
            short i, x;
            short n;
            double o, m;

            for (x = 0; x < TL_RES_LEN; x++)
            {
                m = (1 << 16) / Math.Pow(2, (x + 1) * (ENV_STEP / 4.0) / 8.0);
                m = Math.Floor(m);

                /* we never reach (1<<16) here due to the (x+1) */
                /* result fits within 16 bits at maximum */

                n = (short)m;     /* 16 bits here */
                n >>= 4;        /* 12 bits here */
                if ((n & 1) != 0)      /* round to nearest */
                    n = (short)((n >> 1) + 1);
                else
                    n = (short)(n >> 1);
                /* 11 bits here (rounded) */
                n <<= 2;        /* 13 bits here (as in real chip) */
                tl_tab[x * 2 + 0] = n;
                tl_tab[x * 2 + 1] = (short)-tl_tab[x * 2 + 0];

                for (i = 1; i < 13; i++)
                {
                    tl_tab[x * 2 + 0 + i * 2 * TL_RES_LEN] = (short)(tl_tab[x * 2 + 0] >> i);
                    tl_tab[x * 2 + 1 + i * 2 * TL_RES_LEN] = (short)(-tl_tab[x * 2 + 0 + i * 2 * TL_RES_LEN]);
                }
                //#if 0
                //        			logerror("tl %04i", x);
                //        			for (i=0; i<13; i++)
                //        				logerror(", [%02i] %4x", i*2, tl_tab[ x*2 /*+1*/ + i*2*TL_RES_LEN ]);
                //        			logerror("\n");
                //#endif
            }
            /*logerror("FM.C: TL_TAB_LEN = %i elements (%i bytes)\n",TL_TAB_LEN, (int)sizeof(tl_tab));*/


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

                n = (short)(2.0 * o);
                if ((n & 1) != 0)                      /* round to nearest */
                    n = (short)((n >> 1) + 1);
                else
                    n = (short)(n >> 1);

                sin_tab[i] = (ushort)(n * 2 + (m >= 0.0 ? 0 : 1));
                /*logerror("FM.C: sin [%4i]= %4i (tl_tab value=%5i)\n", i, sin_tab[i],tl_tab[sin_tab[i]]);*/
            }

            /*logerror("FM.C: ENV_QUIET= %08x\n",ENV_QUIET );*/


            /* build LFO PM modulation table */
            for (i = 0; i < 8; i++) /* 8 PM depths */
            {
                byte fnum;
                for (fnum = 0; fnum < 128; fnum++) /* 7 bits meaningful of F-NUMBER */
                {
                    byte value;
                    byte step;
                    uint offset_depth = (uint)i;
                    uint offset_fnum_bit;
                    uint bit_tmp;

                    for (step = 0; step < 8; step++)
                    {
                        value = 0;
                        for (bit_tmp = 0; bit_tmp < 7; bit_tmp++) /* 7 bits */
                        {
                            if ((fnum & (1 << (int)bit_tmp)) != 0) /* only if bit "bit_tmp" is set */
                            {
                                offset_fnum_bit = bit_tmp * 8;
                                value += lfo_pm_output[offset_fnum_bit + offset_depth][step];
                            }
                        }
                        lfo_pm_table[(fnum * 32 * 8) + (i * 32) + step + 0] = value;
                        lfo_pm_table[(fnum * 32 * 8) + (i * 32) + (step ^ 7) + 8] = value;
                        lfo_pm_table[(fnum * 32 * 8) + (i * 32) + step + 16] = -value;
                        lfo_pm_table[(fnum * 32 * 8) + (i * 32) + (step ^ 7) + 24] = -value;
                    }
                    //#if 0
                    //        			logerror("LFO depth=%1x FNUM=%04x (<<4=%4x): ", i, fnum, fnum<<4);
                    //        			for (step=0; step<16; step++) /* dump only positive part of waveforms */
                    //        				logerror("%02x ", lfo_pm_table[(fnum*32*8) + (i*32) + step] );
                    //        			logerror("\n");
                    //#endif

                }
            }


            //# ifdef SAVE_SAMPLE
            //            sample[0] = fopen("sampsum.pcm", "wb");
            //#endif

            return 1;

        }



        private void FMCloseTable()
        {
            //# ifdef SAVE_SAMPLE
            //            fclose(sample[0]);
            //#endif
            return;
        }


        /* CSM Key Controll */
        //INLINE
        private void CSMKeyControll(byte type, FM_CH CH)
        {
            /* all key on then off (only for operators which were OFF!) */
            if (CH.SLOT[SLOT1].key == 0)
            {
                FM_KEYON(type, CH, SLOT1);
                FM_KEYOFF(CH, SLOT1);
            }
            if (CH.SLOT[SLOT2].key == 0)
            {
                FM_KEYON(type, CH, SLOT2);
                FM_KEYOFF(CH, SLOT2);
            }
            if (CH.SLOT[SLOT3].key == 0)
            {
                FM_KEYON(type, CH, SLOT3);
                FM_KEYOFF(CH, SLOT3);
            }
            if (CH.SLOT[SLOT4].key == 0)
            {
                FM_KEYON(type, CH, SLOT4);
                FM_KEYOFF(CH, SLOT4);
            }
        }

        //# ifdef __STATE_H__
        /* FM channel save , internal state only */
        private void FMsave_state_channel(device_config device, FM_CH[] CH, int num_ch)
        {

            int slot, ch;
            int CHPtr = 0;

            for (ch = 0; ch < num_ch; ch++, CHPtr++)
            {
                /* channel */
                state_save_register_device_item_array(device, ch, CH[CHPtr].op1_out);
                state_save_register_device_item(device, ch, CH[CHPtr].fc);
                /* slots */
                for (slot = 0; slot < 4; slot++)
                {
                    FM_SLOT SLOT = CH[CHPtr].SLOT[slot];
                    state_save_register_device_item(device, ch * 4 + slot, SLOT.phase);
                    state_save_register_device_item(device, ch * 4 + slot, SLOT.state);
                    state_save_register_device_item(device, ch * 4 + slot, (uint)SLOT.volume);
                }
            }
        }

        private void FMsave_state_st(device_config device, FM_ST ST)
        {
#if FM_BUSY_FLAG_SUPPORT
        	state_save_register_device_item(device, 0, ST.busy_expiry_time.seconds );
        	state_save_register_device_item(device, 0, ST.busy_expiry_time.attoseconds );
#endif
            state_save_register_device_item(device, 0, ST.address);
            state_save_register_device_item(device, 0, ST.irq);
            state_save_register_device_item(device, 0, ST.irqmask);
            state_save_register_device_item(device, 0, ST.status);
            state_save_register_device_item(device, 0, ST.mode);
            state_save_register_device_item(device, 0, ST.prescaler_sel);
            state_save_register_device_item(device, 0, ST.fn_h);
            state_save_register_device_item(device, 0, (uint)ST.TA);
            state_save_register_device_item(device, 0, (uint)ST.TAC);
            state_save_register_device_item(device, 0, ST.TB);
            state_save_register_device_item(device, 0, (uint)ST.TBC);
        }
        //#endif /* _STATE_H */

        //#if BUILD_OPN



        /* prescaler set (and make time tables) */
        private void OPNSetPres(FM_OPN OPN, short pres, short timer_prescaler, short SSGpres)
        {
            int i;

            /* frequency base */
            OPN.ST.freqbase = (OPN.ST.rate != 0) ? ((double)OPN.ST.clock / OPN.ST.rate) / pres : 0;

            //#if 0
            //OPN.ST.rate = (double)OPN.ST.clock / pres;
            //OPN.ST.freqbase = 1.0;
            //#endif

            OPN.eg_timer_add = (uint)((1 << EG_SH) * OPN.ST.freqbase);
            OPN.eg_timer_overflow = (3) * (1 << EG_SH);


            /* Timer base time */
            OPN.ST.timer_prescaler = timer_prescaler;

            /* SSG part  prescaler set */
            if (SSGpres != 0) OPN.ST.SSG.set_clock(OPN.ST.param, (short)(OPN.ST.clock * 2 / SSGpres));

            /* make time tables */
            init_timetables(OPN.ST, dt_tab);

            /* there are 2048 FNUMs that can be generated using FNUM/BLK registers
                but LFO works with one more bit of a precision so we really need 4096 elements */
            /* calculate fnumber . increment counter table */
            for (i = 0; i < 4096; i++)
            {
                /* freq table for octave 7 */
                /* OPN phase increment counter = 20bit */
                OPN.fn_table[i] = (uint)((double)i * 32 * OPN.ST.freqbase * (1 << (FREQ_SH - 10))); /* -10 because chip works with 10.10 fixed point, while we use 16.16 */
                //#if 0
                //logerror("FM.C: fn_table[%4i] = %08x (dec=%8i)\n",
                //i, OPN.fn_table[i]>>6,OPN.fn_table[i]>>6 );
                //#endif
            }

            /* maximal frequency is required for Phase overflow calculation, register size is 17 bits (Nemesis) */
            OPN.fn_max = (uint)((double)0x20000 * OPN.ST.freqbase * (1 << (FREQ_SH - 10)));

            /* LFO freq. table */
            for (i = 0; i < 8; i++)
            {
                /* Amplitude modulation: 64 output levels (triangle waveform); 1 level lasts for one of "lfo_samples_per_step" samples */
                /* Phase modulation: one entry from lfo_pm_output lasts for one of 4 * "lfo_samples_per_step" samples  */
                OPN.lfo_freq[i] = (uint)((1.0 / lfo_samples_per_step[i]) * (1 << LFO_SH) * OPN.ST.freqbase);
                //#if 0
                //logerror("FM.C: lfo_freq[%i] = %08x (dec=%8i)\n",
                //i, OPN.lfo_freq[i],OPN.lfo_freq[i] );
                //#endif
            }
        }



        /* write a OPN mode register 0x20-0x2f */
        private void OPNWriteMode(FM_OPN OPN, int r, int v)
        {
            byte c;
            FM_CH CH;

            switch (r)
            {
                case 0x21:  /* Test */
                    break;
                case 0x22:  /* LFO FREQ (YM2608/YM2610/YM2610B/YM2612) */
                    if ((OPN.type & TYPE_LFOPAN) != 0)
                    {
                        if ((v & 0x08) != 0) /* LFO enabled ? */
                        {
                            OPN.lfo_inc = OPN.lfo_freq[v & 7];
                        }
                        else
                        {
                            OPN.lfo_inc = 0;
                        }
                    }
                    break;
                case 0x24:  /* timer A High 8*/
                    OPN.ST.TA = (OPN.ST.TA & 0x03) | (((int)v) << 2);
                    break;
                case 0x25:  /* timer A Low 2*/
                    OPN.ST.TA = (OPN.ST.TA & 0x3fc) | (v & 3);
                    break;
                case 0x26:  /* timer B */
                    OPN.ST.TB = (byte)v;
                    break;
                case 0x27:  /* mode, timer control */
                    set_timers(OPN.ST, OPN.ST.param, v);
                    break;
                case 0x28:  /* key on / off */
                    c = (byte)(v & 0x03);
                    if (c == 3) break;
                    if ((v & 0x04) != 0 && (OPN.type & TYPE_6CH) != 0) c += 3;
                    CH = OPN.P_CH[c];
                    if ((v & 0x10) != 0) FM_KEYON(OPN.type, CH, SLOT1); else FM_KEYOFF(CH, SLOT1);
                    if ((v & 0x20) != 0) FM_KEYON(OPN.type, CH, SLOT2); else FM_KEYOFF(CH, SLOT2);
                    if ((v & 0x40) != 0) FM_KEYON(OPN.type, CH, SLOT3); else FM_KEYOFF(CH, SLOT3);
                    if ((v & 0x80) != 0) FM_KEYON(OPN.type, CH, SLOT4); else FM_KEYOFF(CH, SLOT4);
                    break;
            }
        }

        /* write a OPN register (0x30-0xff) */
        private void OPNWriteReg(FM_OPN OPN, int r, int v)
        {
            FM_CH CH;
            FM_SLOT SLOT;

            byte c = OPN_CHAN(r);

            if (c == 3) return; /* 0xX3,0xX7,0xXB,0xXF */

            if (r >= 0x100) c += 3;

            CH = OPN.P_CH[c];

            SLOT = CH.SLOT[OPN_SLOT(r)];

            switch (r & 0xf0)
            {
                case 0x30:  /* DET , MUL */
                    set_det_mul(OPN.ST, CH, SLOT, v);
                    break;

                case 0x40:  /* TL */
                    set_tl(CH, SLOT, v);
                    break;

                case 0x50:  /* KS, AR */
                    set_ar_ksr(OPN.type, CH, SLOT, v);
                    break;

                case 0x60:  /* bit7 = AM ENABLE, DR */
                    set_dr(OPN.type, SLOT, v);

                    if ((OPN.type & TYPE_LFOPAN) != 0) /* YM2608/2610/2610B/2612 */
                    {
                        SLOT.AMmask = (v & 0x80) != 0 ? ~(uint)0 : 0;
                    }
                    break;

                case 0x70:  /*     SR */
                    set_sr(OPN.type, SLOT, v);
                    break;

                case 0x80:  /* SL, RR */
                    set_sl_rr(OPN.type, SLOT, v);
                    break;

                case 0x90:  /* SSG-EG */
                    SLOT.ssg = (byte)(v & 0x0f);
                    SLOT.ssgn = (byte)((v & 0x04) >> 1); /* bit 1 in ssgn = attack */

                    /* SSG-EG envelope shapes :

        			E AtAlH
        			1 0 0 0  \\\\

        			1 0 0 1  \___

        			1 0 1 0  \/\/
        					  ___
        			1 0 1 1  \

        			1 1 0 0  ////
        					  ___
        			1 1 0 1  /

        			1 1 1 0  /\/\

        			1 1 1 1  /___


        			E = SSG-EG enable


        			The shapes are generated using Attack, Decay and Sustain phases.

        			Each single character in the diagrams above represents this whole
        			sequence:

        			- when KEY-ON = 1, normal Attack phase is generated (*without* any
        			  difference when compared to normal mode),

        			- later, when envelope level reaches minimum level (max volume),
        			  the EG switches to Decay phase (which works with bigger steps
        			  when compared to normal mode - see below),

        			- later when envelope level passes the SL level,
        			  the EG swithes to Sustain phase (which works with bigger steps
        			  when compared to normal mode - see below),

        			- finally when envelope level reaches maximum level (min volume),
        			  the EG switches to Attack phase again (depends on actual waveform).

        			Important is that when switch to Attack phase occurs, the phase counter
        			of that operator will be zeroed-out (as in normal KEY-ON) but not always.
        			(I havent found the rule for that - perhaps only when the output level is low)

        			The difference (when compared to normal Envelope Generator mode) is
        			that the resolution in Decay and Sustain phases is 4 times lower;
        			this results in only 256 steps instead of normal 1024.
        			In other words:
        			when SSG-EG is disabled, the step inside of the EG is one,
        			when SSG-EG is enabled, the step is four (in Decay and Sustain phases).

        			Times between the level changes are the same in both modes.


        			Important:
        			Decay 1 Level (so called SL) is compared to actual SSG-EG output, so
        			it is the same in both SSG and no-SSG modes, with this exception:

        			when the SSG-EG is enabled and is generating raising levels
        			(when the EG output is inverted) the SL will be found at wrong level !!!
        			For example, when SL=02:
        				0 -6 = -6dB in non-inverted EG output
        				96-6 = -90dB in inverted EG output
        			Which means that EG compares its level to SL as usual, and that the
        			output is simply inverted afterall.


        			The Yamaha's manuals say that AR should be set to 0x1f (max speed).
        			That is not necessary, but then EG will be generating Attack phase.

        			*/


                    break;

                case 0xa0:
                    switch (OPN_SLOT(r))
                    {
                        case 0:     /* 0xa0-0xa2 : FNUM1 */
                            {
                                uint fn = (uint)((((uint)((OPN.ST.fn_h) & 7)) << 8) + v);
                                byte blk = (byte)(OPN.ST.fn_h >> 3);
                                /* keyscale code */
                                CH.kcode = (byte)((blk << 2) | opn_fktable[fn >> 7]);
                                /* phase increment counter */
                                CH.fc = OPN.fn_table[fn * 2] >> (7 - blk);

                                /* store fnum in clear form for LFO PM calculations */
                                CH.block_fnum = (uint)(((long)blk << 11) | fn);

                                CH.SLOT[SLOT1].Incr = -1;
                            }
                            break;
                        case 1:     /* 0xa4-0xa6 : FNUM2,BLK */
                            OPN.ST.fn_h = (byte)(v & 0x3f);
                            break;
                        case 2:     /* 0xa8-0xaa : 3CH FNUM1 */
                            if (r < 0x100)
                            {
                                uint fn = (uint)((((uint)(OPN.SL3.fn_h & 7)) << 8) + v);
                                byte blk = (byte)(OPN.SL3.fn_h >> 3);
                                /* keyscale code */
                                OPN.SL3.kcode[c] = (byte)((blk << 2) | opn_fktable[fn >> 7]);
                                /* phase increment counter */
                                OPN.SL3.fc[c] = OPN.fn_table[fn * 2] >> (7 - blk);
                                OPN.SL3.block_fnum[c] = (uint)((blk << 11) | fn);
                                (OPN.P_CH)[2].SLOT[SLOT1].Incr = -1;
                            }
                            break;
                        case 3:     /* 0xac-0xae : 3CH FNUM2,BLK */
                            if (r < 0x100)
                                OPN.SL3.fn_h = (byte)(v & 0x3f);
                            break;
                    }
                    break;

                case 0xb0:
                    switch (OPN_SLOT(r))
                    {
                        case 0:     /* 0xb0-0xb2 : FB,ALGO */
                            {
                                int feedback = (v >> 3) & 7;
                                CH.ALGO = (byte)(v & 7);
                                CH.FB = (byte)(feedback != 0 ? (feedback + 6) : 0);
                                setup_connection(OPN, CH, c);
                            }
                            break;
                        case 1:     /* 0xb4-0xb6 : L , R , AMS , PMS (YM2612/YM2610B/YM2610/YM2608) */
                            if ((OPN.type & TYPE_LFOPAN) != 0)
                            {
                                /* b0-2 PMS */
                                CH.pms = (v & 7) * 32; /* CH.pms = PM depth * 32 (index in lfo_pm_table) */

                                /* b4-5 AMS */
                                CH.ams = lfo_ams_depth_shift[(v >> 4) & 0x03];

                                /* PAN :  b7 = L, b6 = R */
                                OPN.pan[c * 2] = (v & 0x80) != 0 ? ~(uint)0 : 0;
                                OPN.pan[c * 2 + 1] = (v & 0x40) != 0 ? ~(uint)0 : 0;

                            }
                            break;
                    }
                    break;
            }
        }

        //#endif /* BUILD_OPN */

        //#if BUILD_OPN_PRESCALER
        /*
          prescaler circuit (best guess to verified chip behaviour)

                       +--------------+  +-sel2-+
                       |              +--|in20  |
                 +---+ |  +-sel1-+       |      |
        M-CLK -+-|1/2|-+--|in10  | +---+ |   out|--INT_CLOCK
               | +---+    |   out|-|1/3|-|in21  |
               +----------|in11  | +---+ +------+
                          +------+

        reg.2d : sel2 = in21 (select sel2)
        reg.2e : sel1 = in11 (select sel1)
        reg.2f : sel1 = in10 , sel2 = in20 (clear selector)
        reset  : sel1 = in11 , sel2 = in21 (clear both)

        */
        private void OPNPrescaler_w(FM_OPN OPN, int addr, int pre_divider)
        {
            int[] opn_pres = new int[4] { 2 * 12, 2 * 12, 6 * 12, 3 * 12 };
            int[] ssg_pres = new int[4] { 1, 1, 4, 2 };
            int sel;

            switch (addr)
            {
                case 0:     /* when reset */
                    OPN.ST.prescaler_sel = 2;
                    break;
                case 1:     /* when postload */
                    break;
                case 0x2d:  /* divider sel : select 1/1 for 1/3line    */
                    OPN.ST.prescaler_sel |= 0x02;
                    break;
                case 0x2e:  /* divider sel , select 1/3line for output */
                    OPN.ST.prescaler_sel |= 0x01;
                    break;
                case 0x2f:  /* divider sel , clear both selector to 1/2,1/2 */
                    OPN.ST.prescaler_sel = 0;
                    break;
            }
            sel = OPN.ST.prescaler_sel & 3;
            /* update prescaler */
            OPNSetPres(OPN, (short)(opn_pres[sel] * pre_divider),
                            (short)(opn_pres[sel] * pre_divider),
                            (short)(ssg_pres[sel] * pre_divider));
        }
        //#endif /* BUILD_OPN_PRESCALER */

        public class FM_base
        {

        }

        //#if BUILD_YM2203
        /*****************************************************************************/
        /*      YM2203 local section                                                 */
        /*****************************************************************************/

        /* here's the virtual YM2203(OPN) */
        private class YM2203:FM_base
        {

            public byte[] REGS = new byte[256];     /* registers         */
            public FM_OPN OPN; /* OPN state         */
            public FM_CH[] CH = new FM_CH[3];            /* channel state     */
        }

        /* Generate samples for one of the YM2203s */
        private void ym2203_update_one(YM2203 chip, int[][] buffer, int length)
        {
            YM2203 F2203 = (YM2203)chip;
            FM_OPN OPN = F2203.OPN;
            int i;
            int[] bufL = buffer[0];
            int[] bufR = buffer[1];
            FM_CH[] cch = new FM_CH[3];

            cch[0] = F2203.CH[0];
            cch[1] = F2203.CH[1];
            cch[2] = F2203.CH[2];

            /* refresh PG and EG */
            refresh_fc_eg_chan(OPN, cch[0]);
            refresh_fc_eg_chan(OPN, cch[1]);
            if ((F2203.OPN.ST.mode & 0xc0) != 0)
            {
                /* 3SLOT MODE */
                if (cch[2].SLOT[SLOT1].Incr == -1)
                {
                    refresh_fc_eg_slot(OPN, cch[2].SLOT[SLOT1], (int)OPN.SL3.fc[1], OPN.SL3.kcode[1]);
                    refresh_fc_eg_slot(OPN, cch[2].SLOT[SLOT2], (int)OPN.SL3.fc[2], OPN.SL3.kcode[2]);
                    refresh_fc_eg_slot(OPN, cch[2].SLOT[SLOT3], (int)OPN.SL3.fc[0], OPN.SL3.kcode[0]);
                    refresh_fc_eg_slot(OPN, cch[2].SLOT[SLOT4], (int)cch[2].fc, cch[2].kcode);
                }
            }
            else
                refresh_fc_eg_chan(OPN, cch[2]);


            /* YM2203 doesn't have LFO so we must keep these globals at 0 level */
            OPN.LFO_AM = 0;
            OPN.LFO_PM = 0;

            /* buffering */
            for (i = 0; i < length; i++)
            {
                /* clear outputs */
                OPN.out_fm[0] = 0;
                OPN.out_fm[1] = 0;
                OPN.out_fm[2] = 0;

                /* advance envelope generator */
                OPN.eg_timer += OPN.eg_timer_add;
                while (OPN.eg_timer >= OPN.eg_timer_overflow)
                {
                    OPN.eg_timer -= OPN.eg_timer_overflow;
                    OPN.eg_cnt++;

                    advance_eg_channel(OPN, cch[0].SLOT);//[SLOT1]);
                    advance_eg_channel(OPN, cch[1].SLOT);//[SLOT1]);
                    advance_eg_channel(OPN, cch[2].SLOT);//[SLOT1]);
                }

                /* calculate FM */
                chan_calc(OPN, cch[0], 0);
                chan_calc(OPN, cch[1], 1);
                chan_calc(OPN, cch[2], 2);

                /* buffering */
                {
                    int lt;

                    lt = OPN.out_fm[0] + OPN.out_fm[1] + OPN.out_fm[2];

                    lt >>= FINAL_SH;

                    //Limit( lt , MAXOUT, MINOUT );

                    //#ifdef SAVE_SAMPLE
                    //SAVE_ALL_CHANNELS
                    //#endif

                    /* buffering */
                    bufL[i] = lt;
                    bufR[i] = lt;
                }

                /* timer A control */
                INTERNAL_TIMER_A(OPN, F2203.OPN.ST, cch[2]);

            }
            INTERNAL_TIMER_B(F2203.OPN.ST, length);
        }

        /* ---------- reset one of chip ---------- */
        private void ym2203_reset_chip(YM2203 chip)
        {
            int i;
            YM2203 F2203 = (YM2203)chip;
            FM_OPN OPN = F2203.OPN;

            /* Reset Prescaler */
            OPNPrescaler_w(OPN, 0, 1);
            /* reset SSG section */
            OPN.ST.SSG.reset(OPN.ST.param);
            /* status clear */
            FM_IRQMASK_SET(OPN.ST, 0x03);
            FM_BUSY_CLEAR(OPN.ST);
            OPNWriteMode(OPN, 0x27, 0x30); /* mode 0 , timer reset */

            OPN.eg_timer = 0;
            OPN.eg_cnt = 0;

            FM_STATUS_RESET(OPN.ST, 0xff);

            reset_channels(OPN.ST, F2203.CH, 3);
            /* reset OPerator paramater */
            for (i = 0xb2; i >= 0x30; i--) OPNWriteReg(OPN, i, 0);
            for (i = 0x26; i >= 0x20; i--) OPNWriteReg(OPN, i, 0);
        }

        //# ifdef __STATE_H__
        private void ym2203_postload(YM2203 chip)
        {
            if (chip != null)
            {
                YM2203 F2203 = (YM2203)chip;
                int r;

                /* prescaler */
                OPNPrescaler_w(F2203.OPN, 1, 1);

                /* SSG registers */
                for (r = 0; r < 16; r++)
                {
                    F2203.OPN.ST.SSG.write(F2203.OPN.ST.param, 0, (short)r);
                    F2203.OPN.ST.SSG.write(F2203.OPN.ST.param, 1, F2203.REGS[r]);
                }

                /* OPN registers */
                /* DT / MULTI , TL , KS / AR , AMON / DR , SR , SL / RR , SSG-EG */
                for (r = 0x30; r < 0x9e; r++)
                    if ((r & 3) != 3)
                        OPNWriteReg(F2203.OPN, r, F2203.REGS[r]);
                /* FB / CONNECT , L / R / AMS / PMS */
                for (r = 0xb0; r < 0xb6; r++)
                    if ((r & 3) != 3)
                        OPNWriteReg(F2203.OPN, r, F2203.REGS[r]);

                /* channels */
                /*FM_channel_postload(F2203.CH,3);*/
            }
        }

        private void YM2203_save_state(YM2203 F2203, device_config device)
        {

            state_save_register_device_item_array(device, 0, F2203.REGS);
            FMsave_state_st(device, F2203.OPN.ST);
            FMsave_state_channel(device, F2203.CH, 3);
            /* 3slots */
            state_save_register_device_item_array(device, 0, F2203.OPN.SL3.fc);
            state_save_register_device_item(device, 0, F2203.OPN.SL3.fn_h);
            state_save_register_device_item_array(device, 0, F2203.OPN.SL3.kcode);
        }
        //#endif /* _STATE_H */

        /* ----------  Initialize YM2203 emulator(s) ----------
           'num' is the number of virtual YM2203s to allocate
           'clock' is the chip clock in Hz
           'rate' is sampling rate
        */
        //void * ym2203_init(void *param, const device_config *device, int clock, int rate,
        //               FM_TIMERHANDLER timer_handler,FM_IRQHANDLER IRQHandler, const ssg_callbacks *ssg)
        private YM2203 ym2203_init(YM2203 param, int clock, int rate,
                       FM_ST.dlgFM_TIMERHANDLER timer_handler, FM_ST.dlgFM_IRQHANDLER IRQHandler, _ssg_callbacks ssg)
        {
            YM2203 F2203;

            /* allocate ym2203 state space */
            F2203 = new YM2203();
            if (F2203 == null)
                return null;
            /* clear */
            //memset(F2203, 0, sizeof(YM2203));

            if (init_tables() == 0)
            {
                //free(F2203);
                return null;
            }

            F2203.OPN.ST.param = param;
            F2203.OPN.type = TYPE_YM2203;
            F2203.OPN.P_CH = F2203.CH;
            //F2203.OPN.ST.device = device;
            F2203.OPN.ST.clock = clock;
            F2203.OPN.ST.rate = rate;

            F2203.OPN.ST.timer_handler = timer_handler;
            F2203.OPN.ST.IRQ_Handler = IRQHandler;
            F2203.OPN.ST.SSG = ssg;

            //#ifdef __STATE_H__
            //YM2203_save_state(F2203, device);
            //#endif
            return F2203;
        }

        /* shut down emulator */
        private void ym2203_shutdown(YM2203 chip)
        {
            YM2203 FM2203 = (YM2203)chip;

            FMCloseTable();
            //free(FM2203);
        }

        /* YM2203 I/O interface */
        private int ym2203_write(byte ChipID, YM2203 chip, int a, byte v)
        {
            YM2203 F2203 = (YM2203)chip;
            FM_OPN OPN = F2203.OPN;

            if ((a & 1) == 0)
            {   /* address port */
                OPN.ST.address = (v &= 0xff);

                /* Write register to SSG emulator */
                if (v < 16) OPN.ST.SSG.write(OPN.ST.param, 0, v);

                /* prescaler select : 2d,2e,2f  */
                if (v >= 0x2d && v <= 0x2f)
                    OPNPrescaler_w(OPN, v, 1);
            }
            else
            {   /* data port */
                int addr = OPN.ST.address;
                F2203.REGS[addr] = v;
                switch (addr & 0xf0)
                {
                    case 0x00:  /* 0x00-0x0f : SSG section */
                        /* Write data to SSG emulator */
                        OPN.ST.SSG.write(OPN.ST.param, (short)a, v);
                        break;
                    case 0x20:  /* 0x20-0x2f : Mode section */
                        ym2203_update_req(ChipID, (YM2203)OPN.ST.param);
                        /* write register */
                        OPNWriteMode(OPN, addr, v);
                        break;
                    default:    /* 0x30-0xff : OPN section */
                        ym2203_update_req(ChipID, (YM2203)OPN.ST.param);
                        /* write register */
                        OPNWriteReg(OPN, addr, v);
                        break;
                }
                FM_BUSY_SET(OPN.ST, 1);
            }
            return OPN.ST.irq;
        }

        private byte ym2203_read(YM2203 chip, int a)
        {
            YM2203 F2203 = (YM2203)chip;
            int addr = F2203.OPN.ST.address;
            byte ret = 0;

            if ((a & 1) == 0)
            {   /* status port */
                ret = FM_STATUS_FLAG(F2203.OPN.ST);
            }
            else
            {   /* data port (only SSG) */
                if (addr < 16) ret = (byte)F2203.OPN.ST.SSG.read(F2203.OPN.ST.param);
            }
            return ret;
        }

        private int ym2203_timer_over(byte ChipID, YM2203 chip, int c)
        {
            YM2203 F2203 = (YM2203)chip;

            if (c != 0)
            {   /* Timer B */
                TimerBOver(F2203.OPN.ST);
            }
            else
            {   /* Timer A */
                ym2203_update_req(ChipID, (YM2203)F2203.OPN.ST.param);
                /* timer update */
                TimerAOver(F2203.OPN.ST);
                /* CSM mode key,TL control */
                if ((F2203.OPN.ST.mode & 0x80) != 0)
                {   /* CSM mode auto key on */
                    CSMKeyControll(F2203.OPN.type, F2203.CH[2]);
                }
            }
            return F2203.OPN.ST.irq;
        }

        private void ym2203_set_mutemask(YM2203 chip, uint MuteMask)
        {
            YM2203 F2203 = (YM2203)chip;
            byte CurChn;

            for (CurChn = 0; CurChn < 3; CurChn++)
                F2203.CH[CurChn].Muted = (byte)((MuteMask >> CurChn) & 0x01);

            return;
        }
        //#endif /* BUILD_YM2203 */



        //#if (BUILD_YM2608 || BUILD_YM2610 || BUILD_YM2610B)

        /* ADPCM type A channel struct */
        private class ADPCM_CH
        {
            public byte flag;          /* port state               */
            public byte flagMask;      /* arrived flag mask        */
            public byte now_data;      /* current ROM data         */
            public uint now_addr;      /* current ROM address      */
            public uint now_step;
            public uint step;
            public uint start;         /* sample data start address*/
            public uint end;           /* sample data end address  */
            public byte IL;                /* Instrument Level         */
            public int adpcm_acc;      /* accumulator              */
            public int adpcm_step;     /* step                     */
            public int adpcm_out;      /* (speedup) hiro-shi!!     */
            public sbyte vol_mul;       /* volume in "0.75dB" steps */
            public byte vol_shift;     /* volume in "-6dB" steps   */
            public int[] pan;           /* &out_adpcm[OPN_xxxx]     */
            public int panPtr;
            public byte Muted;
        }

        /* here's the virtual YM2610 */
        private class YM2610:FM_base
        {
            public byte[] REGS = new byte[512];         /* registers            */
            public FM_OPN OPN;             /* OPN state            */
            public FM_CH[] CH = new FM_CH[6];                /* channel state        */
            public byte addr_A1;           /* address line A1      */

            /* ADPCM-A unit */
            //const byte	*pcmbuf;			/* pcm rom buffer       */
            public byte[] pcmbuf;           /* pcm rom buffer       */
            public int pcmbufPtr;           /* pcm rom buffer       */
            public uint pcm_size;          /* size of pcm rom      */
            public byte adpcmTL;           /* adpcmA total level   */
            public ADPCM_CH[] adpcm = new ADPCM_CH[6];          /* adpcm channels       */
            public uint[] adpcmreg = new uint[0x30];        /* registers            */
            public byte adpcm_arrivedEndAddress;
            public mame.ym_deltat.YM_DELTAT deltaT;               /* Delta-T ADPCM unit   */
            public byte MuteDeltaT;

            public byte flagmask;          /* YM2608 only */
            public byte irqmask;           /* YM2608 only */
        }

        /* here is the virtual YM2608 */
        private class YM2608 : YM2610
        {

        }


        /**** YM2610 ADPCM defines ****/
        private const int ADPCM_SHIFT = (16);      /* frequency step rate   */
        private const int ADPCMA_ADDRESS_SHIFT = 8;   /* adpcm A address shift */

        /* Algorithm and tables verified on real YM2608 and YM2610 */

        /* usual ADPCM table (16 * 1.1^N) */
        private int[] steps = new int[49]
        {
             16,  17,   19,   21,   23,   25,   28,
             31,  34,   37,   41,   45,   50,   55,
             60,  66,   73,   80,   88,   97,  107,
            118, 130,  143,  157,  173,  190,  209,
            230, 253,  279,  307,  337,  371,  408,
            449, 494,  544,  598,  658,  724,  796,
            876, 963, 1060, 1166, 1282, 1411, 1552
        };

        /* different from the usual ADPCM table */
        private int[] step_inc = new int[8] { -1 * 16, -1 * 16, -1 * 16, -1 * 16, 2 * 16, 5 * 16, 7 * 16, 9 * 16 };

        /* speedup purposes only */
        private int[] jedi_table = new int[49 * 16];


        private void Init_ADPCMATable()
        {
            int step, nib;

            for (step = 0; step < 49; step++)
            {
                /* loop over all nibbles and compute the difference */
                for (nib = 0; nib < 16; nib++)
                {
                    int value = (2 * (nib & 0x07) + 1) * steps[step] / 8;
                    jedi_table[step * 16 + nib] = (nib & 0x08) != 0 ? -value : value;
                }
            }
        }

        /* ADPCM A (Non control type) : calculate one channel output */
        //INLINE
        private void ADPCMA_calc_chan(YM2610 F2610, ADPCM_CH ch)
        {
            uint step;
            byte data;

            if (ch.Muted != 0)
                return;


            ch.now_step += ch.step;
            if (ch.now_step >= (1 << ADPCM_SHIFT))
            {
                step = ch.now_step >> ADPCM_SHIFT;
                ch.now_step &= (1 << ADPCM_SHIFT) - 1;
                do
                {
                    /* end check */
                    /* 11-06-2001 JB: corrected comparison. Was > instead of == */
                    /* YM2610 checks lower 20 bits only, the 4 MSB bits are sample bank */
                    /* Here we use 1<<21 to compensate for nibble calculations */

                    if ((ch.now_addr & ((1 << 21) - 1)) == ((ch.end << 1) & ((1 << 21) - 1)))
                    {
                        ch.flag = 0;
                        F2610.adpcm_arrivedEndAddress |= ch.flagMask;
                        return;
                    }
                    //#if 0
                    //        			if ( ch.now_addr > (F2610.pcmsizeA<<1) )
                    //        			{
                    //# ifdef _DEBUG
                    //        				LOG(LOG_WAR,("YM2610: Attempting to play past adpcm rom size!\n" ));
                    //#endif
                    //                    return;
                    //                }
                    //        #endif
                    if ((ch.now_addr & 1) != 0)
                        data = (byte)(ch.now_data & 0x0f);
                    else
                    {
                        ch.now_data = (byte)(F2610.pcmbuf[F2610.pcmbufPtr] + (ch.now_addr >> 1));
                        data = (byte)((ch.now_data >> 4) & 0x0f);
                    }

                    ch.now_addr++;

                    ch.adpcm_acc += jedi_table[ch.adpcm_step + data];

                    /* extend 12-bit signed int */
                    if ((ch.adpcm_acc & ~0x7ff) != 0)
                        ch.adpcm_acc |= ~0xfff;
                    else
                        ch.adpcm_acc &= 0xfff;

                    ch.adpcm_step += step_inc[data & 7];
                    Limit(ref ch.adpcm_step, 48 * 16, 0 * 16);

                } while ((--step) != 0);

                /* calc pcm * volume data */
                ch.adpcm_out = ((ch.adpcm_acc * ch.vol_mul) >> ch.vol_shift) & ~3;  /* multiply, shift and mask out 2 LSB bits */
            }

            /* output for work of output channels (out_adpcm[OPNxxxx])*/
            ch.pan[ch.panPtr] += ch.adpcm_out;
        }

        /* ADPCM type A Write */
        private void FM_ADPCMAWrite(YM2610 F2610, int r, int v)
        {
            ADPCM_CH[] adpcm = F2610.adpcm;
            byte c = (byte)(r & 0x07);

            F2610.adpcmreg[r] = (uint)(v & 0xff); /* stock data */
            switch (r)
            {
                case 0x00: /* DM,--,C5,C4,C3,C2,C1,C0 */
                    if ((v & 0x80) == 0)
                    {
                        /* KEY ON */
                        for (c = 0; c < 6; c++)
                        {
                            if (((v >> c) & 1) != 0)
                            {
                                /**** start adpcm ****/
                                // The .step variable is already set and for the YM2608 it is different on channels 4 and 5.
                                //adpcm[c].step      = (uint)((float)(1<<ADPCM_SHIFT)*((float)F2610.OPN.ST.freqbase)/3.0);
                                adpcm[c].now_addr = adpcm[c].start << 1;
                                adpcm[c].now_step = 0;
                                adpcm[c].adpcm_acc = 0;
                                adpcm[c].adpcm_step = 0;
                                adpcm[c].adpcm_out = 0;
                                adpcm[c].flag = 1;

                                if (F2610.pcmbuf == null)
                                {                   /* Check ROM Mapped */
                                    //# ifdef _DEBUG
                                    //logerror("YM2608-YM2610: ADPCM-A rom not mapped\n");
                                    //#endif
                                    adpcm[c].flag = 0;
                                }
                                else
                                {
                                    if (adpcm[c].end >= F2610.pcm_size)
                                    {   /* Check End in Range */
                                        //# ifdef _DEBUG
                                        //logerror("YM2610: ADPCM-A end out of range: $%08x\n", adpcm[c].end);
                                        //#endif
                                        /*adpcm[c].end = F2610.pcm_size-1;*/ /* JB: DO NOT uncomment this, otherwise you will break the comparison in the ADPCM_CALC_CHA() */
                                    }
                                    if (adpcm[c].start >= F2610.pcm_size)  /* Check Start in Range */
                                    {
                                        //# ifdef _DEBUG
                                        //logerror("YM2608-YM2610: ADPCM-A start out of range: $%08x\n", adpcm[c].start);
                                        //#endif
                                        adpcm[c].flag = 0;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        /* KEY OFF */
                        for (c = 0; c < 6; c++)
                            if (((v >> c) & 1) != 0)
                                adpcm[c].flag = 0;
                    }
                    break;
                case 0x01:  /* B0-5 = TL */
                    F2610.adpcmTL = (byte)((v & 0x3f) ^ 0x3f);
                    for (c = 0; c < 6; c++)
                    {
                        int volume = F2610.adpcmTL + adpcm[c].IL;

                        if (volume >= 63)   /* This is correct, 63 = quiet */
                        {
                            adpcm[c].vol_mul = 0;
                            adpcm[c].vol_shift = 0;
                        }
                        else
                        {
                            adpcm[c].vol_mul = (sbyte)(15 - (volume & 7));       /* so called 0.75 dB */
                            adpcm[c].vol_shift = (byte)(1 + (volume >> 3)); /* Yamaha engineers used the approximation: each -6 dB is close to divide by two (shift right) */
                        }

                        /* calc pcm * volume data */
                        adpcm[c].adpcm_out = ((adpcm[c].adpcm_acc * adpcm[c].vol_mul) >> adpcm[c].vol_shift) & ~3;  /* multiply, shift and mask out low 2 bits */
                    }
                    break;
                default:
                    c = (byte)(r & 0x07);
                    if (c >= 0x06) return;
                    switch (r & 0x38)
                    {
                        case 0x08:  /* B7=L,B6=R, B4-0=IL */
                            {
                                int volume;

                                adpcm[c].IL = (byte)((v & 0x1f) ^ 0x1f);

                                volume = F2610.adpcmTL + adpcm[c].IL;

                                if (volume >= 63)   /* This is correct, 63 = quiet */
                                {
                                    adpcm[c].vol_mul = 0;
                                    adpcm[c].vol_shift = 0;
                                }
                                else
                                {
                                    adpcm[c].vol_mul = (sbyte)(15 - (volume & 7));       /* so called 0.75 dB */
                                    adpcm[c].vol_shift = (byte)(1 + (volume >> 3)); /* Yamaha engineers used the approximation: each -6 dB is close to divide by two (shift right) */
                                }

                                adpcm[c].pan[adpcm[c].panPtr] = F2610.OPN.out_adpcm[(v >> 6) & 0x03];

                                /* calc pcm * volume data */
                                adpcm[c].adpcm_out = ((adpcm[c].adpcm_acc * adpcm[c].vol_mul) >> adpcm[c].vol_shift) & ~3;  /* multiply, shift and mask out low 2 bits */
                            }
                            break;
                        case 0x10:
                        case 0x18:
                            adpcm[c].start = ((F2610.adpcmreg[0x18 + c] * 0x0100 | F2610.adpcmreg[0x10 + c]) << ADPCMA_ADDRESS_SHIFT);
                            break;
                        case 0x20:
                        case 0x28:
                            adpcm[c].end = ((F2610.adpcmreg[0x28 + c] * 0x0100 | F2610.adpcmreg[0x20 + c]) << ADPCMA_ADDRESS_SHIFT);
                            adpcm[c].end += (1 << ADPCMA_ADDRESS_SHIFT) - 1;
                            break;
                    }
                    break;
            }
        }

        //# ifdef __STATE_H__
        /* FM channel save , internal state only */
        private void FMsave_state_adpcma(device_config device, ADPCM_CH[] adpcm)
        {
            int ch;

            for (ch = 0; ch < 6; ch++)
            {
                state_save_register_device_item(device, ch, adpcm[ch].flag);
                state_save_register_device_item(device, ch, adpcm[ch].now_data);
                state_save_register_device_item(device, ch, adpcm[ch].now_addr);
                state_save_register_device_item(device, ch, adpcm[ch].now_step);
                state_save_register_device_item(device, ch, adpcm[ch].adpcm_acc);
                state_save_register_device_item(device, ch, adpcm[ch].adpcm_step);
                state_save_register_device_item(device, ch, adpcm[ch].adpcm_out);
            }
        }
        //#endif /* _STATE_H */

        //#endif /* (BUILD_YM2608||BUILD_YM2610||BUILD_YM2610B) */


        //#if BUILD_YM2608
        /*****************************************************************************/
        /*      YM2608 local section                                                 */
        /*****************************************************************************/



        private ushort[] YM2608_ADPCM_ROM_addr = new ushort[2 * 6] {
            0x0000, 0x01bf, /* bass drum  */
            0x01c0, 0x043f, /* snare drum */
            0x0440, 0x1b7f, /* top cymbal */
            0x1b80, 0x1cff, /* high hat */
            0x1d00, 0x1f7f, /* tom tom  */
            0x1f80, 0x1fff  /* rim shot */
        };

        /*
            This data is derived from the chip's output - internal ROM can't be read.
            It was verified, using real YM2608, that this ADPCM stream produces 100% correct output signal.
        */

        private byte[] YM2608_ADPCM_ROM = new byte[0x2000]{

        /* Source: 01BD.ROM */
        /* Length: 448 / 0x000001C0 */

        0x88,0x08,0x08,0x08,0x00,0x88,0x16,0x76,0x99,0xB8,0x22,0x3A,0x84,0x3C,0xB1,0x54,
        0x10,0xA9,0x98,0x32,0x80,0x33,0x9A,0xA7,0x4A,0xB4,0x58,0xBC,0x15,0x29,0x8A,0x97,
        0x9B,0x44,0xAC,0x80,0x12,0xDE,0x13,0x1B,0xC0,0x58,0xC8,0x11,0x0A,0xA2,0x1A,0xA0,
        0x00,0x98,0x0B,0x93,0x9E,0x92,0x0A,0x88,0xBE,0x14,0x1B,0x98,0x08,0xA1,0x4A,0xC1,
        0x30,0xD9,0x33,0x98,0x10,0x89,0x17,0x1A,0x82,0x29,0x37,0x0C,0x83,0x50,0x9A,0x24,
        0x1A,0x83,0x10,0x23,0x19,0xB3,0x72,0x8A,0x16,0x10,0x0A,0x93,0x70,0x99,0x23,0x99,
        0x02,0x20,0x91,0x18,0x02,0x41,0xAB,0x24,0x18,0x81,0x99,0x4A,0xE8,0x28,0x9A,0x99,
        0xA1,0x2F,0xA8,0x9D,0x90,0x08,0xCC,0xA3,0x1D,0xCA,0x82,0x0B,0xD8,0x08,0xB9,0x09,
        0xBC,0xB8,0x00,0xBE,0x90,0x1B,0xCA,0x00,0x9B,0x8A,0xA8,0x91,0x0F,0xB3,0x3D,0xB8,
        0x31,0x0B,0xA5,0x0A,0x11,0xA1,0x48,0x92,0x10,0x50,0x91,0x30,0x23,0x09,0x37,0x39,
        0xA2,0x72,0x89,0x92,0x30,0x83,0x1C,0x96,0x28,0xB9,0x24,0x8C,0xA1,0x31,0xAD,0xA9,
        0x13,0x9C,0xBA,0xA8,0x0B,0xBF,0xB8,0x9B,0xCA,0x88,0xDB,0xB8,0x19,0xFC,0x92,0x0A,
        0xBA,0x89,0xAB,0xB8,0xAB,0xD8,0x08,0xAD,0xBA,0x33,0x9D,0xAA,0x83,0x3A,0xC0,0x40,
        0xB9,0x15,0x39,0xA2,0x52,0x89,0x02,0x63,0x88,0x13,0x23,0x03,0x52,0x02,0x54,0x00,
        0x11,0x23,0x23,0x35,0x20,0x01,0x44,0x41,0x80,0x24,0x40,0xA9,0x45,0x19,0x81,0x12,
        0x81,0x02,0x11,0x21,0x19,0x02,0x61,0x8A,0x13,0x3A,0x10,0x12,0x23,0x8B,0x37,0x18,
        0x91,0x24,0x10,0x81,0x34,0x20,0x05,0x32,0x82,0x53,0x20,0x14,0x33,0x31,0x34,0x52,
        0x00,0x43,0x32,0x13,0x52,0x22,0x13,0x52,0x11,0x43,0x11,0x32,0x32,0x32,0x22,0x02,
        0x13,0x12,0x89,0x22,0x19,0x81,0x81,0x08,0xA8,0x08,0x8B,0x90,0x1B,0xBA,0x8A,0x9B,
        0xB9,0x89,0xCA,0xB9,0xAB,0xCA,0x9B,0xCA,0xB9,0xAB,0xDA,0x99,0xAC,0xBB,0x9B,0xAC,
        0xAA,0xBA,0xAC,0xAB,0x9A,0xAA,0xAA,0xBA,0xB8,0xA9,0xBA,0x99,0xA9,0x9A,0xA0,0x8A,
        0xA9,0x08,0x8A,0xA9,0x00,0x99,0x89,0x88,0x98,0x08,0x99,0x00,0x89,0x80,0x08,0x98,
        0x00,0x88,0x88,0x80,0x90,0x80,0x90,0x80,0x81,0x99,0x08,0x88,0x99,0x09,0x00,0x1A,
        0xA8,0x10,0x9A,0x88,0x08,0x0A,0x8A,0x89,0x99,0xA8,0x98,0xA9,0x99,0x99,0xA9,0x99,
        0xAA,0x8A,0xAA,0x9B,0x8A,0x9A,0xA9,0x9A,0xBA,0x99,0x9A,0xAA,0x99,0x89,0xA9,0x99,
        0x98,0x9A,0x98,0x88,0x09,0x89,0x09,0x08,0x08,0x09,0x18,0x18,0x00,0x12,0x00,0x11,
        0x11,0x11,0x12,0x12,0x21,0x21,0x22,0x22,0x22,0x22,0x22,0x22,0x32,0x31,0x32,0x31,
        0x32,0x32,0x21,0x31,0x21,0x32,0x21,0x12,0x00,0x80,0x80,0x80,0x80,0x80,0x80,0x80,

        /* Source: 02SD.ROM */
        /* Length: 640 / 0x00000280 */

        0x0A,0xDC,0x14,0x0B,0xBA,0xBC,0x01,0x0F,0xF5,0x2F,0x87,0x19,0xC9,0x24,0x1B,0xA1,
        0x31,0x99,0x90,0x32,0x32,0xFE,0x83,0x48,0xA8,0xA9,0x23,0x19,0xBC,0x91,0x02,0x41,
        0xDE,0x81,0x28,0xA8,0x0A,0xB1,0x72,0xDA,0x23,0xBC,0x04,0x19,0xB8,0x21,0x8A,0x03,
        0x29,0xBA,0x14,0x21,0x0B,0xC0,0x43,0x08,0x91,0x50,0x93,0x0F,0x86,0x1A,0x91,0x18,
        0x21,0xCB,0x27,0x0A,0xA1,0x42,0x8C,0xA9,0x21,0x10,0x08,0xAB,0x94,0x2A,0xDA,0x02,
        0x8B,0x91,0x09,0x98,0xAE,0x80,0xA9,0x02,0x0A,0xE9,0x21,0xBB,0x15,0x20,0xBE,0x92,
        0x42,0x09,0xA9,0x11,0x34,0x08,0x12,0x0A,0x27,0x29,0xA1,0x52,0x12,0x8E,0x92,0x28,
        0x92,0x2B,0xD1,0x23,0xBF,0x81,0x10,0x99,0xA8,0x0A,0xC4,0x3B,0xB9,0xB0,0x00,0x62,
        0xCF,0x92,0x29,0x92,0x2B,0xB1,0x1C,0xB2,0x72,0xAA,0x88,0x11,0x18,0x80,0x13,0x9E,
        0x03,0x18,0xB0,0x60,0xA1,0x28,0x88,0x08,0x04,0x10,0x8F,0x96,0x19,0x90,0x01,0x09,
        0xC8,0x50,0x91,0x8A,0x01,0xAB,0x03,0x50,0xBA,0x9D,0x93,0x68,0xBA,0x80,0x22,0xCB,
        0x41,0xBC,0x92,0x60,0xB9,0x1A,0x95,0x4A,0xC8,0x20,0x88,0x33,0xAC,0x92,0x38,0x83,
        0x09,0x80,0x16,0x09,0x29,0xD0,0x54,0x8C,0xA2,0x28,0x91,0x89,0x93,0x60,0xCD,0x85,
        0x1B,0xA1,0x49,0x90,0x8A,0x80,0x34,0x0C,0xC9,0x14,0x19,0x98,0xA0,0x40,0xA9,0x21,
        0xD9,0x34,0x0A,0xA9,0x10,0x23,0xCB,0x25,0xAA,0x25,0x9B,0x13,0xCD,0x16,0x09,0xA0,
        0x80,0x01,0x19,0x90,0x88,0x21,0xAC,0x33,0x8B,0xD8,0x27,0x3B,0xB8,0x81,0x31,0x80,
        0xAF,0x97,0x0A,0x82,0x0A,0xA0,0x21,0x89,0x8A,0xA2,0x32,0x8D,0xBB,0x87,0x19,0x21,
        0xC9,0xBC,0x45,0x09,0x90,0x09,0xA1,0x24,0x1A,0xD0,0x10,0x08,0x11,0xA9,0x21,0xE8,
        0x60,0xA9,0x14,0x0C,0xD1,0x32,0xAB,0x04,0x0C,0x81,0x90,0x29,0x83,0x9B,0x01,0x8F,
        0x97,0x0B,0x82,0x18,0x88,0xBA,0x06,0x39,0xC8,0x23,0xBC,0x04,0x09,0x92,0x08,0x1A,
        0xBB,0x74,0x8C,0x81,0x18,0x81,0x9D,0x83,0x41,0xCD,0x81,0x40,0x9A,0x90,0x10,0x12,
        0x9C,0xA1,0x68,0xD8,0x33,0x9C,0x91,0x01,0x12,0xBE,0x02,0x09,0x12,0x99,0x9A,0x36,
        0x0A,0xB0,0x30,0x88,0xA3,0x2D,0x12,0xBC,0x03,0x3A,0x11,0xBD,0x08,0xC8,0x62,0x80,
        0x8B,0xD8,0x23,0x38,0xF9,0x12,0x08,0x99,0x91,0x21,0x99,0x85,0x2F,0xB2,0x30,0x90,
        0x88,0xD9,0x53,0xAC,0x82,0x19,0x91,0x20,0xCC,0x96,0x29,0xC9,0x24,0x89,0x80,0x99,
        0x12,0x08,0x18,0x88,0x99,0x23,0xAB,0x73,0xCB,0x33,0x9F,0x04,0x2B,0xB1,0x08,0x03,
        0x1B,0xC9,0x21,0x32,0xFA,0x33,0xDB,0x02,0x33,0xAE,0xB9,0x54,0x8B,0xA1,0x20,0x89,
        0x90,0x11,0x88,0x09,0x98,0x23,0xBE,0x37,0x8D,0x81,0x20,0xAA,0x34,0xBB,0x13,0x18,
        0xB9,0x40,0xB1,0x18,0x83,0x8E,0xB2,0x72,0xBC,0x82,0x30,0xA9,0x9A,0x24,0x8B,0x27,
        0x0E,0x91,0x20,0x90,0x08,0xB0,0x32,0xB9,0x21,0xB0,0xAC,0x45,0x9A,0xA1,0x50,0xA9,
        0x80,0x0A,0x26,0x9B,0x11,0xBB,0x23,0x71,0xCB,0x12,0x10,0xB8,0x40,0xA9,0xA5,0x39,
        0xC0,0x30,0xB2,0x20,0xAA,0xBA,0x76,0x1C,0xC1,0x48,0x98,0x80,0x18,0x81,0xAA,0x23,
        0x9C,0xA2,0x32,0xAC,0x9A,0x43,0x9C,0x12,0xAD,0x82,0x72,0xBC,0x00,0x82,0x39,0xD1,
        0x3A,0xB8,0x35,0x9B,0x10,0x40,0xF9,0x22,0x0A,0xC0,0x51,0xB9,0x82,0x18,0x98,0xA3,
        0x79,0xD0,0x20,0x88,0x09,0x01,0x99,0x82,0x11,0x38,0xFC,0x33,0x09,0xC8,0x40,0xA9,
        0x11,0x29,0xAA,0x94,0x3A,0xC2,0x4A,0xC0,0x89,0x52,0xBC,0x11,0x08,0x09,0xB8,0x71,
        0xA9,0x08,0xA8,0x62,0x8D,0x92,0x10,0x00,0x9E,0x94,0x38,0xBA,0x13,0x88,0x90,0x4A,
        0xE2,0x30,0xBA,0x02,0x00,0x19,0xD9,0x62,0xBB,0x04,0x0B,0xA3,0x68,0xB9,0x21,0x88,
        0x9D,0x04,0x10,0x8C,0xC8,0x62,0x99,0xAA,0x24,0x1A,0x80,0x9A,0x14,0x9B,0x26,0x8C,
        0x92,0x30,0xB9,0x09,0xA3,0x71,0xBB,0x10,0x19,0x82,0x39,0xDB,0x02,0x44,0x9F,0x10,

        /* Source: 04TOP.ROM */
        /* Length: 5952 / 0x00001740 */

        0x07,0xFF,0x7C,0x3C,0x31,0xC6,0xC4,0xBB,0x7F,0x7F,0x7B,0x82,0x8A,0x4D,0x5F,0x7C,
        0x3E,0x44,0xD2,0xB3,0xA0,0x19,0x1B,0x6C,0x81,0x28,0xC4,0xA1,0x1C,0x4B,0x18,0x00,
        0x2A,0xA2,0x0A,0x7C,0x2A,0x00,0x01,0x89,0x98,0x48,0x8A,0x3C,0x28,0x2A,0x5B,0x3E,
        0x3A,0x1A,0x3B,0x3D,0x4B,0x3B,0x4A,0x08,0x2A,0x1A,0x2C,0x4A,0x3B,0x82,0x99,0x3C,
        0x5D,0x29,0x2B,0x39,0x0B,0x23,0xAB,0x1A,0x4C,0x79,0xA3,0x01,0xC1,0x2A,0x0A,0x38,
        0xA7,0xB9,0x12,0x1F,0x29,0x08,0x82,0xA1,0x08,0xA9,0x42,0xAA,0x95,0xB3,0x90,0x81,
        0x09,0xD4,0x1A,0x80,0x1B,0x07,0xB8,0x12,0x8E,0x49,0x81,0x92,0xD3,0x90,0xA1,0x2A,
        0x02,0xE1,0xA3,0x99,0x02,0xB3,0x94,0xB3,0xB0,0xF4,0x98,0x93,0x90,0x13,0xE1,0x81,
        0x99,0x38,0x91,0xA6,0xD3,0x99,0x94,0xC1,0x83,0xB1,0x92,0x98,0x49,0xC4,0xB2,0xA4,
        0xA3,0xD0,0x1A,0x30,0xBA,0x59,0x02,0xD4,0xA0,0xA4,0xA2,0x8A,0x01,0x00,0xB7,0xA8,
        0x18,0x2A,0x2B,0x1E,0x23,0xC8,0x1A,0x00,0x39,0xA0,0x18,0x92,0x4F,0x2D,0x5A,0x10,
        0x89,0x81,0x2A,0x8B,0x6A,0x02,0x09,0xB3,0x8D,0x48,0x1B,0x80,0x19,0x34,0xF8,0x29,
        0x0A,0x7B,0x2A,0x28,0x81,0x0C,0x02,0x1E,0x29,0x09,0x12,0xC2,0x94,0xE1,0x18,0x98,
        0x02,0xC4,0x89,0x91,0x1A,0x20,0xA9,0x02,0x1B,0x48,0x8E,0x20,0x88,0x2D,0x08,0x59,
        0x1B,0x02,0xA3,0xB1,0x8A,0x1E,0x58,0x80,0xC2,0xB6,0x88,0x91,0x88,0x11,0xA1,0xA3,
        0xE2,0x01,0xB0,0x19,0x11,0x09,0xF4,0x88,0x09,0x88,0x19,0x89,0x12,0xF1,0x2A,0x28,
        0x8C,0x25,0x99,0xA4,0x98,0x39,0xA1,0x00,0xD0,0x58,0xAA,0x59,0x01,0x0C,0x00,0x2B,
        0x00,0x08,0x89,0x6B,0x69,0x90,0x01,0x90,0x98,0x12,0xB3,0xF3,0xA0,0x89,0x02,0x3B,
        0x0C,0x50,0xA9,0x4E,0x6B,0x19,0x28,0x09,0xA2,0x08,0x2F,0x20,0x88,0x92,0x8A,0x11,
        0xC4,0x93,0xF1,0x18,0x88,0x11,0xF2,0x80,0x92,0xA8,0x02,0xA8,0xB7,0xB3,0xA3,0xA0,
        0x88,0x1A,0x40,0xE2,0x91,0x19,0x88,0x18,0x91,0x83,0xC1,0xB5,0x92,0xA9,0xC6,0x90,
        0x01,0xC2,0x81,0x98,0x03,0xF0,0x00,0x2C,0x2A,0x92,0x2C,0x83,0x1F,0x3A,0x29,0x00,
        0xB8,0x70,0xAB,0x69,0x18,0x89,0x10,0x0D,0x12,0x0B,0x88,0x4A,0x3A,0x9B,0x70,0xA8,
        0x28,0x2F,0x2A,0x3A,0x1B,0x85,0x88,0x8B,0x6A,0x29,0x00,0x91,0x91,0x1B,0x7C,0x29,
        0x01,0x88,0x90,0x19,0x2B,0x2B,0x00,0x39,0xA8,0x5E,0x21,0x89,0x91,0x09,0x3A,0x6F,
        0x2A,0x18,0x18,0x8B,0x50,0x89,0x2B,0x19,0x49,0x88,0x29,0xF5,0x89,0x08,0x09,0x12,
        0xAA,0x15,0xB0,0x82,0xAC,0x38,0x00,0x3F,0x81,0x10,0xB0,0x49,0xA2,0x81,0x3A,0xC8,
        0x87,0x90,0xC4,0xA3,0x99,0x19,0x83,0xE1,0x84,0xE2,0xA2,0x90,0x80,0x93,0xB5,0xC4,
        0xB3,0xA1,0x0A,0x18,0x92,0xC4,0xA0,0x93,0x0C,0x3A,0x18,0x01,0x1E,0x20,0xB1,0x82,
        0x8C,0x03,0xB5,0x2E,0x82,0x19,0xB2,0x1B,0x1B,0x6B,0x4C,0x19,0x12,0x8B,0x5A,0x11,
        0x0C,0x3A,0x2C,0x18,0x3D,0x08,0x2A,0x5C,0x18,0x00,0x88,0x3D,0x29,0x80,0x2A,0x09,
        0x00,0x7A,0x0A,0x10,0x0B,0x69,0x98,0x10,0x81,0x3F,0x00,0x18,0x19,0x91,0xB7,0x9A,
        0x28,0x8A,0x48,0x92,0xF3,0xA2,0x88,0x98,0x87,0xA1,0x88,0x80,0x81,0x95,0xD1,0xA3,
        0x1B,0x1C,0x39,0x10,0xA1,0x2A,0x0B,0x7A,0x4B,0x80,0x13,0xC1,0xD1,0x2B,0x2A,0x85,
        0xB2,0xA2,0x93,0xB2,0xD3,0x80,0xD1,0x18,0x08,0x08,0xB7,0x98,0x81,0x3F,0x01,0x88,
        0x01,0xE2,0x00,0x9A,0x59,0x08,0x10,0xC3,0x99,0x84,0xA9,0xA5,0x91,0x91,0x91,0x80,
        0xB5,0x94,0xC0,0x01,0x98,0x09,0x84,0xB0,0x80,0x7A,0x08,0x18,0x90,0xA8,0x6A,0x1C,
        0x39,0x2A,0xB7,0x98,0x19,0x10,0x2A,0xA1,0x10,0xBD,0x39,0x18,0x2D,0x39,0x3F,0x10,
        0x3F,0x01,0x09,0x19,0x0A,0x38,0x8C,0x40,0xB3,0xB4,0x93,0xAD,0x20,0x2B,0xD4,0x81,
        0xC3,0xB0,0x39,0xA0,0x23,0xD8,0x04,0xB1,0x9B,0xA7,0x1A,0x92,0x08,0xA5,0x88,0x81,
        0xE2,0x01,0xB8,0x01,0x81,0xC1,0xC7,0x90,0x92,0x80,0xA1,0x97,0xA0,0xA2,0x82,0xB8,
        0x18,0x00,0x9C,0x78,0x98,0x83,0x0B,0x0B,0x32,0x7D,0x19,0x10,0xA1,0x19,0x09,0x0A,
        0x78,0xA8,0x10,0x1B,0x29,0x29,0x1A,0x14,0x2F,0x88,0x4A,0x1B,0x10,0x10,0xAB,0x79,
        0x0D,0x49,0x18,0xA0,0x02,0x1F,0x19,0x3A,0x2B,0x11,0x8A,0x88,0x79,0x8A,0x20,0x49,
        0x9B,0x58,0x0B,0x28,0x18,0xA9,0x3A,0x7D,0x00,0x29,0x88,0x82,0x3D,0x1A,0x38,0xBA,
        0x15,0x09,0xAA,0x51,0x8B,0x83,0x3C,0x8A,0x58,0x1B,0xB5,0x01,0xBB,0x50,0x19,0x99,
        0x24,0xCA,0x21,0x1B,0xA2,0x87,0xA8,0xB1,0x68,0xA1,0xA6,0xA2,0xA8,0x29,0x8B,0x24,
        0xB4,0xE2,0x92,0x8A,0x00,0x19,0x93,0xB5,0xB4,0xB1,0x81,0xB1,0x03,0x9A,0x82,0xA7,
        0x90,0xD6,0xA0,0x80,0x1B,0x29,0x01,0xA4,0xE1,0x18,0x0A,0x2A,0x29,0x92,0xC7,0xA8,
        0x81,0x19,0x89,0x30,0x10,0xE0,0x30,0xB8,0x10,0x0C,0x1A,0x79,0x1B,0xA7,0x80,0xA0,
        0x00,0x0B,0x28,0x18,0xB1,0x85,0x1E,0x00,0x20,0xA9,0x18,0x18,0x1C,0x13,0xBC,0x15,
        0x99,0x2E,0x12,0x00,0xE1,0x00,0x0B,0x3B,0x21,0x90,0x06,0xC9,0x2A,0x49,0x0A,0x18,
        0x20,0xD1,0x3C,0x08,0x00,0x83,0xC9,0x41,0x8E,0x18,0x08,0x02,0xA0,0x09,0xA4,0x7B,
        0x90,0x19,0x2A,0x10,0x2A,0xA8,0x71,0xBA,0x10,0x4A,0x0E,0x22,0xB2,0xB2,0x1B,0x8C,
        0x78,0x1A,0xB5,0x93,0xA9,0x1B,0x49,0x19,0x29,0xA3,0xC6,0x88,0xAA,0x32,0x0D,0x1B,
        0x22,0x08,0xC2,0x18,0xB9,0x79,0x3F,0x01,0x10,0xA9,0x84,0x1C,0x09,0x21,0xB0,0xA7,
        0x0A,0x99,0x50,0x0C,0x81,0x28,0x8B,0x48,0x2E,0x00,0x08,0x99,0x38,0x5B,0x88,0x14,
        0xA9,0x08,0x11,0xAA,0x72,0xC1,0xB3,0x09,0x8A,0x05,0x91,0xF2,0x81,0xA1,0x09,0x02,
        0xF2,0x92,0x99,0x1A,0x49,0x80,0xC5,0x90,0x90,0x18,0x09,0x12,0xA1,0xF2,0x81,0x98,
        0xC6,0x91,0xA0,0x11,0xA0,0x94,0xB4,0xF2,0x81,0x8B,0x03,0x80,0xD2,0x93,0xA8,0x88,
        0x69,0xA0,0x03,0xB8,0x88,0x32,0xBC,0x97,0x80,0xB1,0x3B,0x1A,0xA6,0x00,0xD1,0x01,
        0x0B,0x3B,0x30,0x9B,0x31,0x3E,0x92,0x19,0x8A,0xD3,0x5C,0x1B,0x41,0xA0,0x93,0xA2,
        0xAF,0x39,0x4C,0x01,0x92,0xA8,0x81,0x3C,0x0D,0x78,0x98,0x00,0x19,0x0A,0x20,0x2D,
        0x29,0x3C,0x1B,0x48,0x88,0x99,0x7A,0x2D,0x29,0x2A,0x82,0x80,0xA8,0x49,0x3E,0x19,
        0x11,0x98,0x82,0x9A,0x3B,0x28,0x2F,0x20,0x4C,0x90,0x29,0x19,0x9A,0x7A,0x29,0x28,
        0x98,0x88,0x33,0xCD,0x11,0x3A,0xC1,0xA4,0xA0,0xC4,0x82,0xC8,0x50,0x98,0xB2,0x21,
        0xC0,0xB6,0x98,0x82,0x80,0x9C,0x23,0x00,0xF8,0x30,0xA8,0x1A,0x68,0xA8,0x86,0x9A,
        0x01,0x2A,0x0A,0x97,0x91,0xC1,0x18,0x89,0x02,0x83,0xE0,0x01,0x8B,0x29,0x30,0xE2,
        0x91,0x0B,0x18,0x3B,0x1C,0x11,0x28,0xAC,0x78,0x80,0x93,0x91,0xA9,0x49,0x8B,0x87,
        0x90,0x99,0x3D,0x5A,0x81,0x08,0xA1,0x11,0x2F,0x1A,0x21,0x9B,0x15,0xA2,0xB0,0x11,
        0xC0,0x91,0x5B,0x98,0x24,0xA2,0xF2,0x92,0x8B,0x6A,0x18,0x81,0xB5,0xB1,0x88,0x4C,
        0x00,0x00,0xA4,0xC1,0x2B,0x1A,0x59,0x0A,0x02,0x80,0x1E,0x02,0x08,0xB3,0x80,0x9A,
        0x23,0xB8,0xF2,0x84,0xAB,0x01,0x48,0x90,0xA7,0x90,0x0A,0x29,0x09,0x95,0x99,0xA0,
        0x59,0x2B,0x00,0x97,0xB0,0x29,0x89,0x2A,0x03,0xD0,0xB7,0x1B,0x81,0x00,0xA6,0xB1,
        0x90,0x09,0x48,0xC0,0x11,0x00,0x8A,0x00,0x5B,0x83,0x9A,0x18,0x2F,0x3C,0x18,0x11,
        0xA9,0x04,0x1A,0x4F,0x01,0x98,0x81,0x09,0x09,0x4A,0x18,0xB4,0xA2,0x0B,0x59,0x90,
        0x3B,0x49,0xBC,0x40,0x6A,0x88,0x3A,0x08,0x3E,0x3A,0x80,0x93,0xB0,0xE1,0x5A,0x00,
        0xA4,0xB3,0xE3,0x90,0x0D,0x38,0x09,0x82,0xC4,0xA1,0xB1,0x4C,0x18,0x10,0x91,0xB2,
        0x13,0xEA,0x34,0x99,0x88,0xA6,0x89,0x92,0x91,0xC1,0x20,0xB2,0xC2,0x86,0xD2,0xB3,
        0x80,0xB2,0x08,0x09,0x87,0x91,0xC0,0x11,0x89,0x90,0x28,0xB9,0x79,0x19,0xA4,0x82,
        0xD0,0x03,0x0C,0xA3,0xA5,0xB2,0xB2,0x1B,0x29,0x13,0xF1,0xB4,0x81,0x9D,0x38,0x00,
        0xC4,0xA1,0x89,0x59,0x1A,0x81,0xA4,0xA9,0x1C,0x6A,0x19,0x02,0xB1,0x1A,0x4A,0x0B,
        0x78,0x89,0x81,0x1C,0x2A,0x29,0x4A,0xA3,0x3E,0x1C,0x49,0x1A,0x08,0x21,0xAE,0x28,
        0x4B,0x19,0x20,0x8C,0x10,0x3A,0xAB,0x26,0x8B,0x18,0x59,0x99,0x13,0xA2,0xAB,0x79,
        0x2F,0x18,0x10,0xB2,0x80,0x1B,0x4D,0x5A,0x80,0x82,0x98,0x81,0x80,0x09,0xA5,0x90,
        0x91,0x03,0xC2,0xE2,0x81,0xA8,0x82,0x09,0xC6,0xA3,0xB1,0x08,0x5B,0x08,0x05,0xD1,
        0xA2,0x89,0x2A,0x28,0x91,0xA6,0x88,0xB0,0x49,0x80,0x09,0x08,0x88,0x07,0xB8,0x05,
        0x99,0x81,0x88,0x18,0xE2,0x00,0xC3,0x18,0x0D,0x10,0x30,0xD0,0x93,0x8A,0x09,0x10,
        0x2F,0x11,0x90,0xA1,0x20,0x9B,0xB1,0x73,0xC8,0x94,0x98,0x3B,0x01,0x0C,0x30,0x19,
        0xF8,0x12,0x90,0xBA,0x78,0x0A,0x11,0x98,0xA0,0x79,0x8A,0x30,0x2B,0xC2,0x11,0x0D,
        0x09,0x7A,0x00,0x82,0xB9,0x01,0x7A,0x89,0x21,0x09,0xA1,0x0A,0x7C,0x10,0x88,0xB5,
        0x88,0x0A,0x2B,0x69,0x1A,0x10,0xA0,0x5B,0x19,0x1A,0x10,0x19,0x1A,0x6C,0x20,0x90,
        0xA5,0x98,0x1B,0x0A,0x69,0x82,0xD1,0x18,0x09,0x19,0x2A,0x93,0xD4,0x9A,0x01,0x49,
        0xA2,0xA2,0x82,0xD8,0x22,0xAA,0x97,0xA9,0x2D,0x38,0x2A,0xB6,0x80,0x90,0x0A,0x3C,
        0x82,0x94,0xB8,0x21,0x0E,0x2A,0x22,0xB8,0x00,0x4F,0x2B,0x3A,0x81,0xA1,0x29,0x2C,
        0x6A,0x13,0xD1,0xA2,0x98,0x28,0x0C,0x01,0xD5,0x08,0xA9,0x31,0xB3,0xB0,0xA7,0xB0,
        0x29,0x1B,0x87,0xA2,0xA1,0xB2,0x4A,0x89,0x11,0xC3,0xF3,0x98,0x08,0x03,0xA0,0xA3,
        0xC5,0x90,0xB3,0xB5,0xB4,0xB8,0x02,0x91,0x91,0xD3,0xA4,0xC1,0x1B,0x82,0x28,0xA4,
        0xD1,0x94,0x8A,0x28,0x08,0x03,0xE0,0x80,0xD4,0x90,0x91,0xA1,0x3B,0x3D,0x02,0xE4,
        0xA1,0x92,0x89,0x1A,0x4B,0x95,0xB3,0x90,0x99,0x6A,0x0A,0x30,0xA1,0x93,0xA6,0xA9,
        0x85,0x8B,0x82,0x10,0xB1,0xA3,0x94,0xF8,0x38,0x9A,0x30,0x1A,0x8B,0xA7,0x89,0x01,
        0x5B,0x19,0x18,0x11,0xF0,0x18,0x1C,0x39,0x19,0x0C,0x12,0x1C,0x2A,0x7B,0x3A,0x88,
        0x2B,0x18,0x2B,0x5C,0x20,0x92,0x8D,0x38,0x8A,0x3A,0x5B,0x2E,0x3A,0x2B,0x10,0x12,
        0xBB,0x6A,0x4D,0x18,0x10,0xB1,0x81,0x2A,0x8B,0x79,0x80,0x01,0x0A,0x09,0x5B,0x2D,
        0x84,0x8A,0x08,0x02,0xA2,0x91,0x82,0xE8,0x50,0x9B,0x85,0xA3,0xB0,0xA3,0x1B,0x02,
        0x18,0xF3,0xA2,0x88,0xAB,0x53,0xD1,0xB4,0xA3,0x09,0x09,0x18,0xD4,0x08,0xB0,0x09,
        0x58,0xD1,0x82,0x89,0x81,0x1A,0x18,0x05,0xB9,0xC3,0x30,0xC0,0x95,0x80,0xC3,0x89,
        0x89,0x13,0x88,0xF2,0x93,0x0E,0x18,0x01,0x92,0xA5,0xB8,0x2A,0x39,0xAA,0x33,0x9A,
        0xB1,0x11,0xF5,0xA1,0xA1,0x0A,0x50,0xB8,0x03,0xC4,0xA0,0x4E,0x29,0x10,0x88,0xC2,
        0x1A,0x39,0x1D,0x28,0x98,0x94,0x0E,0x10,0x2A,0x3C,0x02,0x2D,0x1B,0x4B,0x3B,0x49,
        0x19,0xA9,0x48,0x2F,0x29,0x10,0x89,0x02,0x0C,0x10,0x09,0xB9,0x70,0x1B,0x8A,0x50,
        0xA8,0x2B,0x49,0x89,0x69,0x88,0x95,0x89,0x90,0x92,0x4C,0x19,0x82,0xC1,0x01,0x80,
        0xA0,0x2B,0x7A,0x81,0x10,0xC2,0xB7,0x98,0x88,0x19,0x2C,0x03,0xB1,0xA4,0xA1,0x0C,
        0x3B,0x78,0x88,0x85,0xB1,0xA0,0x1B,0x3A,0x4A,0x08,0x94,0x81,0xF1,0x80,0x00,0x0C,
        0x59,0x09,0x18,0x90,0xA6,0x92,0x8C,0x1A,0x79,0x92,0xA8,0x00,0x81,0x2E,0x2A,0x13,
        0xA2,0xB0,0xA5,0x88,0x88,0x89,0x11,0x19,0xA0,0xF3,0x82,0xB0,0x83,0x5F,0x2A,0x01,
        0xA1,0x94,0xB0,0x09,0x78,0x98,0xA3,0xA6,0xA0,0x91,0x80,0x93,0x98,0xC1,0x12,0x18,
        0xC9,0x17,0xA0,0xA0,0x1A,0x21,0x80,0x99,0xD4,0x30,0x9D,0x00,0x10,0x2F,0x08,0x1C,
        0x21,0x08,0xB4,0xC3,0x2B,0xA9,0x52,0xD2,0xA3,0xD1,0x09,0x10,0x8B,0x24,0x92,0xD1,
        0x80,0x19,0xA0,0x2C,0x12,0x49,0xAA,0xB6,0x95,0xB8,0x08,0x3A,0x2B,0x01,0xF3,0xB3,
        0x0B,0x09,0x79,0x18,0xA2,0xA4,0xA0,0x18,0x0C,0x20,0x08,0xA9,0x16,0x0C,0x00,0x1B,
        0x08,0x2B,0x7B,0x01,0x01,0xB9,0x59,0x19,0x8B,0x45,0xA8,0x80,0x0C,0x1A,0x41,0x1E,
        0x00,0x28,0xA8,0x5A,0x00,0xC1,0x49,0x99,0x21,0x1D,0x08,0x85,0x99,0x95,0x89,0x90,
        0x11,0x90,0xD1,0x28,0xB2,0xA7,0x99,0x81,0x02,0xAC,0x13,0x81,0xB2,0xA6,0xA9,0x28,
        0x1C,0xB1,0x33,0xD1,0xC1,0x58,0xA8,0x14,0xB0,0xB7,0x91,0xA0,0x82,0x89,0xC2,0x28,
        0xA1,0xB2,0x49,0xD2,0x94,0xC8,0x12,0x80,0x99,0x85,0x08,0xD3,0x09,0xA2,0xB3,0x1E,
        0x08,0x21,0xB9,0x23,0xB4,0xAB,0x41,0xAC,0x87,0x09,0xA2,0xC5,0x0B,0x2A,0x5A,0x91,
        0x20,0x9A,0x89,0x78,0x9B,0x31,0x89,0x80,0x29,0x0A,0xB7,0x3C,0x98,0x48,0x1D,0x00,
        0x01,0xB0,0x20,0x2F,0x29,0x4A,0x89,0x94,0x1C,0x88,0x28,0x2B,0x10,0x88,0x9A,0x71,
        0x9A,0x08,0x4A,0x2F,0x18,0x2B,0x18,0x02,0xA8,0x4B,0x7A,0x99,0x48,0x80,0xA8,0x20,
        0x1D,0x40,0xA8,0x10,0x08,0xA8,0xC5,0x88,0xC2,0x18,0x88,0x2A,0x12,0xF3,0x82,0xD8,
        0x20,0x0A,0x09,0xA6,0x98,0x04,0xB9,0x11,0x18,0xC3,0xE1,0x29,0xA1,0x11,0xC1,0x03,
        0xE2,0x9A,0x33,0xA9,0xB5,0x98,0x92,0xA1,0x02,0xF8,0x21,0xA8,0x10,0x02,0xC1,0xB7,
        0x1B,0x90,0x5B,0x3C,0x83,0x93,0xE0,0x19,0x1A,0x11,0x11,0xF1,0x92,0x89,0x19,0x2C,
        0x2C,0x41,0x99,0x92,0x90,0x3F,0x18,0x4B,0x00,0x08,0xD2,0x01,0xB2,0xAA,0x78,0x09,
        0x01,0x91,0xA2,0x98,0x2F,0x3A,0x2C,0x01,0x00,0x93,0xE0,0x28,0x2C,0x2B,0x01,0x12,
        0xE1,0x80,0xB3,0x3D,0x3A,0x0A,0x50,0x98,0xC2,0xA0,0x11,0xAA,0x30,0x87,0x90,0xC2,
        0x29,0x88,0x38,0xC8,0xB5,0x90,0xBA,0x70,0x1A,0x02,0x94,0xD0,0x80,0x1A,0x82,0xA6,
        0xB0,0x91,0x18,0xB3,0x00,0x13,0xF1,0xA2,0xC1,0x82,0xB0,0x00,0x15,0x0B,0xD3,0x02,
        0xA8,0x91,0x2B,0x1F,0x49,0x88,0xA6,0x80,0x88,0x08,0x1B,0xA5,0x80,0xB9,0x06,0x0B,
        0x90,0x21,0x9D,0x48,0x18,0xA0,0x15,0xC9,0x82,0x2B,0x1A,0x42,0x9A,0xC4,0x39,0xBC,
        0x69,0x00,0xA0,0x29,0x8C,0x39,0x59,0x08,0x09,0x49,0xA9,0x6B,0x81,0x00,0x98,0xB0,
        0x68,0x3D,0x81,0x88,0x18,0x19,0x1D,0x12,0x80,0xB2,0x3A,0x3F,0x85,0x92,0xD0,0x00,
        0x0A,0x19,0x12,0xF1,0x02,0x9B,0x19,0x40,0xB9,0x11,0x02,0xF2,0x1A,0x08,0x94,0x0A,
        0xC2,0x83,0x0B,0xB4,0xA4,0xC0,0x32,0xD8,0x86,0x98,0x90,0x95,0x89,0xA3,0x83,0xC2,
        0x92,0xE1,0x92,0x82,0xD9,0x03,0x08,0xA9,0x85,0x92,0xA2,0x80,0xE0,0x30,0x8B,0xB3,
        0x87,0x89,0x90,0x83,0xA0,0x08,0x92,0x93,0x3E,0xAB,0x43,0x89,0xE3,0x80,0x83,0x2F,
        0x00,0xA3,0x80,0xC9,0x22,0x3F,0x08,0x81,0x0B,0x33,0x9A,0xA3,0x7B,0x0C,0x29,0x4A,
        0x1B,0x21,0xAA,0x70,0x1B,0x0D,0x48,0x1A,0x81,0x88,0xB1,0x39,0x3F,0x08,0x58,0xA0,
        0x81,0x1A,0x1A,0x2B,0x6D,0x11,0x0A,0x91,0x01,0x1A,0x98,0x5A,0x0C,0x03,0xB1,0x84,
        0xA3,0xAD,0x58,0x2A,0xA1,0x84,0xB1,0xA0,0x5C,0x2B,0x13,0xA8,0x95,0x83,0xE8,0x10,
        0x81,0xB0,0x00,0xC2,0x96,0xA0,0x91,0x00,0x2C,0x90,0x30,0xF2,0x80,0xA8,0x39,0x21,
        0xC1,0x03,0xAC,0x39,0x7C,0x29,0x91,0x1A,0x00,0x19,0x2C,0x3A,0x93,0xB0,0x29,0x8F,
        0x28,0x02,0x93,0xF3,0xA9,0x01,0x03,0xE0,0x08,0x09,0x1D,0x58,0xA1,0x83,0xA9,0x6B,
        0x2A,0x3C,0x21,0x89,0xC2,0x2C,0x4B,0x8A,0x50,0x81,0x98,0xA8,0x32,0x0C,0x8E,0x24,
        0x0B,0x1A,0x81,0x92,0xA1,0x4F,0x18,0x3A,0x0A,0xB4,0x18,0x2E,0x39,0x82,0x19,0xD3,
        0xD0,0x28,0x1B,0x11,0x98,0x07,0xAA,0x28,0x00,0x88,0xB4,0x89,0x1B,0x1F,0x22,0x00,
        0xB3,0xC9,0x33,0xAB,0x2B,0xB5,0x48,0x98,0x98,0xA7,0x10,0xD2,0xC1,0x23,0xCA,0x93,
        0xC6,0x80,0xA1,0x88,0x02,0x89,0xE2,0x09,0x38,0xBA,0x40,0x89,0x21,0xD8,0x49,0x10,
        0x8D,0x02,0x90,0xC3,0x9A,0x24,0x89,0x08,0x84,0xA5,0x9C,0x10,0x11,0x9C,0x88,0x30,
        0x3C,0xA1,0x94,0x58,0x8C,0x0B,0x69,0x29,0x9A,0x81,0x12,0x2B,0x8B,0x79,0x94,0xB0,
        0xC1,0x84,0xC2,0x99,0x25,0x99,0x11,0xA2,0x93,0xE4,0x99,0x80,0x0A,0x00,0x10,0xB7,
        0xB0,0x31,0xBA,0x3C,0x21,0xB3,0xF1,0x18,0xA0,0x2A,0x20,0xA3,0x06,0xE8,0x28,0xA1,
        0xB4,0x08,0x0B,0x11,0x4B,0xB7,0x90,0xA5,0x98,0x3D,0x19,0x02,0xA1,0xC4,0xB2,0x19,
        0x28,0xC0,0xA5,0x92,0xB1,0xA3,0x0A,0x0A,0x08,0x2B,0x70,0xC4,0xB3,0x00,0xBC,0x4B,
        0x39,0x12,0xE3,0xA0,0x00,0x3F,0x18,0x29,0x94,0xD1,0x19,0x09,0x00,0xA1,0x83,0x99,
        0x9B,0x35,0x80,0xC4,0xB1,0x6A,0x1A,0x1C,0x29,0x38,0x0E,0x19,0x5A,0x1A,0x82,0x8A,
        0x59,0x2A,0x2E,0x20,0x88,0xA8,0x3A,0x38,0x3D,0x00,0xB3,0x29,0xAD,0x49,0x10,0x0C,
        0x01,0x01,0xA3,0x8F,0x85,0x09,0x1B,0x88,0x10,0xA3,0xD2,0x90,0x3C,0x5C,0x39,0x03,
        0xD1,0xA0,0x00,0x2A,0x0B,0x04,0xA7,0x90,0xA0,0x11,0x90,0x99,0x83,0xB4,0xB1,0xF1,
        0x84,0x88,0x90,0x18,0x18,0xD3,0xD2,0xB3,0xA0,0x1A,0x21,0xA7,0xB2,0xB3,0x92,0x9A,
        0x22,0xB9,0x28,0x38,0xBD,0x87,0x2A,0xB1,0x13,0x0D,0x0A,0x38,0xC9,0x24,0xC0,0x19,
        0x23,0x0F,0x01,0x88,0xC0,0x2A,0x82,0x18,0x28,0xF0,0x18,0x2A,0x29,0x4B,0x35,0xB8,
        0xA3,0x9D,0x18,0x1B,0x40,0x00,0x9A,0x5C,0x3A,0x09,0x2F,0x38,0x8A,0x3B,0x3B,0x11,
        0x5C,0x19,0x2B,0x4A,0x08,0x0A,0x3D,0x20,0x4F,0x3A,0x19,0x2A,0x18,0x4D,0x1B,0x3A,
        0x11,0x0D,0x3A,0x3C,0x4B,0x93,0x81,0xAA,0x6B,0x4A,0x18,0x00,0xC3,0xC3,0x9A,0x59,
        0x2A,0x1B,0xA7,0xA1,0x81,0x88,0x88,0x58,0xB2,0xB1,0x2B,0x83,0xD4,0x81,0x08,0x0F,
        0x00,0x20,0xC2,0xE2,0x80,0x08,0x1C,0x29,0x04,0xB1,0xA2,0x01,0x1C,0x91,0x00,0x0C,
        0x49,0xB0,0x43,0xF2,0x99,0x39,0x3F,0x00,0x81,0x94,0xC1,0x09,0x1A,0x69,0x90,0x80,
        0x94,0xAA,0x20,0x2A,0x91,0xB1,0x39,0x7A,0x38,0xD1,0x10,0x8A,0x8C,0x5A,0x01,0xB5,
        0x98,0x80,0x2A,0x0B,0x32,0x92,0xF1,0x81,0x9A,0x23,0x8A,0xA3,0xB7,0x09,0x03,0x08,
        0xD0,0x94,0x9A,0x09,0x01,0x93,0xB7,0xC2,0x8C,0x3A,0x83,0x99,0x05,0xA0,0x0B,0x29,
        0x93,0xE5,0x80,0x89,0x38,0x90,0x8A,0xD7,0xA1,0x19,0x1B,0x48,0x98,0x92,0xC3,0xA1,
        0x09,0x3F,0x02,0x0C,0x22,0xC3,0xB2,0xA1,0x01,0x9F,0x4A,0x01,0xA3,0xD3,0xB0,0x28,
        0x3F,0x29,0x20,0xA2,0xC2,0xB1,0x08,0x5A,0x98,0x13,0xD2,0xC1,0x01,0xB2,0x80,0x3D,
        0x03,0xC1,0x89,0x96,0x90,0x90,0x3A,0x1A,0x9A,0x32,0xB6,0xA2,0x8E,0x4A,0x28,0x8A,
        0x84,0xA2,0x8A,0x2D,0x49,0x09,0x88,0x18,0x30,0x9D,0x2C,0x23,0xB1,0x0C,0x92,0x2D,
        0x39,0x82,0xC4,0x2E,0x10,0x1A,0x10,0xB9,0x48,0x19,0x39,0xBA,0x34,0xDA,0x2D,0x48,
        0x1A,0xA6,0x98,0x83,0x9A,0x1D,0x38,0x04,0xD0,0x18,0x90,0x2C,0x11,0x93,0xD3,0x9A,
        0x11,0x08,0x82,0xF1,0x01,0xA0,0x2A,0x93,0xD3,0xB4,0xB8,0x82,0x2F,0x11,0xA3,0xB3,
        0xA8,0x3B,0x09,0x23,0x96,0xC8,0x3B,0x3F,0x93,0x82,0xA1,0x90,0x3F,0x28,0x81,0xD1,
        0x93,0x08,0x2D,0x18,0x91,0xB3,0xB5,0x98,0x2A,0x2B,0x84,0xB1,0x5B,0x8A,0x31,0x18,
        0x80,0x8B,0x7E,0x39,0x2B,0x02,0xC1,0x8B,0x6C,0x49,0x09,0x10,0xA1,0x08,0x01,0x0C,
        0x20,0xA1,0x09,0x4F,0x18,0x00,0x01,0xA0,0x5C,0x1B,0x5B,0x10,0x92,0x90,0x2B,0x5A,
        0x3D,0x18,0x91,0x19,0x98,0x2D,0x39,0x89,0x2D,0x3A,0x48,0x2C,0x11,0xB5,0x9A,0x19,
        0x5B,0x28,0x90,0x95,0x98,0x89,0x2B,0x40,0x08,0x90,0xF3,0x0A,0x08,0xA6,0x80,0x91,
        0xB2,0xA0,0x02,0xF2,0xA1,0xB7,0x89,0x81,0x82,0x91,0xB1,0x21,0xAB,0x32,0xE9,0x04,
        0xA2,0x8D,0x12,0x91,0xA3,0xA3,0xD2,0x8B,0x39,0xD1,0x84,0xE2,0x90,0x00,0x2B,0x29,
        0xA3,0xD4,0xA1,0x91,0x1D,0x5A,0x08,0x19,0x11,0x99,0x08,0x18,0x49,0x0F,0x18,0x10,
        0x82,0xF1,0x00,0x89,0x2F,0x3A,0x01,0xB3,0xC2,0x81,0x3F,0x29,0x08,0x10,0xA1,0xA1,
        0x3B,0x5D,0x19,0x28,0x0B,0x38,0x82,0x91,0x19,0xBD,0x3B,0x7A,0x80,0x12,0xB3,0xE0,
        0x0B,0x6A,0x01,0x88,0xA4,0x08,0x0B,0x08,0x59,0x80,0x80,0x1D,0x49,0x89,0x00,0x84,
        0x99,0x1A,0x2B,0x32,0xE3,0xB4,0xA9,0x3A,0x99,0x31,0xE3,0xAA,0x58,0x3B,0x88,0x95,
        0xC0,0x18,0x4A,0x09,0x30,0xF2,0xA3,0x1C,0x1B,0x49,0x00,0xD3,0xB2,0xA0,0x18,0x11,
        0x92,0xD3,0xB2,0x91,0x80,0xE7,0xA1,0x91,0x98,0x19,0x22,0xC2,0xD2,0x18,0x8D,0x3B,
        0x10,0xA5,0x91,0x98,0x02,0x3E,0x80,0x01,0x90,0xAA,0x13,0xF1,0x02,0xD1,0x08,0x19,
        0x49,0xB4,0x91,0xB4,0x99,0x2A,0x0C,0x32,0xC0,0x05,0x88,0x0B,0x80,0x2C,0x81,0x10,
        0x0B,0x51,0xA9,0x19,0x05,0xBF,0x28,0x20,0xE1,0x90,0x80,0x28,0x19,0x08,0x26,0xB1,
        0xA1,0x18,0x88,0x2A,0xF0,0x12,0x8A,0xB3,0x14,0x1B,0xD4,0xD8,0x10,0x08,0x8A,0x17,
        0xA0,0x98,0x2B,0x3A,0x29,0x48,0xA4,0x99,0x0E,0x4A,0x12,0x8B,0x31,0x8B,0x4E,0x1A,
        0x11,0xB5,0x89,0x91,0x29,0x89,0xC2,0x97,0x90,0x0A,0x19,0x11,0x91,0xC1,0xD5,0x08,
        0x89,0x20,0x91,0xB1,0x1A,0x2D,0x18,0x29,0xD2,0x3B,0x3E,0x3A,0x2A,0x90,0x82,0x1C,
        0x49,0x3B,0x93,0xB6,0xC8,0x4C,0x02,0x91,0x93,0xF2,0x88,0x2D,0x28,0x81,0x82,0xC1,
        0x89,0x2D,0x6B,0x19,0x82,0x80,0x18,0x8B,0x39,0x39,0xC8,0x3A,0x6A,0x0A,0x22,0xD2,
        0x09,0x2C,0x1A,0x68,0x92,0xE2,0x89,0x2A,0x2A,0x30,0xC2,0xA3,0xB4,0x1D,0x2A,0x09,
        0x93,0x18,0xF2,0x89,0x28,0xB3,0x01,0x8F,0x18,0x11,0xA1,0x93,0x90,0xD1,0x7A,0x20,
        0xC3,0xA2,0xA8,0x88,0x1D,0x28,0xA5,0xA2,0xA2,0x0B,0x29,0x2B,0x87,0xC1,0x80,0x0A,
        0x19,0x01,0x12,0xF1,0x10,0x80,0x0A,0x18,0x08,0x2F,0x4A,0x02,0x89,0x1B,0x29,0x5D,
        0x4C,0x08,0x82,0xA1,0x0A,0x3A,0x4B,0x29,0xC6,0xC3,0x09,0x09,0x88,0x39,0x98,0x82,
        0xA5,0x1A,0x30,0x11,0xBD,0x3F,0x12,0x8B,0x28,0xC3,0x88,0x3F,0x2B,0x3B,0x48,0xA1,
        0x80,0x8A,0x4D,0x39,0x01,0x93,0xA2,0xF1,0x19,0x19,0x0A,0x02,0xB2,0x8B,0x24,0xD2,
        0x4B,0x12,0xC8,0x2E,0x10,0xB5,0x89,0x01,0x09,0x1C,0x2A,0x03,0xD4,0x91,0x98,0x99,
        0x11,0x2B,0xE4,0x00,0x00,0x01,0xE0,0xA5,0x89,0x99,0x31,0x18,0xD0,0xB7,0x98,0x18,
        0x0A,0x10,0x94,0xC2,0x90,0x18,0x00,0x99,0x87,0xA0,0x90,0x2A,0x3C,0x02,0xB8,0xC1,
        0x79,0x1A,0x20,0x08,0xA1,0xD2,0x1C,0x29,0x03,0xD1,0x29,0x99,0x2C,0x50,0xB3,0xD1,
        0x08,0x09,0x3C,0x10,0x04,0xB2,0x0D,0x2B,0x59,0x80,0x90,0x01,0x0F,0x3A,0x18,0x01,
        0xA2,0x9B,0x5B,0x3D,0x81,0x03,0xD2,0x98,0x59,0x90,0x81,0x92,0xB4,0x8B,0x1B,0x40,
        0xB2,0xB5,0x08,0x4B,0x01,0x09,0xD1,0x91,0x8B,0x7A,0x10,0xB3,0xC3,0x99,0x49,0x1A,
        0x29,0xB5,0xA2,0xAB,0x40,0x81,0x19,0xB7,0xB0,0x20,0x2B,0xD4,0x88,0xA1,0x91,0x3C,
        0x82,0x37,0xD3,0xB1,0x8A,0x1B,0x30,0xB3,0xF4,0xA1,0x91,0x09,0x10,0x03,0xD0,0x83,
        0xA9,0x8F,0x10,0x01,0x90,0x18,0x80,0x20,0x2B,0xF1,0x28,0x99,0x2A,0x41,0xF0,0x12,
        0xAA,0x83,0x82,0xD1,0xC1,0x08,0x89,0x59,0x09,0x83,0x87,0xB0,0x2A,0x4D,0x18,0x09,
        0x19,0xB3,0x4B,0x3F,0x39,0x19,0x09,0x01,0x89,0x03,0x1F,0x00,0x1A,0x0B,0x10,0x68,
        0xA0,0x18,0x8C,0x6A,0x09,0x08,0x97,0xA1,0x81,0x1B,0x2B,0x4C,0x03,0xB4,0xA8,0x92,
        0x4B,0x3C,0xA1,0x81,0x95,0xA8,0x81,0x12,0xBB,0x92,0x45,0xB9,0x93,0xF4,0x88,0x0A,
        0x2D,0x28,0x00,0xA3,0xA3,0x8A,0x3F,0x48,0xB1,0x92,0xB4,0xA8,0x30,0x80,0xD3,0x80,
        0xD1,0x19,0x3B,0xC4,0x81,0xC1,0x29,0x0D,0x20,0x13,0xC8,0xB4,0x4C,0x09,0x00,0x82,
        0xC2,0x3B,0x0D,0x30,0x0B,0x12,0xF0,0x1B,0x20,0x0A,0xA6,0x80,0x0A,0x4A,0x4A,0x80,
        0x94,0xB1,0x2E,0x3B,0x1A,0x10,0x93,0x10,0x4C,0x3D,0x08,0x82,0xC9,0x19,0x6A,0x2B,
        0x38,0xD1,0x08,0x19,0x2A,0x5A,0x82,0xB1,0x8D,0x29,0x78,0x09,0x82,0x0A,0x2C,0x1B,
        0x19,0x41,0xB8,0x8C,0x79,0x2B,0x11,0x88,0x82,0x91,0xDC,0x28,0x11,0xB0,0x11,0x18,
        0xC9,0x62,0xA1,0x91,0x98,0x3B,0x3A,0xB0,0xF4,0x01,0xC0,0x29,0x39,0xF8,0x95,0x91,
        0x88,0x88,0x91,0x03,0xA1,0xE2,0x18,0x82,0xD1,0xA2,0xD1,0x80,0x19,0x20,0x83,0xB1,
        0xE3,0x80,0x91,0x4D,0x1A,0x03,0xB2,0x09,0x18,0xD1,0x19,0x09,0x92,0xA6,0xA0,0xB6,
        0xB2,0x8B,0x38,0x10,0x42,0xD3,0xD0,0xA8,0x20,0x2C,0x10,0x01,0xB1,0xB4,0xAB,0x5B,
        0x79,0x80,0x10,0x1A,0xA8,0x3D,0x18,0x20,0xB3,0x8F,0x18,0x01,0x00,0x09,0xF3,0x89,
        0x69,0x88,0x81,0x91,0x08,0xE1,0x1A,0x08,0x11,0x81,0x1E,0x29,0xA0,0x01,0x00,0x90,
        0x3E,0x7B,0x18,0x82,0xC3,0xA1,0x2A,0x2C,0x5B,0x81,0xA5,0x90,0x81,0x00,0x0B,0x1A,
        0x1C,0x2C,0x32,0xC0,0xF3,0x80,0x2D,0x2A,0x10,0x02,0xE4,0xC1,0x89,0x4A,0x09,0x01,
        0x03,0xD2,0x98,0x2A,0x39,0x8A,0x89,0x26,0xB1,0xB2,0x12,0xC0,0x0A,0x5A,0x18,0x98,
        0xF3,0x92,0x99,0x99,0x79,0x01,0xB5,0xA1,0x80,0x80,0x90,0x83,0xA0,0xE2,0x81,0x29,
        0x93,0x8A,0x0A,0x6A,0x1F,0x18,0x02,0xC8,0x01,0x19,0x3B,0x4A,0x98,0x17,0xA8,0x0D,
        0x38,0xA1,0x91,0x10,0xA2,0x2B,0x4C,0xA6,0x81,0xBA,0x21,0x4C,0x80,0x21,0xD1,0x92,
        0x2C,0x08,0x30,0x9F,0x93,0x2A,0x89,0x03,0x8B,0x87,0x0A,0x0D,0x12,0x98,0xA4,0x93,
        0xBB,0x59,0x18,0xA1,0x32,0xE9,0x84,0x08,0x8A,0x02,0xA1,0x91,0x4B,0xB4,0x20,0x88,
        0xF0,0x3A,0x1A,0x88,0x87,0xB1,0x92,0x0A,0x08,0x6B,0x83,0xC3,0x91,0xC0,0x2B,0x79,
        0x08,0x8A,0x84,0xA0,0x89,0x40,0x1B,0xA1,0x39,0x98,0x17,0xC2,0xA2,0x12,0xCD,0x20,
        0x89,0x92,0x25,0xB0,0x2D,0x3A,0x8B,0x58,0x2A,0xA0,0x4C,0x08,0x30,0xAE,0x82,0x59,
        0x89,0x1A,0x10,0xC2,0x18,0x2C,0x40,0x1E,0x01,0xA3,0x8A,0x81,0x2C,0x29,0x29,0xA9,
        0x13,0x51,0xAD,0x12,0x89,0x8F,0x18,0x2C,0x39,0x00,0xC1,0x10,0x3C,0x2A,0x41,0xC8,
        0xA2,0x91,0x0A,0x6C,0x10,0x12,0x88,0xE8,0x30,0x91,0x81,0xD8,0x01,0x1B,0x0D,0x07,
        0x00,0xA8,0x92,0x0A,0x28,0xD2,0xC3,0x02,0xAA,0x94,0x81,0xB4,0xB3,0x1A,0x0B,0x13,
        0xF9,0x16,0xA1,0x8A,0x59,0x19,0x02,0xC1,0x91,0x8B,0x3D,0x18,0x3B,0xA4,0x94,0x80,
        0x99,0x88,0x1C,0x79,0x0A,0x02,0x03,0xF8,0x90,0x39,0x5B,0x19,0x02,0xC3,0x90,0xBB,
        0x58,0x6A,0x09,0x02,0x89,0x91,0x88,0x1A,0x69,0x8A,0x19,0x15,0xA0,0xA2,0x00,0x9A,
        0x6B,0x49,0x88,0xA3,0x92,0xBB,0x6B,0x3D,0x38,0x01,0x98,0x91,0x3F,0x09,0x18,0x20,
        0x90,0x80,0xAC,0x70,0x91,0x9B,0x51,0x09,0x88,0x99,0x14,0x8B,0x98,0x83,0x79,0xA0,
        0x99,0x13,0x01,0x19,0xE0,0x83,0x0B,0xB0,0x0C,0x31,0x95,0xB5,0xC2,0x8A,0x39,0x20,
        0x80,0x39,0xF3,0xB1,0x10,0x88,0x5E,0x18,0x94,0xA1,0x88,0xA1,0x98,0x15,0xAA,0x39,
        0xD4,0x84,0xC0,0xA2,0xA2,0x0C,0x81,0x86,0xB5,0xA1,0xB1,0x14,0x1B,0xB1,0x02,0x92,
        0xC3,0xE0,0x88,0x11,0xAA,0x69,0x18,0x81,0xA3,0xB0,0x01,0xBF,0x2A,0x31,0x93,0xF1,
        0x00,0x89,0x18,0x19,0x11,0xD3,0xE0,0x10,0x18,0xB1,0x18,0x24,0x9A,0x2B,0xA4,0xC0,
        0xB0,0x31,0x6C,0x19,0xB4,0x12,0xA8,0xEA,0x58,0x10,0x8B,0x93,0x82,0x88,0x9A,0x41,
        0x10,0xC3,0xEA,0x41,0xA9,0x9C,0x34,0xA1,0x2A,0x79,0xA2,0x01,0xA8,0xB3,0x28,0xCC,
        0x41,0x9A,0xB3,0x4B,0xB3,0x27,0x8B,0x83,0x2B,0x2F,0x08,0x28,0xB2,0x80,0x2C,0x30,
        0x5E,0x09,0x12,0x9B,0x09,0x22,0x5B,0x19,0x8A,0x11,0x59,0x99,0xA4,0x32,0xCD,0x18,
        0x08,0x10,0x85,0xB3,0xB4,0x1E,0x88,0x28,0x8A,0x11,0x09,0xC0,0x79,0x80,0x91,0x3B,
        0x80,0x10,0x0F,0x01,0x80,0x91,0x19,0x3D,0x92,0x28,0xA8,0x37,0x9A,0x0A,0x3A,0x8A,
        0x45,0xA9,0xA4,0x00,0xAA,0x09,0x3D,0x59,0x20,0xE1,0x08,0x98,0x90,0x59,0x10,0x09,
        0xA3,0xC3,0x93,0x99,0x2B,0x69,0x11,0xD1,0xB1,0xA4,0x91,0x3C,0x89,0x83,0xF0,0x10,
        0x91,0xA1,0x89,0x59,0x05,0x99,0x93,0x94,0xC8,0x08,0x0A,0x09,0x17,0xB1,0x83,0xC1,
        0x91,0x40,0xA2,0xC2,0x98,0xC3,0xBA,0x28,0x23,0x0F,0x80,0x50,0xB8,0x19,0x10,0x96,
        0x98,0x8C,0x05,0x98,0x19,0x29,0x2B,0x3B,0x0A,0xE2,0x01,0x0F,0x3C,0x38,0x08,0x09,
        0x81,0x4A,0x6C,0x08,0x00,0x88,0x98,0x38,0x2C,0x5A,0x1B,0x20,0x1A,0x39,0xB0,0x09,
        0xCB,0x5B,0x49,0x09,0x71,0x00,0xC1,0x0E,0x08,0x38,0x0C,0x02,0x10,0x0E,0x10,0x8A,
        0x48,0x19,0x90,0x92,0x0D,0xA3,0x98,0x3B,0x79,0x19,0x01,0x10,0xE1,0x80,0x19,0x2B,
        0x10,0xF2,0x02,0xAB,0x84,0x9A,0x29,0xB4,0x80,0x92,0x03,0x88,0x95,0xD0,0x03,0x90,
        0xA0,0xC7,0xA1,0xB0,0xA2,0x02,0x18,0xB5,0xD4,0x01,0xC0,0x08,0xA2,0x93,0xA8,0xA0,
        0xC3,0x20,0xF3,0x90,0x00,0xD5,0x08,0x89,0xA5,0x80,0xA0,0x81,0x82,0xC2,0x09,0xD1,
        0x13,0xCB,0x03,0x84,0x91,0xE1,0x1B,0x12,0x08,0xAB,0x87,0x18,0xAB,0x58,0x89,0x28,
        0x81,0xC9,0x33,0xA9,0x80,0x2E,0x20,0x83,0xB9,0x20,0x3B,0x9E,0x7A,0x08,0x81,0x18,
        0x0B,0x88,0x79,0x80,0x8B,0x00,0x12,0x0E,0x89,0x51,0x1B,0x81,0xA0,0x3A,0x01,0xAF,
        0x11,0x28,0xBA,0x35,0x98,0x88,0x52,0xC0,0x83,0x2F,0xA9,0x11,0x0A,0x19,0x25,0xD0,
        0x30,0x9C,0x08,0x21,0x98,0x81,0x2A,0xF3,0x2A,0x80,0xB6,0x2B,0x08,0x93,0xE9,0x02,
        0x81,0x8C,0x21,0x00,0xA6,0xA9,0x94,0x01,0x8F,0x80,0x94,0x98,0x93,0xB4,0x00,0x08,
        0xC0,0x14,0x98,0xB3,0xB4,0xC1,0x09,0x18,0xA7,0x00,0xA3,0xC8,0x0A,0x3C,0x19,0x96,
        0x83,0xC1,0x99,0x19,0x4A,0x85,0x80,0xC1,0x91,0x99,0x90,0x2A,0x17,0x95,0x99,0x88,
        0x12,0xAE,0x39,0x08,0x92,0x84,0xB0,0xA8,0x79,0x09,0x19,0x01,0xB2,0xA3,0x8F,0x28,
        0x2B,0xA2,0x40,0x82,0xA0,0x4C,0xA9,0x39,0x8D,0x81,0x70,0x88,0xA0,0x1A,0x49,0x2D,
        0x1A,0x26,0xA8,0x98,0x08,0x29,0x0B,0x12,0x96,0xB1,0xB2,0x3A,0x13,0x9B,0x60,0xA0,
        0x88,0xB2,0x34,0xEA,0x1A,0x2A,0x79,0x98,0x10,0x04,0x8C,0x1C,0x81,0x04,0x8C,0x83,
        0x19,0x2F,0x81,0x93,0x98,0x10,0x08,0x30,0x2A,0xFA,0x05,0x08,0x2A,0x89,0x91,0xA3,
        0xFA,0x11,0x11,0x00,0x8C,0x04,0x8A,0x2A,0xB5,0x10,0xA9,0xC2,0x3D,0x1B,0x32,0x04,
        0x0A,0x1A,0x09,0x40,0x1F,0x92,0x1D,0x2A,0x91,0x10,0x30,0x2F,0x0B,0x68,0x99,0xA2,
        0x92,0x88,0x78,0xA9,0x20,0x28,0xE2,0x92,0x1A,0x99,0x4B,0x19,0x22,0xA1,0xE2,0x21,
        0x2F,0x98,0x29,0x18,0x91,0x08,0xB0,0x79,0x1A,0x82,0x3B,0xB1,0xA7,0x8A,0xB3,0x98,
        0x5B,0x23,0xCA,0x42,0x83,0xF0,0x90,0x18,0x98,0x08,0xB4,0x20,0xA3,0xC0,0x43,0xD8,
        0x80,0x81,0xA3,0x99,0xD9,0xA7,0x19,0x90,0x10,0x05,0xB1,0x8B,0x02,0xA4,0xBD,0x23,
        0x93,0x8A,0x99,0x4B,0x03,0xC1,0xF8,0x38,0x09,0x2B,0x14,0xD0,0x03,0x8A,0x2A,0x39,
        0xB9,0x97,0x90,0xAA,0x50,0x01,0x99,0x51,0xD1,0x09,0x1A,0xB5,0x00,0x8B,0x93,0x08,
        0x98,0x11,0xF9,0x85,0x2B,0x08,0x96,0x89,0x90,0x2A,0x12,0x4A,0xD8,0x85,0x2B,0x0E,
        0x10,0x00,0x01,0xB1,0x9B,0x69,0x1A,0x90,0x40,0xB8,0x01,0x08,0x0A,0x2C,0x09,0x14,
        0x4B,0xE2,0x82,0x88,0xB1,0x78,0x0A,0x01,0xC2,0x93,0x19,0xCE,0x20,0x3C,0x82,0xB4,
        0x1B,0x20,0x8C,0x3B,0x29,0xAB,0x86,0x23,0xD8,0x81,0x9A,0x5A,0x49,0xB0,0x16,0xA0,
        0xB0,0x28,0x1B,0x13,0x93,0xE4,0xA2,0xA9,0x08,0x5A,0xB3,0x12,0xC1,0xE1,0x10,0x88,
        0x01,0x0C,0x92,0x08,0x89,0xB7,0x88,0x81,0x10,0x9A,0x17,0xA0,0xB0,0x13,0x99,0xE0,
        0x39,0x31,0xD2,0xB2,0x80,0x0B,0x2D,0x49,0x80,0x01,0xB0,0x06,0x09,0x0C,0x3A,0x69,
        0xA0,0x08,0xB2,0xA1,0x69,0x2B,0x5A,0x81,0x92,0xBA,0x21,0xB1,0x7D,0x10,0x80,0x08,
        0x88,0x82,0x32,0x0D,0xB0,0x1A,0x1C,0x21,0x94,0xA9,0x58,0xB9,0x5A,0x4A,0xA0,0x13,
        0xA9,0x80,0x7C,0x00,0x20,0x8A,0x04,0x0C,0x00,0x82,0x2A,0xB2,0xAC,0x4B,0x69,0xA0,
        0xA6,0x81,0x9B,0x19,0x38,0x8B,0x17,0xB2,0x81,0x2A,0xBB,0x94,0x29,0xA2,0x15,0xBA,
        0x97,0xA3,0xB9,0x79,0x01,0xB2,0x02,0xF1,0x90,0x0A,0x29,0x11,0x88,0xE5,0xA0,0x81,
        0x19,0x91,0x90,0x28,0xB3,0x14,0xD0,0xB5,0x91,0x9A,0x29,0x0B,0x07,0xA2,0xB3,0x01,
        0x9D,0x28,0x41,0xD0,0x91,0x90,0x82,0x1A,0xA8,0x44,0x9A,0xA9,0x21,0xE3,0xA9,0x4B,
        0x19,0x78,0x89,0x83,0xA3,0xB9,0x5A,0x3D,0x80,0x82,0xA2,0xA0,0x6C,0x10,0x20,0x8B,
        0x93,0x8B,0x0E,0x33,0xA9,0xB1,0x68,0x8A,0x31,0xAC,0x94,0xB4,0x8B,0x32,0x0B,0xB4,
        0x81,0x91,0x1D,0x33,0xD9,0x31,0xE1,0x8B,0x3B,0x30,0x12,0x49,0xD2,0x8E,0x29,0x18,
        0x8A,0x92,0x02,0xAA,0x59,0x1C,0x32,0x88,0x01,0x23,0xFB,0x83,0x29,0xDA,0x59,0x01,
        0x81,0x92,0xE1,0x18,0x8A,0x1D,0x30,0x93,0xF1,0x00,0x01,0x0B,0x39,0x92,0x89,0xA0,
        0x11,0x5B,0xE0,0x82,0x09,0x13,0xAA,0xB4,0x16,0xD8,0x91,0x2A,0x29,0x84,0x1B,0xC5,
        0x98,0x98,0x31,0x98,0x99,0x17,0xA9,0x20,0x92,0xC3,0x18,0x9D,0x20,0x3D,0x89,0x94,
        0xA2,0x1C,0x5C,0x29,0x39,0xA0,0xB3,0x00,0x0C,0x4C,0x48,0x92,0x0A,0x91,0x85,0x9A,
        0x01,0x82,0x1F,0x10,0x99,0x15,0xC1,0xA0,0x39,0x1A,0x1D,0x85,0xB4,0x90,0x1A,0x2A,
        0x4B,0x01,0xB2,0x93,0xBE,0x12,0x83,0xC9,0x18,0x09,0x20,0x78,0xF1,0x08,0x19,0x88,
        0x3A,0x83,0xB3,0xA9,0x93,0x7A,0x0A,0x96,0x98,0x00,0xA8,0x3A,0x30,0x92,0xF2,0x9B,
        0x3D,0x38,0x92,0x92,0xC3,0xB8,0x6B,0x29,0x01,0x01,0xB2,0x2F,0x09,0x19,0x18,0x01,
        0x3B,0x7B,0x10,0xA1,0x90,0x39,0x0F,0x38,0x0A,0xB5,0xA4,0x89,0x8B,0x6A,0x2B,0x12,
        0xC8,0x90,0x40,0x2A,0x9E,0x22,0x88,0x18,0x09,0x3A,0xC3,0xE8,0x09,0x59,0x08,0x12,
        0x94,0xD0,0x1A,0x2C,0x38,0x00,0xA1,0x83,0xE8,0x08,0x3A,0x08,0x10,0x9E,0x83,0x1D,
        0x92,0x19,0x2C,0x39,0x3B,0x59,0x04,0xE1,0x80,0x08,0x8D,0x21,0x81,0xB2,0xB2,0x02,
        0x99,0x91,0xA4,0xD6,0x98,0x99,0x03,0x80,0x98,0xA7,0x91,0x09,0xA1,0xB2,0xB3,0xE1,
        0x12,0x92,0xB1,0x81,0x06,0x99,0x0A,0x23,0xC4,0xB1,0xF2,0x89,0x19,0x3A,0x94,0x82,
        0xE0,0x89,0x38,0x0B,0xA4,0xA5,0x80,0x80,0x8C,0x34,0xB9,0xA9,0x23,0x13,0xB9,0xC1,
        0xC7,0x1B,0x89,0x10,0x20,0x11,0xE3,0xA8,0x4B,0x0B,0x40,0x91,0x90,0x1B,0x5F,0x2A,
        0x18,0x82,0x91,0x0B,0x4A,0x28,0xCA,0x40,0x80,0x5B,0x2C,0x13,0xB0,0x8A,0xA9,0x5A,
        0x58,0x89,0x82,0x88,0x2E,0x3B,0x31,0xA1,0x9B,0x01,0x7A,0x2C,0x01,0x91,0x93,0x3F,
        0x88,0x39,0x10,0xF1,0x91,0x8B,0x48,0x0A,0x12,0xE3,0xA8,0x18,0x28,0x92,0x97,0x98,
        0x99,0x19,0xA1,0x11,0xB6,0x88,0x3B,0x10,0xD3,0xC3,0xA1,0x2A,0x8A,0x49,0x04,0xF1,
        0x91,0x02,0x8A,0x89,0x04,0xF1,0x98,0x80,0x18,0x12,0xE3,0x81,0x98,0x80,0x01,0xB3,
        0xF2,0x99,0x12,0x2A,0xB5,0xB3,0x92,0xAA,0x19,0x50,0xB2,0xC3,0x92,0xD0,0x2B,0x68,
        0x93,0x99,0xC0,0x2C,0x3E,0x80,0x20,0x08,0x93,0x0D,0x2A,0x31,0x8D,0x02,0x2B,0x91,
        0x08,0x0A,0x03,0x2C,0x3C,0x52,0xB9,0xA0,0x12,0xBF,0x3A,0x29,0x01,0x88,0xC0,0x6A,
        0x3C,0x0A,0x49,0x18,0x0B,0x39,0x2B,0x69,0x0A,0x84,0x2A,0x2A,0x1C,0x2A,0xC3,0x8C,
        0x19,0x50,0x09,0x91,0xA7,0x8D,0x18,0x1A,0x28,0x00,0xA0,0x94,0x10,0x1F,0x20,0x90,
        0x8A,0x12,0xD0,0x1A,0x5A,0x81,0x04,0xBC,0x23,0x10,0xE0,0x90,0x90,0x18,0x1A,0xA6,
        0x12,0xB1,0xD0,0x4A,0x08,0x82,0x92,0xB6,0x9A,0x0A,0x12,0x88,0xC3,0xC5,0x8A,0x89,
        0x20,0xB5,0x93,0x0B,0x18,0x00,0x09,0xF2,0x88,0x2A,0x4A,0x08,0x05,0xB2,0xA9,0x3B,
        0x5D,0x28,0xA4,0xB1,0x00,0x19,0x19,0x7A,0xA3,0xB3,0x0A,0x90,0xA1,0xC4,0x80,0xBA,
        0x50,0x13,0xC1,0xC2,0x9A,0x2A,0x7B,0x28,0x84,0xC1,0x09,0x3B,0x4E,0x20,0x91,0xA1,
        0x18,0xAB,0x79,0x10,0xB4,0x08,0x9A,0x11,0x2B,0xF0,0x93,0xAA,0x01,0x6A,0x01,0x93,
        0x80,0xB8,0x2A,0x5B,0x10,0x80,0x89,0x4A,0x5B,0x92,0x15,0xB2,0xA0,0x2F,0x19,0x93,
        0xB8,0x95,0x80,0x1C,0x21,0xA9,0x02,0x0B,0xA0,0x5A,0x18,0x98,0x39,0x1B,0x68,0x00,
        0x91,0x91,0x9C,0x39,0x3E,0x18,0x84,0xB3,0x9B,0x7A,0x08,0x18,0x0A,0xB5,0x91,0x0B,
        0x28,0x39,0x19,0x90,0x0A,0x50,0xAC,0x11,0x01,0xAB,0x88,0x52,0x1B,0x83,0xC4,0xA2,
        0x9A,0xAB,0x03,0x90,0x19,0x93,0x81,0x08,0x92,0x9A,0x68,0x98,0x19,0x39,0xC1,0x92,
        0x8A,0x38,0x4E,0x02,0xB1,0x90,0xC3,0x18,0x2B,0x04,0xC3,0xD2,0x91,0x90,0x81,0x89,
        0x13,0xF1,0x88,0x93,0xA2,0x00,0x91,0xC0,0x5B,0x21,0x99,0x93,0x06,0x9A,0x1B,0x48,
        0x99,0xB7,0x90,0x89,0x18,0x1B,0x11,0xA4,0xB2,0x81,0x9A,0x08,0x97,0x98,0x91,0x10,
        0xB8,0x06,0xA2,0xA0,0x29,0x2B,0x21,0xC2,0xD1,0x10,0x1A,0x4A,0x29,0xF1,0x98,0x29,
        0x1B,0x31,0x10,0xA0,0xA1,0x1D,0x5A,0x29,0xB2,0x82,0xA8,0x0F,0x28,0x21,0x09,0x91,
        0x82,0x4D,0x10,0xA3,0xB0,0x89,0x4C,0x39,0xA0,0xA4,0xA1,0x89,0x1E,0x28,0x29,0xA3,
        0xC3,0x2D,0x19,0x01,0x49,0x01,0x9B,0x0C,0x21,0xC2,0xA2,0x93,0x7C,0x2A,0x10,0x90,

        /* Source: 08HH.ROM */
        /* Length: 384 / 0x00000180 */

        0x75,0xF2,0xAB,0x7D,0x7E,0x5C,0x3B,0x4B,0x3C,0x4D,0x4A,0x02,0xB3,0xC5,0xE7,0xE3,
        0x92,0xB3,0xC4,0xB3,0xC3,0x8A,0x3B,0x5D,0x5C,0x3A,0x84,0xC2,0x91,0xA4,0xE7,0xF7,
        0xF7,0xF4,0xA1,0x1B,0x49,0xA5,0xB1,0x1E,0x7F,0x5A,0x00,0x89,0x39,0xB7,0xA8,0x3D,
        0x4A,0x84,0xE7,0xF7,0xE2,0x2D,0x4C,0x3A,0x4E,0x7D,0x04,0xB0,0x2D,0x4B,0x10,0x80,
        0xA3,0x99,0x10,0x0E,0x59,0x93,0xC4,0xB1,0x81,0xC4,0xA2,0xB2,0x88,0x08,0x3F,0x3B,
        0x28,0xA6,0xC3,0xA2,0xA2,0xC5,0xC1,0x3F,0x7E,0x39,0x81,0x93,0xC2,0xA3,0xE5,0xD2,
        0x80,0x93,0xB8,0x6D,0x49,0x82,0xD4,0xA1,0x90,0x01,0xA0,0x09,0x04,0xE3,0xB2,0x91,
        0xB7,0xB3,0xA8,0x2A,0x03,0xF3,0xA1,0x92,0xC5,0xC3,0xB2,0x0B,0x30,0xB3,0x8E,0x6D,
        0x4A,0x01,0xB4,0xB4,0xC4,0xC3,0x99,0x3B,0x12,0xE3,0xA1,0x88,0x82,0xB4,0x9A,0x5C,
        0x3A,0x18,0x93,0xC3,0xB3,0xB4,0xA8,0x19,0x04,0xF3,0xA8,0x3B,0x10,0xA2,0x88,0xA5,
        0xB2,0x0B,0x6D,0x4B,0x10,0x91,0x89,0x3C,0x18,0x18,0xA6,0xC4,0xC3,0x98,0x19,0x2B,
        0x20,0x91,0xA0,0x4E,0x28,0x93,0xB3,0xC2,0x92,0xA9,0x5A,0x96,0xC4,0xC2,0x09,0x01,
        0xC4,0xA1,0x92,0xC4,0xA1,0x89,0x10,0xA3,0xA1,0x90,0x1C,0x5A,0x01,0xC5,0xA1,0x92,
        0xD4,0xB3,0xC4,0xC4,0xC3,0xA1,0x88,0x1A,0x28,0x89,0x3C,0x3A,0x3D,0x29,0x00,0x93,
        0xB0,0x3D,0x28,0x80,0x91,0x82,0xE3,0x99,0x2A,0x11,0xD6,0xC3,0x99,0x29,0x82,0xC4,
        0xC3,0xA1,0x0A,0x3B,0x3D,0x3A,0x02,0xC3,0xA2,0x99,0x3B,0x2C,0x7C,0x28,0x81,0xA3,
        0xB2,0xA3,0xB1,0x08,0x1A,0x3C,0x18,0x2E,0x4C,0x39,0xA5,0xB3,0xB4,0xC2,0x88,0x08,
        0x19,0x0A,0x49,0xB7,0xB3,0xA2,0xA1,0x92,0xA1,0x93,0xB1,0x0C,0x7D,0x39,0x93,0xB3,
        0xB1,0x1A,0x19,0x5D,0x28,0xA6,0xC4,0xB2,0x90,0x09,0x2A,0x18,0x1B,0x5B,0x28,0x88,
        0x2C,0x29,0x82,0xA0,0x18,0x91,0x2D,0x29,0x2B,0x5C,0x4C,0x3B,0x4C,0x28,0x80,0x92,
        0x90,0x09,0x2B,0x28,0x1D,0x6B,0x11,0xC5,0xB2,0x0B,0x39,0x09,0x4D,0x28,0x88,0x00,
        0x1B,0x28,0x94,0xE3,0xA0,0x1A,0x28,0xB5,0xB4,0xB3,0xB2,0x93,0xE2,0x91,0x92,0xD4,
        0xA0,0x1B,0x4A,0x01,0xA1,0x88,0x2D,0x5C,0x3B,0x28,0x08,0x93,0xD4,0xB2,0x91,0xB4,
        0xA0,0x3E,0x3B,0x4B,0x3B,0x29,0x08,0x93,0x9B,0x7B,0x3A,0x19,0x00,0x80,0x80,0xA0,

        /* Source: 10TOM.ROM */
        /* Length: 640 / 0x00000280 */

        0x77,0x27,0x87,0x01,0x2D,0x4F,0xC3,0xC1,0x92,0x91,0x89,0x59,0x83,0x1A,0x32,0xC2,
        0x95,0xB1,0x81,0x88,0x81,0x4A,0x3D,0x11,0x9E,0x0B,0x88,0x0C,0x18,0x3B,0x11,0x11,
        0x91,0x00,0xA0,0xE2,0x0A,0x48,0x13,0x24,0x81,0x48,0x1B,0x39,0x1C,0x83,0x84,0xA1,
        0xD1,0x8E,0x8A,0x0B,0xC0,0x98,0x92,0xB8,0x39,0x90,0x10,0x92,0xF0,0xB5,0x88,0x32,
        0x49,0x51,0x21,0x03,0x82,0x10,0x8A,0x7A,0x09,0x00,0xA2,0xCA,0x1B,0xCC,0x1C,0xB9,
        0x8E,0x89,0x89,0xA1,0x89,0x92,0x29,0x11,0x60,0x40,0x14,0x22,0x32,0x78,0x40,0x01,
        0x02,0x90,0x81,0xAB,0x0B,0x00,0xAF,0x99,0xCC,0xAB,0xDA,0xA9,0x99,0x1B,0x30,0x14,
        0x92,0x22,0x19,0x68,0x32,0x14,0x26,0x13,0x23,0x23,0x20,0x12,0x9A,0xA8,0xB9,0xFA,
        0xAA,0xCA,0xCC,0x0C,0xA8,0xAE,0x88,0xB9,0x88,0xA0,0x02,0x21,0x50,0x43,0x03,0x81,
        0x2A,0x11,0x34,0x63,0x24,0x33,0x22,0x38,0x8B,0xEA,0xAE,0x99,0xA0,0x90,0x82,0x00,
        0x89,0xBF,0x8A,0xE8,0xA9,0x90,0x01,0x12,0x13,0x12,0x08,0xA9,0xAA,0xC9,0x22,0x63,
        0x63,0x12,0x44,0x00,0x10,0x88,0x9C,0x98,0xA1,0x85,0x03,0x32,0x36,0x80,0x89,0xDB,
        0xDB,0xBB,0xB9,0xBA,0x01,0x81,0x28,0x19,0xCB,0xFA,0xBC,0x09,0x13,0x37,0x34,0x34,
        0x23,0x31,0x20,0x10,0x00,0x00,0x28,0x38,0x10,0x88,0xEC,0x8D,0xCB,0xBC,0xCC,0xBB,
        0xBB,0xC9,0x99,0x00,0x00,0x33,0x11,0x22,0x81,0x07,0x41,0x54,0x34,0x34,0x22,0x31,
        0x00,0x88,0x9A,0x9B,0x98,0xAB,0x8E,0x9B,0xBD,0x9C,0xBC,0xBB,0xDA,0xAA,0xA9,0x99,
        0x18,0x38,0x60,0x20,0x31,0x13,0x13,0x51,0x14,0x31,0x53,0x33,0x35,0x22,0x01,0x8A,
        0x9C,0xA9,0xCA,0xC9,0xA8,0x00,0x10,0x81,0x9C,0x9E,0xAB,0xCC,0xAB,0xBA,0x98,0x30,
        0x52,0x03,0x81,0x08,0x9C,0xAC,0xAC,0x18,0x11,0x03,0x51,0x61,0x41,0x31,0x31,0x02,
        0x01,0x20,0x24,0x43,0x44,0x40,0x30,0x10,0xBC,0xBE,0xCB,0xDB,0xAB,0xBA,0x99,0x98,
        0x99,0xAA,0xBD,0xAA,0xC8,0x90,0x11,0x53,0x37,0x23,0x43,0x34,0x33,0x33,0x33,0x11,
        0x28,0x00,0x19,0xA9,0x9A,0xCB,0xCE,0xBB,0xEB,0xBC,0xBB,0xCA,0xBA,0xA8,0x88,0x11,
        0x12,0x21,0x20,0x22,0x26,0x26,0x23,0x23,0x43,0x24,0x22,0x32,0x20,0x31,0x81,0x9A,
        0xBC,0xBC,0xCB,0xBD,0x9A,0xA9,0x90,0x98,0xBA,0xCC,0xCB,0xBC,0x8B,0x88,0x22,0x35,
        0x23,0x12,0x99,0x8B,0xAA,0xAA,0x89,0x82,0x93,0x31,0x42,0x23,0x23,0x21,0x32,0x11,
        0x20,0x13,0x13,0x24,0x24,0x24,0x22,0x11,0x8A,0x9E,0xAC,0xAC,0xAA,0xBA,0xAA,0xAB,
        0xBD,0xBC,0xCB,0xCB,0xA9,0xA8,0x91,0x12,0x44,0x43,0x44,0x34,0x34,0x42,0x33,0x42,
        0x21,0x11,0x11,0x88,0x80,0xAA,0x0B,0xAC,0xCB,0xEC,0xAC,0xBA,0xCA,0xAB,0x9A,0x99,
        0x80,0x91,0x09,0x08,0x10,0x22,0x44,0x43,0x44,0x33,0x43,0x22,0x13,0x21,0x22,0x20,
        0x09,0x88,0xB9,0xC8,0xBB,0xAB,0xAB,0xA9,0xA9,0x9B,0x9B,0x99,0x90,0x90,0x00,0x81,
        0x00,0x08,0x09,0x8A,0x9A,0xAA,0xA9,0xA9,0x99,0x90,0x80,0x01,0x80,0x00,0x09,0x31,
        0x32,0x44,0x33,0x43,0x34,0x33,0x24,0x22,0x23,0x12,0x10,0x09,0x9B,0xAB,0xCA,0xCC,
        0xBB,0xCB,0xDA,0xCA,0xAB,0xCA,0xAB,0xA9,0xA8,0x92,0x12,0x43,0x53,0x35,0x23,0x33,
        0x43,0x43,0x52,0x22,0x22,0x21,0x01,0x09,0x89,0xA9,0xBB,0xBD,0xBC,0xCB,0xDA,0xAB,
        0xAB,0xAB,0xAA,0xA9,0x99,0xA8,0x09,0x01,0x11,0x34,0x25,0x23,0x33,0x51,0x22,0x31,
        0x12,0x20,0x21,0x12,0x10,0x80,0x99,0x9A,0x99,0x99,0x88,0x08,0x00,0x88,0xA9,0x99,
        0x99,0x80,0x80,0x10,0x01,0x00,0x9A,0xAA,0xBB,0xBA,0xBA,0xA9,0x99,0x99,0x89,0x99,
        0x99,0x00,0x01,0x33,0x35,0x24,0x23,0x34,0x23,0x33,0x34,0x33,0x43,0x32,0x21,0x88,
        0xAB,0xBD,0xBB,0xDB,0xAB,0xBA,0xBB,0xDA,0xBB,0xCB,0xBB,0xBC,0xA8,0x90,0x01,0x12,
        0x23,0x43,0x53,0x34,0x34,0x39,0x80,0x08,0x08,0x08,0x08,0x08,0x08,0x08,0x08,0x00,

        /* Source: 20RIM.ROM */
        /* Length: 128 / 0x00000080 */

        0x0F,0xFF,0x73,0x8E,0x71,0xCD,0x00,0x49,0x10,0x90,0x21,0x49,0xA0,0xDB,0x02,0x3A,
        0xE3,0x0A,0x50,0x98,0xC0,0x59,0xA2,0x99,0x09,0x22,0xA2,0x80,0x10,0xA8,0x5B,0xD2,
        0x88,0x21,0x09,0x96,0xA8,0x10,0x0A,0xE0,0x08,0x48,0x19,0xAB,0x52,0xA8,0x92,0x0C,
        0x03,0x19,0xE2,0x0A,0x12,0xC2,0x81,0x1E,0x01,0xD0,0x48,0x88,0x98,0x01,0x49,0x91,
        0xAA,0x2C,0x25,0x89,0x88,0xB5,0x81,0xA2,0x9A,0x12,0x9E,0x38,0x3B,0x81,0x9B,0x59,
        0x01,0x93,0xCA,0x4A,0x21,0xA0,0x3D,0x0A,0x39,0x3D,0x12,0xA8,0x3F,0x18,0x01,0x92,
        0x1C,0x00,0xB2,0x48,0xB9,0x94,0xA3,0x19,0x4F,0x19,0xB2,0x32,0x90,0xBA,0x01,0xE6,
        0x91,0x80,0xC1,0xA4,0x2A,0x08,0xA1,0xB1,0x25,0xD2,0x88,0x99,0x21,0x80,0x88,0x80,
        };


        /* flag enable control 0x110 */
        //INLINE
        private void YM2608IRQFlagWrite(FM_OPN OPN, YM2608 F2608, int v)
        {
            if ((v & 0x80) != 0)
            {   /* Reset IRQ flag */
                FM_STATUS_RESET(OPN.ST, 0xf7); /* don't touch BUFRDY flag otherwise we'd have to call ymdeltat module to set the flag back */
            }
            else
            {   /* Set status flag mask */
                F2608.flagmask = (byte)(~(v & 0x1f));
                FM_IRQMASK_SET(OPN.ST, (F2608.irqmask & F2608.flagmask));
            }
        }

        /* compatible mode & IRQ enable control 0x29 */
        //INLINE
        private void YM2608IRQMaskWrite(FM_OPN OPN, YM2608 F2608, int v)
        {
            /* SCH,xx,xxx,EN_ZERO,EN_BRDY,EN_EOS,EN_TB,EN_TA */

            /* extend 3ch. enable/disable */
            if ((v & 0x80) != 0)
                OPN.type |= TYPE_6CH;   /* OPNA mode - 6 FM channels */
            else
                OPN.type &= unchecked((byte)~TYPE_6CH);  /* OPN mode - 3 FM channels */

            /* IRQ MASK store and set */
            F2608.irqmask = (byte)(v & 0x1f);
            FM_IRQMASK_SET(OPN.ST, (F2608.irqmask & F2608.flagmask));
        }

        /* Generate samples for one of the YM2608s */
        private void ym2608_update_one(YM2608 chip, int[][] buffer, int length)
        {
            YM2608 F2608 = (YM2608)chip;
            FM_OPN OPN = F2608.OPN;
            mame.ym_deltat.YM_DELTAT DELTAT = F2608.deltaT;
            int i, j;
            int[] bufL, bufR;
            FM_CH[] cch = new FM_CH[6];
            int[] out_fm = OPN.out_fm;

            /* set bufer */
            bufL = buffer[0];
            bufR = buffer[1];

            cch[0] = F2608.CH[0];
            cch[1] = F2608.CH[1];
            cch[2] = F2608.CH[2];
            cch[3] = F2608.CH[3];
            cch[4] = F2608.CH[4];
            cch[5] = F2608.CH[5];

            /* refresh PG and EG */
            refresh_fc_eg_chan(OPN, cch[0]);
            refresh_fc_eg_chan(OPN, cch[1]);
            if ((OPN.ST.mode & 0xc0) != 0)
            {
                /* 3SLOT MODE */
                if (cch[2].SLOT[SLOT1].Incr == -1)
                {
                    refresh_fc_eg_slot(OPN, cch[2].SLOT[SLOT1], (int)OPN.SL3.fc[1], OPN.SL3.kcode[1]);
                    refresh_fc_eg_slot(OPN, cch[2].SLOT[SLOT2], (int)OPN.SL3.fc[2], OPN.SL3.kcode[2]);
                    refresh_fc_eg_slot(OPN, cch[2].SLOT[SLOT3], (int)OPN.SL3.fc[0], OPN.SL3.kcode[0]);
                    refresh_fc_eg_slot(OPN, cch[2].SLOT[SLOT4], (int)cch[2].fc, cch[2].kcode);
                }
            }
            else
                refresh_fc_eg_chan(OPN, cch[2]);
            refresh_fc_eg_chan(OPN, cch[3]);
            refresh_fc_eg_chan(OPN, cch[4]);
            refresh_fc_eg_chan(OPN, cch[5]);


            /* buffering */
            for (i = 0; i < length; i++)
            {

                advance_lfo(OPN);

                /* clear output acc. */
                OPN.out_adpcm[OUTD_LEFT] = OPN.out_adpcm[OUTD_RIGHT] = OPN.out_adpcm[OUTD_CENTER] = 0;
                OPN.out_delta[OUTD_LEFT] = OPN.out_delta[OUTD_RIGHT] = OPN.out_delta[OUTD_CENTER] = 0;
                /* clear outputs */
                out_fm[0] = 0;
                out_fm[1] = 0;
                out_fm[2] = 0;
                out_fm[3] = 0;
                out_fm[4] = 0;
                out_fm[5] = 0;

                /* calculate FM */
                chan_calc(OPN, cch[0], 0);
                chan_calc(OPN, cch[1], 1);
                chan_calc(OPN, cch[2], 2);
                chan_calc(OPN, cch[3], 3);
                chan_calc(OPN, cch[4], 4);
                chan_calc(OPN, cch[5], 5);

                /* deltaT ADPCM */
                if ((DELTAT.portstate & 0x80)!=0 && F2608.MuteDeltaT == 0)
                    ym_Deltat.YM_DELTAT_ADPCM_CALC(DELTAT);

                /* ADPCMA */
                for (j = 0; j < 6; j++)
                {
                    if (F2608.adpcm[j].flag != 0)
                        ADPCMA_calc_chan(F2608, F2608.adpcm[j]);
                }

                /* advance envelope generator */
                OPN.eg_timer += OPN.eg_timer_add;
                while (OPN.eg_timer >= OPN.eg_timer_overflow)
                {
                    OPN.eg_timer -= OPN.eg_timer_overflow;
                    OPN.eg_cnt++;

                    advance_eg_channel(OPN, cch[0].SLOT);//[SLOT1]);
                    advance_eg_channel(OPN, cch[1].SLOT);//[SLOT1]);
                    advance_eg_channel(OPN, cch[2].SLOT);//[SLOT1]);
                    advance_eg_channel(OPN, cch[3].SLOT);//[SLOT1]);
                    advance_eg_channel(OPN, cch[4].SLOT);//[SLOT1]);
                    advance_eg_channel(OPN, cch[5].SLOT);//[SLOT1]);
                }

                /* buffering */
                {
                    int lt, rt;

                    /*lt =  OPN.out_adpcm[OUTD_LEFT]  + OPN.out_adpcm[OUTD_CENTER];
        			rt =  OPN.out_adpcm[OUTD_RIGHT] + OPN.out_adpcm[OUTD_CENTER];
        			lt += (OPN.out_delta[OUTD_LEFT]  + OPN.out_delta[OUTD_CENTER])>>9;
        			rt += (OPN.out_delta[OUTD_RIGHT] + OPN.out_delta[OUTD_CENTER])>>9;
        			lt += ((out_fm[0]>>1) & OPN.pan[0]);	// shift right verified on real YM2608
        			rt += ((out_fm[0]>>1) & OPN.pan[1]);
        			lt += ((out_fm[1]>>1) & OPN.pan[2]);
        			rt += ((out_fm[1]>>1) & OPN.pan[3]);
        			lt += ((out_fm[2]>>1) & OPN.pan[4]);
        			rt += ((out_fm[2]>>1) & OPN.pan[5]);
        			lt += ((out_fm[3]>>1) & OPN.pan[6]);
        			rt += ((out_fm[3]>>1) & OPN.pan[7]);
        			lt += ((out_fm[4]>>1) & OPN.pan[8]);
        			rt += ((out_fm[4]>>1) & OPN.pan[9]);
        			lt += ((out_fm[5]>>1) & OPN.pan[10]);
        			rt += ((out_fm[5]>>1) & OPN.pan[11]);*/
                    // this way it's louder (and more accurate)
                    lt = (OPN.out_adpcm[OUTD_LEFT] + OPN.out_adpcm[OUTD_CENTER]) << 1;
                    rt = (OPN.out_adpcm[OUTD_RIGHT] + OPN.out_adpcm[OUTD_CENTER]) << 1;
                    lt += (OPN.out_delta[OUTD_LEFT] + OPN.out_delta[OUTD_CENTER]) >> 8;
                    rt += (OPN.out_delta[OUTD_RIGHT] + OPN.out_delta[OUTD_CENTER]) >> 8;
                    lt += (int)(out_fm[0] & OPN.pan[0]);
                    rt += (int)(out_fm[0] & OPN.pan[1]);
                    lt += (int)(out_fm[1] & OPN.pan[2]);
                    rt += (int)(out_fm[1] & OPN.pan[3]);
                    lt += (int)(out_fm[2] & OPN.pan[4]);
                    rt += (int)(out_fm[2] & OPN.pan[5]);
                    lt += (int)(out_fm[3] & OPN.pan[6]);
                    rt += (int)(out_fm[3] & OPN.pan[7]);
                    lt += (int)(out_fm[4] & OPN.pan[8]);
                    rt += (int)(out_fm[4] & OPN.pan[9]);
                    lt += (int)(out_fm[5] & OPN.pan[10]);
                    rt += (int)(out_fm[5] & OPN.pan[11]);

                    lt >>= FINAL_SH;
                    rt >>= FINAL_SH;

                    //Limit( lt, MAXOUT, MINOUT );
                    //Limit( rt, MAXOUT, MINOUT );
                    /* buffering */
                    bufL[i] = lt;
                    bufR[i] = rt;

                    //# ifdef SAVE_SAMPLE
                    //SAVE_ALL_CHANNELS
                    //#endif

                }

                /* timer A control */
                INTERNAL_TIMER_A(OPN, OPN.ST, cch[2]);

            }
            INTERNAL_TIMER_B(OPN.ST, length);


            /* check IRQ for DELTA-T EOS */
            FM_STATUS_SET(OPN.ST, 0);

        }

        //#ifdef __STATE_H__
        private void ym2608_postload(YM2608 chip)
        {
            if (chip != null)
            {
                YM2608 F2608 = (YM2608)chip;
                int r;

                /* prescaler */
                OPNPrescaler_w(F2608.OPN, 1, 2);
                F2608.deltaT.freqbase = F2608.OPN.ST.freqbase;
                /* IRQ mask / mode */
                YM2608IRQMaskWrite(F2608.OPN, F2608, F2608.REGS[0x29]);
                /* SSG registers */
                for (r = 0; r < 16; r++)
                {
                    F2608.OPN.ST.SSG.write(F2608.OPN.ST.param, 0, (short)r);
                    F2608.OPN.ST.SSG.write(F2608.OPN.ST.param, 1, F2608.REGS[r]);
                }

                /* OPN registers */
                /* DT / MULTI , TL , KS / AR , AMON / DR , SR , SL / RR , SSG-EG */
                for (r = 0x30; r < 0x9e; r++)
                    if ((r & 3) != 3)
                    {
                        OPNWriteReg(F2608.OPN, r, F2608.REGS[r]);
                        OPNWriteReg(F2608.OPN, r | 0x100, F2608.REGS[r | 0x100]);
                    }
                /* FB / CONNECT , L / R / AMS / PMS */
                for (r = 0xb0; r < 0xb6; r++)
                    if ((r & 3) != 3)
                    {
                        OPNWriteReg(F2608.OPN, r, F2608.REGS[r]);
                        OPNWriteReg(F2608.OPN, r | 0x100, F2608.REGS[r | 0x100]);
                    }
                /* FM channels */
                /*FM_channel_postload(F2608.CH,6);*/
                /* rhythm(ADPCMA) */
                FM_ADPCMAWrite(F2608, 1, F2608.REGS[0x111]);
                for (r = 0x08; r < 0x0c; r++)
                    FM_ADPCMAWrite(F2608, r, F2608.REGS[r + 0x110]);
                /* Delta-T ADPCM unit */
                ym_Deltat.YM_DELTAT_postload(F2608.deltaT, F2608.REGS, 0x100);
            }
        }

        private void YM2608_save_state(YM2608 F2608, device_config device)
        {

            state_save_register_device_item_array(device, 0, F2608.REGS);
            FMsave_state_st(device, F2608.OPN.ST);
            FMsave_state_channel(device, F2608.CH, 6);
            /* 3slots */
            state_save_register_device_item_array(device, 0, F2608.OPN.SL3.fc);
            state_save_register_device_item(device, 0, F2608.OPN.SL3.fn_h);
            state_save_register_device_item_array(device, 0, F2608.OPN.SL3.kcode);
            /* address register1 */
            state_save_register_device_item(device, 0, F2608.addr_A1);
            /* rythm(ADPCMA) */
            FMsave_state_adpcma(device, F2608.adpcm);
            /* Delta-T ADPCM unit */
            ym_Deltat.YM_DELTAT_savestate(device, F2608.deltaT);
        }
        //#endif /* _STATE_H */

        private void YM2608_deltat_status_set(FM_base chip, byte changebits)
        {
            YM2608 F2608 = (YM2608)chip;
            FM_STATUS_SET(F2608.OPN.ST, changebits);
        }

        private void YM2608_deltat_status_reset(FM_base chip, byte changebits)
        {
            YM2608 F2608 = (YM2608)chip;
            FM_STATUS_RESET(F2608.OPN.ST, changebits);
        }

        /* YM2608(OPNA) */
        //void * ym2608_init(void *param, const device_config *device, int clock, int rate,
        //               void *pcmrom,int pcmsize,
        //               FM_TIMERHANDLER timer_handler,FM_IRQHANDLER IRQHandler, const ssg_callbacks *ssg)
        private YM2608 ym2608_init(YM2608 param, int clock, int rate,
                       FM_ST.dlgFM_TIMERHANDLER timer_handler, FM_ST.dlgFM_IRQHANDLER IRQHandler, _ssg_callbacks ssg)
        {

            YM2608 F2608 = new YM2608();

            /* allocate extend state space */
            if (F2608 == null)
                return null;
            /* clear */
            //memset(F2608, 0, sizeof(YM2608));
            /* allocate total level table (128kb space) */
            if (init_tables() == 0)
            {
                //free(F2608);
                return null;
            }

            F2608.OPN.ST.param = param;
            F2608.OPN.type = TYPE_YM2608;
            F2608.OPN.P_CH = F2608.CH;
            //F2608.OPN.ST.device = device;
            F2608.OPN.ST.clock = clock;
            F2608.OPN.ST.rate = rate;

            /* External handlers */
            F2608.OPN.ST.timer_handler = timer_handler;
            F2608.OPN.ST.IRQ_Handler = IRQHandler;
            F2608.OPN.ST.SSG = ssg;

            /* DELTA-T */
            //F2608.deltaT.memory = (byte *)pcmrom;
            //F2608.deltaT.memory_size = pcmsize;
            F2608.deltaT.memory = null;
            F2608.deltaT.memory_size = 0x00;
            F2608.deltaT.memory_mask = 0x00;

            /*F2608.deltaT.write_time = 20.0 / clock;*/    /* a single byte write takes 20 cycles of main clock */
            /*F2608.deltaT.read_time  = 18.0 / clock;*/    /* a single byte read takes 18 cycles of main clock */

            F2608.deltaT.status_set_handler = YM2608_deltat_status_set;
            F2608.deltaT.status_reset_handler = YM2608_deltat_status_reset;
            F2608.deltaT.status_change_which_chip = F2608;
            F2608.deltaT.status_change_EOS_bit = 0x04; /* status flag: set bit2 on End Of Sample */
            F2608.deltaT.status_change_BRDY_bit = 0x08;    /* status flag: set bit3 on BRDY */
            F2608.deltaT.status_change_ZERO_bit = 0x10;    /* status flag: set bit4 if silence continues for more than 290 miliseconds while recording the ADPCM */

            /* ADPCM Rhythm */
            F2608.pcmbuf = YM2608_ADPCM_ROM;
            F2608.pcm_size = 0x2000;

            Init_ADPCMATable();

            //# ifdef __STATE_H__
            //YM2608_save_state(F2608, device);
            //#endif
            return F2608;
        }

        /* shut down emulator */
        private void ym2608_shutdown(YM2608 chip)
        {
            YM2608 F2608 = (YM2608)chip;

            //free(F2608.deltaT.memory);
            F2608.deltaT.memory = null;

            FMCloseTable();
            //free(F2608);
        }

        /* reset one of chips */
        void ym2608_reset_chip(YM2608 chip)
        {
            int i;
            YM2608 F2608 = (YM2608)chip;
            FM_OPN OPN = F2608.OPN;
            mame.ym_deltat.YM_DELTAT DELTAT = F2608.deltaT;

            /* Reset Prescaler */
            OPNPrescaler_w(OPN, 0, 2);
            F2608.deltaT.freqbase = OPN.ST.freqbase;
            /* reset SSG section */
            OPN.ST.SSG.reset(OPN.ST.param);

            /* status clear */
            FM_BUSY_CLEAR(OPN.ST);

            /* register 0x29 - default value after reset is:
                enable only 3 FM channels and enable all the status flags */
            YM2608IRQMaskWrite(OPN, F2608, 0x1f);   /* default value for D4-D0 is 1 */

            /* register 0x10, A1=1 - default value is 1 for D4, D3, D2, 0 for the rest */
            YM2608IRQFlagWrite(OPN, F2608, 0x1c);   /* default: enable timer A and B, disable EOS, BRDY and ZERO */

            OPNWriteMode(OPN, 0x27, 0x30);  /* mode 0 , timer reset */

            OPN.eg_timer = 0;
            OPN.eg_cnt = 0;

            FM_STATUS_RESET(OPN.ST, 0xff);

            reset_channels(OPN.ST, F2608.CH, 6);
            /* reset OPerator paramater */
            for (i = 0xb6; i >= 0xb4; i--)
            {
                OPNWriteReg(OPN, i, 0xc0);
                OPNWriteReg(OPN, i | 0x100, 0xc0);
            }
            for (i = 0xb2; i >= 0x30; i--)
            {
                OPNWriteReg(OPN, i, 0);
                OPNWriteReg(OPN, i | 0x100, 0);
            }
            for (i = 0x26; i >= 0x20; i--) OPNWriteReg(OPN, i, 0);

            /* ADPCM - percussion sounds */
            for (i = 0; i < 6; i++)
            {
                if (i <= 3) /* channels 0,1,2,3 */
                    F2608.adpcm[i].step = (uint)((float)(1 << ADPCM_SHIFT) * ((float)F2608.OPN.ST.freqbase) / 3.0);
                else        /* channels 4 and 5 work with slower clock */
                    F2608.adpcm[i].step = (uint)((float)(1 << ADPCM_SHIFT) * ((float)F2608.OPN.ST.freqbase) / 6.0);

                F2608.adpcm[i].start = YM2608_ADPCM_ROM_addr[i * 2];
                F2608.adpcm[i].end = YM2608_ADPCM_ROM_addr[i * 2 + 1];

                F2608.adpcm[i].now_addr = 0;
                F2608.adpcm[i].now_step = 0;
                /* F2608.adpcm[i].delta     = 21866; */
                F2608.adpcm[i].vol_mul = 0;
                F2608.adpcm[i].pan = OPN.out_adpcm;
                F2608.adpcm[i].panPtr = OUTD_CENTER; /* default center */
                F2608.adpcm[i].flagMask = 0;
                F2608.adpcm[i].flag = 0;
                F2608.adpcm[i].adpcm_acc = 0;
                F2608.adpcm[i].adpcm_step = 0;
                F2608.adpcm[i].adpcm_out = 0;
            }
            F2608.adpcmTL = 0x3f;

            F2608.adpcm_arrivedEndAddress = 0; /* not used */

            /* DELTA-T unit */
            DELTAT.freqbase = OPN.ST.freqbase;
            DELTAT.output_pointer = OPN.out_delta;
            DELTAT.portshift = 5;      /* always 5bits shift */ /* ASG */
            DELTAT.output_range = 1 << 23;
            ym_Deltat.YM_DELTAT_ADPCM_Reset(DELTAT, OUTD_CENTER,mame.ym_deltat.YM_DELTAT_EMULATION_MODE_NORMAL);
        }

        /* YM2608 write */
        /* n = number  */
        /* a = address */
        /* v = value   */
        private int ym2608_write(byte ChipID, YM2608 chip, int a, byte v)
        {
            YM2608 F2608 = (YM2608)chip;
            FM_OPN OPN = F2608.OPN;
            int addr;

            v &= 0xff;  /*adjust to 8 bit bus */


            switch (a & 3)
            {
                case 0: /* address port 0 */
                    OPN.ST.address = v;
                    F2608.addr_A1 = 0;

                    /* Write register to SSG emulator */
                    if (v < 16) OPN.ST.SSG.write(OPN.ST.param, 0, v);
                    /* prescaler selecter : 2d,2e,2f  */
                    if (v >= 0x2d && v <= 0x2f)
                    {
                        OPNPrescaler_w(OPN, v, 2);
                        //TODO: set ADPCM[c].step
                        F2608.deltaT.freqbase = OPN.ST.freqbase;
                    }
                    break;

                case 1: /* data port 0    */
                    if (F2608.addr_A1 != 0)
                        break;  /* verified on real YM2608 */

                    addr = OPN.ST.address;
                    F2608.REGS[addr] = v;
                    switch (addr & 0xf0)
                    {
                        case 0x00:  /* SSG section */
                            /* Write data to SSG emulator */
                            OPN.ST.SSG.write(OPN.ST.param, (short)a, v);
                            break;
                        case 0x10:  /* 0x10-0x1f : Rhythm section */
                            ym2608_update_req(ChipID, (YM2608)OPN.ST.param);
                            FM_ADPCMAWrite(F2608, addr - 0x10, v);
                            break;
                        case 0x20:  /* Mode Register */
                            switch (addr)
                            {
                                case 0x29:  /* SCH,xx,xxx,EN_ZERO,EN_BRDY,EN_EOS,EN_TB,EN_TA */
                                    YM2608IRQMaskWrite(OPN, F2608, v);
                                    break;
                                default:
                                    ym2608_update_req(ChipID, (YM2608)OPN.ST.param);
                                    OPNWriteMode(OPN, addr, v);
                                    break;
                            }
                            break;
                        default:    /* OPN section */
                            ym2608_update_req(ChipID, (YM2608)OPN.ST.param);
                            OPNWriteReg(OPN, addr, v);
                            break;
                    }
                    break;

                case 2: /* address port 1 */
                    OPN.ST.address = v;
                    F2608.addr_A1 = 1;
                    break;

                case 3: /* data port 1    */
                    if (F2608.addr_A1 != 1)
                        break;  /* verified on real YM2608 */

                    addr = OPN.ST.address;
                    F2608.REGS[addr | 0x100] = v;
                    ym2608_update_req(ChipID, (YM2608)OPN.ST.param);
                    switch (addr & 0xf0)
                    {
                        case 0x00:  /* DELTAT PORT */
                            switch (addr)
                            {
                                case 0x0e:  /* DAC data */
                                    //# ifdef _DEBUG
                                    //logerror("YM2608: write to DAC data (unimplemented) value=%02x\n", v);
                                    //#endif
                                    break;
                                default:
                                    /* 0x00-0x0d */
                                    ym_Deltat.YM_DELTAT_ADPCM_Write(F2608.deltaT, addr, v);
                                    break;
                            }
                            break;
                        case 0x10:  /* IRQ Flag control */
                            if (addr == 0x10)
                            {
                                YM2608IRQFlagWrite(OPN, F2608, v);
                            }
                            break;
                        default:
                            OPNWriteReg(OPN, addr | 0x100, v);
                            break;
                    }
                    break;
            }
            return OPN.ST.irq;
        }

        private byte ym2608_read(YM2608 chip, int a)
        {
            YM2608 F2608 = (YM2608)chip;
            int addr = F2608.OPN.ST.address;
            byte ret = 0;

            switch (a & 3)
            {
                case 0: /* status 0 : YM2203 compatible */
                    /* BUSY:x:x:x:x:x:FLAGB:FLAGA */
                    ret = (byte)(FM_STATUS_FLAG(F2608.OPN.ST) & 0x83);
                    break;

                case 1: /* status 0, ID  */
                    if (addr < 16) ret = (byte)F2608.OPN.ST.SSG.read(F2608.OPN.ST.param);
                    else if (addr == 0xff) ret = 0x01; /* ID code */
                    break;

                case 2: /* status 1 : status 0 + ADPCM status */
                    /* BUSY : x : PCMBUSY : ZERO : BRDY : EOS : FLAGB : FLAGA */
                    ret = (byte)(((byte)FM_STATUS_FLAG(F2608.OPN.ST) & (byte)(F2608.flagmask | 0x80)) | (byte)((F2608.deltaT.PCM_BSY & 1) << 5));
                    break;

                case 3:
                    if (addr == 0x08)
                    {
                        ret =ym_Deltat.YM_DELTAT_ADPCM_Read(F2608.deltaT);
                    }
                    else
                    {
                        if (addr == 0x0f)
                        {
                            //# ifdef _DEBUG
                            //logerror("YM2608 A/D conversion is accessed but not implemented !\n");
                            //#endif
                            ret = 0x80; /* 2's complement PCM data - result from A/D conversion */
                        }
                    }
                    break;
            }
            return ret;
        }

        private int ym2608_timer_over(byte ChipID, YM2608 chip, int c)
        {
            YM2608 F2608 = (YM2608)chip;

            switch (c)
            {
                //#if 0
                //case 2:
                //{	/* BUFRDY flag */
                //YM_DELTAT_BRDY_callback( &F2608.deltaT );
                //}
                //break;
                //#endif
                case 1:
                    {   /* Timer B */
                        TimerBOver(F2608.OPN.ST);
                    }
                    break;
                case 0:
                    {   /* Timer A */
                        ym2608_update_req(ChipID, (YM2608)F2608.OPN.ST.param);
                        /* timer update */
                        TimerAOver(F2608.OPN.ST);
                        /* CSM mode key,TL controll */
                        if ((F2608.OPN.ST.mode & 0x80) != 0)
                        {   /* CSM mode total level latch and auto key on */
                            CSMKeyControll(F2608.OPN.type, F2608.CH[2]);
                        }
                    }
                    break;
                default:
                    break;
            }

            return F2608.OPN.ST.irq;
        }

        private void ym2608_write_pcmrom(YM2608 chip, byte rom_id, int ROMSize, int DataStart,
                                 int DataLength, byte[] ROMData)
        {

            YM2608 F2608 = (YM2608)chip;

            switch (rom_id)
            {
                case 0x01:  // ADPCM
                            // unused, it's constant
                    break;
                case 0x02:  // DELTA-T
                    if (F2608.deltaT.memory_size != ROMSize)
                    {
                        F2608.deltaT.memory = new byte[ROMSize];// (byte)realloc(F2608.deltaT.memory, ROMSize);
                        F2608.deltaT.memory_size = (uint)ROMSize;
                        for (int i = 0; i < ROMSize; i++) F2608.deltaT.memory[i] = 0xff;
                        ym_Deltat.YM_DELTAT_calc_mem_mask(F2608.deltaT);
                    }
                    if (DataStart > ROMSize)
                        return;
                    if (DataStart + DataLength > ROMSize)
                        DataLength = ROMSize - DataStart;

                    for (int i = 0; i < DataLength; i++) F2608.deltaT.memory[DataStart + i] = ROMData[i];
                    break;
            }

            return;
        }

        private void ym2608_set_mutemask(YM2608 chip, uint MuteMask)
        {
            YM2608 F2608 = (YM2608)chip;
            byte CurChn;

            for (CurChn = 0; CurChn < 6; CurChn++)
                F2608.CH[CurChn].Muted = (byte)((MuteMask >> CurChn) & 0x01);
            for (CurChn = 0; CurChn < 6; CurChn++)
                F2608.adpcm[CurChn].Muted = (byte)((MuteMask >> (CurChn + 6)) & 0x01);
            F2608.MuteDeltaT = (byte)((MuteMask >> 12) & 0x01);

            return;
        }
        //#endif /* BUILD_YM2608 */



        //#if (BUILD_YM2610 || BUILD_YM2610B)
        /* YM2610(OPNB) */

        /* Generate samples for one of the YM2610s */
        private void ym2610_update_one(YM2610 chip, int[][] buffer, int length)
        {
            YM2610 F2610 = (YM2610)chip;
            FM_OPN OPN = F2610.OPN;
            mame.ym_deltat.YM_DELTAT DELTAT = F2610.deltaT;
            int i, j;
            int[] bufL, bufR;
            FM_CH[] cch = new FM_CH[4];
            int[] out_fm = OPN.out_fm;

            /* buffer setup */
            bufL = buffer[0];
            bufR = buffer[1];

            cch[0] = F2610.CH[1];
            cch[1] = F2610.CH[2];
            cch[2] = F2610.CH[4];
            cch[3] = F2610.CH[5];

            //#ifdef YM2610B_WARNING
            //#define FM_KEY_IS(SLOT) ((SLOT).key)
            //#define FM_MSG_YM2610B "YM2610-%p.CH%d is playing,Check whether the type of the chip is YM2610B\n"
            /* Check YM2610B warning message */
            if (F2610.CH[0].SLOT[3].key != 0)
            {
                //LOG(LOG_WAR, (FM_MSG_YM2610B, F2610.OPN.ST.param, 0));
                F2610.CH[0].SLOT[3].key = 0;
            }
            if (F2610.CH[3].SLOT[3].key != 0)
            {
                //LOG(LOG_WAR, (FM_MSG_YM2610B, F2610.OPN.ST.param, 3));
                F2610.CH[3].SLOT[3].key = 0;
            }
            //#endif

            /* refresh PG and EG */
            refresh_fc_eg_chan(OPN, cch[0]);
            if ((OPN.ST.mode & 0xc0) != 0)
            {
                /* 3SLOT MODE */
                if (cch[1].SLOT[SLOT1].Incr == -1)
                {
                    refresh_fc_eg_slot(OPN, cch[1].SLOT[SLOT1], (int)OPN.SL3.fc[1], OPN.SL3.kcode[1]);
                    refresh_fc_eg_slot(OPN, cch[1].SLOT[SLOT2], (int)OPN.SL3.fc[2], OPN.SL3.kcode[2]);
                    refresh_fc_eg_slot(OPN, cch[1].SLOT[SLOT3], (int)OPN.SL3.fc[0], OPN.SL3.kcode[0]);
                    refresh_fc_eg_slot(OPN, cch[1].SLOT[SLOT4], (int)cch[1].fc, cch[1].kcode);
                }
            }
            else
                refresh_fc_eg_chan(OPN, cch[1]);
            refresh_fc_eg_chan(OPN, cch[2]);
            refresh_fc_eg_chan(OPN, cch[3]);

            /* buffering */
            for (i = 0; i < length; i++)
            {

                advance_lfo(OPN);

                /* clear output acc. */
                OPN.out_adpcm[OUTD_LEFT] = OPN.out_adpcm[OUTD_RIGHT] = OPN.out_adpcm[OUTD_CENTER] = 0;
                OPN.out_delta[OUTD_LEFT] = OPN.out_delta[OUTD_RIGHT] = OPN.out_delta[OUTD_CENTER] = 0;
                /* clear outputs */
                out_fm[1] = 0;
                out_fm[2] = 0;
                out_fm[4] = 0;
                out_fm[5] = 0;

                /* advance envelope generator */
                OPN.eg_timer += OPN.eg_timer_add;
                while (OPN.eg_timer >= OPN.eg_timer_overflow)
                {
                    OPN.eg_timer -= OPN.eg_timer_overflow;
                    OPN.eg_cnt++;

                    advance_eg_channel(OPN, cch[0].SLOT);//[SLOT1]);
                    advance_eg_channel(OPN, cch[1].SLOT);//[SLOT1]);
                    advance_eg_channel(OPN, cch[2].SLOT);//[SLOT1]);
                    advance_eg_channel(OPN, cch[3].SLOT);//[SLOT1]);
                }

                /* calculate FM */
                chan_calc(OPN, cch[0], 1);  /*remapped to 1*/
                chan_calc(OPN, cch[1], 2);  /*remapped to 2*/
                chan_calc(OPN, cch[2], 4);  /*remapped to 4*/
                chan_calc(OPN, cch[3], 5);  /*remapped to 5*/

                /* deltaT ADPCM */
                if ((DELTAT.portstate & 0x80) != 0 && F2610.MuteDeltaT == 0)
                    ym_Deltat.YM_DELTAT_ADPCM_CALC(DELTAT);

                /* ADPCMA */
                for (j = 0; j < 6; j++)
                {
                    if (F2610.adpcm[j].flag != 0)
                        ADPCMA_calc_chan(F2610, F2610.adpcm[j]);
                }

                /* buffering */
                {
                    int lt, rt;

                    /*lt =  OPN.out_adpcm[OUTD_LEFT]  + OPN.out_adpcm[OUTD_CENTER];
                    rt =  OPN.out_adpcm[OUTD_RIGHT] + OPN.out_adpcm[OUTD_CENTER];
                    lt += (OPN.out_delta[OUTD_LEFT]  + OPN.out_delta[OUTD_CENTER])>>9;
                    rt += (OPN.out_delta[OUTD_RIGHT] + OPN.out_delta[OUTD_CENTER])>>9;


                    lt += ((out_fm[1]>>1) & OPN.pan[2]);	// the shift right was verified on real chip
                    rt += ((out_fm[1]>>1) & OPN.pan[3]);
                    lt += ((out_fm[2]>>1) & OPN.pan[4]);
                    rt += ((out_fm[2]>>1) & OPN.pan[5]);

                    lt += ((out_fm[4]>>1) & OPN.pan[8]);
                    rt += ((out_fm[4]>>1) & OPN.pan[9]);
                    lt += ((out_fm[5]>>1) & OPN.pan[10]);
                    rt += ((out_fm[5]>>1) & OPN.pan[11]);*/

                    lt = (OPN.out_adpcm[OUTD_LEFT] + OPN.out_adpcm[OUTD_CENTER]) << 1;
                    rt = (OPN.out_adpcm[OUTD_RIGHT] + OPN.out_adpcm[OUTD_CENTER]) << 1;
                    lt += (OPN.out_delta[OUTD_LEFT] + OPN.out_delta[OUTD_CENTER]) >> 8;
                    rt += (OPN.out_delta[OUTD_RIGHT] + OPN.out_delta[OUTD_CENTER]) >> 8;


                    lt += (int)(out_fm[1] & OPN.pan[2]);
                    rt += (int)(out_fm[1] & OPN.pan[3]);
                    lt += (int)(out_fm[2] & OPN.pan[4]);
                    rt += (int)(out_fm[2] & OPN.pan[5]);

                    lt += (int)(out_fm[4] & OPN.pan[8]);
                    rt += (int)(out_fm[4] & OPN.pan[9]);
                    lt += (int)(out_fm[5] & OPN.pan[10]);
                    rt += (int)(out_fm[5] & OPN.pan[11]);


                    lt >>= FINAL_SH;
                    rt >>= FINAL_SH;

                    //Limit( lt, MAXOUT, MINOUT );
                    //Limit( rt, MAXOUT, MINOUT );

                    //#ifdef SAVE_SAMPLE
                    //SAVE_ALL_CHANNELS
                    //#endif

                    /* buffering */
                    bufL[i] = lt;
                    bufR[i] = rt;
                }

                /* timer A control */
                INTERNAL_TIMER_A(OPN, OPN.ST, cch[1]);

            }
            INTERNAL_TIMER_B(OPN.ST, length);

        }

        //#if BUILD_YM2610B
        /* Generate samples for one of the YM2610Bs */
        void ym2610b_update_one(YM2610 chip, int[][] buffer, int length)
        {
            YM2610 F2610 = (YM2610)chip;
            FM_OPN OPN = F2610.OPN;
            mame.ym_deltat.YM_DELTAT DELTAT = F2610.deltaT;
            int i, j;
            int[] bufL, bufR;
            FM_CH[] cch = new FM_CH[6];
            int[] out_fm = OPN.out_fm;

            /* buffer setup */
            bufL = buffer[0];
            bufR = buffer[1];

            cch[0] = F2610.CH[0];
            cch[1] = F2610.CH[1];
            cch[2] = F2610.CH[2];
            cch[3] = F2610.CH[3];
            cch[4] = F2610.CH[4];
            cch[5] = F2610.CH[5];

            /* refresh PG and EG */
            refresh_fc_eg_chan(OPN, cch[0]);
            refresh_fc_eg_chan(OPN, cch[1]);
            if ((OPN.ST.mode & 0xc0) != 0)
            {
                /* 3SLOT MODE */
                if (cch[2].SLOT[SLOT1].Incr == -1)
                {
                    refresh_fc_eg_slot(OPN, cch[2].SLOT[SLOT1], (int)OPN.SL3.fc[1], OPN.SL3.kcode[1]);
                    refresh_fc_eg_slot(OPN, cch[2].SLOT[SLOT2], (int)OPN.SL3.fc[2], OPN.SL3.kcode[2]);
                    refresh_fc_eg_slot(OPN, cch[2].SLOT[SLOT3], (int)OPN.SL3.fc[0], OPN.SL3.kcode[0]);
                    refresh_fc_eg_slot(OPN, cch[2].SLOT[SLOT4], (int)cch[2].fc, cch[2].kcode);
                }
            }
            else
                refresh_fc_eg_chan(OPN, cch[2]);
            refresh_fc_eg_chan(OPN, cch[3]);
            refresh_fc_eg_chan(OPN, cch[4]);
            refresh_fc_eg_chan(OPN, cch[5]);

            /* buffering */
            for (i = 0; i < length; i++)
            {

                advance_lfo(OPN);

                /* clear output acc. */
                OPN.out_adpcm[OUTD_LEFT] = OPN.out_adpcm[OUTD_RIGHT] = OPN.out_adpcm[OUTD_CENTER] = 0;
                OPN.out_delta[OUTD_LEFT] = OPN.out_delta[OUTD_RIGHT] = OPN.out_delta[OUTD_CENTER] = 0;
                /* clear outputs */
                out_fm[0] = 0;
                out_fm[1] = 0;
                out_fm[2] = 0;
                out_fm[3] = 0;
                out_fm[4] = 0;
                out_fm[5] = 0;

                /* advance envelope generator */
                OPN.eg_timer += OPN.eg_timer_add;
                while (OPN.eg_timer >= OPN.eg_timer_overflow)
                {
                    OPN.eg_timer -= OPN.eg_timer_overflow;
                    OPN.eg_cnt++;

                    advance_eg_channel(OPN, cch[0].SLOT);//SLOT1]);
                    advance_eg_channel(OPN, cch[1].SLOT);//SLOT1]);
                    advance_eg_channel(OPN, cch[2].SLOT);//SLOT1]);
                    advance_eg_channel(OPN, cch[3].SLOT);//SLOT1]);
                    advance_eg_channel(OPN, cch[4].SLOT);//SLOT1]);
                    advance_eg_channel(OPN, cch[5].SLOT);//SLOT1]);
                }

                /* calculate FM */
                chan_calc(OPN, cch[0], 0);
                chan_calc(OPN, cch[1], 1);
                chan_calc(OPN, cch[2], 2);
                chan_calc(OPN, cch[3], 3);
                chan_calc(OPN, cch[4], 4);
                chan_calc(OPN, cch[5], 5);

                /* deltaT ADPCM */
                if ((DELTAT.portstate & 0x80) != 0 && F2610.MuteDeltaT == 0)
                    ym_Deltat.YM_DELTAT_ADPCM_CALC(DELTAT);

                /* ADPCMA */
                for (j = 0; j < 6; j++)
                {
                    if (F2610.adpcm[j].flag != 0)
                        ADPCMA_calc_chan(F2610, F2610.adpcm[j]);
                }

                /* buffering */
                {
                    int lt, rt;

                    /*lt =  OPN.out_adpcm[OUTD_LEFT]  + OPN.out_adpcm[OUTD_CENTER];
        			rt =  OPN.out_adpcm[OUTD_RIGHT] + OPN.out_adpcm[OUTD_CENTER];
        			lt += (OPN.out_delta[OUTD_LEFT]  + OPN.out_delta[OUTD_CENTER])>>9;
        			rt += (OPN.out_delta[OUTD_RIGHT] + OPN.out_delta[OUTD_CENTER])>>9;

        			lt += ((out_fm[0]>>1) & OPN.pan[0]);	// the shift right is verified on YM2610
        			rt += ((out_fm[0]>>1) & OPN.pan[1]);
        			lt += ((out_fm[1]>>1) & OPN.pan[2]);
        			rt += ((out_fm[1]>>1) & OPN.pan[3]);
        			lt += ((out_fm[2]>>1) & OPN.pan[4]);
        			rt += ((out_fm[2]>>1) & OPN.pan[5]);
        			lt += ((out_fm[3]>>1) & OPN.pan[6]);
        			rt += ((out_fm[3]>>1) & OPN.pan[7]);
        			lt += ((out_fm[4]>>1) & OPN.pan[8]);
        			rt += ((out_fm[4]>>1) & OPN.pan[9]);
        			lt += ((out_fm[5]>>1) & OPN.pan[10]);
        			rt += ((out_fm[5]>>1) & OPN.pan[11]);*/
                    lt = (OPN.out_adpcm[OUTD_LEFT] + OPN.out_adpcm[OUTD_CENTER]) << 1;
                    rt = (OPN.out_adpcm[OUTD_RIGHT] + OPN.out_adpcm[OUTD_CENTER]) << 1;
                    lt += (OPN.out_delta[OUTD_LEFT] + OPN.out_delta[OUTD_CENTER]) >> 8;
                    rt += (OPN.out_delta[OUTD_RIGHT] + OPN.out_delta[OUTD_CENTER]) >> 8;

                    lt += (int)(out_fm[0] & OPN.pan[0]);
                    rt += (int)(out_fm[0] & OPN.pan[1]);
                    lt += (int)(out_fm[1] & OPN.pan[2]);
                    rt += (int)(out_fm[1] & OPN.pan[3]);
                    lt += (int)(out_fm[2] & OPN.pan[4]);
                    rt += (int)(out_fm[2] & OPN.pan[5]);
                    lt += (int)(out_fm[3] & OPN.pan[6]);
                    rt += (int)(out_fm[3] & OPN.pan[7]);
                    lt += (int)(out_fm[4] & OPN.pan[8]);
                    rt += (int)(out_fm[4] & OPN.pan[9]);
                    lt += (int)(out_fm[5] & OPN.pan[10]);
                    rt += (int)(out_fm[5] & OPN.pan[11]);


                    lt >>= FINAL_SH;
                    rt >>= FINAL_SH;

                    //Limit( lt, MAXOUT, MINOUT );
                    //Limit( rt, MAXOUT, MINOUT );

                    //#ifdef SAVE_SAMPLE
                    //SAVE_ALL_CHANNELS
                    //#endif

                    /* buffering */
                    bufL[i] = lt;
                    bufR[i] = rt;
                }

                /* timer A control */
                INTERNAL_TIMER_A(OPN, OPN.ST, cch[2]);

            }
            INTERNAL_TIMER_B(OPN.ST, length);

        }
        //#endif /* BUILD_YM2610B */


        //#ifdef __STATE_H__
        private void ym2610_postload(YM2610 chip)
        {
            if (chip != null)
            {
                YM2610 F2610 = (YM2610)chip;
                int r;

                /* SSG registers */
                for (r = 0; r < 16; r++)
                {
                    F2610.OPN.ST.SSG.write(F2610.OPN.ST.param, 0, (short)r);
                    F2610.OPN.ST.SSG.write(F2610.OPN.ST.param, 1, F2610.REGS[r]);
                }

                /* OPN registers */
                /* DT / MULTI , TL , KS / AR , AMON / DR , SR , SL / RR , SSG-EG */
                for (r = 0x30; r < 0x9e; r++)
                    if ((r & 3) != 3)
                    {
                        OPNWriteReg(F2610.OPN, r, F2610.REGS[r]);
                        OPNWriteReg(F2610.OPN, r | 0x100, F2610.REGS[r | 0x100]);
                    }
                /* FB / CONNECT , L / R / AMS / PMS */
                for (r = 0xb0; r < 0xb6; r++)
                    if ((r & 3) != 3)
                    {
                        OPNWriteReg(F2610.OPN, r, F2610.REGS[r]);
                        OPNWriteReg(F2610.OPN, r | 0x100, F2610.REGS[r | 0x100]);
                    }
                /* FM channels */
                /*FM_channel_postload(F2610.CH,6);*/

                /* rhythm(ADPCMA) */
                FM_ADPCMAWrite(F2610, 1, F2610.REGS[0x101]);
                for (r = 0; r < 6; r++)
                {
                    FM_ADPCMAWrite(F2610, r + 0x08, F2610.REGS[r + 0x108]);
                    FM_ADPCMAWrite(F2610, r + 0x10, F2610.REGS[r + 0x110]);
                    FM_ADPCMAWrite(F2610, r + 0x18, F2610.REGS[r + 0x118]);
                    FM_ADPCMAWrite(F2610, r + 0x20, F2610.REGS[r + 0x120]);
                    FM_ADPCMAWrite(F2610, r + 0x28, F2610.REGS[r + 0x128]);
                }
                /* Delta-T ADPCM unit */
                ym_Deltat.YM_DELTAT_postload(F2610.deltaT, F2610.REGS, 0x010);
            }
        }

        private void YM2610_save_state(YM2610 F2610, device_config device)
        {
            state_save_register_device_item_array(device, 0, F2610.REGS);
            FMsave_state_st(device, F2610.OPN.ST);
            FMsave_state_channel(device, F2610.CH, 6);
            /* 3slots */
            state_save_register_device_item_array(device, 0, F2610.OPN.SL3.fc);
            state_save_register_device_item(device, 0, F2610.OPN.SL3.fn_h);
            state_save_register_device_item_array(device, 0, F2610.OPN.SL3.kcode);
            /* address register1 */
            state_save_register_device_item(device, 0, F2610.addr_A1);

            state_save_register_device_item(device, 0, F2610.adpcm_arrivedEndAddress);
            /* rythm(ADPCMA) */
            FMsave_state_adpcma(device, F2610.adpcm);
            /* Delta-T ADPCM unit */
            ym_Deltat.YM_DELTAT_savestate(device, F2610.deltaT);
        }
        //#endif /* _STATE_H */

        private void YM2610_deltat_status_set(FM_base chip, byte changebits)
        {
            YM2610 F2610 = (YM2610)chip;
            F2610.adpcm_arrivedEndAddress |= changebits;
        }

        private void YM2610_deltat_status_reset(FM_base chip, byte changebits)
        {
            YM2610 F2610 = (YM2610)chip;
            F2610.adpcm_arrivedEndAddress &= (byte)(~changebits);
        }

        //void *ym2610_init(void *param, const device_config *device, int clock, int rate,
        //               void *pcmroma,int pcmsizea,void *pcmromb,int pcmsizeb,
        //               FM_TIMERHANDLER timer_handler,FM_IRQHANDLER IRQHandler, const ssg_callbacks *ssg)
        private YM2610 ym2610_init(YM2610 param, int clock, int rate,
                       FM_ST.dlgFM_TIMERHANDLER timer_handler, FM_ST.dlgFM_IRQHANDLER IRQHandler, _ssg_callbacks ssg)
        {

            YM2610 F2610 = new YM2610();

            /* allocate extend state space */
            if (F2610 == null)
                return null;
            /* clear */
            //memset(F2610, 0, sizeof(YM2610));
            /* allocate total level table (128kb space) */
            if (init_tables() == 0)
            {
                //free(F2610);
                return null;
            }

            /* FM */
            F2610.OPN.ST.param = param;
            F2610.OPN.type = TYPE_YM2610;
            F2610.OPN.P_CH = F2610.CH;
            //F2610.OPN.ST.device = device;
            F2610.OPN.ST.clock = clock;
            F2610.OPN.ST.rate = rate;
            /* Extend handler */
            F2610.OPN.ST.timer_handler = timer_handler;
            F2610.OPN.ST.IRQ_Handler = IRQHandler;
            F2610.OPN.ST.SSG = ssg;
            /* ADPCM */
            //F2610.pcmbuf   = (const byte *)pcmroma;
            //F2610.pcm_size = pcmsizea;
            F2610.pcmbuf = null;
            F2610.pcm_size = 0x00;
            /* DELTA-T */
            //F2610.deltaT.memory = (byte *)pcmromb;
            //F2610.deltaT.memory_size = pcmsizeb;
            F2610.deltaT.memory = null;
            F2610.deltaT.memory_size = 0x00;
            F2610.deltaT.memory_mask = 0x00;

            F2610.deltaT.status_set_handler = YM2610_deltat_status_set;
            F2610.deltaT.status_reset_handler = YM2610_deltat_status_reset;
            F2610.deltaT.status_change_which_chip = F2610;
            F2610.deltaT.status_change_EOS_bit = 0x80; /* status flag: set bit7 on End Of Sample */

            Init_ADPCMATable();
            //# ifdef __STATE_H__
            //YM2610_save_state(F2610, device);
            //#endif
            return F2610;
        }

        /* shut down emulator */
        private void ym2610_shutdown(YM2610 chip)
        {
            YM2610 F2610 = (YM2610)chip;

            //free(F2610.pcmbuf);
            F2610.pcmbuf = null;
            //free(F2610.deltaT.memory);
            F2610.deltaT.memory = null;

            FMCloseTable();
            //free(F2610);
        }

        /* reset one of chip */
        private void ym2610_reset_chip(YM2610 chip)
        {
            int i;
            YM2610 F2610 = (YM2610)chip;
            FM_OPN OPN = F2610.OPN;
            mame.ym_deltat.YM_DELTAT DELTAT = F2610.deltaT;

            /*astring name;
        	device_t* dev = F2610.OPN.ST.device;*/

            /* setup PCM buffers again */
            /*name.printf("%s",dev.tag());
        	F2610.pcmbuf   = (const byte *)dev.machine.region(name).base();
        	F2610.pcm_size = dev.machine.region(name).bytes();
        	name.printf("%s.deltat",dev.tag());
        	F2610.deltaT.memory = (byte *)dev.machine.region(name).base();
        	if(F2610.deltaT.memory == NULL)
        	{
        		F2610.deltaT.memory = (byte*)F2610.pcmbuf;
        		F2610.deltaT.memory_size = F2610.pcm_size;
        	}
        	else
        		F2610.deltaT.memory_size = dev.machine.region(name).bytes();*/

            /* Reset Prescaler */
            OPNSetPres(OPN, 6 * 24, 6 * 24, 4 * 2); /* OPN 1/6 , SSG 1/4 */
            /* reset SSG section */
            OPN.ST.SSG.reset(OPN.ST.param);
            /* status clear */
            FM_IRQMASK_SET(OPN.ST, 0x03);
            FM_BUSY_CLEAR(OPN.ST);
            OPNWriteMode(OPN, 0x27, 0x30); /* mode 0 , timer reset */

            OPN.eg_timer = 0;
            OPN.eg_cnt = 0;

            FM_STATUS_RESET(OPN.ST, 0xff);

            reset_channels(OPN.ST, F2610.CH, 6);
            /* reset OPerator paramater */
            for (i = 0xb6; i >= 0xb4; i--)
            {
                OPNWriteReg(OPN, i, 0xc0);
                OPNWriteReg(OPN, i | 0x100, 0xc0);
            }
            for (i = 0xb2; i >= 0x30; i--)
            {
                OPNWriteReg(OPN, i, 0);
                OPNWriteReg(OPN, i | 0x100, 0);
            }
            for (i = 0x26; i >= 0x20; i--) OPNWriteReg(OPN, i, 0);
            /**** ADPCM work initial ****/
            for (i = 0; i < 6; i++)
            {
                F2610.adpcm[i].step = (uint)((float)(1 << ADPCM_SHIFT) * ((float)F2610.OPN.ST.freqbase) / 3.0);
                F2610.adpcm[i].now_addr = 0;
                F2610.adpcm[i].now_step = 0;
                F2610.adpcm[i].start = 0;
                F2610.adpcm[i].end = 0;
                /* F2610.adpcm[i].delta     = 21866; */
                F2610.adpcm[i].vol_mul = 0;
                F2610.adpcm[i].pan = OPN.out_adpcm;
                F2610.adpcm[i].panPtr = OUTD_CENTER; /* default center */
                F2610.adpcm[i].flagMask = (byte)(1 << i);
                F2610.adpcm[i].flag = 0;
                F2610.adpcm[i].adpcm_acc = 0;
                F2610.adpcm[i].adpcm_step = 0;
                F2610.adpcm[i].adpcm_out = 0;
            }
            F2610.adpcmTL = 0x3f;

            F2610.adpcm_arrivedEndAddress = 0;

            /* DELTA-T unit */
            DELTAT.freqbase = OPN.ST.freqbase;
            DELTAT.output_pointer = OPN.out_delta;
            DELTAT.portshift = 8;      /* allways 8bits shift */
            DELTAT.output_range = 1 << 23;
            ym_Deltat.YM_DELTAT_ADPCM_Reset(DELTAT, OUTD_CENTER, mame.ym_deltat.YM_DELTAT_EMULATION_MODE_YM2610);
        }

        /* YM2610 write */
        /* n = number  */
        /* a = address */
        /* v = value   */
        private int ym2610_write(byte ChipID, YM2610 chip, int a, byte v)
        {
            YM2610 F2610 = (YM2610)chip;
            FM_OPN OPN = F2610.OPN;
            int addr;
            int ch;

            v &= 0xff;  /* adjust to 8 bit bus */

            switch (a & 3)
            {
                case 0: /* address port 0 */
                    OPN.ST.address = v;
                    F2610.addr_A1 = 0;

                    /* Write register to SSG emulator */
                    if (v < 16) OPN.ST.SSG.write(OPN.ST.param, 0, v);
                    break;

                case 1: /* data port 0    */
                    if (F2610.addr_A1 != 0)
                        break;  /* verified on real YM2608 */

                    addr = OPN.ST.address;
                    F2610.REGS[addr] = v;
                    switch (addr & 0xf0)
                    {
                        case 0x00:  /* SSG section */
                            /* Write data to SSG emulator */
                            OPN.ST.SSG.write(OPN.ST.param, (short)a, v);
                            break;
                        case 0x10: /* DeltaT ADPCM */
                            ym2610_update_req(ChipID, (YM2610)OPN.ST.param);

                            switch (addr)
                            {
                                case 0x10:  /* control 1 */
                                case 0x11:  /* control 2 */
                                case 0x12:  /* start address L */
                                case 0x13:  /* start address H */
                                case 0x14:  /* stop address L */
                                case 0x15:  /* stop address H */

                                case 0x19:  /* delta-n L */
                                case 0x1a:  /* delta-n H */
                                case 0x1b:  /* volume */
                                    {
                                        ym_Deltat.YM_DELTAT_ADPCM_Write(F2610.deltaT, addr - 0x10, v);
                                    }
                                    break;

                                case 0x1c: /*  FLAG CONTROL : Extend Status Clear/Mask */
                                    {
                                        byte statusmask = (byte)~v;
                                        /* set arrived flag mask */
                                        for (ch = 0; ch < 6; ch++)
                                            F2610.adpcm[ch].flagMask = (byte)(statusmask & (1 << ch));

                                        F2610.deltaT.status_change_EOS_bit = (byte)(statusmask & 0x80);    /* status flag: set bit7 on End Of Sample */

                                        /* clear arrived flag */
                                        F2610.adpcm_arrivedEndAddress &= statusmask;
                                    }
                                    break;

                                default:
                                    //# ifdef _DEBUG
                                    //logerror("YM2610: write to unknown deltat register %02x val=%02x\n", addr, v);
                                    //#endif
                                    break;
                            }

                            break;
                        case 0x20:  /* Mode Register */
                            ym2610_update_req(ChipID, (YM2610)OPN.ST.param);
                            OPNWriteMode(OPN, addr, v);
                            break;
                        default:    /* OPN section */
                            ym2610_update_req(ChipID, (YM2610)OPN.ST.param);
                            /* write register */
                            OPNWriteReg(OPN, addr, v);
                            break;
                    }
                    break;

                case 2: /* address port 1 */
                    OPN.ST.address = v;
                    F2610.addr_A1 = 1;
                    break;

                case 3: /* data port 1    */
                    if (F2610.addr_A1 != 1)
                        break;  /* verified on real YM2608 */

                    ym2610_update_req(ChipID, (YM2610)OPN.ST.param);
                    addr = OPN.ST.address;
                    F2610.REGS[addr | 0x100] = v;
                    if (addr < 0x30)
                        /* 100-12f : ADPCM A section */
                        FM_ADPCMAWrite(F2610, addr, v);
                    else
                        OPNWriteReg(OPN, addr | 0x100, v);
                    break;
            }
            return OPN.ST.irq;
        }

        private byte ym2610_read(YM2610 chip, int a)
        {
            YM2610 F2610 = (YM2610)chip;
            int addr = F2610.OPN.ST.address;
            byte ret = 0;

            switch (a & 3)
            {
                case 0: /* status 0 : YM2203 compatible */
                    ret = (byte)(FM_STATUS_FLAG(F2610.OPN.ST) & 0x83);
                    break;
                case 1: /* data 0 */
                    if (addr < 16) ret = (byte)F2610.OPN.ST.SSG.read(F2610.OPN.ST.param);
                    if (addr == 0xff) ret = 0x01;
                    break;
                case 2: /* status 1 : ADPCM status */
                    /* ADPCM STATUS (arrived End Address) */
                    /* B,--,A5,A4,A3,A2,A1,A0 */
                    /* B     = ADPCM-B(DELTA-T) arrived end address */
                    /* A0-A5 = ADPCM-A          arrived end address */
                    ret = F2610.adpcm_arrivedEndAddress;
                    break;
                case 3:
                    ret = 0;
                    break;
            }
            return ret;
        }

        private int ym2610_timer_over(byte ChipID, YM2610 chip, int c)
        {
            YM2610 F2610 = (YM2610)chip;

            if (c != 0)
            {   /* Timer B */
                TimerBOver(F2610.OPN.ST);
            }
            else
            {   /* Timer A */
                ym2610_update_req(ChipID, (YM2610)F2610.OPN.ST.param);
                /* timer update */
                TimerAOver(F2610.OPN.ST);
                /* CSM mode key,TL controll */
                if ((F2610.OPN.ST.mode & 0x80) != 0)
                {   /* CSM mode total level latch and auto key on */
                    CSMKeyControll(F2610.OPN.type, F2610.CH[2]);
                }
            }
            return F2610.OPN.ST.irq;
        }

        private void ym2610_write_pcmrom(YM2610 chip, byte rom_id, uint ROMSize, int DataStart,
                                 int DataLength, byte[] ROMData)
        {

            YM2610 F2610 = (YM2610)chip;

            switch (rom_id)
            {
                case 0x01:  // ADPCM
                    if (F2610.pcm_size != ROMSize)
                    {
                        F2610.pcmbuf = new byte[ROMSize];// (byte*)realloc(F2610.pcmbuf, ROMSize);
                        F2610.pcm_size = ROMSize;
                        for (int i = 0; i < ROMSize; i++) F2610.pcmbuf[i] = 0xff;
                    }
                    if (DataStart > ROMSize)
                        return;
                    if (DataStart + DataLength > ROMSize)
                        DataLength = (int)(ROMSize - DataStart);

                    for (int i = 0; i < DataLength; i++) F2610.pcmbuf[DataStart + i] = ROMData[i];
                    break;
                case 0x02:  // DELTA-T
                    if (F2610.deltaT.memory_size != ROMSize)
                    {
                        F2610.deltaT.memory = new byte[ROMSize];// (byte*)realloc(F2610.deltaT.memory, ROMSize);
                        F2610.deltaT.memory_size = ROMSize;
                        for (int i = 0; i < ROMSize; i++) F2610.deltaT.memory[i] = 0xff;
                        ym_Deltat.YM_DELTAT_calc_mem_mask(F2610.deltaT);
                    }
                    if (DataStart > ROMSize)
                        return;
                    if (DataStart + DataLength > ROMSize)
                        DataLength = (int)(ROMSize - DataStart);

                    for (int i = 0; i < DataLength; i++) F2610.deltaT.memory[DataStart + i] = ROMData[i];
                    break;
            }

            return;
        }

        private void ym2610_set_mutemask(YM2610 chip, uint MuteMask)
        {
            YM2610 F2610 = (YM2610)chip;
            byte CurChn;

            for (CurChn = 0; CurChn < 6; CurChn++)
                F2610.CH[CurChn].Muted = (byte)((MuteMask >> CurChn) & 0x01);
            for (CurChn = 0; CurChn < 6; CurChn++)
                F2610.adpcm[CurChn].Muted = (byte)((MuteMask >> (CurChn + 6)) & 0x01);
            F2610.MuteDeltaT = (byte)((MuteMask >> 12) & 0x01);

            return;
        }
        //#endif /* (BUILD_YM2610||BUILD_YM2610B) */




        private mame.ym_deltat ym_Deltat = new mame.ym_deltat();

        private void state_save_register_device_item(device_config device, int ch, int fc)
        {
            throw new NotImplementedException();
        }

        private void state_save_register_device_item(device_config device, int ch, uint fc)
        {
            throw new NotImplementedException();
        }

        private void state_save_register_device_item_array(device_config device, int ch, int[] op1_out)
        {
            throw new NotImplementedException();
        }

        private void state_save_register_device_item_array(device_config device, int ch, uint[] op1_out)
        {
            throw new NotImplementedException();
        }

        private void state_save_register_device_item_array(device_config device, int ch, byte[] op1_out)
        {
            throw new NotImplementedException();
        }

        public class device_config
        {
        }

    }

}