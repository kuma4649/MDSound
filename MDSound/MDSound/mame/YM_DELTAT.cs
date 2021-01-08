using System;
using System.Collections.Generic;
using System.Text;

namespace MDSound.mame
{
    public class ym_deltat
    {
        //
        // ymdelta.h
        //

        //#pragma once

        private const int YM_DELTAT_SHIFT = (16);

        public const int YM_DELTAT_EMULATION_MODE_NORMAL = 0;
        public const int YM_DELTAT_EMULATION_MODE_YM2610 = 1;


        public delegate void dlgSTATUS_CHANGE_HANDLER(fm.FM_base chip, byte status_bits);


        /* DELTA-T (adpcm type B) struct */
        public class YM_DELTAT// deltat_adpcm_state
        {     /* AT: rearranged and tigntened structure */
            public byte[] memory;
            public int[] output_pointer;/* pointer of output pointers   */
            public int[] pan;         /* pan : &output_pointer[pan]   */
            public int panPtr;
            public double freqbase;
            //#if 0
            //	double	write_time;		/* Y8950: 10 cycles of main clock; YM2608: 20 cycles of main clock */
            //	double	read_time;		/* Y8950: 8 cycles of main clock;  YM2608: 18 cycles of main clock */
            //#endif
            public uint memory_size;
            public uint memory_mask;
            public int output_range;
            public uint now_addr;        /* current address      */
            public uint now_step;        /* currect step         */
            public uint step;            /* step                 */
            public uint start;           /* start address        */
            public uint limit;           /* limit address        */
            public uint end;         /* end address          */
            public uint delta;           /* delta scale          */
            public int volume;           /* current volume       */
            public int acc;          /* shift Measurement value*/
            public int adpcmd;           /* next Forecast        */
            public int adpcml;           /* current value        */
            public int prev_acc;     /* leveling value       */
            public byte now_data;     /* current rom data     */
            public byte CPU_data;     /* current data from reg 08 */
            public byte portstate;        /* port status          */
            public byte control2;     /* control reg: SAMPLE, DA/AD, RAM TYPE (x8bit / x1bit), ROM/RAM */
            public byte portshift;        /* address bits shift-left:
                            ** 8 for YM2610,
                            ** 5 for Y8950 and YM2608 */

            public byte DRAMportshift;    /* address bits shift-right:
                            ** 0 for ROM and x8bit DRAMs,
                            ** 3 for x1 DRAMs */

            public byte memread;      /* needed for reading/writing external memory */

            /* handlers and parameters for the status flags support */
            public dlgSTATUS_CHANGE_HANDLER status_set_handler;
            public dlgSTATUS_CHANGE_HANDLER status_reset_handler;

            /* note that different chips have these flags on different
			** bits of the status register
			*/
            public fm.FM_base status_change_which_chip; /* this chip id */
            public byte status_change_EOS_bit;        /* 1 on End Of Sample (record/playback/cycle time of AD/DA converting has passed)*/
            public byte status_change_BRDY_bit;       /* 1 after recording 2 datas (2x4bits) or after reading/writing 1 data */
            public byte status_change_ZERO_bit;       /* 1 if silence lasts for more than 290 miliseconds on ADPCM recording */

            /* neither Y8950 nor YM2608 can generate IRQ when PCMBSY bit changes, so instead of above,
			** the statusflag gets ORed with PCM_BSY (below) (on each read of statusflag of Y8950 and YM2608)
			*/
            public byte PCM_BSY;      /* 1 when ADPCM is playing; Y8950/YM2608 only */

            public byte[] reg = new byte[16];      /* adpcm registers      */
            public int regPtr = 0;
            public byte emulation_mode;   /* which chip we're emulating */
        }

        /*void YM_DELTAT_BRDY_callback(YM_DELTAT *DELTAT);*/

        //UINT8 YM_DELTAT_ADPCM_Read(YM_DELTAT* DELTAT);
        //void YM_DELTAT_ADPCM_Write(YM_DELTAT* DELTAT, int r, int v);
        //void YM_DELTAT_ADPCM_Reset(YM_DELTAT* DELTAT, int pan, int emulation_mode);
        //void YM_DELTAT_ADPCM_CALC(YM_DELTAT* DELTAT);

        /*void YM_DELTAT_postload(YM_DELTAT *DELTAT,UINT8 *regs);
		//void YM_DELTAT_savestate(const device_config *device,YM_DELTAT *DELTAT);
		void YM_DELTAT_savestate(YM_DELTAT *DELTAT);*/

        //void YM_DELTAT_calc_mem_mask(YM_DELTAT* DELTAT);





        //
        // ymdelta.c
        //


