using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text;

namespace MDSound
{
    public class es550x : Instrument
    {
        public override string Name { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override string ShortName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void Reset(byte ChipID)
        {
            throw new NotImplementedException();
        }

        public override uint Start(byte ChipID, uint clock)
        {
            throw new NotImplementedException();
        }

        public override uint Start(byte ChipID, uint clock, uint ClockValue, params object[] option)
        {
            throw new NotImplementedException();
        }

        public override void Stop(byte ChipID)
        {
            throw new NotImplementedException();
        }

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {
            throw new NotImplementedException();
        }

        public override int Write(byte ChipID, int port, int adr, int data)
        {
            throw new NotImplementedException();
        }

        protected uint clock()
        {
            return 0;
        }






        public void set_region0<T>(T tag)
        {
            //m_region0.set_tag(tag);
        }
        public void set_region1<T>(T tag)
        {
            //m_region1.set_tag(tag); 
        }
        public void set_region2<T>(T tag) 
        {
            //m_region2.set_tag(tag); 
        }
        public void set_region3<T>(T tag)
        {
            //m_region3.set_tag(tag);
        }
        public void set_channels(int channels) { m_channels = channels; }
        public uint get_voice_index() { return m_voice_index; }
        public object irq_cb() 
        {
            //return m_irq_cb.bind();
            return null;
        }
        public object read_port_cb()
        {
            //return m_read_port_cb.bind();
            return null;
        }
        public object sample_rate_changed() 
        {
            //return m_sample_rate_changed_cb.bind();
            return null;
        }
        protected enum LP
        {
            LP3 = 1,
            LP4 = 2,
            LP_MASK = LP3 | LP4
        };
        // constants for volumes
        protected const sbyte VOLUME_ACC_BIT = 20;
        // constants for address
        protected const sbyte ADDRESS_FRAC_BIT = 11;
        // struct describing a single playing voice
        protected class es550x_voice
        {
            es550x_voice() { }

            // external state
            public uint control = 0;          // control register
            public ulong freqcount = 0;          // frequency count register
            public ulong start = 0;          // start register
            public uint lvol = 0;          // left volume register
            public ulong end = 0;          // end register
            public uint lvramp = 0;          // left volume ramp register
            public ulong accum = 0;          // accumulator register
            public uint rvol = 0;          // right volume register
            public uint rvramp = 0;          // right volume ramp register
            public uint ecount = 0;          // envelope count register
            public uint k2 = 0;          // k2 register
            public uint k2ramp = 0;          // k2 ramp register
            public uint k1 = 0;          // k1 register
            public uint k1ramp = 0;          // k1 ramp register
            public int o4n1 = 0;          // filter storage O4(n-1)
            public int o3n1 = 0;          // filter storage O3(n-1)
            public int o3n2 = 0;          // filter storage O3(n-2)
            public int o2n1 = 0;          // filter storage O2(n-1)
            public int o2n2 = 0;          // filter storage O2(n-2)
            public int o1n1 = 0;          // filter storage O1(n-1)
            public ulong exbank = 0;          // external address bank
            // internal state
            public byte index = 0;         // index of this voice
            public byte filtcount = 0;         // filter count
        };

        //virtual inline u32 get_bank(u32 control) { return 0; }
        //virtual inline u32 get_ca(u32 control) { return 0; }
        //virtual inline u32 get_lp(u32 control) { return 0; }

        private ulong lshift_signed(ulong val, sbyte shift) { return (shift >= 0) ? val << shift : val >> (-shift); }
        private uint rshift_signed(uint val, sbyte shift) { return (shift >= 0) ? val >> shift : val << (-shift); }
        private uint rshift_signed(ulong val, sbyte shift) { return 0; }// (shift >= 0) ? val >> shift : val << (-shift); }
        private long rshift_signed(long val, sbyte shift) { return (shift >= 0) ? val >> shift : val << (-shift); }

        protected ulong get_volume(uint volume) { return m_volume_lookup[(int)rshift_signed(volume, m_volume_shift)]; }

        protected ulong get_address_acc_shifted_val(ulong val, int bias = 0) {
            return lshift_signed(val, (sbyte)(m_address_acc_shift - bias)); }
        protected ulong get_address_acc_res(ulong val, int bias = 0) { return rshift_signed(val, (sbyte)(m_address_acc_shift - bias)); }
        protected ulong get_integer_addr(ulong accum, int bias = 0) {
            return ((accum + (ulong)(bias << (int)ADDRESS_FRAC_BIT)) & m_address_acc_mask) >> ADDRESS_FRAC_BIT; }

        protected long get_sample(int sample, uint volume) { 
            return rshift_signed((long)((ulong)sample * get_volume(volume)), (sbyte)m_volume_acc_shift); }

        protected virtual void update_envelopes(ref es550x_voice voice) { }
        protected virtual void check_for_end_forward(ref es550x_voice voice, ulong accum) { }
        protected virtual void check_for_end_reverse(ref es550x_voice voice, ulong accum) { }
        protected virtual void generate_samples(int[][] outputs) { }

        //       inline void update_index(es550x_voice* voice) { m_voice_index = voice->index; }
        protected ushort read_sample(ref es550x_voice voice, int addr) { return 0; }

        //        internal state
        //       sound_stream* m_stream;               // which stream are we using
        protected int m_sample_rate;          // current sample rate
        protected uint m_master_clock;         // master clock frequency
        private sbyte m_address_acc_shift;    // right shift accumulator for generate integer address
        protected ulong m_address_acc_mask;     // accumulator mask
        private sbyte m_volume_shift;         // right shift volume for generate integer volume
        private long m_volume_acc_shift;     // right shift output for output normalizing
        protected byte m_current_page;         // current register page
        protected byte m_active_voices;        // number of active voices
        protected ushort m_mode;                 // MODE register
        protected byte m_irqv;                 // IRQV register
        private uint m_voice_index;          // current voice index value

        protected es550x_voice[] m_voice = new es550x_voice[32];            // the 32 voices

        private List<short> m_ulaw_lookup;
        private List<uint> m_volume_lookup;

        //        optional_memory_region m_region0;             // memory region where the sample ROM lives
        //        optional_memory_region m_region1;             // memory region where the sample ROM lives
        //        optional_memory_region m_region2;             // memory region where the sample ROM lives
        //        optional_memory_region m_region3;             // memory region where the sample ROM lives
        protected int m_channels;                               // number of output channels: 1 .. 6
        //        devcb_write_line m_irq_cb;                    // irq callback
        //        devcb_read16 m_read_port_cb;                  // input port read
        //        devcb_write32 m_sample_rate_changed_cb;       // callback for when sample rate is changed



















        // license:BSD-3-Clause
        // copyright-holders:Aaron Giles
        /**********************************************************************************************

             Ensoniq ES5505/6 driver
             by Aaron Giles

        Ensoniq OTIS - ES5505                                            Ensoniq OTTO - ES5506

          OTIS is a VLSI device designed in a 2 micron double metal        OTTO is a VLSI device designed in a 1.5 micron double metal
           CMOS process. The device is the next generation of audio         CMOS process. The device is the next generation of audio
           technology from ENSONIQ. This new chip achieves a new            technology from ENSONIQ. All calculations in the device are
           level of audio fidelity performance. These improvements          made with at least 18-bit accuracy.
           are achieved through the use of frequency interpolation
           and on board real time digital filters. All calculations       The major features of OTTO are:
           in the device are made with at least 16 bit accuracy.           - 68 pin PLCC package
                                                                           - On chip real time digital filters
         The major features of OTIS are:                                   - Frequency interpolation
          - 48 Pin dual in line package                                    - 32 independent voices
          - On chip real time digital filters                              - Loop start and stop posistions for each voice
          - Frequency interpolation                                        - Bidirectional and reverse looping
          - 32 independent voices (up from 25 in DOCII)                    - 68000 compatibility for asynchronous bus communication
          - Loop start and stop positions for each voice                   - separate host and sound memory interface
          - Bidirectional and reverse looping                              - 6 channel stereo serial communication port
          - 68000 compatibility for asynchronous bus communication         - Programmable clocks for defining serial protocol
          - On board pulse width modulation D to A                         - Internal volume multiplication and stereo panning
          - 4 channel stereo serial communication port                     - A to D input for pots and wheels
          - Internal volume multiplication and stereo panning              - Hardware support for envelopes
          - A to D input for pots and wheels                               - Support for dual OTTO systems
          - Up to 10MHz operation                                          - Optional compressed data format for sample data
                                                                           - Up to 16MHz operation
                      ______    ______
                    _|o     \__/      |_
         A17/D13 - |_|1             48|_| - VSS                                                           A A A A A A
                    _|                |_                                                                  2 1 1 1 1 1 A
         A18/D14 - |_|2             47|_| - A16/D12                                                       0 9 8 7 6 5 1
                    _|                |_                                                                  / / / / / / 4
         A19/D15 - |_|3             46|_| - A15/D11                                   H H H H H H H V V H D D D D D D /
                    _|                |_                                              D D D D D D D S D D 1 1 1 1 1 1 D
              BS - |_|4             45|_| - A14/D10                                   0 1 2 3 4 5 6 S D 7 5 4 3 2 1 0 9
                    _|                |_                                             ------------------------------------+
          PWZERO - |_|5             44|_| - A13/D9                                  / 9 8 7 6 5 4 3 2 1 6 6 6 6 6 6 6 6  |
                    _|                |_                                           /                    8 7 6 5 4 3 2 1  |
            SER0 - |_|6             43|_| - A12/D8                                |                                      |
                    _|       E        |_                                      SER0|10                                  60|A13/D8
            SER1 - |_|7      N      42|_| - A11/D7                            SER1|11                                  59|A12/D7
                    _|       S        |_                                      SER2|12                                  58|A11/D6
            SER2 - |_|8      O      41|_| - A10/D6                            SER3|13              ENSONIQ             57|A10/D5
                    _|       N        |_                                      SER4|14                                  56|A9/D4
            SER3 - |_|9      I      40|_| - A9/D5                             SER5|15                                  55|A8/D3
                    _|       Q        |_                                      WCLK|16                                  54|A7/D2
         SERWCLK - |_|10            39|_| - A8/D4                            LRCLK|17               ES5506             53|A6/D1
                    _|                |_                                      BCLK|18                                  52|A5/D0
           SERLR - |_|11            38|_| - A7/D3                             RESB|19                                  51|A4
                    _|                |_                                       HA5|20                                  50|A3
         SERBCLK - |_|12     E      37|_| - A6/D2                              HA4|21                OTTO              49|A2
                    _|       S        |_                                       HA3|22                                  48|A1
             RLO - |_|13     5      36|_| - A5/D1                              HA2|23                                  47|A0
                    _|       5        |_                                       HA1|24                                  46|BS1
             RHI - |_|14     0      35|_| - A4/D0                              HA0|25                                  45|BS0
                    _|       5        |_                                    POT_IN|26                                  44|DTACKB
             LLO - |_|15            34|_| - CLKIN                                 |   2 2 2 3 3 3 3 3 3 3 3 3 3 4 4 4 4  |
                    _|                |_                                          |   7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3  |
             LHI - |_|16            33|_| - CAS                                   +--------------------------------------+
                    _|                |_                                              B E E B E B B D S B B B E K B W W
             POT - |_|17     O      32|_| - AMUX                                      S B L N L S S D S S X S   L Q / /
                    _|       T        |_                                              E E R E H M C V V A U A   C R R R
           DTACK - |_|18     I      31|_| - RAS                                       R R D H           R M C     I M
                    _|       S        |_                                              _ D                 A
             R/W - |_|19            30|_| - E                                         T
                    _|                |_                                              O
              MS - |_|20            29|_| - IRQ                                       P
                    _|                |_
              CS - |_|21            28|_| - A3
                    _|                |_
             RES - |_|22            27|_| - A2
                    _|                |_
             VSS - |_|23            26|_| - A1
                    _|                |_
             VDD - |_|24            25|_| - A0
                     |________________|

        ***********************************************************************************************/

        //# include "emu.h"
        //# include "es5506.h"
        //# include <algorithm>

        //#if ES5506_MAKE_WAVS
        //# include "sound/wavwrite.h"
        //#endif

        /**********************************************************************************************

             CONSTANTS

        ***********************************************************************************************/

        private uint LOG_SERIAL = (1U << 1);
        private int VERBOSE = 0;
        //#include "logmacro.h"
        private int RAINE_CHECK = 0;

        private static uint FINE_FILTER_BIT = 16;
        private static uint FILTER_BIT = 12;
        private static uint FILTER_SHIFT = FINE_FILTER_BIT - FILTER_BIT;
        private static uint ULAW_MAXBITS = 8;


        protected enum CONTROL : ushort
        {
            CONTROL_BS1 = 0x8000,
            CONTROL_BS0 = 0x4000,
            CONTROL_CMPD = 0x2000,
            CONTROL_CA2 = 0x1000,
            CONTROL_CA1 = 0x0800,
            CONTROL_CA0 = 0x0400,
            CONTROL_LP4 = 0x0200,
            CONTROL_LP3 = 0x0100,
            CONTROL_IRQ = 0x0080,
            CONTROL_DIR = 0x0040,
            CONTROL_IRQE = 0x0020,
            CONTROL_BLE = 0x0010,
            CONTROL_LPE = 0x0008,
            CONTROL_LEI = 0x0004,
            CONTROL_STOP1 = 0x0002,
            CONTROL_STOP0 = 0x0001,
            CONTROL_BSMASK = (CONTROL_BS1 | CONTROL_BS0),
            CONTROL_CAMASK = (CONTROL_CA2 | CONTROL_CA1 | CONTROL_CA0),
            CONTROL_LPMASK = (CONTROL_LP4 | CONTROL_LP3),
            CONTROL_LOOPMASK = (CONTROL_BLE | CONTROL_LPE),
            CONTROL_STOPMASK = (CONTROL_STOP1 | CONTROL_STOP0),
            // ES5505 has sightly different control bit
            CONTROL_5505_LP4 = 0x0800,
            CONTROL_5505_LP3 = 0x0400,
            CONTROL_5505_CA1 = 0x0200,
            CONTROL_5505_CA0 = 0x0100,
            CONTROL_5505_LPMASK = (CONTROL_5505_LP4 | CONTROL_5505_LP3),
            CONTROL_5505_CAMASK = (CONTROL_5505_CA1 | CONTROL_5505_CA0)
        };


        public void es550x_device()//const machine_config &mconfig, device_type type, const char* tag, device_t * owner, u32 clock)
                                   //: device_t(mconfig, type, tag, owner, clock)
                                   //, device_sound_interface(mconfig, *this)
                                   //, device_memory_interface(mconfig, *this)
                                   //, m_stream(nullptr)
                                   //, m_sample_rate(0)
                                   //, m_master_clock(0)
                                   //, m_address_acc_shift(0)
                                   //, m_address_acc_mask(0)
                                   //, m_volume_shift(0)
                                   //, m_volume_acc_shift(0)
                                   //, m_current_page(0)
                                   //, m_active_voices(0x1f)
                                   //, m_mode(0)
                                   //, m_irqv(0x80)
                                   //, m_voice_index(0)
                                   //#if ES5506_MAKE_WAVS
                                   //	, m_wavraw(nullptr)
                                   //#endif
                                   //	, m_region0(*this, finder_base::DUMMY_TAG)
                                   //	, m_region1(*this, finder_base::DUMMY_TAG)
                                   //	, m_region2(*this, finder_base::DUMMY_TAG)
                                   //	, m_region3(*this, finder_base::DUMMY_TAG)
                                   //	, m_channels(0)
                                   //	, m_irq_cb(*this)
                                   //	, m_read_port_cb(*this, 0)
                                   //	, m_sample_rate_changed_cb(*this)
        {
        }

        //-------------------------------------------------
        //  device_start - device-specific startup
        //-------------------------------------------------
        protected virtual void device_start()
        {
            // initialize the rest of the structure
            m_master_clock = clock();
            m_irqv = 0x80;

            //// register save
            //save_item(NAME(m_sample_rate));

            //save_item(NAME(m_current_page));
            //save_item(NAME(m_active_voices));
            //save_item(NAME(m_mode));
            //save_item(NAME(m_irqv));
            //save_item(NAME(m_voice_index));

            //save_item(STRUCT_MEMBER(m_voice, control));
            //save_item(STRUCT_MEMBER(m_voice, freqcount));
            //save_item(STRUCT_MEMBER(m_voice, start));
            //save_item(STRUCT_MEMBER(m_voice, end));
            //save_item(STRUCT_MEMBER(m_voice, accum));
            //save_item(STRUCT_MEMBER(m_voice, lvol));
            //save_item(STRUCT_MEMBER(m_voice, rvol));
            //save_item(STRUCT_MEMBER(m_voice, k2));
            //save_item(STRUCT_MEMBER(m_voice, k1));
            //save_item(STRUCT_MEMBER(m_voice, o4n1));
            //save_item(STRUCT_MEMBER(m_voice, o3n1));
            //save_item(STRUCT_MEMBER(m_voice, o3n2));
            //save_item(STRUCT_MEMBER(m_voice, o2n1));
            //save_item(STRUCT_MEMBER(m_voice, o2n2));
            //save_item(STRUCT_MEMBER(m_voice, o1n1));
        }

        //-------------------------------------------------
        //  device_clock_changed
        //-------------------------------------------------

        private void device_clock_changed()
        {
            m_master_clock = clock();
            m_sample_rate = (int)(m_master_clock / (16 * (m_active_voices + 1)));
            //m_stream->set_sample_rate(m_sample_rate);
            //m_sample_rate_changed_cb(m_sample_rate);
        }

        //-------------------------------------------------
        //  device_reset - device-specific reset
        //-------------------------------------------------

        private void device_reset()
        {
        }

        //-------------------------------------------------
        //  device_stop - device-specific stop
        //-------------------------------------------------
        private void device_stop()
        {
            //#if ES5506_MAKE_WAVS
            //	{
            //		wav_close(m_wavraw);
            //	}
            //#endif
        }

        /**********************************************************************************************

             update_irq_state -- update the IRQ state

        ***********************************************************************************************/


        private void update_irq_state()
        {
            // ES5505/6 irq line has been set high - inform the host
            //m_irq_cb(1); // IRQB set high
        }

        protected void update_internal_irq_state()
        {
            /*  Host (cpu) has just read the voice interrupt vector (voice IRQ ack).

                Reset the voice vector to show the IRQB line is low (top bit set).
                If we have any stacked interrupts (other voices waiting to be
                processed - with their IRQ bit set) then they will be moved into
                the vector next time the voice is processed.  In emulation
                terms they get updated next time generate_samples() is called.
            */
            m_irqv = 0x80;
            //m_irq_cb(0); // IRQB set low
        }

        /**********************************************************************************************

             compute_tables -- compute static tables

        ***********************************************************************************************/

        protected void compute_tables(uint total_volume_bit, uint exponent_bit, uint mantissa_bit)
        {
            // allocate ulaw lookup table
            m_ulaw_lookup = new List<short>();
            for (int i = 0; i < (1 << (int)ULAW_MAXBITS); i++)
                m_ulaw_lookup.Add(0);

            // generate ulaw lookup table
            for (int i = 0; i < (1 << (int)ULAW_MAXBITS); i++)
            {
                ushort rawval = (ushort)((i << (int)(16 - ULAW_MAXBITS)) | (1 << (int)(15 - ULAW_MAXBITS)));
                byte exponent = (byte)(rawval >> 13);
                uint mantissa = (uint)((rawval << 3) & 0xffff);

                if (exponent == 0)
                    m_ulaw_lookup[i] = (short)(mantissa >> 7);
                else
                {
                    mantissa = (mantissa >> 1) | (~mantissa & 0x8000);
                    m_ulaw_lookup[i] = (short)(mantissa >> (7 - exponent));
                }
            }

            uint volume_bit = (exponent_bit + mantissa_bit);
            m_volume_shift = (sbyte)(total_volume_bit - volume_bit);
            uint volume_len = (uint)(1 << (int)volume_bit);
            // allocate volume lookup table
            m_volume_lookup = new List<uint>();
            for (int i = 0; i < volume_len; i++)
                m_volume_lookup.Add(0);

            // generate volume lookup table
            uint exponent_shift = (uint)(1 << (int)exponent_bit);
            uint exponent_mask = exponent_shift - 1;

            uint mantissa_len = (uint)(1 << (int)mantissa_bit);
            uint mantissa_mask = (mantissa_len - 1);
            uint mantissa_shift = exponent_shift - mantissa_bit - 1;

            for (int i = 0; i < volume_len; i++)
            {
                uint exponent = (uint)((i >> (int)mantissa_bit) & exponent_mask);
                uint mantissa = (uint)((i & mantissa_mask) | mantissa_len);

                m_volume_lookup[i] = (uint)((mantissa << (int)mantissa_shift) >> (int)(exponent_shift - exponent));
            }
            m_volume_acc_shift = (16 + exponent_mask) - VOLUME_ACC_BIT;

            // init the voices
            for (int j = 0; j < 32; j++)
            {
                m_voice[j].index = (byte)j;
                m_voice[j].control = (uint)CONTROL.CONTROL_STOPMASK;
                m_voice[j].lvol = (uint)(1 << (int)(total_volume_bit - 1));
                m_voice[j].rvol = (uint)(1 << (int)(total_volume_bit - 1));
            }
        }

        /**********************************************************************************************

             get_accum_mask -- get address accumulator mask

        ***********************************************************************************************/

        protected void get_accum_mask(uint address_integer, uint address_frac)
        {
            m_address_acc_shift = (sbyte)(ADDRESS_FRAC_BIT - address_frac);
            m_address_acc_mask = lshift_signed(
                (ulong)((((1 << (int)address_integer) - 1) << (int)address_frac) | ((1 << (int)address_frac) - 1)),
                m_address_acc_shift);
            if (m_address_acc_shift > 0)
                m_address_acc_mask = m_address_acc_mask | (ulong)((1 << m_address_acc_shift) - 1);
        }


        /**********************************************************************************************

             interpolate -- interpolate between two samples

        ***********************************************************************************************/

        private int interpolate(int sample1, int sample2, ulong accum)
        {
            uint shifted = 1 << ADDRESS_FRAC_BIT;
            uint mask = shifted - 1;
            accum &= mask & m_address_acc_mask;
            return (sample1 * (int)(shifted - accum) +
                    sample2 * (int)(accum)) >> ADDRESS_FRAC_BIT;
        }


        /**********************************************************************************************

             apply_filters -- apply the 4-pole digital filter to the sample

        ***********************************************************************************************/

        // apply lowpass/highpass result
        private static int apply_lowpass(int _out, int cutoff, int _in)
        {
            return ((int)(cutoff >> (int)FILTER_SHIFT) * (_out - _in) / (1 << (int)FILTER_BIT)) + _in;
        }

        private static int apply_highpass(int _out, int cutoff, int _in, int prev)
        {
            return _out - prev + ((int)(cutoff >> (int)FILTER_SHIFT) * _in) / (1 << (int)(FILTER_BIT + 1)) + _in / 2;
        }

        // update poles from outputs
        private static void update_pole(ref int pole, int sample)
        {
            pole = sample;
        }

        private static void update_2_pole(ref int prev, ref int pole, int sample)
        {
            prev = pole;
            pole = sample;
        }

        private void apply_filters(ref es550x_voice voice, ref int sample)
        {
            // pole 1 is always low-pass using K1
            sample = apply_lowpass(sample, (int)voice.k1, voice.o1n1);
            update_pole(ref voice.o1n1, sample);

            // pole 2 is always low-pass using K1
            sample = apply_lowpass(sample, (int)voice.k1, voice.o2n1);
            update_2_pole(ref voice.o2n2, ref voice.o2n1, sample);

            // remaining poles depend on the current filter setting
            switch (voice.control)//(get_lp(voice.control))
            {
                case 0:
                    // pole 3 is high-pass using K2
                    sample = apply_highpass(sample, (int)voice.k2, voice.o3n1, voice.o2n2);
                    update_2_pole(ref voice.o3n2, ref voice.o3n1, sample);
                    // pole 4 is high-pass using K2
                    sample = apply_highpass(sample, (int)voice.k2, voice.o4n1, voice.o3n2);
                    update_pole(ref voice.o4n1, sample);
                    break;

                case (int)LP.LP3:
                    // pole 3 is low-pass using K1
                    sample = apply_lowpass(sample, (int)voice.k1, voice.o3n1);
                    update_2_pole(ref voice.o3n2, ref voice.o3n1, sample);
                    // pole 4 is high-pass using K2
                    sample = apply_highpass(sample, (int)voice.k2, voice.o4n1, voice.o3n2);
                    update_pole(ref voice.o4n1, sample);
                    break;

                case (int)LP.LP4:
                    // pole 3 is low-pass using K2
                    sample = apply_lowpass(sample, (int)voice.k2, voice.o3n1);
                    update_2_pole(ref voice.o3n2, ref voice.o3n1, sample);
                    // pole 4 is low-pass using K2
                    sample = apply_lowpass(sample, (int)voice.k2, voice.o4n1);
                    update_pole(ref voice.o4n1, sample);
                    break;

                case (int)LP.LP3 | (int)LP.LP4:
                    // pole 3 is low-pass using K1
                    sample = apply_lowpass(sample, (int)voice.k1, voice.o3n1);
                    update_2_pole(ref voice.o3n2, ref voice.o3n1, sample);
                    // pole 4 is low-pass using K2
                    sample = apply_lowpass(sample, (int)voice.k2, voice.o4n1);
                    update_pole(ref voice.o4n1, sample);
                    break;
            }
        }

        /**********************************************************************************************

             generate_ulaw -- general u-law decoding routine

        ***********************************************************************************************/

        private void generate_ulaw(ref es550x_voice voice, int[] dest)
        {
            uint freqcount = (uint)voice.freqcount;
            ulong accum = voice.accum & m_address_acc_mask;

            // outer loop, in case we switch directions
            if ((voice.control & (uint)CONTROL.CONTROL_STOPMASK) == 0)
            {
                // two cases: first case is forward direction
                if ((voice.control & (uint)CONTROL.CONTROL_DIR) == 0)
                {
                    // fetch two samples
                    int val1 = read_sample(ref voice, (int)get_integer_addr(accum));
                    int val2 = read_sample(ref voice, (int)get_integer_addr(accum, 1));

                    // decompress u-law
                    val1 = m_ulaw_lookup[val1 >> (int)(16 - ULAW_MAXBITS)];
                    val2 = m_ulaw_lookup[val2 >> (int)(16 - ULAW_MAXBITS)];

                    // interpolate
                    val1 = interpolate(val1, val2, accum);
                    accum = (accum + freqcount) & m_address_acc_mask;

                    // apply filters
                    apply_filters(ref voice, ref val1);

                    // update filters/volumes
                    if (voice.ecount != 0)
                        update_envelopes(ref voice);

                    // apply volumes and add
                    dest[0] += (int)get_sample(val1, voice.lvol);
                    dest[1] += (int)get_sample(val1, voice.rvol);

                    // check for loop end
                    check_for_end_forward(ref voice, accum);
                }

                // two cases: second case is backward direction
                else
                {
                    // fetch two samples
                    int val1 = read_sample(ref voice, (int)get_integer_addr(accum));
                    int val2 = read_sample(ref voice, (int)get_integer_addr(accum, 1));

                    // decompress u-law
                    val1 = m_ulaw_lookup[val1 >> (int)(16 - ULAW_MAXBITS)];
                    val2 = m_ulaw_lookup[val2 >> (int)(16 - ULAW_MAXBITS)];

                    // interpolate
                    val1 = interpolate(val1, val2, accum);
                    accum = (accum - freqcount) & m_address_acc_mask;

                    // apply filters
                    apply_filters(ref voice, ref val1);

                    // update filters/volumes
                    if (voice.ecount != 0)
                        update_envelopes(ref voice);

                    // apply volumes and add
                    dest[0] += (int)get_sample(val1, voice.lvol);
                    dest[1] += (int)get_sample(val1, voice.rvol);

                    // check for loop end
                    check_for_end_reverse(ref voice, accum);
                }
            }
            else
            {
                // if we stopped, process any additional envelope
                if (voice.ecount != 0)
                    update_envelopes(ref voice);
            }

            voice.accum = accum;
        }



        /**********************************************************************************************

             generate_pcm -- general PCM decoding routine

        ***********************************************************************************************/

        protected void generate_pcm(ref es550x_voice voice, int[] dest)
        {
            uint freqcount = (uint)voice.freqcount;
            ulong accum = voice.accum & m_address_acc_mask;

            // outer loop, in case we switch directions
            if ((voice.control & (uint)CONTROL.CONTROL_STOPMASK) == 0)
            {
                // two cases: first case is forward direction
                if ((voice.control & (uint)CONTROL.CONTROL_DIR) == 0)
                {
                    // fetch two samples
                    int val1 = (short)read_sample(ref voice, (int)get_integer_addr(accum));
                    int val2 = (short)read_sample(ref voice, (int)get_integer_addr(accum, 1));

                    // interpolate
                    val1 = interpolate(val1, val2, accum);
                    accum = (accum + freqcount) & m_address_acc_mask;

                    // apply filters
                    apply_filters(ref voice, ref val1);

                    // update filters/volumes
                    if (voice.ecount != 0)
                        update_envelopes(ref voice);

                    // apply volumes and add
                    dest[0] += (int)get_sample(val1, voice.lvol);
                    dest[1] += (int)get_sample(val1, voice.rvol);

                    // check for loop end
                    check_for_end_forward(ref voice, accum);
                }

                // two cases: second case is backward direction
                else
                {
                    // fetch two samples
                    int val1 = (short)read_sample(ref voice, (int)get_integer_addr(accum));
                    int val2 = (short)read_sample(ref voice, (int)get_integer_addr(accum, 1));

                    // interpolate
                    val1 = interpolate(val1, val2, accum);
                    accum = (accum - freqcount) & m_address_acc_mask;

                    // apply filters
                    apply_filters(ref voice, ref val1);

                    // update filters/volumes
                    if (voice.ecount != 0)
                        update_envelopes(ref voice);

                    // apply volumes and add
                    dest[0] += (int)get_sample(val1, voice.lvol);
                    dest[1] += (int)get_sample(val1, voice.rvol);

                    // check for loop end
                    check_for_end_reverse(ref voice, accum);
                }
            }
            else
            {
                // if we stopped, process any additional envelope
                if (voice.ecount != 0)
                    update_envelopes(ref voice);
            }

            voice.accum = accum;
        }

        /**********************************************************************************************

             generate_irq -- general interrupt handling routine

        ***********************************************************************************************/

        protected void generate_irq(ref es550x_voice voice, int v)
        {
            // does this voice have it's IRQ bit raised?
            if ((voice.control & (uint)CONTROL.CONTROL_IRQ) != 0)
            {
                //LOG("es5506: IRQ raised on voice %d!!\n", v);

                // only update voice vector if existing IRQ is acked by host
                if ((m_irqv & 0x80) != 0)
                {
                    // latch voice number into vector, and set high bit low
                    m_irqv = (byte)(v & 0x1f);

                    // take down IRQ bit on voice
                    voice.control &= ~(uint)CONTROL.CONTROL_IRQ;

                    // inform host of irq
                    update_irq_state();
                }
            }
        }


        //-------------------------------------------------
        //  sound_stream_update - handle a stream update
        //-------------------------------------------------

        private void sound_stream_update(int[][] outputs)
        {
            // loop until all samples are output
            generate_samples(outputs);
        }

    }
}

