using System;
using System.Collections.Generic;
using System.Text;

namespace MDSound
{
    public class es5505 : es550x
    {
        public override string Name { get => "Ensoniq ES5505"; set { } }
        public override string ShortName { get => "ES5505"; set { } }



        private sbyte VOLUME_BIT_ES5505 = 8;
        private sbyte ADDRESS_INTEGER_BIT_ES5505 = 20;
        private sbyte ADDRESS_FRAC_BIT_ES5505 = 9;

        private uint get_ca(uint control) { return (control >> 10) & 7; }



    public void es5505_device()//(const machine_config &mconfig, const char* tag, device_t *owner, u32 clock)
                                   //	: es550x_device(mconfig, ES5505, tag, owner, clock)
                                   //	, m_bank0_config("bank0", ENDIANNESS_BIG, 16, 20, -1) // 20 bit address bus, word addressing only
                                   //	, m_bank1_config("bank1", ENDIANNESS_BIG, 16, 20, -1)
        {
        }


        //-------------------------------------------------
        //  device_start - device-specific startup
        //-------------------------------------------------

        protected override void device_start()
        {
            base.device_start();

            int channels = 1;  // 1 channel by default, for backward compatibility

            // only override the number of channels if the value is in the valid range 1 .. 4
            if (1 <= m_channels && m_channels <= 4)
                channels = m_channels;

            //// create the stream
            //m_stream = stream_alloc(0, 2 * channels, clock() / (16 * 32));

            //// initialize the regions
            //if (m_region0 && !has_configured_map(0))
            //    space(0).install_rom(0
            //        , std::min<offs_t>((1 << ADDRESS_INTEGER_BIT_ES5505) - 1
            //        , (m_region0.bytes() / 2) - 1)
            //        , m_region0.base());

            //if (m_region1 && !has_configured_map(1))
            //    space(1).install_rom(0
            //        , std::min<offs_t>((1 << ADDRESS_INTEGER_BIT_ES5505) - 1
            //        , (m_region1.bytes() / 2) - 1)
            //        , m_region1.base());

            //for (int s = 0; s < 2; s++)
            //    space(s).cache(m_cache[s]);

            // compute the tables
            compute_tables((uint)VOLUME_BIT_ES5505, 4, 4); // 4 bit exponent, 4 bit mantissa

            // initialize the rest of the structure
            m_channels = channels;

            // 20 bit integer and 9 bit fraction
            get_accum_mask((uint)ADDRESS_INTEGER_BIT_ES5505, (uint)ADDRESS_FRAC_BIT_ES5505);

            // success
        }

        //-------------------------------------------------
        //  memory_space_config - return a description of
        //  any address spaces owned by this device
        //-------------------------------------------------

        //private space_config_vector memory_space_config()
        //{
        //    return space_config_vector{ make_pair(0, m_bank0_config),make_pair(1, m_bank1_config) };
        //}


        protected override void update_envelopes(ref es550x_voice voice)
        {
            // no envelopes in ES5505
            voice.ecount = 0;
        }




        // ES5505 : BLE is ignored when LPE = 0
        protected override void check_for_end_forward(ref es550x_voice voice, ulong accum)
        {
            // are we past the end?
            if (accum > voice.end)
            {
                // generate interrupt if required
                if ((voice.control & (uint)CONTROL.CONTROL_IRQE) != 0)
                    voice.control |= (uint)CONTROL.CONTROL_IRQ;

                // handle the different types of looping
                switch (voice.control & (uint)CONTROL.CONTROL_LOOPMASK)
                {
                    // non-looping
                    case 0:
                    case (uint)CONTROL.CONTROL_BLE:
                        voice.control |= (uint)CONTROL.CONTROL_STOP0;
                        break;

                    // uni-directional looping
                    case (uint)CONTROL.CONTROL_LPE:
                        accum = (voice.start + (accum - voice.end)) & m_address_acc_mask;
                        break;

                    // bi-directional looping
                    case (uint)CONTROL.CONTROL_LPE | (uint)CONTROL.CONTROL_BLE:
                        accum = (voice.end - (accum - voice.end)) & m_address_acc_mask;
                        voice.control ^= (uint)CONTROL.CONTROL_DIR;
                        break;
                }
            }
        }

