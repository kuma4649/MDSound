using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static System.Collections.Specialized.BitVector32;

namespace MDSound
{
    public class zsg2 : Instrument
    {
        public override string Name { get => "ZSG2"; set { } }
        public override string ShortName { get => "ZSG2"; set { } }


        public override void Reset(byte ChipID)
        {
            device_reset(ChipID);
        }

        public override uint Start(byte ChipID, uint clock)
        {
            return clock;
        }

        public override uint Start(byte ChipID, uint clock, uint ClockValue, params object[] option)
        {
            //TBD
            // クロックの処理が未実装
            //sampleRate[ChipID] = clock / 768;
            //baseClock[ChipID] = ClockValue;

            return clock;
        }

        public override void Stop(byte ChipID)
        {
        }

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {
            //TBD
            //  更新タイミングの計算
            //  ZSG2は出力が4chである

            sound_stream_update(ChipID, outputs, samples);
        }

        public override int Write(byte ChipID, int port, int adr, int data)
        {
            write(ChipID, adr, (ushort)data, 0xffff);
            return 0;
        }



        //        // license:BSD-3-Clause
        //        // copyright-holders:Olivier Galibert, R. Belmont, hap, superctr
        //        /*
        //            ZOOM ZSG-2 custom wavetable synthesizer
        //        */

        //# ifndef MAME_SOUND_ZSG2_H
        //#define MAME_SOUND_ZSG2_H

        //#pragma once

        //        // ======================> zsg2_device

        //        class zsg2_device : public device_t,
        //					public device_sound_interface
        //{
        //public:
        //	zsg2_device(const machine_config &mconfig, const char* tag, device_t *owner, uint32_t clock);

        //        // configuration helpers
        //        auto ext_read() { return m_ext_read_handler.bind(); }

        //        uint16_t read(offs_t offset, uint16_t mem_mask = ~0);
        //        void write(offs_t offset, uint16_t data, uint16_t mem_mask = ~0);

        //        protected:
        //	// device-level overrides
        //	virtual void device_start() override;
        //	virtual void device_reset() override;

        //	// sound stream update overrides
        //	virtual void sound_stream_update(sound_stream &stream, std::vector<read_stream_view> const &inputs, std::vector<write_stream_view> &outputs) override;

        //private:
        private const ushort STATUS_ACTIVE = 0x8000;

        // 16 registers per channel, 48 channels
        private class zchan
        {
            public ushort[] v = new ushort[16];
            public ushort status;
            public uint cur_pos;
            public uint step_ptr;
            public uint step;
            public uint start_pos;
            public uint end_pos;
            public uint loop_pos;
            public uint page;

            public ushort vol;
            public ushort vol_initial;
            public ushort vol_target;
            public short vol_delta;

            public ushort output_cutoff;
            public ushort output_cutoff_initial;
            public ushort output_cutoff_target;
            public short output_cutoff_delta;

            public int emphasis_filter_state;

            public int output_filter_state;

            // Attenuation for output channels
            public byte[] output_gain = new byte[4];

            public short[] samples = new short[5]; // +1 history
        };

        private ushort[][] m_gain_tab = new ushort[2][] { new ushort[256], new ushort[256] };
        private ushort[][] m_reg = new ushort[2][] { new ushort[32], new ushort[32] };

        private zchan[][] m_chan = new zchan[2][] { new zchan[48], new zchan[48] };
        private uint[] m_sample_count = new uint[2];

        private uint[][] m_mem_base = new uint[2][];
        private uint[] m_read_address = new uint[2];
        private uint[][] m_mem_copy=new uint[2][];
        private uint[] m_mem_blocks=new uint[2];
        private short[][] m_full_samples=new short[2][];
        //        sound_stream* m_stream;
        //        devcb_read32 m_ext_read_handler;

        //        uint32_t read_memory(uint32_t offset);
        //        void chan_w(int ch, int reg, uint16_t data);
        //        uint16_t chan_r(int ch, int reg);
        //        void control_w(int reg, uint16_t data);
        //        uint16_t control_r(int reg);
        //        int16_t* prepare_samples(uint32_t offset);
        //        void filter_samples(zchan* ch);
        //        int16_t get_ramp(uint8_t val);
        //        inline uint16_t ramp(uint16_t current, uint16_t target, int16_t delta);
        //    };

