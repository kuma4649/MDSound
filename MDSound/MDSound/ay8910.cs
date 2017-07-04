using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDSound
{
    public class ay8910 : Instrument
    {
        private fmgen.PSG[] chip = new fmgen.PSG[2];
        private const uint DefaultAY8910ClockValue = 1789750;

        public ay8910()
        {
            visVolume = new int[2][][] {
                new int[1][] { new int[2] { 0, 0 } }
                , new int[1][] { new int[2] { 0, 0 } }
            };
            //0..Main
        }

        public override void Reset(byte ChipID)
        {
            if (chip[ChipID] == null) return;
            chip[ChipID].Reset();
        }

        public override uint Start(byte ChipID, uint clock)
        {
            chip[ChipID] = new fmgen.PSG();
            chip[ChipID].SetClock((int)DefaultAY8910ClockValue, (int)clock);

            return clock;
        }

        public uint Start(byte ChipID, uint clock, uint PSGClockValue, params object[] option)
        {
            chip[ChipID] = new fmgen.PSG();
            chip[ChipID].SetClock((int)PSGClockValue, (int)clock);

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

        public int AY8910_Write(byte ChipID, byte adr, byte data)
        {
            if (chip[ChipID] == null) return 0;
            chip[ChipID].SetReg(adr, data);
            return 0;
        }

        public void AY8910_SetMute(byte ChipID, int val)
        {
            fmgen.PSG PSG = chip[ChipID];
            if (PSG == null) return;


            PSG.SetChannelMask(val);

        }

        public void SetVolume(byte ChipID, int db)
        {
            if (chip[ChipID] == null) return;

            chip[ChipID].SetVolume(db);
        }


    }
}
