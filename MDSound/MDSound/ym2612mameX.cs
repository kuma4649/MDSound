using System;
using System.Collections.Generic;
using System.Text;

namespace MDSound
{
    public class ym2612mameX : ym2612mame
    {
        public XGMFunction XGMfunction = new XGMFunction();
        private int samplerate = 0;

        public override uint Start(byte ChipID, uint clock, uint clockValue, params object[] option)
        {
            samplerate = (int)base.Start(ChipID, clock, clockValue, option);
            return clock;
        }

        public override void Reset(byte ChipID)
        {
            XGMfunction.Reset(ChipID, samplerate);
            base.Reset(ChipID);
        }

        public override void Stop(byte ChipID)
        {
            XGMfunction.Stop(ChipID);
            base.Stop(ChipID);
        }

        public override int Write(byte ChipID, int port, int adr, int data)
        {
            XGMfunction.Write(ChipID, port, adr, data);
            return base.Write(ChipID, port, (byte)adr, (byte)data);
        }

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {
            XGMfunction.Update(ChipID, samples, Write);
            base.Update(ChipID, outputs, samples);
        }
    }
}