        /*
**
** File: ymdeltat.c
**
** YAMAHA DELTA-T adpcm sound emulation subroutine
** used by fmopl.c (Y8950) and fm.c (YM2608 and YM2610/B)
**
** Base program is YM2610 emulator by Hiromitsu Shioya.
** Written by Tatsuyuki Satoh
** Improvements by Jarek Burczynski (bujar at mame dot net)
**
**
** History:
**
** 03-08-2003 Jarek Burczynski:
**  - fixed BRDY flag implementation.
**
** 24-07-2003 Jarek Burczynski, Frits Hilderink:
**  - fixed delault value for control2 in YM_DELTAT_ADPCM_Reset
**
** 22-07-2003 Jarek Burczynski, Frits Hilderink:
**  - fixed external memory support
**
** 15-06-2003 Jarek Burczynski:
**  - implemented CPU -> AUDIO ADPCM synthesis (via writes to the ADPCM data reg $08)
**  - implemented support for the Limit address register
**  - supported two bits from the control register 2 ($01): RAM TYPE (x1 bit/x8 bit), ROM/RAM
**  - implemented external memory access (read/write) via the ADPCM data reg reads/writes
**    Thanks go to Frits Hilderink for the example code.
**
** 14-06-2003 Jarek Burczynski:
**  - various fixes to enable proper support for status register flags: BSRDY, PCM BSY, ZERO
**  - modified EOS handling
**
** 05-04-2003 Jarek Burczynski:
**  - implemented partial support for external/processor memory on sample replay
**
** 01-12-2002 Jarek Burczynski:
**  - fixed first missing sound in gigandes thanks to previous fix (interpolator) by ElSemi
**  - renamed/removed some YM_DELTAT struct fields
**
** 28-12-2001 Acho A. Tang
**  - added EOS status report on ADPCM playback.
**
** 05-08-2001 Jarek Burczynski:
**  - now_step is initialized with 0 at the start of play.
**
** 12-06-2001 Jarek Burczynski:
**  - corrected end of sample bug in YM_DELTAT_ADPCM_CALC.
**    Checked on real YM2610 chip - address register is 24 bits wide.
**    Thanks go to Stefan Jokisch (stefan.jokisch@gmx.de) for tracking down the problem.
**
** TO DO:
**      Check size of the address register on the other chips....
**
** Version 0.72
**
** sound chips that have this unit:
** YM2608   OPNA
** YM2610/B OPNB
** Y8950    MSX AUDIO
**
*/

        //# include "mamedef.h"
        //# include <stdio.h>
        ////#include "sndintrf.h"
        //# include "ymdeltat.h"

        private const int YM_DELTAT_DELTA_MAX = (24576);
        private const int YM_DELTAT_DELTA_MIN = (127);
        private const int YM_DELTAT_DELTA_DEF = (127);

        private const int YM_DELTAT_DECODE_RANGE = 32768;
        private const int YM_DELTAT_DECODE_MIN = (-(YM_DELTAT_DECODE_RANGE));
        private const int YM_DELTAT_DECODE_MAX = ((YM_DELTAT_DECODE_RANGE) - 1);


        /* Forecast to next Forecast (rate = *8) */
        /* 1/8 , 3/8 , 5/8 , 7/8 , 9/8 , 11/8 , 13/8 , 15/8 */
        private int[] ym_deltat_decode_tableB1 = new int[16] {
          1,   3,   5,   7,   9,  11,  13,  15,
          -1,  -3,  -5,  -7,  -9, -11, -13, -15,
        };
        /* delta to next delta (rate= *64) */
        /* 0.9 , 0.9 , 0.9 , 0.9 , 1.2 , 1.6 , 2.0 , 2.4 */
        private int[] ym_deltat_decode_tableB2 = new int[16] {
          57,  57,  57,  57, 77, 102, 128, 153,
          57,  57,  57,  57, 77, 102, 128, 153
        };

        //#if 0
        //void YM_DELTAT_BRDY_callback(YM_DELTAT *DELTAT)
        //{
        //	logerror("BRDY_callback reached (flag set) !\n");

        //	/* set BRDY bit in status register */
        //	if(DELTAT->status_set_handler)
        //		if(DELTAT->status_change_BRDY_bit)
        //			(DELTAT->status_set_handler)(DELTAT->status_change_which_chip, DELTAT->status_change_BRDY_bit);
        //}
        //#endif

