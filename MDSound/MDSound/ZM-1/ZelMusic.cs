using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDSound.ZM_1
{
    public class ZelMusic : Instrument
    {
        public override string Name { get { return "ZelMusic"; } set { } }
        public override string ShortName { get { return "ZM-1"; } set { } }

        public const int MAX_OPERATOR = 48;
        private Operator[] ope = null;

        public override void Reset(byte ChipID)
        {
            ope = new Operator[MAX_OPERATOR];
            for (int i = 0; i < MAX_OPERATOR; i++) ope[i] = new Operator();
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
