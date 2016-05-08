using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDSound
{
    public class MDSound
    {
        private const uint DefaultSamplingRate = 44100;
        private const uint DefaultSamplingBuffer = 512;
        private const uint DefaultPSGClockValue = 3579545;
        private const uint DefaultFMClockValue = 7670454;
        private const uint DefaultRf5c164ClockValue = 12500000;
        private const uint DefaultPwmClockValue = 23011361;

        private uint SamplingRate = 44100;
        private uint SamplingBuffer = 512;
        private uint PSGClockValue = 3579545;
        private uint FMClockValue = 7670454;
        private uint rf5c164ClockValue = 12500000;
        private uint pwmClockValue = 23011361;

        private sn76489 sn76489 = null;
        private SN76489_Context sn76489_context = null;
        private ym2612 ym2612 = null;
        private ym2612_ ym2612_ = null;
        private scd_pcm rf5c164 = null;
        private pwm pwm = null;
        private int[][] buffer = null;
        private int[][] buffer2 = null;
        private int psgMask = 15;// psgはmuteを基準にしているのでビットが逆です
        private int fmMask = 0;
        private int[][] fmVol = new int[6][] { new int[2], new int[2], new int[2], new int[2], new int[2], new int[2] };
        private int[] fmCh3SlotVol = new int[4];
        private int[][] psgVol = new int[4][] { new int[2], new int[2], new int[2], new int[2]};
        private int[] fmKey = new int[6];
        private int[][] rf5c164Vol = new int[8][] { new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2] };


        public MDSound()
        {
            Init(DefaultSamplingRate, DefaultSamplingBuffer, DefaultFMClockValue, DefaultPSGClockValue, DefaultRf5c164ClockValue, DefaultPwmClockValue);
        }

        public MDSound(uint SamplingRate,uint SamplingBuffer,uint FMClockValue, uint PSGClockValue, uint rf5c164ClockValue,uint pwmClockValue)
        {
            Init(SamplingRate, SamplingBuffer, FMClockValue, PSGClockValue, rf5c164ClockValue, pwmClockValue);
        }

        public void Init(uint SamplingRate, uint SamplingBuffer, uint FMClockValue, uint PSGClockValue,uint rf5c164ClockValue,uint pwmClockValue)
        {

            this.SamplingRate = SamplingRate;
            this.SamplingBuffer = SamplingBuffer;
            this.PSGClockValue = PSGClockValue;
            this.FMClockValue = FMClockValue;
            this.rf5c164ClockValue = rf5c164ClockValue;
            this.pwmClockValue = pwmClockValue;

            sn76489 = new sn76489();
            sn76489_context = sn76489.SN76489_Init(PSGClockValue, SamplingRate);
            sn76489.SN76489_Reset(sn76489_context);

            ym2612 = new ym2612();
            ym2612_ = ym2612.YM2612_Init(FMClockValue, SamplingRate, 0);
            ym2612.YM2612_Reset(ym2612_);

            rf5c164 = new scd_pcm();
            rf5c164.device_start_rf5c164(0, rf5c164ClockValue);

            pwm = new pwm();
            pwm.device_start_pwm(0, pwmClockValue);

            buffer = new int[2][] { new int[SamplingBuffer], new int[SamplingBuffer] };
            buffer2 = new int[2][] { new int[1], new int[1] };

            psgMask = 15; 
            fmMask = 0;

        }

        public int[][] Update()
        {

            sn76489.SN76489_Update(sn76489_context, buffer, (int)SamplingBuffer);
            ym2612.YM2612_Update(ym2612_, buffer, (int)SamplingBuffer);
            ym2612.YM2612_DacAndTimers_Update(ym2612_, buffer, (int)SamplingBuffer);
            int[][] pcmBuffer = new int[2][] { new int[SamplingBuffer], new int[SamplingBuffer] };
            rf5c164.rf5c164_update(0, pcmBuffer, (int)SamplingBuffer);
            for (int i = 0; i < SamplingBuffer; i++)
            {
                buffer[0][i] += pcmBuffer[0][i];
                buffer[1][i] += pcmBuffer[1][i];
            }
            pwm.pwm_update(0, pcmBuffer, (int)SamplingBuffer);
            for (int i = 0; i < SamplingBuffer; i++)
            {
                buffer[0][i] += pcmBuffer[0][i];
                buffer[1][i] += pcmBuffer[1][i];
            }

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
            for (int i = 0; i < 8; i++)
            {
                rf5c164Vol[i][0] = 0;
                rf5c164Vol[i][1] = 0;
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
                buffer[0][i] += (int)((double)buffer2[0][0] * 1.7);
                buffer[1][i] += (int)((double)buffer2[1][0] * 1.7);

                buffer2[0][0] = 0;
                buffer2[1][0] = 0;
                ym2612.YM2612_DacAndTimers_Update(ym2612_, buffer2, 1);
                buffer[0][i] += (int)((double)buffer2[0][0] * 1.6);
                buffer[1][i] += (int)((double)buffer2[1][0] * 1.6);

                buffer2[0][0] = 0;
                buffer2[1][0] = 0;
                rf5c164.rf5c164_update(0, buffer2, 1);
                buffer[0][i] += (int)((double)buffer2[0][0] * 0.9);
                buffer[1][i] += (int)((double)buffer2[1][0] * 0.9);

                buffer2[0][0] = 0;
                buffer2[1][0] = 0;
                pwm.pwm_update(0, buffer2, 1);
                buffer[0][i] += (int)((double)buffer2[0][0] * 1.0);
                buffer[1][i] += (int)((double)buffer2[1][0] * 1.0);

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

                for (int ch = 0; ch < 8; ch++)
                {
                    rf5c164Vol[ch][0] = Math.Max(rf5c164Vol[ch][0], (int)(rf5c164.PCM_Chip[0].Channel[ch].Data*rf5c164.PCM_Chip[0].Channel[ch].MUL_L));
                    rf5c164Vol[ch][1] = Math.Max(rf5c164Vol[ch][1], (int)(rf5c164.PCM_Chip[0].Channel[ch].Data * rf5c164.PCM_Chip[0].Channel[ch].MUL_R));
                }

            }

            return buffer;

        }

        public void WriteSN76489(byte data)
        {
            sn76489.SN76489_Write(sn76489_context, data);
        }

        public void WriteYM2612(byte port,byte adr,byte data)
        {
            ym2612.YM2612_Write(ym2612_, (byte)(0 + (port & 1) * 2), adr);
            ym2612.YM2612_Write(ym2612_, (byte)(1 + (port & 1) * 2), data);
        }

        public void WritePWM(byte chipid, byte adr, uint data)
        {
            pwm.pwm_chn_w(chipid, adr, data);// (byte)((adr & 0xf0)>>4),(uint)((adr & 0xf)*0x100+data));
        }

        public void WriteRF5C164(byte chipid, byte adr, byte data)
        {
            rf5c164.PCM_Write_Reg(chipid, adr, data);
        }

        public void WriteRF5C164PCMData(byte chipid, uint RAMStartAdr, uint RAMDataLength, byte[] SrcData, uint SrcStartAdr)
        {
            rf5c164.rf5c164_write_ram2(chipid, RAMStartAdr, RAMDataLength, SrcData, SrcStartAdr);
        }

        public void WriteRF5C164MemW(byte chipid, uint offset, byte data)
        {
            rf5c164.rf5c164_mem_w(chipid, offset, data);
        }

        public int[] ReadPSGRegister()
        {
            return sn76489_context.Registers;
        }

        public int[][] ReadFMRegister()
        {
            return ym2612_.REG;
        }

        public scd_pcm.pcm_chip_ ReadRf5c164Register()
        {
            return rf5c164.PCM_Chip[0];
        }

        public int[][] ReadRf5c164Volume()
        {
            return rf5c164Vol;
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
