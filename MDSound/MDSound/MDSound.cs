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
        private const uint DefaultC140ClockValue = 21390;
        private const c140.C140_TYPE DefaultC140Type = c140.C140_TYPE.ASIC219;

        private uint SamplingRate = 44100;
        private uint SamplingBuffer = 512;

        private uint[] PSGClockValue = new uint[2] { 3579545, 3579545 };
        private uint[] FMClockValue = new uint[2] { 7670454, 7670454 };
        private uint[] rf5c164ClockValue = new uint[2] { 12500000, 12500000 };
        private uint[] pwmClockValue = new uint[2] { 23011361, 23011361 };
        private uint[] c140ClockValue = new uint[2] { 21390, 21390 };
        private c140.C140_TYPE[] c140Type = new c140.C140_TYPE[2] { c140.C140_TYPE.ASIC219, c140.C140_TYPE.ASIC219 };

        private int[] YM2612Volume = new int[2] { 170, 170 };
        private int[] SN76489Volume = new int[2] { 100, 100 };
        private int[] RF5C164Volume = new int[2] { 90, 90 };
        private int[] PWMVolume =new int[2] { 100, 100 };
        private int[] C140Volume = new int[2] { 100, 100 };


        private Chip[] insts = null;

        private sn76489 sn76489 = null;
        private ym2612 ym2612 = null;
        private scd_pcm rf5c164 = null;
        private pwm pwm = null;
        private c140 c140 = null;

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

        public enum enmInstrumentType : int
        {
            None=0,
            YM2612,
            SN76489,
            RF5C164,
            PWM,
            C140
        }

        public class Chip
        {
            public enmInstrumentType type = enmInstrumentType.None;
            public byte ID = 0;
            public uint ClockValue = 0;
            public object[] OptionValues = null;
        }

        public MDSound()
        {
            Init(DefaultSamplingRate, DefaultSamplingBuffer, null);
        }

        public MDSound(uint SamplingRate, uint SamplingBuffer, Chip[] insts)
        {
            Init(SamplingRate, SamplingBuffer, insts);
        }

        public void Init(uint SamplingRate,uint SamplingBuffer, Chip[] insts)
        {
            lock (lockobj)
            {
                this.SamplingRate = SamplingRate;
                this.SamplingBuffer = SamplingBuffer;
                this.insts = insts;

                buffer = new int[2][] { new int[SamplingBuffer], new int[SamplingBuffer] };
                buffer2 = new int[2][] { new int[1], new int[1] };

                psgMask = 15;
                fmMask = 0;

                incFlag = false;


                ym2612 = null;
                sn76489 = null;
                rf5c164 = null;
                pwm = null;
                c140 = null;

                if (insts == null) return;

                foreach (Chip inst in insts)
                {
                    switch (inst.type) {
                        case enmInstrumentType.YM2612:
                            if (inst.ClockValue != uint.MaxValue && inst.ClockValue != 0)
                            {
                                FMClockValue[inst.ID] = inst.ClockValue;
                                if (ym2612 == null) ym2612 = new ym2612();
                                ym2612.Start(inst.ID, SamplingRate, FMClockValue[inst.ID]);
                                ym2612.Reset(inst.ID);
                            }
                            break;
                        case enmInstrumentType.SN76489:
                            if (inst.ClockValue != uint.MaxValue && inst.ClockValue != 0)
                            {
                                PSGClockValue[inst.ID] = inst.ClockValue;
                                if (sn76489 == null) sn76489 = new sn76489();
                                sn76489.Start(inst.ID, SamplingRate, PSGClockValue[inst.ID]);
                                sn76489.Reset(inst.ID);
                            }
                            break;
                        case enmInstrumentType.RF5C164:
                            if (inst.ClockValue != uint.MaxValue && inst.ClockValue != 0)
                            {
                                rf5c164ClockValue[inst.ID] = inst.ClockValue;
                                if (rf5c164 == null) rf5c164 = new scd_pcm();
                                rf5c164.Start(inst.ID, rf5c164ClockValue[inst.ID]);
                            }
                            break;
                        case enmInstrumentType.PWM:
                            if (inst.ClockValue != uint.MaxValue && inst.ClockValue != 0)
                            {
                                pwmClockValue [inst.ID]= inst.ClockValue;
                                if (pwm == null) pwm = new pwm();
                                pwm.Start(inst.ID, pwmClockValue[inst.ID]);
                            }
                            break;
                        case enmInstrumentType.C140:
                            if (inst.ClockValue != uint.MaxValue && inst.ClockValue != 0)
                            {
                                c140ClockValue[inst.ID] = inst.ClockValue;
                                c140Type[inst.ID] = (c140.C140_TYPE)inst.OptionValues[0];
                                if (c140 == null) c140 = new c140();
                                c140.Start(inst.ID, c140ClockValue[inst.ID], c140Type[inst.ID]);
                            }
                            break;
                    }
                }

            }
        }

        public int[][] Update()
        {

            lock (lockobj)
            {
                if (sn76489 != null) sn76489.Update(0, buffer, (int)SamplingBuffer);
                if (ym2612 != null) ym2612.Update(0, buffer, (int)SamplingBuffer);

                int[][] pcmBuffer = new int[2][] { new int[SamplingBuffer], new int[SamplingBuffer] };

                if (rf5c164 != null)
                {
                    rf5c164.Update(0, pcmBuffer, (int)SamplingBuffer);
                    for (int i = 0; i < SamplingBuffer; i++)
                    {
                        buffer[0][i] += pcmBuffer[0][i];
                        buffer[1][i] += pcmBuffer[1][i];
                    }
                }

                if (pwm != null)
                {
                    pwm.Update(0, pcmBuffer, (int)SamplingBuffer);
                    for (int i = 0; i < SamplingBuffer; i++)
                    {
                        buffer[0][i] += pcmBuffer[0][i];
                        buffer[1][i] += pcmBuffer[1][i];
                    }
                }

                if (c140 != null)
                {
                    c140.Update(0, pcmBuffer, (int)SamplingBuffer);
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
            lock (lockobj)
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

                    foreach (Chip inst in insts)
                    {
                        switch (inst.type) {
                            case enmInstrumentType.SN76489:
                                buffer2[0][0] = 0;
                                buffer2[1][0] = 0;
                                sn76489.Update(inst.ID, buffer2, 1);
                                a += (int)(buffer2[0][0] * SN76489Volume[inst.ID] / 100.0);
                                b += (int)(buffer2[1][0] * SN76489Volume[inst.ID] / 100.0);
                                break;
                            case enmInstrumentType.YM2612:
                                buffer2[0][0] = 0;
                                buffer2[1][0] = 0;
                                ym2612.Update(0, buffer2, 1);
                                a += (int)(buffer2[0][0] * YM2612Volume[inst.ID] / 100.0);
                                b += (int)(buffer2[1][0] * YM2612Volume[inst.ID] / 100.0);
                                break;
                            case enmInstrumentType.RF5C164:
                                buffer2[0][0] = 0;
                                buffer2[1][0] = 0;
                                rf5c164.Update(0, buffer2, 1);
                                a += (int)(buffer2[0][0] * RF5C164Volume[inst.ID] / 100.0);
                                b += (int)(buffer2[1][0] * RF5C164Volume[inst.ID] / 100.0);
                                break;
                            case enmInstrumentType.PWM:
                                buffer2[0][0] = 0;
                                buffer2[1][0] = 0;
                                pwm.Update(0, buffer2, 1);
                                a += (int)(buffer2[0][0] * PWMVolume[inst.ID] / 100.0);
                                b += (int)(buffer2[1][0] * PWMVolume[inst.ID] / 100.0);
                                break;
                            case enmInstrumentType.C140:
                                buffer2[0][0] = 0;
                                buffer2[1][0] = 0;
                                c140.Update(0, buffer2, 1);
                                a += (int)(buffer2[0][0] * C140Volume[inst.ID] / 100.0);
                                b += (int)(buffer2[1][0] * C140Volume[inst.ID] / 100.0);
                                break;
                        }
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
                        fmVol[ch][0] = Math.Max(fmVol[ch][0], ym2612.YM2612_Chip[0].CHANNEL[ch].fmVol[0]);
                        fmVol[ch][1] = Math.Max(fmVol[ch][1], ym2612.YM2612_Chip[0].CHANNEL[ch].fmVol[1]);
                    }

                    for (int slot = 0; slot < 4; slot++)
                    {
                        fmCh3SlotVol[slot] = Math.Max(fmCh3SlotVol[slot], ym2612.YM2612_Chip[0].CHANNEL[2].fmSlotVol[slot]);
                    }

                    for (int ch = 0; ch < 4; ch++)
                    {
                        psgVol[ch][0] = Math.Max(psgVol[ch][0], sn76489.SN76489_Chip[0].volume[ch][0]);
                        psgVol[ch][1] = Math.Max(psgVol[ch][1], sn76489.SN76489_Chip[0].volume[ch][0]);
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


        public void setVolume(enmInstrumentType type, byte ChipID, int vol)
        {
            switch (type)
            {
                case enmInstrumentType.SN76489:
                    SN76489Volume[ChipID] = vol;
                    break;
                case enmInstrumentType.YM2612:
                    YM2612Volume[ChipID] = vol;
                    break;
                case enmInstrumentType.RF5C164:
                    RF5C164Volume[ChipID] = vol;
                    break;
                case enmInstrumentType.PWM:
                    PWMVolume[ChipID] = vol;
                    break;
                case enmInstrumentType.C140:
                    C140Volume[ChipID] = vol;
                    break;
            }
        }
        

        public void WriteSN76489(byte data)
        {
            lock (lockobj)
            {
                if (sn76489 == null) return;

                sn76489.SN76489_Write(0, data);
            }
        }

        public void WriteYM2612(byte port, byte adr, byte data)
        {
            lock (lockobj)
            {
                if (ym2612 == null) return;

                ym2612.YM2612_Write(0, (byte)(0 + (port & 1) * 2), adr);
                ym2612.YM2612_Write(0, (byte)(1 + (port & 1) * 2), data);
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

                rf5c164.rf5c164_w(chipid, adr, data);
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

        public void WriteC140(byte chipid, uint offset, byte data)
        {
            lock (lockobj)
            {
                if (c140 == null) return;

                c140.c140_w(chipid, offset, data);
            }
        }

        public void WriteC140PCMData(byte chipid, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            lock (lockobj)
            {
                if (c140 == null) return;

                c140.c140_write_rom2(chipid, ROMSize, DataStart, DataLength, ROMData, SrcStartAdr);
            }
        }
        

        public int[] ReadPSGRegister()
        {
            lock (lockobj)
            {
                return sn76489.SN76489_Chip[0].Registers;
            }
        }

        public int[][] ReadFMRegister()
        {
            lock (lockobj)
            {
                return ym2612.YM2612_Chip[0].REG;
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
                    fmKey[i] = ym2612.YM2612_Chip[0].CHANNEL[i].KeyOn;
                }
                return fmKey;
            }
        }


        public void setPSGMask(int ch)
        {
            lock (lockobj)
            {
                psgMask &= ~ch;
                if (sn76489 != null) sn76489.SN76489_SetMute(0,psgMask);
            }
        }

        public void setFMMask(int ch)
        {
            lock (lockobj)
            {
                fmMask |= ch;
                if (ym2612 != null) ym2612.YM2612_SetMute(0, fmMask);
            }
        }

        public void resetPSGMask(int ch)
        {
            lock (lockobj)
            {
                psgMask |= ch;
                if (sn76489 != null) sn76489.SN76489_SetMute(0, psgMask);
            }
        }

        public void resetFMMask(int ch)
        {
            lock (lockobj)
            {
                fmMask &= ~ch;
                if (ym2612 != null) ym2612.YM2612_SetMute(0, fmMask);
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
