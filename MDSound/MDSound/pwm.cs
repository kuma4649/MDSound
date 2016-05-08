/***************************************************************************
 * Gens: PWM audio emulator.                                               *
 *                                                                         *
 * Copyright (c) 1999-2002 by St駱hane Dallongeville                       *
 * Copyright (c) 2003-2004 by St駱hane Akhoun                              *
 * Copyright (c) 2008-2009 by David Korth                                  *
 *                                                                         *
 * This program is free software; you can redistribute it and/or modify it *
 * under the terms of the GNU General Public License as published by the   *
 * Free Software Foundation; either version 2 of the License, or (at your  *
 * option) any later version.                                              *
 *                                                                         *
 * This program is distributed in the hope that it will be useful, but     *
 * WITHOUT ANY WARRANTY; without even the implied warranty of              *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the           *
 * GNU General Public License for more details.                            *
 *                                                                         *
 * You should have received a copy of the GNU General Public License along *
 * with this program; if not, write to the Free Software Foundation, Inc., *
 * 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.           *
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDSound
{
    public class pwm
    {
        private const int PWM_BUF_SIZE = 4;

        //# include "mamedef.h"
        //# include "pwm.h"

        //# include <string.h>

        //#include "gens_core/mem/mem_sh2.h"
        //#include "gens_core/cpu/sh2/sh2.h"

        private const int CHILLY_WILLY_SCALE = 1;

        //#if PWM_BUF_SIZE == 8
        //        unsigned char PWM_FULL_TAB[PWM_BUF_SIZE * PWM_BUF_SIZE] =
        //        {
        //    0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80,
        //    0x80, 0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        //    0x00, 0x80, 0x40, 0x00, 0x00, 0x00, 0x00, 0x00,
        //    0x00, 0x00, 0x80, 0x40, 0x00, 0x00, 0x00, 0x00,
        //    0x00, 0x00, 0x00, 0x80, 0x40, 0x00, 0x00, 0x00,
        //    0x00, 0x00, 0x00, 0x00, 0x80, 0x40, 0x00, 0x00,
        //    0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x40, 0x00,
        //    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x40,
        //};
        //#elif PWM_BUF_SIZE == 4
        private byte[] PWM_FULL_TAB = new byte[PWM_BUF_SIZE * PWM_BUF_SIZE]
        {
            0x40, 0x00, 0x00, 0x80,
            0x80, 0x40, 0x00, 0x00,
            0x00, 0x80, 0x40, 0x00,
            0x00, 0x00, 0x80, 0x40,
        };
        //#else
        //#error PWM_BUF_SIZE must equal 4 or 8.
        //#endif /* PWM_BUF_SIZE */

        public class pwm_chip
        {
            public ushort[] PWM_FIFO_R = new ushort[8];
            public ushort[] PWM_FIFO_L = new ushort[8];
            public uint PWM_RP_R;
            public uint PWM_WP_R;
            public uint PWM_RP_L;
            public uint PWM_WP_L;
            public uint PWM_Cycles;
            public uint PWM_Cycle;
            public uint PWM_Cycle_Cnt;
            public uint PWM_Int;
            public uint PWM_Int_Cnt;
            public uint PWM_Mode;
            //unsigned int PWM_Enable;
            public uint PWM_Out_R;
            public uint PWM_Out_L;

            public uint PWM_Cycle_Tmp;
            public uint PWM_Cycles_Tmp;
            public uint PWM_Int_Tmp;
            public uint PWM_FIFO_L_Tmp;
            public uint PWM_FIFO_R_Tmp;

            //#if CHILLY_WILLY_SCALE
            // TODO: Fix Chilly Willy's new scaling algorithm.
            /* PWM scaling variables. */
            public int PWM_Offset;
            public int PWM_Scale;
            //int PWM_Loudness;
            //#endif

            public int clock;
        };
        //#if CHILLY_WILLY_SCALE
        // TODO: Fix Chilly Willy's new scaling algorithm.
        private const int PWM_Loudness = 0;
        //#endif

        //void PWM_Init(pwm_chip* chip);
        //void PWM_Recalc_Scale(pwm_chip* chip);

        //void PWM_Set_Cycle(pwm_chip* chip, unsigned int cycle);
        //void PWM_Set_Int(pwm_chip* chip, unsigned int int_time);

        //void PWM_Update(pwm_chip* chip, int** buf, int length);


        private const byte CHIP_SAMPLING_MODE = 0;
        private const int CHIP_SAMPLE_RATE = 0;
        private const int MAX_CHIPS = 0x02;
        private pwm_chip[] PWM_Chip = new pwm_chip[MAX_CHIPS] { new pwm_chip(), new pwm_chip() };

        /**
         * PWM_Init(): Initialize the PWM audio emulator.
         */
        public void PWM_Init(pwm_chip chip)
        {
            chip.PWM_Mode = 0;
            chip.PWM_Out_R = 0;
            chip.PWM_Out_L = 0;

            for (int i = 0; i < 8; i++)
            {
                chip.PWM_FIFO_R[i] = 0x00;
                chip.PWM_FIFO_L[i] = 0x00;
            }
            chip.PWM_RP_R = 0;
            chip.PWM_WP_R = 0;
            chip.PWM_RP_L = 0;
            chip.PWM_WP_L = 0;
            chip.PWM_Cycle_Tmp = 0;
            chip.PWM_Int_Tmp = 0;
            chip.PWM_FIFO_L_Tmp = 0;
            chip.PWM_FIFO_R_Tmp = 0;

            //PWM_Loudness = 0;
            PWM_Set_Cycle(chip, 0);
            PWM_Set_Int(chip, 0);
        }


        //#if CHILLY_WILLY_SCALE
        // TODO: Fix Chilly Willy's new scaling algorithm.
        public void PWM_Recalc_Scale(pwm_chip chip)
        {
            chip.PWM_Offset = ((int)chip.PWM_Cycle / 2) + 1;
            chip.PWM_Scale = (0x7FFF00 / chip.PWM_Offset);
        }
        //#endif


        public void PWM_Set_Cycle(pwm_chip chip, uint cycle)
        {
            cycle--;
            chip.PWM_Cycle = (cycle & 0xFFF);
            chip.PWM_Cycle_Cnt = chip.PWM_Cycles;

            //#if CHILLY_WILLY_SCALE
            // TODO: Fix Chilly Willy's new scaling algorithm.
            PWM_Recalc_Scale(chip);
            //#endif
        }


        public void PWM_Set_Int(pwm_chip chip, uint int_time)
        {
            int_time &= 0x0F;
            if (int_time != 0)
                chip.PWM_Int = chip.PWM_Int_Cnt = int_time;
            else
                chip.PWM_Int = chip.PWM_Int_Cnt = 16;
        }


        public void PWM_Clear_Timer(pwm_chip chip)
        {
            chip.PWM_Cycle_Cnt = 0;
        }


        /**
         * PWM_SHIFT(): Shift PWM data.
         * @param src: Channel (L or R) with the source data.
         * @param dest Channel (L or R) for the destination.
         */
        //#define PWM_SHIFT(src, dest)										\
        //{													\
        //	/* Make sure the source FIFO isn't empty. */							\
        //	if (PWM_RP_##src != PWM_WP_##src)								\
        //	{												\
        //		/* Get destination channel output from the source channel FIFO. */			\
        //		PWM_Out_##dest = PWM_FIFO_##src[PWM_RP_##src];						\
        //													\
        //		/* Increment the source channel read pointer, resetting to 0 if it overflows. */	\
        //		PWM_RP_##src = (PWM_RP_##src + 1) & (PWM_BUF_SIZE - 1);					\
        //	}												\
        //}


        /*static void PWM_Shift_Data(void)
        {
            switch (PWM_Mode & 0x0F)
            {
                case 0x01:
                case 0x0D:
                    // Rx_LL: Right -> Ignore, Left -> Left
                    PWM_SHIFT(L, L);
                    break;

                case 0x02:
                case 0x0E:
                    // Rx_LR: Right -> Ignore, Left -> Right
                    PWM_SHIFT(L, R);
                    break;

                case 0x04:
                case 0x07:
                    // RL_Lx: Right -> Left, Left -> Ignore
                    PWM_SHIFT(R, L);
                    break;

                case 0x05:
                case 0x09:
                    // RR_LL: Right -> Right, Left -> Left
                    PWM_SHIFT(L, L);
                    PWM_SHIFT(R, R);
                    break;

                case 0x06:
                case 0x0A:
                    // RL_LR: Right -> Left, Left -> Right
                    PWM_SHIFT(L, R);
                    PWM_SHIFT(R, L);
                    break;

                case 0x08:
                case 0x0B:
                    // RR_Lx: Right -> Right, Left -> Ignore
                    PWM_SHIFT(R, R);
                    break;

                case 0x00:
                case 0x03:
                case 0x0C:
                case 0x0F:
                default:
                    // Rx_Lx: Right -> Ignore, Left -> Ignore
                    break;
            }
        }


        void PWM_Update_Timer(unsigned int cycle)
        {
            // Don't do anything if PWM is disabled in the Sound menu.

            // Don't do anything if PWM isn't active.
            if ((PWM_Mode & 0x0F) == 0x00)
                return;

            if (PWM_Cycle == 0x00 || (PWM_Cycle_Cnt > cycle))
                return;

            PWM_Shift_Data();

            PWM_Cycle_Cnt += PWM_Cycle;

            PWM_Int_Cnt--;
            if (PWM_Int_Cnt == 0)
            {
                PWM_Int_Cnt = PWM_Int;

                if (PWM_Mode & 0x0080)
                {
                    // RPT => generate DREQ1 as well as INT
                    SH2_DMA1_Request(&M_SH2, 1);
                    SH2_DMA1_Request(&S_SH2, 1);
                }

                if (_32X_MINT & 1)
                    SH2_Interrupt(&M_SH2, 6);
                if (_32X_SINT & 1)
                    SH2_Interrupt(&S_SH2, 6);
            }
        }*/


        public int PWM_Update_Scale(pwm_chip chip, int PWM_In)
        {
            if (PWM_In == 0)
                return 0;

            // TODO: Chilly Willy's new scaling algorithm breaks drx's Sonic 1 32X (with PWM drums).
            //# ifdef CHILLY_WILLY_SCALE
            //return (((PWM_In & 0xFFF) - chip->PWM_Offset) * chip->PWM_Scale) >> (8 - PWM_Loudness);
            // Knuckles' Chaotix: Tachy Touch uses the values 0xF?? for negative values
            // This small modification fixes the terrible pops.
            PWM_In &= 0xFFF;
            if ((PWM_In & 0x800) != 0)
                PWM_In |= ~0xFFF;
            return ((PWM_In - chip.PWM_Offset) * chip.PWM_Scale) >> (8 - PWM_Loudness);
            //#else
            //    const int PWM_adjust = ((chip->PWM_Cycle >> 1) + 1);
            //    int PWM_Ret = ((chip->PWM_In & 0xFFF) - PWM_adjust);

            //    // Increase PWM volume so it's audible.
            //    PWM_Ret <<= (5 + 2);

            //    // Make sure the PWM isn't oversaturated.
            //    if (PWM_Ret > 32767)
            //        PWM_Ret = 32767;
            //    else if (PWM_Ret < -32768)
            //        PWM_Ret = -32768;

            //    return PWM_Ret;
            //#endif
        }


        public void PWM_Update(pwm_chip chip, int[][] buf, int length)
        {
            int tmpOutL;
            int tmpOutR;
            int i;

            //if (!PWM_Enable)
            //	return;

            if (chip.PWM_Out_L == 0 && chip.PWM_Out_R == 0)
            {
                for (i = 0; i < length; i++)
                {
                    buf[0][i] = 0;
                    buf[1][i] = 0;
                }
                return;
            }

            // New PWM scaling algorithm provided by Chilly Willy on the Sonic Retro forums.
            tmpOutL = PWM_Update_Scale(chip, (int)chip.PWM_Out_L);
            tmpOutR = PWM_Update_Scale(chip, (int)chip.PWM_Out_R);

            for (i = 0; i < length; i++)
            {
                buf[0][i] = tmpOutL;
                buf[1][i] = tmpOutR;
            }
        }


        public void pwm_update(byte ChipID, int[][] outputs, int samples)
        {
            pwm_chip chip = PWM_Chip[ChipID];

            PWM_Update(chip, outputs, samples);
        }

        public int device_start_pwm(byte ChipID, uint clock)
        {
            /* allocate memory for the chip */
            //pwm_state *chip = get_safe_token(device);
            pwm_chip chip;
            int rate;

            if (ChipID >= MAX_CHIPS)
                return 0;

            chip = PWM_Chip[ChipID];
            rate = 22020;   // that's the rate the PWM is mostly used
            if ((CHIP_SAMPLING_MODE == 0x01 && rate < CHIP_SAMPLE_RATE) ||
                CHIP_SAMPLING_MODE == 0x02)
                rate = CHIP_SAMPLE_RATE;
            chip.clock = (int)clock;

            PWM_Init(chip);
            /* allocate the stream */
            //chip->stream = stream_create(device, 0, 2, device->clock / 384, chip, rf5c68_update);

            return rate;
        }

        public void device_stop_pwm(byte ChipID)
        {
            //pwm_chip *chip = &PWM_Chip[ChipID];
            //free(chip->ram);

            return;
        }

        public void device_reset_pwm(byte ChipID)
        {
            pwm_chip chip = PWM_Chip[ChipID];
            PWM_Init(chip);
        }

        public void pwm_chn_w(byte ChipID, byte Channel, uint data)
        {
            pwm_chip chip = PWM_Chip[ChipID];

            if (chip.clock == 1)
            {   // old-style commands
                switch (Channel)
                {
                    case 0x00:
                        chip.PWM_Out_L = data;
                        break;
                    case 0x01:
                        chip.PWM_Out_R = data;
                        break;
                    case 0x02:
                        PWM_Set_Cycle(chip, data);
                        break;
                    case 0x03:
                        chip.PWM_Out_L = data;
                        chip.PWM_Out_R = data;
                        break;
                }
            }
            else
            {
                switch (Channel)
                {
                    case 0x00 / 2:  // control register
                        PWM_Set_Int(chip, data >> 8);
                        break;
                    case 0x02 / 2:  // cycle register
                        PWM_Set_Cycle(chip, data);
                        break;
                    case 0x04 / 2:  // l ch
                        chip.PWM_Out_L = data;
                        break;
                    case 0x06 / 2:  // r ch
                        chip.PWM_Out_R = data;
                        if (chip.PWM_Mode == 0)
                        {
                            if (chip.PWM_Out_L == chip.PWM_Out_R)
                            {
                                // fixes these terrible pops when
                                // starting/stopping/pausing the song
                                chip.PWM_Offset = (int)data;
                                chip.PWM_Mode = 0x01;
                            }
                        }
                        break;
                    case 0x08 / 2:  // mono ch
                        chip.PWM_Out_L = data;
                        chip.PWM_Out_R = data;
                        if (chip.PWM_Mode == 0)
                        {
                            chip.PWM_Offset = (int)data;
                            chip.PWM_Mode = 0x01;
                        }
                        break;
                }
            }

            return;
        }
    }
}
