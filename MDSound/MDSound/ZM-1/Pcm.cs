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

        public Pcm(Operator @operator, List<byte> pCMData) : base(@operator)
        {
            this.pCMData = pCMData;
            oldKeyOn = false;
            play = false;
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
                    PlayAddress |= (uint)(data << ((address - 1) * 8));
                    break;
                case 0x05:
                case 0x06:
                case 0x07:
                case 0x08:
                    StopAddress &= (uint)~(0x0000_00ff << ((address - 5) * 8));
                    StopAddress |= (uint)(data << ((address - 5) * 8));
                    break;
                case 0x09:
                case 0x0a:
                case 0x0b:
                case 0x0c:
                    LoopAddress &= (uint)~(0x0000_00ff << ((address - 9) * 8));
                    LoopAddress |= (uint)(data << ((address - 9) * 8));
                    break;
                case 0x0d:
                case 0x0e:
                case 0x0f:
                case 0x10:
                    KeyOffAddress &= (uint)~(0x0000_00ff << ((address - 13) * 8));
                    KeyOffAddress |= (uint)(data << ((address - 13) * 8));
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

        int spd = 0;

        public void Update(int[][] outputs, int samples)
        {
            for (int i = 0; i < samples; i++)
            {
                bool ko = (@operator.NoteByteMatrix & 0x80) != 0;
                if (ko)
                {
                    if (!oldKeyOn)
                    {
                        //キーが押された
                        oldKeyOn = ko;
                        playPtr = PlayAddress;
                        play = true;
                    }
                    else
                    {
                        //押されている最中
                    }
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
                    else
                    {
                        //離されている最中
                    }
                }

                if (!play) continue;

                byte d = pCMData[(int)playPtr];
                spd++;
                if (spd == 6)
                {
                    playPtr++;
                    spd = 0;
                }
                if (playPtr > StopAddress)
                {
                    if (LoopAddress != 0)
                        playPtr = LoopAddress;
                    else play = false;
                }

                outputs[0][i] += d << 4;
                outputs[1][i] += d << 4;

            }
        }


    }

}
