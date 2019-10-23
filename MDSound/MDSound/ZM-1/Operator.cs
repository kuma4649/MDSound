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
        public ZelMusic.commonParam cp;

        private List<byte> pCMData;

        public Operator(int number,ZelMusic.commonParam cp)//, List<byte> pCMData, uint playClock, uint chipClock)
        {
            this.cp = cp;
            this.number = number;
            this.pCMData = cp.PCMData;
            fm = new Fm(this);
            pcm = new Pcm(this, pCMData);
            pcm.SetRate(cp.chipClock, cp.playClock);
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
        private bool _KeyOnFlg = false;
        public bool KeyOnFlg
        {
            get {
                return _KeyOnFlg;
            }
            set {
                _KeyOnFlg = value;
            }
        }

        public void Update(int[][] outputs, int samples)
        {
            pcm.Update(outputs, samples);
        }
    }

}
