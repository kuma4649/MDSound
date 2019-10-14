using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDSound.ZM_1
{
    public class Pcm : ChipElement
    {
        private bool oldKeyOn = false;
        private uint playPtr;
        private bool play;
        private uint adplbase;

        private int adplc;          // 周波数変換用変数
        private int adpld;
        private uint deltan;

        private byte _PCMMode = 0;
        public byte PCMMode
        {
            get { return _PCMMode; }
            set { _PCMMode = value; }
        }

        private uint _PlayAddress = 0;
        public uint PlayAddress
        {
            get { return _PlayAddress; }
            set { _PlayAddress = value; }
        }

        private uint _StopAddress = 0;
        public uint StopAddress
        {
            get { return _StopAddress; }
            set { _StopAddress = value; }
        }

        private uint _LoopAddress = 0;
        public uint LoopAddress
        {
            get { return _LoopAddress; }
            set { _LoopAddress = value; }
        }

        private uint _KeyOffAddress = 0;
        public uint KeyOffAddress
        {
            get { return _KeyOffAddress; }
            set { _KeyOffAddress = value; }
        }

        private byte _PCMConfig = 0;
        public byte PCMConfig
        {
            get { return _PCMConfig; }
            set { _PCMConfig = value; }
        }

        private byte _EffectConfiguration = 0;
        public byte EffectConfiguration
        {
            get { return _EffectConfiguration; }
            set { _EffectConfiguration = value; }
        }

        private List<byte> pCMData;

        private static int[] deltaKcTable = new int[]
        {
             76296,  80784,  85540,  90580,  90580,  95920, 101576, 107568,
            53784*2, 56958*2, 60322*2, 63724*2, 63724*2, 67660*2, 71658*2, 76296*2
        };

        public Pcm(Operator @operator, List<byte> pCMData) : base(@operator)
        {
            this.pCMData = pCMData;
            oldKeyOn = false;
            play = false;

            deltan = 256;
        }

        //fmgen
        public void SetRate(uint chipClock, uint playClock)
        {
            adplbase = (uint)((int)(8192.0 * (chipClock / 72.0) / playClock));
            adpld = (int)(deltan * adplbase >> 16);
        }

        public override void Write(int address,int data)
        {
            switch (address)
            {
                case 0x00:
                    PCMMode = (byte)data;
                    break;
                case 0x01:
                case 0x02:
                case 0x03:
                case 0x04:
                    PlayAddress &= (uint)~(0x0000_00ff << ((address - 1) * 8));
                    PlayAddress |= (uint)((byte)data << ((address - 1) * 8));
                    break;
                case 0x05:
                case 0x06:
                case 0x07:
                case 0x08:
                    StopAddress &= (uint)~(0x0000_00ff << ((address - 5) * 8));
                    StopAddress |= (uint)((byte)data << ((address - 5) * 8));
                    break;
                case 0x09:
                case 0x0a:
                case 0x0b:
                case 0x0c:
                    LoopAddress &= (uint)~(0x0000_00ff << ((address - 9) * 8));
                    LoopAddress |= (uint)((byte)data << ((address - 9) * 8));
                    break;
                case 0x0d:
                case 0x0e:
                case 0x0f:
                case 0x10:
                    KeyOffAddress &= (uint)~(0x0000_00ff << ((address - 13) * 8));
                    KeyOffAddress |= (uint)((byte)data << ((address - 13) * 8));
                    break;
                case 0x12:
                    PCMConfig = (byte)data;
                    break;
                case 0x13:
                    EffectConfiguration = (byte)data;
                    break;

                default:
                    throw new ArgumentOutOfRangeException("アドレス指定が異常です");
            }
        }

        public void Update(int[][] outputs, int samples)
        {
            bool ko = (@operator.KeyFrqmode & 0x80) != 0;
            if (ko)
            {
                if (!oldKeyOn)
                {
                    //キーが押された
                    oldKeyOn = ko;
                    playPtr = PlayAddress;
                    play = true;
                }
                //else
                //{
                //押されている最中
                //}
            }
            else
            {
                if (oldKeyOn)
                {
                    //キーが離された
                    oldKeyOn = ko;
                    if (KeyOffAddress != 0) playPtr = KeyOffAddress;
                    else play = false;
                }
                //else
                //{
                //離されている最中
                //}
            }

            if (!play) return;

            if ((@operator.KeyFrqmode & 0x40) == 0)
            {
                int oct = (byte)((@operator.NoteByteMatrix >> 16) & 0x7);
                int note = (byte)((@operator.NoteByteMatrix >> 8) & 0xf);
                int kf = (byte)(@operator.NoteByteMatrix & 0x3f);
                deltan = (uint)((deltaKcTable[note] >> (10 - (oct + 3))) & 0xffff);
                adpld = (int)(deltan * adplbase >> 16);
            }
            else
            {

            }

            if (adpld <= 8192)      // fplay < fsamp
            {
                for (int i = 0; i < samples; i++)
                {
                    byte d = (byte)(playPtr >= pCMData.Count ? 0 : pCMData[(int)playPtr]);
                    if (adplc < 0)
                    {
                        adplc += 8192;
                        playPtr++;
                        if (playPtr > StopAddress)
                        {
                            if (LoopAddress != 0)
                                playPtr = LoopAddress;
                            else play = false;
                        }
                    }
                    int s = d;// apout;
                    outputs[0][i] += s << 4;
                    outputs[1][i] += s << 4;
                    adplc -= adpld;
                }
                return;
            }

            // fplay > fsamp	(adpld = fplay/famp*8192)
            int t = (-8192 * 8192) / adpld;
            for (int i = 0; i < samples; i++)
            {
                byte d = (byte)(playPtr >= pCMData.Count ? 0 : pCMData[(int)playPtr]);
                int s = d * (8192 + adplc);
                while (adplc < 0)
                {
                    d = (byte)(playPtr >= pCMData.Count ? 0 : pCMData[(int)playPtr]);
                    playPtr++;
                    if (playPtr > StopAddress)
                    {
                        if (LoopAddress != 0)
                            playPtr = LoopAddress;
                        else play = false;
                    }

                    if (!play) return;
                    s -= d * Math.Max(adplc, t);
                    adplc -= t;
                }
                adplc -= 8192;
                s >>= 13;

                outputs[0][i] += s << 4;
                outputs[1][i] += s << 4;
            }

        }


    }

}
