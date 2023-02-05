using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;

namespace MDSound
{
    public class emu2413:Instrument
    {

        //emu2413.h
        //# ifndef _EMU2413_H_
        //#define _EMU2413_H_

        //# include <stdint.h>

        //# ifdef __cplusplus
        //        extern "C" {
        //#endif

        private const int OPLL_DEBUG = 0;

        private enum OPLL_TONE_ENUM {
            OPLL_2413_TONE = 0,
            OPLL_VRC7_TONE = 1,
            OPLL_281B_TONE = 2
        };

        /* voice data */
        private class OPLL_PATCH
        {
            public uint TL, FB, EG, ML, AR, DR, SL, RR, KR, KL, AM, PM, WS;
        }
        //        OPLL_PATCH;

        ///* slot */
        private class OPLL_SLOT
        {
            public byte number;

            /* type flags:
             * 000000SM
             *       |+-- M: 0:modulator 1:carrier
             *       +--- S: 0:normal 1:single slot mode (sd, tom, hh or cym)
             */
            public byte type;

            public OPLL_PATCH patch; /* voice parameter */

            /* slot output */
            public int[] output = new int[2]; /* output value, latest and previous. */

            /* phase generator (pg) */
            public ushort[] wave_table; /* wave table */
            public uint pg_phase;    /* pg phase */
            public uint pg_out;      /* pg output, as index of wave table */
            public byte pg_keep;      /* if 1, pg_phase is preserved when key-on */
            public ushort blk_fnum;    /* (block << 9) | f-number */
            public ushort fnum;        /* f-number (9 bits) */
            public byte blk;          /* block (3 bits) */

            /* envelope generator (eg) */
            public byte eg_state;  /* current state */
            public int volume;    /* current volume */
            public byte key_flag;  /* key-on flag 1:on 0:off */
            public byte sus_flag;  /* key-sus option 1:on 0:off */
            public ushort tll;      /* total level + key scale level*/
            public byte rks;       /* key scale offset (rks) for eg speed */
            public byte eg_rate_h; /* eg speed rate high 4bits */
            public byte eg_rate_l; /* eg speed rate low 2bits */
            public uint eg_shift; /* shift for eg global counter, controls envelope speed */
            public uint eg_out;   /* eg output */

            public uint update_requests; /* flags to debounce update */

            //#if OPLL_DEBUG
            public byte last_eg_state;
            //#endif
        }
        //        OPLL_SLOT;

        ///* mask */
        private int OPLL_MASK_CH(int x) { return (1 << (x)); }
        private const int OPLL_MASK_HH = (1 << (9));
        private const int OPLL_MASK_CYM = (1 << (10));
        private const int OPLL_MASK_TOM = (1 << (11));
        private const int OPLL_MASK_SD = (1 << (12));
        private const int OPLL_MASK_BD = (1 << (13));
        private const int OPLL_MASK_RHYTHM = (OPLL_MASK_HH | OPLL_MASK_CYM | OPLL_MASK_TOM | OPLL_MASK_SD | OPLL_MASK_BD);

        /* rate conveter */
        private class OPLL_RateConv
        {
            public int ch;
            public double timer;
            public double f_ratio;
            public short[] sinc_table;
            public short[][] buf;
        }
        //OPLL_RateConv;

        //private delegate OPLL_RateConv[] OPLL_RateConv_new(double f_inp, double f_out, int ch);
        //private delegate void OPLL_RateConv_reset(OPLL_RateConv[] conv);
        //private delegate void OPLL_RateConv_putData(OPLL_RateConv[] conv, int ch, short data);
        //private delegate short OPLL_RateConv_getData(OPLL_RateConv[] conv, int ch);
        //private delegate void OPLL_RateConv_delete(OPLL_RateConv[] conv);

        private class OPLL
        {
            public uint clk;
            public uint rate;

            public byte chip_type;

            public uint adr;

            public double inp_step;
            public double out_step;
            public double out_time;

            public byte[] reg = new byte[0x40];
            public byte test_flag;
            public uint slot_key_status;
            public byte rhythm_mode;

            public uint eg_counter;

            public uint pm_phase;
            public int am_phase;

            public byte lfo_am;

            public uint noise;
            public byte short_noise;

            public int[] patch_number = new int[9];
            public OPLL_SLOT[] slot = new OPLL_SLOT[18];
            public OPLL_PATCH[][] patch = new OPLL_PATCH[19][] {
                new OPLL_PATCH[2], new OPLL_PATCH[2], new OPLL_PATCH[2], new OPLL_PATCH[2]
                , new OPLL_PATCH[2], new OPLL_PATCH[2], new OPLL_PATCH[2], new OPLL_PATCH[2]
                , new OPLL_PATCH[2], new OPLL_PATCH[2], new OPLL_PATCH[2], new OPLL_PATCH[2]
                , new OPLL_PATCH[2], new OPLL_PATCH[2], new OPLL_PATCH[2], new OPLL_PATCH[2]
                , new OPLL_PATCH[2], new OPLL_PATCH[2], new OPLL_PATCH[2]
            };

            public byte[] pan = new byte[16];
            public float[][] pan_fine = new float[16][] {
                new float[2] , new float[2] , new float[2] , new float[2] ,
            new float[2] ,new float[2] ,new float[2] ,new float[2] ,
            new float[2] ,new float[2] ,new float[2] ,new float[2] ,
            new float[2] ,new float[2] ,new float[2] ,new float[2] };

            public uint mask;

            /* channel output */
            /* 0..8:tone 9:bd 10:hh 11:sd 12:tom 13:cym */
            public short[] ch_out = new short[14];

            public short[] mix_out = new short[2];

            public OPLL_RateConv conv;

            public byte panCh=0;
        }
        //        OPLL;

        //OPLL OPLL_new(uint clk, uint rate);
        //void OPLL_delete(OPLL);

        //void OPLL_reset(OPLL);
        //void OPLL_resetPatch(OPLL, byte);

        //        /**
        //         * Set output wave sampling rate.
        //         * @param rate sampling rate. If clock / 72 (typically 49716 or 49715 at 3.58MHz) is set, the internal rate converter is
        //         * disabled.
        //         */
        //        void OPLL_setRate(OPLL opll, uint rate);

        //        /**
        //         * Set internal calcuration quality. Currently no effects, just for compatibility.
        //         * >= v1.0.0 always synthesizes internal output at clock/72 Hz.
        //         */
        //        void OPLL_setQuality(OPLL opll, byte q);

        //        /**
        //         * Set pan pot (extra function - not YM2413 chip feature)
        //         * @param ch 0..8:tone 9:bd 10:hh 11:sd 12:tom 13:cym 14,15:reserved
        //         * @param pan 0:mute 1:right 2:left 3:center
        //         * ```
        //         * pan: 76543210
        //         *            |+- bit 1: enable Left output
        //         *            +-- bit 0: enable Right output
        //         * ```
        //         */
        //        void OPLL_setPan(OPLL opll, uint ch, byte pan);

        //        /**
        //         * Set fine-grained panning
        //         * @param ch 0..8:tone 9:bd 10:hh 11:sd 12:tom 13:cym 14,15:reserved
        //         * @param pan output strength of left/right channel.
        //         *            pan[0]: left, pan[1]: right. pan[0]=pan[1]=1.0f for center.
        //         */
        //        void OPLL_setPanFine(OPLL opll, uint ch, float pan[2]);

        //        /**
        //         * Set chip type. If vrc7 is selected, r#14 is ignored.
        //         * This method not change the current ROM patch set.
        //         * To change ROM patch set, use OPLL_resetPatch.
        //         * @param type 0:YM2413 1:VRC7
        //         */
        //        void OPLL_setChipType(OPLL opll, byte type);

        //        void OPLL_writeIO(OPLL opll, uint reg, byte val);
        //        void OPLL_writeReg(OPLL opll, uint reg, byte val);

        //        /**
        //         * Calculate one sample
        //         */
        //        short OPLL_calc(OPLL opll);

        //        /**
        //         * Calulate stereo sample
        //         */
        //        void OPLL_calcStereo(OPLL opll, int out[2]);

        //        void OPLL_setPatch(OPLL, const byte* dump);
        //        void OPLL_copyPatch(OPLL, int, OPLL_PATCH*);

        //        /**
        //         * Force to refresh.
        //         * External program should call this function after updating patch parameters.
        //         */
        //        void OPLL_forceRefresh(OPLL);

        //        void OPLL_dumpToPatch(const byte* dump, OPLL_PATCH *patch);
        //void OPLL_patchToDump(const OPLL_PATCH* patch, byte *dump);
        //void OPLL_getDefaultPatch(int type, int num, OPLL_PATCH*);

        //        /**
        //         *  Set channel mask
        //         *  @param mask mask flag: OPLL_MASK_* can be used.
        //         *  - bit 0..8: mask for ch 1 to 9 (OPLL_MASK_CH(i))
        //         *  - bit 9: mask for Hi-Hat (OPLL_MASK_HH)
        //         *  - bit 10: mask for Top-Cym (OPLL_MASK_CYM)
        //         *  - bit 11: mask for Tom (OPLL_MASK_TOM)
        //         *  - bit 12: mask for Snare Drum (OPLL_MASK_SD)
        //         *  - bit 13: mask for Bass Drum (OPLL_MASK_BD)
        //         */
        //        uint OPLL_setMask(OPLL, uint mask);

        //        /**
        //         * Toggler channel mask flag
        //         */
        //        uint OPLL_toggleMask(OPLL, uint mask);

        //        /* for compatibility */
        //#define OPLL_set_rate OPLL_setRate
        //#define OPLL_set_quality OPLL_setQuality
        //#define OPLL_set_pan OPLL_setPan
        //#define OPLL_set_pan_fine OPLL_setPanFine
        //#define OPLL_calc_stereo OPLL_calcStereo
        //#define OPLL_reset_patch OPLL_resetPatch
        //#define OPLL_dump2patch OPLL_dumpToPatch
        //#define OPLL_patch2dump OPLL_patchToDump
        //#define OPLL_setChipMode OPLL_setChipType

        //# ifdef __cplusplus
        //    }
        //#endif

        //#endif






















        //emu2413.c
        //        /**
        //         * emu2413 v1.5.9
        //         * https://github.com/digital-sound-antiques/emu2413
        //         * Copyright (C) 2020 Mitsutaka Okazaki
        //         *
        //         * This source refers to the following documents. The author would like to thank all the authors who have
        //         * contributed to the writing of them.
        //         * - [YM2413 notes](http://www.smspower.org/Development/YM2413) by andete
        //         * - ymf262.c by Jarek Burczynski
        //         * - [VRC7 presets](https://siliconpr0n.org/archive/doku.php?id=vendor:yamaha:opl2#opll_vrc7_patch_format) by Nuke.YKT
        //         * - YMF281B presets by Chabin
        //         */
        //# include "emu2413.h"
        //# include <math.h>
        //# include <stdio.h>
        //# include <stdlib.h>
        //# include <string.h>

