using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MDSound
{
    public class rev
    {
        private int[] Buf = null;
        private int Pos = 0;
        private int Delta = 0;
        public double[] SendLevel = null;
        private int currentCh = 0;

        public rev(int bufSize, int ch)
        {
            this.Buf = new int[bufSize];
            this.Pos = 0;
            this.currentCh = 0;
            SetDelta(64);

            this.SendLevel = new double[ch];
            for (int i = 0; i < ch; i++)
            {
                SetSendLevel(i, 0);
            }
        }

        public void SetDelta(int n)
        {
            this.Delta = (int)Buf.Length / 128 * Math.Max(Math.Min(n, 127), 0);
        }

        public void SetSendLevel(int ch, int n)
        {
            if (n == 0)
            {
                SendLevel[ch] = 0;
                return;
            }
            //SendLevel[ch] = 1.0 / (2 << Math.Max(Math.Min((15 - n), 15), 0));
            n = Math.Max(Math.Min(n, 15), 0);
            SendLevel[ch] = 1.0 * sl[n];
            Console.WriteLine("{0} {1}", ch, SendLevel[ch]);
        }

        private double[] sl = new double[16] {
            0.0050000 , 0.0150000 , 0.0300000 , 0.0530000 ,
            0.0680000 , 0.0800000 , 0.0960000 , 0.1300000 ,
            0.2000000 , 0.3000000 , 0.4000000 , 0.5000000 ,
            0.6000000 , 0.7000000 , 0.8000000 , 0.9000000
        };

        public int GetDataFromPos()
        {
            return Buf[Pos];
        }

        public void ClearDataAtPos()
        {
            Buf[Pos] = 0;
        }

        public void UpdatePos()
        {
            Pos = (1 + Pos) % Buf.Length;
        }

        //public void StoreData(int ch, int v)
        //{
            //int ptr = (Delta + Pos) % Buf.Length;
            //Buf[ptr] += (int)(v * SendLevel[ch]);
        //}

        public void StoreData(int v)
        {
            int ptr = (Delta + Pos) % Buf.Length;
            Buf[ptr] += (int)(v);
        }

        public void SetReg(uint adr, byte data)
        {
            if (adr == 0)
            {
                SetDelta(data & 0x7f);
            }
            else if (adr == 1)
            {
                currentCh = Math.Max(Math.Min(data & 0x3f, 30), 0);
            }
            else if (adr == 2)
            {
                SetSendLevel(currentCh, data & 0xf);
            }
        }
    }

    public class ym2609 : Instrument
    {
        private fmvgen.OPNA2[] chip = new fmvgen.OPNA2[2];
        private const uint DefaultYM2609ClockValue = 8000000;
        private rev[] rev = new rev[2];

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
            rev[ChipID] = new rev((int)clock, 31);
            chip[ChipID] = new fmvgen.OPNA2(rev[ChipID]);
            chip[ChipID].Init(DefaultYM2609ClockValue, clock);

            return clock;
        }

        public override uint Start(byte ChipID, uint clock, uint FMClockValue, params object[] option)
        {
            rev[ChipID] = new rev((int)clock, 31);
            chip[ChipID] = new fmvgen.OPNA2(rev[ChipID]);

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
            buffer[0] = rev[ChipID].GetDataFromPos()/2;
            buffer[1] = rev[ChipID].GetDataFromPos()/2;

            rev[ChipID].StoreData(rev[ChipID].GetDataFromPos() / 2);
            rev[ChipID].ClearDataAtPos();

            chip[ChipID].Mix(buffer, 1);
            for (int i = 0; i < 1; i++)
            {
                outputs[0][i] = buffer[i * 2 + 0];
                outputs[1][i] = buffer[i * 2 + 1];

                //rev[ChipID].StoreData(0, (outputs[0][i] + outputs[1][i]) / 2);
            }
            rev[ChipID].UpdatePos();

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
