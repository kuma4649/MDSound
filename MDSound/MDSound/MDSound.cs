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
        private int[][] buffer2 = null;
        private int psgMask = 15;// psgはmuteを基準にしているのでビットが逆です
        private int fmMask = 0;


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
            buffer2 = new int[2][] { new int[1], new int[1] };

            psgMask = 15; 
            fmMask = 0;


        }

        public int[][] Update()
        {

            sn76489.SN76489_Update(sn76489_context, buffer, SamplingBuffer);
            ym2612.YM2612_Update(ym2612_, buffer, SamplingBuffer);
            ym2612.YM2612_DacAndTimers_Update(ym2612_, buffer, SamplingBuffer);

            return buffer;

        }

        public int[][] Update2(Action frame)
        {
            for (int i = 0; i < SamplingBuffer; i++)
            {

                if (frame != null) { frame(); }

                sn76489.SN76489_Update(sn76489_context, buffer2, 1);
                buffer[0][i] = (int)((double)buffer2[0][0] * 0.9);
                buffer[1][i] = (int)((double)buffer2[1][0] * 0.9);

                buffer2[0][0] = 0;
                buffer2[1][0] = 0;
                ym2612.YM2612_Update(ym2612_, buffer2, 1);
                buffer[0][i] += (int)((double)buffer2[0][0] * 1.6);
                buffer[1][i] += (int)((double)buffer2[1][0] * 1.6);

                buffer2[0][0] = 0;
                buffer2[1][0] = 0;
                ym2612.YM2612_DacAndTimers_Update(ym2612_, buffer2, 1);
                buffer[0][i] += buffer2[0][0];
                buffer[1][i] += buffer2[1][0];
            }

            return buffer;

        }

        public void WritePSG(byte data)
        {
            sn76489.SN76489_Write(sn76489_context, data);
        }

        public void WriteFM(byte port,byte adr,byte data)
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

        public void setPSGMask(int ch)
        {
            psgMask &= ~ch;
            sn76489.SN76489_SetMute(sn76489_context, psgMask);
        }

        public void setFMMask(int ch)
        {
            fmMask |= ch;
            ym2612.YM2612_SetMute(ym2612_,fmMask);
        }

        public void resetPSGMask(int ch)
        {
            psgMask |= ch;
            sn76489.SN76489_SetMute(sn76489_context, psgMask);
        }

        public void resetFMMask(int ch)
        {
            fmMask &= ~ch;
            ym2612.YM2612_SetMute(ym2612_, fmMask);
        }

        public int getTotalVolumeL()
        {
            int v = 0;
            for(int i = 0; i < buffer[0].Length; i++)
            {
                v = Math.Max(v, abs(buffer[0][i]));
            }
            return v;

        }

        public int getTotalVolumeR()
        {
            int v = 0;
            for (int i = 0; i < buffer[1].Length; i++)
            {
                v = Math.Max(v, abs(buffer[1][i]));
            }
            return v;

        }

        private int abs(int n)
        {
            return (n > 0) ? n : -n;
        }

    }
}