        //    DECLARE_DEVICE_TYPE(ZSG2, zsg2_device)

        //#endif // MAME_SOUND_ZSG2_H





        // license:BSD-3-Clause
        // copyright-holders:Olivier Galibert, R. Belmont, hap, superctr
        /*
            ZOOM ZSG-2 custom wavetable synthesizer

            Written by Olivier Galibert
            MAME conversion by R. Belmont
            Working emulation by The Talentuous Hands Of The Popularious hap
            Properly working emulation by superctr
            ---------------------------------------------------------

            Register map:
            000-5fe : Channel specific registers (48 channels)
                      (high)   (low)
               +000 : xxxxxxxx -------- : Start address (low)
               +000 : -------- xxxxxxxx :   Unknown register (usually cleared)
               +002 : xxxxxxxx -------- : Address page
                    : -------- xxxxxxxx : Start address (high)
               +004 : -------- -------- :   Unknown register (usually cleared)
               +006 : -----x-- -------- :   Unknown bit, always set
               +008 : xxxxxxxx xxxxxxxx : Frequency
               +00a : xxxxxxxx -------- : DSP ch 3 (right) output gain
                    : -------- xxxxxxxx : Loop address (low)
               +00c : xxxxxxxx xxxxxxxx : End address
               +00e : xxxxxxxx -------- : DSP ch 2 (Left) output gain
                    : -------- xxxxxxxx : Loop address (high)
               +010 : xxxxxxxx xxxxxxxx : Initial filter time constant
               +012 : xxxxxxxx xxxxxxxx : Current filter time constant
               +014 : xxxxxxxx xxxxxxxx : Initial volume
               +016 : xxxxxxxx xxxxxxxx : Current volume?
               +018 : xxxxxxxx xxxxxxxx : Target filter time constant
               +01a : xxxxxxxx -------- : DSP ch 1 (chorus) output gain
                    : -------- xxxxxxxx : Filter ramping speed
               +01c : xxxxxxxx xxxxxxxx : Target volume
               +01e : xxxxxxxx -------- : DSP ch 0 (reverb) output gain
                    : -------- xxxxxxxx : Filter ramping speed
            600-604 : Key on flags (each bit corresponds to a channel)
            608-60c : Key off flags (each bit corresponds to a channel)
            618     : Unknown register (usually 0x5cbc is written)
            61a     : Unknown register (usually 0x5cbc is written)
            620     : Unknown register (usually 0x0128 is written)
            628     : Unknown register (usually 0x0066 is written)
            630     : Unknown register (usually 0x0001 is written)
            638     : ROM readback address low
            63a     : ROM readback address high
            63c     : ROM readback word low
            63e     : ROM readback word high

            ---------------------------------------------------------

            Additional notes on the sample format, reverse-engineered
            by Olivier Galibert and David Haywood:

            The zoom sample rom is decomposed in 0x40000 bytes pages.  Each page
            starts by a header and is followed by compressed samples.

            The header is a vector of 16 bytes structures composed of 4 32bits
            little-endian values representing:
            - sample start position in bytes, always a multiple of 4
            - sample end position in bytes, minus 4, always...
            - loop position in bytes, always....
            - flags, probably

            It is interesting to note that this header is *not* parsed by the
            ZSG.  The main program reads the rom through appropriate ZSG
            commands, and use the results in subsequent register setups.  It's
            not even obvious that the ZSG cares about the pages, it may just
            see the address space as linear.  In the same line, the
            interpretation of the flags is obviously dependent on the main
            program, not the ZSG, but some of the bits are directly copied to
            some of the registers.

            The samples are compressed with a 2:1 ratio.  Each block of 4-bytes
            becomes 4 16-bits samples.  Reading the 4 bytes as a *little-endian*
            32bits values, the structure is:

            42222222 51111111 60000000 ssss3333

            's' is a 4-bit scale value.  '0000000', '1111111', '2222222' and
            '6543333' are signed 7-bits values corresponding to the 4 samples.
            To compute the final 16bits value, left-align and shift right by s.
            Yes, that simple.

            ---------------------------------------------------------

        TODO:
        - Filter and ramping behavior might not be perfect.
        - clicking / popping noises in gdarius, raystorm: maybe the sample ROMs are bad dumps?
        - memory reads out of range sometimes

        */

