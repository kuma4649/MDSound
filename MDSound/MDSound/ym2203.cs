namespace MDSound
{
    public class ym2203 : Instrument
    {
        private fmgen.OPN[] chip = new fmgen.OPN[2];
        private const uint DefaultYM2203ClockValue = 3000000;

        public override void Reset(byte ChipID)
        {
            if (chip[ChipID] == null) return;
            chip[ChipID].Reset();
        }

        public override uint Start(byte ChipID, uint clock)
        {
            chip[ChipID] = new fmgen.OPN();
            chip[ChipID].Init(DefaultYM2203ClockValue, clock);

            return clock;
        }

        public uint Start(byte ChipID, uint clock, uint FMClockValue, params object[] option)
        {
            chip[ChipID] = new fmgen.OPN();
            chip[ChipID].Init(FMClockValue, clock);

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
        }

        public int YM2203_Write(byte ChipID, byte adr, byte data)
        {
            if (chip[ChipID] == null) return 0;
            chip[ChipID].SetReg(adr, data);
            return 0;
        }

        public void YM2203_SetMute(byte ChipID, int val)
        {
            fmgen.OPN YM2203 = chip[ChipID];
            if (YM2203 == null) return;


            YM2203.SetChannelMask((uint)val);
            
        }

        public void SetFMVolume(byte ChipID, int db)
        {
            if (chip[ChipID] == null) return;

            chip[ChipID].SetVolumeFM(db);
        }

        public void SetPSGVolume(byte ChipID, int db)
        {
            if (chip[ChipID] == null) return;

            chip[ChipID].SetVolumePSG(db);
        }


    }
}
