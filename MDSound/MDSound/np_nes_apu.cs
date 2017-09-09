using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDSound
{
    public class np_nes_apu
    {

        //
        // NES 2A03
        //
        // Ported from NSFPlay 2.2 to VGMPlay (including C++ -> C conversion)
        // by Valley Bell on 24 September 2013
        // Updated to NSFPlay 2.3 on 26 September 2013
        // (Note: Encoding is UTF-8)

        //#include <assert.h>
        //# include <stdlib.h>
        //# include <string.h>	// for memset
        //# include <stddef.h>	// for NULL
        //# include "mamedef.h"
        //# include "../stdbool.h"
        //# include "np_nes_apu.h"


        // Master Clock: 21477272 (NTSC)
        // APU Clock = Master Clock / 12
        private const double DEFAULT_CLOCK = 1789772.0; // not sure if this shouldn't be 1789772,667 instead
        private const int DEFAULT_RATE = 44100;


        /** Upper half of APU **/
        public enum OPT
        {
            OPT_UNMUTE_ON_RESET = 0,
            OPT_NONLINEAR_MIXER,
            OPT_PHASE_REFRESH,
            OPT_DUTY_SWAP,
            OPT_END
        };

        public enum SQR
        {
            SQR0_MASK = 1,
            SQR1_MASK = 2,
        };

        // Note: For increased speed, I'll inline all of NSFPlay's Counter member functions.
        private const int COUNTER_SHIFT = 24;

        //private Counter counter = new Counter();
        public class Counter
        {
            public double ratio;
            public UInt32 val, step;
        };

        private void COUNTER_setcycle(Counter cntr, Int32 s)
        {
            (cntr).step = (UInt32)((cntr).ratio / (s + 1));
        }

        private void COUNTER_iup(Counter cntr)
        {
            (cntr).val += (cntr).step;
        }

        private UInt32 COUNTER_value(Counter cntr)
        {
            return ((cntr).val >> COUNTER_SHIFT);
        }

        private void COUNTER_init(Counter cntr, double clk, double rate)
        {
            (cntr).ratio = (1 << COUNTER_SHIFT) * (1.0 * clk / rate);
            (cntr).step = (UInt32)((cntr).ratio + 0.5);
            (cntr).val = 0;
        }


        //private NES_APU nNES_APU = new NES_APU();
        public class NES_APU
        {
            public int[] option = new int[(int)OPT.OPT_END];        // 各種オプション
            public Int32 mask;
            public Int32[][] sm = new Int32[2][] { new Int32[2], new Int32[2] };

            public UInt32 gclock;
            public byte[] reg = new byte[0x20];
            public Int32[] _out = new Int32[2];
            public double rate, clock;

            public Int32[] square_table = new Int32[32];     // nonlinear mixer

            public Int32[] scounter = new Int32[2];            // frequency divider
            public Int32[] sphase = new Int32[2];              // phase counter

            public Int32[] duty = new Int32[2];
            public Int32[] volume = new Int32[2];
            public Int32[] freq = new Int32[2];
            public Int32[] sfreq = new Int32[2];

            public bool[] sweep_enable = new bool[2];
            public bool[] sweep_mode = new bool[2];
            public bool[] sweep_write = new bool[2];
            public Int32[] sweep_div_period = new Int32[2];
            public Int32[] sweep_div = new Int32[2];
            public Int32[] sweep_amount = new Int32[2];

            public bool[] envelope_disable = new bool[2];
            public bool[] envelope_loop = new bool[2];
            public bool[] envelope_write = new bool[2];
            public Int32[] envelope_div_period = new Int32[2];
            public Int32[] envelope_div = new Int32[2];
            public Int32[] envelope_counter = new Int32[2];

            public Int32[] length_counter = new Int32[2];

            public bool[] enable = new bool[2];

            public Counter tick_count=new Counter();
            public UInt32 tick_last;
        };

        //static void sweep_sqr(NES_APU* apu, int ch);    // calculates target sweep frequency
        //static INT32 calc_sqr(NES_APU* apu, int ch, UINT32 clocks);
        //static void Tick(NES_APU* apu, UINT32 clocks);


        private static void sweep_sqr(NES_APU apu, int i)
        {
            Int32 shifted = apu.freq[i] >> apu.sweep_amount[i];
            if (i == 0 && apu.sweep_mode[i]) shifted += 1;
            apu.sfreq[i] = apu.freq[i] + (apu.sweep_mode[i] ? -shifted : shifted);
            //DEBUG_OUT("shifted[%d] = %d (%d >> %d)\n",i,shifted,apu->freq[i],apu->sweep_amount[i]);
        }

        public void NES_APU_np_FrameSequence(NES_APU chip, int s)
        {
            NES_APU apu = (NES_APU)chip;
            Int32 i;

            //DEBUG_OUT("FrameSequence(%d)\n",s);

            if (s > 3) return; // no operation in step 4

            // 240hz clock
            for (i = 0; i < 2; ++i)
            {
                bool divider = false;
                if (apu.envelope_write[i])
                {
                    apu.envelope_write[i] = false;
                    apu.envelope_counter[i] = 15;
                    apu.envelope_div[i] = 0;
                }
                else
                {
                    ++apu.envelope_div[i];
                    if (apu.envelope_div[i] > apu.envelope_div_period[i])
                    {
                        divider = true;
                        apu.envelope_div[i] = 0;
                    }
                }
                if (divider)
                {
                    if (apu.envelope_loop[i] && apu.envelope_counter[i] == 0)
                        apu.envelope_counter[i] = 15;
                    else if (apu.envelope_counter[i] > 0)
                        --apu.envelope_counter[i];
                }
            }

            // 120hz clock
            if ((s & 1) == 0)
                for (i = 0; i < 2; ++i)
                {
                    if (!apu.envelope_loop[i] && (apu.length_counter[i] > 0))
                        --apu.length_counter[i];

                    if (apu.sweep_enable[i])
                    {
                        //DEBUG_OUT("Clock sweep: %d\n", i);

                        --apu.sweep_div[i];
                        if (apu.sweep_div[i] <= 0)
                        {
                            sweep_sqr(apu, i);  // calculate new sweep target

                            //DEBUG_OUT("sweep_div[%d] (0/%d)\n",i,apu->sweep_div_period[i]);
                            //DEBUG_OUT("freq[%d]=%d > sfreq[%d]=%d\n",i,apu->freq[i],i,apu->sfreq[i]);

                            if (apu.freq[i] >= 8 && apu.sfreq[i] < 0x800 && apu.sweep_amount[i] > 0) // update frequency if appropriate
                            {
                                apu.freq[i] = apu.sfreq[i] < 0 ? 0 : apu.sfreq[i];
                                if (apu.scounter[i] > apu.freq[i]) apu.scounter[i] = apu.freq[i];
                            }
                            apu.sweep_div[i] = apu.sweep_div_period[i] + 1;

                            //DEBUG_OUT("freq[%d]=%d\n",i,apu->freq[i]);
                        }

                        if (apu.sweep_write[i])
                        {
                            apu.sweep_div[i] = apu.sweep_div_period[i] + 1;
                            apu.sweep_write[i] = false;
                        }
                    }
                }

        }

        private Int16[][] sqrtbl = new Int16[4][]  {
                new Int16[16] { 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                new Int16[16] { 0, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                new Int16[16] { 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0 },
                new Int16[16] { 1, 1, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }
            };

        private Int32 calc_sqr(NES_APU apu, Int32 i, UInt32 clocks)
        {
            Int32 ret = 0;

            apu.scounter[i] += (Int32)clocks;
            while (apu.scounter[i] > apu.freq[i])
            {
                apu.sphase[i] = (apu.sphase[i] + 1) & 15;
                apu.scounter[i] -= (apu.freq[i] + 1);
            }

            //INT32 ret = 0;
            if (apu.length_counter[i] > 0 &&
                apu.freq[i] >= 8 &&
                apu.sfreq[i] < 0x800
                )
            {
                Int32 v = apu.envelope_disable[i] ? apu.volume[i] : apu.envelope_counter[i];
                ret = sqrtbl[apu.duty[i]][apu.sphase[i]] != 0 ? v : 0;
            }

            return ret;
        }

        private bool NES_APU_np_Read(NES_APU chip, UInt32 adr, ref UInt32 val)
        {
            NES_APU apu = (NES_APU)chip;

            if (0x4000 <= adr && adr < 0x4008)
            {
                val |= apu.reg[adr & 0x7];
                return true;
            }
            else if (adr == 0x4015)
            {
                val |= (uint)((apu.length_counter[1] != 0 ? 2 : 0) | (apu.length_counter[0] != 0 ? 1 : 0));
                return true;
            }
            else
                return false;
        }

        private void Tick(NES_APU apu, UInt32 clocks)
        {
            apu._out[0] = calc_sqr(apu, 0, clocks);
            apu._out[1] = calc_sqr(apu, 1, clocks);
        }

        private Int32[] m = new Int32[2];

        // 生成される波形の振幅は0-8191
        public UInt32 NES_APU_np_Render(NES_APU chip, Int32[] b) //b[2])
        {
            NES_APU apu = (NES_APU)chip;

            COUNTER_iup(apu.tick_count);
            Tick(apu, (COUNTER_value(apu.tick_count) - apu.tick_last) & 0xFF);
            apu.tick_last = COUNTER_value(apu.tick_count);

            apu._out[0] = (apu.mask & 1) != 0 ? 0 : apu._out[0];
            apu._out[1] = (apu.mask & 2) != 0 ? 0 : apu._out[1];

            if (apu.option[(int)OPT.OPT_NONLINEAR_MIXER] != 0)
            {
                Int32 voltage;
                Int32 _ref;

                voltage = apu.square_table[apu._out[0] + apu._out[1]];
                m[0] = apu._out[0] << 6;
                m[1] = apu._out[1] << 6;
                _ref = m[0] + m[1];
                if (_ref > 0)
                {
                    m[0] = (m[0] * voltage) / _ref;
                    m[1] = (m[1] * voltage) / _ref;
                }
                else
                {
                    m[0] = voltage;
                    m[1] = voltage;
                }
            }
            else
            {
                m[0] = apu._out[0] << 6;
                m[1] = apu._out[1] << 6;
            }

            // Shifting is (x-2) to match the volume of MAME's NES APU sound core
            b[0] = m[0] * apu.sm[0][0];
            b[0] += m[1] * apu.sm[0][1];
            b[0] >>= 7 - 2; // was 7, but is now 8 for bipolar square

            b[1] = m[0] * apu.sm[1][0];
            b[1] += m[1] * apu.sm[1][1];
            b[1] >>= 7 - 2; // see above

            return 2;
        }

        public NES_APU NES_APU_np_Create(Int32 clock, Int32 rate)
        {
            NES_APU apu;
            Int32 i, c, t;

            apu = new NES_APU();// malloc(sizeof(NES_APU));
            if (apu == null)
                return null;
            //memset(apu, 0x00, sizeof(NES_APU));

            //NES_APU_np_SetClock(apu, DEFAULT_CLOCK);
            //NES_APU_np_SetRate(apu, DEFAULT_RATE);
            NES_APU_np_SetClock(apu, clock);
            NES_APU_np_SetRate(apu, rate);
            apu.option[(int)OPT.OPT_UNMUTE_ON_RESET] = 1;// true;
            apu.option[(int)OPT.OPT_PHASE_REFRESH] = 1;// true;
            apu.option[(int)OPT.OPT_NONLINEAR_MIXER] = 1;// true;
            apu.option[(int)OPT.OPT_DUTY_SWAP] = 0;// false;

            apu.square_table[0] = 0;
            for (i = 1; i < 32; i++)
                apu.square_table[i] = (Int32)((8192.0 * 95.88) / (8128.0 / i + 100));

            for (c = 0; c < 2; ++c)
                for (t = 0; t < 2; ++t)
                    apu.sm[c][t] = 128;

            return apu;
        }

        public void NES_APU_np_Destroy(NES_APU chip)
        {
            //free(chip);
        }

        public void NES_APU_np_Reset(NES_APU chip)
        {
            NES_APU apu = (NES_APU)chip;
            Int32 i;
            apu.gclock = 0;
            apu.mask = 0;

            apu.scounter[0] = 0;
            apu.scounter[1] = 0;
            apu.sphase[0] = 0;
            apu.sphase[0] = 0;

            apu.sweep_div[0] = 1;
            apu.sweep_div[1] = 1;
            apu.envelope_div[0] = 0;
            apu.envelope_div[1] = 0;
            apu.length_counter[0] = 0;
            apu.length_counter[1] = 0;
            apu.envelope_counter[0] = 0;
            apu.envelope_counter[1] = 0;

            for (i = 0x4000; i < 0x4008; i++)
                NES_APU_np_Write(apu, (UInt32)i, 0);

            NES_APU_np_Write(apu, 0x4015, 0);
            if (apu.option[(int)OPT.OPT_UNMUTE_ON_RESET] != 0)
                NES_APU_np_Write(apu, 0x4015, 0x0f);

            for (i = 0; i < 2; i++)
                apu._out[i] = 0;

            NES_APU_np_SetRate(apu, apu.rate);
        }

        public void NES_APU_np_SetOption(NES_APU chip, Int32 id, Int32 val)
        {
            NES_APU apu = (NES_APU)chip;

            if (id < (int)OPT.OPT_END) apu.option[id] = val;
        }

        public void NES_APU_np_SetClock(NES_APU chip, double c)
        {
            NES_APU apu = (NES_APU)chip;

            apu.clock = c;
        }

        public void NES_APU_np_SetRate(NES_APU chip, double r)
        {
            NES_APU apu = (NES_APU)chip;

            apu.rate = r != 0 ? r : DEFAULT_RATE;

            COUNTER_init(apu.tick_count, apu.clock, apu.rate);
            apu.tick_last = 0;
        }

        public void NES_APU_np_SetMask(NES_APU chip, Int32 m)
        {
            NES_APU apu = (NES_APU)chip;
            apu.mask = m;
        }

        public void NES_APU_np_SetStereoMix(NES_APU chip, Int32 trk, Int16 mixl, Int16 mixr)
        {
            NES_APU apu = (NES_APU)chip;

            if (trk < 0) return;
            if (trk > 1) return;
            apu.sm[0][trk] = mixl;
            apu.sm[1][trk] = mixr;
        }

        byte[] length_table = new byte[32] {
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

        public bool NES_APU_np_Write(NES_APU chip, UInt32 adr, UInt32 val)
        {
            NES_APU apu = (NES_APU)chip;
            Int32 ch;


            if (0x4000 <= adr && adr < 0x4008)
            {
                //DEBUG_OUT("$%04X = %02X\n",adr,val);

                adr &= 0xf;
                ch = (Int32)(adr >> 2);
                switch (adr)
                {
                    case 0x0:
                    case 0x4:
                        apu.volume[ch] = (Int32)(val & 15);
                        apu.envelope_disable[ch] = ((val >> 4) & 1) != 0;
                        apu.envelope_loop[ch] = ((val >> 5) & 1) != 0;
                        apu.envelope_div_period[ch] = (Int32)(val & 15);
                        apu.duty[ch] = (Int32)((val >> 6) & 3);
                        if (apu.option[(int)OPT.OPT_DUTY_SWAP] != 0)
                        {
                            if (apu.duty[ch] == 1) apu.duty[ch] = 2;
                            else if (apu.duty[ch] == 2) apu.duty[ch] = 1;
                        }
                        break;

                    case 0x1:
                    case 0x5:
                        apu.sweep_enable[ch] = ((val >> 7) & 1) != 0;
                        apu.sweep_div_period[ch] = (Int32)(((val >> 4) & 7));
                        apu.sweep_mode[ch] = ((val >> 3) & 1) != 0;
                        apu.sweep_amount[ch] = (Int32)(val & 7);
                        apu.sweep_write[ch] = true;
                        sweep_sqr(apu, ch);
                        break;

                    case 0x2:
                    case 0x6:
                        apu.freq[ch] = (Int32)(val | (UInt32)(apu.freq[ch] & 0x700));
                        sweep_sqr(apu, ch);
                        if (apu.scounter[ch] > apu.freq[ch]) apu.scounter[ch] = apu.freq[ch];
                        break;

                    case 0x3:
                    case 0x7:
                        apu.freq[ch] = (Int32)((UInt32)(apu.freq[ch] & 0xFF) | ((val & 0x7) << 8));

                        if (apu.option[(int)OPT.OPT_PHASE_REFRESH] != 0)
                            apu.sphase[ch] = 0;
                        apu.envelope_write[ch] = true;
                        if (apu.enable[ch])
                        {
                            apu.length_counter[ch] = length_table[(val >> 3) & 0x1f];
                        }
                        sweep_sqr(apu, ch);
                        if (apu.scounter[ch] > apu.freq[ch]) apu.scounter[ch] = apu.freq[ch];
                        break;

                    default:
                        return false;
                }
                apu.reg[adr] = (byte)val;
                return true;
            }
            else if (adr == 0x4015)
            {
                apu.enable[0] = (val & 1) != 0 ? true : false;
                apu.enable[1] = (val & 2) != 0 ? true : false;

                if (!apu.enable[0])
                    apu.length_counter[0] = 0;
                if (!apu.enable[1])
                    apu.length_counter[1] = 0;

                apu.reg[adr - 0x4000] = (byte)val;
                return true;
            }

            // 4017 is handled in np_nes_dmc.c
            //else if (adr == 0x4017)
            //{
            //}

            return false;
        }

    }
}
