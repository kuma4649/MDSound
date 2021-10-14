using System;
using System.Collections.Generic;
using System.Text;

namespace MDSound
{
    public class VRC6 : Instrument
    {
        public override string Name { get { return "VRC6"; } set { } }
        public override string ShortName { get { return "VRC6"; } set { } }

        private double apu_clock_rest = 0;

        public override void Reset(byte ChipID)
        {
            vrc6[ChipID].Reset();
        }

        public override uint Start(byte ChipID, uint clock)
        {
            return Start(ChipID, clock, 100, null);
        }

        public override uint Start(byte ChipID, uint sampleRate, uint masterClock, params object[] option)
        {
            vrc6[ChipID].SetClock(masterClock);
            vrc6[ChipID].SetRate(sampleRate);

            if (option != null && option.Length > 0)
            {
                for (int i = 0; i < option.Length; i++)
                {
                    if (option[i] is Tuple<int, int>)
                    {
                        Tuple<int, int> item = (Tuple<int, int>)option[i];
                        vrc6[ChipID].SetOption(item.Item1, item.Item2);
                    }
                }
            }
            SetVolumeVRC6(0);

            return sampleRate;
        }

        public override void Stop(byte ChipID)
        {
            vrc6[ChipID].Reset();
        }

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {
            b[0] = 0;
            b[1] = 0;

            double apu_clock_per_sample = 0;
            apu_clock_per_sample = vrc6[ChipID].clock / vrc6[ChipID].rate;
            apu_clock_rest += apu_clock_per_sample;
            int apu_clocks = (int)(apu_clock_rest);
            if (apu_clocks > 0) apu_clock_rest -= (double)(apu_clocks);

            vrc6[ChipID].Tick((uint)apu_clocks);
            vrc6[ChipID].Render(b);

            outputs[0][0] += (short)((Limit(b[0], 0x7fff, -0x8000) * volume) >> 12);//12以下だと音割れる
            outputs[1][0] += (short)((Limit(b[1], 0x7fff, -0x8000) * volume) >> 12);//12以下だと音割れる

        }

        public override int Write(byte ChipID, int port, int adr, int data)
        {
            vrc6[ChipID].Write((uint)adr, (uint)data);
            return 0;
        }





        private np.chip.nes_vrc6[] vrc6 = new np.chip.nes_vrc6[2];
        private int[] b = new int[2];
        private int volume = 0;

        public VRC6()
        {
            vrc6 = new np.chip.nes_vrc6[] { new np.chip.nes_vrc6(), new np.chip.nes_vrc6() };
            SetVolumeVRC6(0);
        }

        public void SetVolumeVRC6(int db)
        {
            db = Math.Min(db, 20);
            if (db > -192)
                volume = (int)(16384.0 * Math.Pow(10.0, db / 40.0));
            else
                volume = 0;
        }

        public static int Limit(int v, int max, int min)
        {
            return v > max ? max : (v < min ? min : v);
        }

    }
}