        //# ifndef INLINE
        //#if defined(_MSC_VER)
        //#define INLINE __inline
        //#elif defined(__GNUC__)
        //#define INLINE __inline__
        //#else
        //#define INLINE inline
        //#endif
        //#endif

        private const double _PI_ = 3.14159265358979323846264338327950288;

        private const int OPLL_TONE_NUM = 3;
        /* clang-format off */
        private byte[][] default_inst = new byte[OPLL_TONE_NUM][] {
            new byte[(16 + 3) * 8] {
        0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00, // 0: User
        0x71,0x61,0x1e,0x17,0xd0,0x78,0x00,0x17, // 1: Violin
        0x13,0x41,0x1a,0x0d,0xd8,0xf7,0x23,0x13, // 2: Guitar
        0x13,0x01,0x99,0x00,0xf2,0xc4,0x21,0x23, // 3: Piano
        0x11,0x61,0x0e,0x07,0x8d,0x64,0x70,0x27, // 4: Flute
        0x32,0x21,0x1e,0x06,0xe1,0x76,0x01,0x28, // 5: Clarinet
        0x31,0x22,0x16,0x05,0xe0,0x71,0x00,0x18, // 6: Oboe
        0x21,0x61,0x1d,0x07,0x82,0x81,0x11,0x07, // 7: Trumpet
        0x33,0x21,0x2d,0x13,0xb0,0x70,0x00,0x07, // 8: Organ
        0x61,0x61,0x1b,0x06,0x64,0x65,0x10,0x17, // 9: Horn
        0x41,0x61,0x0b,0x18,0x85,0xf0,0x81,0x07, // A: Synthesizer
        0x33,0x01,0x83,0x11,0xea,0xef,0x10,0x04, // B: Harpsichord
        0x17,0xc1,0x24,0x07,0xf8,0xf8,0x22,0x12, // C: Vibraphone
        0x61,0x50,0x0c,0x05,0xd2,0xf5,0x40,0x42, // D: Synthsizer Bass
        0x01,0x01,0x55,0x03,0xe9,0x90,0x03,0x02, // E: Acoustic Bass
        0x41,0x41,0x89,0x03,0xf1,0xe4,0xc0,0x13, // F: Electric Guitar
        0x01,0x01,0x18,0x0f,0xdf,0xf8,0x6a,0x6d, // R: Bass Drum (from VRC7)
        0x01,0x01,0x00,0x00,0xc8,0xd8,0xa7,0x68, // R: High-Hat(M) / Snare Drum(C) (from VRC7)
        0x05,0x01,0x00,0x00,0xf8,0xaa,0x59,0x55, // R: Tom-tom(M) / Top Cymbal(C) (from VRC7)
            }    ,
            new byte[(16 + 3) * 8] {
        /* VRC7 presets from Nuke.YKT */
        0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
        0x03,0x21,0x05,0x06,0xe8,0x81,0x42,0x27,
        0x13,0x41,0x14,0x0d,0xd8,0xf6,0x23,0x12,
        0x11,0x11,0x08,0x08,0xfa,0xb2,0x20,0x12,
        0x31,0x61,0x0c,0x07,0xa8,0x64,0x61,0x27,
        0x32,0x21,0x1e,0x06,0xe1,0x76,0x01,0x28,
        0x02,0x01,0x06,0x00,0xa3,0xe2,0xf4,0xf4,
        0x21,0x61,0x1d,0x07,0x82,0x81,0x11,0x07,
        0x23,0x21,0x22,0x17,0xa2,0x72,0x01,0x17,
        0x35,0x11,0x25,0x00,0x40,0x73,0x72,0x01,
        0xb5,0x01,0x0f,0x0F,0xa8,0xa5,0x51,0x02,
        0x17,0xc1,0x24,0x07,0xf8,0xf8,0x22,0x12,
        0x71,0x23,0x11,0x06,0x65,0x74,0x18,0x16,
        0x01,0x02,0xd3,0x05,0xc9,0x95,0x03,0x02,
        0x61,0x63,0x0c,0x00,0x94,0xC0,0x33,0xf6,
        0x21,0x72,0x0d,0x00,0xc1,0xd5,0x56,0x06,
        0x01,0x01,0x18,0x0f,0xdf,0xf8,0x6a,0x6d,
        0x01,0x01,0x00,0x00,0xc8,0xd8,0xa7,0x68,
        0x05,0x01,0x00,0x00,0xf8,0xaa,0x59,0x55,
            }    ,
            new byte[(16 + 3) * 8] {
        /* YMF281B presets */
        0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00, // 0: User
        0x62,0x21,0x1a,0x07,0xf0,0x6f,0x00,0x16, // 1: Electric Strings (form Chabin's patch)
        0x40,0x10,0x45,0x00,0xf6,0x83,0x73,0x63, // 2: Bow Wow (based on plgDavid's patch, KSL fixed)
        0x13,0x01,0x99,0x00,0xf2,0xc3,0x21,0x23, // 3: Electric Guitar (similar to YM2413 but different DR(C))
        0x01,0x61,0x0b,0x0f,0xf9,0x64,0x70,0x17, // 4: Organ (based on Chabin, TL/DR fixed)
        0x32,0x21,0x1e,0x06,0xe1,0x76,0x01,0x28, // 5: Clarinet (identical to YM2413)
        0x60,0x01,0x82,0x0e,0xf9,0x61,0x20,0x27, // 6: Saxophone (based on plgDavid, PM/EG fixed)
        0x21,0x61,0x1c,0x07,0x84,0x81,0x11,0x07, // 7: Trumpet (similar to YM2413 but different TL/DR(M))
        0x37,0x32,0xc9,0x01,0x66,0x64,0x40,0x28, // 8: Street Organ (from Chabin)
        0x01,0x21,0x07,0x03,0xa5,0x71,0x51,0x07, // 9: Synth Brass (based on Chabin, TL fixed)
        0x06,0x01,0x5e,0x07,0xf3,0xf3,0xf6,0x13, // A: Electric Piano (based on Chabin, DR/RR/KR fixed)
        0x00,0x00,0x18,0x06,0xf5,0xf3,0x20,0x23, // B: Bass (based on Chabin, EG fixed) 
        0x17,0xc1,0x24,0x07,0xf8,0xf8,0x22,0x12, // C: Vibraphone (identical to YM2413)
        0x35,0x64,0x00,0x00,0xff,0xf3,0x77,0xf5, // D: Chimes (from plgDavid)
        0x11,0x31,0x00,0x07,0xdd,0xf3,0xff,0xfb, // E: Tom Tom II (from plgDavid)
        0x3a,0x21,0x00,0x07,0x80,0x84,0x0f,0xf5, // F: Noise (based on plgDavid, AR fixed)
        0x01,0x01,0x18,0x0f,0xdf,0xf8,0x6a,0x6d, // R: Bass Drum (identical to YM2413)
        0x01,0x01,0x00,0x00,0xc8,0xd8,0xa7,0x68, // R: High-Hat(M) / Snare Drum(C) (identical to YM2413)
        0x05,0x01,0x00,0x00,0xf8,0xaa,0x59,0x55, // R: Tom-tom(M) / Top Cymbal(C) (identical to YM2413)
            }
        };

        /* clang-format on */

        /* phase increment counter */
        private const int DP_BITS = 19;
        private const int DP_WIDTH = (1 << DP_BITS);
        private const int DP_BASE_BITS = (DP_BITS - PG_BITS);

        /* dynamic range of envelope output */
        private const double EG_STEP = 0.375;
        private const int EG_BITS = 7;
        private const int EG_MUTE = ((1 << EG_BITS) - 1);
        private const int EG_MAX = (EG_MUTE - 4);

        /* dynamic range of total level */
        private const double TL_STEP = 0.75;
        private const int TL_BITS = 6;

        /* dynamic range of sustine level */
        private const double SL_STEP = 3.0;
        private const int SL_BITS = 4;

        /* damper speed before key-on. key-scale affects. */
        private const int DAMPER_RATE = 12;

        private int TL2EG(int d) { return ((d) << 1); }

        /* sine table */
        private const int PG_BITS = 10; /* 2^10 = 1024 length sine table */
        private const int PG_WIDTH = (1 << PG_BITS);

        /* clang-format off */
        /* exp_table[x] = round((exp2((double)x / 256.0) - 1) * 1024) */
        private ushort[] exp_table = new ushort[256] {
        0,    3,    6,    8,    11,   14,   17,   20,   22,   25,   28,   31,   34,   37,   40,   42,
        45,   48,   51,   54,   57,   60,   63,   66,   69,   72,   75,   78,   81,   84,   87,   90,
        93,   96,   99,   102,  105,  108,  111,  114,  117,  120,  123,  126,  130,  133,  136,  139,
        142,  145,  148,  152,  155,  158,  161,  164,  168,  171,  174,  177,  181,  184,  187,  190,
        194,  197,  200,  204,  207,  210,  214,  217,  220,  224,  227,  231,  234,  237,  241,  244,
        248,  251,  255,  258,  262,  265,  268,  272,  276,  279,  283,  286,  290,  293,  297,  300,
        304,  308,  311,  315,  318,  322,  326,  329,  333,  337,  340,  344,  348,  352,  355,  359,
        363,  367,  370,  374,  378,  382,  385,  389,  393,  397,  401,  405,  409,  412,  416,  420,
        424,  428,  432,  436,  440,  444,  448,  452,  456,  460,  464,  468,  472,  476,  480,  484,
        488,  492,  496,  501,  505,  509,  513,  517,  521,  526,  530,  534,  538,  542,  547,  551,
        555,  560,  564,  568,  572,  577,  581,  585,  590,  594,  599,  603,  607,  612,  616,  621,
        625,  630,  634,  639,  643,  648,  652,  657,  661,  666,  670,  675,  680,  684,  689,  693,
        698,  703,  708,  712,  717,  722,  726,  731,  736,  741,  745,  750,  755,  760,  765,  770,
        774,  779,  784,  789,  794,  799,  804,  809,  814,  819,  824,  829,  834,  839,  844,  849,
        854,  859,  864,  869,  874,  880,  885,  890,  895,  900,  906,  911,  916,  921,  927,  932,
        937,  942,  948,  953,  959,  964,  969,  975,  980,  986,  991,  996, 1002, 1007, 1013, 1018
        };
        /* fullsin_table[x] = round(-log2(sin((x + 0.5) * PI / (PG_WIDTH / 4) / 2)) * 256) */
        private ushort[] fullsin_table = new ushort[1024] {//PG_WIDTH
        2137, 1731, 1543, 1419, 1326, 1252, 1190, 1137, 1091, 1050, 1013, 979,  949,  920,  894,  869,
        846,  825,  804,  785,  767,  749,  732,  717,  701,  687,  672,  659,  646,  633,  621,  609,
        598,  587,  576,  566,  556,  546,  536,  527,  518,  509,  501,  492,  484,  476,  468,  461,
        453,  446,  439,  432,  425,  418,  411,  405,  399,  392,  386,  380,  375,  369,  363,  358,
        352,  347,  341,  336,  331,  326,  321,  316,  311,  307,  302,  297,  293,  289,  284,  280,
        276,  271,  267,  263,  259,  255,  251,  248,  244,  240,  236,  233,  229,  226,  222,  219,
        215,  212,  209,  205,  202,  199,  196,  193,  190,  187,  184,  181,  178,  175,  172,  169,
        167,  164,  161,  159,  156,  153,  151,  148,  146,  143,  141,  138,  136,  134,  131,  129,
        127,  125,  122,  120,  118,  116,  114,  112,  110,  108,  106,  104,  102,  100,  98,   96,
        94,   92,   91,   89,   87,   85,   83,   82,   80,   78,   77,   75,   74,   72,   70,   69,
        67,   66,   64,   63,   62,   60,   59,   57,   56,   55,   53,   52,   51,   49,   48,   47,
        46,   45,   43,   42,   41,   40,   39,   38,   37,   36,   35,   34,   33,   32,   31,   30,
        29,   28,   27,   26,   25,   24,   23,   23,   22,   21,   20,   20,   19,   18,   17,   17,
        16,   15,   15,   14,   13,   13,   12,   12,   11,   10,   10,   9,    9,    8,    8,    7,
        7,    7,    6,    6,    5,    5,    5,    4,    4,    4,    3,    3,    3,    2,    2,    2,
        2,    1,    1,    1,    1,    1,    1,    1,    0,    0,    0,    0,    0,    0,    0,    0,

        0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,

        0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,

        0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,

        };
        /* clang-format on */