        protected override void check_for_end_reverse(ref es550x_voice voice, ulong accum)
        {
            // are we past the end?
            if (accum < voice.start)
            {
                // generate interrupt if required
                if ((voice.control & (uint)CONTROL.CONTROL_IRQE) != 0)
                    voice.control |= (uint)CONTROL.CONTROL_IRQ;

                // handle the different types of looping
                switch (voice.control & (uint)CONTROL.CONTROL_LOOPMASK)
                {
                    // non-looping
                    case 0:
                    case (uint)CONTROL.CONTROL_BLE:
                        voice.control |= (uint)CONTROL.CONTROL_STOP0;
                        break;

                    // uni-directional looping
                    case (uint)CONTROL.CONTROL_LPE:
                        accum = (voice.end - (voice.start - accum)) & m_address_acc_mask;
                        break;

                    // bi-directional looping
                    case (uint)CONTROL.CONTROL_LPE | (uint)CONTROL.CONTROL_BLE:
                        accum = (voice.start + (voice.start - accum)) & m_address_acc_mask;
                        voice.control ^= (uint)CONTROL.CONTROL_DIR;
                        break;
                }
            }
        }


        protected override void generate_samples(int[][] outputs)
        {
            // loop while we still have samples to generate
            for (int sampindex = 0; sampindex < outputs[0].Length; sampindex++)
            {
                // loop over voices
                int[] cursample = new int[12];
                for (int v = 0; v <= m_active_voices; v++)
                {
                    es550x_voice voice = m_voice[v];

                    // This special case does not appear to match the behaviour observed in the es5505 in
                    // actual Ensoniq synthesizers: those, it turns out, do set loop start and end to the
                    // same value, and expect the voice to keep running. Examples can be found among the
                    // transwaves on the VFX / SD-1 series of synthesizers.
//#if 0
			// special case: if end == start, stop the voice
			//if (voice->start == voice->end)
				//voice->control |= CONTROL_STOP0;
//#endif

                    int voice_channel = (int)get_ca(voice.control);
                    int channel = voice_channel % m_channels;
                    int l = channel << 1;

                    // generate from the appropriate source
                    // no compressed sample support
                    //generate_pcm(ref voice, cursample[l]);

                    // does this voice have it's IRQ bit raised?
                    generate_irq(ref voice, v);
                }

                //for (int c = 0; c < outputs.Length; c++)
                //    outputs[c].put_int(sampindex, cursample[c], 32768);
            }
        }



        /**********************************************************************************************

             reg_write -- handle a write to the selected ES5505 register

        ***********************************************************************************************/

