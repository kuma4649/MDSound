using System;
using System.Collections.Generic;
using System.Text;

namespace MDSound
{
    public class SinWave : Instrument
    {
        public override string Name { get { return "SinWave"; } set { } }
        public override string ShortName { get { return "SIN"; } set { } }

        public SinWave()
        {
            visVolume = new int[2][][] {
                new int[1][] { new int[2] { 0, 0 } }
                , new int[1][] { new int[2] { 0, 0 } }
            };
            //0..Main
        }

        public override void Reset(byte ChipID)
        {
            if (chip[ChipID] == null)
            {
                chip[ChipID] = new SinWaveGen();
            }
            //chip[ChipID].render = false;
        }

        public override uint Start(byte ChipID, uint clock)
        {
            return Start(ChipID, clock, DefaultClockValue, null);
        }

        public override uint Start(byte ChipID, uint clock, uint ClockValue, params object[] option)
        {
            Reset(ChipID);
            chip[ChipID].clock = (double)clock;
            chip[ChipID].render = true;

            return clock;//SamplingRate
        }

        public override void Stop(byte ChipID)
        {
            if (chip[ChipID] == null) return;
            chip[ChipID].render = false;
        }

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {
            if (chip[ChipID] == null) return;
            chip[ChipID].Update(outputs, samples);
        }

        public override int Write(byte ChipID, int port, int adr, int data)
        {
            if (chip[ChipID] == null) return 0;
            return chip[ChipID].Write(data);
        }




        private const uint DefaultClockValue = 0;
        private SinWaveGen[] chip = new SinWaveGen[2];

        private class SinWaveGen
        {
            public bool render = true;

            public double clock = 44100.0;
            public double tone = 440.0;
            public double delta = 0;

            public void Update(int[][] outputs, int samples)
            {
                if (!render) return;

                for (int i = 0; i < samples; i++)
                {
                    double d = (Math.Sin(delta * Math.PI / 180.0) * 4000.0);
                    int n = (int)d;
                    delta += 360.0f * tone / clock;

                    outputs[0][i] = n;
                    outputs[1][i] = n;
                }
            }

            public int Write(int data)
            {
                render = (data != 0);
                return 0;
            }
        }
    }
}