        public byte YM_DELTAT_ADPCM_Read(YM_DELTAT DELTAT)
        {
            byte v = 0;

            /* external memory read */
            if ((DELTAT.portstate & 0xe0) == 0x20)
            {
                /* two dummy reads */
                if (DELTAT.memread != 0)
                {
                    DELTAT.now_addr = DELTAT.start << 1;
                    DELTAT.memread--;
                    return 0;
                }


                if (DELTAT.now_addr != (DELTAT.end << 1))
                {
                    v = DELTAT.memory[DELTAT.now_addr >> 1];

                    /*logerror("YM Delta-T memory read  $%08x, v=$%02x\n", DELTAT.now_addr >> 1, v);*/

                    DELTAT.now_addr += 2; /* two nibbles at a time */

                    /* reset BRDY bit in status register, which means we are reading the memory now */
                    if (DELTAT.status_reset_handler != null)
                        if (DELTAT.status_change_BRDY_bit != 0)
                            DELTAT.status_reset_handler(DELTAT.status_change_which_chip, DELTAT.status_change_BRDY_bit);

                    /* setup a timer that will callback us in 10 master clock cycles for Y8950
                    * in the callback set the BRDY flag to 1 , which means we have another data ready.
                    * For now, we don't really do this; we simply reset and set the flag in zero time, so that the IRQ will work.
                    */
                    /* set BRDY bit in status register */
                    if (DELTAT.status_set_handler != null)
                        if (DELTAT.status_change_BRDY_bit != 0)
                            DELTAT.status_set_handler(DELTAT.status_change_which_chip, DELTAT.status_change_BRDY_bit);
                }
                else
                {
                    /* set EOS bit in status register */
                    if (DELTAT.status_set_handler != null)
                        if (DELTAT.status_change_EOS_bit != 0)
                            DELTAT.status_set_handler(DELTAT.status_change_which_chip, DELTAT.status_change_EOS_bit);
                }
            }

            return v;
        }


        /* 0-DRAM x1, 1-ROM, 2-DRAM x8, 3-ROM (3 is bad setting - not allowed by the manual) */
        private byte[] dram_rightshift = new byte[4] { 3, 0, 0, 0 };

