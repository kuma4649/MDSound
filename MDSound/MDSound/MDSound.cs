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
        private int[][] fmVol = new int[6][] { new int[2], new int[2], new int[2], new int[2], new int[2], new int[2] };
        private int[] fmCh3SlotVol = new int[4];
        private int[][] psgVol = new int[4][] { new int[2], new int[2], new int[2], new int[2]};
        private int[] fmKey = new int[6];


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
            for (int i = 0; i < 6; i++)
            {
                fmVol[i][0] = 0;
                fmVol[i][1] = 0;
            }
            for (int i = 0; i < 4; i++)
            {
                fmCh3SlotVol[i] = 0;
            }
            for (int i = 0; i < 4; i++)
            {
                psgVol[i][0] = 0;
                psgVol[i][1] = 0;
            }

            for (int i = 0; i < SamplingBuffer; i++)
            {


                if (frame != null) { frame(); }

                sn76489.SN76489_Update(sn76489_context, buffer2, 1);
                buffer[0][i] = (int)((double)buffer2[0][0] * 1.0);
                buffer[1][i] = (int)((double)buffer2[1][0] * 1.0);

                buffer2[0][0] = 0;
                buffer2[1][0] = 0;
                ym2612.YM2612_Update(ym2612_, buffer2, 1);
                buffer[0][i] += (int)((double)buffer2[0][0] * 1.5);
                buffer[1][i] += (int)((double)buffer2[1][0] * 1.5);

                buffer2[0][0] = 0;
                buffer2[1][0] = 0;
                ym2612.YM2612_DacAndTimers_Update(ym2612_, buffer2, 1);
                buffer[0][i] += (int)((double)buffer2[0][0] * 1.6);
                buffer[1][i] += (int)((double)buffer2[1][0] * 1.6);

                buffer[0][i] = Math.Max(Math.Min(buffer[0][i], short.MaxValue), short.MinValue);
                buffer[1][i] = Math.Max(Math.Min(buffer[1][i], short.MaxValue), short.MinValue);

                for (int ch = 0; ch < 6; ch++)
                {
                    fmVol[ch][0] = Math.Max(fmVol[ch][0], ym2612_.CHANNEL[ch].fmVol[0]);
                    fmVol[ch][1] = Math.Max(fmVol[ch][1], ym2612_.CHANNEL[ch].fmVol[1]);
                }

                for (int slot = 0; slot < 4; slot++)
                {
                    fmCh3SlotVol[slot] = Math.Max(fmCh3SlotVol[slot], ym2612_.CHANNEL[2].fmSlotVol[slot]);
                }

                for (int ch = 0; ch < 4; ch++)
                {
                    psgVol[ch][0] = Math.Max(psgVol[ch][0], sn76489_context.volume[ch][0]);
                    psgVol[ch][1] = Math.Max(psgVol[ch][1], sn76489_context.volume[ch][0]);
                }
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

        public int[][] ReadFMVolume()
        {
            return fmVol;
        }

        public int[] ReadFMCh3SlotVolume()
        {
            return fmCh3SlotVol;
        }

        public int[][] ReadPSGVolume()
        {
            return psgVol;
        }

        public int[] ReadFMKeyOn()
        {
            for (int i = 0; i < 6; i++)
            {
                fmKey[i] = ym2612_.CHANNEL[i].KeyOn;
            }
            return fmKey;
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
