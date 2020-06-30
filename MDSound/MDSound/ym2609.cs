using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MDSound
{

    public class ym2609 : Instrument
    {
        private fmvgen.OPNA2[] chip = new fmvgen.OPNA2[2];
        private const uint DefaultYM2609ClockValue = 8000000;
        private reverb[] reverb = new reverb[2];
        private distortion[] distortion = new distortion[2];
        private chorus[] chorus = new chorus[2];
        private const int MaxCh= 39;

        public override string Name { get { return "YM2609"; } set { } }
        public override string ShortName { get { return "OPNA2"; } set { } }

        public ym2609()
        {
            visVolume = new int[2][][] {
                new int[5][] { new int[2] { 0, 0 }, new int[2] { 0, 0 }, new int[2] { 0, 0 } , new int[2] { 0, 0 } , new int[2] { 0, 0 } }
                , new int[5][] { new int[2] { 0, 0 }, new int[2] { 0, 0 }, new int[2] { 0, 0 } , new int[2] { 0, 0 } , new int[2] { 0, 0 } }
            };
            //0..Main 1..FM 2..SSG 3..Rhm 4..PCM
        }

        public override void Reset(byte ChipID)
        {
            chip[ChipID].Reset();
        }

        public override uint Start(byte ChipID, uint clock)
        {
            reverb[ChipID] = new reverb((int)clock, MaxCh);
            distortion[ChipID] = new distortion((int)clock, MaxCh);
            chorus[ChipID] = new chorus((int)clock, MaxCh);
            chip[ChipID] = new fmvgen.OPNA2(reverb[ChipID], distortion[ChipID], chorus[ChipID]);
            chip[ChipID].Init(DefaultYM2609ClockValue, clock);

            return clock;
        }

        public override uint Start(byte ChipID, uint clock, uint FMClockValue, params object[] option)
        {
            reverb[ChipID] = new reverb((int)clock, MaxCh);
            distortion[ChipID] = new distortion((int)clock, MaxCh);
            chorus[ChipID] = new chorus((int)clock, MaxCh);
            chip[ChipID] = new fmvgen.OPNA2(reverb[ChipID], distortion[ChipID], chorus[ChipID]);

            if (option != null && option.Length > 0 && option[0] is Func<string, Stream>)
            {
                chip[ChipID].Init(FMClockValue, clock, false, (Func<string, Stream>)option[0]);
            }
            else
            {
                chip[ChipID].Init(FMClockValue, clock);
            }

            return clock;
        }

        public override void Stop(byte ChipID)
        {
            chip[ChipID] = null;
        }

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {
            int[] buffer = new int[2];
            buffer[0] = reverb[ChipID].GetDataFromPos(0)/2;
            buffer[1] = reverb[ChipID].GetDataFromPos(1)/2;

            reverb[ChipID].StoreData(0, reverb[ChipID].GetDataFromPos(0) / 2);
            reverb[ChipID].StoreData(1, reverb[ChipID].GetDataFromPos(1) / 2);
            reverb[ChipID].ClearDataAtPos();

            chip[ChipID].Mix(buffer, 1);
            for (int i = 0; i < 1; i++)
            {
                outputs[0][i] = buffer[i * 2 + 0];
                outputs[1][i] = buffer[i * 2 + 1];

                //rev[ChipID].StoreData(0, (outputs[0][i] + outputs[1][i]) / 2);
            }
            reverb[ChipID].UpdatePos();

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

        public int YM2609_Write(byte ChipID, uint adr, byte data)
        {
            if (chip[ChipID] == null) return 0;

            chip[ChipID].SetReg(adr, data);
            return 0;
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

        public void SetRhythmVolume(byte ChipID, int db)
        {
            if (chip[ChipID] == null) return;

            chip[ChipID].SetVolumeRhythmTotal(db);
        }

        public void SetAdpcmVolume(byte ChipID, int db)
        {
            if (chip[ChipID] == null) return;

            chip[ChipID].SetVolumeADPCM(db);
        }

        public void SetAdpcmA(byte ChipID, byte[] _adpcma, int _adpcma_size)
        {
            if (chip[ChipID] == null) return;
            chip[ChipID].setAdpcmA(_adpcma, _adpcma_size);
        }

        public override int Write(byte ChipID, int port, int adr, int data)
        {
            return YM2609_Write(ChipID, (uint)adr, (byte)data);
        }
    }
}
