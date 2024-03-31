using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDSound
{
    public class K051649 : Instrument
    {
        public override void Reset(byte ChipID)
        {
            device_reset_k051649(ChipID);
            visVolume = new int[2][][] {
                new int[1][] { new int[2] { 0, 0 } }
                , new int[1][] { new int[2] { 0, 0 } }
            };
        }

        public override uint Start(byte ChipID, uint clock)
        {
            return (UInt32)device_start_k051649(ChipID, (Int32)clock);
        }

        public override uint Start(byte ChipID, uint SamplingRate, uint clock, params object[] Option)
        {
            if (SCC1Data[ChipID] == null)
            {
                SCC1Data[ChipID] = new k051649_state();
                for (int i = 0; i < SCC1Data[ChipID].channel_list.Length; i++)
                {
                    SCC1Data[ChipID].channel_list[i] = new k051649_sound_channel();
                }
            }

            UInt32 sampRate = (UInt32)device_start_k051649(ChipID, (Int32)clock);
            //int flags = 1;
            //if (Option != null && Option.Length > 0) flags = (int)(byte)Option[0];
            //k054539_init_flags(ChipID, flags);

            return sampRate;
        }

        public override void Stop(byte ChipID)
        {
            device_stop_k051649(ChipID);
        }

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {
            k051649_update(ChipID, outputs, samples);

            visVolume[ChipID][0][0] = outputs[0][0];
            visVolume[ChipID][0][1] = outputs[1][0];
        }



        /***************************************************************************

    Konami 051649 - SCC1 sound as used in Haunted Castle, City Bomber

    This file is pieced together by Bryan McPhail from a combination of
    Namco Sound, Amuse by Cab, Haunted Castle schematics and whoever first
    figured out SCC!

    The 051649 is a 5 channel sound generator, each channel gets its
    waveform from RAM (32 bytes per waveform, 8 bit signed data).

    This sound chip is the same as the sound chip in some Konami
    megaROM cartridges for the MSX. It is actually well researched
    and documented:

        http://bifi.msxnet.org/msxnet/tech/scc.html

    Thanks to Sean Young (sean@mess.org) for some bugfixes.

    K052539 is more or less equivalent to this chip except channel 5
    does not share waveram with channel 4.

***************************************************************************/

        //# include "mamedef.h"
        //# include <stdlib.h>
        //# include <string.h>	// for memset
        //#include "emu.h"
        //#include "streams.h"
        //# include "k051649.h"

        private const Int32 FREQ_BITS = 16;
        private const Int32 DEF_GAIN = 8;

        /* this structure defines the parameters for a channel */
        public class k051649_sound_channel
        {
            public UInt64 counter;
            public Int32 frequency;
            public Int32 volume;
            public Int32 key;
            public sbyte[] waveram = new sbyte[32];        /* 19991207.CAB */
            public byte Muted;
        }

        public class k051649_state
        {
            public k051649_sound_channel[] channel_list = new k051649_sound_channel[5];

            /* global sound parameters */
            //sound_stream * stream;
            public Int32 mclock, rate;

            /* mixer tables and internal buffers */
            public Int16[] mixer_table;
            public Int32 ptr_mixer_table;
            public Int16[] mixer_lookup;
            public Int32 ptr_mixer_lookup;
            public Int16[] mixer_buffer;
            public Int32 ptr_mixer_buffer;

            public Int32 cur_reg;
            public byte test;
        }

        private const Int32 MAX_CHIPS = 0x02;
        private k051649_state[] SCC1Data = new k051649_state[MAX_CHIPS];

        public override string Name { get { return "K051649"; } set { } }
        public override string ShortName { get { return "K051"; } set { } }

        /*INLINE k051649_state *get_safe_token(running_device *device)
        {
            assert(device != NULL);
            assert(device->type() == K051649);
            return (k051649_state *)downcast<legacy_device_base *>(device)->token();
        }*/

        /* build a table to divide by the number of voices */
        private void make_mixer_table(/*running_machine *machine,*/ k051649_state info, Int32 voices)
        {
            Int32 count = voices * 256;
            Int32 i;

            /* allocate memory */
            //info->mixer_table = auto_alloc_array(machine, INT16, 512 * voices);
            info.mixer_table = new Int16[2 * count];// (INT16*)malloc(sizeof(INT16) * 2 * count);
            info.ptr_mixer_table = 0;

            /* find the middle of the table */
            info.ptr_mixer_lookup = count;

            /* fill in the table - 16 bit case */
            for (i = 0; i < count; i++)
            {
                Int32 val = i * DEF_GAIN * 16 / voices;
                //if (val > 32767) val = 32767;
                if (val > 32768) val = 32768;
                info.mixer_table[info.ptr_mixer_lookup + i] = (Int16)val;
                info.mixer_table[info.ptr_mixer_lookup - i] = (Int16)(-val);
            }
        }


        /* generate sound to the mix buffer */
        //static STREAM_UPDATE( k051649_update )
        private void k051649_update(byte ChipID, Int32[][] outputs, Int32 samples)
        {
            //k051649_state *info = (k051649_state *)param;
            k051649_state info = SCC1Data[ChipID];
            k051649_sound_channel[] voice = info.channel_list;
            Int32[] buffer = outputs[0];
            Int32[] buffer2 = outputs[1];
            Int16[] mix;
            Int32 ptr_mix = 0;
            Int32 i, j;

            // zap the contents of the mixer buffer
            //memset(info->mixer_buffer, 0, samples * sizeof(short));
            if (info.mixer_buffer == null || info.mixer_buffer.Length < samples)
            {
                info.mixer_buffer = new Int16[samples];
            }
            for (i = 0; i < samples; i++) info.mixer_buffer[i] = 0;

            for (j = 0; j < 5; j++)
            {
                // channel is halted for freq < 9
                if (voice[j].frequency > 8 && voice[j].Muted == 0)
                {
                    sbyte[] w = voice[j].waveram;            /* 19991207.CAB */
                    Int32 v = voice[j].volume * voice[j].key;
                    Int32 c = (Int32)voice[j].counter;
                    /* Amuse source:  Cab suggests this method gives greater resolution */
                    /* Sean Young 20010417: the formula is really: f = clock/(16*(f+1))*/
                    Int32 step = (Int32)(((Int64)info.mclock * (1 << FREQ_BITS)) / (float)((voice[j].frequency + 1) * 16 * (info.rate / 32)) + 0.5);

                    mix = info.mixer_buffer;
                    ptr_mix = 0;

                    // add our contribution
                    for (i = 0; i < samples; i++)
                    {
                        Int32 offs;

                        c += step;
                        offs = (c >> FREQ_BITS) & 0x1f;
                        mix[ptr_mix++] += (Int16)((w[offs] * v) >> 3);
                    }

                    // update the counter for this voice
                    voice[j].counter = (UInt64)c;
                }
            }

            // mix it down
            mix = info.mixer_buffer;
            ptr_mix = 0;
            for (i = 0; i < samples; i++)
            {
                buffer[i] = buffer2[i] = info.mixer_table[info.ptr_mixer_lookup + mix[ptr_mix++]];
                i++;
            }
        }

        //static DEVICE_START( k051649 )
        private Int32 device_start_k051649(byte ChipID, Int32 clock)
        {
            //k051649_state *info = get_safe_token(device);
            k051649_state info;
            byte CurChn;

            if (ChipID >= MAX_CHIPS)
                return 0;

            info = SCC1Data[ChipID];
            /* get stream channels */
            //info->rate = device->clock()/16;
            //info->stream = stream_create(device, 0, 1, info->rate, info, k051649_update);
            //info->mclock = device->clock();
            info.mclock = clock & 0x7FFFFFFF;
            info.rate = info.mclock / 16;

            /* allocate a buffer to mix into - 1 second's worth should be more than enough */
            //info->mixer_buffer = auto_alloc_array(device->machine, short, 2 * info->rate);
            info.mixer_buffer = new Int16[info.rate];// (short*)malloc(sizeof(short) * info->rate);

            /* build the mixer table */
            //make_mixer_table(device->machine, info, 5);
            make_mixer_table(info, 5);

            for (CurChn = 0; CurChn < 5; CurChn++)
                info.channel_list[CurChn].Muted = 0x00;

            return info.rate;
        }

        private void device_stop_k051649(byte ChipID)
        {
            k051649_state info = SCC1Data[ChipID];

            //free(info->mixer_buffer);
            //free(info->mixer_table);

            return;
        }

        //static DEVICE_RESET( k051649 )
        private void device_reset_k051649(byte ChipID)
        {
            //k051649_state *info = get_safe_token(device);
            k051649_state info = SCC1Data[ChipID];
            k051649_sound_channel[] voice = info.channel_list;
            Int32 i;

            // reset all the voices
            for (i = 0; i < 5; i++)
            {
                voice[i].frequency = 0;
                voice[i].volume = 0;
                voice[i].counter = 0;
                voice[i].key = 0;
            }

            // other parameters
            info.test = 0x00;
            info.cur_reg = 0x00;

            return;
        }

        /********************************************************************************/

        //WRITE8_DEVICE_HANDLER( k051649_waveform_w )
        private void k051649_waveform_w(byte ChipID, Int32 offset, byte data)
        {
            //k051649_state *info = get_safe_token(device);
            k051649_state info = SCC1Data[ChipID];

            // waveram is read-only?
            if (((info.test & 0x40) != 0) || (((info.test & 0x80) != 0) && offset >= 0x60))
                return;

            //stream_update(info->stream);

            if (offset >= 0x60)
            {
                // channel 5 shares waveram with channel 4
                info.channel_list[3].waveram[offset & 0x1f] = (sbyte)data;
                info.channel_list[4].waveram[offset & 0x1f] = (sbyte)data;
            }
            else
            {
                int ch = offset >> 5;
                if (ch < info.channel_list.Length)
                {
                    info.channel_list[offset >> 5].waveram[offset & 0x1f] = (sbyte)data;
                }
            }
        }

        //READ8_DEVICE_HANDLER ( k051649_waveform_r )
        private byte k051649_waveform_r(byte ChipID, Int32 offset)
        {
            //k051649_state *info = get_safe_token(device);
            k051649_state info = SCC1Data[ChipID];
            int ch;
            // test-register bits 6/7 expose the internal counter
            if ((info.test & 0xc0) != 0)
            {
                //stream_update(info->stream);

                if (offset >= 0x60)
                {
                    ch = 3 + (info.test >> 6 & 1);
                    if (ch < info.channel_list.Length)
                    {
                        offset += (Int32)((info.channel_list[ch].counter >> FREQ_BITS));
                    }
                }
                else if ((info.test & 0x40) != 0)
                {
                    ch = offset >> 5;
                    if (ch < info.channel_list.Length)
                    {
                        offset += (Int32)((info.channel_list[ch].counter >> FREQ_BITS));
                    }
                }
            }

            ch = offset >> 5;
            if (ch < info.channel_list.Length)
            {
                return (byte)info.channel_list[ch].waveram[offset & 0x1f];
            }
            return 0;
        }

        /* SY 20001114: Channel 5 doesn't share the waveform with channel 4 on this chip */
        //WRITE8_DEVICE_HANDLER( k052539_waveform_w )
        private void k052539_waveform_w(byte ChipID, Int32 offset, byte data)
        {
            //k051649_state *info = get_safe_token(device);
            k051649_state info = SCC1Data[ChipID];

            // waveram is read-only?
            if ((info.test & 0x40) != 0)
                return;

            //stream_update(info->stream);
            int ch = offset >> 5;
            if (ch < info.channel_list.Length)
            {
                info.channel_list[ch].waveram[offset & 0x1f] = (sbyte)data;
            }
        }

        //READ8_DEVICE_HANDLER ( k052539_waveform_r )
        private byte k052539_waveform_r(byte ChipID, Int32 offset)
        {
            //k051649_state *info = get_safe_token(device);
            k051649_state info = SCC1Data[ChipID];
            int ch;
            // test-register bit 6 exposes the internal counter
            if ((info.test & 0x40) != 0)
            {
                //stream_update(info->stream);
                ch = offset >> 5;
                if (ch < info.channel_list.Length)
                {
                    offset += (Int32)((info.channel_list[ch].counter >> FREQ_BITS));
                }
            }
            ch = offset >> 5;
            if (ch < info.channel_list.Length)
            {
                return (byte)info.channel_list[ch].waveram[offset & 0x1f];
            }
            return 0;
        }

        //WRITE8_DEVICE_HANDLER( k051649_volume_w )
        private void k051649_volume_w(byte ChipID, Int32 offset, byte data)
        {
            //k051649_state *info = get_safe_token(device);
            k051649_state info = SCC1Data[ChipID];
            //stream_update(info->stream);
            int ch = offset &0x7;
            if (ch < info.channel_list.Length)
            {
                info.channel_list[ch].volume = data & 0xf;
            }
        }

        //WRITE8_DEVICE_HANDLER( k051649_frequency_w )
        private void k051649_frequency_w(byte ChipID, Int32 offset, byte data)
        {
            //k051649_state *info = get_safe_token(device);
            k051649_state info = SCC1Data[ChipID];
            k051649_sound_channel chn;

            int ch = offset >> 1;
            if (ch >= info.channel_list.Length) return;
            chn = info.channel_list[ch];

            //stream_update(info->stream);

            // test-register bit 5 resets the internal counter
            if ((info.test & 0x20) != 0)
                chn.counter = 0xffffffffffffffff;//~0
            else if (chn.frequency < 9)
                chn.counter |= ((1 << FREQ_BITS) - 1);

            // update frequency
            if ((offset & 1) != 0)
                chn.frequency = (chn.frequency & 0x0FF) | ((data << 8) & 0xF00);
            else
                chn.frequency = (chn.frequency & 0xF00) | (data << 0);
            chn.counter &= 0xFFFF0000; // Valley Bell: Behaviour according to openMSX
        }

        //WRITE8_DEVICE_HANDLER( k051649_keyonoff_w )
        private void k051649_keyonoff_w(byte ChipID, Int32 offset, byte data)
        {
            //k051649_state *info = get_safe_token(device);
            k051649_state info = SCC1Data[ChipID];
            int i;
            //stream_update(info->stream);

            for (i = 0; i < 5; i++)
            {
                info.channel_list[i].key = data & 1;
                data >>= 1;
            }
        }

        //WRITE8_MEMBER( k051649_device::k051649_test_w )
        private void k051649_test_w(byte ChipID, Int32 offset, byte data)
        {
            k051649_state info = SCC1Data[ChipID];
            info.test = data;
        }


        //READ8_MEMBER ( k051649_device::k051649_test_r )
        private byte k051649_test_r(byte ChipID, Int32 offset)
        {
            // reading the test register sets it to $ff!
            k051649_test_w(ChipID, offset, 0xff);
            return 0xff;
        }


        private void k051649_w(byte ChipID, int offset, byte data)
        {
            k051649_state info = SCC1Data[ChipID];
            if (info == null) return;

            switch (offset & 1)
            {
                case 0x00:
                    info.cur_reg = data;
                    break;
                case 0x01:
                    switch (offset >> 1)
                    {
                        case 0x00:
                            k051649_waveform_w(ChipID, info.cur_reg, data);
                            break;
                        case 0x01:
                            k051649_frequency_w(ChipID, info.cur_reg, data);
                            break;
                        case 0x02:
                            k051649_volume_w(ChipID, info.cur_reg, data);
                            break;
                        case 0x03:
                            k051649_keyonoff_w(ChipID, info.cur_reg, data);
                            break;
                        case 0x04:
                            k052539_waveform_w(ChipID, info.cur_reg, data);
                            break;
                        case 0x05:
                            k051649_test_w(ChipID, info.cur_reg, data);
                            break;
                    }
                    break;
            }

            return;
        }

        private void k051649_set_mute_mask(byte ChipID, UInt32 MuteMask)
        {
            k051649_state info = SCC1Data[ChipID];
            byte CurChn;

            for (CurChn = 0; CurChn < 5; CurChn++)
                info.channel_list[CurChn].Muted = (byte)((MuteMask >> CurChn) & 0x01);

            return;
        }

        public override int Write(byte ChipID, int port, int adr, int data)
        {
            k051649_w(ChipID, adr, (byte)data);
            return 0;
        }

        public k051649_state GetK051649_State(byte ChipID)
        {
            return SCC1Data[ChipID];
        }

        /**************************************************************************
         * Generic get_info
         **************************************************************************/

        /*DEVICE_GET_INFO( k051649 )
        {
            switch (state)
            {
                // --- the following bits of info are returned as 64-bit signed integers ---
                case DEVINFO_INT_TOKEN_BYTES:					info->i = sizeof(k051649_state);				break;

                // --- the following bits of info are returned as pointers to data or functions ---
                case DEVINFO_FCT_START:							info->start = DEVICE_START_NAME( k051649 );		break;
                case DEVINFO_FCT_STOP:							// nothing //									break;
                case DEVINFO_FCT_RESET:							info->reset = DEVICE_RESET_NAME( k051649 );		break;

                // --- the following bits of info are returned as NULL-terminated strings ---
                case DEVINFO_STR_NAME:							strcpy(info->s, "K051649");						break;
                case DEVINFO_STR_FAMILY:					strcpy(info->s, "Konami custom");				break;
                case DEVINFO_STR_VERSION:					strcpy(info->s, "1.0");							break;
                case DEVINFO_STR_SOURCE_FILE:						strcpy(info->s, __FILE__);						break;
                case DEVINFO_STR_CREDITS:					strcpy(info->s, "Copyright Nicola Salmoria and the MAME Team"); break;
            }
        }*/


        //DEFINE_LEGACY_SOUND_DEVICE(K051649, k051649);

    }
}