        //# include "emu.h"
        //# include "zsg2.h"

        //# include <algorithm>
        //# include <fstream>
        //# include <cmath>

        private const int EMPHASIS_INITIAL_BIAS = 0;
        // Adjusts the cutoff constant of the filter by right-shifting the filter state.
        // The current value gives a -6dB cutoff frequency at about 81.5 Hz, assuming
        // sample playback at 32.552 kHz.
        private const int EMPHASIS_FILTER_SHIFT = (16 - 10);
        private const int EMPHASIS_ROUNDING = 0x20;
        // Adjusts the output amplitude by right-shifting the filtered output. Should be
        // kept relative to the filter shift. A too low value will cause clipping, while
        // too high will cause quantization noise.
        private const int EMPHASIS_OUTPUT_SHIFT = 1;

        // device type definition
        // DEFINE_DEVICE_TYPE(ZSG2, zsg2_device, "zsg2", "ZOOM ZSG-2")

        //-------------------------------------------------
        //  zsg2_device - constructor
        //-------------------------------------------------

        //zsg2_device::zsg2_device(const machine_config &mconfig, const char* tag, device_t *owner, uint32_t clock)
        //	: device_t(mconfig, ZSG2, tag, owner, clock)
        //	, device_sound_interface(mconfig, *this)
        //	, m_mem_base(*this, DEVICE_SELF)
        //	, m_read_address(0)
        //	, m_ext_read_handler(*this, 0)
        //        {
        //        }

        //-------------------------------------------------
        //  device_start - device-specific startup
        //-------------------------------------------------

        private void device_start(byte ChipID)
        {
            m_chan[ChipID] = new zchan[48];

            //m_stream = stream_alloc(0, 4, clock() / 768);
            m_mem_blocks[ChipID] = (uint)m_mem_base[ChipID].Length;
            m_mem_copy[ChipID] = new uint[m_mem_blocks[ChipID]];
            m_full_samples[ChipID] = new short[m_mem_blocks[ChipID] * 4 + 4]; // +4 is for empty block

            // register for savestates
            //save_pointer(NAME(m_mem_copy), m_mem_blocks / sizeof(uint32_t));
            //save_pointer(NAME(m_full_samples), (m_mem_blocks * 4 + 4) / sizeof(int16_t));
            //save_item(NAME(m_read_address));

            // Generate the output gain table. Assuming -1dB per step for now.
            for (int i = 1; i < 32; i++)
            {
                double val = Math.Pow(10, -(31 - i) / 20.0) * 65535.0;
                m_gain_tab[ChipID][i] = (ushort)val;
            }
            m_gain_tab[ChipID][0] = 0;

            //for (int ch = 0; ch < 48; ch++)
            //{
            //    save_item(NAME(m_chan[ch].v), ch);
            //    save_item(NAME(m_chan[ch].status), ch);
            //    save_item(NAME(m_chan[ch].cur_pos), ch);
            //    save_item(NAME(m_chan[ch].step_ptr), ch);
            //    save_item(NAME(m_chan[ch].step), ch);
            //    save_item(NAME(m_chan[ch].start_pos), ch);
            //    save_item(NAME(m_chan[ch].end_pos), ch);
            //    save_item(NAME(m_chan[ch].loop_pos), ch);
            //    save_item(NAME(m_chan[ch].page), ch);
            //    save_item(NAME(m_chan[ch].vol), ch);
            //    save_item(NAME(m_chan[ch].vol_initial), ch);
            //    save_item(NAME(m_chan[ch].vol_target), ch);
            //    save_item(NAME(m_chan[ch].vol_delta), ch);
            //    save_item(NAME(m_chan[ch].output_cutoff), ch);
            //    save_item(NAME(m_chan[ch].output_cutoff_initial), ch);
            //    save_item(NAME(m_chan[ch].output_cutoff_target), ch);
            //    save_item(NAME(m_chan[ch].output_cutoff_delta), ch);
            //    save_item(NAME(m_chan[ch].emphasis_filter_state), ch);
            //    save_item(NAME(m_chan[ch].output_filter_state), ch);
            //    save_item(NAME(m_chan[ch].output_gain), ch);
            //    save_item(NAME(m_chan[ch].samples), ch);
            //}
            //save_item(NAME(m_sample_count));
        }

