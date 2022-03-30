using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDSound
{
    public class ym2610 : Instrument
    {
        private fmgen.OPNB[] chip = new fmgen.OPNB[2];
        private const uint DefaultYM2610ClockValue = 8000000;

        public override string Name { get { return "YM2610"; } set { } }
        public override string ShortName { get { return "OPNB"; } set { } }

        public ym2610()
        {
            visVolume = new int[2][][] {
                new int[5][] { new int[2] { 0, 0 }, new int[2] { 0, 0 }, new int[2] { 0, 0 } , new int[2] { 0, 0 } , new int[2] { 0, 0 } }
                , new int[5][] { new int[2] { 0, 0 }, new int[2] { 0, 0 }, new int[2] { 0, 0 } , new int[2] { 0, 0 } , new int[2] { 0, 0 } }
            };
            //0..Main 1..FM 2..SSG 3..PCMa 4..PCMb
        }

        public override void Reset(byte ChipID)
        {
            if (chip[ChipID] == null) return;
            chip[ChipID].Reset();
        }

        public override uint Start(byte ChipID, uint clock)
        {
            chip[ChipID] = new fmgen.OPNB();
            chip[ChipID].Init(DefaultYM2610ClockValue, clock);

            return clock;
        }

        public override uint Start(byte ChipID, uint clock, uint FMClockValue, params object[] option)
        {
            chip[ChipID] = new fmgen.OPNB();
            chip[ChipID].Init(FMClockValue, clock,false, new byte[0x20ffff], 0x20ffff, new byte[0x20ffff], 0x20ffff);

            return clock;
        }

        public override void Stop(byte ChipID)
        {
            chip[ChipID] = null;
        }

        public void ChangePSGMode(byte ChipID, int mode)
        {
            chip[ChipID].ChangePSGMode(mode);
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
            visVolume[ChipID][1][0] = chip[ChipID].visVolume[0];
            visVolume[ChipID][1][1] = chip[ChipID].visVolume[1];
            visVolume[ChipID][2][0] = chip[ChipID].psg.visVolume;
            visVolume[ChipID][2][1] = chip[ChipID].psg.visVolume;
            visVolume[ChipID][3][0] = chip[ChipID].visRtmVolume[0];
            visVolume[ChipID][3][1] = chip[ChipID].visRtmVolume[1];
            visVolume[ChipID][4][0] = chip[ChipID].visAPCMVolume[0];
            visVolume[ChipID][4][1] = chip[ChipID].visAPCMVolume[1];
        }

        private int YM2610_Write(byte ChipID, uint adr, byte data)
        {
            if (chip[ChipID] == null) return 0;
            chip[ChipID].SetReg(adr, data);
            return 0;
        }

        public void YM2610_setAdpcmA(byte ChipID, byte[] _adpcma, int _adpcma_size)
        {
            if (chip[ChipID] == null) return;
            chip[ChipID].setAdpcmA(_adpcma, _adpcma_size);
        }

        public void YM2610_setAdpcmB(byte ChipID, byte[] _adpcmb, int _adpcmb_size)
        {
            if (chip[ChipID] == null) return;
            chip[ChipID].setAdpcmB(_adpcmb, _adpcmb_size);
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

        public void SetAdpcmAVolume(byte ChipID, int db)
        {
            if (chip[ChipID] == null) return;

            chip[ChipID].SetVolumeADPCMATotal(db);
        }

        public void SetAdpcmBVolume(byte ChipID, int db)
        {
            if (chip[ChipID] == null) return;

            chip[ChipID].SetVolumeADPCMB(db);
        }

        public override int Write(byte ChipID, int port, int adr, int data)
        {
            return YM2610_Write(ChipID, (uint)adr, (byte)data);
        }
    }
}
