using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDSound.ZM_1
{
    public class Operator
    {
        public Fm fm;
        public Pcm pcm;
        public SlotConfiguration sc;
        public int number;

        private List<byte> pCMData;

        public Operator(int number, List<byte> pCMData, uint playClock, uint chipClock)
        {
            this.number = number;
            this.pCMData = pCMData;
            fm = new Fm(this);
            pcm = new Pcm(this, pCMData);
            pcm.SetRate(chipClock, playClock);
            sc = new SlotConfiguration(this);
        }

        private uint _NoteByteMatrix = 0;
        public uint NoteByteMatrix
        {
            get
            {
                return _NoteByteMatrix;
            }
            set
            {
                _NoteByteMatrix = value;
            }
        }

        private byte _KeyFrqmode = 0;
        public byte KeyFrqmode
        {
            get
            {
                return _KeyFrqmode;
            }
            set
            {
                _KeyFrqmode = value;
            }
        }

        public void Update(int[][] outputs, int samples)
        {
            pcm.Update(outputs, samples);
        }
    }

}