        //-------------------------------------------------
        //  device_reset - device-specific reset
        //-------------------------------------------------

        private void device_reset(byte ChipID)
        {
            m_read_address[ChipID] = 0;

            // stop playing and clear all channels
            control_w(ChipID, 4, 0xffff);
            control_w(ChipID, 5, 0xffff);
            control_w(ChipID, 6, 0xffff);

            for (int ch = 0; ch < 48; ch++)
                for (int reg = 0; reg < 0x10; reg++)
                    chan_w(ChipID, ch, reg, 0);
            m_sample_count[ChipID] = 0;

            //#if DEBUG
            //            for (int i = 0; i < m_mem_blocks; i++)
            //                prepare_samples(i);

            //            FILE* f;

            //            f = fopen("zoom_samples.bin", "wb");
            //            fwrite(m_mem_copy.get(), 1, m_mem_blocks * 4, f);
            //            fclose(f);

            //            f = fopen("zoom_samples.raw", "wb");
            //            fwrite(m_full_samples.get(), 2, m_mem_blocks * 4, f);
            //            fclose(f);
            //#endif
        }

        /******************************************************************************/

        private uint read_memory(byte ChipID,uint offset)
        {
            if (offset >= m_mem_blocks[ChipID])
                return 0;

            //if (m_ext_read_handler.isunset())
            return m_mem_base[ChipID][offset];

            //return m_ext_read_handler(offset);
        }

        private short[] prepare_samples(byte ChipID,uint offset, out uint ptr)
        {
            uint block = read_memory(ChipID, offset);

            if (block == 0)
            {
                ptr = m_mem_blocks[ChipID];
                return m_full_samples[ChipID]; // overflow or 0
            }

            if (block == m_mem_copy[ChipID][offset])
            {
                ptr = offset * 4;
                return m_full_samples[ChipID]; // cached
            }

            m_mem_copy[ChipID][offset] = block;
            offset *= 4;

            // decompress 32 bit block to 4 16-bit samples
            // 42222222 51111111 60000000 ssss3333
            m_full_samples[ChipID][offset | 0] = (short)(block >> 8 & 0x7f);
            m_full_samples[ChipID][offset | 1] = (short)(block >> 16 & 0x7f);
            m_full_samples[ChipID][offset | 2] = (short)(block >> 24 & 0x7f);
            m_full_samples[ChipID][offset | 3] = (short)((block >> (8 + 1) & 0x40) | (block >> (16 + 2) & 0x20) | (block >> (24 + 3) & 0x10) | (block & 0xf));

            // sign-extend and shift
            byte shift = (byte)(block >> 4 & 0xf);
            for (uint i = offset; i < (offset + 4); i++)
            {
                m_full_samples[ChipID][i] <<= 9;
                m_full_samples[ChipID][i] >>= shift;
            }

            ptr = offset;
            return m_full_samples[ChipID];
        }

        // Fill the buffer with filtered samples
        private void filter_samples(byte ChipID,ref zchan ch)
        {
            short[] raw_samples = prepare_samples(ChipID, ch.page | ch.cur_pos, out uint ptr);
            ch.samples[0] = ch.samples[4]; // we want to remember the last sample

            for (int i = 0; i < 4; i++)
            {
                ch.emphasis_filter_state += raw_samples[ptr + i] - ((ch.emphasis_filter_state + EMPHASIS_ROUNDING) >> EMPHASIS_FILTER_SHIFT);

                int sample = ch.emphasis_filter_state >> EMPHASIS_OUTPUT_SHIFT;
                ch.samples[i + 1] = (short)Math.Min(Math.Max(sample, -32768), 32767);
            }
        }

