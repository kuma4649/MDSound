using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace MDSound
{
    public class PCM8PP : Instrument
    {
        //
        // 既存処理の部分はほぼX68SoundのPCM8.csをそのまま使用
        //
        //
        // 未実装状況
        // 番号 データ形式                 出力     仕様
        //  7H  16bit Signed PCM(Through)  Monoural 再生周波数に左右される // どんな動きかよくわかんない
        //  FH  Valiabled 16bit Signed PCM Monoural 周波数を可変出来る#1   // zmusicでは使用しないと思う
        // 17H  Valiabled  8bit Signed PCM Monoural 周波数を可変出来る#1   // zmusicでは使用しないと思う
        // 1FH  Valiabled 16bit Signed PCM Stereo   周波数を可変出来る#1   // zmusicでは使用しないと思う
        // 27H  Valiabled  8bit Signed PCM Stereo   周波数を可変出来る#1   // zmusicでは使用しないと思う
        // 28H  Valiabled ADPCM            Monoural 周波数を可変出来る#2   // zmusicでは使用しないと思う
        // 29H  Valiabled 16bit Signed PCM Monoural 周波数を可変出来る#2   // zmusicでは使用しないと思う
        //


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
            public int PcmKind = 0;
            public bool N1DataFlag = false;
            public byte N1Data = 0;
            public int InpPcm = 0;
            public int InpPcm_prev = 0;
            public int OutPcm = 0;
            public int Pcm = 0;
            public int Scale = 0;
            public int Pcm16Prev = 0;
            public bool adpcmUpdate = true;
            public bool mute = false;
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
            15625.0,
            //8bit signed PCM mono
            15625.0,
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
        public int[] dltLTBL = new int[48 + 1]{
            16,17,19,21,23,25,28,31,34,37,41,45,50,55,60,66,
            73,80,88,97,107,118,130,143,157,173,190,209,230,253,279,307,
            337,371,408,449,494,544,598,658,724,796,876,963,1060,1166,1282,1411,1552,
        };
        public int[] DCT = new int[16] {
            -1,-1,-1,-1,2,4,6,8,
            -1,-1,-1,-1,2,4,6,8,
        };
        private const int MAXPCMVAL = (2047);

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
                //バッファクリア
                outputs[0][i] = 0;
                outputs[1][i] = 0;

                for (int c = 0; c < ch.Length; c++)
                {
                    //発音していないなら次のチャンネルの処理へ
                    if (!ch[c].play) continue;

                    chStats st = ch[c];
                    int valL = 0;
                    int valR = 0;

                    if (st.PcmKind < 7)
                    {
                        //pcm8(既存)の加工処理
                        if (st.PcmKind == 5)
                        {   // 16bitPCM
                            valL= (short)((mem[st.adrsPtr] << 8) + mem[st.adrsPtr + 1]);
                            //pcm16_2pcm(st, valL);
                            //st.OutPcm = ((st.InpPcm << 9) - (st.InpPcm_prev << 9) + 459 * st.OutPcm) >> 9;
                            //st.InpPcm_prev = st.InpPcm;
                            //音量反映
                            valL = valL * st.volume;
                            valL = valL >> 3;//3 適当
                            valR = valL;
                        }
                        else if (st.PcmKind == 6)
                        {   // 8bitPCM
                            valL = (byte)mem[st.adrsPtr];
                            //pcm16_2pcm(st,valL);
                            //st.OutPcm = ((st.InpPcm << 9) - (st.InpPcm_prev << 9) + 459 * st.OutPcm) >> 9;
                            //st.InpPcm_prev = st.InpPcm;
                            //音量反映
                            valL = valL * st.volume;
                            valL <<= 5;
                            valL = valL >> 3;//3 適当
                            valR = valL;
                        }
                        else
                        {
                            if (st.adpcmUpdate)
                            {
                                st.adpcmUpdate = false;
                                if (!st.N1DataFlag)
                                {
                                    int N10Data;
                                    N10Data = mem[st.adrsPtr];
                                    adpcm2pcm(st, (byte)(N10Data & 0x0F));
                                    st.N1Data = (byte)((N10Data >> 4) & 0x0F);
                                }
                                else
                                {
                                    adpcm2pcm(st, st.N1Data);
                                }
                                st.OutPcm = ((st.InpPcm << 9) - (st.InpPcm_prev << 9) + 459 * st.OutPcm) >> 9;
                                st.InpPcm_prev = st.InpPcm;
                            }
                            valR = valL = ((st.OutPcm * st.volume) >> 8);// >> 4);
                        }
                    }
                    else
                    {
                        //pcm8ppの加工処理

                        //音声データ加工
                        valL = (sbyte)mem[st.adrsPtr];
                        if (st.type == 2) valL = (short)(((byte)valL << 8) + mem[st.adrsPtr + 1]);
                        //音量反映
                        valL = valL * st.volume;
                        if (st.type != 2) valL <<= 5;
                        valL = valL >> 3;//3 適当
                        if (st.outs == 1)
                        {
                            valR = valL;
                        }
                        else
                        {
                            if (st.type != 2)
                            {
                                valR = (sbyte)mem[st.adrsPtr + 1];
                                //音量反映
                                valR = valR * st.volume;
                                valR <<= 5;
                            }
                            else
                            {
                                valR = (short)((mem[st.adrsPtr + 2] << 8) + mem[st.adrsPtr + 3]);
                                //音量反映
                                valR = valR * st.volume;
                            }
                            valR = valR >> 3;//3 適当
                        }
                    }

                    //バッファへ格納(加算)
                    if (!st.mute)
                    {
                        outputs[0][i] += valL * ((st.pan & 1) != 0 ? 1 : 0);
                        outputs[1][i] += valR * ((st.pan & 2) != 0 ? 1 : 0);
                    }

                    //ポインタ移動
                    double step = st.freq / sampleRate;
                    st.step += step;
                    while (st.step >= 1.0)
                    {
                        if (st.type != 0)
                        {
                            st.adrsPtr += (uint)st.type;
                            if (st.outs != 1) st.adrsPtr += (uint)st.type;
                        }
                        else
                        {
                            st.N1DataFlag = !st.N1DataFlag;
                            if (!st.N1DataFlag)
                                st.adrsPtr++;
                            st.adpcmUpdate = true;
                        }
                        st.step -= 1.0;
                    }
                    //エンド位置を越えたら演奏終了
                    if (st.adrsPtr >= st.endAdrs) 
                        st.play = false;

                }
            }
        }

        public override int Write(byte ChipID, int port, int adr, int data)
        {
            return 0;
        }



        public void KeyOn(int c, uint adrsPtr, int mode, int len,int d3Freq=0)
        {
            int v = (byte)(mode >> 16) & 0xff;
            if (v != 0xff)
            {
                v &= 0xf;
                ch[c].volume = volTable[v];
                ch[c].mode = (int)(((uint)ch[c].mode & 0xFF00FFFF) | (uint)(v << 16)); 
            }
            int m = (byte)(mode >> 8);
            if (m != 0xff)
            {
                ch[c].PcmKind = m;
                ch[c].freq = freqTable[m];
                ch[c].outs = outsTable[m];
                ch[c].type = typeTable[m];
                ch[c].mode = (int)(((uint)ch[c].mode & 0xFFFF00FF) | (uint)(m << 8));
                if (freqTable[m] < 0 && m >= 0xf)
                {
                    ch[c].freq = d3Freq / 256.0;
                }
            }
            int p = (byte)mode;
            if (p != 0xff)
            {
                if ((p & 3) != 0)
                {
                    ch[c].play = true;
                    ch[c].adrsPtr = adrsPtr;
                    ch[c].len = (uint)len;
                    ch[c].endAdrs = (uint)(adrsPtr + len);
                    ch[c].pan = p;
                    ch[c].mode = (int)(((uint)ch[c].mode & 0xFFFFFF00) | (uint)(p << 0));
                }
                else
                {
                    ch[c].play = false;
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

        public void SetMute(int c,bool b)
        {
            ch[c].mute = b; 
        }

        public void MountMemory(byte[] mem)
        {
            this.mem = mem;
        }



        // adpcmを入力して InpPcm の値を変化させる
        // -2047<<(4+4) <= InpPcm <= +2047<<(4+4)
        private void adpcm2pcm(chStats st, byte adpcm)
        {


            int dltL;
            dltL = dltLTBL[st.Scale];
            dltL = (dltL & ((adpcm & 4) != 0 ? -1 : 0))
                        + ((dltL >> 1) & ((adpcm & 2) != 0 ? -1 : 0))
                        + ((dltL >> 2) & ((adpcm & 1) != 0 ? -1 : 0)) + (dltL >> 3);
            int sign = (adpcm & 8) != 0 ? -1 : 0;
            dltL = (dltL ^ sign) + (sign & 1);
            st.Pcm += dltL;


            if ((uint)(st.Pcm + MAXPCMVAL) > (uint)(MAXPCMVAL * 2))
            {
                if ((int)(st.Pcm + MAXPCMVAL) >= (int)(MAXPCMVAL * 2))
                {
                    st.Pcm = MAXPCMVAL;
                }
                else
                {
                    st.Pcm = -MAXPCMVAL;
                }
            }

            st.InpPcm = (st.Pcm & -4)//(int)0xFFFFFFFC) 
                << (4 + 4);

            st.Scale += DCT[adpcm];
            if ((uint)st.Scale > (uint)48)
            {
                if ((int)st.Scale >= (int)48)
                {
                    st.Scale = 48;
                }
                else
                {
                    st.Scale = 0;
                }
            }
        }

        // pcm16を入力して InpPcm の値を変化させる
        // -2047<<(4+4) <= InpPcm <= +2047<<(4+4)
        private void pcm16_2pcm(chStats st,int pcm16)
        {
            st.Pcm += pcm16 - st.Pcm16Prev;
            st.Pcm16Prev = pcm16;


            if ((uint)(st.Pcm + MAXPCMVAL) > (uint)(MAXPCMVAL * 2))
            {
                if ((int)(st.Pcm + MAXPCMVAL) >= (int)(MAXPCMVAL * 2))
                {
                    st.Pcm = MAXPCMVAL;
                }
                else
                {
                    st.Pcm = -MAXPCMVAL;
                }
            }

            st.InpPcm = (st.Pcm & -4)//(int)0xFFFFFFFC) 
                << (4 + 4);
        }



    }
}