        /* DELTA-T ADPCM write register */
        public void YM_DELTAT_ADPCM_Write(YM_DELTAT DELTAT, int r, int v)
        {
            if (r >= 0x10) return;
            DELTAT.reg[DELTAT.regPtr + r] = (byte)v; /* stock data */

            switch (r)
            {
                case 0x00:
                    /*
                    START:
                        Accessing *external* memory is started when START bit (D7) is set to "1", so
                        you must set all conditions needed for recording/playback before starting.
                        If you access *CPU-managed* memory, recording/playback starts after
                        read/write of ADPCM data register $08.

                    REC:
                        0 = ADPCM synthesis (playback)
                        1 = ADPCM analysis (record)

                    MEMDATA:
                        0 = processor (*CPU-managed*) memory (means: using register $08)
                        1 = external memory (using start/end/limit registers to access memory: RAM or ROM)


                    SPOFF:
                        controls output pin that should disable the speaker while ADPCM analysis

                    RESET and REPEAT only work with external memory.


                    some examples:
                    value:   START, REC, MEMDAT, REPEAT, SPOFF, x,x,RESET   meaning:
                      C8     1      1    0       0       1      0 0 0       Analysis (recording) from AUDIO to CPU (to reg $08), sample rate in PRESCALER register
                      E8     1      1    1       0       1      0 0 0       Analysis (recording) from AUDIO to EXT.MEMORY,       sample rate in PRESCALER register
                      80     1      0    0       0       0      0 0 0       Synthesis (playing) from CPU (from reg $08) to AUDIO,sample rate in DELTA-N register
                      a0     1      0    1       0       0      0 0 0       Synthesis (playing) from EXT.MEMORY to AUDIO,        sample rate in DELTA-N register

                      60     0      1    1       0       0      0 0 0       External memory write via ADPCM data register $08
                      20     0      0    1       0       0      0 0 0       External memory read via ADPCM data register $08

                    */
                    /* handle emulation mode */
                    if (DELTAT.emulation_mode == YM_DELTAT_EMULATION_MODE_YM2610)
                    {
                        v |= 0x20;      /*  YM2610 always uses external memory and doesn't even have memory flag bit. */
                    }

                    DELTAT.portstate = (byte)(v & (0x80 | 0x40 | 0x20 | 0x10 | 0x01)); /* start, rec, memory mode, repeat flag copy, reset(bit0) */

                    if ((DELTAT.portstate & 0x80) != 0)/* START,REC,MEMDATA,REPEAT,SPOFF,--,--,RESET */
                    {
                        /* set PCM BUSY bit */
                        DELTAT.PCM_BSY = 1;

                        /* start ADPCM */
                        DELTAT.now_step = 0;
                        DELTAT.acc = 0;
                        DELTAT.prev_acc = 0;
                        DELTAT.adpcml = 0;
                        DELTAT.adpcmd = YM_DELTAT_DELTA_DEF;
                        DELTAT.now_data = 0;
                        if (DELTAT.start > DELTAT.end)
                            logerror("DeltaT-Warning: Start: %06X, End: %06X\n", DELTAT.start, DELTAT.end);
                    }

                    if ((DELTAT.portstate & 0x20) != 0) /* do we access external memory? */
                    {
                        DELTAT.now_addr = DELTAT.start << 1;
                        DELTAT.memread = 2;    /* two dummy reads needed before accesing external memory via register $08*/

                        /* if yes, then let's check if ADPCM memory is mapped and big enough */
                        if (DELTAT.memory == null)
                        {
#if DEBUG
                            logerror("YM Delta-T ADPCM rom not mapped\n");
#endif
                            DELTAT.portstate = 0x00;
                            DELTAT.PCM_BSY = 0;
                        }
                        else
                        {
                            if (DELTAT.end >= DELTAT.memory_size) /* Check End in Range */
                            {
#if DEBUG
                                logerror("YM Delta-T ADPCM end out of range: $%08x\n", DELTAT.end);
#endif
                                DELTAT.end = DELTAT.memory_size - 1;
                            }
                            if (DELTAT.start >= DELTAT.memory_size)   /* Check Start in Range */
                            {
#if DEBUG
                                logerror("YM Delta-T ADPCM start out of range: $%08x\n", DELTAT.start);
#endif
                                DELTAT.portstate = 0x00;
                                DELTAT.PCM_BSY = 0;
                            }
                        }
                    }
                    else    /* we access CPU memory (ADPCM data register $08) so we only reset now_addr here */
                    {
                        DELTAT.now_addr = 0;
                    }

                    if ((DELTAT.portstate & 0x01) != 0)
                    {
                        DELTAT.portstate = 0x00;

                        /* clear PCM BUSY bit (in status register) */
                        DELTAT.PCM_BSY = 0;

                        /* set BRDY flag */
                        if (DELTAT.status_set_handler != null)
                            if (DELTAT.status_change_BRDY_bit != 0)
                                DELTAT.status_set_handler(DELTAT.status_change_which_chip, DELTAT.status_change_BRDY_bit);
                    }
                    break;
                case 0x01:  /* L,R,-,-,SAMPLE,DA/AD,RAMTYPE,ROM */
                    /* handle emulation mode */
                    if (DELTAT.emulation_mode == YM_DELTAT_EMULATION_MODE_YM2610)
                    {
                        v |= 0x01;      /*  YM2610 always uses ROM as an external memory and doesn't have ROM/RAM memory flag bit. */
                    }

                    DELTAT.pan = DELTAT.output_pointer;
                    DELTAT.panPtr = (v >> 6) & 0x03;
                    if ((DELTAT.control2 & 3) != (v & 3))
                    {
                        /*0-DRAM x1, 1-ROM, 2-DRAM x8, 3-ROM (3 is bad setting - not allowed by the manual) */
                        if (DELTAT.DRAMportshift != dram_rightshift[v & 3])
                        {
                            DELTAT.DRAMportshift = dram_rightshift[v & 3];

                            /* final shift value depends on chip type and memory type selected:
                                    8 for YM2610 (ROM only),
                                    5 for ROM for Y8950 and YM2608,
                                    5 for x8bit DRAMs for Y8950 and YM2608,
                                    2 for x1bit DRAMs for Y8950 and YM2608.
                            */

                            /* refresh addresses */
                            DELTAT.start = (uint)((DELTAT.reg[DELTAT.regPtr+0x3] * 0x0100 | DELTAT.reg[DELTAT.regPtr + 0x2]) << (DELTAT.portshift - DELTAT.DRAMportshift));
                            DELTAT.end = (uint)((DELTAT.reg[DELTAT.regPtr + 0x5] * 0x0100 | DELTAT.reg[DELTAT.regPtr + 0x4]) << (DELTAT.portshift - DELTAT.DRAMportshift));
                            DELTAT.end += (uint)((1 << (DELTAT.portshift - DELTAT.DRAMportshift)) - 1);
                            DELTAT.limit = (uint)((DELTAT.reg[DELTAT.regPtr + 0xd] * 0x0100 | DELTAT.reg[DELTAT.regPtr + 0xc]) << (DELTAT.portshift - DELTAT.DRAMportshift));
                        }
                    }
                    DELTAT.control2 = (byte)v;
                    break;
                case 0x02:  /* Start Address L */
                case 0x03:  /* Start Address H */
                    DELTAT.start = (uint)((DELTAT.reg[DELTAT.regPtr + 0x3] * 0x0100 | DELTAT.reg[DELTAT.regPtr + 0x2]) << (DELTAT.portshift - DELTAT.DRAMportshift));
                    /*logerror("DELTAT start: 02=%2x 03=%2x addr=%8x\n",DELTAT.reg[0x2], DELTAT.reg[0x3],DELTAT.start );*/
                    break;
                case 0x04:  /* Stop Address L */
                case 0x05:  /* Stop Address H */
                    DELTAT.end = (uint)((DELTAT.reg[DELTAT.regPtr + 0x5] * 0x0100 | DELTAT.reg[DELTAT.regPtr + 0x4]) << (DELTAT.portshift - DELTAT.DRAMportshift));
                    DELTAT.end += (uint)((1 << (DELTAT.portshift - DELTAT.DRAMportshift)) - 1);
                    /*logerror("DELTAT end  : 04=%2x 05=%2x addr=%8x\n",DELTAT.reg[0x4], DELTAT.reg[0x5],DELTAT.end   );*/
                    break;
                case 0x06:  /* Prescale L (ADPCM and Record frq) */
                case 0x07:  /* Prescale H */
                    break;
                case 0x08:  /* ADPCM data */

                    /*
                    some examples:
                    value:   START, REC, MEMDAT, REPEAT, SPOFF, x,x,RESET   meaning:
                      C8     1      1    0       0       1      0 0 0       Analysis (recording) from AUDIO to CPU (to reg $08), sample rate in PRESCALER register
                      E8     1      1    1       0       1      0 0 0       Analysis (recording) from AUDIO to EXT.MEMORY,       sample rate in PRESCALER register
                      80     1      0    0       0       0      0 0 0       Synthesis (playing) from CPU (from reg $08) to AUDIO,sample rate in DELTA-N register
                      a0     1      0    1       0       0      0 0 0       Synthesis (playing) from EXT.MEMORY to AUDIO,        sample rate in DELTA-N register

                      60     0      1    1       0       0      0 0 0       External memory write via ADPCM data register $08
                      20     0      0    1       0       0      0 0 0       External memory read via ADPCM data register $08

                    */

                    /* external memory write */
                    if ((DELTAT.portstate & 0xe0) == 0x60)
                    {
                        if (DELTAT.memread != 0)
                        {
                            DELTAT.now_addr = DELTAT.start << 1;
                            DELTAT.memread = 0;
                        }

                        /*logerror("YM Delta-T memory write $%08x, v=$%02x\n", DELTAT.now_addr >> 1, v);*/

                        if (DELTAT.now_addr != (DELTAT.end << 1))
                        {
                            DELTAT.memory[DELTAT.now_addr >> 1] = (byte)v;
                            DELTAT.now_addr += 2; /* two nibbles at a time */

                            /* reset BRDY bit in status register, which means we are processing the write */
                            if (DELTAT.status_reset_handler != null)
                                if (DELTAT.status_change_BRDY_bit != 0)
                                    DELTAT.status_reset_handler(DELTAT.status_change_which_chip, DELTAT.status_change_BRDY_bit);

                            /* setup a timer that will callback us in 10 master clock cycles for Y8950
                            * in the callback set the BRDY flag to 1 , which means we have written the data.
                            * For now, we don't really do this; we simply reset and set the flag in zero time, so that the IRQ will work.
                            */
                            /* set BRDY bit in status register */
                            if (DELTAT.status_set_handler != null)
                                if (DELTAT.status_change_BRDY_bit != 0)
                                    DELTAT.status_set_handler(DELTAT.status_change_which_chip, DELTAT.status_change_BRDY_bit);

                        }
                        else
                        {
                            /* set EOS bit in status register */
                            if (DELTAT.status_set_handler != null)
                                if (DELTAT.status_change_EOS_bit != 0)
                                    DELTAT.status_set_handler(DELTAT.status_change_which_chip, DELTAT.status_change_EOS_bit);
                        }

                        return;
                    }

                    /* ADPCM synthesis from CPU */
                    if ((DELTAT.portstate & 0xe0) == 0x80)
                    {
                        DELTAT.CPU_data = (byte)v;

                        /* Reset BRDY bit in status register, which means we are full of data */
                        if (DELTAT.status_reset_handler != null)
                            if (DELTAT.status_change_BRDY_bit != 0)
                                DELTAT.status_reset_handler(DELTAT.status_change_which_chip, DELTAT.status_change_BRDY_bit);
                        return;
                    }

                    break;
                case 0x09:  /* DELTA-N L (ADPCM Playback Prescaler) */
                case 0x0a:  /* DELTA-N H */
                    DELTAT.delta = (uint)(DELTAT.reg[DELTAT.regPtr + 0xa] * 0x0100 | DELTAT.reg[DELTAT.regPtr + 0x9]);
                    DELTAT.step = (uint)((double)(DELTAT.delta /* *(1<<(YM_DELTAT_SHIFT-16)) */ ) * (DELTAT.freqbase));
                    /*logerror("DELTAT deltan:09=%2x 0a=%2x\n",DELTAT.reg[0x9], DELTAT.reg[0xa]);*/
                    break;
                case 0x0b:  /* Output level control (volume, linear) */
                    {
                        int oldvol = DELTAT.volume;
                        DELTAT.volume = (v & 0xff) * (DELTAT.output_range / 256) / YM_DELTAT_DECODE_RANGE;
                        /*                              v     *     ((1<<16)>>8)        >>  15;
                        *                       thus:   v     *     (1<<8)              >>  15;
                        *                       thus: output_range must be (1 << (15+8)) at least
                        *                               v     *     ((1<<23)>>8)        >>  15;
                        *                               v     *     (1<<15)             >>  15;
                        */
                        /*logerror("DELTAT vol = %2x\n",v&0xff);*/
                        if (oldvol != 0)
                        {
                            DELTAT.adpcml = (int)((double)DELTAT.adpcml / (double)oldvol * (double)DELTAT.volume);
                        }
                    }
                    break;
                case 0x0c:  /* Limit Address L */
                case 0x0d:  /* Limit Address H */
                    DELTAT.limit = (uint)((DELTAT.reg[DELTAT.regPtr + 0xd] * 0x0100 | DELTAT.reg[DELTAT.regPtr + 0xc]) << (DELTAT.portshift - DELTAT.DRAMportshift));
                    /*logerror("DELTAT limit: 0c=%2x 0d=%2x addr=%8x\n",DELTAT.reg[0xc], DELTAT.reg[0xd],DELTAT.limit );*/
                    break;
            }
        }