        //-------------------------------------------------
        //  sound_stream_update - handle a stream update
        //-------------------------------------------------

        private void sound_stream_update(byte ChipID,int[][] outputs, int samples)
        {
            for (int i = 0; i < samples; i++)
            {
                int[] mix = new int[4];

                // loop over all channels
                for (int j = 0; j < m_chan[ChipID].Length; j++)
                {
                    zchan elem = m_chan[ChipID][j];
                    if ((~elem.status & STATUS_ACTIVE) != 0)
                        continue;

                    elem.step_ptr += elem.step;
                    if ((elem.step_ptr & 0xffff0000) != 0)
                    {
                        if (++elem.cur_pos >= elem.end_pos)
                        {
                            // loop sample
                            elem.cur_pos = elem.loop_pos;
                            if ((elem.cur_pos + 1) >= elem.end_pos)
                            {
                                // end of sample
                                elem.vol = 0; //this should help the channel allocation just a bit
                                elem.status &= unchecked((ushort)~STATUS_ACTIVE);
                                continue;
                            }
                        }

                        if (elem.cur_pos == elem.start_pos)
                            elem.emphasis_filter_state = EMPHASIS_INITIAL_BIAS;

                        elem.step_ptr &= 0xffff;
                        filter_samples(ChipID, ref elem);
                    }

                    byte sample_pos = (byte)(elem.step_ptr >> 14 & 3);
                    int sample = elem.samples[sample_pos];

                    // linear interpolation (hardware certainly does something similar)
                    sample += ((ushort)(elem.step_ptr << 2 & 0xffff) * (short)(elem.samples[sample_pos + 1] - sample)) >> 16;

                    // another filter...
                    elem.output_filter_state += (sample - (elem.output_filter_state >> 16)) * elem.output_cutoff;
                    sample = elem.output_filter_state >> 16;

                    // To prevent DC bias, we need to slowly discharge the filter when the output filter cutoff is 0
                    if (elem.output_cutoff == 0)
                        elem.output_filter_state >>= 1;

                    sample = (sample * elem.vol) >> 16;

                    for (int output = 0; output < 4; output++)
                    {
                        int output_gain = elem.output_gain[output] & 0x1f; // left / right
                        int output_sample = sample;

                        if ((elem.output_gain[output] & 0x80) != 0) // perhaps ?
                            output_sample = -output_sample;

                        mix[output] += (output_sample * m_gain_tab[ChipID][output_gain & 0x1f]) >> 16;
                    }

                    // Apply ramping every other update
                    // It's possible key on is handled on the other sample
                    if ((m_sample_count[ChipID] & 1) != 0)
                    {
                        elem.vol = ramp(elem.vol, elem.vol_target, elem.vol_delta);
                        elem.output_cutoff = ramp(elem.output_cutoff, elem.output_cutoff_target, elem.output_cutoff_delta);
                    }
                }

                for (int output = 0; output < 4; output++)
                    outputs[output][i] = Math.Min(Math.Max(mix[output], -32768), 32767);
            }
            m_sample_count[ChipID]++;
        }

        /******************************************************************************/

