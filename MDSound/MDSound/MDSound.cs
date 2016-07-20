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
        private int[][] psgVol = new int[4][] { new int[2], new int[2], new int[2], new int[2] };
        private int[] fmKey = new int[6];
        private int[][] rf5c164Vol = new int[8][] { new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2], new int[2] };

        private bool incFlag = false;
        private object lockobj = new object();

        private int YM2612Volume = 170;
        private int SN76489Volume = 100;
        private int RF5C164Volume =90;
        private int PWMVolume = 100;



        public MDSound()
        {
            Init(DefaultSamplingRate, DefaultSamplingBuffer, DefaultFMClockValue, DefaultPSGClockValue, DefaultRf5c164ClockValue, DefaultPwmClockValue);
        }

        public MDSound(uint SamplingRate, uint SamplingBuffer, uint FMClockValue, uint PSGClockValue, uint rf5c164ClockValue, uint pwmClockValue)
        {
            Init(SamplingRate, SamplingBuffer, FMClockValue, PSGClockValue, rf5c164ClockValue, pwmClockValue);
        }

        public void setVolume(int ym2612Vol , int sn76489Vol , int rf5c164Vol , int pwmVol)
        {
            YM2612Volume = ym2612Vol;
            SN76489Volume = sn76489Vol;
            RF5C164Volume = rf5c164Vol;
            PWMVolume = pwmVol;
        }

    public void Init(uint SamplingRate, uint SamplingBuffer, uint FMClockValue, uint PSGClockValue, uint rf5c164ClockValue, uint pwmClockValue)
        {
            lock (lockobj)
            {
                this.SamplingRate = SamplingRate;
                this.SamplingBuffer = SamplingBuffer;
                this.PSGClockValue = PSGClockValue;
                this.FMClockValue = FMClockValue;
                this.rf5c164ClockValue = rf5c164ClockValue;
                this.pwmClockValue = pwmClockValue;

                sn76489 = null;
                if (PSGClockValue != uint.MaxValue && PSGClockValue != 0)
                {
                    sn76489 = new sn76489();
                    sn76489_context = sn76489.SN76489_Init(PSGClockValue, SamplingRate);
                    sn76489.SN76489_Reset(sn76489_context);
                }

                ym2612 = null;
                if (FMClockValue != uint.MaxValue && FMClockValue!=0)
                {
                    ym2612 = new ym2612();
                    ym2612_ = ym2612.YM2612_Init(FMClockValue, SamplingRate, 0);
                    ym2612.YM2612_Reset(ym2612_);
                }

                rf5c164 = null;
                if (rf5c164ClockValue != uint.MaxValue && rf5c164ClockValue!=0)
                {
                    rf5c164 = new scd_pcm();
                    rf5c164.device_start_rf5c164(0, rf5c164ClockValue);
                }

                pwm = null;
                if (pwmClockValue != uint.MaxValue && pwmClockValue != 0)
                {
                    pwm = new pwm();
                    pwm.device_start_pwm(0, pwmClockValue);
                }

                buffer = new int[2][] { new int[SamplingBuffer], new int[SamplingBuffer] };
                buffer2 = new int[2][] { new int[1], new int[1] };

                psgMask = 15;
                fmMask = 0;

                incFlag = false;
            }
        }

        public int[][] Update()
        {

            lock(lockobj)
            {
                if (sn76489 != null) sn76489.SN76489_Update(sn76489_context, buffer, (int)SamplingBuffer);
                if (ym2612 != null) ym2612.YM2612_Update(ym2612_, buffer, (int)SamplingBuffer);
                if (ym2612 != null) ym2612.YM2612_DacAndTimers_Update(ym2612_, buffer, (int)SamplingBuffer);

                int[][] pcmBuffer = new int[2][] { new int[SamplingBuffer], new int[SamplingBuffer] };

                if (rf5c164 != null)
                {
                    rf5c164.rf5c164_update(0, pcmBuffer, (int)SamplingBuffer);
                    for (int i = 0; i < SamplingBuffer; i++)
                    {
                        buffer[0][i] += pcmBuffer[0][i];
                        buffer[1][i] += pcmBuffer[1][i];
                    }
                }

                if (pwm != null)
                {
                    pwm.pwm_update(0, pcmBuffer, (int)SamplingBuffer);
                    for (int i = 0; i < SamplingBuffer; i++)
                    {
                        buffer[0][i] += pcmBuffer[0][i];
                        buffer[1][i] += pcmBuffer[1][i];
                    }
                }

                return buffer;
            }
        }

        public int Update2(short[] buf, int offset, int sampleCount, Action frame)
        {
            lock(lockobj)
            {
                int a, b;

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

                for (int i = 0; i < sampleCount / 2; i++)
                {

                    if (frame != null) frame();

                    a = 0;
                    b = 0;

                    if (sn76489 != null)
                    {
                        buffer2[0][0] = 0;
                        buffer2[1][0] = 0;
                        sn76489.SN76489_Update(sn76489_context, buffer2, 1);
                        a += (int)(buffer2[0][0] * SN76489Volume / 100.0);
                        b += (int)(buffer2[1][0] * SN76489Volume / 100.0);
                    }

                    if (ym2612 != null)
                    {
                        buffer2[0][0] = 0;
                        buffer2[1][0] = 0;
                        ym2612.YM2612_Update(ym2612_, buffer2, 1);
                        a += (int)(buffer2[0][0] * YM2612Volume / 100.0);
                        b += (int)(buffer2[1][0] * YM2612Volume / 100.0);

                        buffer2[0][0] = 0;
                        buffer2[1][0] = 0;
                        ym2612.YM2612_DacAndTimers_Update(ym2612_, buffer2, 1);
                        a += (int)(buffer2[0][0] * YM2612Volume / 100.0);
                        b += (int)(buffer2[1][0] * YM2612Volume / 100.0);
                    }

                    if (rf5c164 != null)
                    {
                        buffer2[0][0] = 0;
                        buffer2[1][0] = 0;
                        rf5c164.rf5c164_update(0, buffer2, 1);
                        a += (int)(buffer2[0][0] * RF5C164Volume / 100.0);
                        b += (int)(buffer2[1][0] * RF5C164Volume / 100.0);
                    }

                    if (pwm != null)
                    {
                        buffer2[0][0] = 0;
                        buffer2[1][0] = 0;
                        pwm.pwm_update(0, buffer2, 1);
                        a += (int)(buffer2[0][0] * PWMVolume / 100.0);
                        b += (int)(buffer2[1][0] * PWMVolume / 100.0);
                    }

                    if (incFlag)
                    {
                        buf[offset + i * 2 + 0] += (short)Math.Max(Math.Min(a, short.MaxValue), short.MinValue);
                        buf[offset + i * 2 + 1] += (short)Math.Max(Math.Min(b, short.MaxValue), short.MinValue);
                    }
                    else
                    {
                        buf[offset + i * 2 + 0] = (short)Math.Max(Math.Min(a, short.MaxValue), short.MinValue);
                        buf[offset + i * 2 + 1] = (short)Math.Max(Math.Min(b, short.MaxValue), short.MinValue);
                    }

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
                        rf5c164Vol[ch][0] = Math.Max(rf5c164Vol[ch][0], (int)(rf5c164.PCM_Chip[0].Channel[ch].Data * rf5c164.PCM_Chip[0].Channel[ch].MUL_L));
                        rf5c164Vol[ch][1] = Math.Max(rf5c164Vol[ch][1], (int)(rf5c164.PCM_Chip[0].Channel[ch].Data * rf5c164.PCM_Chip[0].Channel[ch].MUL_R));
                    }

                }

                return sampleCount;

            }
        }

        public void WriteSN76489(byte data)
        {
            lock (lockobj)
            {
                if (sn76489 == null) return;

                sn76489.SN76489_Write(sn76489_context, data);
            }
        }

        public void WriteYM2612(byte port, byte adr, byte data)
        {
            lock (lockobj)
            {
                if (ym2612 == null) return;

                ym2612.YM2612_Write(ym2612_, (byte)(0 + (port & 1) * 2), adr);
                ym2612.YM2612_Write(ym2612_, (byte)(1 + (port & 1) * 2), data);
            }
        }

        public void WritePWM(byte chipid, byte adr, uint data)
        {
            lock (lockobj)
            {
                if (pwm == null) return;

                pwm.pwm_chn_w(chipid, adr, data);// (byte)((adr & 0xf0)>>4),(uint)((adr & 0xf)*0x100+data));
            }
        }

        public void WriteRF5C164(byte chipid, byte adr, byte data)
        {
            lock (lockobj)
            {
                if (rf5c164 == null) return;

                rf5c164.PCM_Write_Reg(chipid, adr, data);
            }
        }

        public void WriteRF5C164PCMData(byte chipid, uint RAMStartAdr, uint RAMDataLength, byte[] SrcData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (rf5c164 == null) return;

                rf5c164.rf5c164_write_ram2(chipid, RAMStartAdr, RAMDataLength, SrcData, SrcStartAdr);
            }
        }

        public void WriteRF5C164MemW(byte chipid, uint offset, byte data)
        {
            lock (lockobj)
            {
                if (rf5c164 == null) return;

                rf5c164.rf5c164_mem_w(chipid, offset, data);
            }
        }

        public int[] ReadPSGRegister()
        {
            lock (lockobj)
            {
                return sn76489_context.Registers;
            }
        }

        public int[][] ReadFMRegister()
        {
            lock (lockobj)
            {
                return ym2612_.REG;
            }
        }

        public scd_pcm.pcm_chip_ ReadRf5c164Register()
        {
            lock (lockobj)
            {
                if (rf5c164 == null || rf5c164.PCM_Chip == null || rf5c164.PCM_Chip.Length < 1) return null;
                return rf5c164.PCM_Chip[0];
            }
        }

        public int[][] ReadRf5c164Volume()
        {
            lock (lockobj)
            {
                return rf5c164Vol;
            }
        }

        public int[][] ReadFMVolume()
        {
            lock (lockobj)
            {
                return fmVol;
            }
        }

        public int[] ReadFMCh3SlotVolume()
        {
            lock (lockobj)
            {
                return fmCh3SlotVol;
            }
        }

        public int[][] ReadPSGVolume()
        {
            lock (lockobj)
            {
                return psgVol;
            }
        }

        public int[] ReadFMKeyOn()
        {
            lock (lockobj)
            {
                for (int i = 0; i < 6; i++)
                {
                    fmKey[i] = ym2612_.CHANNEL[i].KeyOn;
                }
                return fmKey;
            }
        }

        public void setPSGMask(int ch)
        {
            lock (lockobj)
            {
                psgMask &= ~ch;
                if (sn76489 != null) sn76489.SN76489_SetMute(sn76489_context, psgMask);
            }
        }

        public void setFMMask(int ch)
        {
            lock (lockobj)
            {
                fmMask |= ch;
                if (ym2612 != null) ym2612.YM2612_SetMute(ym2612_, fmMask);
            }
        }

        public void resetPSGMask(int ch)
        {
            lock (lockobj)
            {
                psgMask |= ch;
                if (sn76489 != null) sn76489.SN76489_SetMute(sn76489_context, psgMask);
            }
        }

        public void resetFMMask(int ch)
        {
            lock (lockobj)
            {
                fmMask &= ~ch;
                if (ym2612 != null) ym2612.YM2612_SetMute(ym2612_, fmMask);
            }
        }

        public int getTotalVolumeL()
        {
            lock (lockobj)
            {
                int v = 0;
                for (int i = 0; i < buffer[0].Length; i++)
                {
                    v = Math.Max(v, abs(buffer[0][i]));
                }
                return v;
            }
        }

        public int getTotalVolumeR()
        {
            lock (lockobj)
            {
                int v = 0;
                for (int i = 0; i < buffer[1].Length; i++)
                {
                    v = Math.Max(v, abs(buffer[1][i]));
                }
                return v;
            }
        }

        public void setIncFlag()
        {
            lock (lockobj)
            {
                incFlag = true;
            }
        }

        public void resetIncFlag()
        {
            lock (lockobj)
            {
                incFlag = false;
            }
        }

        private int abs(int n)
        {
            return (n > 0) ? n : -n;
        }

    }
}
