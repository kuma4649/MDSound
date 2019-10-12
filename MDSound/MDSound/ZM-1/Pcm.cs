using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDSound.ZM_1
{
    public class Pcm
    {
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

        private ushort _KeyOffAddress = 0;
        public ushort KeyOffAddress
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

        private byte _LPFFilter = 0;
        public byte LPFFilter
        {
            get { return _LPFFilter; }
            set { _LPFFilter = value; }
        }

        private byte _HPFFilter = 0;
        public byte HPFFilter
        {
            get { return _HPFFilter; }
            set { _HPFFilter = value; }
        }

        private byte _EffectConfiguration = 0;
        public byte EffectConfiguration
        {
            get { return _EffectConfiguration; }
            set { _EffectConfiguration = value; }
        }

        private List<byte>[] pCMData;

        public Pcm(List<byte>[] pCMData)
        {
            this.pCMData = pCMData;
        }


        public void Write(byte address,int data)
        {
            switch (address)
            {
                case 0x00:
                    PCMMode = (byte)data;
                    break;
                case 0x01:
                    PlayAddress = (uint)data;
                    break;
                case 0x05:
                    StopAddress = (uint)data;
                    break;
                case 0x09:
                    LoopAddress = (uint)data;
                    break;
                case 0x0d:
                    KeyOffAddress = (ushort)data;
                    break;
                case 0x12:
                    PCMConfig = (byte)data;
                    break;
                case 0x13:
                    LPFFilter = (byte)data;
                    break;
                case 0x14:
                    HPFFilter = (byte)data;
                    break;
                case 0x15:
                    EffectConfiguration = (byte)data;
                    break;

                default:
                    throw new ArgumentOutOfRangeException("アドレス指定が異常です");
            }
        }

    }

}
