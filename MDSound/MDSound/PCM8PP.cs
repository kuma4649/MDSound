using System;
using System.Collections.Generic;
using System.Text;

namespace MDSound
{
    public class PCM8PP : Instrument
    {
        public class chStats
        {
            public bool play = false;
            public int mode = 0;
            public uint adrsPtr = 0;
            public uint len = 0;
            public uint endAdrs = 0;
            public double freq = 0;
            public int outs = 0;
            public int type = 0;
            public int volume = 0;
            public int pan = 3;
            public double step = 0;
        }

        private chStats[] ch;
        private byte[] mem;
        private double sampleRate;
        private double baseClock;

        private double[] freqTable = new double[]
        {
            //ADPCM mono
            3906.2,
            5208.0,
            7812.5,
            10416.7,
            15625.0,
            //16bit signed PCM mono
            -1,
            //8bit signed PCM mono
            -1,
            //16bit signed PCM (Through) mono
            -1,
            //16bit signed PCM mono
            15625.0,
            16000.0,
            22050.0,
            24000.0,
            32000.0,
            44100.0,
            48000.0,
            -1,
            //8bit signed PCM mono
            15625.0,
            16000.0,
            22050.0,
            24000.0,
            32000.0,
            44100.0,
            48000.0,
            -1,
            //16bit signed PCM stereo
            15625.0,
            16000.0,
            22050.0,
            24000.0,
            32000.0,
            44100.0,
            48000.0,
            -1,
            //8bit signed PCM stereo
            15625.0,
            16000.0,
            22050.0,
            24000.0,
            32000.0,
            44100.0,
            48000.0,
            -1,
            //variabled ADPCM mono
            -1,
            //variabled 16bit signed PCM mono
            -1
        };
        private int[] outsTable = new int[]
        {
            1,1,1,1, 1,1,1,1,
            1,1,1,1, 1,1,1,1,
            1,1,1,1, 1,1,1,1,
            2,2,2,2, 2,2,2,2,
            2,2,2,2, 2,2,2,2,
            1,1
        };
        private int[] typeTable = new int[]
        {
            0,0,0,0,0,
            2,1,2,
            2,2,2,2,2,2,2,2,
            1,1,1,1,1,1,1,1,
            2,2,2,2,2,2,2,2,
            1,1,1,1,1,1,1,1,
            0,2
        };
        private int[] volTable = new int[16] {
            2,3,4,5,6,8,10,12,16,20,24,32,40,48,64,80,
        };

        public override string Name { get { return "PCM8PP"; } set { } }
        public override string ShortName { get { return "PCM8PP"; } set { } }

        public override void Reset(byte ChipID)
        {
            //2
            ch = new chStats[16];
            for (int i = 0; i < ch.Length; i++)
            {
                ch[i] = new chStats();
            }
        }

        public override uint Start(byte ChipID, uint clock)
        {
            return clock;
        }


        public override uint Start(byte ChipID, uint clock, uint ClockValue, params object[] option)
        {
            //1
            sampleRate = clock;
            baseClock = ClockValue;
            return clock;
        }

        public override void Stop(byte ChipID)
        {
        }

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {
            for (int i = 0; i < samples; i++)
            {
                outputs[0][i] = 0;
                outputs[1][i] = 0;
                for (int c = 0; c < ch.Length; c++)
                {
                    if (!ch[c].play) continue;
                    int val=0;
                    if (ch[c].type == 1) val = mem[ch[c].adrsPtr];
                    if (ch[c].type == 2) val = (short)((mem[ch[c].adrsPtr] << 8) + mem[ch[c].adrsPtr+1]);
                    double step = sampleRate / ch[c].freq;
                    ch[c].step += step;
                    while (ch[c].step >= 1.0)
                    {
                        ch[c].adrsPtr += (uint)(1 * ch[c].type);
                        ch[c].step -= 1.0;
                    }
                    if (ch[c].adrsPtr >= ch[c].endAdrs) 
                        ch[c].play = false;

                    val = ((val * ch[c].volume) >> 3);
                    if (ch[c].outs == 1)
                    {
                        outputs[0][i] += val;
                        outputs[1][i] += val;
                    }
                    else
                    {
                        outputs[0][i] += val * ((ch[c].pan & 1) != 0 ? 1 : 0);
                        outputs[1][i] += val * ((ch[c].pan & 2) != 0 ? 1 : 0);
                    }
                }
            }
        }

        public override int Write(byte ChipID, int port, int adr, int data)
        {
            return 0;
        }



        public void KeyOn(int c, uint adrsPtr, int mode, int len)
        {
            ch[c].play = true;
            int v = (byte)(mode >> 16) & 0xff;
            if (v != 0xff)
            {
                v &= 0xf;
                ch[c].volume = volTable[v];
                ch[c].mode = (int)(((uint)ch[c].mode & 0xFF00FFFF) | (uint)(v << 16)); ;
            }
            int m = (byte)(mode >> 8);
            if (m != 0xff)
            {
                ch[c].freq = freqTable[m];
                ch[c].outs = outsTable[m];
                ch[c].type = typeTable[m];
                ch[c].mode = (int)(((uint)ch[c].mode & 0xFFFF00FF) | (uint)(m << 8)); ;
            }
            int p = (byte)mode;
            if (p != 0xff)
            {
                if ((p&3) != 0)
                {
                    ch[c].adrsPtr = adrsPtr;
                    ch[c].len = (uint)len;
                    ch[c].endAdrs = (uint)(adrsPtr + len);
                    ch[c].pan = p;
                    ch[c].mode = (int)(((uint)ch[c].mode & 0xFFFFFF00) | (uint)(p << 0)); 
                }
            }
        }

        public void KeyOff(int c)
        {
            ch[c].play = false;
            ch[c].adrsPtr = (uint)0;
            ch[c].mode = 0;
            ch[c].len = (uint)0;
            ch[c].endAdrs = (uint)0;
        }


        public void MountMemory(byte[] mem)
        {
            this.mem = mem;
        }

    }
}