        private void chan_w(byte ChipID,int ch, int reg, ushort data)
        {
            switch (reg)
            {
                case 0x0:
                    // lo byte: unknown, 0 on most games
                    // hi byte: start address low
                    m_chan[ChipID][ch].start_pos = (m_chan[ChipID][ch].start_pos & 0xff00) | (uint)(data >> 8 & 0xff);
                    break;

                case 0x1:
                    // lo byte: start address high
                    // hi byte: address page
                    m_chan[ChipID][ch].start_pos = (m_chan[ChipID][ch].start_pos & 0x00ff) | (uint)(data << 8 & 0xff00);
                    m_chan[ChipID][ch].page = (uint)(data << 8 & 0xff0000);
                    break;

                case 0x2:
                    // no function? always 0
                    break;

                case 0x3:
                    // unknown, always 0x0400. is this a flag?
                    m_chan[ChipID][ch].status &= 0x8000;
                    m_chan[ChipID][ch].status |= (ushort)(data & 0x7fff);
                    break;

                case 0x4:
                    // frequency
                    m_chan[ChipID][ch].step = (uint)(data + 1);
                    break;

                case 0x5:
                    // lo byte: loop address low
                    // hi byte: right output gain (direct)
                    m_chan[ChipID][ch].loop_pos = (m_chan[ChipID][ch].loop_pos & 0xff00) | (uint)(data & 0xff);
                    m_chan[ChipID][ch].output_gain[3] = (byte)(data >> 8);
                    break;

                case 0x6:
                    // end address
                    m_chan[ChipID][ch].end_pos = data;
                    break;

                case 0x7:
                    // lo byte: loop address high
                    // hi byte: left output gain (direct)
                    m_chan[ChipID][ch].loop_pos = (m_chan[ChipID][ch].loop_pos & 0x00ff) | (uint)(data << 8 & 0xff00);
                    m_chan[ChipID][ch].output_gain[2] = (byte)(data >> 8);
                    break;

                case 0x8:
                    // IIR lowpass time constant (initial, latched on key on)
                    m_chan[ChipID][ch].output_cutoff_initial = data;
                    break;

                case 0x9:
                    // writes 0 at key on
                    m_chan[ChipID][ch].output_cutoff = data;
                    break;

                case 0xa:
                    // volume (initial, latched on key on)
                    m_chan[ChipID][ch].vol_initial = data;
                    break;

                case 0xb:
                    // writes 0 at key on
                    m_chan[ChipID][ch].vol = data;
                    break;

                case 0xc:
                    // IIR lowpass time constant (target)
                    m_chan[ChipID][ch].output_cutoff_target = data;
                    break;

                case 0xd:
                    // hi byte: DSP channel 1 (chorus) gain
                    // lo byte: Filter ramping speed
                    m_chan[ChipID][ch].output_gain[1] = (byte)(data >> 8);
                    m_chan[ChipID][ch].output_cutoff_delta = get_ramp((byte)(data & 0xff));
                    break;

                case 0xe:
                    // volume target
                    m_chan[ChipID][ch].vol_target = data;
                    break;

                case 0xf:
                    // hi byte: DSP channel 0 (reverb) gain
                    // lo byte: Volume ramping speed
                    m_chan[ChipID][ch].output_gain[0] = (byte)(data >> 8);
                    m_chan[ChipID][ch].vol_delta = get_ramp((byte)(data & 0xff));
                    break;

                default:
                    break;
            }

            m_chan[ChipID][ch].v[reg] = data;
        }

        private ushort chan_r(byte ChipID,int ch, int reg)
        {
            switch (reg)
            {
                case 0x3:
                    // no games read from this.
                    return m_chan[ChipID][ch].status;
                case 0x9:
                    // pretty certain, though no games actually read from this.
                    return m_chan[ChipID][ch].output_cutoff;
                case 0xb: // Only later games (taitogn) read this register...
                          // GNet games use some of the flags to decide which channels to kill when
                          // all the channels are busy. (take raycris song #23 as an example)
                    return m_chan[ChipID][ch].vol;
                default:
                    break;
            }

            return m_chan[ChipID][ch].v[reg];
        }

        // Convert ramping register value to something more usable.
        // Upper 4 bits is a shift amount, lower 4 bits is a 2's complement value.
        // Get ramp amount by sign extending the low 4 bits, XOR by 8, then
        // shifting it by the upper 4 bits.
        // CPU uses a lookup table (stored in gdarius sound cpu ROM at 0x6332) to
        // calculate this value, for now I'm generating an opproximate inverse.
        private short get_ramp(byte val)
        {
            short frac = (short)(val << 12); // sign extend
            frac = (short)(((frac >> 12) ^ 8) << (val >> 4));
            return (short)(frac >> 4);
        }

        private ushort ramp(ushort current, ushort target, short delta)
        {
            int rampval = current + delta;
            if (delta < 0 && rampval < target)
                rampval = target;
            else if (delta >= 0 && rampval > target)
                rampval = target;

            return (ushort)rampval;
        }

        /******************************************************************************/