        private ushort[] halfsin_table = new ushort[PG_WIDTH];
        private ushort[][] wave_table_map = new ushort[2][]{
            new ushort[PG_WIDTH],new ushort[PG_WIDTH]
        };// { fullsin_table, halfsin_table };

        /* pitch modulator */
        /* offset to fnum, rough approximation of 14 cents depth. */
        private sbyte[][] pm_table = new sbyte[8][]{
            new sbyte[8]{0, 0, 0, 0, 0, 0, 0, 0},    // fnum = 000xxxxxx
            new sbyte[8]{0, 0, 1, 0, 0, 0, -1, 0},   // fnum = 001xxxxxx
            new sbyte[8]{0, 1, 2, 1, 0, -1, -2, -1}, // fnum = 010xxxxxx
            new sbyte[8]{0, 1, 3, 1, 0, -1, -3, -1}, // fnum = 011xxxxxx
            new sbyte[8]{0, 2, 4, 2, 0, -2, -4, -2}, // fnum = 100xxxxxx
            new sbyte[8]{0, 2, 5, 2, 0, -2, -5, -2}, // fnum = 101xxxxxx
            new sbyte[8]{0, 3, 6, 3, 0, -3, -6, -3}, // fnum = 110xxxxxx
            new sbyte[8]{0, 3, 7, 3, 0, -3, -7, -3}, // fnum = 111xxxxxx
        };

        /* amplitude lfo table */
        /* The following envelop pattern is verified on real YM2413. */
        /* each element repeates 64 cycles */
        private byte[] am_table = new byte[210] {0,  0,  0,  0,  0,  0,  0,  0,  1,  1,  1,  1,  1,  1,  1,  1,  //
                                        2,  2,  2,  2,  2,  2,  2,  2,  3,  3,  3,  3,  3,  3,  3,  3,  //
                                        4,  4,  4,  4,  4,  4,  4,  4,  5,  5,  5,  5,  5,  5,  5,  5,  //
                                        6,  6,  6,  6,  6,  6,  6,  6,  7,  7,  7,  7,  7,  7,  7,  7,  //
                                        8,  8,  8,  8,  8,  8,  8,  8,  9,  9,  9,  9,  9,  9,  9,  9,  //
                                        10, 10, 10, 10, 10, 10, 10, 10, 11, 11, 11, 11, 11, 11, 11, 11, //
                                        12, 12, 12, 12, 12, 12, 12, 12,                                 //
                                        13, 13, 13,                                                     //
                                        12, 12, 12, 12, 12, 12, 12, 12,                                 //
                                        11, 11, 11, 11, 11, 11, 11, 11, 10, 10, 10, 10, 10, 10, 10, 10, //
                                        9,  9,  9,  9,  9,  9,  9,  9,  8,  8,  8,  8,  8,  8,  8,  8,  //
                                        7,  7,  7,  7,  7,  7,  7,  7,  6,  6,  6,  6,  6,  6,  6,  6,  //
                                        5,  5,  5,  5,  5,  5,  5,  5,  4,  4,  4,  4,  4,  4,  4,  4,  //
                                        3,  3,  3,  3,  3,  3,  3,  3,  2,  2,  2,  2,  2,  2,  2,  2,  //
                                        1,  1,  1,  1,  1,  1,  1,  1,  0,  0,  0,  0,  0,  0,  0 };

        /* envelope decay increment step table */
        /* based on andete's research */
        private byte[][] eg_step_tables = new byte[4][] {
            new byte[8]{0, 1, 0, 1, 0, 1, 0, 1},
            new byte[8]{0, 1, 0, 1, 1, 1, 0, 1},
            new byte[8]{0, 1, 1, 1, 0, 1, 1, 1},
            new byte[8]{0, 1, 1, 1, 1, 1, 1, 1},
        };

        private enum __OPLL_EG_STATE { ATTACK, DECAY, SUSTAIN, RELEASE, DAMP, UNKNOWN };

        private uint[] ml_table = new uint[16]  {
            1,     1 * 2, 2 * 2,  3 * 2,  4 * 2,  5 * 2,  6 * 2,  7 * 2,
            8 * 2, 9 * 2, 10 * 2, 10 * 2, 12 * 2, 12 * 2, 15 * 2, 15 * 2
        };

        //#define dB2(x) ((x)*2)
        //        static double kl_table[16] = {dB2(0.000),  dB2(9.000),  dB2(12.000), dB2(13.875), dB2(15.000), dB2(16.125),
        //                              dB2(16.875), dB2(17.625), dB2(18.000), dB2(18.750), dB2(19.125), dB2(19.500),
        //                              dB2(19.875), dB2(20.250), dB2(20.625), dB2(21.000)};
        private double[] kl_table = new double[16] {
            0.000*2,  9.000*2,  12.000*2, 13.875*2, 15.000*2, 16.125*2,
            16.875*2, 17.625*2, 18.000*2, 18.750*2, 19.125*2, 19.500*2,
            19.875*2, 20.250*2, 20.625*2, 21.000*2
        };

        private uint[][][] tll_table;// new uint[8 * 16][1 << TL_BITS][4]

