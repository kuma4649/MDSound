using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDSound.ZM_1
{
    public class SlotConfiguration
    {

        private byte _LeftVolume = 0;
        public byte LeftVolume
        {
            get { return _LeftVolume; }
            set { _LeftVolume = value; }
        }

        private byte _RightVolume = 0;
        public byte RightVolume
        {
            get { return _RightVolume; }
            set { _RightVolume = value; }
        }

        private byte _LFOFRQ = 0;
        public byte LFOFRQ
        {
            get { return _LFOFRQ; }
            set { _LFOFRQ = value; }
        }

        private byte _LFOSW = 0;
        public byte LFOSW
        {
            get { return _LFOSW; }
            set { _LFOSW = value; }
        }



        //TBD
        public void Write(byte address, byte data)
        {
            switch (address)
            {
                case 0x00:
                    LeftVolume = (byte)data;
                    break;
                case 0x01:
                    RightVolume = (byte)data;
                    break;
                case 0x02:
                    LFOFRQ = (byte)data;
                    break;
                case 0x03:
                    LFOSW = (byte)data;
                    break;

                default:
                    throw new ArgumentOutOfRangeException("アドレス指定が異常です");
            }
        }
    }
}
