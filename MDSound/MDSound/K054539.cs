using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDSound
{
    public class K054539 : Instrument
    {

        public K054539()
        {
            visVolume = new int[2][][] {
                new int[1][] { new int[2] { 0, 0 } }
                , new int[1][] { new int[2] { 0, 0 } }
            };
            //0..Main
        }

        public override void Reset(byte ChipID)
        {
            device_reset_k054539(ChipID);
        }

        public override uint Start(byte ChipID, uint clock)
        {
            return (uint)device_start_k054539(ChipID, (int)clock);
        }

        public override void Stop(byte ChipID)
        {
            device_stop_k054539(ChipID);
        }

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {
            k054539_update(ChipID, outputs, samples);

            visVolume[ChipID][0][0] = outputs[0][0];
            visVolume[ChipID][0][1] = outputs[1][0];
        }

        public override uint Start(byte ChipID, uint SamplingRate, uint clock, params object[] Option)
        {
            uint sampRate= (uint)device_start_k054539(ChipID, (int)clock);
            int flags = 1;
            if (Option != null && Option.Length > 0) flags = (int)(byte)Option[0];
            k054539_init_flags(ChipID, flags);

            return sampRate;
        }


        private const int K054539_RESET_FLAGS = 0;
        private const int K054539_REVERSE_STEREO = 1;
        private const int K054539_DISABLE_REVERB = 2;
        private const int K054539_UPDATE_AT_KEYON = 4;

        /*********************************************************

            Konami 054539 (TOP) PCM Sound Chip

            A lot of information comes from Amuse.
            Big thanks to them.

        *********************************************************/

        //#include "emu.h"
        //# include <stdlib.h>
        //# include <string.h>	// for memset
        //# include <stddef.h>	// for NULL
        //# include <math.h>
        //# include "mamedef.h"
        //# ifdef _DEBUG
        //# include <stdio.h>
        //#endif
        //# include "k054539.h"

        //#define VERBOSE 0
        //#define LOG(x) do { if (VERBOSE) logerror x; } while (0)

        /* Registers:
           00..ff: 20 bytes/channel, 8 channels
             00..02: pitch (lsb, mid, msb)
                 03: volume (0=max, 0x40=-36dB)
                 04: reverb volume (idem)
             05: pan (1-f right, 10 middle, 11-1f left)
             06..07: reverb delay (0=max, current computation non-trusted)
             08..0a: loop (lsb, mid, msb)
             0c..0e: start (lsb, mid, msb) (and current position ?)

           100.1ff: effects?
             13f: pan of the analog input (1-1f)

           200..20f: 2 bytes/channel, 8 channels
             00: type (b2-3), reverse (b5)
             01: loop (b0)

           214: Key on (b0-7 = channel 0-7)
           215: Key off          ""
           225: ?
           227: Timer frequency
           228: ?
           229: ?
           22a: ?
           22b: ?
           22c: Channel active? (b0-7 = channel 0-7)
           22d: Data read/write port
           22e: ROM/RAM select (00..7f == ROM banks, 80 = Reverb RAM)
           22f: Global control:
                .......x - Enable PCM
                ......x. - Timer related?
                ...x.... - Enable ROM/RAM readback from 0x22d
                ..x..... - Timer output enable?
                x....... - Disable register RAM updates

            The chip has an optional 0x8000 byte reverb buffer.
            The reverb delay is actually an offset in this buffer.
        */

        public class k054539_channel
        {
            public uint pos;
            public uint pfrac;
            public int val;
            public int pval;
        };

        public class k054539_state
        {
            //const k054539_interface *intf;
            //device_t *device;
            public double[] voltab = new double[256];
            public double[] pantab = new double[0xf];

            public double[] k054539_gain = new double[8];
            public byte[][] k054539_posreg_latch = new byte[8][] {
                new byte[3], new byte[3], new byte[3], new byte[3],
                new byte[3],new byte[3],new byte[3],new byte[3] };
            public int k054539_flags;

            public byte[] regs = new byte[0x230];
            public byte[] ram;
            public int ptrRam=0;
            public int reverb_pos;

            public int cur_ptr;
            public int cur_limit;
            public byte[] cur_zone;
            public int ptrCur_zone;
            public byte[] rom;
            public int ptrRom=0;

            public uint rom_size;
            public uint rom_mask;
            //sound_stream * stream;

            public k054539_channel[] channels = new k054539_channel[8] {
                new k054539_channel(),
                new k054539_channel(),
                new k054539_channel(),
                new k054539_channel(),
                new k054539_channel(),
                new k054539_channel(),
                new k054539_channel(),
                new k054539_channel()
            };
            public byte[] Muted = new byte[8];

            public int clock;
        };

        private const int MAX_CHIPS = 0x02;
        public k054539_state[] K054539Data = new k054539_state[MAX_CHIPS] { new k054539_state(), new k054539_state() };

        public override string Name { get { return "K054539"; } set { } }
        public override string ShortName { get { return "K054"; } set { } }

        /*INLINE k054539_state *get_safe_token(device_t *device)
        {
            assert(device != NULL);
            assert(device->type() == K054539);
            return (k054539_state *)downcast<legacy_device_base *>(device)->token();
        }*/

        //*

        //void k054539_init_flags(device_t *device, int flags)
        public void k054539_init_flags(byte ChipID, int flags)
        {
            //k054539_state *info = get_safe_token(device);
            k054539_state info = K054539Data[ChipID];
            info.k054539_flags = flags;
        }

        //void k054539_set_gain(device_t *device, int channel, double gain)
        public void k054539_set_gain(byte ChipID, int channel, double gain)
        {
            //k054539_state *info = get_safe_token(device);
            k054539_state info = K054539Data[ChipID];
            if (gain >= 0) info.k054539_gain[channel] = gain;
        }
        //*

        private static int k054539_regupdate(k054539_state info)
        {
            return (info.regs[0x22f] & 0x80);
        }

        private static void k054539_keyon(k054539_state info, int channel)
        {
            if (k054539_regupdate(info) == 0)
                info.regs[0x22c] |= (byte)(1 << channel);
        }

        private static void k054539_keyoff(k054539_state info, int channel)
        {
            if (k054539_regupdate(info) == 0)
                info.regs[0x22c] &= (byte)(~(1 << channel));
        }

        //static STREAM_UPDATE( k054539_update )
        private void k054539_update(byte ChipID, int[][] outputs, int samples)
        {
            //k054539_state *info = (k054539_state *)param;
            k054539_state info = K054539Data[ChipID];
            const double VOL_CAP = 1.80;

            short[] dpcm = new short[16]{
                0<<8, 1<<8, 2<<8, 4<<8, 8<<8, 16<<8, 32<<8, 64<<8,
                0<<8,-64<<8, -32<<8, -16<<8, -8<<8, -4<<8, -2<<8, -1<<8
            };

            byte[] rbase = info.ram;//caution original INT16* 
            byte[] rom;
            uint rom_mask;
            int i, ch;
            double lval, rval;
            byte[] base1, base2;
            int ptrBase1, ptrBase2;
            k054539_channel[] chan;
            int ptrChan;
            int delta, vol, bval, pan;
            double cur_gain, lvol, rvol, rbvol;
            int rdelta;
            uint cur_pos;
            int fdelta, pdelta;
            int cur_pfrac, cur_val, cur_pval;

            for (i = 0; i < samples; i++)
            {
                outputs[0][i] = 0;
                outputs[1][i] = 0;
            }

            if ((info.regs[0x22f] & 1) == 0) //Enable PCM
                return;

            rom = info.rom;
            rom_mask = info.rom_mask;

            for (i = 0; i != samples; i++)
            {
                //リバーブ
                if ((info.k054539_flags & K054539_DISABLE_REVERB) == 0)
                {
                    //lval = rval = rbase[info.reverb_pos];
                    short val = (short)(rbase[info.reverb_pos * 2] + rbase[info.reverb_pos * 2 + 1] * 0x100);
                    lval = rval = val; 
                }
                else
                    lval = rval = 0;
                //rbase[info.reverb_pos] = 0;
                //Console.Write("rbase[info->reverb_pos({0})] = {1} \n", info.reverb_pos, lval);
                rbase[info.reverb_pos * 2] = 0;
                rbase[info.reverb_pos * 2 + 1] = 0;


                for (ch = 0; ch < 8; ch++)
                    if (((info.regs[0x22c] & (1 << ch)) != 0) && info.Muted[ch] == 0)//0x22c ChannelActive
                    {
                        base1 = info.regs;
                        ptrBase1 = 0x20 * ch;
                        base2 = info.regs;
                        ptrBase2 = 0x200 + 0x2 * ch;
                        chan = info.channels;
                        ptrChan = ch;

                        //pitch
                        delta = base1[ptrBase1 + 0x00] | (base1[ptrBase1 + 0x01] << 8) | (base1[ptrBase1 + 0x02] << 16);

                        vol = base1[ptrBase1 + 0x03];

                        //0x04 reverb vol
                        bval = vol + base1[ptrBase1 + 0x04];
                        if (bval > 255)
                            bval = 255;

                        pan = base1[ptrBase1 + 0x05];
                        // DJ Main: 81-87 right, 88 middle, 89-8f left
                        if (pan >= 0x81 && pan <= 0x8f)
                            pan -= 0x81;
                        else if (pan >= 0x11 && pan <= 0x1f)
                            pan -= 0x11;
                        else
                            pan = 0x18 - 0x11;

                        cur_gain = info.k054539_gain[ch];

                        lvol = info.voltab[vol] * info.pantab[pan] * cur_gain;
                        if (lvol > VOL_CAP)
                            lvol = VOL_CAP;

                        rvol = info.voltab[vol] * info.pantab[0xe - pan] * cur_gain;
                        if (rvol > VOL_CAP)
                            rvol = VOL_CAP;

                        rbvol = info.voltab[bval] * cur_gain / 2;
                        if (rbvol > VOL_CAP)
                            rbvol = VOL_CAP;

                        //Console.Write("ch={0} lvol={1} rvol={2}\n", ch, lvol, rvol);

                        rdelta = (base1[ptrBase1 + 6] | (base1[ptrBase1 + 7] << 8)) >> 3;
                        rdelta = (rdelta + info.reverb_pos) & 0x3fff;

                        cur_pos = (uint)((base1[ptrBase1 + 0x0c] | (base1[ptrBase1 + 0x0d] << 8) | (base1[ptrBase1 + 0x0e] << 16)) & rom_mask);

                        if ((base2[ptrBase2 + 0] & 0x20) != 0)
                        {
                            delta = -delta;
                            fdelta = +0x10000;
                            pdelta = -1;
                        }
                        else
                        {
                            fdelta = -0x10000;
                            pdelta = +1;
                        }

                        if (cur_pos != chan[ptrChan].pos)
                        {
                            chan[ptrChan].pos = cur_pos;
                            cur_pfrac = 0;
                            cur_val = 0;
                            cur_pval = 0;
                        }
                        else
                        {
                            cur_pfrac = (int)chan[ptrChan].pfrac;
                            cur_val = chan[ptrChan].val;
                            cur_pval = chan[ptrChan].pval;
                        }

                        switch (base2[ptrBase2 + 0] & 0xc)
                        {
                            case 0x0:
                                { // 8bit pcm
                                    cur_pfrac += delta;
                                    while ((cur_pfrac & ~0xffff) != 0)
                                    {
                                        cur_pfrac += fdelta;
                                        cur_pos += (uint)pdelta;

                                        cur_pval = cur_val;
                                        if (rom.Length <= cur_pos)
                                        {
                                            cur_pos = (uint)(rom.Length - 1);
                                        }
                                        cur_val = (short)(rom[cur_pos] << 8);
                                        //if(cur_val == (INT16)0x8000 && (base2[1] & 1))
                                        if (rom[cur_pos] == 0x80 && (base2[ptrBase2 + 1] & 1) != 0)
                                        {
                                            cur_pos = (uint)((base1[ptrBase1 + 0x08] | (base1[ptrBase1 + 0x09] << 8) | (base1[ptrBase1 + 0x0a] << 16)) & rom_mask);
                                            cur_val = (short)(rom[cur_pos] << 8);
                                        }
                                        //if(cur_val == (INT16)0x8000) 
                                        if (rom[cur_pos] == 0x80)
                                            {
                                                k054539_keyoff(info, ch);
                                            cur_val = 0;
                                            break;
                                        }
                                    }
                                    //Console.Write("ch={0} cur_pos={1} cur_val={2}\n", ch, cur_pos, cur_val);
                                    //if(ch!=6) cur_val = 0;
                                    break;
                                }

                            case 0x4:
                                { // 16bit pcm lsb first
                                    pdelta <<= 1;

                                    cur_pfrac += delta;
                                    while ((cur_pfrac & ~0xffff) != 0)
                                    {
                                        cur_pfrac += fdelta;
                                        cur_pos += (uint)pdelta;

                                        cur_pval = cur_val;
                                        cur_val = (short)(rom[cur_pos] | rom[cur_pos + 1] << 8);
                                        if (cur_val == (short)(0x8000-0x10000) && (base2[ptrBase2 + 1] & 1) != 0)
                                        {
                                            cur_pos = (uint)((base1[ptrBase1 + 0x08] | (base1[ptrBase1 + 0x09] << 8) | (base1[ptrBase1 + 0x0a] << 16)) & rom_mask);
                                            cur_val = (short)(rom[cur_pos] | rom[cur_pos + 1] << 8);
                                        }
                                        if (cur_val == (short)(0x8000-0x10000))
                                        {
                                            k054539_keyoff(info, ch);
                                            cur_val = 0;
                                            break;
                                        }
                                    }
                                    //cur_val = 0;
                                    break;
                                }

                            case 0x8:
                                { // 4bit dpcm
                                    cur_pos <<= 1;
                                    cur_pfrac <<= 1;
                                    if ((cur_pfrac & 0x10000) != 0)
                                    {
                                        cur_pfrac &= 0xffff;
                                        cur_pos |= 1;
                                    }

                                    cur_pfrac += delta;
                                    while ((cur_pfrac & ~0xffff) != 0)
                                    {
                                        cur_pfrac += fdelta;
                                        cur_pos += (uint)pdelta;

                                        cur_pval = cur_val;
                                        cur_val = rom[cur_pos >> 1];
                                        if (cur_val == 0x88 && (base2[ptrBase2 + 1] & 1) != 0)
                                        {
                                            cur_pos = (uint)((base1[ptrBase1 + 0x08] | (base1[ptrBase1 + 0x09] << 8) | (base1[ptrBase1 + 0x0a] << 16)) & rom_mask) << 1;
                                            cur_val = rom[cur_pos >> 1];
                                        }
                                        if (cur_val == 0x88)
                                        {
                                            k054539_keyoff(info, ch);
                                            cur_val = 0;
                                            break;
                                        }
                                        if ((cur_pos & 1) != 0)
                                            cur_val >>= 4;
                                        else
                                            cur_val &= 15;
                                        cur_val = cur_pval + dpcm[cur_val];
                                        if (cur_val < -32768)
                                            cur_val = -32768;
                                        else if (cur_val > 32767)
                                            cur_val = 32767;
                                    }

                                    cur_pfrac >>= 1;
                                    if ((cur_pos & 1) != 0)
                                        cur_pfrac |= 0x8000;
                                    cur_pos >>= 1;
                                    //cur_val = 0;
                                    break;
                                }
                            default:
                                //LOG(("Unknown sample type %x for channel %d\n", base2[0] & 0xc, ch));
                                break;
                        }
                        lval += cur_val * lvol;
                        rval += cur_val * rvol;
                        //if (ch == 6)
                        //{
                        //    Console.Write("ch={0} lval={1}\n", ch, lval);
                        //}
                        int ptr = (rdelta + info.reverb_pos) & 0x1fff;
                        short valu = (short)(rbase[ptr * 2] + rbase[ptr * 2 + 1] * 0x100);
                        valu += (short)(cur_val * rbvol);
                        rbase[ptr * 2] = (byte)(valu & 0xff);
                        rbase[ptr * 2 + 1] = (byte)((valu & 0xff00) >> 8);

                        chan[ptrChan].pos = cur_pos;
                        chan[ptrChan].pfrac = (uint)cur_pfrac;
                        chan[ptrChan].pval = cur_pval;
                        chan[ptrChan].val = cur_val;

                        if (k054539_regupdate(info) == 0)
                        {
                            base1[ptrBase1 + 0x0c] = (byte)(cur_pos & 0xff);
                            base1[ptrBase1 + 0x0d] = (byte)((cur_pos >> 8) & 0xff);
                            base1[ptrBase1 + 0x0e] = (byte)((cur_pos >> 16) & 0xff);
                        }
                    }
                info.reverb_pos = (info.reverb_pos + 1) & 0x1fff;
                outputs[0][i] = (int)lval;
                outputs[1][i] = (int)rval;
                outputs[0][i] <<= 1;
                outputs[1][i] <<= 1;
                //Console.Write( "outputs[0][i] = {0}\n", outputs[0][i]);

            }
        }


        /*static TIMER_CALLBACK( k054539_irq )
        {
            k054539_state *info = (k054539_state *)ptr;
            if(info->regs[0x22f] & 0x20)
                info->intf->irq(info->device);
        }*/

        //static void k054539_init_chip(device_t *device, k054539_state *info)
        private static int k054539_init_chip(k054539_state info, int clock)
        {
            //int i;

            if (clock < 1000000)    // if < 1 MHz, then it's the sample rate, not the clock
                clock *= 384;   // (for backwards compatibility with old VGM logs)
            info.clock = clock;
            // most of these are done in device_reset
            //	memset(info->regs, 0, sizeof(info->regs));
            //	memset(info->k054539_posreg_latch, 0, sizeof(info->k054539_posreg_latch)); //*
            info.k054539_flags |= K054539_UPDATE_AT_KEYON; //* make it default until proven otherwise

            info.ram = new byte[0x4000];
            //	info->reverb_pos = 0;
            //	info->cur_ptr = 0;
            //	memset(info->ram, 0, 0x4000);

            /*const memory_region *region = (info->intf->rgnoverride != NULL) ? device->machine().region(info->intf->rgnoverride) : device->region();
            info->rom = *region;
            info->rom_size = region->bytes();
            info->rom_mask = 0xffffffffU;
            for(i=0; i<32; i++)
                if((1U<<i) >= info->rom_size) {
                    info->rom_mask = (1U<<i) - 1;
                    break;
                }*/
            info.rom = null;
            info.rom_size = 0;
            info.rom_mask = 0x00;

            //if(info->intf->irq)
            // One or more of the registers must be the timer period
            // And anyway, this particular frequency is probably wrong
            // 480 hz is TRUSTED by gokuparo disco stage - the looping sample doesn't line up otherwise
            //	device->machine().scheduler().timer_pulse(attotime::from_hz(480), FUNC(k054539_irq), 0, info);

            //info->stream = device->machine().sound().stream_alloc(*device, 0, 2, device->clock() / 384, info, k054539_update);

            //device->save_item(NAME(info->regs));
            //device->save_pointer(NAME(info->ram), 0x4000);
            //device->save_item(NAME(info->cur_ptr));

            return info.clock / 384;
        }

        //WRITE8_DEVICE_HANDLER( k054539_w )
        private void k054539_w(byte ChipID, int offset, byte data)
        {
            //k054539_state *info = get_safe_token(device);
            k054539_state info = K054539Data[ChipID];

            //#if 0
            //	int voice, reg;

            //	/* The K054539 has behavior like many other wavetable chips including
            //       the Ensoniq 550x and Gravis GF-1: if a voice is active, writing
            //       to it's current position is silently ignored.

            //       Dadandaan depends on this or the vocals go wrong.
            //       */
            //	if (offset < 8*0x20)
            //	{
            //		voice = offset / 0x20;
            //		reg = offset & ~0x20;

            //		if(info->regs[0x22c] & (1<<voice))
            //		{
            //			if (reg >= 0xc && reg <= 0xe)
            //				return;
            //		}
            //	}
            //#endif

            bool latch;
            int offs, ch, pan;
            byte[] regbase;
            int regptr;

            regbase = info.regs;
            latch = (info.k054539_flags & K054539_UPDATE_AT_KEYON) != 0 && (regbase[0x22f] & 1) != 0;
            //Console.Write("latch = {0} \n", latch);

            if (latch && offset < 0x100)
            {
                offs = (offset & 0x1f) - 0xc;
                ch = offset >> 5;

                if (offs >= 0 && offs <= 2)
                {
                    // latch writes to the position index registers
                    info.k054539_posreg_latch[ch][offs] = data;
                    //Console.Write("info->k054539_posreg_latch[{0}][{1}] = {2} \n", ch, offs, data);
                    return;
                }
            }

            else
                switch (offset)
                {
                    case 0x13f:
                        pan = (data >= 0x11 && data <= 0x1f) ? data - 0x11 : 0x18 - 0x11;
                        //if(info->intf->apan)
                        //	info->intf->apan(info->device, info->pantab[pan], info->pantab[0xe - pan]);
                        break;

                    case 0x214:
                        if (latch)
                        {
                            for (ch = 0; ch < 8; ch++)
                            {
                                if ((data & (1 << ch)) != 0)
                                {
                                    regptr = (ch << 5) + 0xc;

                                    // update the chip at key-on
                                    regbase[regptr + 0] = info.k054539_posreg_latch[ch][0];
                                    regbase[regptr + 1] = info.k054539_posreg_latch[ch][1];
                                    regbase[regptr + 2] = info.k054539_posreg_latch[ch][2];

                                    k054539_keyon(info, ch);
                                }
                            }
                        }
                        else
                        {
                            for (ch = 0; ch < 8; ch++)
                                if ((data & (1 << ch)) != 0)
                                    k054539_keyon(info, ch);
                        }
                        break;

                    case 0x215:
                        for (ch = 0; ch < 8; ch++)
                            if ((data & (1 << ch)) != 0)
                                k054539_keyoff(info, ch);
                        break;

                    /*case 0x227:
                    {
                        attotime period = attotime::from_hz((float)(38 + data) * (clock()/384.0f/14400.0f)) / 2.0f;

                        m_timer->adjust(period, 0, period);

                        m_timer_state = 0;
                        m_timer_handler(m_timer_state);
                    }*/
                    //break;

                    case 0x22d:
                        if (regbase[0x22e] == 0x80)
                            info.cur_zone[info.ptrCur_zone + info.cur_ptr] = data;
                        info.cur_ptr++;
                        if (info.cur_ptr == info.cur_limit)
                            info.cur_ptr = 0;
                        break;

                    case 0x22e:
                        info.cur_zone =
                            data == 0x80 ? info.ram : info.rom;
                        info.ptrCur_zone =
                            data == 0x80 ? 0 : (0x20000 * data);
                        info.cur_limit = data == 0x80 ? 0x4000 : 0x20000;
                        info.cur_ptr = 0;
                        break;

                    /*case 0x22f:
                        if (!(data & 0x20)) // Disable timer output?
                        {
                            m_timer_state = 0;
                            m_timer_handler(m_timer_state);
                        }
                    break;*/

                    default:
                        //#if 0
                        //			if(regbase[offset] != data) {
                        //				if((offset & 0xff00) == 0) {
                        //					chanoff = offset & 0x1f;
                        //					if(chanoff < 4 || chanoff == 5 ||
                        //					   (chanoff >=8 && chanoff <= 0xa) ||
                        //					   (chanoff >= 0xc && chanoff <= 0xe))
                        //						break;
                        //				}
                        //				if(1 || ((offset >= 0x200) && (offset <= 0x210)))
                        //					break;
                        //				logerror("K054539 %03x = %02x\n", offset, data);
                        //			}
                        //#endif
                        break;
                }

            regbase[offset] = data;
        }

        private static void reset_zones(k054539_state info)
        {
            int data = info.regs[0x22e];
            info.cur_zone = data == 0x80 ? info.ram : info.rom;
            info.ptrCur_zone = data == 0x80 ? 0 : (0x20000 * data);
            info.cur_limit = data == 0x80 ? 0x4000 : 0x20000;
        }

        //READ8_DEVICE_HANDLER( k054539_r )
        public byte k054539_r(byte ChipID, int offset)
        {
            //k054539_state *info = get_safe_token(device);
            k054539_state info = K054539Data[ChipID];
            switch (offset)
            {
                case 0x22d:
                    if ((info.regs[0x22f] & 0x10) != 0)
                    {
                        byte res = info.cur_zone[info.ptrCur_zone + info.cur_ptr];
                        info.cur_ptr++;
                        if (info.cur_ptr == info.cur_limit)
                            info.cur_ptr = 0;
                        return res;
                    }
                    else
                        return 0;
                case 0x22c:
                    break;
                default:
                    //LOG(("K054539 read %03x\n", offset));
                    break;
            }
            return info.regs[offset];
        }

        //static DEVICE_START( k054539 )
        private int device_start_k054539(byte ChipID, int clock)
        {
            //static const k054539_interface defintrf = { 0 };
            int i;
            //k054539_state *info = get_safe_token(device);
            k054539_state info;

            if (ChipID >= MAX_CHIPS)
                return 0;

            info = K054539Data[ChipID];
            //info->device = device;

            for (i = 0; i < 8; i++)
                info.k054539_gain[i] = 1.0;
            info.k054539_flags = K054539_RESET_FLAGS;

            //info->intf = (device->static_config() != NULL) ? (const k054539_interface *)device->static_config() : &defintrf;

            /*
                I've tried various equations on volume control but none worked consistently.
                The upper four channels in most MW/GX games simply need a significant boost
                to sound right. For example, the bass and smash sound volumes in Violent Storm
                have roughly the same values and the voices in Tokimeki Puzzledama are given
                values smaller than those of the hihats. Needless to say the two K054539 chips
                in Mystic Warriors are completely out of balance. Rather than forcing a
                "one size fits all" function to the voltab the current invert exponential
                appraoch seems most appropriate.
            */
            // Factor the 1/4 for the number of channels in the volume (1/8 is too harsh, 1/2 gives clipping)
            // vol=0 -> no attenuation, vol=0x40 -> -36dB
            for (i = 0; i < 256; i++)
                info.voltab[i] = Math.Pow(10.0, (-36.0 * (double)i / (double)0x40) / 20.0) / 4.0;

            // Pan table for the left channel
            // Right channel is identical with inverted index
            // Formula is such that pan[i]**2+pan[0xe-i]**2 = 1 (constant output power)
            // and pan[0xe] = 1 (full panning)
            for (i = 0; i < 0xf; i++)
                info.pantab[i] = Math.Sqrt((double)i) / Math.Sqrt((double)0xe);

            //k054539_init_chip(device, info);

            //device->machine().save().register_postload(save_prepost_delegate(FUNC(reset_zones), info));

            for (i = 0; i < 8; i++)
                info.Muted[i] = 0x00;

            return k054539_init_chip(info, clock);
        }

        private void device_stop_k054539(byte ChipID)
        {
            k054539_state info = K054539Data[ChipID];

            info.rom = null;
            info.ram = null;

            return;
        }

        private void device_reset_k054539(byte ChipID)
        {
            k054539_state info = K054539Data[ChipID];

            for (int i = 0; i < info.regs.Length; i++)
            {
                info.regs[i] = 0;
            }
            for (int i = 0; i < info.k054539_posreg_latch.Length; i++)
            {
                for (int j = 0; j < info.k054539_posreg_latch[i].Length; j++)
                    info.k054539_posreg_latch[i][j] = 0;
            }
            //info->k054539_flags |= K054539_UPDATE_AT_KEYON;

            info.reverb_pos = 0;
            info.cur_ptr = 0;
            for (int i = 0; i < 0x4000; i++)
            {
                info.ram[i] = 0;
            }

            return;
        }

        private void k054539_write_rom(byte ChipID, int ROMSize, int DataStart, int DataLength,
                               byte[] ROMData)
        {
            k054539_state info = K054539Data[ChipID];

            if (info.rom_size != ROMSize)
            {
                byte i;

                info.rom = new byte[ROMSize];
                info.rom_size = (uint)ROMSize;
                for (i = 0; i < ROMSize; i++)
                {
                    info.rom[i] = 0xff;
                }

                info.rom_mask = 0xFFFFFFFF;
                for (i = 0; i < 32; i++)
                {
                    if ((1U << i) >= info.rom_size)
                    {
                        info.rom_mask = (uint)((1 << i) - 1);
                        break;
                    }
                }
            }
            if (DataStart > ROMSize)
                return;
            if (DataStart + DataLength > ROMSize)
                DataLength = ROMSize - DataStart;

            for (int j = 0; j < DataLength; j++)
            {
                info.rom[DataStart + j] = ROMData[j];
            }

            return;
        }

        public void k054539_write_rom2(byte ChipID, int ROMSize, int DataStart, int DataLength,
                               byte[] ROMData,int startAdr)
        {
            k054539_state info = K054539Data[ChipID];

            if (info.rom_size != ROMSize)
            {
                byte i;

                info.rom = new byte[ROMSize];
                info.rom_size = (uint)ROMSize;
                for (int ind = 0; ind < ROMSize; ind++)
                {
                    info.rom[ind] = 0xff;
                }

                info.rom_mask = 0xFFFFFFFF;
                for (i = 0; i < 32; i++)
                {
                    if ((1U << i) >= info.rom_size)
                    {
                        info.rom_mask = (uint)((1 << i) - 1);
                        break;
                    }
                }
            }
            if (DataStart > ROMSize)
                return;
            if (DataStart + DataLength > ROMSize)
                DataLength = ROMSize - DataStart;

            for (int j = 0; j < DataLength; j++)
            {
                info.rom[DataStart + j] = ROMData[startAdr+ j];
            }

            return;
        }


        public void k054539_set_mute_mask(byte ChipID, uint MuteMask)
        {
            k054539_state info = K054539Data[ChipID];
            byte CurChn;

            for (CurChn = 0; CurChn < 8; CurChn++)
                info.Muted[CurChn] = (byte)((MuteMask >> CurChn) & 0x01);

            return;
        }

        public override int Write(byte ChipID, int port, int adr, int data)
        {
            k054539_w(ChipID, adr, (byte)data);
            return 0;
        }




        /**************************************************************************
         * Generic get_info
         **************************************************************************/

        /*DEVICE_GET_INFO( k054539 )
        {
            switch (state)
            {
                // --- the following bits of info are returned as 64-bit signed integers --- //
                case DEVINFO_INT_TOKEN_BYTES:					info->i = sizeof(k054539_state);				break;

                // --- the following bits of info are returned as pointers to data or functions --- //
                case DEVINFO_FCT_START:							info->start = DEVICE_START_NAME( k054539 );		break;
                case DEVINFO_FCT_STOP:							// nothing //									break;
                case DEVINFO_FCT_RESET:							// nothing //									break;

                // --- the following bits of info are returned as NULL-terminated strings --- //
                case DEVINFO_STR_NAME:							strcpy(info->s, "K054539");						break;
                case DEVINFO_STR_FAMILY:					strcpy(info->s, "Konami custom");				break;
                case DEVINFO_STR_VERSION:					strcpy(info->s, "1.0");							break;
                case DEVINFO_STR_SOURCE_FILE:						strcpy(info->s, __FILE__);						break;
                case DEVINFO_STR_CREDITS:					strcpy(info->s, "Copyright Nicola Salmoria and the MAME Team"); break;
            }
        }*/


        //DEFINE_LEGACY_SOUND_DEVICE(K054539, k054539);

    }
}