        private void reg_write_low(ref es550x_voice voice, int offset, ushort data, ushort mem_mask)
        {
            switch (offset)
            {
                case 0x00:  // CR
                    voice.control |= 0xf000; // bit 15-12 always 1
                    if ((mem_mask & 0x000000ffU) != 0)
                    {
#if RAINE_CHECK
				voice->control &= ~(CONTROL_STOPMASK | CONTROL_LOOPMASK | CONTROL_DIR);
#else
                        voice.control &= unchecked((uint)~0x00ff);
#endif
                        voice.control |= (uint)(data & 0x00ff);
                    }
                    if ((mem_mask & 0x0000ff00U) != 0)
                        voice.control = (uint)((voice.control & ~0x0f00) | (data & 0x0f00));

                    //LOG("%s:voice %d, control=%04x (raw=%04x & %04x)\n"
                    //    , machine().describe_context()
                    //    , m_current_page & 0x1f
                    //    , voice.control
                    //    , data
                    //    , mem_mask ^ 0xffff);
                    break;


                case 0x01:  // FC
                    if ((mem_mask & 0x000000ffU) != 0)
                        voice.freqcount = (voice.freqcount
                            & ~get_address_acc_shifted_val(0x00fe, 1))
                            | (get_address_acc_shifted_val((ulong)(data & 0x00fe), 1));
                    if ((mem_mask & 0x0000ff00U) != 0)
                        voice.freqcount = (voice.freqcount
                            & ~get_address_acc_shifted_val(0xff00, 1))
                            | (get_address_acc_shifted_val((ulong)(data & 0xff00), 1));
                    //LOG("%s:voice %d, freq count=%08x\n", machine().describe_context(), m_current_page & 0x1f, get_address_acc_res(voice.freqcount, 1));
                    break;

                case 0x02:  // STRT (hi)
                    if ((mem_mask & 0x000000ffU) != 0)
                        voice.start = (voice.start
                            & ~get_address_acc_shifted_val(0x00ff0000))
                            | (get_address_acc_shifted_val((ulong)((data & 0x00ff) << 16)));
                    if ((mem_mask & 0x0000ff00U) != 0)
                        voice.start = (voice.start
                            & ~get_address_acc_shifted_val(0x1f000000))
                            | (get_address_acc_shifted_val((ulong)((data & 0x1f00) << 16)));
                    //LOG("%s:voice %d, loop start=%08x\n", machine().describe_context(), m_current_page & 0x1f, get_address_acc_res(voice.start));
                    break;

                case 0x03:  // STRT (lo)
                    if ((mem_mask & 0x000000ffU) != 0)
                        voice.start = (voice.start
                            & ~get_address_acc_shifted_val(0x000000e0))
                            | (get_address_acc_shifted_val((ulong)(data & 0x00e0)));
                    if ((mem_mask & 0x0000ff00U) != 0)
                        voice.start = (voice.start
                            & ~get_address_acc_shifted_val(0x0000ff00))
                            | (get_address_acc_shifted_val((ulong)(data & 0xff00)));
                    //LOG("%s:voice %d, loop start=%08x\n", machine().describe_context(), m_current_page & 0x1f, get_address_acc_res(voice.start));
                    break;

                case 0x04:  // END (hi)
                    if ((mem_mask & 0x000000ffU) != 0)
                        voice.end = (voice.end
                            & ~get_address_acc_shifted_val(0x00ff0000))
                            | (get_address_acc_shifted_val((ulong)((data & 0x00ff) << 16)));
                    if ((mem_mask & 0x0000ff00U) != 0)
                        voice.end = (voice.end
                            & ~get_address_acc_shifted_val(0x1f000000))
                            | (get_address_acc_shifted_val((ulong)((data & 0x1f00) << 16)));
#if RAINE_CHECK
			voice->control |= CONTROL_STOP0;
#endif
                    //LOG("%s:voice %d, loop end=%08x\n", machine().describe_context(), m_current_page & 0x1f, get_address_acc_res(voice->end));
                    break;

                case 0x05:  // END (lo)
                    if ((mem_mask & 0x000000ffU) != 0)
                        voice.end = (voice.end
                            & ~get_address_acc_shifted_val(0x000000e0))
                            | (get_address_acc_shifted_val((ulong)(data & 0x00e0)));
                    if ((mem_mask & 0x0000ff00U) != 0)
                        voice.end = (voice.end
                            & ~get_address_acc_shifted_val(0x0000ff00))
                            | (get_address_acc_shifted_val((ulong)(data & 0xff00)));
#if RAINE_CHECK
			voice->control |= CONTROL_STOP0;
#endif
                    //LOG("%s:voice %d, loop end=%08x\n", machine().describe_context(), m_current_page & 0x1f, get_address_acc_res(voice->end));
                    break;

                case 0x06:  // K2
                    if ((mem_mask & 0x000000ffU) != 0)
                        voice.k2 = (uint)(voice.k2 & ~0x00f0) | (uint)(data & 0x00f0);
                    if ((mem_mask & 0x0000ff00U) != 0)
                        voice.k2 = (uint)(voice.k2 & ~0xff00) | (uint)(data & 0xff00);
                    //LOG("%s:voice %d, K2=%03x\n", machine().describe_context(), m_current_page & 0x1f, voice->k2 >> FILTER_SHIFT);
                    break;

                case 0x07:  // K1
                    if ((mem_mask & 0x000000ffU) != 0)
                        voice.k1 = (uint)(voice.k1 & ~0x00f0) | (uint)(data & 0x00f0);
                    if ((mem_mask & 0x0000ff00U) != 0)
                        voice.k1 = (uint)(voice.k1 & ~0xff00) | (uint)(data & 0xff00);
                    //LOG("%s:voice %d, K1=%03x\n", machine().describe_context(), m_current_page & 0x1f, voice->k1 >> FILTER_SHIFT);
                    break;

                case 0x08:  // LVOL
                    if ((mem_mask & 0x0000ff00U) != 0)
                        voice.lvol = (uint)(voice.lvol & ~0xff) | (uint)((data & 0xff00) >> 8);
                    //LOG("%s:voice %d, left vol=%02x\n", machine().describe_context(), m_current_page & 0x1f, voice->lvol);
                    break;

                case 0x09:  // RVOL
                    if ((mem_mask & 0x0000ff00U) != 0)
                        voice.rvol = (uint)(voice.rvol & ~0xff) | (uint)((data & 0xff00) >> 8);
                    //LOG("%s:voice %d, right vol=%02x\n", machine().describe_context(), m_current_page & 0x1f, voice->rvol);
                    break;

                case 0x0a:  // ACC (hi)
                    if ((mem_mask & 0x000000ffU) != 0)
                        voice.accum = (voice.accum
                            & ~get_address_acc_shifted_val(0x00ff0000))
                            | (get_address_acc_shifted_val((ulong)((data & 0x00ff) << 16)));
                    if ((mem_mask & 0x0000ff00U) != 0)
                        voice.accum = (voice.accum
                            & ~get_address_acc_shifted_val(0x1f000000))
                            | (get_address_acc_shifted_val((ulong)((data & 0x1f00) << 16)));
                    //LOG("%s:voice %d, accum=%08x\n", machine().describe_context(), m_current_page & 0x1f, get_address_acc_res(voice->accum));
                    break;

                case 0x0b:  // ACC (lo)
                    if ((mem_mask & 0x000000ffU) != 0)
                        voice.accum = (voice.accum
                            & ~get_address_acc_shifted_val(0x000000ff))
                            | (get_address_acc_shifted_val((ulong)(data & 0x00ff)));
                    if ((mem_mask & 0x0000ff00U) != 0)
                        voice.accum = (voice.accum
                            & ~get_address_acc_shifted_val(0x0000ff00))
                            | (get_address_acc_shifted_val((ulong)(data & 0xff00)));
                    //LOG("%s:voice %d, accum=%08x\n", machine().describe_context(), m_current_page & 0x1f, get_address_acc_res(voice->accum));
                    break;

                case 0x0c:  // unused
                    break;

                case 0x0d:  // ACT
                    if ((mem_mask & 0x000000ffU) != 0)
                    {
                        m_active_voices = (byte)(data & 0x1f);
                        m_sample_rate = (int)(m_master_clock / (16 * (m_active_voices + 1)));
                        //m_stream.set_sample_rate(m_sample_rate);
                        //m_sample_rate_changed_cb(m_sample_rate);

                        //LOG("active voices=%d, sample_rate=%d\n", m_active_voices, m_sample_rate);
                    }
                    break;

                case 0x0e:  // IRQV - read only
                    break;

                case 0x0f:  // PAGE
                    if ((mem_mask & 0x000000ffU) != 0)
                        m_current_page = (byte)(data & 0x7f);
                    break;
            }
        }


