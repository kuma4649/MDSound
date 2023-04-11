using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDSound
{
    public class K053260 : Instrument
    {
        public override string Name { get { return "K053260"; } set { } }
        public override string ShortName { get { return "K053"; } set { } }

        public override void Reset(byte ChipID)
        {
            device_reset_k053260(ChipID);

            visVolume = new int[2][][] {
                new int[1][] { new int[2] { 0, 0 } }
                , new int[1][] { new int[2] { 0, 0 } }
            };
        }

        public override UInt32 Start(byte ChipID, UInt32 clock)
        {
            return (UInt32)device_start_k053260(ChipID, (Int32)3579545);
        }

        public override UInt32 Start(byte ChipID, UInt32 clock, UInt32 ClockValue, params object[] option)
        {
            return (UInt32)device_start_k053260(ChipID, (Int32)ClockValue);
        }

        public override void Stop(byte ChipID)
        {
            device_stop_k053260(ChipID);
        }

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {
            k053260_update(ChipID, outputs, samples);

            visVolume[ChipID][0][0] = outputs[0][0];
            visVolume[ChipID][0][1] = outputs[1][0];
        }

        public override int Write(byte ChipID, int port, int adr, int data)
        {
            k053260_w(ChipID, adr, (byte)data);
            return 0;
        }




        /*********************************************************

            Konami 053260 PCM/ADPCM Sound Chip

        *********************************************************/

        //#pragma once

        //#include "devlegcy.h"

        /*typedef struct _k053260_interface k053260_interface;
        struct _k053260_interface {
            const char *rgnoverride;
            timer_expired_func irq;			// called on SH1 complete cycle ( clock / 32 ) //
        };*/


        //public void k053260_update(byte ChipID, Int32[][] outputs, Int32 Samples) { }
        //public Int32 device_start_k053260(byte ChipID, Int32 clock) { return 0; }
        //public void device_stop_k053260(byte ChipID) { }
        //public void device_reset_k053260(byte ChipID) { }

        //WRITE8_DEVICE_HANDLER( k053260_w );
        //READ8_DEVICE_HANDLER( k053260_r );
        //public void k053260_w(byte ChipID, Int32 offset, byte data) { }
        //public byte k053260_r(byte ChipID, Int32 offset) { return 0; }

        //public void k053260_write_rom(byte ChipID, Int32 ROMSize, Int32 DataStart, Int32 DataLength, byte[] ROMData) { }
        //public void k053260_set_mute_mask(byte ChipID, UInt32 MuteMask) { }

        //DECLARE_LEGACY_SOUND_DEVICE(K053260, k053260);





        /*********************************************************

            Konami 053260 PCM Sound Chip

        *********************************************************/

        //# include "mamedef.h"
        //#include "emu.h"
        //# ifdef _DEBUG
        //# include <stdio.h>
        //#endif
        //# include <stdlib.h>
        //# include <string.h>	// for memset
        //# include <stddef.h>	// for NULL
        //# include "k053260.h"

        /* 2004-02-28: Fixed PPCM decoding. Games sound much better now.*/

        //#define LOG 0

        private const Int32 BASE_SHIFT = 16;

        public class k053260_channel
        {
            public UInt32 rate;
            public UInt32 size;
            public UInt32 start;
            public UInt32 bank;
            public UInt32 volume;
            public Int32 play;
            public Int32 dir;
            public UInt32 pan;
            public UInt32 pos;
            public Int32 loop;
            public Int32 ppcm; /* packed PCM ( 4 bit signed ) */
            public Int32 ppcm_data;
            public byte Muted;
        };

        public class k053260_state
        {
            //sound_stream *				channel;
            public Int32 mode;
            public Int32[] regs = new Int32[0x30];
            public byte[] rom;
            //int							rom_size;
            public UInt32 rom_size;
            public UInt32[] delta_table;
            public k053260_channel[] channels = new k053260_channel[4] { new k053260_channel(), new k053260_channel(), new k053260_channel(), new k053260_channel() };
            //const k053260_interface		*intf;
            //device_t				*device;
        };

        private const Int32 MAX_CHIPS = 0x02;
        private k053260_state[] K053260Data = new k053260_state[2] { new k053260_state(), new k053260_state() };//[MAX_CHIPS];

        /*INLINE k053260_state *get_safe_token(device_t *device)
        {
            assert(device != NULL);
            assert(device->type() == K053260);
            return (k053260_state *)downcast<legacy_device_base *>(device)->token();
        }*/


        private void InitDeltaTable(k053260_state ic, Int32 rate, Int32 clock)
        {
            Int32 i;
            double _base = (double)rate;
            double max = (double)(clock); /* Hz */
            UInt32 val;

            for (i = 0; i < 0x1000; i++)
            {
                double v = (double)(0x1000 - i);
                double target = (max) / v;
                double _fixed = (double)(1 << BASE_SHIFT);

                if (target != 0 && _base != 0)
                {
                    target = _fixed / (_base / target);
                    val = (UInt32)target;
                    if (val == 0)
                        val = 1;
                }
                else
                    val = 1;

                ic.delta_table[i] = val;
            }
        }

        //static DEVICE_RESET( k053260 )
        public void device_reset_k053260(byte ChipID)
        {
            //k053260_state *ic = get_safe_token(device);
            k053260_state ic = K053260Data[ChipID];
            Int32 i;

            for (i = 0; i < 4; i++)
            {
                ic.channels[i].rate = 0;
                ic.channels[i].size = 0;
                ic.channels[i].start = 0;
                ic.channels[i].bank = 0;
                ic.channels[i].volume = 0;
                ic.channels[i].play = 0;
                ic.channels[i].dir = 0;
                ic.channels[i].pan = 0;
                ic.channels[i].pos = 0;
                ic.channels[i].loop = 0;
                ic.channels[i].ppcm = 0;
                ic.channels[i].ppcm_data = 0;
            }
        }

        private Int32 limit(Int32 val, Int32 max, Int32 min)
        {
            if (val > max)
                val = max;
            else if (val < min)
                val = min;

            return val;
        }

        //#define MAXOUT 0x7fff
        private const Int32 MAXOUT = +0x8000;
        private const Int32 MINOUT = -0x8000;

        private sbyte[] dpcmcnv = new sbyte[] { 0, 1, 2, 4, 8, 16, 32, 64, -128, -64, -32, -16, -8, -4, -2, -1 };
        private Int32[] lvol = new Int32[4], rvol = new Int32[4], play = new Int32[4], dir = new Int32[4], loop = new Int32[4], ppcm = new Int32[4];
        //byte[] rom = new byte[4];
        private UInt32[] ptrRom = new UInt32[4];
        private UInt32[] delta = new UInt32[4], end = new UInt32[4], pos = new UInt32[4];
        private sbyte[] ppcm_data = new sbyte[4];

        //static STREAM_UPDATE( k053260_update )
        public void k053260_update(byte ChipID, Int32[][] outputs, Int32 samples)
        {

            //sbyte[] dpcmcnv = new sbyte[] { 0, 1, 2, 4, 8, 16, 32, 64, -128, -64, -32, -16, -8, -4, -2, -1 };
            Int32 i, j;
            //Int32[] lvol = new Int32[4], rvol = new Int32[4], play = new Int32[4], loop = new Int32[4], ppcm = new Int32[4];
            //byte[] rom = new byte[4];
            //UInt32[] ptrRom = new UInt32[4];
            //UInt32[] delta = new UInt32[4], end = new UInt32[4], pos = new UInt32[4];
            //sbyte[] ppcm_data = new sbyte[4];
            Int32 dataL, dataR;
            sbyte d;
            //k053260_state *ic = (k053260_state *)param;
            k053260_state ic = K053260Data[ChipID];

            /* precache some values */
            for (i = 0; i < 4; i++)
            {
                if (ic.channels[i].Muted != 0)
                {
                    play[i] = 0;
                    continue;
                }
                //rom[i] = ic.rom[ic.channels[i].start + (ic.channels[i].bank << 16)];
                ptrRom[i] = ic.channels[i].start + (ic.channels[i].bank << 16);
                delta[i] = ic.delta_table[ic.channels[i].rate];
                lvol[i] = (Int32)(ic.channels[i].volume * ic.channels[i].pan);
                rvol[i] = (Int32)(ic.channels[i].volume * (8 - ic.channels[i].pan));
                end[i] = ic.channels[i].size;
                pos[i] = ic.channels[i].pos;
                play[i] = ic.channels[i].play;
                dir[i] = ic.channels[i].dir;
                loop[i] = ic.channels[i].loop;
                ppcm[i] = ic.channels[i].ppcm;
                ppcm_data[i] = (sbyte)ic.channels[i].ppcm_data;
                if (ppcm[i] != 0)
                    delta[i] /= 2;
            }

            for (j = 0; j < samples; j++)
            {

                dataL = dataR = 0;

                for (i = 0; i < 4; i++)
                {
                    /* see if the voice is on */
                    if (play[i] != 0)
                    {
                        /* see if we're done */
                        if ((pos[i] >> BASE_SHIFT) >= end[i])
                        {

                            ppcm_data[i] = 0;
                            if (loop[i] != 0)
                                pos[i] = 0;
                            else
                            {
                                play[i] = 0;
                                continue;
                            }
                        }
                        UInt32 position = pos[i] >> BASE_SHIFT;

                        if (ppcm[i] != 0)
                        { /* Packed PCM */
                          /* we only update the signal if we're starting or a real sound sample has gone by */
                          /* this is all due to the dynamic sample rate conversion */
                            if (pos[i] == 0 || ((pos[i] ^ (pos[i] - delta[i])) & 0x8000) == 0x8000)

                            {
                                Int32 newdata;
                                if ((pos[i] & 0x8000) != 0)
                                {

                                    //newdata = ((rom[i][pos[i] >> BASE_SHIFT]) >> 4) & 0x0f; /*high nybble*/
                                    newdata = ((ic.rom[ptrRom[i] + ((dir[i] > 0) ? -position : position)]) >> 4) & 0x0f; /*high nybble*/
                                }
                                else
                                {
                                    //newdata = ((rom[i][pos[i] >> BASE_SHIFT])) & 0x0f; /*low nybble*/
                                    newdata = ((ic.rom[ptrRom[i] + ((dir[i] > 0) ? -position : position)])) & 0x0f; /*low nybble*/
                                }

                                /*ppcm_data[i] = (( ( ppcm_data[i] * 62 ) >> 6 ) + dpcmcnv[newdata]);

                                if ( ppcm_data[i] > 127 )
                                    ppcm_data[i] = 127;
                                else
                                    if ( ppcm_data[i] < -128 )
                                        ppcm_data[i] = -128;*/
                                ppcm_data[i] += dpcmcnv[newdata];
                            }



                            d = ppcm_data[i];

                            pos[i] += delta[i];
                        }
                        else
                        { /* PCM */
                            //d = rom[i][pos[i] >> BASE_SHIFT];
                            d = (sbyte)ic.rom[ptrRom[i] + ((dir[i] > 0) ? -position : position)];

                            pos[i] += delta[i];
                        }

                        if ((ic.mode & 2) != 0)
                        {
                            dataL += (d * lvol[i]) >> 2;
                            dataR += (d * rvol[i]) >> 2;
                        }
                    }
                }

                outputs[1][j] = limit(dataL, MAXOUT, MINOUT);
                outputs[0][j] = limit(dataR, MAXOUT, MINOUT);
            }

            /* update the regs now */
            for (i = 0; i < 4; i++)
            {
                if (ic.channels[i].Muted != 0)
                    continue;
                ic.channels[i].pos = pos[i];
                ic.channels[i].play = play[i];
                ic.channels[i].ppcm_data = ppcm_data[i];
            }
        }

        //static DEVICE_START( k053260 )
        public Int32 device_start_k053260(byte ChipID, Int32 clock)
        {
            //static const k053260_interface defintrf = { 0 };
            //k053260_state *ic = get_safe_token(device);
            k053260_state ic;
            //Int32 rate = device->clock() / 32;
            Int32 rate = clock / 32;
            Int32 i;

            if (ChipID >= MAX_CHIPS)
                return 0;

            ic = K053260Data[ChipID];

            /* Initialize our chip structure */
            //ic.device = device;
            //ic.intf = (device->static_config() != NULL) ? (const k053260_interface *)device->static_config() : &defintrf;

            ic.mode = 0;

            //const memory_region *region = (ic.intf->rgnoverride != NULL) ? device->machine().region(ic.intf->rgnoverride) : device->region();

            //ic.rom = *region;
            //ic.rom_size = region->bytes();
            ic.rom = null;
            ic.rom_size = 0x00;

            // has to be done by the player after calling device_start
            //DEVICE_RESET_CALL(k053260);

            for (i = 0; i < 0x30; i++)
                ic.regs[i] = 0;

            //ic.delta_table = auto_alloc_array( device->machine(), UInt32, 0x1000 );
            ic.delta_table = new UInt32[0x1000];// (UInt32*)malloc(0x1000 * sizeof(UInt32));

            //ic.channel = device->machine().sound().stream_alloc( *device, 0, 2, rate, ic, k053260_update );

            //InitDeltaTable( ic, rate, device->clock() );
            InitDeltaTable(ic, rate, clock);

            /* register with the save state system */
            /*device->save_item(NAME(ic.mode));
            device->save_item(NAME(ic.regs));

            for ( i = 0; i < 4; i++ )
            {
                device->save_item(NAME(ic.channels[i].rate), i);
                device->save_item(NAME(ic.channels[i].size), i);
                device->save_item(NAME(ic.channels[i].start), i);
                device->save_item(NAME(ic.channels[i].bank), i);
                device->save_item(NAME(ic.channels[i].volume), i);
                device->save_item(NAME(ic.channels[i].play), i);
                device->save_item(NAME(ic.channels[i].pan), i);
                device->save_item(NAME(ic.channels[i].pos), i);
                device->save_item(NAME(ic.channels[i].loop), i);
                device->save_item(NAME(ic.channels[i].ppcm), i);
                device->save_item(NAME(ic.channels[i].ppcm_data), i);
            }*/

            /* setup SH1 timer if necessary */
            //if ( ic.intf->irq )
            //	device->machine().scheduler().timer_pulse( attotime::from_hz(device->clock()) * 32, ic.intf->irq, "ic.intf->irq" );

            for (i = 0; i < 4; i++)
                ic.channels[i].Muted = 0x00;

            return rate;
        }

        public void device_stop_k053260(byte ChipID)
        {
            k053260_state ic = K053260Data[ChipID];

            //free(ic.delta_table);
            //free(ic.rom);
            ic.rom = null;

            return;
        }

        private void check_bounds(k053260_state ic, Int32 channel)
        {

            Int32 channel_start = (Int32)((ic.channels[channel].bank << 16) + ic.channels[channel].start);
            Int32 channel_end = (Int32)(channel_start + ic.channels[channel].size - 1);

            if (channel_start > ic.rom_size)
            {
                //logerror("K53260: Attempting to start playing past the end of the ROM ( start = %06x, end = %06x ).\n", channel_start, channel_end);

                ic.channels[channel].play = 0;

                return;
            }

            if (channel_end > ic.rom_size)
            {
                //logerror("K53260: Attempting to play past the end of the ROM ( start = %06x, end = %06x ).\n", channel_start, channel_end);

                ic.channels[channel].size = (UInt32)(ic.rom_size - channel_start);
            }
            //if (LOG) logerror("K053260: Sample Start = %06x, Sample End = %06x, Sample rate = %04x, PPCM = %s\n", channel_start, channel_end, ic.channels[channel].rate, ic.channels[channel].ppcm ? "yes" : "no");
        }

        //WRITE8_DEVICE_HANDLER( k053260_w )
        public void k053260_w(byte ChipID, Int32 offset, byte data)
        {
            Int32 i, t;
            Int32 r = offset;
            Int32 v = data;

            //k053260_state *ic = get_safe_token(device);
            k053260_state ic = K053260Data[ChipID];

            if (r > 0x2f)
            {
                //logerror("K053260: Writing past registers\n");
                return;
            }

            //ic.channel->update();

            /* before we update the regs, we need to check for a latched reg */
            if (r == 0x28)
            {
                t = ic.regs[r] ^ v;

                for (i = 0; i < 4; i++)
                {
                    ic.channels[i].dir = (v & (16 << i)) != 0 ? 1 : 0;
                    if ((t & (1 << i)) != 0)
                    {
                        if ((v & (1 << i)) != 0)
                        {
                            ic.channels[i].play = 1;
                            ic.channels[i].pos = 0;
                            ic.channels[i].ppcm_data = 0;
                            check_bounds(ic, i);
                        }
                        else
                            ic.channels[i].play = 0;
                    }
                }

                ic.regs[r] = v;
                return;
            }

            /* update regs */
            ic.regs[r] = v;

            /* communication registers */
            if (r < 8)
                return;

            /* channel setup */
            if (r < 0x28)
            {
                Int32 channel = (r - 8) / 8;

                switch ((r - 8) & 0x07)
                {
                    case 0: /* sample rate low */
                        ic.channels[channel].rate &= 0x0f00;
                        ic.channels[channel].rate |= (UInt32)v;
                        break;

                    case 1: /* sample rate high */
                        ic.channels[channel].rate &= 0x00ff;
                        ic.channels[channel].rate |= (UInt32)((v & 0x0f) << 8);
                        break;

                    case 2: /* size low */
                        ic.channels[channel].size &= 0xff00;
                        ic.channels[channel].size |= (UInt32)v;
                        break;

                    case 3: /* size high */
                        ic.channels[channel].size &= 0x00ff;
                        ic.channels[channel].size |= (UInt32)(v << 8);
                        break;

                    case 4: /* start low */
                        ic.channels[channel].start &= 0xff00;
                        ic.channels[channel].start |= (UInt32)v;
                        break;

                    case 5: /* start high */
                        ic.channels[channel].start &= 0x00ff;
                        ic.channels[channel].start |= (UInt32)(v << 8);
                        break;

                    case 6: /* bank */
                        ic.channels[channel].bank = (UInt32)(v & 0xff);
                        break;

                    case 7: /* volume is 7 bits. Convert to 8 bits now. */
                        ic.channels[channel].volume = (UInt32)(((v & 0x7f) << 1) | (v & 1));
                        break;
                }

                return;
            }

            switch (r)
            {
                case 0x2a: /* loop, ppcm */
                    for (i = 0; i < 4; i++)
                        ic.channels[i].loop = (v & (1 << i)) != 0 ? 1 : 0;

                    for (i = 4; i < 8; i++)
                        ic.channels[i - 4].ppcm = (v & (1 << i)) != 0 ? 1 : 0;
                    break;

                case 0x2c: /* pan */
                    ic.channels[0].pan = (UInt32)(v & 7);
                    ic.channels[1].pan = (UInt32)((v >> 3) & 7);
                    break;

                case 0x2d: /* more pan */
                    ic.channels[2].pan = (UInt32)(v & 7);
                    ic.channels[3].pan = (UInt32)((v >> 3) & 7);
                    break;

                case 0x2f: /* control */
                    ic.mode = v & 7;
                    /* bit 0 = read ROM */
                    /* bit 1 = enable sound output */
                    /* bit 2 = unknown */
                    break;
            }
        }

        //READ8_DEVICE_HANDLER( k053260_r )
        public byte k053260_r(byte ChipID, Int32 offset)
        {
            //k053260_state *ic = get_safe_token(device);
            k053260_state ic = K053260Data[ChipID];

            switch (offset)
            {
                case 0x29: /* channel status */
                    {
                        Int32 i, status = 0;

                        for (i = 0; i < 4; i++)
                            status |= ic.channels[i].play << i;

                        return (byte)status;
                    }
                    //break;

                case 0x2e: /* read ROM */
                    if ((ic.mode & 1) != 0)
                    {
                        UInt32 offs = ic.channels[0].start + (ic.channels[0].pos >> BASE_SHIFT) + (ic.channels[0].bank << 16);

                        ic.channels[0].pos += (1 << 16);

                        if (offs > ic.rom_size)
                        {
                            //logerror("%s: K53260: Attempting to read past ROM size in ROM Read Mode (offs = %06x, size = %06x).\n", device->machine().describe_context(),offs,ic.rom_size );
                            //logerror("K53260: Attempting to read past ROM size in ROM Read Mode (offs = %06x, size = %06x).\n", offs, ic.rom_size);

                            return 0;
                        }

                        return ic.rom[offs];
                    }
                    break;
            }

            return (byte)ic.regs[offset];
        }

        public void k053260_write_rom(byte ChipID, Int32 ROMSize, Int32 DataStart, Int32 DataLength, byte[] ROMData)
        {
            k053260_state info = K053260Data[ChipID];

            if (info.rom_size != ROMSize)
            {
                info.rom = new byte[ROMSize];// (byte*)realloc(info.rom, ROMSize);
                info.rom_size = (UInt32)ROMSize;

                for (Int32 i = 0; i < ROMSize; i++) info.rom[i] = 0xff;
                //memset(info.rom, 0xFF, ROMSize);
            }

            if (DataStart > ROMSize)
                return;
            if (DataStart + DataLength > ROMSize)
                DataLength = ROMSize - DataStart;

            for (Int32 i = 0; i < DataLength; i++) info.rom[i + DataStart] = ROMData[i];
            //memcpy(info.rom + DataStart, ROMData, DataLength);

            return;
        }

        public void k053260_write_rom(byte ChipID, Int32 ROMSize, Int32 DataStart, Int32 DataLength, byte[] ROMData, Int32 SrcStartAdr)
        {
            k053260_state info = K053260Data[ChipID];

            if (info.rom_size != ROMSize)
            {
                info.rom = new byte[ROMSize];// (byte*)realloc(info.rom, ROMSize);
                info.rom_size = (UInt32)ROMSize;

                for (Int32 i = 0; i < ROMSize; i++) info.rom[i] = 0xff;
                //memset(info.rom, 0xFF, ROMSize);
            }

            if (DataStart > ROMSize)
                return;
            if (DataStart + DataLength > ROMSize)
                DataLength = ROMSize - DataStart;

            for (Int32 i = 0; i < DataLength; i++) info.rom[i + DataStart] = ROMData[i+SrcStartAdr];
            //memcpy(info.rom + DataStart, ROMData, DataLength);

            return;
        }


        public void k053260_set_mute_mask(byte ChipID, UInt32 MuteMask)
        {
            k053260_state info = K053260Data[ChipID];
            byte CurChn;

            for (CurChn = 0; CurChn < 4; CurChn++)
                info.channels[CurChn].Muted = (byte)((MuteMask >> CurChn) & 0x01);

            return;
        }

        /**************************************************************************
         * Generic get_info
         **************************************************************************/

        /*DEVICE_GET_INFO( k053260 )
        {
            switch (state)
            {
                // --- the following bits of info are returned as 64-bit signed integers --- //
                case DEVINFO_INT_TOKEN_BYTES:					info.i = sizeof(k053260_state);				break;

                // --- the following bits of info are returned as pointers to data or functions --- //
                case DEVINFO_FCT_START:							info.start = DEVICE_START_NAME( k053260 );		break;
                case DEVINFO_FCT_STOP:							// nothing //									break;
                case DEVINFO_FCT_RESET:							info.reset = DEVICE_RESET_NAME( k053260 );		break;

                // --- the following bits of info are returned as NULL-terminated strings --- //
                case DEVINFO_STR_NAME:							strcpy(info.s, "K053260");						break;
                case DEVINFO_STR_FAMILY:						strcpy(info.s, "Konami custom");				break;
                case DEVINFO_STR_VERSION:						strcpy(info.s, "1.0");							break;
                case DEVINFO_STR_SOURCE_FILE:					strcpy(info.s, __FILE__);						break;
                case DEVINFO_STR_CREDITS:						strcpy(info.s, "Copyright Nicola Salmoria and the MAME Team"); break;
            }
        }


        DEFINE_LEGACY_SOUND_DEVICE(K053260, k053260);*/

    }
}
