using System;
using System.Collections.Generic;
using System.Text;

namespace MDSound
{
    public class ym2612mame : Instrument
    {
        private const int MAX_CHIPS = 2;
        private const uint DefaultFMClockValue = 7670454;
        public mame.fm2612[] YM2612_Chip = new mame.fm2612[MAX_CHIPS] { null, null };
        private mame.fm2612.YM2612[] ym2612 = new mame.fm2612.YM2612[MAX_CHIPS];

        public override string Name { get { return "YM2612mame"; } set { } }
        public override string ShortName { get { return "OPN2mame"; } set { } }

        public ym2612mame()
        {
            visVolume = new int[2][][] {
                new int[1][] { new int[2] { 0, 0 } }
                , new int[1][] { new int[2] { 0, 0 } }
            };
            //0..Main
        }

        public override void Reset(byte ChipID)
        {
            if (YM2612_Chip[ChipID] == null) return;
            //if (ym2612[ChipID] == null) return;

            YM2612_Chip[ChipID].ym2612_reset_chip(ym2612[ChipID]);
        }

        public override uint Start(byte ChipID, uint clock)
        {
            YM2612_Chip[ChipID] = new mame.fm2612();
            ym2612[ChipID] = YM2612_Chip[ChipID].ym2612_init(new mame.fm2612.YM2612(), (int)DefaultFMClockValue, (int)clock, null, null);

            return clock;
        }

        public override uint Start(byte ChipID, uint clock, uint ClockValue, params object[] option)
        {
            YM2612_Chip[ChipID] = new mame.fm2612();
            ym2612[ChipID] = YM2612_Chip[ChipID].ym2612_init(new mame.fm2612.YM2612(), (int)ClockValue, (int)clock, null, null);
            YM2612_Chip[ChipID].ym2612_update_request = ym2612_update_request;

            return clock;
        }

        void ym2612_update_request(byte ChipID, fm.FM_base param)
        {
            YM2612_Chip[ChipID].ym2612_update_one((fm.FM_base)ym2612[ChipID], new int[2][], 0);
        }

        public override void Stop(byte ChipID)
        {
            YM2612_Chip[ChipID] = null;
        }

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {
            YM2612_Chip[ChipID].ym2612_update_one(ym2612[ChipID], outputs, samples);

            visVolume[ChipID][0][0] = outputs[0][0];
            visVolume[ChipID][0][1] = outputs[1][0];
        }

        public override int Write(byte ChipID, int port, int adr, int data)
        {
            if (YM2612_Chip[ChipID] == null) return 0;

            return YM2612_Chip[ChipID].ym2612_write(ChipID, ym2612[ChipID], (byte)adr, (byte)data);
        }

        public void SetMute(byte ChipID, uint mask)
        {
            if (YM2612_Chip[ChipID] == null) return;
            YM2612_Chip[ChipID].ym2612_set_mutemask(ChipID, ym2612[ChipID], mask);
        }
    }
}
