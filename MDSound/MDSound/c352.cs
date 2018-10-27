using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDSound
{
    public class c352 : Instrument
    {

        public c352()
        {
            visVolume = new int[2][][] {
                new int[1][] { new int[2] { 0, 0 } }
                , new int[1][] { new int[2] { 0, 0 } }
            };
            //0..Main
        }

        public override void Reset(byte ChipID)
        {
            device_reset_c352(ChipID);
        }

        public override uint Start(byte ChipID, uint clock)
        {
            return Start(ChipID, 44100, clock);
        }

        public override void Stop(byte ChipID)
        {
            device_stop_c352(ChipID);
        }

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {
            c352_update(ChipID, outputs, samples);

            visVolume[ChipID][0][0] = outputs[0][0];
            visVolume[ChipID][0][1] = outputs[1][0];
        }

        public override uint Start(byte ChipID, uint SamplingRate, uint clock, params object[] Option)
        {
            byte bytC352ClkDiv = 0;
            if (Option == null || Option.Length<1) bytC352ClkDiv = 0;
            else bytC352ClkDiv= (byte)Option[0];

            return (uint)device_start_c352(ChipID, (int)clock, bytC352ClkDiv*4);
        }



        //        // license:BSD-3-Clause
        //        // copyright-holders:R. Belmont, superctr
        //        /*
        //            c352.c - Namco C352 custom PCM chip emulation
        //            v2.0
        //            By R. Belmont
        //            Rewritten and improved by superctr
        //            Additional code by cync and the hoot development team

        //            Thanks to Cap of VivaNonno for info and The_Author for preliminary reverse-engineering

        //            Chip specs:
        //            32 voices
        //            Supports 8-bit linear and 8-bit muLaw samples
        //            Output: digital, 16 bit, 4 channels
        //            Output sample rate is the input clock / (288 * 2).
        //         */

        //        //#include "emu.h"
        //        //#include "streams.h"
        //# include <math.h>
        //# include <stdlib.h>
        //# include <string.h> // for memset
        //# include <stddef.h> // for NULL
        //# include "mamedef.h"
        //# include "c352.h"


        public static ushort[][] flags = new ushort[2][] { new ushort[32], new ushort[32] };


        private const int C352_VOICES = 32;
        private enum C352_FLG
        {
            BUSY = 0x8000,   // channel is busy
            KEYON = 0x4000,   // Keyon
            KEYOFF = 0x2000,   // Keyoff
            LOOPTRG = 0x1000,   // Loop Trigger
            LOOPHIST = 0x0800,   // Loop History
            FM = 0x0400,   // Frequency Modulation
            PHASERL = 0x0200,   // Rear Left invert phase 180 degrees
            PHASEFL = 0x0100,   // Front Left invert phase 180 degrees
            PHASEFR = 0x0080,   // invert phase 180 degrees (e.g. flip sign of sample)
            LDIR = 0x0040,   // loop direction
            LINK = 0x0020,   // "long-format" sample (can't loop, not sure what else it means)
            NOISE = 0x0010,   // play noise instead of sample
            MULAW = 0x0008,   // sample is mulaw instead of linear 8-bit PCM
            FILTER = 0x0004,   // don't apply filter
            REVLOOP = 0x0003,   // loop backwards
            LOOP = 0x0002,   // loop forward
            REVERSE = 0x0001    // play sample backwards
        };

        public class C352_Voice
        {

            public uint pos=0;
            public uint counter = 0;

            public short sample = 0;
            public short last_sample = 0;

            public ushort vol_f = 0;
            public ushort vol_r = 0;
            public byte[] curr_vol = new byte[4] { 0, 0, 0, 0 };
            public ushort freq = 0;
            public ushort flags = 0;

            public ushort wave_bank = 0;
            public ushort wave_start = 0;
            public ushort wave_end = 0;
            public ushort wave_loop = 0;

            public byte mute = 0;

        }

        public class C352
        {

            public uint sample_rate_base;
            public ushort divider;

            public C352_Voice[] v = new C352_Voice[C352_VOICES];

            public ushort random;
            public ushort control; // control flags, purpose unknown.

            public byte[] wave;
            public uint wavesize;
            public uint wave_mask;

            public byte muteRear;     // flag from VGM header
                                      //UINT8 optMuteRear;  // option

        }


        private const int MAX_CHIPS = 0x02;
        private static C352[] C352Data = new C352[MAX_CHIPS] { new C352(), new C352() };

        private static byte MuteAllRear = 0x00;

        public override string Name { get { return "C352"; } set { } }
        public override string ShortName { get { return "C352"; } set { } }

        private static void C352_fetch_sample(C352 c, C352_Voice v)
        {
            //Console.WriteLine("v->sample = {0}  v->pos = {1}  c->wave_mask = {2}  v->flags ={3} ", v.sample, v.pos, c.wave_mask, v.flags);

            v.last_sample = v.sample;

            //if (v.flags & C352_FLG_NOISE)
            if ((v.flags & 0x0010) != 0)
            {
                c.random = (ushort)((c.random >> 1) ^ ((-(c.random & 1)) & 0xfff6));
                v.sample = (short)c.random;
            }
            else
            {
                sbyte s, s2;
                ushort pos;

                s = (sbyte)c.wave[v.pos & c.wave_mask];

                v.sample = (short)(s << 8);
                //if (v.flags & C352_FLG_MULAW)
                if ((v.flags & 0x0008) != 0)
                {
                    s2 = (sbyte)((s & 0x7f) >> 4);

                    v.sample = (short)(((s2 * s2) << 4) - (~(s2 << 1)) * (s & 0x0f));
                    v.sample = (short)(((s & 0x80) != 0) ? ((~v.sample) << 5) : (v.sample << 5));
                }

                pos = (ushort)(v.pos & 0xffff);

                //if ((v.flags & C352_FLG_LOOP) && v.flags & C352_FLG_REVERSE)
                if ((v.flags & 0x0002) != 0 && (v.flags & 0x0001) != 0)
                {
                    // backwards>forwards
                    //if ((v.flags & C352_FLG_LDIR) && pos == v.wave_loop)
                    if ((v.flags & 0x0040) != 0 && pos == v.wave_loop)
                        //v.flags &= ~C352_FLG_LDIR;
                        v.flags &= 0xffbf;
                    // forwards>backwards
                    //else if (!(v.flags & C352_FLG_LDIR) && pos == v.wave_end)
                    else if ((v.flags & 0x0040) == 0 && pos == v.wave_end)
                        //v.flags |= C352_FLG_LDIR;
                        v.flags |= 0x0040;

                    //v.pos += (v.flags & C352_FLG_LDIR) ? -1 : 1;
                    v.pos = (uint)(v.pos + ((v.flags & 0x0040) != 0 ? -1 : 1));
                }
                else if (pos == v.wave_end)
                {
                    //if ((v.flags & C352_FLG_LINK) && (v.flags & C352_FLG_LOOP))
                    if ((v.flags & 0x0020) != 0 && (v.flags & 0x0002) != 0)
                    {
                        v.pos = (uint)((v.wave_start << 16) | v.wave_loop);
                        //v.flags |= C352_FLG_LOOPHIST;
                        v.flags |= 0x0800;
                    }
                    //else if (v.flags & C352_FLG_LOOP)
                    else if ((v.flags & 0x0002) != 0)
                    {
                        v.pos = (v.pos & 0xff0000) | v.wave_loop;
                        //v.flags |= C352_FLG_LOOPHIST;
                        v.flags |= 0x0800;
                    }
                    else
                    {
                        //v.flags |= C352_FLG_KEYOFF;
                        v.flags |= 0x2000;
                        //v.flags &= ~C352_FLG_BUSY;
                        v.flags &= 0x7fff;
                        v.sample = 0;
                    }
                }
                else
                {
                    //v.pos += (v.flags & C352_FLG_REVERSE) ? -1 : 1;
                    v.pos = (uint)(v.pos + ((v.flags & 0x0001) != 0 ? -1 : 1));
                }
            }
        }

        private static void c352_ramp_volume(C352_Voice v, int ch, byte val)
        {
            short vol_delta = (short)(v.curr_vol[ch] - val);
            if (vol_delta != 0)
                v.curr_vol[ch] = (byte)(v.curr_vol[ch] + ((vol_delta > 0) ? -1 : 1));
            //Console.WriteLine("v.curr_vol[ch{0}] = {1} val={2}", ch, v.curr_vol[ch], val);
        }

        private void c352_update(byte ChipID, int[][] outputs, int samples)
        {
            C352 c = C352Data[ChipID];
            int i, j;
            short s;
            int next_counter;
            C352_Voice v;

            int[] _out = new int[4];
            //short[] _out = new short[4];

            for (i = 0; i < samples; i++)
            {
                outputs[0][i] = 0;
                outputs[1][i] = 0;
            }

            for (i = 0; i < samples; i++)
            {
                _out[0] = _out[1] = _out[2] = _out[3] = 0;

                for (j = 0; j < C352_VOICES; j++)
                {

                    v = c.v[j];
                    s = 0;
                    flags[ChipID][j] = v.flags;

                    //Console.WriteLine(" v.flags={0}", v.flags);
                    //if (v.flags & C352_FLG_BUSY)
                    if ((v.flags & 0x8000) != 0)
                    {
                        next_counter = (int)(v.counter + v.freq);

                        if ((next_counter & 0x10000) != 0)
                        {
                            C352_fetch_sample(c, v);
                            //Console.WriteLine("fetch");
                            //Console.WriteLine(" ch={0} 0={1}  1={2}  2={3}  3={4}",j, _out[0], _out[1], _out[2], _out[3]);
                        }

                        if (((next_counter ^ v.counter) & 0x18000) != 0)
                        {
                            c352_ramp_volume(v, 0, (byte)(v.vol_f >> 8));
                            c352_ramp_volume(v, 1, (byte)(v.vol_f & 0xff));
                            c352_ramp_volume(v, 2, (byte)(v.vol_r >> 8));
                            c352_ramp_volume(v, 3, (byte)(v.vol_r & 0xff));
                        }

                        v.counter = (uint)(next_counter & 0xffff);
                        //Console.WriteLine(" v.freq={0}", v.freq);
                        //Console.WriteLine(" v.counter={0}", v.counter);

                        s = v.sample;

                        // Interpolate samples
                        //if ((v.flags & C352_FLG_FILTER) == 0)
                        if ((v.flags & 0x0004) == 0)
                            s = (short)(v.last_sample + (v.counter * (v.sample - v.last_sample) >> 16));
                    }

                    if (c.v[j].mute == 0)
                    {
                        // Left
                        //_out[0] += (((v.flags & C352_FLG_PHASEFL) ? -s : s) * v.curr_vol[0]) >> 8;
                        //_out[2] += (((v.flags & C352_FLG_PHASEFR) ? -s : s) * v.curr_vol[2]) >> 8;
                        _out[0] +=( (((v.flags & 0x0100) != 0 ? -s : s) * v.curr_vol[0]) >> 9);
                        _out[2] +=( (((v.flags & 0x0080) != 0 ? -s : s) * v.curr_vol[2]) >> 9);
                        // Right
                        //_out[1] += (((v.flags & C352_FLG_PHASERL) ? -s : s) * v.curr_vol[1]) >> 8;
                        //_out[3] += (((v.flags & C352_FLG_PHASERL) ? -s : s) * v.curr_vol[3]) >> 8;
                        _out[1] +=( (((v.flags & 0x0200) != 0 ? -s : s) * v.curr_vol[1]) >> 9);
                        _out[3] +=( (((v.flags & 0x0200) != 0 ? -s : s) * v.curr_vol[3]) >> 9);
                    }

                    //Console.WriteLine("out [0]={0}  [1]={1}  [2]={2}  [3]={3}", _out[0] , _out[1] , _out[2] , _out[3]);
                }

                outputs[0][i] += _out[0];
                outputs[1][i] += _out[1];
                if (c.muteRear == 0 && MuteAllRear == 0)
                {
                    outputs[0][i] += _out[2];
                    outputs[1][i] += _out[3];
                }
            }
        }

        private int device_start_c352(byte ChipID, int clock, int clkdiv)
        {
            C352 c;

            if (ChipID >= MAX_CHIPS)
                return 0;

            c = C352Data[ChipID];

            c.wave = null;
            c.wavesize = 0x00;

            c.divider = (ushort)(clkdiv != 0 ? clkdiv : 288);
            c.sample_rate_base = (uint)((clock & 0x7FFFFFFF) / c.divider);
            c.muteRear = (byte)((clock & 0x80000000) >> 31);

            c.v = new C352_Voice[C352_VOICES];
            for (int i = 0; i < C352_VOICES; i++)
            {
                c.v[i] = new C352_Voice();
            }

            c352_set_mute_mask(ChipID, 0x00000000);

            return (int)c.sample_rate_base;
        }

        private void device_stop_c352(byte ChipID)
        {
            C352 c = C352Data[ChipID];

            c.wave = null;

            return;
        }

        private void device_reset_c352(byte ChipID)
        {
            C352 c = C352Data[ChipID];
            uint muteMask;

            muteMask = c352_get_mute_mask(ChipID);

            // clear all channels states
            c.v = new C352_Voice[C352_VOICES];
            for (int i = 0; i < C352_VOICES; i++)
            {
                c.v[i] = new C352_Voice();
            }

            // init noise generator
            c.random = 0x1234;
            c.control = 0;

            c352_set_mute_mask(ChipID, muteMask);

            return;
        }

        //        private static ushort[] C352RegMap = new ushort[8]{
        //    offsetof(C352_Voice,vol_f) / sizeof(ushort),
        //    offsetof(C352_Voice,vol_r) / sizeof(ushort),
        //    offsetof(C352_Voice,freq) / sizeof(ushort),
        //    offsetof(C352_Voice,flags) / sizeof(ushort),
        //    offsetof(C352_Voice,wave_bank) / sizeof(ushort),
        //    offsetof(C352_Voice,wave_start) / sizeof(ushort),
        //    offsetof(C352_Voice,wave_end) / sizeof(ushort),
        //    offsetof(C352_Voice,wave_loop) / sizeof(ushort),
        //};

        private ushort c352_r(byte ChipID, int address)
        {
            C352 c = C352Data[ChipID];

            if (address < 0x100)
            {
                int ch = address / 8;
                switch (address % 8)
                {
                    case 0:
                        return c.v[ch].vol_f;
                    case 1:
                        return c.v[ch].vol_r;
                    case 2:
                        return c.v[ch].freq;
                    case 3:
                        return c.v[ch].flags;
                    case 4:
                        return c.v[ch].wave_bank;
                    case 5:
                        return c.v[ch].wave_start;
                    case 6:
                        return c.v[ch].wave_end;
                    case 7:
                        return c.v[ch].wave_loop;
                }
            }
            else if (address == 0x200)
                return c.control;
            else
                return 0;

            return 0;
        }

        private void c352_w(byte ChipID, int address, ushort val)
        {
            //Console.WriteLine("address = {0}  val = {1}", address, val);

            C352 c = C352Data[ChipID];

            int i;

            if (address < 0x100) // Channel registers, see map above.
            {
                int ch = address / 8;
                switch (address % 8)
                {
                    case 0:
                        c.v[ch].vol_f = val;
                        break;
                    case 1:
                        c.v[ch].vol_r = val;
                        break;
                    case 2:
                        c.v[ch].freq = val;
                        //Console.WriteLine("c.v[ch{0}].freq = {1}", ch, val);
                        break;
                    case 3:
                        c.v[ch].flags = val;
                        break;
                    case 4:
                        c.v[ch].wave_bank = val;
                        break;
                    case 5:
                        c.v[ch].wave_start = val;
                        break;
                    case 6:
                        c.v[ch].wave_end = val;
                        break;
                    case 7:
                        c.v[ch].wave_loop = val;
                        break;
                }
            }
            else if (address == 0x200)
            {
                c.control = val;
                //logerror("C352 control register write: %04x\n",val);
            }
            else if (address == 0x202) // execute keyons/keyoffs
            {
                for (i = 0; i < C352_VOICES; i++)
                {
                    //if ((c.v[i].flags & C352_FLG_KEYON))
                    if ((c.v[i].flags & 0x4000) != 0)
                    {
                        c.v[i].pos = (uint)((c.v[i].wave_bank << 16) | c.v[i].wave_start);

                        c.v[i].sample = 0;
                        c.v[i].last_sample = 0;
                        c.v[i].counter = 0xffff;

                        //c.v[i].flags |= C352_FLG.BUSY;
                        c.v[i].flags |= 0x8000;
                        //c.v[i].flags &= ~(C352_FLG_KEYON | C352_FLG_LOOPHIST);
                        c.v[i].flags &= 0xb7ff;

                        c.v[i].curr_vol[0] = c.v[i].curr_vol[1] = 0;
                        c.v[i].curr_vol[2] = c.v[i].curr_vol[3] = 0;
                    }
                    //else if (c.v[i].flags & C352_FLG.KEYOFF)
                    else if ((c.v[i].flags & 0x2000) != 0)
                    {
                        //c.v[i].flags &= ~(C352_FLG.BUSY | C352_FLG.KEYOFF);
                        c.v[i].flags &= 0x5fff;
                        c.v[i].counter = 0xffff;
                    }
                }
            }
        }


        public void c352_write_rom(byte ChipID, uint ROMSize, int DataStart, int DataLength,
                            byte[] ROMData)
        {
            C352 c = C352Data[ChipID];

            if (c.wavesize != ROMSize)
            {
                c.wave = new byte[ROMSize];
                c.wavesize = ROMSize;
                for (c.wave_mask = 1; c.wave_mask < c.wavesize; c.wave_mask <<= 1)
                    ;
                c.wave_mask--;
                for (int i = 0; i < ROMSize; i++)
                {
                    c.wave[i] = 0xff;
                }
            }
            if (DataStart > ROMSize)
                return;
            if (DataStart + DataLength > ROMSize)
                DataLength = (int)(ROMSize - DataStart);

            for (int i = 0; i < DataLength; i++)
            {
                c.wave[i + DataStart] = ROMData[i];
            }

            return;
        }

        public void c352_write_rom2(byte ChipID, uint ROMSize, int DataStart, int DataLength,
                            byte[] ROMData,uint SrcStartAdr)
        {
            //Console.WriteLine("ROMSize={0:x} , DataStart={1:x} , DataLength={2:x}", ROMSize, DataStart, DataLength);
            C352 c = C352Data[ChipID];

            if (c.wavesize != ROMSize)
            {
                c.wave = new byte[ROMSize];
                c.wavesize = ROMSize;
                for (c.wave_mask = 1; c.wave_mask < c.wavesize; c.wave_mask <<= 1)
                    ;
                c.wave_mask--;
                for (int i = 0; i < ROMSize; i++)
                {
                    c.wave[i] = 0xff;
                }
            }
            if (DataStart > ROMSize)
                return;
            if (DataStart + DataLength > ROMSize)
                DataLength = (int)(ROMSize - DataStart);

            for (int i = 0; i < DataLength; i++)
            {
                c.wave[i + DataStart] = ROMData[i+ SrcStartAdr];
            }

            return;
        }

        private void c352_set_mute_mask(byte ChipID, uint MuteMask)
        {
            C352 c = C352Data[ChipID];
            byte CurChn;

            for (CurChn = 0; CurChn < C352_VOICES; CurChn++)
                c.v[CurChn].mute = (byte)((MuteMask >> CurChn) & 0x01);

            return;
        }

        private uint c352_get_mute_mask(byte ChipID)
        {
            C352 c = C352Data[ChipID];
            uint muteMask;
            byte CurChn;

            muteMask = 0x00000000;
            for (CurChn = 0; CurChn < C352_VOICES; CurChn++)
                muteMask |= (uint)((c.v[CurChn].mute << CurChn));

            return muteMask;
        }

        public void c352_set_options(byte Flags)
        {
            MuteAllRear = (byte)((Flags & 0x01) >> 0);

            return;
        }

        public override int Write(byte ChipID, int port, int adr, int data)
        {
            c352_w(ChipID, adr, (ushort)data);
            return 0;
        }
    }
}
