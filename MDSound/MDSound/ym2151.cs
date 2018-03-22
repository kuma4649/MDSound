using System;

namespace MDSound
{
    public class ym2151 : Instrument
    {
        private fmgen.OPM[] chip = new fmgen.OPM[2];
        private const uint DefaultYM2151ClockValue = 3579545;

        public override string Name { get { return "YM2151"; } set { } }
        public override string ShortName { get { return "OPM"; } set { } }

        public ym2151()
        {
            visVolume = new int[2][][] {
                new int[1][] { new int[2] { 0, 0 } }
                , new int[1][] { new int[2] { 0, 0 } }
            };
        }

        public override void Reset(byte ChipID)
        {
            if (chip[ChipID] == null) return;
            chip[ChipID].Reset();
        }

        public override uint Start(byte ChipID, uint clock)
        {
            chip[ChipID] = new fmgen.OPM();
            chip[ChipID].Init(DefaultYM2151ClockValue,clock);

            return clock;
        }

        public override uint Start(byte ChipID, uint clock, uint FMClockValue, params object[] option)
        {
            chip[ChipID] = new fmgen.OPM();
            chip[ChipID].Init(FMClockValue,clock);

            return clock;
        }

        public override void Stop(byte ChipID)
        {
            chip[ChipID] = null;
        }

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {
            if (chip[ChipID] == null) return;
            int[] buffer = new int[2];
            buffer[0] = 0;
            buffer[1] = 0;
            chip[ChipID].Mix(buffer, 1);
            for (int i = 0; i < 1; i++)
            {
                outputs[0][i] = buffer[i * 2 + 0];
                outputs[1][i] = buffer[i * 2 + 1];
                //Console.Write("[{0:d8}] : [{1:d8}] [{2}]\r\n", outputs[0][i], outputs[1][i],i);
            }

            visVolume[ChipID][0][0] = outputs[0][0];
            visVolume[ChipID][0][1] = outputs[1][0];
        }

        private int YM2151_Write(byte ChipID, byte adr, byte data)
        {
            if (chip[ChipID] == null) return 0;

            chip[ChipID].SetReg(adr, data);
            return 0;
        }

        public override int Write(byte ChipID, int port, int adr, int data)
        {
            return YM2151_Write(ChipID, (byte)adr, (byte)data);
        }
    }
}
