using System;
using System.Collections.Generic;
using System.Text;

namespace MDSound
{
    public class gigatron : Instrument
    {
        public override string Name { get { return "Gigatron"; } set { } }
        public override string ShortName { get { return "Gigatron"; } set { } }

        public class Channel
        {
            public short osc;
            public short key;
            public byte wavX;
            public byte wavA;
        }

        public class GigatronState
        {
            public Channel[] ch = new Channel[4] {
                new Channel(), new Channel(), new Channel(), new Channel()
            };
            public byte[] soundTable = new byte[256];
            public byte samp = 3;
            public double scanlineCounter = 0;

            public double scanlines = 521.0;
            public double vSync = 59.98;
            public double bClock = 521.0 * 59.98;// scanlines * vSync
            public double audioSampleRate = 44100;
            public byte channelMask = 0x3;
        }

        public GigatronState[] gig = new GigatronState[2] {
            new GigatronState(),new GigatronState()
        };



        public override void Reset(byte chipID)
        {
            chipID &= 1;
            GigatronState g = gig[chipID];

            Stop(chipID);
            resetSample(chipID);
        }

        public override uint Start(byte chipID, uint clock)
        {
            chipID &= 1;
            GigatronState g = gig[chipID];
            g.audioSampleRate = clock;
            g.scanlines = 521.0;
            g.vSync = 59.98;
            g.bClock = g.scanlines * g.vSync;

            Reset(chipID);

            return clock;
        }

        public override uint Start(byte chipID, uint clock, uint ClockValue, params object[] option)
        {
            chipID &= 1;
            GigatronState g = gig[chipID];
            g.audioSampleRate = clock;
            g.bClock = ClockValue;

            Reset(chipID);  

            return clock;
        }

        public override void Stop(byte chipID)
        {
            chipID &= 1;
            GigatronState g = gig[chipID];
            foreach(Channel ch in g.ch)
            {
                ch.osc = 0;
                ch.key = 0;
                ch.wavX = 0;
                ch.wavA = 0;
            }
        }

        public override void Update(byte chipID, int[][] outputs, int samples)
        {
            chipID &= 1;
            GigatronState g = gig[chipID];

            //合成処理
            for (int p = 0; p < samples; p++)
            {

                //scanlineCounterの更新
                g.scanlineCounter += g.audioSampleRate / g.bClock;

                // 4scanline毎に実施
                while (g.scanlineCounter >= 4.0)
                {
                    g.samp = 3;

                    //channel update
                    for (int n = 0; n < 4; n++)
                    {

                        int c = n & g.channelMask;// ? from dev.asm.py

                        g.ch[c].osc += g.ch[c].key;
                        byte i = (byte)((g.ch[c].osc >> 7) & 0xfc);
                        i ^= g.ch[c].wavX;
                        i = (byte)(g.soundTable[i] + g.ch[c].wavA);
                        i = (byte)((i & 128) != 0 ? 63 : (i & 63));
                        g.samp += i;
                    }

                    //上位4bitのみ出力
                    g.samp &= 0xf0;

                    g.scanlineCounter -= 4.0;
                }

                outputs[0][p] = g.samp << 8;
                outputs[1][p] = g.samp << 8;
            }

        }

        public override int Write(byte chipID, int port, int adr, int data)
        {
            chipID &= 1;
            GigatronState g = gig[chipID];
            ushort ad = (ushort)adr;
            byte dat = (byte)data;

            if (ad == 0x21)
            {
                g.channelMask = (byte)(dat & 0x7);
                return 0;
            }

            int hi = ad & 0xff00;
            int lo = ad & 0xff;

            if (hi == 0x0700)
            {
                g.soundTable[lo] = dat;
                return 0;
            }

            if (lo < 250) return 0;
            if (hi < 0x100) return 0;
            if (hi > 0x400) return 0;

            Channel c = g.ch[(hi >> 8) - 1];
            switch (lo)
            {
                case 250:
                    c.wavA = dat;
                    break;
                case 251:
                    c.wavX = dat;
                    break;
                case 252:
                    c.key = (short)((c.key & 0xff80) | (dat & 0x7f));
                    break;
                case 253:
                    c.key = (short)((c.key & 0x007f) | (dat << 7));
                    break;
                case 254:
                    c.osc = (short)((c.osc & 0xff80) | (dat & 0x7f));
                    break;
                case 255:
                    c.osc = (short)((c.osc & 0x007f) | (dat << 7));
                    break;
            }

            return 0;
        }

        private void resetSample(byte chipID)
        {
            chipID &= 1;
            GigatronState g = gig[chipID];

            g.soundTable = new byte[256];
            int r = (int)DateTime.Now.Ticks;
            for (int i = 0; i < 64; i++)
            {
                //noise
                r += r * 56465321 + 456156321;
                g.soundTable[i * 4 + 0] = (byte)(r & 63);
                //Triangle
                g.soundTable[i * 4 + 1] = (byte)(i < 32 ? 2 * i : (127 - 2 * i));
                //Pulse
                g.soundTable[i * 4 + 2] = (byte)(i < 32 ? 0 : 63);
                //Sawtooth
                g.soundTable[i * 4 + 3] = (byte)i;
            }

        }

    }
}