        private void reg_write_high(ref es550x_voice voice, int offset, ushort data, ushort mem_mask)
        {
            switch (offset)
            {
                case 0x00:  // CR
                    voice.control |= 0xf000; // bit 15-12 always 1
                    if (((mem_mask & 0x000000ffU) != 0))
                        voice.control = (uint)(voice.control & ~0x00ff) | (uint)(data & 0x00ff);
                    if (((mem_mask & 0x0000ff00U) != 0))
                        voice.control = (uint)(voice.control & ~0x0f00) | (uint)(data & 0x0f00);

                    //LOG("%s:voice %d, control=%04x (raw=%04x & %04x)\n", machine().describe_context(), m_current_page & 0x1f, voice->control, data, mem_mask);
                    break;


                case 0x01:  // O4(n-1)
                    if (((mem_mask & 0x000000ffU) != 0))
                        voice.o4n1 = (voice.o4n1 & ~0x00ff) | (data & 0x00ff);
                    if (((mem_mask & 0x0000ff00U) != 0))
                        voice.o4n1 = (short)((voice.o4n1 & ~0xff00) | (data & 0xff00));
                    //LOG("%s:voice %d, O4(n-1)=%04x\n", machine().describe_context(), m_current_page & 0x1f, voice.o4n1 & 0xffff);
                    break;

                case 0x02:  // O3(n-1)
                    if (((mem_mask & 0x000000ffU) != 0))
                        voice.o3n1 = (voice.o3n1 & ~0x00ff) | (data & 0x00ff);
                    if (((mem_mask & 0x0000ff00U) != 0))
                        voice.o3n1 = (short)((voice.o3n1 & ~0xff00) | (data & 0xff00));
                    //LOG("%s:voice %d, O3(n-1)=%04x\n", machine().describe_context(), m_current_page & 0x1f, voice.o3n1 & 0xffff);
                    break;

                case 0x03:  // O3(n-2)
                    if (((mem_mask & 0x000000ffU) != 0))
                        voice.o3n2 = (voice.o3n2 & ~0x00ff) | (data & 0x00ff);
                    if (((mem_mask & 0x0000ff00U) != 0))
                        voice.o3n2 = (short)((voice.o3n2 & ~0xff00) | (data & 0xff00));
                    //LOG("%s:voice %d, O3(n-2)=%04x\n", machine().describe_context(), m_current_page & 0x1f, voice.o3n2 & 0xffff);
                    break;

                case 0x04:  // O2(n-1)
                    if (((mem_mask & 0x000000ffU) != 0))
                        voice.o2n1 = (voice.o2n1 & ~0x00ff) | (data & 0x00ff);
                    if (((mem_mask & 0x0000ff00U) != 0))
                        voice.o2n1 = (short)((voice.o2n1 & ~0xff00) | (data & 0xff00));
                    //LOG("%s:voice %d, O2(n-1)=%04x\n", machine().describe_context(), m_current_page & 0x1f, voice.o2n1 & 0xffff);
                    break;

                case 0x05:  // O2(n-2)
                    if (((mem_mask & 0x000000ffU) != 0))
                        voice.o2n2 = (voice.o2n2 & ~0x00ff) | (data & 0x00ff);
                    if (((mem_mask & 0x0000ff00U) != 0))
                        voice.o2n2 = (short)((voice.o2n2 & ~0xff00) | (data & 0xff00));
                    //LOG("%s:voice %d, O2(n-2)=%04x\n", machine().describe_context(), m_current_page & 0x1f, voice.o2n2 & 0xffff);
                    break;

                case 0x06:  // O1(n-1)
                    if (((mem_mask & 0x000000ffU) != 0))
                        voice.o1n1 = (voice.o1n1 & ~0x00ff) | (data & 0x00ff);
                    if (((mem_mask & 0x0000ff00U) != 0))
                        voice.o1n1 = (short)((voice.o1n1 & ~0xff00) | (data & 0xff00));
                    //LOG("%s:voice %d, O1(n-1)=%04x (accum=%08x)\n", machine().describe_context(), m_current_page & 0x1f, voice->o1n1 & 0xffff, get_address_acc_res(voice->accum));
                    break;

                case 0x07:
                case 0x08:
                case 0x09:
                case 0x0a:
                case 0x0b:
                case 0x0c:  // unused
                    break;

                case 0x0d:  // ACT
                    if (((mem_mask & 0x000000ffU) != 0))
                    {
                        m_active_voices = (byte)(data & 0x1f);
                        m_sample_rate = (int)(m_master_clock / (16 * (m_active_voices + 1)));
                        //m_stream.set_sample_rate(m_sample_rate);
                        //m_sample_rate_changed_cb(m_sample_rate);

                        //LOG("active voices=%d, sample_rate=%d\n", m_active_voices, m_sample_rate);
                    }
                    break;

                case 0x0e:  // IRQV - read only
                    break;

                case 0x0f:  // PAGE
                    if (((mem_mask & 0x000000ffU) != 0))
                        m_current_page = (byte)(data & 0x7f);
                    break;
            }
        }