        private void logerror(string msg, params object[] param)
        {
            throw new NotImplementedException();
        }

        public void YM_DELTAT_ADPCM_Reset(YM_DELTAT DELTAT, int pan, int emulation_mode)
        {
            DELTAT.now_addr = 0;
            DELTAT.now_step = 0;
            DELTAT.step = 0;
            DELTAT.start = 0;
            DELTAT.end = 0;
            DELTAT.limit = unchecked((uint)~0); /* this way YM2610 and Y8950 (both of which don't have limit address reg) will still work */
            DELTAT.volume = 0;
            DELTAT.pan = DELTAT.output_pointer;
            DELTAT.panPtr = pan;
            DELTAT.acc = 0;
            DELTAT.prev_acc = 0;
            DELTAT.adpcmd = 127;
            DELTAT.adpcml = 0;
            DELTAT.emulation_mode = (byte)emulation_mode;
            DELTAT.portstate = (byte)((emulation_mode == YM_DELTAT_EMULATION_MODE_YM2610) ? 0x20 : 0);
            DELTAT.control2 = (byte)((emulation_mode == YM_DELTAT_EMULATION_MODE_YM2610) ? 0x01 : 0);  /* default setting depends on the emulation mode. MSX demo called "facdemo_4" doesn't setup control2 register at all and still works */
            DELTAT.DRAMportshift = dram_rightshift[DELTAT.control2 & 3];

            /* The flag mask register disables the BRDY after the reset, however
            ** as soon as the mask is enabled the flag needs to be set. */

            /* set BRDY bit in status register */
            if (DELTAT.status_set_handler != null)
                if (DELTAT.status_change_BRDY_bit != 0)
                    DELTAT.status_set_handler(DELTAT.status_change_which_chip, DELTAT.status_change_BRDY_bit);
        }

