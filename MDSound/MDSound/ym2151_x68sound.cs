using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDSound
{
    public class ym2151_x68sound : Instrument
    {
        private const uint DefaultYM2151ClockValue = 4000000;//X68000 clock 

        public override string Name { get { return "YM2151x68sound"; } set { } }
        public override string ShortName { get { return "OPMx"; } set { } }

        public override uint Start(byte ChipID, uint clock)
        {
            return Start(ChipID, clock, DefaultYM2151ClockValue, null);
        }

        public override uint Start(byte ChipID, uint sampleRate, uint ClockValue, params object[] option)
        {
            if (ChipID > 1) return 0;

            x68sound[ChipID] = new NX68Sound.X68Sound();
            sound_Iocs[ChipID] = new NX68Sound.sound_iocs(x68sound[ChipID]);

            if (option != null)
            {
                if (option.Length > 0 && option[0] != null) opmflag = (int)option[0];
                if (option.Length > 1 && option[1] != null) adpcmflag = (int)option[1];
                if (option.Length > 2 && option[2] != null) pcmbuf = (int)option[2];
            }

            x68sound[ChipID].X68Sound_StartPcm((int)sampleRate, opmflag, adpcmflag, pcmbuf);
            x68sound[ChipID].X68Sound_OpmClock((int)ClockValue);

            return sampleRate;
        }

        public override void Stop(byte ChipID)
        {
            if (x68sound[ChipID] == null) return;

            x68sound[ChipID].X68Sound_Free();

            x68sound[ChipID] = null;
            sound_Iocs[ChipID] = null;
        }

        public override void Reset(byte ChipID)
        {
            if (x68sound[ChipID] == null) return;

            x68sound[ChipID].X68Sound_Reset();
        }

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {
            if (x68sound[ChipID] == null) return;

            for (int i = 0; i < samples; i++)
            {
                x68sound[ChipID].X68Sound_GetPcm(buf[ChipID], 0, samples * 2);
                outputs[0][i] = buf[ChipID][0];
                outputs[1][i] = buf[ChipID][1];
            }
        }

        public void Update(byte ChipID, int[][] outputs, int samples, Action<Action, bool> oneFrameproc)
        {
            if (x68sound[ChipID] == null) return;

            for (int i = 0; i < samples; i++)
            {
                x68sound[ChipID].X68Sound_GetPcm(buf[ChipID], 0, samples * 2, oneFrameproc);
                outputs[0][i] = buf[ChipID][0];
                outputs[1][i] = buf[ChipID][1];
            }
        }

        public override int Write(byte ChipID, int port, int adr, int data)
        {
            if (x68sound[ChipID] == null) return 0;
            sound_Iocs[ChipID]._iocs_opmset(adr, data);
            return 0;
        }

        public void SetMask(byte ChipID,int n)
        {
            if (x68sound[ChipID] == null) return;
            x68sound[ChipID].X68Sound_SetMask(n);
        }

        public NX68Sound.X68Sound[] x68sound = new NX68Sound.X68Sound[2] { null, null };
        public NX68Sound.sound_iocs[] sound_Iocs = new NX68Sound.sound_iocs[2] { null, null };

        private short[][] buf = new short[2][] { new short[2], new short[2] };
        private int opmflag = 1;
        private int adpcmflag = 0;
        private int pcmbuf = 5;

    }
}