        private void reg_write_test(ref es550x_voice voice, int offset, ushort data, ushort mem_mask)
        {
            switch (offset)
            {
                case 0x00:  // CH0L

                case 0x01:  // CH0R
                case 0x02:  // CH1L
                case 0x03:  // CH1R
                case 0x04:  // CH2L
                case 0x05:  // CH2R
                case 0x06:  // CH3L
                case 0x07:  // CH3R
                    break;

                case 0x08:  // SERMODE
                    m_mode |= 0x7f8; // bit 10-3 always 1
                    if (((mem_mask & 0x0000ff00U) != 0))
                        m_mode = (ushort)((m_mode & ~0xf800) | (data & 0xf800)); // MSB[4:0] (unknown purpose)
                    if (((mem_mask & 0x000000ffU) != 0))
                        m_mode = (ushort)((m_mode & ~0x0007) | (data & 0x0007)); // SONY/BB, TEST, A/D
                    //LOGMASKED(LOG_SERIAL, "%s: serial mode = %04x & %04x", machine().describe_context(), m_mode, mem_mask);
                    break;

                case 0x09:  // PAR
                    break;

                case 0x0d:  // ACT
                    if (((mem_mask & 0x000000ffU) != 0))
                    {
                        m_active_voices = (byte)(data & 0x1f);
                        m_sample_rate = (int)(m_master_clock / (16 * (m_active_voices + 1)));
                        //m_stream.set_sample_rate(m_sample_rate);
                        //m_sample_rate_changed_cb(m_sample_rate);

                        //LOG("active voices=%d, sample_rate=%d\n", m_active_voices, m_sample_rate);
                    }
                    break;

                case 0x0e:  // IRQV - read only
                    break;

                case 0x0f:  // PAGE
                    if (((mem_mask & 0x000000ffU) != 0))
                        m_current_page = (byte)(data & 0x7f);
                    break;
            }
        }


