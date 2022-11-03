using System;
using System.Collections.Generic;
using System.IO;

namespace MDSound
{
    public class ym2608 : Instrument
    {
        private fmgen.OPNA[] chip = new fmgen.OPNA[2];
        //private fmgen.OPNA2[] chip = new fmgen.OPNA2[2];
        private const uint DefaultYM2608ClockValue = 8000000;

        public override string Name { get { return "YM2608"; } set { } }
        public override string ShortName { get { return "OPNA"; } set { } }

        public ym2608()
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
            chip[ChipID] = new fmgen.OPNA(ChipID);
            //chip[ChipID] = new fmgen.OPNA2();
            chip[ChipID].Init(DefaultYM2608ClockValue, clock);

            return clock;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ChipID"></param>
        /// <param name="clock"></param>
        /// <param name="FMClockValue"></param>
        /// <param name="option">リズム音ファイルのパス(終端に\をつけること)</param>
        /// <returns></returns>
        public override uint Start(byte ChipID, uint clock, uint FMClockValue, params object[] option)
        {
            chip[ChipID] = new fmgen.OPNA(ChipID);
            //chip[ChipID] = new fmgen.OPNA2();
            if (option != null && option.Length > 0)
            {
                if (option.Length > 1 && option[1] is List<byte[]>)
                {
                    chip[ChipID].presetRhythmPCMData = (List<byte[]>)option[1];
                }

                if (option[0] is Func<string, Stream>)
                {
                    chip[ChipID].Init(FMClockValue, clock, false, (Func<string, Stream>)option[0]);
                }
                else if (option[0] is string)
                    chip[ChipID].Init(FMClockValue, clock, false, (string)option[0]);
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

        public void ChangePSGMode(byte ChipID, int mode)
        {
            chip[ChipID].ChangePSGMode(mode);
        }

        public string ReadErrMsg(byte ChipID)
        {
            return chip[ChipID].ReadErrMsg();
        }

        int[] buffer = new int[2];

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {
            buffer[0] = 0;
            buffer[1] = 0;
            chip[ChipID].Mix(buffer, 1);
            for (int i = 0; i < 1; i++)
            {
                outputs[0][i] = buffer[i * 2 + 0];
                outputs[1][i] = buffer[i * 2 + 1];
#if LIMIT_CHECKER
                if (outputs[0][i] > 0x7fff || outputs[0][i] < -0x8000)
                 Console.Write("limit over [{0:d8}] : [{1:d8}]\r\n", outputs[0][i], outputs[1][i]);
#endif
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

        private int YM2608_Write(byte ChipID, uint adr, byte data)
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

        public override int Write(byte ChipID, int port, int adr, int data)
        {
            return YM2608_Write(ChipID, (uint)adr, (byte)data);
        }

        public byte[] GetADPCMBuffer(byte ChipID)
        {
            return chip[ChipID].GetADPCMBuffer();
        }

        public uint ReadStatusEx(byte ChipID)
        {
            return chip[ChipID].ReadStatusEx();
        }
    }
}
