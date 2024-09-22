using System;
using System.Collections.Generic;
using System.Text;

namespace MDSound
{
    public class zxbeep : Instrument
    {
        public override string Name { get { return "ZXBeep"; } set { } }
        public override string ShortName { get { return "Beeper"; } set { } }

        private zx[] chip = new zx[2] { new zx(), new zx() };

        public override void Reset(byte ChipID)
        {
            chip[ChipID].val = 0;
            chip[ChipID].vol = 0xfff;
        }

        public override uint Start(byte ChipID, uint clock)
        {
            Reset(ChipID);
            return clock;
        }

        public override uint Start(byte ChipID, uint clock, uint ClockValue, params object[] option)
        {
            Reset(ChipID);
            return clock;
        }

        public override void Stop(byte ChipID)
        {
            Reset(ChipID);
        }

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {
            for (int i = 0; i < 1; i++)
            {
                outputs[0][i] = chip[ChipID].val;
                outputs[1][i] = chip[ChipID].val;
            }
        }

        public override int Write(byte ChipID, int port, int adr, int data)
        {
            chip[ChipID].val = (short)(chip[ChipID].val != 0 ? 0 : chip[ChipID].vol);
            return 0;
        }

    }

    public class zx
    {
        public short val = 0;
        public short vol = 0xfff;
    }
}
