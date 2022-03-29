using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDSound
{
    public class multipcm : Instrument
    {
        public override void Reset(byte ChipID)
        {
            device_reset_multipcm(ChipID);

            visVolume = new int[2][][] {
                new int[1][] { new int[2] { 0, 0 } }
                , new int[1][] { new int[2] { 0, 0 } }
            };
        }

        public override uint Start(byte ChipID, uint clock)
        {
            uint ret= (uint)device_start_multipcm(ChipID, (int)clock);
            return ret;
        }

        public override uint Start(byte ChipID, uint samplingrate, uint clock, params object[] option)
        {
            uint ret= (uint)device_start_multipcm(ChipID, (int)clock);
            return ret;
        }

        public override void Stop(byte ChipID)
        {
            device_stop_multipcm(ChipID);
        }

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {
            MultiPCM_update(ChipID, outputs, samples);

            visVolume[ChipID][0][0] = outputs[0][0];
            visVolume[ChipID][0][1] = outputs[1][0];
        }

        //#pragma once

        //#include "devlegcy.h"

        //WRITE8_DEVICE_HANDLER( multipcm_w );
        //READ8_DEVICE_HANDLER( multipcm_r );

        //void multipcm_set_bank(running_device *device, UINT32 leftoffs, UINT32 rightoffs);

        //DECLARE_LEGACY_SOUND_DEVICE(MULTIPCM, multipcm);




        /*
 * Sega System 32 Multi/Model 1/Model 2 custom PCM chip (315-5560) emulation.
 *
 * by Miguel Angel Horna (ElSemi) for Model 2 Emulator and MAME.
 * Information by R.Belmont and the YMF278B (OPL4) manual.
 *
 * voice registers:
 * 0: Pan
 * 1: Index of sample
 * 2: LSB of pitch (low 2 bits seem unused so)
 * 3: MSB of pitch (ooooppppppppppxx) (o=octave (4 bit signed), p=pitch (10 bits), x=unused?
 * 4: voice control: top bit = 1 for key on, 0 for key off
 * 5: bit 0: 0: interpolate volume changes, 1: direct set volume,
      bits 1-7 = volume attenuate (0=max, 7f=min)
 * 6: LFO frequency + Phase LFO depth
 * 7: Amplitude LFO size
 *
 * The first sample ROM contains a variable length table with 12
 * bytes per instrument/sample. This is very similar to the YMF278B.
 *
 * The first 3 bytes are the offset into the file (big endian).
 * The next 2 are the loop start offset into the file (big endian)
 * The next 2 are the 2's complement of the total sample size (big endian)
 * The next byte is LFO freq + depth (copied to reg 6 ?)
 * The next 3 are envelope params (Attack, Decay1 and 2, sustain level, release, Key Rate Scaling)
 * The next byte is Amplitude LFO size (copied to reg 7 ?)
 *
 * TODO
 * - The YM278B manual states that the chip supports 512 instruments. The MultiPCM probably supports them
 * too but the high bit position is unknown (probably reg 2 low bit). Any game use more than 256?
 *
 */

        //#include "emu.h"
        //#include "streams.h"
        //# include "mamedef.h"
        //# include <math.h>
        //# include <stdlib.h>
        //# include <string.h>	// for memset
        //# include <stddef.h>	// for NULL
        //# include "multipcm.h"

        private const double MULTIPCM_CLOCKDIV = 180.0f;// (224.0f);

        public class _Sample
        {
            public UInt32 start;
            public UInt32 loop;
            public UInt32 end;
            public byte attack_reg;
            public byte decay1_reg;
            public byte decay2_reg;
            public byte decay_level;
            public byte release_reg;
            public byte key_rate_scale;
            public byte lfo_vibrato_reg;
            public byte lfo_amplitude_reg;
            public byte format;
        };

        public enum _STATE {
            ATTACK,
            DECAY1,
            DECAY2,
            RELEASE 
        }

        public class _envelope_gen_t
        {
            public Int32 volume; //
            public _STATE state;
            public Int32 step=0;
            //step vals
            public Int32 attack_rate;     //Attack
            public Int32 decay1_rate;    //Decay1
            public Int32 decay2_rate;    //Decay2
            public Int32 release_rate;     //Release
            public Int32 decay_level;     //Decay level
        };

        public class _lfo_t
        {
            public UInt16 phase;
            public UInt32 phase_step;
            public Int32[] table;
            public Int32[] scale;
        };


        public class _slot_t
        {
            public byte slot_index;
            public byte[] regs = new byte[8];
            public byte playing;
            public _Sample sample = new _Sample();
            public UInt32 Base;
            public UInt32 offset;
            public UInt32 step;
            public UInt32 pan;
            public UInt32 total_level;
            public UInt32 dest_total_level;
            public Int32 total_level_step;
            public Int32 prev_sample;
            public _envelope_gen_t envelope_gen =new _envelope_gen_t();
            public _lfo_t pitch_lfo =new _lfo_t();   //Phase lfo
            public _lfo_t amplitude_lfo = new _lfo_t();   //AM lfo
            public byte format;

            public byte muted;
        };

        //private _MultiPCM MultiPCM;
        public class _MultiPCM
        {
            //sound_stream * stream;
            //public _Sample[] Samples = new _Sample[0x200];        //Max 512 samples

            public _slot_t[] slots = new _slot_t[28]{
                new _slot_t(), new _slot_t(), new _slot_t(), new _slot_t(), new _slot_t(), new _slot_t(), new _slot_t(),
                new _slot_t(),new _slot_t(),new _slot_t(),new _slot_t(),new _slot_t(),new _slot_t(),new _slot_t(),
                new _slot_t(),new _slot_t(),new _slot_t(),new _slot_t(),new _slot_t(),new _slot_t(),new _slot_t(),
                new _slot_t(),new _slot_t(),new _slot_t(),new _slot_t(),new _slot_t(),new _slot_t(),new _slot_t() };
            public Int32 cur_slot;
            public Int32 address;
            public byte sega_banking;
            public UInt32 bank0;
            public UInt32 bank1;
            public float rate;

            //I include these in the chip because they depend on the chip clock
            public UInt32[] attack_step = new UInt32[0x40];
            public UInt32[] decay_release_step = new UInt32[0x40];    //Envelope step table
            public UInt32[] freq_step_table = new UInt32[0x400];      //Frequency step table
            
            public Int32[] total_level_steps=new int[2];

            public UInt32 ROMMask;
            public UInt32 ROMSize;
            public byte[] ROM;

        };


        private byte IsInit = 0x00;
        private Int32[] left_pan_table = new Int32[0x800];
        private Int32[] right_pan_table = new Int32[0x800];

        //private UInt32 FIX(float v) { return ((UInt32)((float)(1 << SHIFT) * (v))); }

        private readonly Int32[] VALUE_TO_CHANNEL = new Int32[]{
    0, 1, 2, 3, 4, 5, 6 , -1,
    7, 8, 9, 10,11,12,13, -1,
    14,15,16,17,18,19,20, -1,
    21,22,23,24,25,26,27, -1,
        };


        //private Int32 SHIFT = 12;


        ////private double MULTIPCM_RATE = 44100.0;


        private Int32 MAX_CHIPS = 0x02;
        private _MultiPCM[] MultiPCMData = new _MultiPCM[2] { new _MultiPCM(), new _MultiPCM() };// MAX_CHIPS];

        ///*INLINE MultiPCM *get_safe_token(running_device *device)
        //{
        //    assert(device != NULL);
        //    assert(device->type() == MULTIPCM);
        //    return (MultiPCM *)downcast<legacy_device_base *>(device)->token();
        //}*/


        /*******************************
                ENVELOPE SECTION
        *******************************/

        //Times are based on a 44100Hz timebase. It's adjusted to the actual sampling rate on startup

        private readonly double[] BASE_TIMES = new double[64] {
    0,          0,          0,          0,
    6222.95,    4978.37,    4148.66,    3556.01,
    3111.47,    2489.21,    2074.33,    1778.00,
    1555.74,    1244.63,    1037.19,    889.02,
    777.87,     622.31,     518.59,     444.54,
    388.93,     311.16,     259.32,     222.27,
    194.47,     155.60,     129.66,     111.16,
    97.23,      77.82,      64.85,      55.60,
    48.62,      38.91,      32.43,      27.80,
    24.31,      19.46,      16.24,      13.92,
    12.15,      9.75,       8.12,       6.98,
    6.08,       4.90,       4.08,       3.49,
    3.04,       2.49,       2.13,       1.90,
    1.72,       1.41,       1.18,       1.04,
    0.91,       0.73,       0.59,       0.50,
    0.45,       0.45,       0.45,       0.45
        };

        private const double attack_rate_to_decay_rate = 14.32833;
        private Int32[] linear_to_exp_volume = new Int32[0x400];
        //private Int32[] TLSteps = new Int32[2];

        private const Int32 TL_SHIFT = 12;
        private const Int32 EG_SHIFT = 16;

        private void init_sample(_MultiPCM ptChip, _Sample sample, UInt16 index)
        {
            UInt32 address = (UInt32)((index * 12) & ptChip.ROMMask);

            sample.start = (uint)((ptChip.ROM[address + 0] << 16) | (ptChip.ROM[address + 1] << 8) | ptChip.ROM[address + 2]);
            sample.format = (byte)((sample.start >> 20) & 0xfe);
            sample.start &= 0x3fffff;
            sample.loop = (uint)((ptChip.ROM[address + 3] << 8) | ptChip.ROM[address + 4]);
            sample.end = (uint)(0xffff - ((ptChip.ROM[address + 5] << 8) | ptChip.ROM[address + 6]));
            sample.attack_reg = (byte)((ptChip.ROM[address + 8] >> 4) & 0xf);
            sample.decay1_reg = (byte)(ptChip.ROM[address + 8] & 0xf);
            sample.decay2_reg = (byte)(ptChip.ROM[address + 9] & 0xf);
            sample.decay_level = (byte)((ptChip.ROM[address + 9] >> 4) & 0xf);
            sample.release_reg = (byte)(ptChip.ROM[address + 10] & 0xf);
            sample.key_rate_scale = (byte)((ptChip.ROM[address + 10] >> 4) & 0xf);
            sample.lfo_vibrato_reg = ptChip.ROM[address + 7];
            sample.lfo_amplitude_reg = (byte)(ptChip.ROM[address + 11] & 0xf);
        }

        private Int32 envelope_generator_update(_slot_t slot)
        {
            switch (slot.envelope_gen.state)
            {
                case _STATE.ATTACK:
                    slot.envelope_gen.volume += slot.envelope_gen.attack_rate;
                    if (slot.envelope_gen.volume >= (0x3ff << EG_SHIFT))
                    {
                        slot.envelope_gen.state = _STATE.DECAY1;
                        if (slot.envelope_gen.decay1_rate >= (0x400 << EG_SHIFT)) //Skip DECAY1, go directly to DECAY2
                            slot.envelope_gen.state = _STATE.DECAY2;
                        slot.envelope_gen.volume = 0x3ff << EG_SHIFT;
                    }
                    break;
                case _STATE.DECAY1:
                    slot.envelope_gen.volume -= slot.envelope_gen.decay1_rate;
                    if (slot.envelope_gen.volume <= 0)
                        slot.envelope_gen.volume = 0;
                    if (slot.envelope_gen.volume >> EG_SHIFT <= (slot.envelope_gen.decay_level << (10 - 4)))
                        slot.envelope_gen.state = _STATE.DECAY2;
                    break;
                case _STATE.DECAY2:
                    slot.envelope_gen.volume -= slot.envelope_gen.decay2_rate;
                    if (slot.envelope_gen.volume <= 0)
                        slot.envelope_gen.volume = 0;
                    break;
                case _STATE.RELEASE:
                    slot.envelope_gen.volume -= slot.envelope_gen.release_rate;
                    if (slot.envelope_gen.volume <= 0)
                    {
                        slot.envelope_gen.volume = 0;
                        slot.playing = 0;
                    }
                    break;
                default:
                    return 1 << TL_SHIFT;
            }
            return linear_to_exp_volume[slot.envelope_gen.volume >> EG_SHIFT];
        }

        private UInt32 get_rate(UInt32[] steps, UInt32 rate, UInt32 val)
        {
            Int32 r = (Int32)(4 * val + rate);
            if (val == 0)
                return steps[0];
            if (val == 0xf)
                return steps[0x3f];
            if (r > 0x3f)
                r = 0x3f;
            return steps[r];
        }

        private void envelope_generator_calc(_MultiPCM ptChip, _slot_t slot)
        {
            Int32 octave = ((slot.regs[3] >> 4) - 1) & 0xf;
            Int32 rate;
            if ((octave & 8) != 0)
                octave = octave - 16;

            if (slot.sample.key_rate_scale != 0xf)
                rate = (octave + slot.sample.key_rate_scale) * 2 + ((slot.regs[3] >> 3) & 1);
            else
                rate = 0;

            slot.envelope_gen.attack_rate = (Int32)get_rate(ptChip.attack_step, (UInt32)rate, slot.sample.attack_reg);
            slot.envelope_gen.decay1_rate = (Int32)get_rate(ptChip.decay_release_step, (UInt32)rate, slot.sample.decay1_reg);
            slot.envelope_gen.decay2_rate = (Int32)get_rate(ptChip.decay_release_step, (UInt32)rate, slot.sample.decay2_reg);
            slot.envelope_gen.release_rate = (Int32)get_rate(ptChip.decay_release_step, (UInt32)rate, slot.sample.release_reg);
            slot.envelope_gen.decay_level = 0xf - slot.sample.decay_level;

        }

        /*****************************
                LFO  SECTION
        *****************************/

        private const Int32 LFO_SHIFT = 8;


        //private UInt32 LFIX(float v) { return ((UInt32)((float)(1 << LFO_SHIFT) * (v))); }

        ////Convert DB to multiply amplitude
        //private UInt32 DB(float v) { return LFIX((float)Math.Pow(10.0, (v / 20.0))); }

        ////Convert cents to step increment
        //private UInt32 CENTS(float v) { return LFIX((float)Math.Pow(2.0, v / 1200.0)); }

        private Int32[] pitch_table = new Int32[256];
        private Int32[] amplitude_table = new Int32[256];

        private readonly float[] LFO_FREQ = new float[8] {
    0.168f,
    2.019f,
    3.196f,
    4.206f,
    5.215f,
    5.888f,
    6.224f,
    7.066f
        };  //Hz;

        private readonly float[] PHASE_SCALE_LIMIT = new float[8] {
    0.0f,
    3.378f,
    5.065f,
    6.750f,
    10.114f,
    20.170f,
    40.180f,
    79.307f
        }; //cents

        private readonly float[] AMPLITUDE_SCALE_LIMIT = new float[8] {
    0.0f,
    0.4f,
    0.8f,
    1.5f,
    3.0f,
    6.0f,
    12.0f,
    24.0f
        };                 //DB

        private Int32[][] pitch_scale_tables = new Int32[8][] { new Int32[256], new Int32[256], new Int32[256], new Int32[256], new Int32[256], new Int32[256], new Int32[256], new Int32[256] };
        private Int32[][] amplitude_scale_tables = new Int32[8][] { new Int32[256], new Int32[256], new Int32[256], new Int32[256], new Int32[256], new Int32[256], new Int32[256], new Int32[256] };

        private UInt32 value_to_fixed(UInt32 bits, float value)
        {

            float float_shift = (float)(1 << (int)bits);
            return (UInt32)(float_shift * value);
        }

        private void lfo_init()
        {
            Int32 i, table;

            for (i = 0; i < 256; ++i)
            {
                if (i < 64)
                    pitch_table[i] = i * 2 + 128;
                else if (i < 128)
                    pitch_table[i] = 383 - i * 2;
                else if (i < 192)
                    pitch_table[i] = 384 - i * 2;
                else
                    pitch_table[i] = i * 2 - 383;

                if (i < 128)
                    amplitude_table[i] = 255 - (i * 2);
                else
                    amplitude_table[i] = (i * 2) - 256;
            }

            for (table = 0; table < 8; ++table)
            {
                float limit = PHASE_SCALE_LIMIT[table];
                for (i = -128; i < 128; ++i)
                {
                    float value = (limit * (float)i) / 128.0f;
                    float converted = (float)Math.Pow(2.0f, value / 1200.0f);
                    pitch_scale_tables[table][i + 128] = (int)value_to_fixed(LFO_SHIFT, converted);
                }

                limit = -AMPLITUDE_SCALE_LIMIT[table];
                for (i = 0; i < 256; ++i)
                {
                    float value = (limit * (float)i) / 256.0f;
                    float converted = (float)Math.Pow(10.0f, value / 20.0f);
                    amplitude_scale_tables[table][i] = (int)value_to_fixed(LFO_SHIFT, converted);
                }
            }
        }

        private Int32 pitch_lfo_step(_lfo_t lfo)
        {
            int p;
            lfo.phase += (UInt16)lfo.phase_step;
            p = lfo.table[(lfo.phase >> LFO_SHIFT) & 0xff];
            p = lfo.scale[p];
            return p << (TL_SHIFT - LFO_SHIFT);
        }

        private Int32 amplitude_lfo_step(_lfo_t lfo)
        {
            Int32 p;
            lfo.phase += (UInt16)lfo.phase_step;
            p = lfo.table[(lfo.phase >> LFO_SHIFT) & 0xff];
            p = lfo.scale[p];
            return p << (TL_SHIFT - LFO_SHIFT);
        }

        private void lfo_compute_step(_MultiPCM ptChip, _lfo_t lfo, UInt32 lfo_frequency, UInt32 lfo_scale, Int32 amplitude_lfo)
        {
            float step = (float)(LFO_FREQ[lfo_frequency] * 256.0f / (float)ptChip.rate);
            lfo.phase_step = (UInt32)((float)(1 << LFO_SHIFT) * step);
            if (amplitude_lfo != 0)
            {
                lfo.table = amplitude_table;
                lfo.scale = amplitude_scale_tables[lfo_scale];
            }
            else
            {
                lfo.table = pitch_table;
                lfo.scale = pitch_scale_tables[lfo_scale];
            }
        }



        private void write_slot(_MultiPCM ptChip, _slot_t slot, Int32 reg, byte data)
        {
            slot.regs[reg] = data;

            switch (reg)
            {
                case 0: //PANPOT
                    slot.pan = (UInt32)((data >> 4) & 0xf);
                    break;
                case 1: //Sample
                        //according to YMF278 sample write causes some base params written to the regs (envelope+lfos)
                        //the game should never change the sample while playing.
                        // patched to load all sample data here, so registers 6 and 7 aren't overridden by KeyOn -Valley Bell
                    init_sample(ptChip, slot.sample, (ushort)(slot.regs[1] | ((slot.regs[2] & 1) << 8)));
                    write_slot(ptChip, slot, 6, slot.sample.lfo_vibrato_reg);
                    write_slot(ptChip, slot, 7, slot.sample.lfo_amplitude_reg);
                    break;
                case 2: //Pitch
                case 3:
                    {
                        UInt32 oct = (UInt32)(((slot.regs[3] >> 4) - 1) & 0xf);
                        UInt32 pitch = (UInt32)(((slot.regs[3] & 0xf) << 6) | (slot.regs[2] >> 2));
                        pitch = ptChip.freq_step_table[pitch];
                        if ((oct & 0x8) != 0)
                            pitch >>= (Int32)(16 - oct);
                        else
                            pitch <<= (Int32)oct;
                        slot.step = (UInt32)(pitch / ptChip.rate);
                    }
                    break;
                case 4:     //KeyOn/Off (and more?)
                    {
                        if ((data & 0x80) != 0) //KeyOn
                        {
                            slot.playing = 1;
                            slot.Base = slot.sample.start;
                            slot.offset = 0;
                            slot.prev_sample = 0;
                            slot.total_level = slot.dest_total_level << TL_SHIFT;
                            slot.format = slot.sample.format;

                            envelope_generator_calc(ptChip, slot);
                            slot.envelope_gen.state = _STATE.ATTACK;
                            slot.envelope_gen.volume = 0;

                            if (ptChip.sega_banking != 0)
                            {
                                slot.Base &= 0x1fffff;
                                if (slot.Base >= 0x100000)
                                {
                                    if ((slot.Base & 0x080000) != 0)
                                        slot.Base = (slot.Base & 0x07ffff) | ptChip.bank1;

                                    else
                                        slot.Base = (slot.Base & 0x07ffff) | ptChip.bank0;
                                }
                            }
                        }
                        else
                        {
                            if (slot.playing != 0)
                            {
                                if (slot.sample.release_reg != 0xf)
                                    slot.envelope_gen.state = _STATE.RELEASE;
                                else
                                    slot.playing = 0;
                            }
                        }
                    }
                    break;
                case 5: //TL+Interpolation
                    {
                        slot.dest_total_level = (UInt32)((data >> 1) & 0x7f);
                        if ((data & 1) == 0)    //Interpolate TL
                        {
                            if ((slot.total_level >> TL_SHIFT) > slot.dest_total_level)
                                slot.total_level_step = ptChip.total_level_steps[0]; // decrease
                            else
                                slot.total_level_step = ptChip.total_level_steps[1]; // increase
                        }
                        else
                            slot.total_level = slot.dest_total_level << TL_SHIFT;
                    }
                    break;
                case 6: //LFO freq+PLFO
                    {
                        if (data != 0)
                        {
                            lfo_compute_step(ptChip, slot.pitch_lfo, (UInt32)(slot.regs[6] >> 3) & 7, (UInt32)(slot.regs[6] & 7), 0);
                            lfo_compute_step(ptChip, slot.amplitude_lfo, (UInt32)(slot.regs[6] >> 3) & 7, (UInt32)(slot.regs[7] & 7), 1);
                        }
                    }
                    break;
                case 7: //ALFO
                    {
                        if (data != 0)
                        {
                            lfo_compute_step(ptChip, slot.pitch_lfo, (UInt32)(slot.regs[6] >> 3) & 7, (UInt32)(slot.regs[6] & 7), 0);
                            lfo_compute_step(ptChip, slot.amplitude_lfo, (UInt32)(slot.regs[6] >> 3) & 7, (UInt32)(slot.regs[7] & 7), 1);
                        }
                    }
                    break;
            }
        }

        private byte read_byte(_MultiPCM ptChip, UInt32 addr)
        {
            return ptChip.ROM[addr & ptChip.ROMMask];
        }

        //static STREAM_UPDATE( MultiPCM_update )
        public void MultiPCM_update(byte ChipID, Int32[][] outputs, Int32 samples)
        {
            //MultiPCM *ptChip = (MultiPCM *)param;
            _MultiPCM ptChip = MultiPCMData[ChipID];
            //Int32[][] datap = new Int32[2][];
            Int32 i, sl;

            //datap[0] = outputs[0];
            //datap[1] = outputs[1];

            for (int j = 0; j < samples; j++)
            {
                outputs[0][j] = 0;
                outputs[1][j] = 0;
            }
            //memset(datap[0], 0, sizeof(*datap[0]) * samples);
            //memset(datap[1], 0, sizeof(*datap[1]) * samples);


            for (i = 0; i < samples; ++i)
            {
                Int32 smpl = 0;
                Int32 smpr = 0;
                for (sl = 0; sl < 28; ++sl)
                {
                    _slot_t slot = ptChip.slots[sl];
                    if (slot.playing != 0 && slot.muted == 0)
                    {
                        UInt32 vol = (slot.total_level >> TL_SHIFT) | (slot.pan << 7);
                        UInt32 spos = slot.offset >> TL_SHIFT;
                        UInt32 step = slot.step;
                        Int32 csample = 0;
                        Int32 fpart = (Int32)(slot.offset & ((1 << TL_SHIFT) - 1));
                        Int32 sample;// = (csample * fpart + slot.prev_sample * ((1 << SHIFT) - fpart)) >> SHIFT;


                        if ((slot.format & 8) != 0)   // 12-bit linear
                        {
                            UInt32 adr = slot.Base + (spos >> 2) * 6;
                            switch (spos & 3)
                            {
                                case 0:
                                    { // ab.c .... ....
                                        Int16 w0 = (short)(read_byte(ptChip, adr) << 8 | ((read_byte(ptChip, adr + 1) & 0xf) << 4));
                                        csample = w0;
                                        break;
                                    }
                                case 1:
                                    { // ..C. AB.. ....
                                        Int16 w0 = (short)((read_byte(ptChip, adr + 2) << 8) | (read_byte(ptChip, adr + 1) & 0xf0));
                                        csample = w0;
                                        break;
                                    }
                                case 2:
                                    { // .... ..ab .c..
                                        Int16 w0 = (short)(read_byte(ptChip, adr + 3) << 8 | ((read_byte(ptChip, adr + 4) & 0xf) << 4));
                                        csample = w0;
                                        break;
                                    }
                                case 3:
                                    { // .... .... C.AB
                                        Int16 w0 = (short)((read_byte(ptChip, adr + 5) << 8) | (read_byte(ptChip, adr + 4) & 0xf0));
                                        csample = w0;
                                        break;
                                    }
                            }
                        }
                        else
                        {
                            csample = (Int16)(read_byte(ptChip, slot.Base + spos) << 8);
                        }

                        sample = (csample * fpart + slot.prev_sample * ((1 << TL_SHIFT) - fpart)) >> TL_SHIFT;

                        if ((slot.regs[6] & 7) != 0) // Vibrato enabled
                        {
                            step = (uint)(step * pitch_lfo_step(slot.pitch_lfo));
                            step >>= TL_SHIFT;
                        }

                        slot.offset += step;
                        if (slot.offset >= (slot.sample.end << TL_SHIFT))
                        {
                            slot.offset = slot.sample.loop << TL_SHIFT;
                        }

                        if ((spos ^ (slot.offset >> TL_SHIFT)) != 0)
                        {
                            slot.prev_sample = csample;
                        }

                        if ((slot.total_level >> TL_SHIFT) != slot.dest_total_level)
                        {
                            slot.total_level += (uint)slot.total_level_step;
                        }

                        if ((slot.regs[7] & 7) != 0) // Tremolo enabled
                        {
                            sample = sample * amplitude_lfo_step(slot.amplitude_lfo);
                            sample >>= TL_SHIFT;
                        }

                        sample = (sample * envelope_generator_update(slot)) >> 10;

                        smpl += (left_pan_table[vol] * sample) >> TL_SHIFT;
                        smpr += (right_pan_table[vol] * sample) >> TL_SHIFT;
                    }
                }

                outputs[0][i] = smpl;
                outputs[1][i] = smpr;
            }
        }

        //READ8_DEVICE_HANDLER( multipcm_r )
        public _MultiPCM multipcm_r(int ChipID)//, Int32 offset)
        {
            return MultiPCMData[ChipID];
        }

        //static DEVICE_START( multipcm )
        public Int32 device_start_multipcm(byte ChipID, int clock)
        {
            //MultiPCM *ptChip = get_safe_token(device);
            _MultiPCM ptChip;
            int i;

            if (ChipID >= MAX_CHIPS)
                return 0;

            ptChip = MultiPCMData[ChipID];
            if (ptChip == null)
                return -1;

            ptChip.ROM = null;
            ptChip.ROMMask = 0x00;
            ptChip.ROMSize = 0x00;
            ptChip.ROM = null;
            //ptChip->Rate=(float) device->clock() / MULTIPCM_CLOCKDIV;
            ptChip.rate = (float)(clock / MULTIPCM_CLOCKDIV);


            if (IsInit == 0)
            {
                Int32 level;

                IsInit = 1;

                //Volume+pan table
                for (level = 0; level < 0x80; ++level)
                {
                    float vol_db = (float)level * (-24.0f) / 64.0f;
                    float total_level = (float)Math.Pow(10.0f, vol_db / 20.0f) / 4.0f;
                    Int32 pan;

                    for (pan = 0; pan < 0x10; ++pan)
                    {
                        float pan_left, pan_right;
                        if (pan == 0x8)
                        {
                            pan_left = 0.0f;
                            pan_right = 0.0f;
                        }
                        else if (pan == 0x0)
                        {
                            pan_left = 1.0f;
                            pan_right = 1.0f;
                        }
                        else if ((pan & 0x8) != 0)
                        {
                            Int32 inverted_pan = 0x10 - pan;
                            float pan_vol_db = (float)inverted_pan * (-12.0f) / 4.0f;

                            pan_left = 1.0f;
                            pan_right = (float)Math.Pow(10.0f, pan_vol_db / 20.0f);

                            if ((inverted_pan & 0x7) == 7)
                                pan_right = 0.0f;
                        }
                        else
                        {
                            float pan_vol_db = (float)pan * (-12.0f) / 4.0f;

                            pan_left = (float)Math.Pow(10.0f, pan_vol_db / 20.0f);
                            pan_right = 1.0f;

                            if ((pan & 0x7) == 7)
                                pan_left = 0.0f;
                        }

                        left_pan_table[(pan << 7) | level] = (int)value_to_fixed(TL_SHIFT, pan_left * total_level);
                        right_pan_table[(pan << 7) | level] = (int)value_to_fixed(TL_SHIFT, pan_right * total_level);

                    }
                }

                // build the linear->exponential ramps
                for (i = 0; i < 0x400; ++i)
                {
                    float db = -(96.0f - (96.0f * (float)i / (float)0x400));
                    float exp_volume = (float)Math.Pow(10.0f, db / 20.0f);
                    linear_to_exp_volume[i] = (int)value_to_fixed(TL_SHIFT, exp_volume);
                }

                lfo_init();
            }

            //Pitch steps
            for (i = 0; i < 0x400; ++i)
            {
                float fcent = ptChip.rate * (1024.0f + (float)i) / 1024.0f;
                ptChip.freq_step_table[i] = value_to_fixed(TL_SHIFT, fcent);
            }

            //Envelope steps
            for (i = 0; i < 0x40; ++i)
            {
                //Times are based on 44100 clock, adjust to real chip clock
                ptChip.attack_step[i] = (UInt32)((float)(0x400 << EG_SHIFT) / (BASE_TIMES[i] * 44100.0 / (1000.0)));
                ptChip.decay_release_step[i] = (UInt32)((float)(0x400 << EG_SHIFT) / (BASE_TIMES[i] * attack_rate_to_decay_rate * 44100.0 / (1000.0)));
            }
            ptChip.attack_step[0] = ptChip.attack_step[1] = ptChip.attack_step[2] = ptChip.attack_step[3] = 0;
            ptChip.attack_step[0x3f] = 0x400 << EG_SHIFT;
            ptChip.decay_release_step[0] = ptChip.decay_release_step[1] = ptChip.decay_release_step[2] = ptChip.decay_release_step[3] = 0;

            //TL Interpolation steps
            //lower
            ptChip.total_level_steps[0] = -(Int32)((float)(0x80 << TL_SHIFT) / (78.2 * 44100.0 / 1000.0));
            //raise
            ptChip.total_level_steps[1] = (Int32)((float)(0x80 << TL_SHIFT) / (78.2 * 2 * 44100.0 / 1000.0));

            ptChip.sega_banking = 0;
            ptChip.bank0 = ptChip.bank1 = 0x000000;

            multipcm_set_mute_mask(ChipID, 0x00);

            //ptChip._devData.chipInf = ptChip;
            //INIT_DEVINF(retDevInf, ptChip._devData, (UInt32)ptChip.rate, &devDef);

            return (Int32)(ptChip.rate + 0.5);
        }


        public void device_stop_multipcm(byte ChipID)
        {
            _MultiPCM ptChip = MultiPCMData[ChipID];

            //free(ptChip->ROM); 
            ptChip.ROM = null;

            return;
        }

        public void device_reset_multipcm(byte ChipID)
        {
            _MultiPCM ptChip = MultiPCMData[ChipID];
            int i;

            for (i = 0; i < 28; ++i)
            {
                ptChip.slots[i].slot_index = (byte)i;
                ptChip.slots[i].playing = 0;
            }

            return;
        }


        public override string Name { get { return "Multi PCM"; } set { } }
        public override string ShortName { get { return "mPCM"; } set { } }


        //WRITE8_DEVICE_HANDLER( multipcm_w )
        private void multipcm_write(byte ChipID, Int32 offset, byte data)
        {
            //MultiPCM *ptChip = get_safe_token(device);
            _MultiPCM ptChip = MultiPCMData[ChipID];
            switch (offset)
            {
                case 0:     //Data write
                    if (ptChip.cur_slot == -1)
                        return;
                    write_slot(ptChip, ptChip.slots[ptChip.cur_slot], ptChip.address, data);
                    break;
                case 1:
                    ptChip.cur_slot = VALUE_TO_CHANNEL[data & 0x1f];
                    break;

                case 2:
                    ptChip.address = (data > 7) ? 7 : data;
                    break;

                // special SEGA banking
                case 0x10:  // 1 MB banking (Sega Model 1)
                    ptChip.sega_banking = 1;
                    ptChip.bank0 = (uint)((data << 20) | 0x000000);
                    ptChip.bank1 = (uint)((data << 20) | 0x080000);
                    break;
                case 0x11:  // 512 KB banking - low bank (Sega Multi 32)
                    ptChip.sega_banking = 1;
                    ptChip.bank0 = (uint)(data << 19);
                    break;
                case 0x12:  // 512 KB banking - high bank (Sega Multi 32)
                    ptChip.sega_banking = 1;
                    ptChip.bank1 = (uint)(data << 19);
                    break;
            }
        }

        private void multipcm_w_quick(byte ChipID, byte offset, byte data)
        {
            _MultiPCM ptChip = MultiPCMData[ChipID];

            ptChip.cur_slot = VALUE_TO_CHANNEL[(offset >> 3) & 0x1F];
            ptChip.address = offset & 0x07;
            if (ptChip.cur_slot == -1)
                return;
            write_slot(ptChip, ptChip.slots[ptChip.cur_slot], ptChip.address, data);
        }

        /* MAME/M1 access functions */

        public void multipcm_alloc_rom(byte ChipID, UInt32 memsize)
        {
            _MultiPCM ptChip = MultiPCMData[ChipID];

            if (ptChip.ROMSize == memsize)
                return;

            ptChip.ROM =new byte[memsize];
            ptChip.ROMSize = memsize;
            for (int i = 0; i < memsize; i++) ptChip.ROM[i] = 0xFF;

            ptChip.ROMMask = common.pow2_mask(memsize);

            return;
        }

        public void multipcm_write_rom(byte ChipID, UInt32 offset, UInt32 length, byte[] data)
        {
            _MultiPCM ptChip = MultiPCMData[ChipID];

            if (offset > ptChip.ROMSize)
                return;
            if (offset + length > ptChip.ROMSize)
                length = (uint)(ptChip.ROMSize - offset);

            for (int i = 0; i < length; i++) ptChip.ROM[i + offset] = data[i];

            return;
        }

        public void multipcm_write_rom2(byte ChipID,UInt32 ROMSize, UInt32 offset, UInt32 length, byte[] data, Int32 srcStartAddress)
        {
            _MultiPCM ptChip = MultiPCMData[ChipID];

            if (ptChip.ROM == null || ptChip.ROM.Length<ROMSize)
            {
                multipcm_alloc_rom(ChipID, ROMSize);
            }

            if (offset > ptChip.ROMSize)
                return;
            if (offset + length > ptChip.ROMSize)
                length = (uint)(ptChip.ROMSize - offset);

            for (int i = 0; i < length; i++)
            {
                if (data.Length <= i + srcStartAddress) break;
                ptChip.ROM[i + offset] = data[i + srcStartAddress];
            }


            return;
        }

        public void multipcm_set_mute_mask(byte ChipID, UInt32 MuteMask)
        {
            _MultiPCM ptChip = MultiPCMData[ChipID];
            byte CurChn;

            for (CurChn = 0; CurChn < 28; CurChn++)
                ptChip.slots[CurChn].muted = (byte)((MuteMask >> CurChn) & 0x01);

            return;
        }

        public override int Write(byte ChipID, int port, int adr, int data)
        {
            multipcm_write(ChipID, adr, (byte)data);
            //multipcm_w_quick(ChipID, (byte)adr, (byte)data);
            return 0;
        }

        public void multipcm_bank_write(byte chipID, byte ch, ushort adr)
        {
            byte bankmask =(byte)( ch & 0x03);
            if (bankmask == 0x03 && (adr & 0x08)==0)
            {
                // 1 MB banking (reg 0x10)
                multipcm_write(chipID, 0x10, (byte)(adr / 0x10));
            }
            else
            {
                // 512 KB banking (regs 0x11/0x12)
                if ((bankmask & 0x02)!=0)    // low bank
                    multipcm_write(chipID, 0x11, (byte)(adr / 0x08));
                if ((bankmask & 0x01)!=0)    // high bank
                    multipcm_write(chipID, 0x12, (byte)(adr / 0x08));
            }
        }
    }
}
