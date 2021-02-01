using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDSound
{
    public class rf5c68 : Instrument
    {
        public override string Name { get { return "RF5C68"; } set { } }
        public override string ShortName { get { return "RF68"; } set { } }

        public override void Reset(byte ChipID)
        {
            visVolume = new int[2][][] {
                new int[1][] { new int[2] { 0, 0 } }
                , new int[1][] { new int[2] { 0, 0 } }
            };
        }

        public override uint Start(byte ChipID, uint clock)
        {
            return Start(ChipID, clock, 0, null);
        }

        public override uint Start(byte ChipID, uint clock, uint ClockValue, params object[] option)
        {
            return (uint)device_start_rf5c68(ChipID, (int)ClockValue);
        }

        public override void Stop(byte ChipID)
        {
            device_stop_rf5c68(ChipID);
        }

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {
            rf5c68_update(ChipID, outputs, samples);

            visVolume[ChipID][0][0] = outputs[0][0];
            visVolume[ChipID][0][1] = outputs[1][0];
        }

        public override int Write(byte ChipID, int port, int adr, int data)
        {
            rf5c68_w(ChipID, adr, (byte)data);
            return 0;
        }


        //void rf5c68_update(byte ChipID, stream_sample_t** outputs, int samples);
        //int device_start_rf5c68(byte ChipID, int clock);
        //void device_stop_rf5c68(byte ChipID);
        //void device_reset_rf5c68(byte ChipID);
        //void rf5c68_w(byte ChipID, offs_t offset, byte data);
        //byte rf5c68_mem_r(byte ChipID, offs_t offset);
        //void rf5c68_mem_w(byte ChipID, offs_t offset, byte data);
        //void rf5c68_write_ram(byte ChipID, offs_t DataStart, offs_t DataLength, const byte* RAMData);
        //void rf5c68_set_mute_mask(byte ChipID, UINT32 MuteMask);


        /*********************************************************/
        /*    ricoh RF5C68(or clone) PCM controller              */
        /*********************************************************/

        //# include "mamedef.h"
        //# include <stdlib.h>
        //# include <string.h>	// for memset
        //# include <stddef.h>	// for NULL
        //#include "sndintrf.h"
        //#include "streams.h"
        //# include "rf5c68.h"
        //# include <math.h>


        private const int NUM_CHANNELS = (8);
        private const int STEAM_STEP = 0x800;



        public class pcm_channel
        {
            public byte enable;
            public byte env;
            public byte pan;
            public byte start;
            public UInt32 addr;
            public UInt16 step;
            public UInt16 loopst;
            public byte Muted;

            public bool key = false;//発音中ならtrue
            public bool keyOn = false;//キーオンしたときにtrue(falseは受け取り側が行う)
        };
        //public _pcm_channel pcm_channel;

        public class mem_stream
        {
            public UInt32 BaseAddr;
            public UInt32 EndAddr;
            public UInt32 CurAddr;
            public UInt16 CurStep;
            public byte[] MemPnt;
        };
        //public _mem_stream mem_stream;

        public class rf5c68_state
        {
            //sound_stream *		stream;
            public pcm_channel[] chan = new pcm_channel[NUM_CHANNELS] {
                new pcm_channel(), new pcm_channel(), new pcm_channel(), new pcm_channel()
                , new pcm_channel(), new pcm_channel(), new pcm_channel(), new pcm_channel()
            };
            public byte cbank;
            public byte wbank;
            public byte enable;
            public UInt32 datasize;
            public byte[] data;
            //void				(*sample_callback)(running_device* device,int channel);
            public mem_stream memstrm = new mem_stream();
        };
        //public _rf5c68_state rf5c68_state;


        //static void rf5c68_mem_stream_flush(rf5c68_state* chip);

        private const int MAX_CHIPS = 0x02;
        public rf5c68_state[] RF5C68Data = new rf5c68_state[MAX_CHIPS] { new rf5c68_state(), new rf5c68_state() };

        /*INLINE rf5c68_state *get_safe_token(const device_config *device)
        {
            assert(device != NULL);
            assert(device->token != NULL);
            assert(device->type == SOUND);
            assert(sound_get_type(device) == SOUND_RF5C68);
            return (rf5c68_state *)device->token;
        }*/

        /************************************************/
        /*    RF5C68 stream update                      */
        /************************************************/

        private void memstream_sample_check(rf5c68_state chip, UInt32 addr, UInt16 Speed)
        {
            mem_stream ms = chip.memstrm;
            UInt32 SmplSpd;

            SmplSpd = (uint)((Speed >= 0x0800) ? (Speed >> 11) : 1);
            if (addr >= ms.CurAddr)
            {
                // Is the stream too fast? (e.g. about to catch up the output)
                if (addr - ms.CurAddr <= SmplSpd * 5)
                {
                    // Yes - delay the stream
                    ms.CurAddr -= SmplSpd * 4;
                    if (ms.CurAddr < ms.BaseAddr)
                        ms.CurAddr = ms.BaseAddr;
                }
            }
            else
            {
                // Is the stream too slow? (e.g. the output is about to catch up the stream)
                if (ms.CurAddr - addr <= SmplSpd * 5)
                {
                    if (ms.CurAddr + SmplSpd * 4 >= ms.EndAddr)
                    {
                        rf5c68_mem_stream_flush(chip);
                    }
                    else
                    {
                        //memcpy(chip.data + ms.CurAddr, ms.MemPnt + (ms.CurAddr - ms.BaseAddr), SmplSpd * 4);
                        Array.Copy(ms.MemPnt, (ms.CurAddr - ms.BaseAddr), chip.data, ms.CurAddr, SmplSpd * 4);
                        ms.CurAddr += SmplSpd * 4;
                    }
                }
            }

            return;
        }

        //static STREAM_UPDATE( rf5c68_update )
        private void rf5c68_update(byte ChipID, int[][] outputs, int samples)
        {
            //rf5c68_state *chip = (rf5c68_state *)param;
            rf5c68_state chip = RF5C68Data[ChipID];
            mem_stream ms = chip.memstrm;
            int[] left = outputs[0];
            int[] right = outputs[1];
            int i, j;

            /* start with clean buffers */
            for (int ind = 0; ind < samples; ind++)
            {
                left[ind] = 0;
                right[ind] = 0;
            }

            /* bail if not enabled */
            if (chip.enable == 0)
                return;

            /* loop over channels */
            for (i = 0; i < NUM_CHANNELS; i++)
            {
                pcm_channel chan = chip.chan[i];

                /* if this channel is active, accumulate samples */
                if (chan.enable != 0)// && chan.Muted == 0)
                {
                    int lv = (chan.pan & 0x0f) * chan.env;
                    int rv = ((chan.pan >> 4) & 0x0f) * chan.env;

                    /* loop over the sample buffer */
                    for (j = 0; j < samples; j++)
                    {
                        int sample;

                        /* trigger sample callback */
                        /*if(chip.sample_callback)
                        {
                            if(((chan.addr >> 11) & 0xfff) == 0xfff)
                                chip.sample_callback(chip.device,((chan.addr >> 11)/0x2000));
                        }*/

                        memstream_sample_check(chip, (chan.addr >> 11) & 0xFFFF, chan.step);
                        /* fetch the sample and handle looping */
                        sample = chip.data[(chan.addr >> 11) & 0xffff];
                        if (sample == 0xff)
                        {
                            chan.addr = (uint)(chan.loopst << 11);
                            sample = chip.data[(chan.addr >> 11) & 0xffff];

                            /* if we loop to a loop point, we're effectively dead */
                            if (sample == 0xff)
                            {
                                chan.key = false;
                                break;
                            }
                        }
                        chan.key = true;
                        chan.addr += chan.step;

                        if (chan.Muted == 0)
                        {
                            /* add to the buffer */
                            if ((sample & 0x80) != 0)
                            {
                                sample &= 0x7f;
                                left[j] += (sample * lv) >> 5;
                                right[j] += (sample * rv) >> 5;
                            }
                            else
                            {
                                left[j] -= (sample * lv) >> 5;
                                right[j] -= (sample * rv) >> 5;
                            }
                        }

                        //Console.WriteLine("Ch:{0} L:{1} R:{2}", i, outputs[0][j], outputs[1][j]);
                    }
                }
            }

            if (samples != 0 && ms.CurAddr < ms.EndAddr)
            {
                ms.CurStep += (UInt16)(STEAM_STEP * samples);
                if (ms.CurStep >= 0x0800)  // 1 << 11
                {
                    i = ms.CurStep >> 11;
                    ms.CurStep &= 0x07FF;

                    if (ms.CurAddr + i > ms.EndAddr)
                        i = (int)(ms.EndAddr - ms.CurAddr);

                    //memcpy(chip.data + ms.CurAddr, ms.MemPnt + (ms.CurAddr - ms.BaseAddr), i);
                    Array.Copy(ms.MemPnt, (ms.CurAddr - ms.BaseAddr), chip.data, ms.CurAddr, i);
                    ms.CurAddr += (uint)i;
                }
            }
            // I think, this is completely useless
            /* now clamp and shift the result (output is only 10 bits) */
            /*for (j = 0; j < samples; j++)
            {
                stream_sample_t temp;

                temp = left[j];
                if (temp > 32767) temp = 32767;
                else if (temp < -32768) temp = -32768;
                left[j] = temp & ~0x3f;

                temp = right[j];
                if (temp > 32767) temp = 32767;
                else if (temp < -32768) temp = -32768;
                right[j] = temp & ~0x3f;
            }*/
        }


        /************************************************/
        /*    RF5C68 start                              */
        /************************************************/

        //static DEVICE_START( rf5c68 )
        private int device_start_rf5c68(byte ChipID, int clock)
        {
            //const rf5c68_interface* intf = (const rf5c68_interface*)device->baseconfig().static_config();

            /* allocate memory for the chip */
            //rf5c68_state *chip = get_safe_token(device);
            rf5c68_state chip;
            int chn;

            if (ChipID >= MAX_CHIPS)
                return 0;

            chip = RF5C68Data[ChipID];

            chip.datasize = 0x10000;
            chip.data = new byte[chip.datasize];

            /* allocate the stream */
            //chip.stream = stream_create(device, 0, 2, device->clock / 384, chip, rf5c68_update);

            /* set up callback */
            /*if(intf != NULL)
                chip.sample_callback = intf->sample_end_callback;
            else
                chip.sample_callback = NULL;*/
            for (chn = 0; chn < NUM_CHANNELS; chn++)
                chip.chan[chn].Muted = 0x00;

            return (clock & 0x7FFFFFFF) / 384;
        }

        private void device_stop_rf5c68(byte ChipID)
        {
            rf5c68_state chip = RF5C68Data[ChipID];
            //free(chip.data);
            chip.data = null;

            return;
        }

        private void device_reset_rf5c68(byte ChipID)
        {
            rf5c68_state chip = RF5C68Data[ChipID];
            int i;
            pcm_channel chan;
            mem_stream ms = chip.memstrm;

            // Clear the PCM memory.
            //memset(chip.data, 0x00, chip.datasize);
            for (int ind = 0; ind < chip.datasize; ind++) chip.data[ind] = 0;
            chip.enable = 0;
            chip.cbank = 0;
            chip.wbank = 0;

            /* clear channel registers */
            for (i = 0; i < NUM_CHANNELS; i++)
            {
                chan = chip.chan[i];
                chan.enable = 0;
                chan.env = 0;
                chan.pan = 0;
                chan.start = 0;
                chan.addr = 0;
                chan.step = 0;
                chan.loopst = 0;
            }

            ms.BaseAddr = 0x0000;
            ms.CurAddr = 0x0000;
            ms.EndAddr = 0x0000;
            ms.CurStep = 0x0000;
            ms.MemPnt = null;
        }

        /************************************************/
        /*    RF5C68 write register                     */
        /************************************************/

        //WRITE8_DEVICE_HANDLER( rf5c68_w )
        public void rf5c68_w(byte ChipID, int offset, byte data)
        {
            //rf5c68_state *chip = get_safe_token(device);
            rf5c68_state chip = RF5C68Data[ChipID];
            pcm_channel chan = chip.chan[chip.cbank];
            int i;

            /* force the stream to update first */
            //stream_update(chip.stream);

            /* switch off the address */
            switch (offset)
            {
                case 0x00:  /* envelope */
                    chan.env = data;
                    break;

                case 0x01:  /* pan */
                    chan.pan = data;
                    break;

                case 0x02:  /* FDL */
                    chan.step = (UInt16)((chan.step & 0xff00) | (data & 0x00ff));
                    break;

                case 0x03:  /* FDH */
                    chan.step = (UInt16)((chan.step & 0x00ff) | ((data << 8) & 0xff00));
                    break;

                case 0x04:  /* LSL */
                    chan.loopst = (UInt16)((chan.loopst & 0xff00) | (data & 0x00ff));
                    break;

                case 0x05:  /* LSH */
                    chan.loopst = (UInt16)((chan.loopst & 0x00ff) | ((data << 8) & 0xff00));
                    break;

                case 0x06:  /* ST */
                    chan.start = data;
                    if (chan.enable == 0)
                        chan.addr = (uint)(chan.start << (8 + 11));
                    break;

                case 0x07:  /* control reg */
                    chip.enable = (byte)((data >> 7) & 1);
                    if ((data & 0x40) != 0)
                        chip.cbank = (byte)(data & 7);
                    else
                        chip.wbank = (byte)(data & 15);
                    break;

                case 0x08:  /* channel on/off reg */
                    for (i = 0; i < 8; i++)
                    {
                        byte old = chip.chan[i].enable;
                        
                        chip.chan[i].enable = (byte)((~data >> i) & 1);

                        if (old == 0 && chip.chan[i].enable != 0) chip.chan[i].keyOn = true;

                        if (chip.chan[i].enable == 0)
                            chip.chan[i].addr = (uint)(chip.chan[i].start << (8 + 11));
                    }
                    break;
            }
        }


        /************************************************/
        /*    RF5C68 read memory                        */
        /************************************************/

        //READ8_DEVICE_HANDLER( rf5c68_mem_r )
        private byte rf5c68_mem_r(byte ChipID, int offset)
        {
            //rf5c68_state *chip = get_safe_token(device);
            rf5c68_state chip = RF5C68Data[ChipID];
            return chip.data[chip.wbank * 0x1000 + offset];
        }


        /************************************************/
        /*    RF5C68 write memory                       */
        /************************************************/

        //WRITE8_DEVICE_HANDLER( rf5c68_mem_w )
        public void rf5c68_mem_w(byte ChipID, int offset, byte data)
        {
            //rf5c68_state *chip = get_safe_token(device);
            rf5c68_state chip = RF5C68Data[ChipID];
            rf5c68_mem_stream_flush(chip);
            chip.data[chip.wbank * 0x1000 | offset] = data;
        }

        private void rf5c68_mem_stream_flush(rf5c68_state chip)
        {
            mem_stream ms = chip.memstrm;

            if (ms.CurAddr >= ms.EndAddr)
                return;

            //memcpy(chip.data + ms.CurAddr, ms.MemPnt + (ms.CurAddr - ms.BaseAddr), ms.EndAddr - ms.CurAddr);
            Array.Copy(ms.MemPnt, (ms.CurAddr - ms.BaseAddr), chip.data, ms.CurAddr, ms.EndAddr - ms.CurAddr);
            ms.CurAddr = ms.EndAddr;

            return;
        }

        private void rf5c68_write_ram(byte ChipID, int DataStart, int DataLength, byte[] RAMData)
        {

            rf5c68_state chip = RF5C68Data[ChipID];
            mem_stream ms = chip.memstrm;
            UInt16 BytCnt;

            DataStart |= chip.wbank * 0x1000;
            if (DataStart >= chip.datasize)
                return;
            if (DataStart + DataLength > chip.datasize)
                DataLength = (int)(chip.datasize - DataStart);

            //memcpy(chip.data + DataStart, RAMData, DataLength);

            rf5c68_mem_stream_flush(chip);

            ms.BaseAddr = (uint)DataStart;
            ms.CurAddr = ms.BaseAddr;
            ms.EndAddr = (uint)(ms.BaseAddr + DataLength);
            ms.CurStep = 0x0000;
            ms.MemPnt = RAMData;

            //BytCnt = (STEAM_STEP * 32) >> 11;
            BytCnt = 0x40;  // SegaSonic Arcade: Run! Run! Run! needs such a high value
            if (ms.CurAddr + BytCnt > ms.EndAddr)
                BytCnt = (UInt16)(ms.EndAddr - ms.CurAddr);

            //memcpy(chip.data + ms.CurAddr, ms.MemPnt + (ms.CurAddr - ms.BaseAddr), BytCnt);
            Array.Copy(ms.MemPnt, (ms.CurAddr - ms.BaseAddr), chip.data, ms.CurAddr, BytCnt);
            ms.CurAddr += BytCnt;

            return;
        }

        public void rf5c68_write_ram2(byte ChipID, int DataStart, int DataLength, byte[] RAMData, uint SrcStartAdr)
        {

            rf5c68_state chip = RF5C68Data[ChipID];
            mem_stream ms = chip.memstrm;
            UInt16 BytCnt;

            DataStart |= chip.wbank * 0x1000;
            if (DataStart >= chip.datasize)
                return;
            if (DataStart + DataLength > chip.datasize)
                DataLength = (int)(chip.datasize - DataStart);

            //memcpy(chip.data + DataStart, RAMData, DataLength);

            rf5c68_mem_stream_flush(chip);

            ms.BaseAddr = (uint)DataStart;
            ms.CurAddr = ms.BaseAddr;
            ms.EndAddr = (uint)(ms.BaseAddr + DataLength);
            ms.CurStep = 0x0000;
            byte[] dat = new byte[DataLength];
            for(int ind = 0; ind < DataLength; ind++)
            {
                dat[ind] = RAMData[SrcStartAdr + ind];
            }
            ms.MemPnt = dat;

            //BytCnt = (STEAM_STEP * 32) >> 11;
            BytCnt = 0x40;  // SegaSonic Arcade: Run! Run! Run! needs such a high value
            if (ms.CurAddr + BytCnt > ms.EndAddr)
                BytCnt = (UInt16)(ms.EndAddr - ms.CurAddr);

            //memcpy(chip.data + ms.CurAddr, ms.MemPnt + (ms.CurAddr - ms.BaseAddr), BytCnt);
            Array.Copy(ms.MemPnt, (ms.CurAddr - ms.BaseAddr), chip.data, ms.CurAddr, BytCnt);
            ms.CurAddr += BytCnt;

            return;
        }


        private void rf5c68_set_mute_mask(byte ChipID, UInt32 MuteMask)
        {
            rf5c68_state chip = RF5C68Data[ChipID];
            byte CurChn;

            for (CurChn = 0; CurChn < NUM_CHANNELS; CurChn++)
                chip.chan[CurChn].Muted = (byte)((MuteMask >> CurChn) & 0x01);

            return;
        }



        /**************************************************************************
         * Generic get_info
         **************************************************************************/

        /*DEVICE_GET_INFO( rf5c68 )
        {
            switch (state)
            {
                // --- the following bits of info are returned as 64-bit signed integers ---
                case DEVINFO_INT_TOKEN_BYTES:					info->i = sizeof(rf5c68_state);				break;

                // --- the following bits of info are returned as pointers to data or functions ---
                case DEVINFO_FCT_START:							info->start = DEVICE_START_NAME( rf5c68 );			break;
                case DEVINFO_FCT_STOP:							// Nothing										break;
                case DEVINFO_FCT_RESET:							// Nothing										break;

                // --- the following bits of info are returned as NULL-terminated strings ---
                case DEVINFO_STR_NAME:							strcpy(info->s, "RF5C68");						break;
                case DEVINFO_STR_FAMILY:					strcpy(info->s, "Ricoh PCM");					break;
                case DEVINFO_STR_VERSION:					strcpy(info->s, "1.0");							break;
                case DEVINFO_STR_SOURCE_FILE:						strcpy(info->s, __FILE__);						break;
                case DEVINFO_STR_CREDITS:					strcpy(info->s, "Copyright Nicola Salmoria and the MAME Team"); break;
            }
        }*/

        /**************** end of file ****************/

    }
}
