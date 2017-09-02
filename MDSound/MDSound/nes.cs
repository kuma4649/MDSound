using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDSound
{
    public class nes : Instrument
    {
        public override void Reset(byte ChipID)
        {
            throw new NotImplementedException();
        }

        public override uint Start(byte ChipID, uint clock)
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
    }
}
