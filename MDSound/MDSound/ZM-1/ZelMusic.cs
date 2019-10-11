using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDSound
{
    public class ZelMusic : Instrument
    {
        public override string Name { get { return "ZelMusic"; } set { } }
        public override string ShortName { get { return "ZM-1"; } set { } }

        public override void Reset(byte ChipID)
        {
            throw new NotImplementedException();
        }

        public override uint Start(byte ChipID, uint clock)
        {
            throw new NotImplementedException();
        }

        public override uint Start(byte ChipID, uint clock, uint ClockValue, params object[] option)
        {
            throw new NotImplementedException();
        }

        public override void Stop(byte ChipID)
        {
            throw new NotImplementedException();
        }

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {
            throw new NotImplementedException();
        }

        public override int Write(byte ChipID, int port, int adr, int data)
        {
            throw new NotImplementedException();
        }
    }
}