        public void YM_DELTAT_postload(YM_DELTAT DELTAT,byte[] regs,int regPtr)
        {
        	int r;

        	// to keep adpcml
        	DELTAT.volume = 0;
            // update
            for (r = 1; r < 16; r++)
                YM_DELTAT_ADPCM_Write(DELTAT, r, regs[regPtr + r]);
        	DELTAT.reg = regs;
            DELTAT.regPtr = regPtr;

            // current rom data
            if (DELTAT.memory != null)
                DELTAT.now_data = DELTAT.memory[(DELTAT.now_addr >> 1)];

        }
        public void YM_DELTAT_savestate(fm.device_config device, YM_DELTAT DELTAT) { }
        public void YM_DELTAT_savestate(YM_DELTAT DELTAT)
        {
        //#ifdef __STATE_H__
        //	state_save_register_device_item(device, 0, DELTAT.portstate);
        //	state_save_register_device_item(device, 0, DELTAT.now_addr);
        //	state_save_register_device_item(device, 0, DELTAT.now_step);
        //	state_save_register_device_item(device, 0, DELTAT.acc);
        //	state_save_register_device_item(device, 0, DELTAT.prev_acc);
        //	state_save_register_device_item(device, 0, DELTAT.adpcmd);
        //	state_save_register_device_item(device, 0, DELTAT.adpcml);
        //#endif
        }


