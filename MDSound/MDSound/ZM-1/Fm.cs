using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDSound.ZM_1
{
    public class Fm
    {
        private byte _AR = 0;
        public byte AR
        {
            get { return _AR; }
            set { _AR = (byte)(value & 0x1f); }
        }

        private byte _D1R = 0;
        public byte D1R
        {
            get { return _D1R; }
            set { _D1R = (byte)(value & 0x1f); }
        }

        private byte _D2R = 0;
        public byte D2R
        {
            get { return _D2R; }
            set { _D2R = (byte)(value & 0x1f); }
        }

        private byte _D1L = 0;
        public byte D1L
        {
            get { return _D1L; }
            set { _D1L = (byte)(value & 0x0f); }
        }

        private byte _RR = 0;
        public byte RR
        {
            get { return _RR; }
            set { _RR = (byte)(value & 0x0f); }
        }

        private byte _TL = 0;
        public byte TL
        {
            get { return _TL; }
            set { _TL = (byte)(value & 0x7f); }
        }

        private byte _MUL = 0;
        public byte MUL
        {
            get { return _MUL; }
            set { _MUL = (byte)(value & 0x0f); }
        }

        private byte _DT12 = 0;
        public byte DT12
        {
            get { return _DT12; }
            set { _DT12 = (byte)(value & 0x1f); }
        }

        private byte _KsAmsen = 0;
        public byte KsAmsen
        {
            get { return _KsAmsen; }
            set { _KsAmsen = (byte)(value & 0x07); }
        }

        private byte _PmsAms = 0;
        public byte PmsAms
        {
            get { return _PmsAms; }
            set { _PmsAms = (byte)(value & 0x1f); }
        }

        public void Write(byte adress ,byte data)
        {
            switch (adress)
            {
                case 0x00:
                    AR = data;
                    break;
                case 0x01:
                    D1R = data;
                    break;
                case 0x02:
                    D2R = data;
                    break;
                case 0x03:
                    D1L = data;
                    break;
                case 0x04:
                    RR = data;
                    break;
                case 0x05:
                    TL = data;
                    break;
                case 0x06:
                    MUL = data;
                    break;
                case 0x07:
                    DT12 = data;
                    break;
                case 0x08:
                    KsAmsen = data;
                    break;
                case 0x09:
                    PmsAms = data;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("アドレス指定が異常です");
            }
        }
    }

}
