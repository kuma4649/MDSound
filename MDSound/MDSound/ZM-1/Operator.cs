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

        private List<byte>[] pCMData;

        public Operator(List<byte>[] pCMData)
        {
            this.pCMData = pCMData;
            fm = new Fm();
            pcm = new Pcm(pCMData);
            sc = new SlotConfiguration();
        }
    }

}