        private void YM_DELTAT_Limit(ref int val, int max, int min)
        {
            if (val > max) val = max;
            else if (val < min) val = min;
        }

        //INLINE
        private void YM_DELTAT_synthesis_from_external_memory(YM_DELTAT DELTAT)
        {
            uint step;
            int data;

            DELTAT.now_step += DELTAT.step;
            if (DELTAT.now_step >= (1 << YM_DELTAT_SHIFT))
            {
                step = DELTAT.now_step >> YM_DELTAT_SHIFT;
                DELTAT.now_step &= (1 << YM_DELTAT_SHIFT) - 1;
                do
                {

                    if (DELTAT.now_addr == (DELTAT.limit << 1))
                        DELTAT.now_addr = 0;

                    if (DELTAT.now_addr == (DELTAT.end << 1))
                    {   /* 12-06-2001 JB: corrected comparison. Was > instead of == */
                        if ((DELTAT.portstate & 0x10) != 0)
                        {
                            /* repeat start */
                            DELTAT.now_addr = DELTAT.start << 1;
                            DELTAT.acc = 0;
                            DELTAT.adpcmd = YM_DELTAT_DELTA_DEF;
                            DELTAT.prev_acc = 0;
                        }
                        else
                        {
                            /* set EOS bit in status register */
                            if (DELTAT.status_set_handler != null)
                                if (DELTAT.status_change_EOS_bit != 0)
                                    DELTAT.status_set_handler(DELTAT.status_change_which_chip, DELTAT.status_change_EOS_bit);

                            /* clear PCM BUSY bit (reflected in status register) */
                            DELTAT.PCM_BSY = 0;

                            DELTAT.portstate = 0;
                            DELTAT.adpcml = 0;
                            DELTAT.prev_acc = 0;
                            return;
                        }
                    }

                    if ((DELTAT.now_addr & 1) != 0) data = DELTAT.now_data & 0x0f;
                    else
                    {
                        DELTAT.now_data = DELTAT.memory[(DELTAT.now_addr >> 1)];
                        data = DELTAT.now_data >> 4;
                    }

                    DELTAT.now_addr++;
                    /* 12-06-2001 JB: */
                    /* YM2610 address register is 24 bits wide.*/
                    /* The "+1" is there because we use 1 bit more for nibble calculations.*/
                    /* WARNING: */
                    /* Side effect: we should take the size of the mapped ROM into account */
                    //DELTAT.now_addr &= ( (1<<(24+1))-1);
                    DELTAT.now_addr &= DELTAT.memory_mask;


                    /* store accumulator value */
                    DELTAT.prev_acc = DELTAT.acc;

                    /* Forecast to next Forecast */
                    DELTAT.acc += (ym_deltat_decode_tableB1[data] * DELTAT.adpcmd / 8);
                    YM_DELTAT_Limit(ref DELTAT.acc, YM_DELTAT_DECODE_MAX, YM_DELTAT_DECODE_MIN);

                    /* delta to next delta */
                    DELTAT.adpcmd = (DELTAT.adpcmd * ym_deltat_decode_tableB2[data]) / 64;
                    YM_DELTAT_Limit(ref DELTAT.adpcmd, YM_DELTAT_DELTA_MAX, YM_DELTAT_DELTA_MIN);

                    /* ElSemi: Fix interpolator. */
                    /*DELTAT.prev_acc = prev_acc + ((DELTAT.acc - prev_acc) / 2 );*/

                } while ((--step) != 0);

            }

            /* ElSemi: Fix interpolator. */
            DELTAT.adpcml = DELTAT.prev_acc * (int)((1 << YM_DELTAT_SHIFT) - DELTAT.now_step);
            DELTAT.adpcml += (DELTAT.acc * (int)DELTAT.now_step);
            DELTAT.adpcml = (DELTAT.adpcml >> YM_DELTAT_SHIFT) * (int)DELTAT.volume;

            /* output for work of output channels (outd[OPNxxxx])*/
            DELTAT.pan[DELTAT.panPtr] += DELTAT.adpcml;
        }



