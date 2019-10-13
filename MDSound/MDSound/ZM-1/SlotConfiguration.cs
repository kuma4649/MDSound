using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDSound.ZM_1
{
    public class SlotConfiguration: ChipElement
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

        private byte _LFOSW = 0;
        public byte LFOSW
        {
            get { return _LFOSW; }
            set { _LFOSW = value; }
        }

        private byte _EFF1VAL = 0;
        public byte EFF1VAL
        {
            get { return _EFF1VAL; }
            set { _EFF1VAL = value; }
        }

        private byte _EFF2VAL = 0;

        public SlotConfiguration(Operator @operator):base(@operator)
        {
        }

        public byte EFF2VAL
        {
            get { return _EFF2VAL; }
            set { _EFF2VAL = value; }
        }



        //TBD
        public override void Write(int address, int data)
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
                    LPFFilter = (byte)data;
                    break;
                case 0x04:
                    HPFFilter= (byte)data;
                    break;
                case 0x05:
                    LFOSW = (byte)data;
                    break;
                case 0x06:
                    EFF1VAL = (byte)data;
                    break;
                case 0x07:
                    EFF2VAL = (byte)data;
                    break;

                default:
                    throw new ArgumentOutOfRangeException("アドレス指定が異常です");
            }
        }
    }
}
