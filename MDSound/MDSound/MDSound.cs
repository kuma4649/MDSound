using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDSound
{
    public class MDSound
    {
        private const int DefaultSamplingRate = 44100;
        private const int DefaultSamplingBuffer = 512;
        private const int DefaultPSGClockValue = 3579545;
        private const int DefaultFMClockValue = 7670454;

        private int SamplingRate = 44100;
        private int SamplingBuffer = 512;
        private int PSGClockValue = 3579545;
        private int FMClockValue = 7670454;
        private sn76489 sn76489 = null;
        private SN76489_Context sn76489_context = null;
        private ym2612 ym2612 = null;
        private ym2612_ ym2612_ = null;
        private int[][] buffer = null;


        public MDSound()
        {
            Init(DefaultSamplingRate, DefaultSamplingBuffer, DefaultFMClockValue, DefaultPSGClockValue);
        }

        public MDSound(int SamplingRate,int SamplingBuffer,int FMClockValue, int PSGClockValue)
        {
            Init(SamplingRate, SamplingBuffer, FMClockValue, PSGClockValue);
        }

        public void Init(int SamplingRate, int SamplingBuffer, int FMClockValue, int PSGClockValue)
        {

            this.SamplingRate = SamplingRate;
            this.SamplingBuffer = SamplingBuffer;
            this.PSGClockValue = PSGClockValue;
            this.FMClockValue = FMClockValue;

            sn76489 = new sn76489();
            sn76489_context = sn76489.SN76489_Init(PSGClockValue, SamplingRate);
            sn76489.SN76489_Reset(sn76489_context);

            ym2612 = new ym2612();
            ym2612_ = ym2612.YM2612_Init(FMClockValue, SamplingRate, 0);
            ym2612.YM2612_Reset(ym2612_);

            buffer = new int[2][] { new int[SamplingBuffer], new int[SamplingBuffer] };

        }

        public int[][] Update()
        {

            sn76489.SN76489_Update(sn76489_context, buffer, SamplingBuffer);
            ym2612.YM2612_Update(ym2612_, buffer, SamplingBuffer);
            ym2612.YM2612_DacAndTimers_Update(ym2612_, buffer, SamplingBuffer);

            return buffer;

        }

        public void Write(byte data)
        {
            sn76489.SN76489_Write(sn76489_context, data);
        }

        public void Write(byte port,byte adr,byte data)
        {
            ym2612.YM2612_Write(ym2612_, (byte)(0 + (port & 1) * 2), adr);
            ym2612.YM2612_Write(ym2612_, (byte)(1 + (port & 1) * 2), data);
        }

        public int[] ReadPSGRegister()
        {
            return sn76489_context.Registers;
        }

        public int[][] ReadFMRegister()
        {
            return ym2612_.REG;
        }

    }
}
