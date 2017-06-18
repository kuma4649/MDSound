using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDSound
{
    public class ym2413 : Instrument
    {
        //# ifndef _EMU2413_H_
        //#define _EMU2413_H_

        //# include "emutypes.h"

        //# ifdef EMU2413_DLL_EXPORTS
        //#define EMU2413_API __declspec(dllexport)
        //#elif defined(EMU2413_DLL_IMPORTS)
        //#define EMU2413_API __declspec(dllimport)
        //#else
        //#define EMU2413_API
        //#endif

        private const double PI = 3.14159265358979323846;

        private enum OPLL_TONE_ENUM { OPLL_2413_TONE = 0, OPLL_VRC7_TONE = 1, OPLL_281B_TONE = 2 };

        /* voice data */
        private class OPLL_PATCH
        {
            public uint TL, FB, EG, ML, AR, DR, SL, RR, KR, KL, AM, PM, WF;
        }

        /* slot */
        private class OPLL_SLOT
        {

            public OPLL_PATCH patch;

            public int type;          /* 0 : modulator 1 : carrier */

            /* OUTPUT */
            public int feedback;
            public int[] output = new int[2];   /* Output value of slot */

            /* for Phase Generator (PG) */
            public ushort[] sintbl;    /* Wavetable */
            public uint phase;      /* Phase */
            public uint dphase;     /* Phase increment amount */
            public uint pgout;      /* output */

            /* for Envelope Generator (EG) */
            public int fnum;          /* F-Number */
            public int block;         /* Block */
            public int volume;        /* Current volume */
            public int sustine;       /* Sustine 1 = ON, 0 = OFF */
            public uint tll;         /* Total Level + Key scale level*/
            public uint rks;        /* Key scale offset (Rks) */
            public int eg_mode;       /* Current state */
            public uint eg_phase;   /* Phase */
            public uint eg_dphase;  /* Phase increment amount */
            public uint egout;      /* output */

        }

        /* Mask */
        private int OPLL_MASK_CH(int x) { return (1 << (x)); }
        private int OPLL_MASK_HH = (1 << (9));
        private int OPLL_MASK_CYM = (1 << (10));
        private int OPLL_MASK_TOM = (1 << (11));
        private int OPLL_MASK_SD = (1 << (12));
        private int OPLL_MASK_BD = (1 << (13));
        //private int OPLL_MASK_RHYTHM = 0x1f << 9;//(OPLL_MASK_HH | OPLL_MASK_CYM | OPLL_MASK_TOM | OPLL_MASK_SD | OPLL_MASK_BD);

        /* opll */
        private class OPLL
        {

            public byte vrc7_mode;
            public byte adr;
            //e_uint32 adr ;
            public int _out;

            //#ifndef EMU2413_COMPACTION
            public uint realstep;
            public uint oplltime;
            public uint opllstep;
            public int prev, next;
            public int[] sprev = new int[2];
            public int[] snext = new int[2];
            public float[][] pan = new float[14][] { new float[2], new float[2], new float[2], new float[2], new float[2], new float[2], new float[2],
                new float[2],new float[2],new float[2],new float[2],new float[2],new float[2],new float[2] };
            //#endif

            /* Register */
            public byte[] reg = new byte[0x40];
            public int[] slot_on_flag = new int[18];

            /* Pitch Modulator */
            public uint pm_phase;
            public int lfo_pm;

            /* Amp Modulator */
            public int am_phase;
            public int lfo_am;

            public uint quality;

            /* Noise Generator */
            public uint noise_seed;

            /* Channel Data */
            public int[] patch_number = new int[9];
            public int[] key_status = new int[9];

            /* Slot */
            public OPLL_SLOT[] slot = new OPLL_SLOT[18];

            /* Voice Data */
            public OPLL_PATCH[][] patch = new OPLL_PATCH[19][] {
                new OPLL_PATCH[2], new OPLL_PATCH[2], new OPLL_PATCH[2], new OPLL_PATCH[2]
                , new OPLL_PATCH[2], new OPLL_PATCH[2], new OPLL_PATCH[2], new OPLL_PATCH[2]
                , new OPLL_PATCH[2], new OPLL_PATCH[2], new OPLL_PATCH[2], new OPLL_PATCH[2]
                , new OPLL_PATCH[2], new OPLL_PATCH[2], new OPLL_PATCH[2], new OPLL_PATCH[2]
                , new OPLL_PATCH[2], new OPLL_PATCH[2], new OPLL_PATCH[2]
            };
            public int[] patch_update = new int[2]; /* flag for check patch update */

            public uint mask;

        }

        /* Create Object */
        //private OPLL OPLL_new(uint clk, uint rate) { return null; }
        //private void OPLL_delete(OPLL opll) { }

        /* Setup */
        //private void OPLL_reset(OPLL opll) { }
        //private void OPLL_reset_patch(OPLL opll, int a) { }
        //private void OPLL_set_rate(OPLL opll, uint r) { }
        //private void OPLL_set_quality(OPLL opll, uint q) { }
        //private void OPLL_set_pan(OPLL opll, uint ch, int pan) { }

        /* Port/Register access */
        //private void OPLL_writeIO(OPLL opll, uint reg, uint val) { }
        //private void OPLL_writeReg(OPLL opll, uint reg, uint val) { }

        /* Synthsize */
        //private short OPLL_calc(OPLL opll) { return 0; }
        //private void OPLL_calc_stereo(OPLL opll, int[][] _out, int samples) { }

        /* Misc */
        //private void OPLL_setPatch(OPLL opll, byte[] dump) { }
        //private void OPLL_copyPatch(OPLL opll, int a, OPLL_PATCH p) { }
        //private void OPLL_forceRefresh(OPLL opll) { }

        /* Utility */
        //private void OPLL_dump2patch(byte[] dump, OPLL_PATCH patch) { }
        //private void OPLL_patch2dump(OPLL_PATCH patch, byte[] dump) { }
        //private void OPLL_getDefaultPatch(int type, int num, OPLL_PATCH p) { }

        /* Channel Mask */
        //EMU2413_API e_uint32 OPLL_setMask(OPLL *, e_uint32 mask) ;
        //EMU2413_API e_uint32 OPLL_toggleMask(OPLL *, e_uint32 mask) ;
        //private void OPLL_SetMuteMask(OPLL opll, uint MuteMask) { }
        //private void OPLL_SetChipMode(OPLL opll, byte Mode) { }

        //#define dump2patch OPLL_dump2patch

        //#endif

        /***********************************************************************************

          emu2413.c -- YM2413 emulator written by Mitsutaka Okazaki 2001

          2001 01-08 : Version 0.10 -- 1st version.
          2001 01-15 : Version 0.20 -- semi-public version.
          2001 01-16 : Version 0.30 -- 1st public version.
          2001 01-17 : Version 0.31 -- Fixed bassdrum problem.
                     : Version 0.32 -- LPF implemented.
          2001 01-18 : Version 0.33 -- Fixed the drum problem, refine the mix-down method.
                                    -- Fixed the LFO bug.
          2001 01-24 : Version 0.35 -- Fixed the drum problem, 
                                       support undocumented EG behavior.
          2001 02-02 : Version 0.38 -- Improved the performance.
                                       Fixed the hi-hat and cymbal model.
                                       Fixed the default percussive datas.
                                       Noise reduction.
                                       Fixed the feedback problem.
          2001 03-03 : Version 0.39 -- Fixed some drum bugs.
                                       Improved the performance.
          2001 03-04 : Version 0.40 -- Improved the feedback.
                                       Change the default table size.
                                       Clock and Rate can be changed during play.
          2001 06-24 : Version 0.50 -- Improved the hi-hat and the cymbal tone.
                                       Added VRC7 patch (OPLL_reset_patch is changed).
                                       Fixed OPLL_reset() bug.
                                       Added OPLL_setMask, OPLL_getMask and OPLL_toggleMask.
                                       Added OPLL_writeIO.
          2001 09-28 : Version 0.51 -- Removed the noise table.
          2002 01-28 : Version 0.52 -- Added Stereo mode.
          2002 02-07 : Version 0.53 -- Fixed some drum bugs.
          2002 02-20 : Version 0.54 -- Added the best quality mode.
          2002 03-02 : Version 0.55 -- Removed OPLL_init & OPLL_close.
          2002 05-30 : Version 0.60 -- Fixed HH&CYM generator and all voice datas.
          2004 04-10 : Version 0.61 -- Added YMF281B tone (defined by Chabin).

          References: 
            fmopl.c        -- 1999,2000 written by Tatsuyuki Satoh (MAME development).
            fmopl.c(fixed) -- (C) 2002 Jarek Burczynski.
            s_opl.c        -- 2001 written by Mamiya (NEZplug development).
            fmgen.cpp      -- 1999,2000 written by cisc.
            fmpac.ill      -- 2000 created by NARUTO.
            MSX-Datapack
            YMU757 data sheet
            YM2143 data sheet

        **************************************************************************************/

        /**
         *  Additions by Maxim:
         *  - per-channel panning
         *
         **/
        //# include <stdio.h>
        //# include <stdlib.h>
        //# include <string.h>
        //# include <math.h>
        //# include "mamedef.h"
        //#undef INLINE
        //# include "emu2413.h"
        //# include "panning.h" // Maxim

        /*#ifdef EMU2413_COMPACTION
        #define OPLL_TONE_NUM 1
        static unsigned char default_inst[OPLL_TONE_NUM][(16 + 3) * 16] = {
          {
        #include "2413tone.h"
           }
        };
        #else
        #define OPLL_TONE_NUM 3
        static unsigned char default_inst[OPLL_TONE_NUM][(16 + 3) * 16] = {
          { 
        #include "2413tone.h" 
          },
          {
        #include "vrc7tone.h"
           },
          {
        #include "281btone.h"
          }
        };
        #endif*/

        // Note: Dump size changed to 8 per instrument, since 9-15 were unused. -VB
        private const int OPLL_TONE_NUM = 1;
        private byte[][] default_inst = new byte[1][] {
            new byte[(16 + 3) * 8]  {
/* YM2413 tone by okazaki@angel.ne.jp */
0x49,0x4c,0x4c,0x32,0x00,0x00,0x00,0x00,
0x61,0x61,0x1e,0x17,0xf0,0x7f,0x00,0x17,
0x13,0x41,0x16,0x0e,0xfd,0xf4,0x23,0x23,
0x03,0x01,0x9a,0x04,0xf3,0xf3,0x13,0xf3,
0x11,0x61,0x0e,0x07,0xfa,0x64,0x70,0x17,
0x22,0x21,0x1e,0x06,0xf0,0x76,0x00,0x28,
0x21,0x22,0x16,0x05,0xf0,0x71,0x00,0x18,
0x21,0x61,0x1d,0x07,0x82,0x80,0x17,0x17,
0x23,0x21,0x2d,0x16,0x90,0x90,0x00,0x07,
0x21,0x21,0x1b,0x06,0x64,0x65,0x10,0x17,
0x21,0x21,0x0b,0x1a,0x85,0xa0,0x70,0x07,
0x23,0x01,0x83,0x10,0xff,0xb4,0x10,0xf4,
0x97,0xc1,0x20,0x07,0xff,0xf4,0x22,0x22,
0x61,0x00,0x0c,0x05,0xc2,0xf6,0x40,0x44,
0x01,0x01,0x56,0x03,0x94,0xc2,0x03,0x12,
0x21,0x01,0x89,0x03,0xf1,0xe4,0xf0,0x23,
0x07,0x21,0x14,0x00,0xee,0xf8,0xff,0xf8,
0x01,0x31,0x00,0x00,0xf8,0xf7,0xf8,0xf7,
0x25,0x11,0x00,0x00,0xf8,0xfa,0xf8,0x55,
            }
        };
        //  {
        //#include "2413tone.h"
        //  }
        //};

        /* Size of Sintable ( 8 -- 18 can be used. 9 recommended.) */
        private int PG_BITS = 9;
        private const int PG_WIDTH = (1 << 9);// PG_BITS);

        /* Phase increment counter */
        private int DP_BITS = 18;
        private int DP_WIDTH = (1 << 18);// DP_BITS);
        private int DP_BASE_BITS = 18 - 9;//(DP_BITS - PG_BITS);

        /* Dynamic range (Accuracy of sin table) */
        //private int DB_BITS = 8;
        private double DB_STEP = (48.0 / (1 << 8));// DB_BITS));
        private const int DB_MUTE = (1 << 8);// DB_BITS);

        /* Dynamic range of envelope */
        private double EG_STEP = 0.375;
        private const int EG_BITS = 7;
        //private int EG_MUTE = (1 << 7);// EG_BITS);

        /* Dynamic range of total level */
        private double TL_STEP = 0.75;
        //private int TL_BITS = 6;
        //private int TL_MUTE = (1 << 6);// TL_BITS);

        /* Dynamic range of sustine level */
        private double SL_STEP = 3.0;
        //private int SL_BITS = 4;
        //private int SL_MUTE = (1 << 4);// SL_BITS);

        //#define EG2DB(d) ((d)*(e_int32)(EG_STEP/DB_STEP))
        private int EG2DB(int d) { return ((d) * (int)(EG_STEP / DB_STEP)); }
        //#define TL2EG(d) ((d)*(e_int32)(TL_STEP/EG_STEP))
        private int TL2EG(int d) { return ((d) * (int)(TL_STEP / EG_STEP)); }
        //#define SL2EG(d) ((d)*(e_int32)(SL_STEP/EG_STEP))
        private int SL2EG(int d) { return ((d) * (int)(SL_STEP / EG_STEP)); }

        //#define DB_POS(x) (e_uint32)((x)/DB_STEP)
        private uint DB_POS(double x) { return (uint)(x / DB_STEP); }
        //#define DB_NEG(x) (e_uint32)(DB_MUTE+DB_MUTE+(x)/DB_STEP)
        private uint DB_NEG(double x) { return (uint)(DB_MUTE+DB_MUTE+ x / DB_STEP); }

        /* Bits for liner value */
        private int DB2LIN_AMP_BITS = 8;
        private int SLOT_AMP_BITS = 8;//(DB2LIN_AMP_BITS);

        /* Bits for envelope phase incremental counter */
        private int EG_DP_BITS = 22;
        private int EG_DP_WIDTH = (1 << 22);// EG_DP_BITS);

        /* Bits for Pitch and Amp modulator */
        private int PM_PG_BITS = 8;
        private const int PM_PG_WIDTH = (1 << 8);// PM_PG_BITS);
        private int PM_DP_BITS = 16;
        private int PM_DP_WIDTH = (1 << 16);//PM_DP_BITS);
        private int AM_PG_BITS = 8;
        private const int AM_PG_WIDTH = (1 << 8);//AM_PG_BITS);
        private int AM_DP_BITS = 16;
        private int AM_DP_WIDTH = (1 << 16);//AM_DP_BITS);

        /* PM table is calcurated by PM_AMP * pow(2,PM_DEPTH*sin(x)/1200) */
        private int PM_AMP_BITS = 8;
        private int PM_AMP = (1 << 8);// PM_AMP_BITS);

        /* PM speed(Hz) and depth(cent) */
        private double PM_SPEED = 6.4;
        private double PM_DEPTH = 13.75;

        /* AM speed(Hz) and depth(dB) */
        private double AM_SPEED = 3.6413;
        private double AM_DEPTH = 4.875;

        /* Cut the lower b bit(s) off. */
        private int HIGHBITS(int c, int b) { return ((c) >> (b)); }

        /* Leave the lower b bit(s). */
        private int LOWBITS(int c, int b) { return ((c) & ((1 << (b)) - 1)); }

        /* Expand x which is s bits to d bits. */
        private int EXPAND_BITS(int x, int s, int d) { return ((x) << ((d) - (s))); }

        /* Expand x which is s bits to d bits and fill expanded bits '1' */
        private int EXPAND_BITS_X(int x, int s, int d) { return (((x) << ((d) - (s))) | ((1 << ((d) - (s))) - 1)); }

        /* Adjust envelope speed which depends on sampling rate. */
        private int RATE_ADJUST(uint x) { return (rate == 49716 ? (int)x : (int)((double)(x) * clk / 72 / rate + 0.5)); }        /* added 0.5 to round the value*/

        private OPLL_SLOT MOD(OPLL o, int x) { return (o.slot[x << 1]); }
        private OPLL_SLOT CAR(OPLL o, int x) { return o.slot[(x << 1) | 1]; }

        private int BIT(int s, int b) { return (((s) >> (b)) & 1); }

        /* Input clock */
        private uint clk = 844451141;
        /* Sampling rate */
        private uint rate = 3354932;

        /* WaveTable for each envelope amp */
        private ushort[] fullsintable = new ushort[PG_WIDTH];
        private ushort[] halfsintable = new ushort[PG_WIDTH];

        private ushort[][] waveform = new ushort[2][];// { fullsintable, halfsintable };

        /* LFO Table */
        private int[] pmtable = new int[PM_PG_WIDTH];
        private int[] amtable = new int[AM_PG_WIDTH];

        /* Phase delta for LFO */
        private uint pm_dphase;
        private uint am_dphase;

        /* dB to Liner table */
        private short[] DB2LIN_TABLE = new short[(DB_MUTE + DB_MUTE) * 2];

        /* Liner to Log curve conversion table (for Attack rate). */
        private ushort[] AR_ADJUST_TABLE = new ushort[1 << EG_BITS];

        /* Empty voice data */
        private OPLL_PATCH null_patch = new OPLL_PATCH(); //{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        /* Basic voice Data */
        //private OPLL_PATCH[][] default_patch = new OPLL_PATCH[OPLL_TONE_NUM][] { new OPLL_PATCH[(16 + 3) * 2] };
        private OPLL_PATCH[][] default_patch = new OPLL_PATCH[19][] {
            new OPLL_PATCH[2], new OPLL_PATCH[2], new OPLL_PATCH[2], new OPLL_PATCH[2]
            , new OPLL_PATCH[2], new OPLL_PATCH[2], new OPLL_PATCH[2], new OPLL_PATCH[2]
            , new OPLL_PATCH[2], new OPLL_PATCH[2], new OPLL_PATCH[2], new OPLL_PATCH[2]
            , new OPLL_PATCH[2], new OPLL_PATCH[2], new OPLL_PATCH[2], new OPLL_PATCH[2]
            , new OPLL_PATCH[2], new OPLL_PATCH[2], new OPLL_PATCH[2]};

        /* Definition of envelope mode */
        private enum OPLL_EG_STATE { READY, ATTACK, DECAY, SUSHOLD, SUSTINE, RELEASE, SETTLE, FINISH };

        /* Phase incr table for Attack */
        private uint[][] dphaseARTable = new uint[16][] {
            new uint[16], new uint[16], new uint[16], new uint[16],
            new uint[16], new uint[16], new uint[16], new uint[16],
            new uint[16], new uint[16], new uint[16], new uint[16],
            new uint[16], new uint[16], new uint[16], new uint[16]};
        /* Phase incr table for Decay and Release */
        private uint[][] dphaseDRTable = new uint[16][] {
            new uint[16], new uint[16], new uint[16], new uint[16],
            new uint[16], new uint[16], new uint[16], new uint[16],
            new uint[16], new uint[16], new uint[16], new uint[16],
            new uint[16], new uint[16], new uint[16], new uint[16]};

        /* KSL + TL Table */
        private uint[][][][] tllTable;//[16][8][1 << TL_BITS][4];
        private int[][][] rksTable;//[2][8][2];

        /* Phase incr table for PG */
        private uint[][][] dphaseTable;//[512][8][16];

        /***************************************************

                          Create tables

        ****************************************************/
        private int Min(int i, int j)
        {
            if (i < j)
                return i;
            else
                return j;
        }

        /* Table for AR to LogCurve. */
        private void makeAdjustTable()
        {
            int i;

            AR_ADJUST_TABLE[0] = (1 << EG_BITS) - 1;
            for (i = 1; i < (1 << EG_BITS); i++)
                AR_ADJUST_TABLE[i] = (ushort)((double)(1 << EG_BITS) - 1 - ((1 << EG_BITS) - 1) * Math.Log(i) / Math.Log(127));
        }


        /* Table for dB(0 -- (1<<DB_BITS)-1) to Liner(0 -- DB2LIN_AMP_WIDTH) */
        private void makeDB2LinTable()
        {
            int i;

            for (i = 0; i < DB_MUTE + DB_MUTE; i++)
            {
                DB2LIN_TABLE[i] = (short)((double)((1 << DB2LIN_AMP_BITS) - 1) * Math.Pow(10, -(double)i * DB_STEP / 20));
                if (i >= DB_MUTE) DB2LIN_TABLE[i] = 0;
                DB2LIN_TABLE[i + DB_MUTE + DB_MUTE] = (short)(-DB2LIN_TABLE[i]);
            }
        }

        /* Liner(+0.0 - +1.0) to dB((1<<DB_BITS) - 1 -- 0) */
        private int lin2db(double d)
        {
            if (d == 0)
                return (DB_MUTE - 1);
            else
                return Min(-(int)(20.0 * Math.Log10(d) / DB_STEP), DB_MUTE - 1);  /* 0 -- 127 */
        }

        /* Sin Table */
        private void makeSinTable()
        {
            int i;

            for (i = 0; i < PG_WIDTH / 4; i++)
            {
                fullsintable[i] = (ushort)lin2db(Math.Sin(2.0 * PI * i / PG_WIDTH));
            }

            for (i = 0; i < PG_WIDTH / 4; i++)
            {
                fullsintable[PG_WIDTH / 2 - 1 - i] = fullsintable[i];
            }

            for (i = 0; i < PG_WIDTH / 2; i++)
            {
                fullsintable[PG_WIDTH / 2 + i] = (ushort)(DB_MUTE + DB_MUTE + fullsintable[i]);
            }

            for (i = 0; i < PG_WIDTH / 2; i++)
                halfsintable[i] = fullsintable[i];
            for (i = PG_WIDTH / 2; i < PG_WIDTH; i++)
                halfsintable[i] = fullsintable[0];

            waveform[0] = fullsintable;
            waveform[1] = halfsintable;
        }

        private double saw(double phase)
        {
            if (phase <= PI / 2)
                return phase * 2 / PI;
            else if (phase <= PI * 3 / 2)
                return 2.0 - (phase * 2 / PI);
            else
                return -4.0 + phase * 2 / PI;
        }

        /* Table for Pitch Modulator */
        private void makePmTable()
        {
            int i;

            for (i = 0; i < PM_PG_WIDTH; i++)
                /* pmtable[i] = (e_int32) ((double) PM_AMP * pow (2, (double) PM_DEPTH * sin (2.0 * PI * i / PM_PG_WIDTH) / 1200)); */
                pmtable[i] = (int)((double)PM_AMP * Math.Pow(2, (double)PM_DEPTH * saw(2.0 * PI * i / PM_PG_WIDTH) / 1200));
        }

        /* Table for Amp Modulator */
        private void makeAmTable()
        {
            int i;

            for (i = 0; i < AM_PG_WIDTH; i++)
                /* amtable[i] = (e_int32) ((double) AM_DEPTH / 2 / DB_STEP * (1.0 + sin (2.0 * PI * i / PM_PG_WIDTH))); */
                amtable[i] = (int)((double)AM_DEPTH / 2 / DB_STEP * (1.0 + saw(2.0 * PI * i / PM_PG_WIDTH)));
        }

        /* Phase increment counter table */
        private void makeDphaseTable()
        {
            uint fnum, block, ML;
            uint[] mltable = new uint[16]
              { 1, 1 * 2, 2 * 2, 3 * 2, 4 * 2, 5 * 2, 6 * 2, 7 * 2, 8 * 2, 9 * 2, 10 * 2, 10 * 2, 12 * 2, 12 * 2, 15 * 2, 15 * 2 };

            dphaseTable = new uint[512][][];
            for (fnum = 0; fnum < 512; fnum++)
            {
                dphaseTable[fnum] = new uint[8][];
                for (block = 0; block < 8; block++)
                {
                    dphaseTable[fnum][block] = new uint[16];
                    for (ML = 0; ML < 16; ML++)
                    {
                        uint x = fnum * mltable[ML];
                        x = x << (int)block;
                        x = x >> (20 - DP_BITS);
                        dphaseTable[fnum][block][ML] = (rate == 49716 ? x : (uint)((double)(x) * clk / 72 / rate + 0.5));
                    }
                }
            }
            //dphaseTable[fnum][block][ML] = RATE_ADJUST(((fnum * mltable[ML]) << block) >> (20 - DP_BITS));
            //#define RATE_ADJUST(x) (rate==49716?x:(e_uint32)((double)(x)*clk/72/rate + 0.5))        /* added 0.5 to round the value*/
        }

        private void makeTllTable()
        {
            //#define dB2(x) ((x)*2)

            double[] kltable = new double[16]{
                2*0.000, 2*9.000, 2*12.000, 2*13.875
                , 2*15.000, 2*16.125, 2*16.875, 2*17.625
                ,2*18.000, 2*18.750, 2*19.125, 2*19.500
                , 2*19.875, 2*20.250, 2*20.625, 2*21.000
            };

            int tmp;
            int fnum, block, TL, KL;

            tllTable = new uint[16][][][];
            for (fnum = 0; fnum < 16; fnum++)
            {
                tllTable[fnum] = new uint[8][][];
                for (block = 0; block < 8; block++)
                {
                    tllTable[fnum][block] = new uint[64][];
                    for (TL = 0; TL < 64; TL++)
                    {
                        tllTable[fnum][block][TL] = new uint[4];
                        for (KL = 0; KL < 4; KL++)
                        {
                            //#define TL2EG(d) ((d)*(e_int32)(TL_STEP/EG_STEP))
                            if (KL == 0)
                            {
                                tllTable[fnum][block][TL][KL] = (uint)((TL) * (int)(TL_STEP / EG_STEP));
                            }
                            else
                            {
                                tmp = (int)(kltable[fnum] - 2 * (3.000) * (7 - block));
                                if (tmp <= 0)
                                    tllTable[fnum][block][TL][KL] = (uint)((TL) * (int)(TL_STEP / EG_STEP));
                                else
                                    tllTable[fnum][block][TL][KL] = (uint)(((tmp >> (3 - KL)) / EG_STEP) + ((TL) * (int)(TL_STEP / EG_STEP)));
                            }
                        }
                    }
                }
            }
        }

        //# ifdef USE_SPEC_ENV_SPEED
        private double[][] attacktime = new double[16][] {
            new double[4]{0, 0, 0, 0},
            new double[4]{1730.15, 1400.60, 1153.43, 988.66},
            new double[4]{865.08, 700.30, 576.72, 494.33},
            new double[4]{432.54, 350.15, 288.36, 247.16},
            new double[4]{216.27, 175.07, 144.18, 123.58},
            new double[4]{108.13, 87.54, 72.09, 61.79},
            new double[4]{54.07, 43.77, 36.04, 30.90},
            new double[4]{27.03, 21.88, 18.02, 15.45},
            new double[4]{13.52, 10.94, 9.01, 7.72},
            new double[4]{6.76, 5.47, 4.51, 3.86},
            new double[4]{3.38, 2.74, 2.25, 1.93},
            new double[4]{1.69, 1.37, 1.13, 0.97},
            new double[4]{0.84, 0.70, 0.60, 0.54},
            new double[4]{0.50, 0.42, 0.34, 0.30},
            new double[4]{0.28, 0.22, 0.18, 0.14},
            new double[4]{0.00, 0.00, 0.00, 0.00}
        };

        private double[][] decaytime = new double[16][] {
            new double[4] {0, 0, 0, 0},
            new double[4] {20926.60, 16807.20, 14006.00, 12028.60},
            new double[4] {10463.30, 8403.58, 7002.98, 6014.32},
            new double[4] {5231.64, 4201.79, 3501.49, 3007.16},
            new double[4] {2615.82, 2100.89, 1750.75, 1503.58},
            new double[4] {1307.91, 1050.45, 875.37, 751.79},
            new double[4] {653.95, 525.22, 437.69, 375.90},
            new double[4] {326.98, 262.61, 218.84, 187.95},
            new double[4] {163.49, 131.31, 109.42, 93.97},
            new double[4] {81.74, 65.65, 54.71, 46.99},
            new double[4] {40.87, 32.83, 27.36, 23.49},
            new double[4] {20.44, 16.41, 13.68, 11.75},
            new double[4] {10.22, 8.21, 6.84, 5.87},
            new double[4] {5.11, 4.10, 3.42, 2.94},
            new double[4] {2.55, 2.05, 1.71, 1.47},
            new double[4] {1.27, 1.27, 1.27, 1.27}
        };
        //#endif

        /* Rate Table for Attack */
        private void makeDphaseARTable()
        {
            int AR, Rks, RM, RL;

            //# ifdef USE_SPEC_ENV_SPEED
            uint[][] attacktable = new uint[16][] {
                new uint[4], new uint[4],new uint[4],new uint[4],
                new uint[4], new uint[4],new uint[4],new uint[4],
                new uint[4], new uint[4],new uint[4],new uint[4],
                new uint[4], new uint[4],new uint[4],new uint[4]
            };

            for (RM = 0; RM < 16; RM++)
                for (RL = 0; RL < 4; RL++)
                {
                    if (RM == 0)
                        attacktable[RM][RL] = 0;
                    else if (RM == 15)
                        attacktable[RM][RL] = (uint)EG_DP_WIDTH;
                    else
                        attacktable[RM][RL] = (uint)((double)(1 << EG_DP_BITS) / (attacktime[RM][RL] * 3579545 / 72000));

                }
            //#endif

            for (AR = 0; AR < 16; AR++)
                for (Rks = 0; Rks < 16; Rks++)
                {
                    RM = AR + (Rks >> 2);
                    RL = Rks & 3;
                    if (RM > 15)
                        RM = 15;
                    switch (AR)
                    {
                        case 0:
                            dphaseARTable[AR][Rks] = 0;
                            break;
                        case 15:
                            dphaseARTable[AR][Rks] = 0;/*EG_DP_WIDTH;*/
                            break;
                        default:
                            //#ifdef USE_SPEC_ENV_SPEED
                            //#define RATE_ADJUST(x) (rate==49716?x:(e_uint32)((double)(x)*clk/72/rate + 0.5))        /* added 0.5 to round the value*/
                            dphaseARTable[AR][Rks] = (rate == 49716 ? attacktable[RM][RL] : (uint)((double)(attacktable[RM][RL]) * clk / 72 / rate + 0.5));
                            //#else
                            //dphaseARTable[AR][Rks] = RATE_ADJUST((3 * (RL + 4) << (RM + 1)));
                            //#endif
                            break;
                    }
                }
        }

        /* Rate Table for Decay and Release */
        private void makeDphaseDRTable()
        {
            int DR, Rks, RM, RL;

            //# ifdef USE_SPEC_ENV_SPEED
            uint[][] decaytable = new uint[16][] {
                new uint[4], new uint[4],new uint[4],new uint[4],
                new uint[4], new uint[4],new uint[4],new uint[4],
                new uint[4], new uint[4],new uint[4],new uint[4],
                new uint[4], new uint[4],new uint[4],new uint[4]
            };

            for (RM = 0; RM < 16; RM++)
                for (RL = 0; RL < 4; RL++)
                    if (RM == 0)
                        decaytable[RM][RL] = 0;
                    else
                        decaytable[RM][RL] = (uint)((double)(1 << EG_DP_BITS) / (decaytime[RM][RL] * 3579545 / 72000));
            //#endif

            for (DR = 0; DR < 16; DR++)
                for (Rks = 0; Rks < 16; Rks++)
                {
                    RM = DR + (Rks >> 2);
                    RL = Rks & 3;
                    if (RM > 15)
                        RM = 15;
                    switch (DR)
                    {
                        case 0:
                            dphaseDRTable[DR][Rks] = 0;
                            break;
                        default:
                            //#ifdef USE_SPEC_ENV_SPEED
                            dphaseDRTable[DR][Rks] = (rate == 49716 ? decaytable[RM][RL] : (uint)((double)(decaytable[RM][RL]) * clk / 72 / rate + 0.5)); //RATE_ADJUST(decaytable[RM][RL]);
                            //#else
                            //dphaseDRTable[DR][Rks] = RATE_ADJUST((RL + 4) << (RM - 1));
                            //#endif
                            break;
                    }
                }
        }

        private void makeRksTable()
        {

            int fnum8, block, KR;

            rksTable = new int[2][][];
            for (fnum8 = 0; fnum8 < 2; fnum8++)
            {
                rksTable[fnum8] = new int[8][];
                for (block = 0; block < 8; block++)
                {
                    rksTable[fnum8][block] = new int[2];
                    for (KR = 0; KR < 2; KR++)
                    {
                        if (KR != 0)
                            rksTable[fnum8][block][KR] = (block << 1) + fnum8;
                        else
                            rksTable[fnum8][block][KR] = block >> 1;
                    }
                }
            }
        }

        //private void OPLL_dump2patch(byte[] dump, OPLL_PATCH[] patch)
        private void OPLL_dump2patch(byte[] dump, int startAdr, OPLL_PATCH[][] patch)
        {
            patch[startAdr][0].AM = (uint)((dump[startAdr*8 + 0] >> 7) & 1);
            patch[startAdr][1].AM = (uint)((dump[startAdr*8 + 1] >> 7) & 1);
            patch[startAdr][0].PM = (uint)((dump[startAdr*8 + 0] >> 6) & 1);
            patch[startAdr][1].PM = (uint)((dump[startAdr*8 + 1] >> 6) & 1);
            patch[startAdr][0].EG = (uint)((dump[startAdr*8 + 0] >> 5) & 1);
            patch[startAdr][1].EG = (uint)((dump[startAdr*8 + 1] >> 5) & 1);
            patch[startAdr][0].KR = (uint)((dump[startAdr*8 + 0] >> 4) & 1);
            patch[startAdr][1].KR = (uint)((dump[startAdr*8 + 1] >> 4) & 1);
            patch[startAdr][0].ML = (uint)((dump[startAdr*8 + 0]) & 15);
            patch[startAdr][1].ML = (uint)((dump[startAdr*8 + 1]) & 15);
            patch[startAdr][0].KL = (uint)((dump[startAdr*8 + 2] >> 6) & 3);
            patch[startAdr][1].KL = (uint)((dump[startAdr*8 + 3] >> 6) & 3);
            patch[startAdr][0].TL = (uint)((dump[startAdr*8 + 2]) & 63);
            patch[startAdr][0].FB = (uint)((dump[startAdr*8 + 3]) & 7);
            patch[startAdr][0].WF = (uint)((dump[startAdr*8 + 3] >> 3) & 1);
            patch[startAdr][1].WF = (uint)((dump[startAdr*8 + 3] >> 4) & 1);
            patch[startAdr][0].AR = (uint)((dump[startAdr*8 + 4] >> 4) & 15);
            patch[startAdr][1].AR = (uint)((dump[startAdr*8 + 5] >> 4) & 15);
            patch[startAdr][0].DR = (uint)((dump[startAdr*8 + 4]) & 15);
            patch[startAdr][1].DR = (uint)((dump[startAdr*8 + 5]) & 15);
            patch[startAdr][0].SL = (uint)((dump[startAdr*8 + 6] >> 4) & 15);
            patch[startAdr][1].SL = (uint)((dump[startAdr*8 + 7] >> 4) & 15);
            patch[startAdr][0].RR = (uint)((dump[startAdr*8 + 6]) & 15);
            patch[startAdr][1].RR = (uint)((dump[startAdr*8 + 7]) & 15);
        }

        private void OPLL_getDefaultPatch(int num, OPLL_PATCH[][] patch)
        {
            //OPLL_dump2patch(default_inst[type][num * 8], patch);
            OPLL_dump2patch(default_inst[0], num, patch);
        }

        private void makeDefaultPatch()
        {
            int j;

                for (j = 0; j < 19; j++)
                    OPLL_getDefaultPatch(j, default_patch);

        }

        private void OPLL_setPatch(OPLL opll, byte[] dump)
        {
            OPLL_PATCH[][] patch = new OPLL_PATCH[2][];
            int i;

            for (i = 0; i < 19; i++)
            {
                OPLL_dump2patch(dump, i , patch);
                opll.patch[0][i] = patch[0][i];
                opll.patch[1][i] = patch[1][i];
            }
        }

        private void OPLL_patch2dump(OPLL_PATCH[] patch, byte[] dump)
        {
            dump[0] = (byte)((patch[0].AM << 7) + (patch[0].PM << 6) + (patch[0].EG << 5) + (patch[0].KR << 4) + patch[0].ML);
            dump[1] = (byte)((patch[1].AM << 7) + (patch[1].PM << 6) + (patch[1].EG << 5) + (patch[1].KR << 4) + patch[1].ML);
            dump[2] = (byte)((patch[0].KL << 6) + patch[0].TL);
            dump[3] = (byte)((patch[1].KL << 6) + (patch[1].WF << 4) + (patch[0].WF << 3) + patch[0].FB);
            dump[4] = (byte)((patch[0].AR << 4) + patch[0].DR);
            dump[5] = (byte)((patch[1].AR << 4) + patch[1].DR);
            dump[6] = (byte)((patch[0].SL << 4) + patch[0].RR);
            dump[7] = (byte)((patch[1].SL << 4) + patch[1].RR);
            dump[8] = 0;
            dump[9] = 0;
            dump[10] = 0;
            dump[11] = 0;
            dump[12] = 0;
            dump[13] = 0;
            dump[14] = 0;
            dump[15] = 0;
        }

        /************************************************************

                              Calc Parameters

        ************************************************************/

        private uint calc_eg_dphase(OPLL_SLOT slot)
        {

            switch (slot.eg_mode)
            {
                case (int)OPLL_EG_STATE.ATTACK:
                    return dphaseARTable[slot.patch.AR][slot.rks];

                case (int)OPLL_EG_STATE.DECAY:
                    return dphaseDRTable[slot.patch.DR][slot.rks];

                case (int)OPLL_EG_STATE.SUSHOLD:
                    return 0;

                case (int)OPLL_EG_STATE.SUSTINE:
                    return dphaseDRTable[slot.patch.RR][slot.rks];

                case (int)OPLL_EG_STATE.RELEASE:
                    if (slot.sustine != 0)
                        return dphaseDRTable[5][slot.rks];
                    else if (slot.patch.EG != 0)
                        return dphaseDRTable[slot.patch.RR][slot.rks];
                    else
                        return dphaseDRTable[7][slot.rks];

                case (int)OPLL_EG_STATE.SETTLE:
                    return dphaseDRTable[15][0];

                case (int)OPLL_EG_STATE.FINISH:
                    return 0;

                default:
                    return 0;
            }
        }

        /*************************************************************

                            OPLL internal interfaces

        *************************************************************/
        private int SLOT_BD1 = 12;
        private int SLOT_BD2 = 13;
        private int SLOT_HH = 14;
        private int SLOT_SD = 15;
        private int SLOT_TOM = 16;
        private int SLOT_CYM = 17;
        private void UPDATE_PG(OPLL_SLOT S) { S.dphase = dphaseTable[S.fnum][S.block][S.patch.ML]; }
        private void UPDATE_TLL(OPLL_SLOT S)
        {
            S.tll = (S.type == 0) ? tllTable[S.fnum >> 5][S.block][S.patch.TL][S.patch.KL] : tllTable[S.fnum >> 5][S.block][S.volume][S.patch.KL];
        }
        private void UPDATE_RKS(OPLL_SLOT S) { S.rks = (uint)rksTable[S.fnum >> 8][S.block][S.patch.KR]; }
        private void UPDATE_WF(OPLL_SLOT S) { S.sintbl = waveform[S.patch.WF]; }
        private void UPDATE_EG(OPLL_SLOT S) { S.eg_dphase = calc_eg_dphase(S); }
        private void UPDATE_ALL(OPLL_SLOT S)
        {
            UPDATE_PG(S);
            UPDATE_TLL(S);
            UPDATE_RKS(S);
            UPDATE_WF(S);
            UPDATE_EG(S);
        }/* EG should be updated last. */

        /* Slot key on  */
        private void slotOn(OPLL_SLOT slot)
        {
            slot.eg_mode = (int)OPLL_EG_STATE.ATTACK;
            slot.eg_phase = 0;
            slot.phase = 0;
            UPDATE_EG(slot);
        }

        /* Slot key on without reseting the phase */
        private void slotOn2(OPLL_SLOT slot)
        {
            slot.eg_mode = (int)OPLL_EG_STATE.ATTACK;
            slot.eg_phase = 0;
            UPDATE_EG(slot);
        }

        /* Slot key off */
        private void slotOff(OPLL_SLOT slot)
        {
            if (slot.eg_mode == (int)OPLL_EG_STATE.ATTACK)
                slot.eg_phase = (uint)EXPAND_BITS(AR_ADJUST_TABLE[HIGHBITS((int)slot.eg_phase, EG_DP_BITS - EG_BITS)], EG_BITS, EG_DP_BITS);
            slot.eg_mode = (int)OPLL_EG_STATE.RELEASE;
            UPDATE_EG(slot);
        }

        /* Channel key on */
        private void keyOn(OPLL opll, int i)
        {
            if (opll.slot_on_flag[i * 2] == 0)
                slotOn(MOD(opll, i));
            if (opll.slot_on_flag[i * 2 + 1] == 0)
                slotOn(CAR(opll, i));
            opll.key_status[i] = 1;
        }

        /* Channel key off */
        private void keyOff(OPLL opll, int i)
        {
            if (opll.slot_on_flag[i * 2 + 1] != 0)
                slotOff(CAR(opll, i));
            opll.key_status[i] = 0;
        }

        private void keyOn_BD(OPLL opll)
        {
            keyOn(opll, 6);
        }

        private void keyOn_SD(OPLL opll)
        {
            if (opll.slot_on_flag[SLOT_SD] == 0)
                slotOn(CAR(opll, 7));
        }

        private void keyOn_TOM(OPLL opll)
        {
            if (opll.slot_on_flag[SLOT_TOM] == 0)
                slotOn(MOD(opll, 8));
        }

        private void keyOn_HH(OPLL opll)
        {
            if (opll.slot_on_flag[SLOT_HH] == 0)
                slotOn2(MOD(opll, 7));
        }

        private void keyOn_CYM(OPLL opll)
        {
            if (opll.slot_on_flag[SLOT_CYM] == 0)
                slotOn2(CAR(opll, 8));
        }

        /* Drum key off */
        private void keyOff_BD(OPLL opll)
        {
            keyOff(opll, 6);
        }

        private void keyOff_SD(OPLL opll)
        {
            if (opll.slot_on_flag[SLOT_SD] != 0)
                slotOff(CAR(opll, 7));
        }

        private void keyOff_TOM(OPLL opll)
        {
            if (opll.slot_on_flag[SLOT_TOM] != 0)
                slotOff(MOD(opll, 8));
        }

        private void keyOff_HH(OPLL opll)
        {
            if (opll.slot_on_flag[SLOT_HH] != 0)
                slotOff(MOD(opll, 7));
        }

        private void keyOff_CYM(OPLL opll)
        {
            if (opll.slot_on_flag[SLOT_CYM] != 0)
                slotOff(CAR(opll, 8));
        }

        /* Change a voice */
        private void setPatch(OPLL opll, int i, int num)
        {
            opll.patch_number[i] = num;
            MOD(opll, i).patch = opll.patch[num][0];
            CAR(opll, i).patch = opll.patch[num][1];
        }

        /* Change a rhythm voice */
        private void setSlotPatch(OPLL_SLOT slot, OPLL_PATCH patch)
        {
            slot.patch = patch;
        }

        /* Set sustine parameter */
        private void setSustine(OPLL opll, int c, int sustine)
        {
            CAR(opll, c).sustine = sustine;
            if (MOD(opll, c).type != 0)
                MOD(opll, c).sustine = sustine;
        }

        /* Volume : 6bit ( Volume register << 2 ) */
        private void setVolume(OPLL opll, int c, int volume)
        {
            CAR(opll, c).volume = volume;
        }

        private void setSlotVolume(OPLL_SLOT slot, int volume)
        {
            slot.volume = volume;
        }

        /* Set F-Number ( fnum : 9bit ) */
        private void setFnumber(OPLL opll, int c, int fnum)
        {
            CAR(opll, c).fnum = fnum;
            MOD(opll, c).fnum = fnum;
        }

        /* Set Block data (block : 3bit ) */
        private void setBlock(OPLL opll, int c, int block)
        {
            CAR(opll, c).block = block;
            MOD(opll, c).block = block;
        }

        /* Change Rhythm Mode */
        private void update_rhythm_mode(OPLL opll)
        {
            if ((opll.patch_number[6] & 0x10) != 0)
            {
                if ((opll.slot_on_flag[SLOT_BD2] | (opll.reg[0x0e] & 0x20)) == 0)
                {
                    opll.slot[SLOT_BD1].eg_mode = (int)OPLL_EG_STATE.FINISH;
                    opll.slot[SLOT_BD2].eg_mode = (int)OPLL_EG_STATE.FINISH;
                    setPatch(opll, 6, opll.reg[0x36] >> 4);
                }
            }
            else if ((opll.reg[0x0e] & 0x20) != 0)
            {
                opll.patch_number[6] = 16;
                opll.slot[SLOT_BD1].eg_mode = (int)OPLL_EG_STATE.FINISH;
                opll.slot[SLOT_BD2].eg_mode = (int)OPLL_EG_STATE.FINISH;
                setSlotPatch(opll.slot[SLOT_BD1], opll.patch[16][0]);
                setSlotPatch(opll.slot[SLOT_BD2], opll.patch[16][1]);
            }

            if ((opll.patch_number[7] & 0x10) != 0)
            {
                if (!((opll.slot_on_flag[SLOT_HH] != 0 && opll.slot_on_flag[SLOT_SD] != 0) | (opll.reg[0x0e] & 0x20) != 0))
                {
                    opll.slot[SLOT_HH].type = 0;
                    opll.slot[SLOT_HH].eg_mode = (int)OPLL_EG_STATE.FINISH;
                    opll.slot[SLOT_SD].eg_mode = (int)OPLL_EG_STATE.FINISH;
                    setPatch(opll, 7, opll.reg[0x37] >> 4);
                }
            }
            else if ((opll.reg[0x0e] & 0x20) != 0)
            {
                opll.patch_number[7] = 17;
                opll.slot[SLOT_HH].type = 1;
                opll.slot[SLOT_HH].eg_mode = (int)OPLL_EG_STATE.FINISH;
                opll.slot[SLOT_SD].eg_mode = (int)OPLL_EG_STATE.FINISH;
                setSlotPatch(opll.slot[SLOT_HH], opll.patch[17][0]);
                setSlotPatch(opll.slot[SLOT_SD], opll.patch[17][1]);
            }

            if ((opll.patch_number[8] & 0x10) != 0)
            {
                if (!((opll.slot_on_flag[SLOT_CYM] != 0 && opll.slot_on_flag[SLOT_TOM] != 0) | (opll.reg[0x0e] & 0x20) != 0))
                {
                    opll.slot[SLOT_TOM].type = 0;
                    opll.slot[SLOT_TOM].eg_mode = (int)OPLL_EG_STATE.FINISH;
                    opll.slot[SLOT_CYM].eg_mode = (int)OPLL_EG_STATE.FINISH;
                    setPatch(opll, 8, opll.reg[0x38] >> 4);
                }
            }
            else if ((opll.reg[0x0e] & 0x20) != 0)
            {
                opll.patch_number[8] = 18;
                opll.slot[SLOT_TOM].type = 1;
                opll.slot[SLOT_TOM].eg_mode = (int)OPLL_EG_STATE.FINISH;
                opll.slot[SLOT_CYM].eg_mode = (int)OPLL_EG_STATE.FINISH;
                setSlotPatch(opll.slot[SLOT_TOM], opll.patch[18][0]);
                setSlotPatch(opll.slot[SLOT_CYM], opll.patch[18][1]);
            }
        }

        private void update_key_status(OPLL opll)
        {
            int ch;

            for (ch = 0; ch < 9; ch++)
                opll.slot_on_flag[ch * 2] = opll.slot_on_flag[ch * 2 + 1] = (opll.reg[0x20 + ch]) & 0x10;

            if ((opll.reg[0x0e] & 0x20) != 0)
            {
                opll.slot_on_flag[SLOT_BD1] |= (opll.reg[0x0e] & 0x10);
                opll.slot_on_flag[SLOT_BD2] |= (opll.reg[0x0e] & 0x10);
                opll.slot_on_flag[SLOT_SD] |= (opll.reg[0x0e] & 0x08);
                opll.slot_on_flag[SLOT_HH] |= (opll.reg[0x0e] & 0x01);
                opll.slot_on_flag[SLOT_TOM] |= (opll.reg[0x0e] & 0x04);
                opll.slot_on_flag[SLOT_CYM] |= (opll.reg[0x0e] & 0x02);
            }
        }

        private void OPLL_copyPatch(OPLL opll, int num, OPLL_PATCH[] patch)
        {
            //memcpy(opll.patch[num], patch, sizeof(OPLL_PATCH));
            opll.patch[num][0].AM = patch[0].AM;
            opll.patch[num][0].AR = patch[0].AR;
            opll.patch[num][0].DR = patch[0].DR;
            opll.patch[num][0].EG = patch[0].EG;
            opll.patch[num][0].FB = patch[0].FB;
            opll.patch[num][0].KL = patch[0].KL;
            opll.patch[num][0].KR = patch[0].KR;
            opll.patch[num][0].ML = patch[0].ML;
            opll.patch[num][0].PM = patch[0].PM;
            opll.patch[num][0].RR = patch[0].RR;
            opll.patch[num][0].SL = patch[0].SL;
            opll.patch[num][0].TL = patch[0].TL;
            opll.patch[num][0].WF = patch[0].WF;

            opll.patch[num][1].AM = patch[1].AM;
            opll.patch[num][1].AR = patch[1].AR;
            opll.patch[num][1].DR = patch[1].DR;
            opll.patch[num][1].EG = patch[1].EG;
            opll.patch[num][1].FB = patch[1].FB;
            opll.patch[num][1].KL = patch[1].KL;
            opll.patch[num][1].KR = patch[1].KR;
            opll.patch[num][1].ML = patch[1].ML;
            opll.patch[num][1].PM = patch[1].PM;
            opll.patch[num][1].RR = patch[1].RR;
            opll.patch[num][1].SL = patch[1].SL;
            opll.patch[num][1].TL = patch[1].TL;
            opll.patch[num][1].WF = patch[1].WF;
        }

        /***********************************************************

                              Initializing

        ***********************************************************/

        private void OPLL_SLOT_reset(OPLL_SLOT slot, int type)
        {
            slot.type = type;
            slot.sintbl = waveform[0];
            slot.phase = 0;
            slot.dphase = 0;
            slot.output[0] = 0;
            slot.output[1] = 0;
            slot.feedback = 0;
            slot.eg_mode = (int)OPLL_EG_STATE.FINISH;
            slot.eg_phase = (uint)EG_DP_WIDTH;
            slot.eg_dphase = 0;
            slot.rks = 0;
            slot.tll = 0;
            slot.sustine = 0;
            slot.fnum = 0;
            slot.block = 0;
            slot.volume = 0;
            slot.pgout = 0;
            slot.egout = 0;
            slot.patch = null_patch;
        }

        private void internal_refresh()
        {
            makeDphaseTable();
            makeDphaseARTable();
            makeDphaseDRTable();
            pm_dphase = (uint)RATE_ADJUST((uint)(PM_SPEED * PM_DP_WIDTH / (clk / 72)));
            am_dphase = (uint)RATE_ADJUST((uint)(AM_SPEED * AM_DP_WIDTH / (clk / 72)));
        }

        private void maketables(uint c, uint r)
        {
            if (c != clk)
            {
                clk = c;
                makePmTable();
                makeAmTable();
                makeDB2LinTable();
                makeAdjustTable();
                makeTllTable();
                makeRksTable();
                makeSinTable();
                makeDefaultPatch();
            }

            if (r != rate)
            {
                rate = r;
                internal_refresh();
            }
        }

        private OPLL OPLL_new(uint clk, uint rate)
        {

            OPLL opll;
            int i;

            default_patch = new OPLL_PATCH[19][];
            for (i = 0; i < 19; i++)
            {
                default_patch[i] = new OPLL_PATCH[2];
                for (int j = 0; j < 2 ; j++)
                {
                    default_patch[i][j] = new OPLL_PATCH();
                }
            }

            maketables(clk, rate);

            opll = new OPLL();

            opll.vrc7_mode = 0x00;

            for (i = 0; i < 19; i++)
            {
                opll.patch[i][0] = new OPLL_PATCH();
                opll.patch[i][1] = new OPLL_PATCH();
            }

            for (i = 0; i < 14; i++)
                centre_panning(opll.pan[i]);

            opll.mask = 0;

            OPLL_reset(opll);
            OPLL_reset_patch(opll, 0);

            return opll;
        }


        private void OPLL_delete(OPLL opll)
        {
            opll = null;
        }


        /* Reset patch datas by system default. */
        private void OPLL_reset_patch(OPLL opll, int type)
        {
            int i;

            for (i = 0; i < 19; i++)
            {
                OPLL_copyPatch(opll, i, default_patch[i]);
            }
        }

        /* Reset whole of OPLL except patch datas. */
        private void OPLL_reset(OPLL opll)
        {
            int i;

            if (opll == null)
                return;

            opll.adr = 0;
            opll._out = 0;

            opll.pm_phase = 0;
            opll.am_phase = 0;

            opll.noise_seed = 0xffff;
            //opll->mask = 0;

            for (i = 0; i < 18; i++)
            {
                opll.slot[i] = new OPLL_SLOT();
                OPLL_SLOT_reset(opll.slot[i], i % 2);
            }

            for (i = 0; i < 9; i++)
            {
                opll.key_status[i] = 0;
                setPatch(opll, i, 0);
            }

            for (i = 0; i < 0x40; i++)
                OPLL_writeReg(opll, (uint)i, 0);

            //# ifndef EMU2413_COMPACTION
            opll.realstep = (uint)((1 << 31) / rate);
            opll.opllstep = (uint)((1 << 31) / (clk / 72));
            opll.oplltime = 0;
            /*for (i = 0; i < 14; i++)
            {
                  //centre_panning( opll->pan[i] );
                  opll->pan[i][0] = 1.0f;
                  opll->pan[i][1] = 1.0f;
              }*/
            opll.sprev[0] = opll.sprev[1] = 0;
            opll.snext[0] = opll.snext[1] = 0;
            //#endif
        }

        /* Force Refresh (When external program changes some parameters). */
        private void OPLL_forceRefresh(OPLL opll)
        {
            int i;

            if (opll == null)
                return;

            for (i = 0; i < 9; i++)
                setPatch(opll, i, opll.patch_number[i]);

            for (i = 0; i < 18; i++)
            {
                UPDATE_PG(opll.slot[i]);
                UPDATE_RKS(opll.slot[i]);
                UPDATE_TLL(opll.slot[i]);
                UPDATE_WF(opll.slot[i]);
                UPDATE_EG(opll.slot[i]);
            }
        }

        private void OPLL_set_rate(OPLL opll, uint r)
        {
            if (opll.quality != 0)
                rate = 49716;
            else
                rate = r;
            internal_refresh();
            rate = r;
        }

        private void OPLL_set_quality(OPLL opll, uint q)
        {
            opll.quality = q;
            OPLL_set_rate(opll, rate);
        }

        /*********************************************************

                         Generate wave data

        *********************************************************/
        /* Convert Amp(0 to EG_HEIGHT) to Phase(0 to 2PI). */
        //#if ( SLOT_AMP_BITS - PG_BITS ) > 0
        //#define wave2_2pi(e)  ( (e) >> ( SLOT_AMP_BITS - PG_BITS ))
        //private OPLL_SLOT wave2_2pi(OPLL_SLOT e) {
        //    return e >> (SLOT_AMP_BITS - PG_BITS);
        //}
        //#else
        //#define wave2_2pi(e)  ( (e) << ( PG_BITS - SLOT_AMP_BITS ))
        private int wave2_2pi(int e)
        {
            return ((e) << (PG_BITS - SLOT_AMP_BITS));
        }
        //#endif

        /* Convert Amp(0 to EG_HEIGHT) to Phase(0 to 4PI). */
        //#if (SLOT_AMP_BITS - PG_BITS - 1 ) == 0
        //#define wave2_4pi(e)  (e)
        //#elif (SLOT_AMP_BITS - PG_BITS - 1 ) > 0
        //#define wave2_4pi(e)  ( (e) >> ( SLOT_AMP_BITS - PG_BITS - 1 ))
        //#else
        //#define wave2_4pi(e)  ( (e) << ( 1 + PG_BITS - SLOT_AMP_BITS ))
        private int wave2_4pi(int e)
        {
            return ((e) << (1 + PG_BITS - SLOT_AMP_BITS));
        }
        //#endif

        /* Convert Amp(0 to EG_HEIGHT) to Phase(0 to 8PI). */
        //#if (SLOT_AMP_BITS - PG_BITS - 2 ) == 0
        //#define wave2_8pi(e)  (e)
        //#elif (SLOT_AMP_BITS - PG_BITS - 2 ) > 0
        //#define wave2_8pi(e)  ( (e) >> ( SLOT_AMP_BITS - PG_BITS - 2 ))
        //#else
        //#define wave2_8pi(e)  ( (e) << ( 2 + PG_BITS - SLOT_AMP_BITS ))
        private int wave2_8pi(int e)
        {
            return ((e) << (2 + PG_BITS - SLOT_AMP_BITS));
        }
        //#endif

        /* Update AM, PM unit */
        private void update_ampm(OPLL opll)
        {
            opll.pm_phase = (uint)((opll.pm_phase + pm_dphase) & (PM_DP_WIDTH - 1));
            opll.am_phase = (int)((opll.am_phase + am_dphase) & (AM_DP_WIDTH - 1));
            opll.lfo_am = amtable[HIGHBITS(opll.am_phase, AM_DP_BITS - AM_PG_BITS)];
            opll.lfo_pm = pmtable[HIGHBITS((int)opll.pm_phase, PM_DP_BITS - PM_PG_BITS)];
        }

        /* PG */
        private void calc_phase(OPLL_SLOT slot, int lfo)
        {
            if (slot.patch.PM != 0)
                slot.phase += (uint)((slot.dphase * lfo) >> PM_AMP_BITS);
            else
                slot.phase += slot.dphase;

            slot.phase &= (uint)(DP_WIDTH - 1);

            slot.pgout = (uint)HIGHBITS((int)slot.phase, DP_BASE_BITS);
        }

        /* Update Noise unit */
        private void update_noise(OPLL opll)
        {
            if ((opll.noise_seed & 1) != 0) opll.noise_seed ^= 0x8003020;
            opll.noise_seed >>= 1;
        }

        private uint S2E(double x)
        {
            return (uint)(SL2EG((int)(x / SL_STEP)) << (EG_DP_BITS - EG_BITS));
        }

        /* EG */
        private void calc_envelope(OPLL_SLOT slot, int lfo)
        {
            //#define S2E(x) (SL2EG((e_int32)(x/SL_STEP))<<(EG_DP_BITS-EG_BITS))

            uint[] SL = new uint[16]{
    S2E (0.0), S2E (3.0), S2E (6.0), S2E (9.0), S2E (12.0), S2E (15.0), S2E (18.0), S2E (21.0),
    S2E (24.0), S2E (27.0), S2E (30.0), S2E (33.0), S2E (36.0), S2E (39.0), S2E (42.0), S2E (48.0)
            };

            uint egout;

            switch (slot.eg_mode)
            {
                case (int)OPLL_EG_STATE.ATTACK:
                    egout = AR_ADJUST_TABLE[HIGHBITS((int)slot.eg_phase, EG_DP_BITS - EG_BITS)];
                    slot.eg_phase += slot.eg_dphase;
                    if ((EG_DP_WIDTH & slot.eg_phase) != 0 || (slot.patch.AR == 15))
                    {
                        egout = 0;
                        slot.eg_phase = 0;
                        slot.eg_mode = (int)OPLL_EG_STATE.DECAY;
                        UPDATE_EG(slot);
                    }
                    break;

                case (int)OPLL_EG_STATE.DECAY:
                    egout = (uint)HIGHBITS((int)slot.eg_phase, EG_DP_BITS - EG_BITS);
                    slot.eg_phase += slot.eg_dphase;
                    if (slot.eg_phase >= SL[slot.patch.SL])
                    {
                        if ((slot.patch.EG) != 0)
                        {
                            slot.eg_phase = SL[slot.patch.SL];
                            slot.eg_mode = (int)OPLL_EG_STATE.SUSHOLD;
                            UPDATE_EG(slot);
                        }
                        else
                        {
                            slot.eg_phase = SL[slot.patch.SL];
                            slot.eg_mode = (int)OPLL_EG_STATE.SUSTINE;
                            UPDATE_EG(slot);
                        }
                    }
                    break;

                case (int)OPLL_EG_STATE.SUSHOLD:
                    egout = (uint)HIGHBITS((int)slot.eg_phase, EG_DP_BITS - EG_BITS);
                    if (slot.patch.EG == 0)
                    {
                        slot.eg_mode = (int)OPLL_EG_STATE.SUSTINE;
                        UPDATE_EG(slot);
                    }
                    break;

                case (int)OPLL_EG_STATE.SUSTINE:
                case (int)OPLL_EG_STATE.RELEASE:
                    egout = (uint)HIGHBITS((int)slot.eg_phase, EG_DP_BITS - EG_BITS);
                    slot.eg_phase += slot.eg_dphase;
                    if (egout >= (1 << EG_BITS))
                    {
                        slot.eg_mode = (int)OPLL_EG_STATE.FINISH;
                        egout = (1 << EG_BITS) - 1;
                    }
                    break;

                case (int)OPLL_EG_STATE.SETTLE:
                    egout = (uint)HIGHBITS((int)slot.eg_phase, EG_DP_BITS - EG_BITS);
                    slot.eg_phase += slot.eg_dphase;
                    if (egout >= (1 << EG_BITS))
                    {
                        slot.eg_mode = (int)OPLL_EG_STATE.ATTACK;
                        egout = (1 << EG_BITS) - 1;
                        UPDATE_EG(slot);
                    }
                    break;

                case (int)OPLL_EG_STATE.FINISH:
                    egout = (1 << EG_BITS) - 1;
                    break;

                default:
                    egout = (1 << EG_BITS) - 1;
                    break;
            }

            if (slot.patch.AM != 0)
                egout = (uint)(EG2DB((int)(egout + slot.tll)) + lfo);
            else
            {
                egout = (uint)EG2DB((int)(egout + slot.tll));
                //Console.WriteLine("egout {0} slot->tll {1} (e_int32)(EG_STEP/DB_STEP) {2}", egout, slot.tll, (short)(EG_STEP / DB_STEP));
            }

            if (egout >= DB_MUTE)
                egout = DB_MUTE - 1;

            slot.egout = egout | 3;
        }

        /* CARRIOR */
        private int calc_slot_car(OPLL_SLOT slot, int fm)
        {
            if (slot.egout >= (DB_MUTE - 1))
            {
                //Console.WriteLine("calc_slot_car: output over");
                slot.output[0] = 0;
            }
            else
            {
                //Console.WriteLine("calc_slot_car: slot->egout {0}", slot.egout);
                slot.output[0] = DB2LIN_TABLE[slot.sintbl[(slot.pgout + wave2_8pi(fm)) & (PG_WIDTH - 1)] + slot.egout];
            }

            slot.output[1] = (slot.output[1] + slot.output[0]) >> 1;
            return slot.output[1];
        }

        /* MODULATOR */
        private int calc_slot_mod(OPLL_SLOT slot)
        {
            int fm;

            slot.output[1] = slot.output[0];

            if (slot.egout >= (DB_MUTE - 1))
            {
                slot.output[0] = 0;
            }
            else if (slot.patch.FB != 0)
            {
                fm = wave2_4pi(slot.feedback) >> (int)(7 - slot.patch.FB);
                slot.output[0] = DB2LIN_TABLE[slot.sintbl[(slot.pgout + fm) & (PG_WIDTH - 1)] + slot.egout];
            }
            else
            {
                slot.output[0] = DB2LIN_TABLE[slot.sintbl[slot.pgout] + slot.egout];
            }

            slot.feedback = (slot.output[1] + slot.output[0]) >> 1;

            return slot.feedback;

        }

        /* TOM */
        private int calc_slot_tom(OPLL_SLOT slot)
        {
            if (slot.egout >= (DB_MUTE - 1))
                return 0;

            return DB2LIN_TABLE[slot.sintbl[slot.pgout] + slot.egout];

        }

        /* SNARE */
        private int calc_slot_snare(OPLL_SLOT slot, uint noise)
        {
            if (slot.egout >= (DB_MUTE - 1))
                return 0;

            if (BIT((int)slot.pgout, 7) != 0)
                return DB2LIN_TABLE[(noise != 0 ? DB_POS(0.0) : DB_POS(15.0)) + slot.egout];
            else
                return DB2LIN_TABLE[(noise != 0 ? DB_NEG(0.0) : DB_NEG(15.0)) + slot.egout];
        }

        /* 
          TOP-CYM 
         */
        private int calc_slot_cym(OPLL_SLOT slot, uint pgout_hh)
        {
            uint dbout;

            if (slot.egout >= (DB_MUTE - 1))
                return 0;
            else if ((
                /* the same as fmopl.c */
                ((BIT((int)pgout_hh, PG_BITS - 8) ^ BIT((int)pgout_hh, PG_BITS - 1)) | BIT((int)pgout_hh, PG_BITS - 7)) ^
               /* different from fmopl.c */
               (BIT((int)slot.pgout, PG_BITS - 7) & ~BIT((int)slot.pgout, PG_BITS - 5))) != 0
              )
                dbout = DB_NEG(3.0);
            else
                dbout = DB_POS(3.0);

            return DB2LIN_TABLE[dbout + slot.egout];
        }

        /* 
          HI-HAT 
        */
        private int calc_slot_hat(OPLL_SLOT slot, int pgout_cym, uint noise)
        {
            uint dbout;

            if (slot.egout >= (DB_MUTE - 1))
                return 0;
            else if ((
                /* the same as fmopl.c */
                ((BIT((int)slot.pgout, PG_BITS - 8) ^ BIT((int)slot.pgout, PG_BITS - 1)) | BIT((int)slot.pgout, PG_BITS - 7)) ^
                /* different from fmopl.c */
                (BIT(pgout_cym, PG_BITS - 7) & ~BIT(pgout_cym, PG_BITS - 5))) != 0
              )
            {
                if (noise != 0)
                    dbout = DB_NEG(12.0);
                else
                    dbout = DB_NEG(24.0);
            }
            else
            {
                if (noise != 0)
                    dbout = DB_POS(12.0);
                else
                    dbout = DB_POS(24.0);
            }

            return DB2LIN_TABLE[dbout + slot.egout];
        }

        private short calc(OPLL opll)
        {
            int inst = 0, perc = 0, _out = 0;
            int i;

            update_ampm(opll);
            update_noise(opll);

            for (i = 0; i < 18; i++)
            {
                calc_phase(opll.slot[i], opll.lfo_pm);
                calc_envelope(opll.slot[i], opll.lfo_am);
            }

            for (i = 0; i < 6; i++)
                if ((opll.mask & OPLL_MASK_CH(i))==0 && (CAR(opll, i).eg_mode != (int)OPLL_EG_STATE.FINISH))
                    inst += calc_slot_car(CAR(opll, i), calc_slot_mod(MOD(opll, i)));

            if (opll.vrc7_mode == 0)
            {
                /* CH6 */
                if (opll.patch_number[6] <= 15)
                {
                    if ((opll.mask & OPLL_MASK_CH(6))==0 && (CAR(opll, 6).eg_mode != (int)OPLL_EG_STATE.FINISH))
                        inst += calc_slot_car(CAR(opll, 6), calc_slot_mod(MOD(opll, 6)));
                }
                else
                {
                    if ((opll.mask & OPLL_MASK_BD)==0 && (CAR(opll, 6).eg_mode != (int)OPLL_EG_STATE.FINISH))
                        perc += calc_slot_car(CAR(opll, 6), calc_slot_mod(MOD(opll, 6)));
                }

                /* CH7 */
                if (opll.patch_number[7] <= 15)
                {
                    if ((opll.mask & OPLL_MASK_CH(7))==0 && (CAR(opll, 7).eg_mode != (int)OPLL_EG_STATE.FINISH))
                        inst += calc_slot_car(CAR(opll, 7), calc_slot_mod(MOD(opll, 7)));
                }
                else
                {
                    if ((opll.mask & OPLL_MASK_HH)==0 && (MOD(opll, 7).eg_mode != (int)OPLL_EG_STATE.FINISH))
                        perc += calc_slot_hat(MOD(opll, 7), (int)CAR(opll, 8).pgout, opll.noise_seed & 1);
                    if ((opll.mask & OPLL_MASK_SD)==0 && (CAR(opll, 7).eg_mode != (int)OPLL_EG_STATE.FINISH))
                        perc -= calc_slot_snare(CAR(opll, 7), opll.noise_seed & 1);
                }

                /* CH8 */
                if (opll.patch_number[8] <= 15)
                {
                    if ((opll.mask & OPLL_MASK_CH(8))==0 && (CAR(opll, 8).eg_mode != (int)OPLL_EG_STATE.FINISH))
                        inst += calc_slot_car(CAR(opll, 8), calc_slot_mod(MOD(opll, 8)));
                }
                else
                {
                    if ((opll.mask & OPLL_MASK_TOM)==0 && (MOD(opll, 8).eg_mode != (int)OPLL_EG_STATE.FINISH))
                        perc += calc_slot_tom(MOD(opll, 8));
                    if ((opll.mask & OPLL_MASK_CYM)==0 && (CAR(opll, 8).eg_mode != (int)OPLL_EG_STATE.FINISH))
                        perc -= calc_slot_cym(CAR(opll, 8), MOD(opll, 7).pgout);
                }
            } // end if (! opll.vrc7_mode)

            _out = inst + (perc << 1);
            return (short)_out;
        }

        //#ifdef EMU2413_COMPACTION
        private short OPLL_calc(OPLL opll)
        {
            return calc(opll);
        }
        //#else
        private short OPLL_calc_(OPLL opll)
        {
            if (opll.quality == 0)
                return calc(opll);

            while (opll.realstep > opll.oplltime)
            {
                opll.oplltime += opll.opllstep;
                opll.prev = opll.next;
                opll.next = calc(opll);
            }

            opll.oplltime -= opll.realstep;
            opll._out = (short)(((double)opll.next * (opll.opllstep - opll.oplltime)
                                    + (double)opll.prev * opll.oplltime) / opll.opllstep);

            return (short)opll._out;
        }
        //#endif

        /*e_uint32
        OPLL_setMask (OPLL * opll, e_uint32 mask)
        {
          e_uint32 ret;

          if (opll)
          {
            ret = opll->mask;
            opll->mask = mask;
            return ret;
          }
          else
            return 0;
        }*/

        private void OPLL_SetMuteMask(OPLL opll, uint MuteMask)
        {
            byte CurChn;
            uint ChnMsk;

            for (CurChn = 0; CurChn < 14; CurChn++)
            {
                if (CurChn < 9)
                {
                    ChnMsk = (uint)OPLL_MASK_CH(CurChn);
                }
                else
                {
                    switch (CurChn)
                    {
                        case 9:
                            ChnMsk = (uint)OPLL_MASK_BD;
                            break;
                        case 10:
                            ChnMsk = (uint)OPLL_MASK_SD;
                            break;
                        case 11:
                            ChnMsk = (uint)OPLL_MASK_TOM;
                            break;
                        case 12:
                            ChnMsk = (uint)OPLL_MASK_CYM;
                            break;
                        case 13:
                            ChnMsk = (uint)OPLL_MASK_HH;
                            break;
                        default:
                            ChnMsk = 0;
                            break;
                    }
                }
                if (((MuteMask >> CurChn) & 0x01) != 0)
                    opll.mask |= ChnMsk;
                else
                    opll.mask &= ~ChnMsk;
            }

            return;
        }

        /*e_uint32
        OPLL_toggleMask (OPLL * opll, e_uint32 mask)
        {
          e_uint32 ret;

          if (opll)
          {
            ret = opll->mask;
            opll->mask ^= mask;
            return ret;
          }
          else
            return 0;
        }*/

        private void OPLL_SetChipMode(OPLL opll, byte Mode)
        {
            // Enable/Disable VRC7 Mode (with only 6 instead of 9 channels and no rhythm part)
            opll.vrc7_mode = Mode;

            return;
        }

        /****************************************************

                               I/O Ctrl

        *****************************************************/

        private void OPLL_writeReg(OPLL opll, uint reg, uint data)
        {
            //Console.WriteLine("OPLL_writeReg:reg:{0}:data:{1}", reg,data);

            int i, v, ch;

            data = data & 0xff;
            reg = reg & 0x3f;
            opll.reg[reg] = (byte)data;

            switch (reg)
            {
                case 0x00:
                    opll.patch[0][0].AM = (data >> 7) & 1;
                    opll.patch[0][0].PM = (data >> 6) & 1;
                    opll.patch[0][0].EG = (data >> 5) & 1;
                    opll.patch[0][0].KR = (data >> 4) & 1;
                    opll.patch[0][0].ML = (data) & 15;
                    for (i = 0; i < 9; i++)
                    {
                        if (opll.patch_number[i] == 0)
                        {
                            UPDATE_PG(MOD(opll, i));
                            UPDATE_RKS(MOD(opll, i));
                            UPDATE_EG(MOD(opll, i));
                        }
                    }
                    break;

                case 0x01:
                    opll.patch[0][1].AM = (data >> 7) & 1;
                    opll.patch[0][1].PM = (data >> 6) & 1;
                    opll.patch[0][1].EG = (data >> 5) & 1;
                    opll.patch[0][1].KR = (data >> 4) & 1;
                    opll.patch[0][1].ML = (data) & 15;
                    for (i = 0; i < 9; i++)
                    {
                        if (opll.patch_number[i] == 0)
                        {
                            UPDATE_PG(CAR(opll, i));
                            UPDATE_RKS(CAR(opll, i));
                            UPDATE_EG(CAR(opll, i));
                        }
                    }
                    break;

                case 0x02:
                    opll.patch[0][0].KL = (data >> 6) & 3;
                    opll.patch[0][0].TL = (data) & 63;
                    for (i = 0; i < 9; i++)
                    {
                        if (opll.patch_number[i] == 0)
                        {
                            UPDATE_TLL(MOD(opll, i));
                        }
                    }
                    break;

                case 0x03:
                    opll.patch[0][1].KL = (data >> 6) & 3;
                    opll.patch[0][1].WF = (data >> 4) & 1;
                    opll.patch[0][0].WF = (data >> 3) & 1;
                    opll.patch[0][0].FB = (data) & 7;
                    for (i = 0; i < 9; i++)
                    {
                        if (opll.patch_number[i] == 0)
                        {
                            UPDATE_WF(MOD(opll, i));
                            UPDATE_WF(CAR(opll, i));
                        }
                    }
                    break;

                case 0x04:
                    opll.patch[0][0].AR = (data >> 4) & 15;
                    opll.patch[0][0].DR = (data) & 15;
                    for (i = 0; i < 9; i++)
                    {
                        if (opll.patch_number[i] == 0)
                        {
                            UPDATE_EG(MOD(opll, i));
                        }
                    }
                    break;

                case 0x05:
                    opll.patch[0][1].AR = (data >> 4) & 15;
                    opll.patch[0][1].DR = (data) & 15;
                    for (i = 0; i < 9; i++)
                    {
                        if (opll.patch_number[i] == 0)
                        {
                            UPDATE_EG(CAR(opll, i));
                        }
                    }
                    break;

                case 0x06:
                    opll.patch[0][0].SL = (data >> 4) & 15;
                    opll.patch[0][0].RR = (data) & 15;
                    for (i = 0; i < 9; i++)
                    {
                        if (opll.patch_number[i] == 0)
                        {
                            UPDATE_EG(MOD(opll, i));
                        }
                    }
                    break;

                case 0x07:
                    opll.patch[0][1].SL = (data >> 4) & 15;
                    opll.patch[0][1].RR = (data) & 15;
                    for (i = 0; i < 9; i++)
                    {
                        if (opll.patch_number[i] == 0)
                        {
                            UPDATE_EG(CAR(opll, i));
                        }
                    }
                    break;

                case 0x0e:
                    if (opll.vrc7_mode != 0)
                        break;
                    update_rhythm_mode(opll);
                    if ((data & 0x20) != 0)
                    {
                        if ((data & 0x10) != 0)
                            keyOn_BD(opll);
                        else
                            keyOff_BD(opll);
                        if ((data & 0x8) != 0)
                            keyOn_SD(opll);
                        else
                            keyOff_SD(opll);
                        if ((data & 0x4) != 0)
                            keyOn_TOM(opll);
                        else
                            keyOff_TOM(opll);
                        if ((data & 0x2) != 0)
                            keyOn_CYM(opll);
                        else
                            keyOff_CYM(opll);
                        if ((data & 0x1) != 0)
                            keyOn_HH(opll);
                        else
                            keyOff_HH(opll);
                    }
                    update_key_status(opll);

                    UPDATE_ALL(MOD(opll, 6));
                    UPDATE_ALL(CAR(opll, 6));
                    UPDATE_ALL(MOD(opll, 7));
                    UPDATE_ALL(CAR(opll, 7));
                    UPDATE_ALL(MOD(opll, 8));
                    UPDATE_ALL(CAR(opll, 8));

                    break;

                case 0x0f:
                    break;

                case 0x10:
                case 0x11:
                case 0x12:
                case 0x13:
                case 0x14:
                case 0x15:
                case 0x16:
                case 0x17:
                case 0x18:
                    ch = (int)(reg - 0x10);
                    if (opll.vrc7_mode != 0 && ch >= 6)
                        break;
                    setFnumber(opll, ch, (int)(data + ((opll.reg[0x20 + ch] & 1) << 8)));
                    UPDATE_ALL(MOD(opll, ch));
                    UPDATE_ALL(CAR(opll, ch));
                    break;

                case 0x20:
                case 0x21:
                case 0x22:
                case 0x23:
                case 0x24:
                case 0x25:
                case 0x26:
                case 0x27:
                case 0x28:
                    ch = (int)(reg - 0x20);
                    if (opll.vrc7_mode != 0 && ch >= 6)
                        break;
                    setFnumber(opll, ch, (int)(((data & 1) << 8) + opll.reg[0x10 + ch]));
                    setBlock(opll, ch, (int)((data >> 1) & 7));
                    setSustine(opll, ch, (int)((data >> 5) & 1));
                    if (ch < 0x06 || (opll.reg[0x0E] & 0x20) == 0)
                    {
                        // Valley Bell Fix: prevent commands 0x26-0x28 from turning
                        // the drums (BD, SD, CYM) off
                        if ((data & 0x10) != 0)
                            keyOn(opll, ch);
                        else
                            keyOff(opll, ch);
                    }
                    UPDATE_ALL(MOD(opll, ch));
                    UPDATE_ALL(CAR(opll, ch));
                    update_key_status(opll);
                    update_rhythm_mode(opll);
                    break;

                case 0x30:
                case 0x31:
                case 0x32:
                case 0x33:
                case 0x34:
                case 0x35:
                case 0x36:
                case 0x37:
                case 0x38:
                    if (opll.vrc7_mode != 0 && reg >= 0x36)
                        break;
                    i = (int)((data >> 4) & 15);
                    v = (int)(data & 15);
                    if ((opll.reg[0x0e] & 0x20) != 0 && (reg >= 0x36))
                    {
                        switch (reg)
                        {
                            case 0x37:
                                setSlotVolume(MOD(opll, 7), i << 2);
                                break;
                            case 0x38:
                                setSlotVolume(MOD(opll, 8), i << 2);
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        setPatch(opll, (int)(reg - 0x30), i);
                    }
                    setVolume(opll, (int)(reg - 0x30), v << 2);
                    UPDATE_ALL(MOD(opll, (int)(reg - 0x30)));
                    UPDATE_ALL(CAR(opll, (int)(reg - 0x30)));
                    break;

                default:
                    break;

            }
        }

        private void OPLL_writeIO(OPLL opll, uint adr, uint val)
        {
            if ((adr & 1) != 0)
                OPLL_writeReg(opll, opll.adr, val);
            else
                opll.adr = (byte)val;
        }

        //#ifndef EMU2413_COMPACTION
        /* STEREO MODE (OPT) */
        private void OPLL_set_pan(OPLL opll, uint ch, int pan)
        {
            uint fnl_ch;

            if (ch >= 14)
                return;

            if (ch < 9)
                fnl_ch = ch;
            else
                fnl_ch = 13 - (ch - 9);
            calc_panning(opll.pan[fnl_ch], pan); // Maxim
        }

        private void calc_stereo(OPLL opll, int[] _out)//[2]
        {
            /* Maxim: added stereo control (multiply each side by a float in opll->pan[ch][side]) */
            int l = 0, r = 0;
            /*  e_int32 b[4] = { 0, 0, 0, 0 };        /* Ignore, Right, Left, Center */
            /*  e_int32 r[4] = { 0, 0, 0, 0 };        /* Ignore, Right, Left, Center */
            int i;
            int channel;

            update_ampm(opll);
            update_noise(opll);

            for (i = 0; i < 18; i++)
            {
                calc_phase(opll.slot[i], opll.lfo_pm);
                calc_envelope(opll.slot[i], opll.lfo_am);
            }

            for (i = 0; i < 6; i++)
                if ((opll.mask & OPLL_MASK_CH(i))==0 && (CAR(opll, i).eg_mode != (int)OPLL_EG_STATE.FINISH))
                {
                    channel = calc_slot_car(CAR(opll, i), calc_slot_mod(MOD(opll, i)));
                    if (opll.pan[i][0] == 1.0f)
                    {
                        l += channel;
                        r += channel;
                    }
                    else
                    {
                        l += (int)(channel * opll.pan[i][0]);
                        r += (int)(channel * opll.pan[i][1]);
                    }
                }


            if (opll.vrc7_mode == 0)
            {
                if (opll.patch_number[6] <= 15)
                {
                    if ((opll.mask & OPLL_MASK_CH(6))==0 && (CAR(opll, 6).eg_mode != (int)OPLL_EG_STATE.FINISH))
                    {
                        channel = calc_slot_car(CAR(opll, 6), calc_slot_mod(MOD(opll, 6)));
                        if (opll.pan[6][0] == 1.0f)
                        {
                            l += channel;
                            r += channel;
                        }
                        else
                        {
                            l += (int)(channel * opll.pan[6][0]);
                            r += (int)(channel * opll.pan[6][1]);
                        }

                    }
                }
                else
                {
                    if ((opll.mask & OPLL_MASK_BD)==0 && (CAR(opll, 6).eg_mode != (int)OPLL_EG_STATE.FINISH))
                    {
                        channel = calc_slot_car(CAR(opll, 6), calc_slot_mod(MOD(opll, 6))) << 1;
                        if (opll.pan[9][0] == 1.0f)
                        {
                            l += channel;
                            r += channel;
                        }
                        else
                        {
                            l += (int)(channel * opll.pan[9][0]);
                            r += (int)(channel * opll.pan[9][1]);
                        }
                    }
                }

                if (opll.patch_number[7] <= 15)
                {
                    if ((opll.mask & OPLL_MASK_CH(7))==0 && (CAR(opll, 7).eg_mode != (int)OPLL_EG_STATE.FINISH))
                    {
                        channel = calc_slot_car(CAR(opll, 7), calc_slot_mod(MOD(opll, 7)));
                        if (opll.pan[7][0] == 1.0f)
                        {
                            l += channel;
                            r += channel;
                        }
                        else
                        {
                            l += (int)(channel * opll.pan[7][0]);
                            r += (int)(channel * opll.pan[7][1]);
                        }
                    }
                }
                else
                {
                    if ((opll.mask & OPLL_MASK_HH)==0 && (MOD(opll, 7).eg_mode != (int)OPLL_EG_STATE.FINISH))
                    {
                        channel = calc_slot_hat(MOD(opll, 7), (int)CAR(opll, 8).pgout, opll.noise_seed & 1) << 1;
                        if (opll.pan[10][0] == 1.0f)
                        {
                            l += channel;
                            r += channel;
                        }
                        else
                        {
                            l += (int)(channel * opll.pan[10][0]);
                            r += (int)(channel * opll.pan[10][1]);
                        }
                    }
                    if ((opll.mask & OPLL_MASK_SD)==0 && (CAR(opll, 7).eg_mode != (int)OPLL_EG_STATE.FINISH))
                    {
                        channel = -(calc_slot_snare(CAR(opll, 7), opll.noise_seed & 1) << 1); // this one is negated
                        if (opll.pan[11][0] == 1.0f)
                        {
                            l += channel;
                            r += channel;
                        }
                        else
                        {
                            l += (int)(channel * opll.pan[11][0]);
                            r += (int)(channel * opll.pan[11][1]);
                        }
                    }
                }

                if (opll.patch_number[8] <= 15)
                {
                    if ((opll.mask & OPLL_MASK_CH(8))==0 && (CAR(opll, 8).eg_mode != (int)OPLL_EG_STATE.FINISH))
                    {
                        channel = calc_slot_car(CAR(opll, 8), calc_slot_mod(MOD(opll, 8)));
                        if (opll.pan[8][0] == 1.0f)
                        {
                            l += channel;
                            r += channel;
                        }
                        else
                        {
                            l += (int)(channel * opll.pan[8][0]);
                            r += (int)(channel * opll.pan[8][1]);
                        }
                    }
                }
                else
                {
                    if ((opll.mask & OPLL_MASK_TOM)==0 && (MOD(opll, 8).eg_mode != (int)OPLL_EG_STATE.FINISH))
                    {
                        channel = calc_slot_tom(MOD(opll, 8)) << 1;
                        if (opll.pan[12][0] == 1.0f)
                        {
                            l += channel;
                            r += channel;
                        }
                        else
                        {
                            l += (int)(channel * opll.pan[12][0]);
                            r += (int)(channel * opll.pan[12][1]);
                        }
                    }
                    if ((opll.mask & OPLL_MASK_CYM)==0 && (CAR(opll, 8).eg_mode != (int)OPLL_EG_STATE.FINISH))
                    {
                        channel = -(calc_slot_cym(CAR(opll, 8), MOD(opll, 7).pgout) << 1); // negated
                        if (opll.pan[13][0] == 1.0f)
                        {
                            l += channel;
                            r += channel;
                        }
                        else
                        {
                            l += (int)(channel * opll.pan[13][0]);
                            r += (int)(channel * opll.pan[13][1]);
                        }
                    }
                }
            } // end if (! opll->vrc7_mode)
              /*
                out[1] = (b[1] + b[3] + ((r[1] + r[3]) << 1)) <<3;
                out[0] = (b[2] + b[3] + ((r[2] + r[3]) << 1)) <<3;
                */
            _out[0] = l << 3;
            _out[1] = r << 3;
        }

        /*void
        OPLL_calc_stereo (OPLL * opll, e_int32 out[2], samples)
        {
          if (!opll->quality)
          {
            calc_stereo (opll, out);
            return;
          }

          while (opll->realstep > opll->oplltime)
          {
            opll->oplltime += opll->opllstep;
            opll->sprev[0] = opll->snext[0];
            opll->sprev[1] = opll->snext[1];
            calc_stereo (opll, opll->snext);
          }

          opll->oplltime -= opll->realstep;
          out[0] = (e_int16) (((double) opll->snext[0] * (opll->opllstep - opll->oplltime)
                               + (double) opll->sprev[0] * opll->oplltime) / opll->opllstep);
          out[1] = (e_int16) (((double) opll->snext[1] * (opll->opllstep - opll->oplltime)
                               + (double) opll->sprev[1] * opll->oplltime) / opll->opllstep);
        }*/
        private void OPLL_calc_stereo(OPLL opll, int[][] _out, int samples)
        {
            int[] bufMO = _out[0];
            int[] bufRO = _out[1];
            int[] buffers = new int[2];

            int i;

            for (i = 0; i < samples; i++)
            {
                if (opll.quality == 0)
                {
                    calc_stereo(opll, buffers);
                    bufMO[i] = buffers[0];
                    bufRO[i] = buffers[1];
                }
                else
                {
                    while (opll.realstep > opll.oplltime)
                    {
                        opll.oplltime += opll.opllstep;
                        opll.sprev[0] = opll.snext[0];
                        opll.sprev[1] = opll.snext[1];
                        calc_stereo(opll, opll.snext);
                    }

                    opll.oplltime -= opll.realstep;
                    bufMO[i] = (int)(((double)opll.snext[0] * (opll.opllstep - opll.oplltime)
                                         + (double)opll.sprev[0] * opll.oplltime) / opll.opllstep);
                    bufRO[i] = (int)(((double)opll.snext[1] * (opll.opllstep - opll.oplltime)
                                         + (double)opll.sprev[1] * opll.oplltime) / opll.opllstep);
                }
                bufMO[i] <<= 1;
                bufRO[i] <<= 1;
                //Console.WriteLine("OPLL_calc_stereo:out[0][{0}]:{1}:out[1][{0}]:{2}:samples:{3}", i, _out[0][i], _out[1][i], samples);
            }

        }
        //#endif /* EMU2413_COMPACTION */

        private double SQRT2 = 1.414213562;
        private int RANGE = 512;
        private void calc_panning(float[] channels, int position)
        {
            if (position > RANGE / 2)
                position = RANGE / 2;
            else if (position < -RANGE / 2)
                position = -RANGE / 2;
            position += RANGE / 2;  // make -256..0..256 -> 0..256..512

            // Equal power law: equation is
            // right = sin( position / range * pi / 2) * sqrt( 2 )
            // left is equivalent to right with position = range - position
            // position is in the range 0 .. RANGE
            // RANGE / 2 = centre, result = 1.0f
            channels[1] = (float)(Math.Sin((double)position / RANGE * PI / 2) * SQRT2);
            position = RANGE - position;
            channels[0] = (float)(Math.Sin((double)position / RANGE * PI / 2) * SQRT2);
        }

        //-----------------------------------------------------------------
        // Reset the panning values to the centre position
        //-----------------------------------------------------------------
        private void centre_panning(float[] channels)
        {
            channels[0] = channels[1] = 1.0f;
        }


        public override void Reset(byte ChipID)
        {
            OPLL_reset(opll_[ChipID]);
        }

        private const uint DefaultYM2413ClockValue = 3579545;

        public override uint Start(byte ChipID, uint SamplingRate)
        {
            opll_[ChipID] = OPLL_new(DefaultYM2413ClockValue, SamplingRate);
            //OPLL_set_quality(opll_[ChipID], 1);
            OPLL_set_quality(opll_[ChipID], 0);
            return SamplingRate;
        }

        private OPLL[] opll_ = new OPLL[2];

        public uint Start(byte ChipID, uint SamplingRate, uint FMClockValue, params object[] Option)
        {
            opll_[ChipID] = OPLL_new(FMClockValue, SamplingRate);
            //OPLL_set_quality(opll_[ChipID], 1);
            OPLL_set_quality(opll_[ChipID], 0);
            return SamplingRate;
        }

        public override void Stop(byte ChipID)
        {
            opll_[ChipID] = null;
        }

        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {
            //sw.Reset();
            //sw.Start();

            OPLL_calc_stereo(opll_[ChipID], outputs, samples);

            //Console.WriteLine(sw.Elapsed);

        }

        public void YM2413_Write(byte ChipID, byte Adr, byte Data)
        {
            OPLL_writeReg(opll_[ChipID], Adr, Data);
        }
    }
}