        private void control_w(byte ChipID, int reg, ushort data)
        {
            switch (reg)
            {
                case 0x00:
                case 0x01:
                case 0x02:
                    {
                        // key on
                        int _base = (reg & 3) << 4;
                        for (int i = 0; i < 16; i++)
                        {
                            if ((data & (1 << i)) != 0)
                            {
                                int ch = _base | i;
                                m_chan[ChipID][ch].status |= STATUS_ACTIVE;
                                m_chan[ChipID][ch].cur_pos = m_chan[ChipID][ch].start_pos - 1;
                                m_chan[ChipID][ch].step_ptr = 0x10000;
                                // Ignoring the "initial volume" for now because it causes lots of clicking
                                m_chan[ChipID][ch].vol = 0; // m_chan[ch].vol_initial;
                                m_chan[ChipID][ch].vol_delta = 0x0400; // register 06 ?
                                m_chan[ChipID][ch].output_cutoff = m_chan[ChipID][ch].output_cutoff_initial;
                                m_chan[ChipID][ch].output_filter_state = 0;
                            }
                        }
                        break;
                    }

                case 0x04:
                case 0x05:
                case 0x06:
                    {
                        // key off
                        int _base = (reg & 3) << 4;
                        for (int i = 0; i < 16; i++)
                        {
                            if ((data & (1 << i)) != 0)
                            {
                                int ch = _base | i;
                                m_chan[ChipID][ch].vol = 0;
                                m_chan[ChipID][ch].status &= unchecked((ushort)~STATUS_ACTIVE);
                            }
                        }
                        break;
                    }

                //      case 0x0c: //These registers are sometimes written to by the CPU. Unknown purpose.
                //          break;
                //      case 0x0d:
                //          break;
                //      case 0x10:
                //          break;

                //      case 0x18:
                //          break;

                case 0x1c:
                    // rom readback address low (low 2 bits always 0)
                    //if ((data & 3) != 0) popmessage("ZSG2 address %04X, contact MAMEdev", data);
                    m_read_address[ChipID] = (m_read_address[ChipID] & 0x3fffc000) | (uint)(data >> 2 & 0x00003fff);
                    break;
                case 0x1d:
                    // rom readback address high
                    m_read_address[ChipID] = (m_read_address[ChipID] & 0x00003fff) | (uint)(data << 14 & 0x3fffc000);
                    break;

                default:
                    if (reg < 0x20)
                        m_reg[ChipID][reg] = data;
                    //logerror("ZSG2 control   %02X = %04X\n", reg, data & 0xffff);
                    break;
            }
        }

        private ushort control_r(byte ChipID, int reg)
        {
            switch (reg)
            {
                case 0x14:
                    // memory bus busy?
                    // right before reading memory, it polls until low 8 bits are 0
                    return 0;

                case 0x1e:
                    // rom readback word low
                    return (ushort)(read_memory(ChipID, m_read_address[ChipID]) & 0xffff);
                case 0x1f:
                    // rom readback word high
                    return (ushort)(read_memory(ChipID, m_read_address[ChipID]) >> 16);

                default:
                    if (reg < 0x20)
                        return m_reg[ChipID][reg];
                    break;
            }

            return 0;
        }

        /******************************************************************************/

        private void write(byte ChipID, int offset, ushort data, ushort mem_mask)
        {
            // we only support full 16-bit accesses
            if (mem_mask != 0xffff)
            {
                //popmessage("ZSG2 write mask %04X, contact MAMEdev", mem_mask);
                return;
            }

            //m_stream.update();

            if (offset < 0x300)
            {
                int chan = offset >> 4;
                int reg = offset & 0xf;

                chan_w(ChipID, chan, reg, data);
            }
            else
            {
                control_w(ChipID, offset - 0x300, data);
            }
        }

        private ushort read(byte ChipID, int offset, ushort mem_mask)
        {
            // we only support full 16-bit accesses
            if (mem_mask != 0xffff)
            {
                //popmessage("ZSG2 read mask %04X, contact MAMEdev", mem_mask);
                return 0;
            }

            if (offset < 0x300)
            {
                int chan = offset >> 4;
                int reg = offset & 0xf;

                return chan_r(ChipID, chan, reg);
            }
            else
            {
                return control_r(ChipID, offset - 0x300);
            }
        }



    }
}