        private int[][] rks_table = new int[8 * 2][]{
            new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2],
            new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2]
        };

        private OPLL_PATCH null_patch = new OPLL_PATCH()
        {
            AM = 0,
            AR = 0,
            DR = 0,
            EG = 0,
            FB = 0,
            KL = 0,
            KR = 0,
            ML = 0,
            PM = 0,
            RR = 0,
            SL = 0,
            TL = 0,
            WS = 0
        };

        private OPLL_PATCH[][][] default_patch = new OPLL_PATCH[OPLL_TONE_NUM][][]{
        new OPLL_PATCH[19][] {
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()}, 
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()}
        },
        new OPLL_PATCH[19][] {
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()}
        },
        new OPLL_PATCH[19][] {
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()},
            new OPLL_PATCH[2]{ new OPLL_PATCH(),new OPLL_PATCH()}
        }
        };

        /* don't forget min/max is defined as a macro in stdlib.h of Visual C. */
        //# ifndef min
        private int min(int i, int j) { return (i < j) ? i : j; }
        //#endif
        //# ifndef max
        private int max(int i, int j) { return (i > j) ? i : j; }
        //#endif

        /***************************************************

                   Internal Sample Rate Converter

        ****************************************************/
        /* Note: to disable internal rate converter, set clock/72 to output sampling rate. */

        /*
         * LW is truncate length of sinc(x) calculation.
         * Lower LW is faster, higher LW results better quality.
         * LW must be a non-zero positive even number, no upper limit.
         * LW=16 or greater is recommended when upsampling.
         * LW=8 is practically okay for downsampling.
         */
        private const int LW = 16;

        /* resolution of sinc(x) table. sinc(x) where 0.0<=x<1.0 corresponds to sinc_table[0...SINC_RESO-1] */
        private const int SINC_RESO = 256;
        private const int SINC_AMP_BITS = 12;

        // double hamming(double x) { return 0.54 - 0.46 * cos(2 * PI * x); }
        private double blackman(double x) { return 0.42 - 0.5 * Math.Cos(2 * _PI_ * x) + 0.08 * Math.Cos(4 * _PI_ * x); }
        private double sinc(double x) { return (x == 0.0 ? 1.0 : Math.Sin(_PI_ * x) / (_PI_ * x)); }
        private double windowed_sinc(double x) { return blackman(0.5 + 0.5 * x / (LW / 2)) * sinc(x); }

        /* f_inp: input frequency. f_out: output frequencey, ch: number of channels */
        private OPLL_RateConv OPLL_RateConv_new(double f_inp, double f_out, int ch)
        {
            OPLL_RateConv conv = new OPLL_RateConv();// malloc(sizeof(OPLL_RateConv));
            int i;

            conv.ch = ch;
            conv.f_ratio = f_inp / f_out;
            conv.buf = new short[ch][];// malloc(sizeof(void*) * ch);
            for (i = 0; i < ch; i++)
            {
                conv.buf[i] = new short[LW];// malloc(sizeof(conv->buf[0][0]) * LW);
            }

            /* create sinc_table for positive 0 <= x < LW/2 */
            conv.sinc_table = new short[SINC_RESO * LW / 2];// malloc(sizeof(conv->sinc_table[0]) * SINC_RESO * LW / 2);
            for (i = 0; i < SINC_RESO * LW / 2; i++)
            {
                double x = (double)i / SINC_RESO;
                if (f_out < f_inp)
                {
                    /* for downsampling */
                    conv.sinc_table[i] = (short)((1 << SINC_AMP_BITS) * windowed_sinc(x / conv.f_ratio) / conv.f_ratio);
                }
                else
                {
                    /* for upsampling */
                    conv.sinc_table[i] = (short)((1 << SINC_AMP_BITS) * windowed_sinc(x));
                }
            }

            return conv;
        }

        private short lookup_sinc_table(short[] table, double x)
        {
            short index = (short)(x * SINC_RESO);
            if (index < 0)
                index = (short)-index;
            return table[Math.Min((short)(SINC_RESO * LW / 2 - 1), index)];
        }

        private void OPLL_RateConv_reset(OPLL_RateConv conv)
        {
            int i;
            conv.timer = 0;
            for (i = 0; i < conv.ch; i++)
            {
                for (int j = 0; j < LW; j++)
                {
                    conv.buf[i][j] = 0;
                }
                //memset(conv.buf[i], 0, sizeof(conv.buf[i][0]) * LW);
            }
        }

        /* put original data to this converter at f_inp. */
        private void OPLL_RateConv_putData(OPLL_RateConv conv, int ch, short data)
        {
            short[] buf = conv.buf[ch];
            int i;
            for (i = 0; i < LW - 1; i++)
            {
                buf[i] = buf[i + 1];
            }
            buf[LW - 1] = data;
        }

        /* get resampled data from this converter at f_out. */
        /* this function must be called f_out / f_inp times per one putData call. */
        private short OPLL_RateConv_getData(OPLL_RateConv conv, int ch)
        {
            short[] buf = conv.buf[ch];
            int sum = 0;
            int k;
            double dn;
            conv.timer += conv.f_ratio;
            dn = conv.timer - Math.Floor(conv.timer);
            conv.timer = dn;

            for (k = 0; k < LW; k++)
            {
                double x = ((double)k - (LW / 2 - 1)) - dn;
                sum += buf[k] * lookup_sinc_table(conv.sinc_table, x);
            }
            return (short)(sum >> SINC_AMP_BITS);
        }

        private void OPLL_RateConv_delete(OPLL_RateConv conv)
        {
            int i;
            for (i = 0; i < conv.ch; i++)
            {
                //free(conv.buf[i]);
            }
            //free(conv.buf);
            //free(conv.sinc_table);
            //free(conv);
        }

        /***************************************************

                          Create tables

        ****************************************************/

        private void makeSinTable()
        {
            int x;

            for (x = 0; x < PG_WIDTH / 4; x++)
            {
                fullsin_table[PG_WIDTH / 4 + x] = fullsin_table[PG_WIDTH / 4 - x - 1];
            }

            for (x = 0; x < PG_WIDTH / 2; x++)
            {
                fullsin_table[PG_WIDTH / 2 + x] = (ushort)(0x8000 | fullsin_table[x]);
            }

            for (x = 0; x < PG_WIDTH / 2; x++)
                halfsin_table[x] = fullsin_table[x];

            for (x = PG_WIDTH / 2; x < PG_WIDTH; x++)
                halfsin_table[x] = 0xfff;
            for (int j = 0; j < PG_WIDTH; j++)
            {
                wave_table_map[0][j] = fullsin_table[j];
                wave_table_map[1][j] = halfsin_table[j];
            }
        }

        private void makeTllTable()
        {

            int tmp;
            int fnum, block, TL, KL;
            if (tll_table == null)
            {
                tll_table = new uint[8 * 16][][];
                for (int i = 0; i < 8*16; i++)
                {
                    tll_table[i] = new uint[1<<TL_BITS][];
                    for (int j = 0; j < 1<<TL_BITS; j++)
                    {
                        tll_table[i][j] = new uint[4];
                        for (int k = 0; k < 4; k++)
                        {
                            tll_table[i][j][k] = 0;
                        }
                    }
                }
            }


            for (fnum = 0; fnum < 16; fnum++)
            {
                for (block = 0; block < 8; block++)
                {
                    for (TL = 0; TL < 64; TL++)
                    {
                        for (KL = 0; KL < 4; KL++)
                        {
                            if (KL == 0)
                            {
                                tll_table[(block << 4) | fnum][TL][KL] = (uint)TL2EG(TL);
                            }
                            else
                            {
                                tmp = (int)(kl_table[fnum] - (3.000) * 2 * (7 - block));
                                if (tmp <= 0)
                                    tll_table[(block << 4) | fnum][TL][KL] = (uint)TL2EG(TL);
                                else
                                    tll_table[(block << 4) | fnum][TL][KL] = (uint)((tmp >> (3 - KL)) / EG_STEP) + (uint)TL2EG(TL);
                            }
                        }
                    }
                }
            }
        }

        private void makeRksTable()
        {
            int fnum8, block;
            for (fnum8 = 0; fnum8 < 2; fnum8++)
                for (block = 0; block < 8; block++)
                {
                    rks_table[(block << 1) | fnum8][1] = (block << 1) + fnum8;
                    rks_table[(block << 1) | fnum8][0] = block >> 1;
                }
        }

        private void makeDefaultPatch()
        {
            int i, j;
            for (i = 0; i < OPLL_TONE_NUM; i++)
                for (j = 0; j < 19; j++)
                    OPLL_getDefaultPatch(i, j, default_patch);// [i][j * 2]);
        }

        private byte table_initialized = 0;

        private void initializeTables()
        {
            makeTllTable();
            makeRksTable();
            makeSinTable();
            makeDefaultPatch();
            table_initialized = 1;
        }

        /*********************************************************

                              Synthesizing

        *********************************************************/
        private int SLOT_BD1 = 12;
        private int SLOT_BD2 =13;
        private int SLOT_HH =14;
        private int SLOT_SD =15;
        private int SLOT_TOM =16;
        private int SLOT_CYM =17;

        /* utility macros */
        private OPLL_SLOT MOD(OPLL o, int x) { return (o.slot[(x) << 1]); }
        private OPLL_SLOT CAR(OPLL o, int x) { return (o.slot[((x) << 1) | 1]); }
        private int BIT(int s, int b) { return (((s) >> (b)) & 1); }

        #if OPLL_DEBUG
        private void _debug_print_patch(OPLL_SLOT slot) {
          OPLL_PATCH p = slot.patch;
            Console.WriteLine("[slot#{0} am:{1} pm:{2} eg:{3} kr:{4} ml:{5} kl:{6} tl:{7} ws:{8} fb:{9} A:{10} D:{11} S:{12} R:{13}]\n",
                slot.number, //
                   p.AM, p.PM, p.EG, p.KR, p.ML,                                                                     //
                   p.KL, p.TL, p.WS, p.FB,                                                                            //
                   p.AR, p.DR, p.EG, p.SL, p.RR);
        }

        private string _debug_eg_state_name(OPLL_SLOT slot)
        {
            switch (slot.eg_state)
            {
                case (byte)__OPLL_EG_STATE.ATTACK:
                    return "attack";
                case (byte)__OPLL_EG_STATE.DECAY:
                    return "decay";
                case (byte)__OPLL_EG_STATE.SUSTAIN:
                    return "sustain";
                case (byte)__OPLL_EG_STATE.RELEASE:
                    return "release";
                case (byte)__OPLL_EG_STATE.DAMP:
                    return "damp";
                default:
                    return "unknown";
            }
        }

        private void _debug_print_slot_info(OPLL_SLOT slot)
        {
            string name = _debug_eg_state_name(slot);
            Console.WriteLine("[slot#{0} state:{1} fnum:{2:03x} rate:{3}-{4}]\n",
                slot.number, name, slot.blk_fnum, slot.eg_rate_h,
                   slot.eg_rate_l);
            _debug_print_patch(slot);
            //fflush(stdout);
        }
        #endif

        private int get_parameter_rate(OPLL_SLOT slot)
        {

            if ((slot.type & 1) == 0 && slot.key_flag == 0)
            {
                return 0;
            }

            switch (slot.eg_state)
            {
                case (byte)__OPLL_EG_STATE.ATTACK:
                    return (int)slot.patch.AR;
                case (byte)__OPLL_EG_STATE.DECAY:
                    return (int)slot.patch.DR;
                case (byte)__OPLL_EG_STATE.SUSTAIN:
                    return (int)(slot.patch.EG != 0 ? 0 : slot.patch.RR);
                case (byte)__OPLL_EG_STATE.RELEASE:
                    if (slot.sus_flag != 0)
                    {
                        return 5;
                    }
                    else if (slot.patch.EG != 0)
                    {
                        return (int)slot.patch.RR;
                    }
                    else
                    {
                        return 7;
                    }
                case (byte)__OPLL_EG_STATE.DAMP:
                    return DAMPER_RATE;
                default:
                    return 0;
            }
        }

        private enum SLOT_UPDATE_FLAG : uint
        {
            UPDATE_WS = 1,
            UPDATE_TLL = 2,
            UPDATE_RKS = 4,
            UPDATE_EG = 8,
            UPDATE_ALL = 255,
        };

        private void request_update(OPLL_SLOT slot, uint flag) { slot.update_requests |= flag; }

        private void commit_slot_update(OPLL_SLOT slot)
        {
            #if OPLL_DEBUG
            if (slot.last_eg_state != slot.eg_state)
            {
                _debug_print_slot_info(slot);
                slot.last_eg_state = slot.eg_state;
            }
            #endif

            if ((slot.update_requests & (uint)SLOT_UPDATE_FLAG.UPDATE_WS) != 0)
            {
                slot.wave_table = wave_table_map[slot.patch.WS];
            }

            if ((slot.update_requests & (uint)SLOT_UPDATE_FLAG.UPDATE_TLL) != 0)
            {
                if ((slot.type & 1) == 0)
                {
                    slot.tll = (ushort)tll_table[slot.blk_fnum >> 5][slot.patch.TL][slot.patch.KL];
                }
                else
                {
                    slot.tll = (ushort)tll_table[slot.blk_fnum >> 5][slot.volume][slot.patch.KL];
                }
            }

            if ((slot.update_requests & (uint)SLOT_UPDATE_FLAG.UPDATE_RKS) != 0)
            {
                slot.rks = (byte)rks_table[slot.blk_fnum >> 8][slot.patch.KR];
            }

            if ((slot.update_requests & ((uint)SLOT_UPDATE_FLAG.UPDATE_RKS | (uint)SLOT_UPDATE_FLAG.UPDATE_EG)) != 0)
            {
                int p_rate = get_parameter_rate(slot);

                if (p_rate == 0)
                {
                    slot.eg_shift = 0;
                    slot.eg_rate_h = 0;
                    slot.eg_rate_l = 0;
                    return;
                }

                slot.eg_rate_h = (byte)Math.Min(15, p_rate + (slot.rks >> 2));
                slot.eg_rate_l = (byte)(slot.rks & 3);
                if (slot.eg_state == (byte)__OPLL_EG_STATE.ATTACK)
                {
                    slot.eg_shift = (uint)((0 < slot.eg_rate_h && slot.eg_rate_h < 12) ? (13 - slot.eg_rate_h) : 0);
                }
                else
                {
                    slot.eg_shift = (uint)((slot.eg_rate_h < 13) ? (13 - slot.eg_rate_h) : 0);
                }
            }

            slot.update_requests = 0;
        }

        private void reset_slot(OPLL_SLOT slot, int number)
        {
            slot.number = (byte)number;
            slot.type = (byte)(number % 2);
            slot.pg_keep = 0;
            slot.wave_table = wave_table_map[0];
            slot.pg_phase = 0;
            slot.output[0] = 0;
            slot.output[1] = 0;
            slot.eg_state = (byte)__OPLL_EG_STATE.RELEASE;
            slot.eg_shift = 0;
            slot.rks = 0;
            slot.tll = 0;
            slot.key_flag = 0;
            slot.sus_flag = 0;
            slot.blk_fnum = 0;
            slot.blk = 0;
            slot.fnum = 0;
            slot.volume = 0;
            slot.pg_out = 0;
            slot.eg_out = EG_MUTE;
            slot.patch = null_patch;
        }

        private void slotOn(OPLL opll, int i)
        {
            OPLL_SLOT slot = opll.slot[i];
            slot.key_flag = 1;
            slot.eg_state = (byte)__OPLL_EG_STATE.DAMP;
            request_update(slot, (uint)SLOT_UPDATE_FLAG.UPDATE_EG);
        }

        private void slotOff(OPLL opll, int i)
        {
            OPLL_SLOT slot = opll.slot[i];
            slot.key_flag = 0;
            if ((slot.type & 1) != 0)
            {
                slot.eg_state = (byte)__OPLL_EG_STATE.RELEASE;
                request_update(slot, (uint)SLOT_UPDATE_FLAG.UPDATE_EG);
            }
        }

        private void update_key_status(OPLL opll)
        {
            byte r14 = opll.reg[0x0e];
            byte rhythm_mode = (byte)BIT(r14, 5);
            uint new_slot_key_status = 0;
            uint updated_status;
            int ch;

            for (ch = 0; ch < 9; ch++)
                if ((opll.reg[0x20 + ch] & 0x10) != 0)
                    new_slot_key_status |= (uint)(3 << (ch * 2));

            if (rhythm_mode != 0)
            {
                if ((r14 & 0x10) != 0)
                    new_slot_key_status |= (uint)(3 << SLOT_BD1);

                if ((r14 & 0x01) != 0)
                    new_slot_key_status |= (uint)(1 << SLOT_HH);

                if ((r14 & 0x08) != 0)
                    new_slot_key_status |= (uint)(1 << SLOT_SD);

                if ((r14 & 0x04) != 0)
                    new_slot_key_status |= (uint)(1 << SLOT_TOM);

                if ((r14 & 0x02) != 0)
                    new_slot_key_status |= (uint)(1 << SLOT_CYM);
            }

            updated_status = opll.slot_key_status ^ new_slot_key_status;

            if (updated_status != 0)
            {
                int i;
                for (i = 0; i < 18; i++)
                    if (BIT((int)updated_status, i) != 0)
                    {
                        if (BIT((int)new_slot_key_status, i) != 0)
                        {
                            slotOn(opll, i);
                        }
                        else
                        {
                            slotOff(opll, i);
                        }
                    }
            }

            opll.slot_key_status = new_slot_key_status;
        }

        private void set_patch(OPLL opll, int ch, int num)
        {
            opll.patch_number[ch] = num;
            MOD(opll, ch).patch = opll.patch[num][0];
            CAR(opll, ch).patch = opll.patch[num][1];
            request_update(MOD(opll, ch), (uint)SLOT_UPDATE_FLAG.UPDATE_ALL);
            request_update(CAR(opll, ch), (uint)SLOT_UPDATE_FLAG.UPDATE_ALL);
        }

        private void set_sus_flag(OPLL opll, int ch, int flag)
        {
            CAR(opll, ch).sus_flag = (byte)flag;
            request_update(CAR(opll, ch), (uint)SLOT_UPDATE_FLAG.UPDATE_EG);
            if ((MOD(opll, ch).type & 1) != 0)
            {
                MOD(opll, ch).sus_flag = (byte)flag;
                request_update(MOD(opll, ch), (uint)SLOT_UPDATE_FLAG.UPDATE_EG);
            }
        }

        /* set volume ( volume : 6bit, register value << 2 ) */
        private void set_volume(OPLL opll, int ch, int volume)
        {
            CAR(opll, ch).volume = volume;
            request_update(CAR(opll, ch), (uint)SLOT_UPDATE_FLAG.UPDATE_TLL);
        }

        private void set_slot_volume(OPLL_SLOT slot, int volume)
        {
            slot.volume = volume;
            request_update(slot, (uint)SLOT_UPDATE_FLAG.UPDATE_TLL);
        }

        /* set f-Nnmber ( fnum : 9bit ) */
        private void set_fnumber(OPLL opll, int ch, int fnum)
        {
            OPLL_SLOT car = CAR(opll, ch);
            OPLL_SLOT mod = MOD(opll, ch);
            car.fnum = (ushort)fnum;
            car.blk_fnum = (ushort)((car.blk_fnum & 0xe00) | (fnum & 0x1ff));
            mod.fnum = (ushort)fnum;
            mod.blk_fnum = (ushort)((mod.blk_fnum & 0xe00) | (fnum & 0x1ff));
            request_update(car, (uint)SLOT_UPDATE_FLAG.UPDATE_EG | (uint)SLOT_UPDATE_FLAG.UPDATE_RKS | (uint)SLOT_UPDATE_FLAG.UPDATE_TLL);
            request_update(mod, (uint)SLOT_UPDATE_FLAG.UPDATE_EG | (uint)SLOT_UPDATE_FLAG.UPDATE_RKS | (uint)SLOT_UPDATE_FLAG.UPDATE_TLL);
        }

        /* set block data (blk : 3bit ) */
        private void set_block(OPLL opll, int ch, int blk)
        {
            OPLL_SLOT car = CAR(opll, ch);
            OPLL_SLOT mod = MOD(opll, ch);
            car.blk = (byte)blk;
            car.blk_fnum = (ushort)(((blk & 7) << 9) | (car.blk_fnum & 0x1ff));
            mod.blk = (byte)blk;
            mod.blk_fnum = (ushort)(((blk & 7) << 9) | (mod.blk_fnum & 0x1ff));
            request_update(car, (uint)SLOT_UPDATE_FLAG.UPDATE_EG | (uint)SLOT_UPDATE_FLAG.UPDATE_RKS | (uint)SLOT_UPDATE_FLAG.UPDATE_TLL);
            request_update(mod, (uint)SLOT_UPDATE_FLAG.UPDATE_EG | (uint)SLOT_UPDATE_FLAG.UPDATE_RKS | (uint)SLOT_UPDATE_FLAG.UPDATE_TLL);
        }

        private void update_rhythm_mode(OPLL opll)
        {
            byte new_rhythm_mode = (byte)((opll.reg[0x0e] >> 5) & 1);

            if (opll.rhythm_mode != new_rhythm_mode)
            {

                if (new_rhythm_mode != 0)
                {
                    opll.slot[SLOT_HH].type = 3;
                    opll.slot[SLOT_HH].pg_keep = 1;
                    opll.slot[SLOT_SD].type = 3;
                    opll.slot[SLOT_TOM].type = 3;
                    opll.slot[SLOT_CYM].type = 3;
                    opll.slot[SLOT_CYM].pg_keep = 1;
                    set_patch(opll, 6, 16);
                    set_patch(opll, 7, 17);
                    set_patch(opll, 8, 18);
                    set_slot_volume(opll.slot[SLOT_HH], ((opll.reg[0x37] >> 4) & 15) << 2);
                    set_slot_volume(opll.slot[SLOT_TOM], ((opll.reg[0x38] >> 4) & 15) << 2);
                }
                else
                {
                    opll.slot[SLOT_HH].type = 0;
                    opll.slot[SLOT_HH].pg_keep = 0;
                    opll.slot[SLOT_SD].type = 1;
                    opll.slot[SLOT_TOM].type = 0;
                    opll.slot[SLOT_CYM].type = 1;
                    opll.slot[SLOT_CYM].pg_keep = 0;
                    set_patch(opll, 6, opll.reg[0x36] >> 4);
                    set_patch(opll, 7, opll.reg[0x37] >> 4);
                    set_patch(opll, 8, opll.reg[0x38] >> 4);
                }
            }

            opll.rhythm_mode = new_rhythm_mode;
        }

        private void update_ampm(OPLL opll)
        {
            if ((opll.test_flag & 2) != 0)
            {
                opll.pm_phase = 0;
                opll.am_phase = 0;
            }
            else
            {
                opll.pm_phase += (uint)((opll.test_flag & 8) != 0 ? 1024 : 1);
                opll.am_phase += (opll.test_flag & 8) != 0 ? 64 : 1;
            }
            opll.lfo_am = am_table[(opll.am_phase >> 6) % am_table.Length];// sizeof(am_table)];
        }

        private void update_noise(OPLL opll, int cycle)
        {
            int i;
            for (i = 0; i < cycle; i++)
            {
                if( (opll.noise & 1)!=0)
                {
                    opll.noise ^= 0x800200;
                }
                opll.noise >>= 1;
            }
        }

        private void update_short_noise(OPLL opll)
        {
            uint pg_hh = opll.slot[SLOT_HH].pg_out;
            uint pg_cym = opll.slot[SLOT_CYM].pg_out;

            byte h_bit2 = (byte)BIT((int)pg_hh, PG_BITS - 8);
            byte h_bit7 = (byte)BIT((int)pg_hh, PG_BITS - 3);
            byte h_bit3 = (byte)BIT((int)pg_hh, PG_BITS - 7);

            byte c_bit3 = (byte)BIT((int)pg_cym, PG_BITS - 7);
            byte c_bit5 = (byte)BIT((int)pg_cym, PG_BITS - 5);

            opll.short_noise = (byte)((h_bit2 ^ h_bit7) | (h_bit3 ^ c_bit5) | (c_bit3 ^ c_bit5));
        }

        private void calc_phase(OPLL_SLOT slot, int pm_phase, byte reset)
        {
            sbyte pm = (sbyte)(slot.patch.PM != 0 ? pm_table[(slot.fnum >> 6) & 7][(pm_phase >> 10) & 7] : 0);
            if (reset != 0)
            {
                slot.pg_phase = 0;
            }
            slot.pg_phase += (uint)((((slot.fnum & 0x1ff) * 2 + pm) * ml_table[slot.patch.ML]) << slot.blk >> 2);
            slot.pg_phase &= (DP_WIDTH - 1);
            slot.pg_out = slot.pg_phase >> DP_BASE_BITS;
        }

        private byte lookup_attack_step(OPLL_SLOT slot, uint counter)
        {
            int index;

            switch (slot.eg_rate_h)
            {
                case 12:
                    index = (int)((counter & 0xc) >> 1);
                    return (byte)(4 - eg_step_tables[slot.eg_rate_l][index]);
                case 13:
                    index = (int)((counter & 0xc) >> 1);
                    return (byte)(3 - eg_step_tables[slot.eg_rate_l][index]);
                case 14:
                    index = (int)((counter & 0xc) >> 1);
                    return (byte)(2 - eg_step_tables[slot.eg_rate_l][index]);
                case 0:
                case 15:
                    return 0;
                default:
                    index = (int)(counter >> (int)slot.eg_shift);
                    return (byte)(eg_step_tables[slot.eg_rate_l][index & 7] != 0 ? 4 : 0);
            }
        }

        private byte lookup_decay_step(OPLL_SLOT slot, uint counter)
        {
            int index;

            switch (slot.eg_rate_h)
            {
                case 0:
                    return 0;
                case 13:
                    index = (int)(((counter & 0xc) >> 1) | (counter & 1));
                    return eg_step_tables[slot.eg_rate_l][index];
                case 14:
                    index = (int)(((counter & 0xc) >> 1));
                    return (byte)(eg_step_tables[slot.eg_rate_l][index] + 1);
                case 15:
                    return 2;
                default:
                    index = (int)(counter >> (int)slot.eg_shift);
                    return eg_step_tables[slot.eg_rate_l][index & 7];
            }
        }

        private void start_envelope(OPLL_SLOT slot)
        {
            if (Math.Min(15, slot.patch.AR + (slot.rks >> 2)) == 15)
            {
                slot.eg_state = (byte)__OPLL_EG_STATE.DECAY;
                slot.eg_out = 0;
            }
            else
            {
                slot.eg_state = (byte)__OPLL_EG_STATE.ATTACK;
            }
            request_update(slot, (uint)SLOT_UPDATE_FLAG.UPDATE_EG);
        }

        private void calc_envelope(OPLL_SLOT slot, OPLL_SLOT buddy, ushort eg_counter, byte test)
        {

            uint mask = (uint)((1 << (int)slot.eg_shift) - 1);
            byte s;

            if (slot.eg_state == (byte)__OPLL_EG_STATE.ATTACK)
            {
                if (0 < slot.eg_out && 0 < slot.eg_rate_h && (eg_counter & mask & ~3) == 0)
                {
                    s = lookup_attack_step(slot, eg_counter);
                    if (0 < s)
                    {
                        slot.eg_out = (uint)Math.Max(0, (int)((int)slot.eg_out - (slot.eg_out >> s) - 1));
                    }
                }
            }
            else
            {
                if (slot.eg_rate_h > 0 && (eg_counter & mask) == 0)
                {
                    slot.eg_out = Math.Min(EG_MUTE, slot.eg_out + lookup_decay_step(slot, eg_counter));
                }
            }

            switch (slot.eg_state)
            {
                case (byte)__OPLL_EG_STATE.DAMP:
                    // DAMP to ATTACK transition is occured when the envelope reaches EG_MAX (max attenuation but it's not mute).
                    // Do not forget to check (eg_counter & mask) == 0 to synchronize it with the progress of the envelope.
                    if (slot.eg_out >= EG_MAX && (eg_counter & mask) == 0)
                    {
                        start_envelope(slot);
                        if ((slot.type & 1) != 0)
                        {
                            if (slot.pg_keep == 0)
                            {
                                slot.pg_phase = 0;
                            }
                            if (buddy != null && buddy.pg_keep == 0)
                            {
                                buddy.pg_phase = 0;
                            }
                        }
                    }
                    break;

                case (byte)__OPLL_EG_STATE.ATTACK:
                    if (slot.eg_out == 0)
                    {
                        slot.eg_state = (byte)__OPLL_EG_STATE.DECAY;
                        request_update(slot, (uint)SLOT_UPDATE_FLAG.UPDATE_EG);
                    }
                    break;

                case (byte)__OPLL_EG_STATE.DECAY:
                    // DECAY to SUSTAIN transition must be checked at every cycle regardless of the conditions of the envelope rate and
                    // counter. i.e. the transition is not synchronized with the progress of the envelope.
                    if ((slot.eg_out >> 3) == slot.patch.SL)
                    {
                        slot.eg_state = (byte)__OPLL_EG_STATE.SUSTAIN;
                        request_update(slot, (uint)SLOT_UPDATE_FLAG.UPDATE_EG);
                    }
                    break;

                case (byte)__OPLL_EG_STATE.SUSTAIN:
                case (byte)__OPLL_EG_STATE.RELEASE:
                default:
                    break;
            }

            if (test != 0)
            {
                slot.eg_out = 0;
            }
        }

        private void update_slots(OPLL opll)
        {
            int i;
            opll.eg_counter++;

            for (i = 0; i < 18; i++)
            {
                OPLL_SLOT slot = opll.slot[i];
                OPLL_SLOT buddy = null;
                if (slot.type == 0)
                {
                    buddy = opll.slot[i + 1];
                }
                if (slot.type == 1)
                {
                    buddy = opll.slot[i - 1];
                }
                if (slot.update_requests != 0)
                {
                    commit_slot_update(slot);
                }
                calc_envelope(slot, buddy, (ushort)opll.eg_counter, (byte)(opll.test_flag & 1));
                calc_phase(slot, (ushort)opll.pm_phase, (byte)(opll.test_flag & 4));
            }
        }

        /* output: -4095...4095 */
        private short lookup_exp_table(ushort i)
        {
            /* from andete's expression */
            short t = (short)(exp_table[(i & 0xff) ^ 0xff] + 1024);
            short res = (short)(t >> ((i & 0x7f00) >> 8));
            return (short)(((i & 0x8000) != 0 ? (short)~res : res) << 1);
        }

        private short to_linear(ushort h, OPLL_SLOT slot, short am)
        {
            ushort att;
            if (slot.eg_out > EG_MAX)
                return 0;

            att = (ushort)(Math.Min(EG_MUTE, (slot.eg_out + slot.tll + am)) << 4);
            return lookup_exp_table((ushort)(h + att));
        }

        private short calc_slot_car(OPLL opll, int ch, short fm)
        {
            OPLL_SLOT slot = CAR(opll, ch);

            byte am = (byte)(slot.patch.AM != 0 ? opll.lfo_am : 0);

            slot.output[1] = slot.output[0];
            slot.output[0] = to_linear(slot.wave_table[(slot.pg_out + 2 * (fm >> 1)) & (PG_WIDTH - 1)], slot, am);

            return (short)slot.output[0];
        }

        private short calc_slot_mod(OPLL opll, int ch)
        {
            OPLL_SLOT slot = MOD(opll, ch);

            short fm = (short)(slot.patch.FB > 0 ? ((slot.output[1] + slot.output[0]) >> (int)(9 - slot.patch.FB)) : 0);
            byte am = (byte)(slot.patch.AM != 0 ? opll.lfo_am : 0);

            slot.output[1] = slot.output[0];
            slot.output[0] = to_linear(slot.wave_table[(slot.pg_out + fm) & (PG_WIDTH - 1)], slot, am);

            return (short)slot.output[0];
        }

        private short calc_slot_tom(OPLL opll)
        {
            OPLL_SLOT slot = MOD(opll, 8);

            return to_linear(slot.wave_table[slot.pg_out], slot, 0);
        }

        /* Specify phase offset directly based on 10-bit (1024-length) sine table */
        private int _PD(int phase) { return ((PG_BITS < 10) ? (phase >> (10 - PG_BITS)) : (phase << (PG_BITS - 10))); }

        private short calc_slot_snare(OPLL opll)
        {
            OPLL_SLOT slot = CAR(opll, 7);

            uint phase;

            if (BIT((int)slot.pg_out, PG_BITS - 2) != 0)
                phase = (uint)((opll.noise & 1) != 0 ? _PD(0x300) : _PD(0x200));
            else
                phase = (uint)((opll.noise & 1) != 0 ? _PD(0x0) : _PD(0x100));

            return to_linear(slot.wave_table[phase], slot, 0);
        }

        private short calc_slot_cym(OPLL opll)
        {
            OPLL_SLOT slot = CAR(opll, 8);

            uint phase = (uint)(opll.short_noise!=0 ? _PD(0x300) : _PD(0x100));

            return to_linear(slot.wave_table[phase], slot, 0);
        }

        private short calc_slot_hat(OPLL opll)
        {
            OPLL_SLOT slot = MOD(opll, 7);

            uint phase;

            if (opll.short_noise != 0)
                phase = (uint)((opll.noise & 1) != 0 ? _PD(0x2d0) : _PD(0x234));
            else
                phase = (uint)((opll.noise & 1) != 0 ? _PD(0x34) : _PD(0xd0));

            return to_linear(slot.wave_table[phase], slot, 0);
        }

        private int _MO(int x) { return (-(x) >> 1); }
        private int _RO(int x) { return (x); }

        private void update_output(OPLL opll)
        {
            short[] _out;
            int i;

            update_ampm(opll);
            update_short_noise(opll);
            update_slots(opll);

            _out = opll.ch_out;

            /* CH1-6 */
            for (i = 0; i < 6; i++)
            {
                if ((opll.mask & OPLL_MASK_CH(i)) == 0)
                {
                    _out[i] = (short)_MO(calc_slot_car(opll, i, calc_slot_mod(opll, i)));
                }
            }

            /* CH7 */
            if (opll.rhythm_mode == 0)
            {
                if ((opll.mask & OPLL_MASK_CH(6)) == 0)
                {
                    _out[6] = (short)_MO(calc_slot_car(opll, 6, calc_slot_mod(opll, 6)));
                }
            }
            else
            {
                if ((opll.mask & OPLL_MASK_BD) == 0)
                {
                    _out[9] = (short)_RO(calc_slot_car(opll, 6, calc_slot_mod(opll, 6)));
                }
            }
            update_noise(opll, 14);

            /* CH8 */
            if (opll.rhythm_mode == 0)
            {
                if ((opll.mask & OPLL_MASK_CH(7)) == 0)
                {
                    _out[7] = (short)_MO(calc_slot_car(opll, 7, calc_slot_mod(opll, 7)));
                }
            }
            else
            {
                if ((opll.mask & OPLL_MASK_HH) == 0)
                {
                    _out[10] = (short)_RO(calc_slot_hat(opll));
                }
                if ((opll.mask & OPLL_MASK_SD) == 0)
                {
                    _out[11] = (short)_RO(calc_slot_snare(opll));
                }
            }
            update_noise(opll, 2);

            /* CH9 */
            if (opll.rhythm_mode == 0)
            {
                if ((opll.mask & OPLL_MASK_CH(8)) == 0)
                {
                    _out[8] = (short)_MO(calc_slot_car(opll, 8, calc_slot_mod(opll, 8)));
                }
            }
            else
            {
                if ((opll.mask & OPLL_MASK_TOM) == 0)
                {
                    _out[12] = (short)_RO(calc_slot_tom(opll));
                }
                if ((opll.mask & OPLL_MASK_CYM) == 0)
                {
                    _out[13] = (short)_RO(calc_slot_cym(opll));
                }
            }
            update_noise(opll, 2);
        }

        private void mix_output(OPLL opll)
        {
            short _out = 0;
            int i;
            for (i = 0; i < 14; i++)
            {
                _out += opll.ch_out[i];
            }
            if (opll.conv != null)
            {
                OPLL_RateConv_putData(opll.conv, 0, _out);
            }
            else
            {
                opll.mix_out[0] = _out;
            }
        }

        private void mix_output_stereo(OPLL opll)
        {
            short[] _out = opll.mix_out;
            int i;
            _out[0] = _out[1] = 0;
            for (i = 0; i < 14; i++)
            {
                if ((opll.pan[i] & 2) != 0)
                    _out[0] += (short)(opll.ch_out[i] * opll.pan_fine[i][0]);
                if ((opll.pan[i] & 1) != 0)
                    _out[1] += (short)(opll.ch_out[i] * opll.pan_fine[i][1]);
            }
            if (opll.conv != null)
            {
                OPLL_RateConv_putData(opll.conv, 0, _out[0]);
                OPLL_RateConv_putData(opll.conv, 1, _out[1]);
            }
        }

        /***********************************************************

                           External Interfaces

        ***********************************************************/

        private OPLL OPLL_new(uint clk, uint rate)
        {
            OPLL opll;
            int i;

            if (table_initialized == 0)
            {
                initializeTables();
            }

            opll = new OPLL();// (OPLL)calloc(sizeof(OPLL), 1);
            if (opll == null)
                return null;

            for (i = 0; i < 19; i++)
            {
                opll.patch[i] = new OPLL_PATCH[2];// null_patch;
                for (int j = 0; j < 2; j++)
                {
                    opll.patch[i][j] = new OPLL_PATCH();
                }
                //memcpy(&opll.patch[i], &null_patch, sizeof(OPLL_PATCH));
            }

            opll.clk = clk;
            opll.rate = rate;
            opll.mask = 0;
            opll.conv = null;
            opll.mix_out[0] = 0;
            opll.mix_out[1] = 0;

            OPLL_reset(opll);
            OPLL_setChipType(opll, 0);
            OPLL_resetPatch(opll, 0);
            return opll;
        }

        private void OPLL_delete(OPLL opll)
        {
            if (opll.conv != null)
            {
                OPLL_RateConv_delete(opll.conv);
                opll.conv = null;
            }
            //free(opll);
        }

        private void reset_rate_conversion_params(OPLL opll)
        {
            double f_out = opll.rate;
            double f_inp = opll.clk / 72.0;

            opll.out_time = 0;
            opll.out_step = f_inp;
            opll.inp_step = f_out;

            if (opll.conv != null)
            {
                OPLL_RateConv_delete(opll.conv);
                opll.conv = null;
            }

            if (Math.Floor(f_inp) != f_out && Math.Floor(f_inp + 0.5) != f_out)
            {
                opll.conv = OPLL_RateConv_new(f_inp, f_out, 2);
            }

            if (opll.conv != null)
            {
                OPLL_RateConv_reset(opll.conv);
            }
        }

        private void OPLL_reset(OPLL opll)
        {
            int i;

            if (opll == null)
                return;

            opll.adr = 0;

            opll.pm_phase = 0;
            opll.am_phase = 0;

            opll.noise = 0x1;
            opll.mask = 0;

            opll.rhythm_mode = 0;
            opll.slot_key_status = 0;
            opll.eg_counter = 0;

            reset_rate_conversion_params(opll);

            for (i = 0; i < 18; i++)
            {
                opll.slot[i] = new OPLL_SLOT();
                reset_slot(opll.slot[i], i);
            }

            for (i = 0; i < 9; i++)
            {
                set_patch(opll, i, 0);
            }

            for (i = 0; i < 0x40; i++)
                OPLL_writeReg(opll, (uint)i, 0);

            for (i = 0; i < 15; i++)
            {
                opll.pan[i] = 3;
                opll.pan_fine[i][1] = opll.pan_fine[i][0] = 1.0f;
            }

            for (i = 0; i < 14; i++)
            {
                opll.ch_out[i] = 0;
            }
        }

        private void OPLL_forceRefresh(OPLL opll)
        {
            int i;

            if (opll == null)
                return;

            for (i = 0; i < 9; i++)
            {
                set_patch(opll, i, opll.patch_number[i]);
            }

            for (i = 0; i < 18; i++)
            {
                request_update(opll.slot[i],(uint)SLOT_UPDATE_FLAG.UPDATE_ALL);
            }
        }

        private void OPLL_setRate(OPLL opll, uint rate)
        {
            opll.rate = rate;
            reset_rate_conversion_params(opll);
        }

        private void OPLL_setQuality(OPLL opll, byte q) { }

        private void OPLL_setChipType(OPLL opll, byte type) { opll.chip_type = type; }

        private void OPLL_writeReg(OPLL opll, uint reg, byte data)
        {
            int ch, i;

            if (reg >= 0x40)
            {
                extendFunction(opll, reg, data);
                return;
            }

            /* mirror registers */
            if ((0x19 <= reg && reg <= 0x1f) || (0x29 <= reg && reg <= 0x2f) || (0x39 <= reg && reg <= 0x3f))
            {
                reg -= 9;
            }

            opll.reg[reg] = (byte)data;

            switch (reg)
            {
                case 0x00:
                    opll.patch[0][0].AM = (uint)((data >> 7) & 1);
                    opll.patch[0][0].PM = (uint)((data >> 6) & 1);
                    opll.patch[0][0].EG = (uint)((data >> 5) & 1);
                    opll.patch[0][0].KR = (uint)((data >> 4) & 1);
                    opll.patch[0][0].ML = (uint)((data) & 15);
                    for (i = 0; i < 9; i++)
                    {
                        if (opll.patch_number[i] == 0)
                        {
                            request_update(MOD(opll, i), (uint)SLOT_UPDATE_FLAG.UPDATE_RKS | (uint)SLOT_UPDATE_FLAG.UPDATE_EG);
                        }
                    }
                    break;

                case 0x01:
                    opll.patch[0][1].AM = (uint)((data >> 7) & 1);
                    opll.patch[0][1].PM = (uint)((data >> 6) & 1);
                    opll.patch[0][1].EG = (uint)((data >> 5) & 1);
                    opll.patch[0][1].KR = (uint)((data >> 4) & 1);
                    opll.patch[0][1].ML = (uint)((data) & 15);
                    for (i = 0; i < 9; i++)
                    {
                        if (opll.patch_number[i] == 0)
                        {
                            request_update(CAR(opll, i), (uint)SLOT_UPDATE_FLAG.UPDATE_RKS | (uint)SLOT_UPDATE_FLAG.UPDATE_EG);
                        }
                    }
                    break;

                case 0x02:
                    opll.patch[0][0].KL = (uint)((data >> 6) & 3);
                    opll.patch[0][0].TL = (uint)((data) & 63);
                    for (i = 0; i < 9; i++)
                    {
                        if (opll.patch_number[i] == 0)
                        {
                            request_update(MOD(opll, i), (uint)SLOT_UPDATE_FLAG.UPDATE_TLL);
                        }
                    }
                    break;

                case 0x03:
                    opll.patch[0][1].KL = (uint)((data >> 6) & 3);
                    opll.patch[0][1].WS = (uint)((data >> 4) & 1);
                    opll.patch[0][0].WS = (uint)((data >> 3) & 1);
                    opll.patch[0][0].FB = (uint)((data) & 7);
                    for (i = 0; i < 9; i++)
                    {
                        if (opll.patch_number[i] == 0)
                        {
                            request_update(MOD(opll, i), (uint)SLOT_UPDATE_FLAG.UPDATE_WS);
                            request_update(CAR(opll, i), (uint)SLOT_UPDATE_FLAG.UPDATE_WS | (uint)SLOT_UPDATE_FLAG.UPDATE_TLL);
                        }
                    }
                    break;

                case 0x04:
                    opll.patch[0][0].AR = (uint)((data >> 4) & 15);
                    opll.patch[0][0].DR = (uint)((data) & 15);
                    for (i = 0; i < 9; i++)
                    {
                        if (opll.patch_number[i] == 0)
                        {
                            request_update(MOD(opll, i), (uint)SLOT_UPDATE_FLAG.UPDATE_EG);
                        }
                    }
                    break;

                case 0x05:
                    opll.patch[0][1].AR = (uint)((data >> 4) & 15);
                    opll.patch[0][1].DR = (uint)((data) & 15);
                    for (i = 0; i < 9; i++)
                    {
                        if (opll.patch_number[i] == 0)
                        {
                            request_update(CAR(opll, i), (uint)SLOT_UPDATE_FLAG.UPDATE_EG);
                        }
                    }
                    break;

                case 0x06:
                    opll.patch[0][0].SL = (uint)((data >> 4) & 15);
                    opll.patch[0][0].RR = (uint)((data) & 15);
                    for (i = 0; i < 9; i++)
                    {
                        if (opll.patch_number[i] == 0)
                        {
                            request_update(MOD(opll, i), (uint)SLOT_UPDATE_FLAG.UPDATE_EG);
                        }
                    }
                    break;

                case 0x07:
                    opll.patch[0][1].SL = (uint)((data >> 4) & 15);
                    opll.patch[0][1].RR = (uint)((data) & 15);
                    for (i = 0; i < 9; i++)
                    {
                        if (opll.patch_number[i] == 0)
                        {
                            request_update(CAR(opll, i), (uint)SLOT_UPDATE_FLAG.UPDATE_EG);
                        }
                    }
                    break;

                case 0x0e:
                    if (opll.chip_type == 1)
                        break;
                    update_rhythm_mode(opll);
                    update_key_status(opll);
                    break;

                case 0x0f:
                    opll.test_flag = data;
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
                    set_fnumber(opll, ch, data + ((opll.reg[0x20 + ch] & 1) << 8));
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
                    set_fnumber(opll, ch, ((data & 1) << 8) + opll.reg[0x10 + ch]);
                    set_block(opll, ch, (data >> 1) & 7);
                    set_sus_flag(opll, ch, (data >> 5) & 1);
                    update_key_status(opll);
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
                    if ((opll.reg[0x0e] & 32) != 0 && (reg >= 0x36))
                    {
                        switch (reg)
                        {
                            case 0x37:
                                set_slot_volume(MOD(opll, 7), ((data >> 4) & 15) << 2);
                                break;
                            case 0x38:
                                set_slot_volume(MOD(opll, 8), ((data >> 4) & 15) << 2);
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        set_patch(opll, (int)(reg - 0x30), (data >> 4) & 15);
                    }
                    set_volume(opll, (int)(reg - 0x30), (data & 15) << 2);
                    break;

                default:
                    break;
            }
        }

        private void extendFunction(OPLL opll, uint reg, byte data)
        {
            switch (reg)
            {
                case 0x40: //パンChannel指定
                    opll.panCh = (byte)Math.Min(Math.Max(data, (byte)0), (byte)13);
                    break;
                case 0x41: //パン値指定
                    opll.pan[opll.panCh] = (byte)(
                        ((data & 0xf0) != 0 ? 0x02 : 0x00)
                        | ((data & 0x0f) != 0 ? 0x01 : 0x00));
                    opll.pan[opll.panCh] = (byte)(
                        (opll.pan[opll.panCh] == 0) 
                        ? 3 
                        : opll.pan[opll.panCh]);

                    opll.pan_fine[opll.panCh][0] = 1.0f * (data >> 4) / 15.0f;
                    opll.pan_fine[opll.panCh][1] = 1.0f * (data & 0xf) / 15.0f;
                    break;
            }
        }

        private void OPLL_writeIO(OPLL opll, uint adr, byte val)
        {
            if ((adr & 1)!=0)
                OPLL_writeReg(opll, opll.adr, val);
            else
                opll.adr = val;
        }

        private void OPLL_setPan(OPLL opll, uint ch, byte pan) { opll.pan[ch & 15] = pan; }

        private void OPLL_setPanFine(OPLL opll, uint ch, float[] pan)//[2])
        {
            opll.pan_fine[ch & 15][0] = pan[0];
            opll.pan_fine[ch & 15][1] = pan[1];
        }

        private void OPLL_dumpToPatch(byte[] dump,int startAdr, OPLL_PATCH[][] patch)
        {
            if (patch[startAdr][0] == null) patch[startAdr][0] = new OPLL_PATCH();
            if (patch[startAdr][1] == null) patch[startAdr][1] = new OPLL_PATCH();

            patch[startAdr][0].AM = (uint)((dump[0 + startAdr*8] >> 7) & 1);
            patch[startAdr][1].AM = (uint)((dump[1 + startAdr*8] >> 7) & 1);
            patch[startAdr][0].PM = (uint)((dump[0 + startAdr*8] >> 6) & 1);
            patch[startAdr][1].PM = (uint)((dump[1 + startAdr*8] >> 6) & 1);
            patch[startAdr][0].EG = (uint)((dump[0 + startAdr*8] >> 5) & 1);
            patch[startAdr][1].EG = (uint)((dump[1 + startAdr*8] >> 5) & 1);
            patch[startAdr][0].KR = (uint)((dump[0 + startAdr*8] >> 4) & 1);
            patch[startAdr][1].KR = (uint)((dump[1 + startAdr*8] >> 4) & 1);
            patch[startAdr][0].ML = (uint)((dump[0 + startAdr*8]) & 15);
            patch[startAdr][1].ML = (uint)((dump[1 + startAdr*8]) & 15);
            patch[startAdr][0].KL = (uint)((dump[2 + startAdr*8] >> 6) & 3);
            patch[startAdr][1].KL = (uint)((dump[3 + startAdr*8] >> 6) & 3);
            patch[startAdr][0].TL = (uint)((dump[2 + startAdr*8]) & 63);
            patch[startAdr][1].TL = 0;               
            patch[startAdr][0].FB = (uint)((dump[3 + startAdr*8]) & 7);
            patch[startAdr][1].FB = 0;               
            patch[startAdr][0].WS = (uint)((dump[3 + startAdr*8] >> 3) & 1);
            patch[startAdr][1].WS = (uint)((dump[3 + startAdr*8] >> 4) & 1);
            patch[startAdr][0].AR = (uint)((dump[4 + startAdr*8] >> 4) & 15);
            patch[startAdr][1].AR = (uint)((dump[5 + startAdr*8] >> 4) & 15);
            patch[startAdr][0].DR = (uint)((dump[4 + startAdr*8]) & 15);
            patch[startAdr][1].DR = (uint)((dump[5 + startAdr*8]) & 15);
            patch[startAdr][0].SL = (uint)((dump[6 + startAdr*8] >> 4) & 15);
            patch[startAdr][1].SL = (uint)((dump[7 + startAdr*8] >> 4) & 15);
            patch[startAdr][0].RR = (uint)((dump[6 + startAdr*8]) & 15);
            patch[startAdr][1].RR = (uint)((dump[7 + startAdr*8]) & 15);
        }

        private void OPLL_getDefaultPatch(int type, int num, OPLL_PATCH[][][] patch)
        {
            //OPLL_dumpToPatch(default_inst[type], num * 8, patch);
            OPLL_dumpToPatch(default_inst[type], num, patch[type]);
        }

        private void OPLL_setPatch(OPLL opll, byte[] dump)
        {
            OPLL_PATCH[][] patch=new OPLL_PATCH[2][];// [2];
            int i;
            for (i = 0; i < 19; i++)
            {
                OPLL_dumpToPatch(dump,  i , patch);
                opll.patch[i][0] = patch[i][0];
                opll.patch[i][1] = patch[i][1];
            }
        }

        private void OPLL_patchToDump(OPLL_PATCH[] patch, byte[] dump)
        {
            dump[0] = (byte) ((patch[0].AM << 7) + (patch[0].PM << 6) + (patch[0].EG << 5) + (patch[0].KR << 4) + patch[0].ML);
            dump[1] = (byte) ((patch[1].AM << 7) + (patch[1].PM << 6) + (patch[1].EG << 5) + (patch[1].KR << 4) + patch[1].ML);
            dump[2] = (byte) ((patch[0].KL << 6) + patch[0].TL);
            dump[3] = (byte) ((patch[1].KL << 6) + (patch[1].WS << 4) + (patch[0].WS << 3) + patch[0].FB);
            dump[4] = (byte) ((patch[0].AR << 4) + patch[0].DR);
            dump[5] = (byte) ((patch[1].AR << 4) + patch[1].DR);
            dump[6] = (byte) ((patch[0].SL << 4) + patch[0].RR);
            dump[7] = (byte) ((patch[1].SL << 4) + patch[1].RR);
        }

        private void OPLL_copyPatch(OPLL opll, int num, OPLL_PATCH[] patch)
        {
            //memcpy(&opll.patch[num], patch, sizeof(OPLL_PATCH));
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
            opll.patch[num][0].WS = patch[0].WS;

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
            opll.patch[num][1].WS = patch[1].WS;
        }

        private void OPLL_resetPatch(OPLL opll, byte type)
        {
            int i;
            for (i = 0; i < 19; i++)
                OPLL_copyPatch(opll, i, default_patch[type][i]);
        }

        private short OPLL_calc(OPLL opll)
        {
            while (opll.out_step > opll.out_time)
            {
                opll.out_time += opll.inp_step;
                update_output(opll);
                mix_output(opll);
            }
            opll.out_time -= opll.out_step;
            if (opll.conv != null)
            {
                opll.mix_out[0] = OPLL_RateConv_getData(opll.conv, 0);
            }
            return opll.mix_out[0];
        }

        private void OPLL_calcStereo(OPLL opll, int[] _out)//[2])
        {
            while (opll.out_step > opll.out_time)
            {
                opll.out_time += opll.inp_step;
                update_output(opll);
                mix_output_stereo(opll);
            }
            opll.out_time -= opll.out_step;
            if (opll.conv != null)
            {
                _out[0] = OPLL_RateConv_getData(opll.conv, 0);
                _out[1] = OPLL_RateConv_getData(opll.conv, 1);
            }
            else
            {
                _out[0] = opll.mix_out[0];
                _out[1] = opll.mix_out[1];
            }
        }

        private uint OPLL_setMask(OPLL opll, uint mask)
        {
            uint ret;

            if (opll!=null)
            {
                ret = opll.mask;
                opll.mask = mask;
                return ret;
            }
            else
                return 0;
        }

        private uint OPLL_toggleMask(OPLL opll, uint mask)
        {
            uint ret;

            if (opll != null)
            {
                ret = opll.mask;
                opll.mask ^= mask;
                return ret;
            }
            else
                return 0;
        }











        private OPLL[] opll_ = new OPLL[2];
        private int[] buffers = new int[2];
        private const uint DefaultYM2413ClockValue = 3579545;

        public override string Name { get { return "EMU2413"; } set { } }
        public override string ShortName { get { return "OPLLe"; } set { } }

        public override uint Start(byte ChipID, uint SamplingRate)
        {
            visVolume = new int[2][][] {
                new int[1][] { new int[2] { 0, 0 } }
                , new int[1][] { new int[2] { 0, 0 } }
            };
            opll_[ChipID] = OPLL_new(DefaultYM2413ClockValue, SamplingRate);
            return SamplingRate;
        }

        public override uint Start(byte ChipID, uint SamplingRate, uint FMClockValue, params object[] option)
        {
            visVolume = new int[2][][] {
                new int[1][] { new int[2] { 0, 0 } }
                , new int[1][] { new int[2] { 0, 0 } }
            };
            opll_[ChipID] = OPLL_new(FMClockValue, SamplingRate);
            return SamplingRate;
        }

        public override void Stop(byte ChipID)
        {
            opll_[ChipID] = null;
        }

        public override void Reset(byte ChipID)
        {
        }

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {
            for (int i = 0; i < samples; i++)
            {
                OPLL_calcStereo(opll_[ChipID], buffers);
                outputs[0][i] = buffers[0] << 1;
                outputs[1][i] = buffers[1] << 1;
            }

            visVolume[ChipID][0][0] = outputs[0][0];
            visVolume[ChipID][0][1] = outputs[1][0];

        }

        private void YM2413_Write(byte ChipID, byte Adr, byte Data)
        {
            if (opll_[ChipID] == null) return;
            OPLL_writeReg(opll_[ChipID], Adr, Data);
        }

        public override int Write(byte ChipID, int port, int adr, int data)
        {
            YM2413_Write(ChipID, (byte)adr, (byte)data);
            return 0;
        }


    }
}