        private void write(int offset, ushort data, ushort mem_mask)
        {
            es550x_voice voice = m_voice[m_current_page & 0x1f];

            //  logerror("%s:ES5505 write %02x/%02x = %04x & %04x\n", machine().describe_context(), m_current_page, offset, data, mem_mask);

            // force an update
            //m_stream.update();

            // switch off the page and register
            if (m_current_page < 0x20)
                reg_write_low(ref voice, offset, data, mem_mask);
            else if (m_current_page < 0x40)
                reg_write_high(ref voice, offset, data, mem_mask);
            else
                reg_write_test(ref voice, offset, data, mem_mask);
        }



        /**********************************************************************************************

             reg_read -- read from the specified ES5505 register

        ***********************************************************************************************/

        private ushort reg_read_low(ref es550x_voice voice, int offset)
        {
            ushort result = 0;

            switch (offset)
            {
                case 0x00:  // CR
                    result = (ushort)(voice.control | 0xf000);
                    break;

                case 0x01:  // FC
                    result = (ushort)get_address_acc_res(voice.freqcount, 1);
                    break;

                case 0x02:  // STRT (hi)
                    result = (ushort)(get_address_acc_res(voice.start) >> 16);
                    break;

                case 0x03:  // STRT (lo)
                    result = (ushort)get_address_acc_res(voice.start);
                    break;

                case 0x04:  // END (hi)
                    result = (ushort)(get_address_acc_res(voice.end) >> 16);
                    break;

                case 0x05:  // END (lo)
                    result = (ushort)get_address_acc_res(voice.end);
                    break;

                case 0x06:  // K2
                    result = (ushort)voice.k2;
                    break;

                case 0x07:  // K1
                    result = (ushort)voice.k1;
                    break;

                case 0x08:  // LVOL
                    result = (ushort)(voice.lvol << 8);
                    break;

                case 0x09:  // RVOL
                    result = (ushort)(voice.rvol << 8);
                    break;

                case 0x0a:  // ACC (hi)
                    result = (ushort)(get_address_acc_res(voice.accum) >> 16);
                    break;

                case 0x0b:  // ACC (lo)
                    result = (ushort)get_address_acc_res(voice.accum);
                    break;

                case 0x0c:  // unused
                    break;

                case 0x0d:  // ACT
                    result = m_active_voices;
                    break;

                case 0x0e:  // IRQV
                    result = m_irqv;
                    //if (!machine().side_effects_disabled())
                        //update_internal_irq_state();
                    break;

                case 0x0f:  // PAGE
                    result = m_current_page;
                    break;
            }
            return result;
        }


