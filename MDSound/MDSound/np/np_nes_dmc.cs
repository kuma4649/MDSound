using MDSound.np.cpu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDSound.np
{
    public class np_nes_dmc
    {
        //
        // NSFPlay tag2.4のソースを移植 2021/08/14
        //
        // https://github.com/bbbradsmith/nsfplay/releases/tag/2.4
        //


        public np_nes_apu nes_apu = null;
        Random rnd = new System.Random();

        // Ported from NSFPlay to VGMPlay (including C++ -> C conversion)
        // by Valley Bell on 25 September 2013
        // Updated to NSFPlay 2.3 on 26 September 2013
        // (Note: Encoding is UTF-8)

        //# include <stdlib.h>	// for rand
        //# include <stdlib.h>
        //# include <string.h>	// for memset
        //# include <stddef.h>	// for NULL
        //# include "mamedef.h"
        //# include "../stdbool.h"
        //# include "np_nes_apu.h"	// for NES_APU_np_FrameSequence
        //# include "np_nes_dmc.h"


        // Master Clock: 21477272 (NTSC)
        // APU Clock = Master Clock / 12
        private const double DEFAULT_CLOCK = 1789772.0;
        private const Int32 DEFAULT_CLK_PAL = 1662607;
        private const Int32 DEFAULT_RATE = 44100;


        /** Bottom Half of APU **/
        public enum OPT : int
        {
            OPT_ENABLE_4011 = 0,
            OPT_ENABLE_PNOISE,
            OPT_UNMUTE_ON_RESET,
            OPT_DPCM_ANTI_CLICK,
            OPT_NONLINEAR_MIXER,
            OPT_RANDOMIZE_NOISE,
            OPT_TRI_MUTE,
            OPT_RANDOMIZE_TRI,
            OPT_DPCM_REVERSE,
            OPT_END
        };


        // Note: For increased speed, I'll inline all of NSFPlay's Counter member functions.
        private const Int32 COUNTER_SHIFT = 24;

        public Counter counter = new Counter();
        public class Counter
        {
            public double ratio;
            public UInt32 val, step;
        }

        private void COUNTER_setcycle(Counter cntr, Int32 s)
        {
            cntr.step = (UInt32)(cntr.ratio / (s + 1));
        }

        private void COUNTER_iup(Counter cntr)
        {
            cntr.val += cntr.step;
        }

        private UInt32 COUNTER_value(Counter cntr)
        {
            return (cntr.val >> COUNTER_SHIFT);
        }

        private void COUNTER_init(Counter cntr, double clk, double rate)
        {
            (cntr).ratio = (1 << COUNTER_SHIFT) * (1.0 * clk / rate);
            (cntr).step = (UInt32)((cntr).ratio + 0.5);
            (cntr).val = 0;
        }


        public class NES_DMC
        {
            //const int GETA_BITS;
            //static const UINT32 freq_table[2][16];
            //static const UINT32 wavlen_table[2][16];
            public UInt32[][][][] tnd_table;            //[2][16][16][128];

            public Int32[] option = new Int32[10];// OPT_END];
            public Int32 mask;
            public Int32[][] sm = new Int32[2][] { new Int32[3], new Int32[3] };
            public byte[] reg = new byte[0x20];
            public UInt32 len_reg;
            public UInt32 adr_reg;
            public IDevice org_memory;
            public byte[] memory;
            public Int32 ptrMemory;
            public UInt32[] _out = new UInt32[3];
            public UInt32 daddress;
            public UInt32 dlength;
            public UInt32 data;
            public bool empty;
            public Int16 damp;
            public Int32 dac_lsb;
            public bool dmc_pop;
            public Int32 dmc_pop_offset;
            public Int32 dmc_pop_follow;
            public double clock;
            public UInt32 rate;
            public Int32 pal;
            public Int32 mode;
            public bool irq;

            public Int32[] counter = new Int32[3];  // frequency dividers
            public Int32 tphase;         // triangle phase
            public UInt32 nfreq;       // noise frequency
            public UInt32 dfreq;       // DPCM frequency

            public UInt32 tri_freq;
            public Int32 linear_counter;
            public Int32 linear_counter_reload;
            public bool linear_counter_halt;
            public bool linear_counter_control;

            public Int32 noise_volume;
            public UInt32 noise, noise_tap;

            // noise envelope
            public bool envelope_loop;
            public bool envelope_disable;
            public bool envelope_write;
            public Int32 envelope_div_period;
            public Int32 envelope_div;
            public Int32 envelope_counter;

            public bool[] enable = new bool[2];
            public Int32[] length_counter = new Int32[2];  // 0=tri, 1=noise

            //public TrackInfoBasic[] trkinfo = new TrackInfoBasic[3];

            // frame sequencer
            public np_nes_apu.NES_APU apu;  // apu is clocked by DMC's frame sequencer
            public Int32 frame_sequence_count;   // current cycle count
            public Int32 frame_sequence_length;  // CPU cycles per FrameSequence
            public Int32 frame_sequence_step;    // current step of frame sequence
            public Int32 frame_sequence_steps;   // 4/5 steps per frame
            public bool frame_irq;
            public bool frame_irq_enable;

            //public NES_CPU cpu; // IRQ needs CPU access

            public Counter tick_count = new Counter();
            public UInt32 tick_last;
            internal km6502 cpu;
        }
        //INLINE UINT32 calc_tri(NES_DMC* dmc, UINT32 clocks);
        //INLINE UINT32 calc_dmc(NES_DMC* dmc, UINT32 clocks);
        //INLINE UINT32 calc_noise(NES_DMC* dmc, UINT32 clocks);
        //static void FrameSequence(NES_DMC* dmc, int s);
        private int GetDamp(NES_DMC dmc) { return (dmc.damp << 1) | dmc.dac_lsb; }
        //static void TickFrameSequence(NES_DMC* dmc, UINT32 clocks);
        //static void Tick(NES_DMC* dmc, UINT32 clocks);

        private const Int32 GETA_BITS = 20;
        private UInt32[][] wavlen_table = new UInt32[2][]{
            new UInt32[16]{	// NTSC
                4, 8, 16, 32, 64, 96, 128, 160, 202, 254, 380, 508, 762, 1016, 2034, 4068
            },
            new UInt32[16]{	// PAL
                4, 8, 14, 30, 60, 88, 118, 148, 188, 236, 354, 472, 708,  944, 1890, 3778
            }
        };

        private UInt32[][] freq_table = new UInt32[2][]  {
            new UInt32[16]{	// NTSC
                428, 380, 340, 320, 286, 254, 226, 214, 190, 160, 142, 128, 106, 84, 72, 54
            },
            new UInt32[16]{	// PAL
                398, 354, 316, 298, 276, 236, 210, 198, 176, 148, 132, 118,  98, 78, 66, 50
            }
        };

        private UInt32[] BITREVERSE = new uint[256]{
            0x00, 0x80, 0x40, 0xC0, 0x20, 0xA0, 0x60, 0xE0, 0x10, 0x90, 0x50, 0xD0, 0x30, 0xB0, 0x70, 0xF0,
            0x08, 0x88, 0x48, 0xC8, 0x28, 0xA8, 0x68, 0xE8, 0x18, 0x98, 0x58, 0xD8, 0x38, 0xB8, 0x78, 0xF8,
            0x04, 0x84, 0x44, 0xC4, 0x24, 0xA4, 0x64, 0xE4, 0x14, 0x94, 0x54, 0xD4, 0x34, 0xB4, 0x74, 0xF4,
            0x0C, 0x8C, 0x4C, 0xCC, 0x2C, 0xAC, 0x6C, 0xEC, 0x1C, 0x9C, 0x5C, 0xDC, 0x3C, 0xBC, 0x7C, 0xFC,
            0x02, 0x82, 0x42, 0xC2, 0x22, 0xA2, 0x62, 0xE2, 0x12, 0x92, 0x52, 0xD2, 0x32, 0xB2, 0x72, 0xF2,
            0x0A, 0x8A, 0x4A, 0xCA, 0x2A, 0xAA, 0x6A, 0xEA, 0x1A, 0x9A, 0x5A, 0xDA, 0x3A, 0xBA, 0x7A, 0xFA,
            0x06, 0x86, 0x46, 0xC6, 0x26, 0xA6, 0x66, 0xE6, 0x16, 0x96, 0x56, 0xD6, 0x36, 0xB6, 0x76, 0xF6,
            0x0E, 0x8E, 0x4E, 0xCE, 0x2E, 0xAE, 0x6E, 0xEE, 0x1E, 0x9E, 0x5E, 0xDE, 0x3E, 0xBE, 0x7E, 0xFE,
            0x01, 0x81, 0x41, 0xC1, 0x21, 0xA1, 0x61, 0xE1, 0x11, 0x91, 0x51, 0xD1, 0x31, 0xB1, 0x71, 0xF1,
            0x09, 0x89, 0x49, 0xC9, 0x29, 0xA9, 0x69, 0xE9, 0x19, 0x99, 0x59, 0xD9, 0x39, 0xB9, 0x79, 0xF9,
            0x05, 0x85, 0x45, 0xC5, 0x25, 0xA5, 0x65, 0xE5, 0x15, 0x95, 0x55, 0xD5, 0x35, 0xB5, 0x75, 0xF5,
            0x0D, 0x8D, 0x4D, 0xCD, 0x2D, 0xAD, 0x6D, 0xED, 0x1D, 0x9D, 0x5D, 0xDD, 0x3D, 0xBD, 0x7D, 0xFD,
            0x03, 0x83, 0x43, 0xC3, 0x23, 0xA3, 0x63, 0xE3, 0x13, 0x93, 0x53, 0xD3, 0x33, 0xB3, 0x73, 0xF3,
            0x0B, 0x8B, 0x4B, 0xCB, 0x2B, 0xAB, 0x6B, 0xEB, 0x1B, 0x9B, 0x5B, 0xDB, 0x3B, 0xBB, 0x7B, 0xFB,
            0x07, 0x87, 0x47, 0xC7, 0x27, 0xA7, 0x67, 0xE7, 0x17, 0x97, 0x57, 0xD7, 0x37, 0xB7, 0x77, 0xF7,
            0x0F, 0x8F, 0x4F, 0xCF, 0x2F, 0xAF, 0x6F, 0xEF, 0x1F, 0x9F, 0x5F, 0xDF, 0x3F, 0xBF, 0x7F, 0xFF,
        };

        public NES_DMC NES_DMC_np_Create(Int32 clock, Int32 rate)
        {
            NES_DMC dmc;
            int c, t;

            dmc = new NES_DMC();// (NES_DMC*)malloc(sizeof(NES_DMC));
            if (dmc == null)
                return null;
            //memset(dmc, 0x00, sizeof(NES_DMC));

            //NES_DMC_np_SetClock(dmc, DEFAULT_CLOCK);
            //NES_DMC_np_SetRate(dmc, DEFAULT_RATE);
            //NES_DMC_np_SetPal(dmc, false);
            NES_DMC_np_SetClock(dmc, clock);    // does SetPal, too
            NES_DMC_np_SetRate(dmc, rate);
            dmc.option[(int)OPT.OPT_ENABLE_4011] = 1;
            dmc.option[(int)OPT.OPT_ENABLE_PNOISE] = 1;
            dmc.option[(int)OPT.OPT_UNMUTE_ON_RESET] = 1;
            dmc.option[(int)OPT.OPT_DPCM_ANTI_CLICK] = 0;
            dmc.option[(int)OPT.OPT_NONLINEAR_MIXER] = 1;
            dmc.option[(int)OPT.OPT_RANDOMIZE_NOISE] = 1;
            dmc.option[(int)OPT.OPT_RANDOMIZE_TRI] = 1;
            dmc.option[(int)OPT.OPT_TRI_MUTE] = 1;
            dmc.option[(int)OPT.OPT_DPCM_REVERSE] = 0;

            dmc.tnd_table = new UInt32[2][][][];
            for (int i = 0; i < 2; i++)
            {
                dmc.tnd_table[i] = new UInt32[16][][];
                for (int j = 0; j < 16; j++)
                {
                    dmc.tnd_table[i][j] = new UInt32[16][];
                    for (int k = 0; k < 16; k++)
                    {
                        dmc.tnd_table[i][j][k] = new UInt32[128];
                    }
                }
            }
            dmc.tnd_table[0][0][0][0] = 0;
            dmc.tnd_table[1][0][0][0] = 0;

            dmc.apu = null;
            dmc.frame_sequence_count = 0;
            dmc.frame_sequence_length = 7458;
            dmc.frame_sequence_steps = 4;

            for (c = 0; c < 2; ++c)
                for (t = 0; t < 3; ++t)
                    dmc.sm[c][t] = 128;

            return dmc;
        }


        public void NES_DMC_np_Destroy(NES_DMC chip)
        {
            chip = null;
            //free(chip);
        }

        private Int32 NES_DMC_np_GetDamp(NES_DMC chip)
        {
            NES_DMC dmc = chip;

            return (dmc.damp << 1) | dmc.dac_lsb;
        }

        public void NES_DMC_np_SetMask(NES_DMC chip, int m)
        {
            NES_DMC dmc = chip;

            dmc.mask = m;
        }

        public void NES_DMC_np_SetStereoMix(NES_DMC chip, Int32 trk, Int16 mixl, Int16 mixr)
        {
            NES_DMC dmc = chip;

            if (trk < 0) return;
            if (trk > 2) return;
            dmc.sm[0][trk] = mixl;
            dmc.sm[1][trk] = mixr;
        }

        //ITrackInfo* NES_DMC::GetTrackInfo(int trk)
        //{
        //    switch (trk)
        //    {
        //        case 0:
        //            trkinfo[trk].max_volume = 255;
        //            trkinfo[0].key = (linear_counter > 0 && length_counter[0] > 0 && enable[0]);
        //            trkinfo[0].volume = 0;
        //            trkinfo[0]._freq = tri_freq;
        //            if (trkinfo[0]._freq)
        //                trkinfo[0].freq = clock / 32 / (trkinfo[0]._freq + 1);
        //            else
        //                trkinfo[0].freq = 0;
        //            trkinfo[0].tone = -1;
        //            trkinfo[0].output = out[0];
        //            break;
        //        case 1:
        //            trkinfo[1].max_volume = 15;
        //            trkinfo[1].volume = noise_volume + (envelope_disable ? 0 : 0x10) + (envelope_loop ? 0x20 : 0);
        //            trkinfo[1].key = length_counter[1] > 0 && enable[1] &&
        //                             (envelope_disable ? (noise_volume > 0) : (envelope_counter > 0));
        //            trkinfo[1]._freq = reg[0x400e - 0x4008] & 0xF;
        //            trkinfo[1].freq = clock / double(wavlen_table[pal][trkinfo[1]._freq] * ((noise_tap & (1 << 6)) ? 93 : 1));
        //            trkinfo[1].tone = noise_tap & (1 << 6);
        //            trkinfo[1].output = out[1];
        //            break;
        //        case 2:
        //            trkinfo[2].max_volume = 127;
        //            trkinfo[2].volume = reg[0x4011 - 0x4008] & 0x7F;
        //            trkinfo[2].key = dlength > 0;
        //            trkinfo[2]._freq = reg[0x4010 - 0x4008] & 0xF;
        //            trkinfo[2].freq = clock / double(freq_table[pal][trkinfo[2]._freq]);
        //            trkinfo[2].tone = (0xc000 | (adr_reg << 6));
        //            trkinfo[2].output = (damp << 1) | dac_lsb;
        //            break;
        //        default:
        //            return NULL;
        //    }
        //    return &trkinfo[trk];
        //}

        private void FrameSequence(NES_DMC dmc, int s)
        {
            //DEBUG_OUT("FrameSequence: %d\n",s);

            if (s > 3) return;  // no operation in step 4

            if (dmc.apu != null)
            {
                nes_apu.NES_APU_np_FrameSequence(dmc.apu, s);
            }

            if (s == 0 && (dmc.frame_sequence_steps == 4))
            {
                if (dmc.frame_irq_enable) dmc.frame_irq = true;
                dmc.cpu?.UpdateIRQ(enmIRQ_devices.IRQD_FRAME, dmc.frame_irq && dmc.frame_irq_enable);
            }

            // 240hz clock
            {
                // triangle linear counter
                if (dmc.linear_counter_halt)
                {
                    dmc.linear_counter = dmc.linear_counter_reload;
                }
                else
                {
                    if (dmc.linear_counter > 0) --dmc.linear_counter;
                }
                if (!dmc.linear_counter_control)
                {
                    dmc.linear_counter_halt = false;
                }

                ////$4009 unuse address
                //dmc.reg[1] = (byte)(
                //    (dmc.linear_counter != 0 ? 4 : 0) //triangle
                //    | (dmc.length_counter[1]!=0 ? 8:0) //noise
                //    | (dmc.active ? 0x10 : 0) //dmc
                //    );

                // noise envelope
                bool divider = false;
                if (dmc.envelope_write)
                {
                    dmc.envelope_write = false;
                    dmc.envelope_counter = 15;
                    dmc.envelope_div = 0;
                }
                else
                {
                    ++dmc.envelope_div;
                    if (dmc.envelope_div > dmc.envelope_div_period)
                    {
                        divider = true;
                        dmc.envelope_div = 0;
                    }
                }
                if (divider)
                {
                    if (dmc.envelope_loop && dmc.envelope_counter == 0)
                        dmc.envelope_counter = 15;
                    else if (dmc.envelope_counter > 0)
                        --dmc.envelope_counter;    // TODO: Make this work.
                }
            }

            // 120hz clock
            if ((s & 1) == 0)
            {
                // triangle length counter
                if (!dmc.linear_counter_control && (dmc.length_counter[0] > 0))
                    --dmc.length_counter[0];

                // noise length counter
                if (!dmc.envelope_loop && (dmc.length_counter[1] > 0))
                    --dmc.length_counter[1];

            }

        }

        private UInt32[] tritbl = new UInt32[32]
        {
     15,14,13,12,11,10, 9, 8,
      7, 6, 5, 4, 3, 2, 1, 0,
      0, 1, 2, 3, 4, 5, 6, 7,
      8, 9,10,11,12,13,14,15
        };

        // 三角波チャンネルの計算 戻り値は0-15
        private UInt32 calc_tri(NES_DMC dmc, UInt32 clocks)
        {
            byte tri = 0;
            if (dmc.linear_counter > 0 && dmc.length_counter[0] > 0
                && (dmc.option[(Int32)OPT.OPT_TRI_MUTE] == 0 || dmc.tri_freq > 0))
            {
                tri = 1;
                dmc.counter[0] -= (Int32)clocks;
                while (dmc.counter[0] < 0)
                {
                    dmc.tphase = (dmc.tphase + 1) & 31;
                    dmc.counter[0] += (Int32)(dmc.tri_freq + 1);
                }
            }
            //// Note: else-block added by VB
            //else if (dmc.option[(Int32)OPT.OPT_TRI_NULL] != 0)
            //{
            //    if (dmc.tphase != 0 && dmc.tphase < 31)
            //    {
            //        // Finish the Triangle wave to prevent clicks.
            //        dmc.counter[0] += clocks;
            //        while (dmc.counter[0] > dmc.tri_freq && dmc.tphase != 0)
            //        {
            //            dmc.tphase = (dmc.tphase + 1) & 31;
            //            dmc.counter[0] -= (dmc.tri_freq + 1);
            //        }
            //    }
            //}

            dmc.reg[0x10] = tri;
            return tritbl[dmc.tphase];
        }

        // ノイズチャンネルの計算 戻り値は0-127
        // 低サンプリングレートで合成するとエイリアスノイズが激しいので
        // ノイズだけはこの関数内で高クロック合成し、簡易なサンプリングレート
        // 変換を行っている。
        private UInt32 calc_noise(NES_DMC dmc, UInt32 clocks)
        {
            byte noi = 1;

            UInt32 env, last, count, accum, clocks_accum;

            env = (UInt32)(dmc.envelope_disable ? dmc.noise_volume : dmc.envelope_counter);
            if (dmc.length_counter[1] < 1) { env = 0; }

            if (env == 0) noi = 0;
            dmc.reg[0x11] = noi;

            last = (dmc.noise & 0x4000) != 0 ? 0 : env;

            if (clocks < 1) return last;

            // simple anti-aliasing (noise requires it, even when oversampling is off)
            count = 0;
            accum = (UInt32)dmc.counter[1] * last;
            UInt32 accum_clocks = (UInt32)dmc.counter[1];
            //# ifdef _DEBUG
            //            INT32 start_clocks = counter[1];
            //#endif
            if (dmc.counter[1] < 0) // only happens on startup when using the randomize noise option
            {
                accum = 0;
                accum_clocks = 0;
            }

            dmc.counter[1] -= (Int32)clocks;
            //	assert(dmc->nfreq > 0);	// prevent infinite loop
            while (dmc.counter[1] < 0)
            {
                // tick the noise generator
                UInt32 feedback = (UInt32)((dmc.noise & 1) ^ (((dmc.noise & dmc.noise_tap) != 0) ? 1 : 0));
                dmc.noise = (dmc.noise >> 1) | (feedback << 14);

                last = (dmc.noise & 0x4000) != 0 ? 0 : env;
                accum += (last * dmc.nfreq);
                dmc.counter[1] += (Int32)dmc.nfreq;
                ++count;
                accum_clocks += dmc.nfreq;
            }

            if (count < 1) // no change over interval, don't anti-alias
            {
                return last;
            }

            accum -= (UInt32)(last * dmc.counter[1]); // remove these samples which belong in the next calc
            accum_clocks -= (UInt32)dmc.counter[1];
            //# ifdef _DEBUG
            //if (start_clocks >= 0) assert(accum_clocks == clocks); // these should be equal
            //#endif

            UInt32 average = accum / accum_clocks;
            //assert(average <= 15); // above this would indicate overflow
            return average;
        }

        // Tick the DMC for the number of clocks, and return output counter;
        private UInt32 calc_dmc(NES_DMC dmc, UInt32 clocks)
        {
            dmc.counter[2] -= (Int32)clocks;
            //assert(dfreq > 0); // prevent infinite loop
            while (dmc.counter[2] < 0)
            {
                dmc.counter[2] += (Int32)dmc.dfreq;

                if (dmc.data > 0x100) // data = 0x100 when shift register is empty
                {
                    if (!dmc.empty)
                    {
                        if ((dmc.data & 1) != 0 && (dmc.damp < 63))
                            dmc.damp++;
                        else if ((dmc.data & 1) == 0 && (0 < dmc.damp))
                            dmc.damp--;
                    }
                    dmc.data >>= 1;
                }

                if (dmc.data <= 0x100) // shift register is empty
                {
                    if (dmc.dlength > 0)
                    {
                        dmc.data = dmc.memory[dmc.daddress + dmc.ptrMemory];
                        //cpu->StealCycles(4); // DMC read takes 3 or 4 CPU cycles, usually 4
                        // (checking for the 3-cycle case would require sub-instruction emulation)
                        dmc.data &= 0xFF; // read 8 bits
                        if (dmc.option[(Int32)OPT.OPT_DPCM_REVERSE] != 0) dmc.data = BITREVERSE[dmc.data];
                        dmc.data |= 0x10000; // use an extra bit to signal end of data
                        dmc.empty = false;
                        dmc.daddress = ((dmc.daddress + 1) & 0xFFFF) | 0x8000;
                        --dmc.dlength;
                        if (dmc.dlength == 0)
                        {
                            if ((dmc.mode & 1) != 0) // looped DPCM = auto-reload
                            {
                                dmc.daddress = ((dmc.adr_reg << 6) | 0xC000);
                                dmc.dlength = (dmc.len_reg << 4) + 1;
                            }
                            else if ((dmc.mode & 2) != 0) // IRQ and not looped
                            {
                                dmc.irq = true;
                                dmc.cpu?.UpdateIRQ(enmIRQ_devices.IRQD_DMC, true);
                            }
                        }
                    }
                    else
                    {
                        dmc.data = 0x10000; // DMC will do nothing
                        dmc.empty = true;
                    }
                }
            }

            dmc.reg[0x12] = (byte)(dmc.empty ? 0 : 1);// dpc;
            return (uint)((dmc.damp << 1) + dmc.dac_lsb);
        }

        private UInt32 org_calc_dmc(NES_DMC dmc, UInt32 clocks)
        {
            dmc.counter[2] -= (Int32)clocks;
            //assert(dfreq > 0); // prevent infinite loop
            while (dmc.counter[2] < 0)
            {
                dmc.counter[2] += (Int32)dmc.dfreq;

                if (dmc.data > 0x100) // data = 0x100 when shift register is empty
                {
                    if (!dmc.empty)
                    {
                        if ((dmc.data & 1) != 0 && (dmc.damp < 63))
                            dmc.damp++;
                        else if ((dmc.data & 1) == 0 && (0 < dmc.damp))
                            dmc.damp--;
                    }
                    dmc.data >>= 1;
                }

                if (dmc.data <= 0x100) // shift register is empty
                {
                    if (dmc.dlength > 0)
                    {
                        dmc.org_memory.Read(dmc.daddress, ref dmc.data);
                        //dmc.data = dmc.memory[dmc.daddress + dmc.ptrMemory];
                        //cpu->StealCycles(4); // DMC read takes 3 or 4 CPU cycles, usually 4
                        // (checking for the 3-cycle case would require sub-instruction emulation)
                        dmc.data &= 0xFF; // read 8 bits
                        if (dmc.option[(Int32)OPT.OPT_DPCM_REVERSE] != 0) dmc.data = BITREVERSE[dmc.data];
                        dmc.data |= 0x10000; // use an extra bit to signal end of data
                        dmc.empty = false;
                        dmc.daddress = ((dmc.daddress + 1) & 0xFFFF) | 0x8000;
                        --dmc.dlength;
                        if (dmc.dlength == 0)
                        {
                            if ((dmc.mode & 1) != 0) // looped DPCM = auto-reload
                            {
                                dmc.daddress = ((dmc.adr_reg << 6) | 0xC000);
                                dmc.dlength = (dmc.len_reg << 4) + 1;
                            }
                            else if ((dmc.mode & 2) != 0) // IRQ and not looped
                            {
                                dmc.irq = true;
                                dmc.cpu?.UpdateIRQ(enmIRQ_devices.IRQD_DMC, true);
                            }
                        }
                    }
                    else
                    {
                        dmc.data = 0x10000; // DMC will do nothing
                        dmc.empty = true;
                    }
                }
            }

            dmc.reg[0x12] = (byte)(dmc.empty ? 0 : 1);// dpc;
            return (UInt32)((dmc.damp << 1) + dmc.dac_lsb);
        }

        public void TickFrameSequence(NES_DMC dmc, UInt32 clocks)
        {
            dmc.frame_sequence_count += (Int32)clocks;
            while (dmc.frame_sequence_count > dmc.frame_sequence_length)
            {
                FrameSequence(dmc, dmc.frame_sequence_step);
                dmc.frame_sequence_count -= dmc.frame_sequence_length;
                ++dmc.frame_sequence_step;
                if (dmc.frame_sequence_step >= dmc.frame_sequence_steps)
                    dmc.frame_sequence_step = 0;
            }
        }

        public void Tick(NES_DMC dmc, UInt32 clocks)
        {
            dmc._out[0] = calc_tri(dmc, clocks);
            dmc._out[1] = calc_noise(dmc, clocks);
            dmc._out[2] = calc_dmc(dmc, clocks);
        }

        public void org_Tick(NES_DMC dmc, UInt32 clocks)
        {
            dmc._out[0] = calc_tri(dmc, clocks);
            dmc._out[1] = calc_noise(dmc, clocks);
            dmc._out[2] = org_calc_dmc(dmc, clocks);
        }

        private Int32[] m = new Int32[3];
        public UInt32 NES_DMC_np_Render(NES_DMC chip, Int32[] b)//b[2])
        {
            NES_DMC dmc = chip;
            UInt32 clocks;

            COUNTER_iup(dmc.tick_count);   // increase counter (overflows after 255)
            clocks = (COUNTER_value(dmc.tick_count) - dmc.tick_last) & 0xFF;
            TickFrameSequence(dmc, clocks);
            Tick(dmc, clocks);
            dmc.tick_last = COUNTER_value(dmc.tick_count);

            dmc._out[0] = (dmc.mask & 1) != 0 ? 0 : dmc._out[0];
            dmc._out[1] = (dmc.mask & 2) != 0 ? 0 : dmc._out[1];
            dmc._out[2] = (dmc.mask & 4) != 0 ? 0 : dmc._out[2];

            m[0] = (Int32)dmc.tnd_table[0][dmc._out[0]][0][0];
            m[1] = (Int32)dmc.tnd_table[0][0][dmc._out[1]][0];
            m[2] = (Int32)dmc.tnd_table[0][0][0][dmc._out[2]];

            if (dmc.option[(Int32)OPT.OPT_NONLINEAR_MIXER] != 0)
            {
                Int32 _ref = m[0] + m[1] + m[2];
                Int32 voltage = (Int32)dmc.tnd_table[1][dmc._out[0]][dmc._out[1]][dmc._out[2]];
                int i;
                if (_ref != 0)
                {
                    for (i = 0; i < 3; ++i)
                        m[i] = (m[i] * voltage) / _ref;
                }
                else
                {
                    for (i = 0; i < 3; ++i)
                        m[i] = voltage;
                }
            }

            // anti-click nullifies any 4011 write but preserves nonlinearity
            if (dmc.option[(Int32)OPT.OPT_DPCM_ANTI_CLICK] != 0)
            {
                if (dmc.dmc_pop) // $4011 will cause pop this frame
                {
                    // adjust offset to counteract pop
                    dmc.dmc_pop_offset += dmc.dmc_pop_follow - m[2];
                    dmc.dmc_pop = false;

                    // prevent overflow, keep headspace at edges
                    //const INT32 OFFSET_MAX = (1 << 30) - (4 << 16);
                    Int32 OFFSET_MAX = ((1 << 30) - (4 << 16));
                    if (dmc.dmc_pop_offset > OFFSET_MAX) dmc.dmc_pop_offset = OFFSET_MAX;
                    if (dmc.dmc_pop_offset < -OFFSET_MAX) dmc.dmc_pop_offset = -OFFSET_MAX;
                }
                dmc.dmc_pop_follow = m[2]; // remember previous position

                m[2] += dmc.dmc_pop_offset; // apply offset

                // TODO implement this in a better way
                // roll off offset (not ideal, but prevents overflow)
                if (dmc.dmc_pop_offset > 0) --dmc.dmc_pop_offset;
                else if (dmc.dmc_pop_offset < 0) ++dmc.dmc_pop_offset;
            }

            b[0] = m[0] * dmc.sm[0][0];
            b[0] += m[1] * dmc.sm[0][1];
            b[0] += m[2] * dmc.sm[0][2];
            b[0] >>= 5;

            b[1] = m[0] * dmc.sm[1][0];
            b[1] += m[1] * dmc.sm[1][1];
            b[1] += m[2] * dmc.sm[1][2];
            b[1] >>= 5;

            //dmc.reg[0x10] = (byte)(m[0]);
            //dmc.reg[0x11] = (byte)(m[0] >> 8);
            //dmc.reg[0x12] = (byte)(m[1]);
            //dmc.reg[0x13] = (byte)(m[1] >> 8);
            //dmc.reg[0x14] = (byte)(m[2]);
            //dmc.reg[0x15] = (byte)(m[2] >> 8);

            return 2;
        }

        public UInt32 NES_DMC_org_Render(NES_DMC chip, Int32[] b)//b[2])
        {
            NES_DMC dmc = chip;
            //UInt32 clocks;

            dmc._out[0] = (dmc.mask & 1) != 0 ? 0 : dmc._out[0];
            dmc._out[1] = (dmc.mask & 2) != 0 ? 0 : dmc._out[1];
            dmc._out[2] = (dmc.mask & 4) != 0 ? 0 : dmc._out[2];

            m[0] = (Int32)dmc.tnd_table[0][dmc._out[0]][0][0];
            m[1] = (Int32)dmc.tnd_table[0][0][dmc._out[1]][0];
            m[2] = (Int32)dmc.tnd_table[0][0][0][dmc._out[2]];

            if (dmc.option[(Int32)OPT.OPT_NONLINEAR_MIXER] != 0)
            {
                Int32 _ref = m[0] + m[1] + m[2];
                Int32 voltage = (Int32)dmc.tnd_table[1][dmc._out[0]][dmc._out[1]][dmc._out[2]];
                int i;
                if (_ref != 0)
                {
                    for (i = 0; i < 3; ++i)
                        m[i] = (m[i] * voltage) / _ref;
                }
                else
                {
                    for (i = 0; i < 3; ++i)
                        m[i] = voltage;
                }
            }

            // anti-click nullifies any 4011 write but preserves nonlinearity
            if (dmc.option[(Int32)OPT.OPT_DPCM_ANTI_CLICK] != 0)
            {
                if (dmc.dmc_pop) // $4011 will cause pop this frame
                {
                    // adjust offset to counteract pop
                    dmc.dmc_pop_offset += dmc.dmc_pop_follow - m[2];
                    dmc.dmc_pop = false;

                    // prevent overflow, keep headspace at edges
                    //const INT32 OFFSET_MAX = (1 << 30) - (4 << 16);
                    Int32 OFFSET_MAX = ((1 << 30) - (4 << 16));
                    if (dmc.dmc_pop_offset > OFFSET_MAX) dmc.dmc_pop_offset = OFFSET_MAX;
                    if (dmc.dmc_pop_offset < -OFFSET_MAX) dmc.dmc_pop_offset = -OFFSET_MAX;
                }
                dmc.dmc_pop_follow = m[2]; // remember previous position

                m[2] += dmc.dmc_pop_offset; // apply offset

                // TODO implement this in a better way
                // roll off offset (not ideal, but prevents overflow)
                if (dmc.dmc_pop_offset > 0) --dmc.dmc_pop_offset;
                else if (dmc.dmc_pop_offset < 0) ++dmc.dmc_pop_offset;
            }

            b[0] = m[0] * dmc.sm[0][0];
            b[0] += m[1] * dmc.sm[0][1];
            b[0] += m[2] * dmc.sm[0][2];
            b[0] >>= 7 - 3;

            b[1] = m[0] * dmc.sm[1][0];
            b[1] += m[1] * dmc.sm[1][1];
            b[1] += m[2] * dmc.sm[1][2];
            b[1] >>= 7 - 3;

            //dmc.reg[0x10] = (byte)(m[0]);
            //dmc.reg[0x11] = (byte)(m[0] >> 8);
            //dmc.reg[0x12] = (byte)(m[1]);
            //dmc.reg[0x13] = (byte)(m[1] >> 8);
            //dmc.reg[0x14] = (byte)(m[2]);
            //dmc.reg[0x15] = (byte)(m[2] >> 8);

            return 2;
        }

        public void NES_DMC_np_SetClock(NES_DMC chip, double c)
        {
            NES_DMC dmc = chip;

            dmc.clock = (UInt32)(c);

            if (Math.Abs(dmc.clock - DEFAULT_CLK_PAL) <= 1000)  // check for approximately DEFAULT_CLK_PAL
                NES_DMC_np_SetPal(dmc, true);
            else
                NES_DMC_np_SetPal(dmc, false);
        }

        public void NES_DMC_np_SetRate(NES_DMC chip, double r)
        {
            NES_DMC dmc = chip;

            dmc.rate = (UInt32)(r != 0 ? r : DEFAULT_RATE);

            COUNTER_init(dmc.tick_count, dmc.clock, dmc.rate);
            dmc.tick_last = 0;
        }

        private void NES_DMC_np_SetPal(NES_DMC chip, bool is_pal)
        {
            NES_DMC dmc = chip;

            dmc.pal = (is_pal ? 1 : 0);
            // set CPU cycles in frame_sequence
            dmc.frame_sequence_length = is_pal ? 8314 : 7458;
        }

        public void NES_DMC_np_SetAPU(NES_DMC chip, np_nes_apu.NES_APU apu_)
        {
            NES_DMC dmc = chip;
            dmc.apu = apu_;
        }

        // Initializing TRI, NOISE, DPCM mixing table
        private void InitializeTNDTable(NES_DMC dmc, double wt, double wn, double wd)
        {
            // volume adjusted by 0.95 based on empirical measurements
            const double MASTER = 8192.0 * 0.95;
            // truthfully, the nonlinear curve does not appear to match well
            // with my tests, triangle in particular seems too quiet relatively.
            // do more testing of the APU/DMC DAC later

            int t, n, d;

            {   // Linear Mixer
                for (t = 0; t < 16; t++)
                {
                    for (n = 0; n < 16; n++)
                    {
                        for (d = 0; d < 128; d++)
                        {
                            dmc.tnd_table[0][t][n][d] = (UInt32)(MASTER * (3.0 * t + 2.0 * n + d) / 208.0);
                        }
                    }
                }
            }
            {   // Non-Linear Mixer
                dmc.tnd_table[1][0][0][0] = 0;
                for (t = 0; t < 16; t++)
                {
                    for (n = 0; n < 16; n++)
                    {
                        for (d = 0; d < 128; d++)
                        {
                            if (t != 0 || n != 0 || d != 0)
                                dmc.tnd_table[1][t][n][d] = (UInt32)((MASTER * 159.79) / (100.0 + 1.0 / ((double)t / wt + (double)n / wn + (double)d / wd)));
                        }
                    }
                }
            }

        }

        public void NES_DMC_np_Reset(NES_DMC chip)
        {
            NES_DMC dmc = chip;
            int i;
            dmc.mask = 0;

            InitializeTNDTable(dmc, 8227, 12241, 22638);

            dmc.counter[0] = 0;
            dmc.counter[1] = 0;
            dmc.counter[2] = 0;
            dmc.tphase = 0;
            dmc.nfreq = wavlen_table[0][0];
            dmc.dfreq = freq_table[0][0];
            dmc.tri_freq = 0;
            dmc.linear_counter = 0;
            dmc.linear_counter_reload = 0;
            dmc.linear_counter_halt = false;
            dmc.linear_counter_control = false;
            dmc.noise_volume = 0;
            dmc.noise = 0;
            dmc.noise_tap = 0;
            dmc.envelope_loop = false;
            dmc.envelope_disable = false;
            dmc.envelope_write = false;
            dmc.envelope_div_period = 0;
            dmc.envelope_div = 0;
            dmc.envelope_counter = 0;
            dmc.enable[0] = false;
            dmc.enable[1] = false;
            dmc.length_counter[0] = 0;
            dmc.length_counter[1] = 0;
            dmc.frame_irq = false;
            dmc.frame_irq_enable = false;
            dmc.frame_sequence_count = 0;
            dmc.frame_sequence_steps = 4;
            dmc.frame_sequence_step = 0;
            dmc.cpu?.UpdateIRQ(enmIRQ_devices.IRQD_FRAME, false);

            for (i = 0; i < 0x0f; i++)
                NES_DMC_np_Write(dmc, (UInt32)(0x4008 + i), 0);
            NES_DMC_np_Write(dmc, 0x4017, 0x40);

            dmc.irq = false;
            NES_DMC_np_Write(dmc, 0x4015, 0x00);
            if (dmc.option[(Int32)OPT.OPT_UNMUTE_ON_RESET] != 0)
                NES_DMC_np_Write(dmc, 0x4015, 0x0f);
            dmc.cpu?.UpdateIRQ(enmIRQ_devices.IRQD_DMC, false);

            dmc._out[0] = dmc._out[1] = dmc._out[2] = 0;
            dmc.damp = 0;
            dmc.dmc_pop = false;
            dmc.dmc_pop_offset = 0;
            dmc.dmc_pop_follow = 0;
            dmc.dac_lsb = 0;
            dmc.data = 0x100;
            dmc.empty = true;
            dmc.adr_reg = 0;
            dmc.dlength = 0;
            dmc.len_reg = 0;
            dmc.daddress = 0;
            dmc.noise = 1;
            dmc.noise_tap = (1 << 1);
            if (dmc.option[(Int32)OPT.OPT_RANDOMIZE_NOISE] != 0)
            {
                dmc.noise |= (UInt32)rnd.Next();// rand();
                dmc.counter[1] = -(rnd.Next() & 511);
            }
            if (dmc.option[(Int32)OPT.OPT_RANDOMIZE_TRI] != 0)
            {
                dmc.tphase |= (Int32)(rnd.Next() & 31);// rand();
                dmc.counter[0] = -(rnd.Next() & 2047);
            }

            NES_DMC_np_SetRate(dmc, dmc.rate);
        }

        public void NES_DMC_np_SetMemory(NES_DMC chip, byte[] r, Int32 ptr)
        {
            NES_DMC dmc = chip;
            dmc.memory = r;
            dmc.ptrMemory = ptr;
        }

        public void NES_DMC_org_SetMemory(NES_DMC chip, IDevice r)
        {
            NES_DMC dmc = chip;
            dmc.org_memory = r;
        }

        public void NES_DMC_np_SetOption(NES_DMC chip, int id, int val)
        {
            NES_DMC dmc = chip;

            if (id < (Int32)OPT.OPT_END)
            {
                dmc.option[id] = val;
                if (id == (Int32)OPT.OPT_NONLINEAR_MIXER)
                    InitializeTNDTable(dmc, 8227, 12241, 22638);
            }
        }

        private byte[] length_table = new byte[32] {
            0x0A, 0xFE,
            0x14, 0x02,
            0x28, 0x04,
            0x50, 0x06,
            0xA0, 0x08,
            0x3C, 0x0A,
            0x0E, 0x0C,
            0x1A, 0x0E,
            0x0C, 0x10,
            0x18, 0x12,
            0x30, 0x14,
            0x60, 0x16,
            0xC0, 0x18,
            0x48, 0x1A,
            0x10, 0x1C,
            0x20, 0x1E
        };

        public bool NES_DMC_np_Write(NES_DMC chip, UInt32 adr, UInt32 val)
        {
            NES_DMC dmc = chip;

            if (adr == 0x4015)
            {
                dmc.enable[0] = (val & 4) != 0;
                dmc.enable[1] = (val & 8) != 0;

                if (!dmc.enable[0])
                {
                    dmc.length_counter[0] = 0;
                    //dmc.tphase = 0;//KUMA:止めたら出力も0にしたい
                }
                if (!dmc.enable[1])
                {
                    dmc.length_counter[1] = 0;
                }

                if ((val & 16) != 0 && dmc.dlength == 0)
                {
                    //dmc.enable[2] = dmc.active = true;
                    dmc.daddress = (0xC000 | (dmc.adr_reg << 6));
                    dmc.dlength = (dmc.len_reg << 4) + 1;
                    //dmc.irq = false;
                }
                else if ((val & 16) == 0)
                {
                    dmc.dlength = 0;
                    //.enable[2] = dmc.active = false;
                }

                dmc.irq = false;
                dmc.cpu?.UpdateIRQ(enmIRQ_devices.IRQD_DMC, false);

                dmc.reg[adr - 0x4008] = (byte)val;
                return true;
            }

            if (adr == 0x4017)
            {
                //DEBUG_OUT("4017 = %02X\n", val);
                dmc.frame_irq_enable = ((val & 0x40) != 0x40);
                if (dmc.frame_irq_enable) dmc.frame_irq = false;
                dmc.cpu?.UpdateIRQ(enmIRQ_devices.IRQD_FRAME, false);

                dmc.frame_sequence_count = 0;
                if ((val & 0x80) != 0)
                {
                    dmc.frame_sequence_steps = 5;
                    dmc.frame_sequence_step = 0;
                    FrameSequence(dmc, dmc.frame_sequence_step);
                    ++dmc.frame_sequence_step;
                }
                else
                {
                    dmc.frame_sequence_steps = 4;
                    dmc.frame_sequence_step = 1;
                }
            }

            if (adr < 0x4008 || 0x4013 < adr)
                return false;

            dmc.reg[adr - 0x4008] = (byte)(val & 0xff);

            //DEBUG_OUT("$%04X %02X\n", adr, val);

            switch (adr)
            {

                // tri

                case 0x4008:
                    dmc.linear_counter_control = ((val >> 7) & 1) != 0;
                    dmc.linear_counter_reload = (Int32)(val & 0x7F);
                    break;

                case 0x4009:
                    break;

                case 0x400a:
                    dmc.tri_freq = val | (dmc.tri_freq & 0x700);
                    break;

                case 0x400b:
                    dmc.tri_freq = (dmc.tri_freq & 0xff) | ((val & 0x7) << 8);
                    dmc.linear_counter_halt = true;
                    if (dmc.enable[0])
                    {
                        dmc.length_counter[0] = length_table[(val >> 3) & 0x1f];
                    }
                    break;

                // noise

                case 0x400c:
                    dmc.noise_volume = (Int32)(val & 15);
                    dmc.envelope_div_period = (Int32)(val & 15);
                    dmc.envelope_disable = ((val >> 4) & 1) != 0;
                    dmc.envelope_loop = ((val >> 5) & 1) != 0;
                    break;

                case 0x400d:
                    break;

                case 0x400e:
                    if (dmc.option[(Int32)OPT.OPT_ENABLE_PNOISE] != 0)
                        dmc.noise_tap = (UInt32)((val & 0x80) != 0 ? (1 << 6) : (1 << 1));
                    else
                        dmc.noise_tap = (UInt32)(1 << 1);
                    dmc.nfreq = wavlen_table[dmc.pal][val & 15];
                    break;

                case 0x400f:
                    if (dmc.enable[1])
                    {
                        dmc.length_counter[1] = length_table[(val >> 3) & 0x1f];
                    }
                    dmc.envelope_write = true;
                    break;

                // dmc

                case 0x4010:
                    dmc.mode = (Int32)((val >> 6) & 3);
                    if ((dmc.mode & 2) == 0)
                    {
                        dmc.irq = false;
                        dmc.cpu?.UpdateIRQ(enmIRQ_devices.IRQD_DMC, false);
                    }
                    dmc.dfreq = freq_table[dmc.pal][val & 15];
                    break;

                case 0x4011:
                    if (dmc.option[(Int32)OPT.OPT_ENABLE_4011] != 0)
                    {
                        dmc.damp = (Int16)((val >> 1) & 0x3f);
                        dmc.dac_lsb = (Int32)(val & 1);
                        dmc.dmc_pop = true;
                    }
                    break;

                case 0x4012:
                    dmc.adr_reg = val & 0xff;
                    // ここでdaddressは更新されない
                    break;

                case 0x4013:
                    dmc.len_reg = val & 0xff;
                    // ここでlengthは更新されない
                    break;

                default:
                    return false;
            }

            return true;
        }

        public bool NES_DMC_np_Read(NES_DMC chip, UInt32 adr, ref UInt32 val)
        {
            NES_DMC dmc = chip;

            if (adr == 0x4015)
            {
                val |= (UInt32)((dmc.irq ? 0x80 : 0)
                     | (dmc.frame_irq ? 0x40 : 0)
                     | (dmc.dlength > 0 ? 0x10 : 0)
                     | (dmc.length_counter[1] != 0 ? 0x08 : 0)
                     | (dmc.length_counter[0] != 0 ? 0x04 : 0))
                     ;

                dmc.frame_irq = false;
                dmc.cpu?.UpdateIRQ(enmIRQ_devices.IRQD_FRAME, false);
                return true;
            }
            else if (0x4008 <= adr && adr <= 0x4014)
            {
                val |= dmc.reg[adr - 0x4008];
                return true;
            }
            else
                return false;
        }

        public void SetCPU(NES_DMC chip, km6502 cpu)
        {
            NES_DMC dmc = chip;
            dmc.cpu=cpu;
        }
    }
}