        //INLINE
        private void YM_DELTAT_synthesis_from_CPU_memory(YM_DELTAT DELTAT)
        {
            uint step;
            int data;

            DELTAT.now_step += DELTAT.step;
            if (DELTAT.now_step >= (1 << YM_DELTAT_SHIFT))
            {
                step = DELTAT.now_step >> YM_DELTAT_SHIFT;
                DELTAT.now_step &= (1 << YM_DELTAT_SHIFT) - 1;
                do
                {

                    if ((DELTAT.now_addr & 1) != 0)
                    {
                        data = DELTAT.now_data & 0x0f;

                        DELTAT.now_data = DELTAT.CPU_data;

                        /* after we used CPU_data, we set BRDY bit in status register,
                        * which means we are ready to accept another byte of data */
                        if (DELTAT.status_set_handler != null)
                            if (DELTAT.status_change_BRDY_bit != 0)
                                DELTAT.status_set_handler(DELTAT.status_change_which_chip, DELTAT.status_change_BRDY_bit);
                    }
                    else
                    {
                        data = DELTAT.now_data >> 4;
                    }

                    DELTAT.now_addr++;

                    /* store accumulator value */
                    DELTAT.prev_acc = DELTAT.acc;

                    /* Forecast to next Forecast */
                    DELTAT.acc += (ym_deltat_decode_tableB1[data] * DELTAT.adpcmd / 8);
                    YM_DELTAT_Limit(ref DELTAT.acc, YM_DELTAT_DECODE_MAX, YM_DELTAT_DECODE_MIN);

                    /* delta to next delta */
                    DELTAT.adpcmd = (DELTAT.adpcmd * ym_deltat_decode_tableB2[data]) / 64;
                    YM_DELTAT_Limit(ref DELTAT.adpcmd, YM_DELTAT_DELTA_MAX, YM_DELTAT_DELTA_MIN);


                } while ((--step) != 0);

            }

            /* ElSemi: Fix interpolator. */
            DELTAT.adpcml = DELTAT.prev_acc * (int)((1 << YM_DELTAT_SHIFT) - DELTAT.now_step);
            DELTAT.adpcml += (DELTAT.acc * (int)DELTAT.now_step);
            DELTAT.adpcml = (DELTAT.adpcml >> YM_DELTAT_SHIFT) * (int)DELTAT.volume;

            /* output for work of output channels (outd[OPNxxxx])*/
            DELTAT.pan[DELTAT.panPtr] += DELTAT.adpcml;
        }



        /* ADPCM B (Delta-T control type) */
        public void YM_DELTAT_ADPCM_CALC(YM_DELTAT DELTAT)
        {

            /*
            some examples:
            value:   START, REC, MEMDAT, REPEAT, SPOFF, x,x,RESET   meaning:
              80     1      0    0       0       0      0 0 0       Synthesis (playing) from CPU (from reg $08) to AUDIO,sample rate in DELTA-N register
              a0     1      0    1       0       0      0 0 0       Synthesis (playing) from EXT.MEMORY to AUDIO,        sample rate in DELTA-N register
              C8     1      1    0       0       1      0 0 0       Analysis (recording) from AUDIO to CPU (to reg $08), sample rate in PRESCALER register
              E8     1      1    1       0       1      0 0 0       Analysis (recording) from AUDIO to EXT.MEMORY,       sample rate in PRESCALER register

              60     0      1    1       0       0      0 0 0       External memory write via ADPCM data register $08
              20     0      0    1       0       0      0 0 0       External memory read via ADPCM data register $08

            */

            if ((DELTAT.portstate & 0xe0) == 0xa0)
            {
                YM_DELTAT_synthesis_from_external_memory(DELTAT);
                return;
            }

            if ((DELTAT.portstate & 0xe0) == 0x80)
            {
                /* ADPCM synthesis from CPU-managed memory (from reg $08) */
                YM_DELTAT_synthesis_from_CPU_memory(DELTAT);    /* change output based on data in ADPCM data reg ($08) */
                return;
            }

            //todo: ADPCM analysis
            //  if ( (DELTAT.portstate & 0xe0)==0xc0 )
            //  if ( (DELTAT.portstate & 0xe0)==0xe0 )

            return;
        }

        public void YM_DELTAT_calc_mem_mask(YM_DELTAT DELTAT)
        {
            uint MaskSize;

            MaskSize = 0x01;
            while (MaskSize < DELTAT.memory_size)
                MaskSize <<= 1;

            DELTAT.memory_mask = (MaskSize << 1) - 1;  // it's Mask<<1 because of the nibbles

            return;
        }
    }
}