        private ushort reg_read_high(ref es550x_voice voice, int offset)
        {
            ushort result = 0;

            switch (offset)
            {
                case 0x00:  // CR
                    result = (ushort)(voice.control | 0xf000);
                    break;

                case 0x01:  // O4(n-1)
                    result = (ushort)(voice.o4n1 & 0xffff);
                    break;

                case 0x02:  // O3(n-1)
                    result = (ushort)(voice.o3n1 & 0xffff);
                    break;

                case 0x03:  // O3(n-2)
                    result = (ushort)(voice.o3n2 & 0xffff);
                    break;

                case 0x04:  // O2(n-1)
                    result = (ushort)(voice.o2n1 & 0xffff);
                    break;

                case 0x05:  // O2(n-2)
                    result = (ushort)(voice.o2n2 & 0xffff);
                    break;

                case 0x06:  // O1(n-1)
                            // special case for the Taito F3 games: they set the accumulator on a stopped
                            // voice and assume the filters continue to process the data. They then read
                            // the O1(n-1) in order to extract raw data from the sound ROMs. Since we don't
                            // want to waste time filtering stopped channels, we just look for a read from
                            // this register on a stopped voice, and return the raw sample data at the
                            // accumulator
                    if ((voice.control & (uint)CONTROL.CONTROL_STOPMASK) != 0)
                    {
                        voice.o1n1 = read_sample(ref voice, (int)get_integer_addr(voice.accum));
                        // logerror("%02x %08x ==> %08x\n",voice.o1n1,get_bank(voice.control),get_integer_addr(voice.accum));
                    }
                    result = (ushort)(voice.o1n1 & 0xffff);
                    break;

                case 0x07:
                case 0x08:
                case 0x09:
                case 0x0a:
                case 0x0b:
                case 0x0c:  // unused
                    break;

                case 0x0d:  // ACT
                    result = m_active_voices;
                    break;

                case 0x0e:  // IRQV
                    result = m_irqv;
                    //if (!machine().side_effects_disabled())
                        //update_internal_irq_state();
                    break;

                case 0x0f:  // PAGE
                    result = m_current_page;
                    break;
            }
            return result;
        }


        private ushort reg_read_test(ref es550x_voice voice, int offset)
        {
            ushort result = 0;

            switch (offset)
            {
                case 0x00:  // CH0L
                case 0x01:  // CH0R
                case 0x02:  // CH1L
                case 0x03:  // CH1R
                case 0x04:  // CH2L
                case 0x05:  // CH2R
                case 0x06:  // CH3L
                case 0x07:  // CH3R
                    break;

                case 0x08:  // SERMODE
                    result = (ushort)(m_mode | 0x7f8);
                    break;

                case 0x09:  // PAR
                    //if (!m_read_port_cb.isunset())
                        //result = (ushort)(m_read_port_cb(0) & 0xffc0); // 10 bit, 15:6
                    break;

                // The following are global, and thus accessible form all pages
                case 0x0d:  // ACT
                    result = m_active_voices;
                    break;

                case 0x0e:  // IRQV
                    result = m_irqv;
                    //if (!machine().side_effects_disabled())
                        //update_internal_irq_state();
                    break;

                case 0x0f:  // PAGE
                    result = m_current_page;
                    break;
            }
            return result;
        }


        private ushort read(int offset)
        {
            es550x_voice voice = m_voice[m_current_page & 0x1f];
            ushort result;

            //LOG("read from %02x/%02x -> ", m_current_page, offset);

            // force an update
            //m_stream.update();

            // switch off the page and register
            if (m_current_page < 0x20)
                result = reg_read_low(ref voice, offset);
            else if (m_current_page < 0x40)
                result = reg_read_high(ref voice, offset);
            else
                result = reg_read_test(ref voice, offset);

            //LOG("%04x (accum=%08x)\n", result, voice->accum);

            // return the high byte
            return result;
        }



























    }
}
