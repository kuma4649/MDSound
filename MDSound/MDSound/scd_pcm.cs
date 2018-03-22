using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDSound
{
    public class scd_pcm : Instrument
    {
        public class pcm_chip_
        {
            public float Rate;
            public int Smpl0Patch;
            public int Enable;
            public int Cur_Chan;
            public int Bank;

            public pcm_chan_[] Channel=new pcm_chan_[8] { new pcm_chan_(), new pcm_chan_(), new pcm_chan_(), new pcm_chan_(), new pcm_chan_(), new pcm_chan_(), new pcm_chan_(), new pcm_chan_() };
            public ulong RAMSize;
            public byte[] RAM;

        };

        public class pcm_chan_
        {
            public uint ENV;       /* envelope register */
            public uint PAN;       /* pan register */
            public uint MUL_L;     /* envelope & pan product letf */
            public uint MUL_R;     /* envelope & pan product right */
            public uint St_Addr;   /* start address register */
            public uint Loop_Addr; /* loop address register */
            public uint Addr;      /* current address register */
            public uint Step;      /* frequency register */
            public uint Step_B;    /* frequency register binaire */
            public uint Enable;    /* channel on/off register */
            public int Data;               /* wave data */
            public uint Muted;
        };

        private const int PCM_STEP_SHIFT = 11;

        //private const int MAX_CHIPS = 0x02;

        public pcm_chip_[] PCM_Chip = new pcm_chip_[0x02] { new pcm_chip_(), new pcm_chip_() };//MAX_CHIPS

        /**
         * PCM_Init(): Initialize the PCM chip.
         * @param Rate Sample rate.
         * @return 0 if successful.
         */
        private int PCM_Init(byte ChipID, uint Rate)
        {
            pcm_chip_ chip = PCM_Chip[ChipID];
            int i;

            chip.Smpl0Patch = 0;
            for (i = 0; i < 8; i++)
                chip.Channel[i].Muted = 0x00;

            chip.RAMSize = 64 * 1024;
            chip.RAM = new byte[chip.RAMSize];
            PCM_Reset(ChipID);
            PCM_Set_Rate(ChipID, Rate);

            return 0;
        }

        /**
         * PCM_Reset(): Reset the PCM chip.
         */
        private void PCM_Reset(byte ChipID)
        {
            pcm_chip_ chip = PCM_Chip[ChipID];
            int i;
            pcm_chan_ chan;

            // Clear the PCM memory.
            for (ulong j = 0; j < chip.RAMSize; j++) chip.RAM[j] = 0x00;

            chip.Enable = 0;
            chip.Cur_Chan = 0;
            chip.Bank = 0;

            /* clear channel registers */
            for (i = 0; i < 8; i++)
            {
                chan = chip.Channel[i];
                chan.Enable = 0;
                chan.ENV = 0;
                chan.PAN = 0;
                chan.St_Addr = 0;
                chan.Addr = 0;
                chan.Loop_Addr = 0;
                chan.Step = 0;
                chan.Step_B = 0;
                chan.Data = 0;
            }
        }

        /**
         * PCM_Set_Rate(): Change the PCM sample rate.
         * @param Rate New sample rate.
         */
        private void PCM_Set_Rate(byte ChipID, uint Rate)
        {
            pcm_chip_ chip = PCM_Chip[ChipID];
            int i;

            if (Rate == 0)
                return;

            //chip->Rate = (float) (32 * 1024) / (float) Rate;
            chip.Rate = (float)(31.8 * 1024) / (float)Rate;

            for (i = 0; i < 8; i++)
            {
                chip.Channel[i].Step =
                    (uint)((float)chip.Channel[i].Step_B * chip.Rate);
            }
        }

        /**
         * PCM_Write_Reg(): Write to a PCM register.
         * @param Reg Register ID.
         * @param Data Data to write.
         */
        private void PCM_Write_Reg(byte ChipID, uint Reg, uint Data)
        {
            pcm_chip_ chip = PCM_Chip[ChipID];
            int i;
            pcm_chan_ chan = chip.Channel[chip.Cur_Chan];

            Data &= 0xFF;

            switch (Reg)
            {
                case 0x00:
                    /* evelope register */
                    chan.ENV = Data;
                    chan.MUL_L = (Data * (chan.PAN & 0x0F)) >> 5;
                    chan.MUL_R = (Data * (chan.PAN >> 4)) >> 5;
                    break;

                case 0x01:
                    /* pan register */
                    chan.PAN = Data;
                    chan.MUL_L = ((Data & 0x0F) * chan.ENV) >> 5;
                    chan.MUL_R = ((Data >> 4) * chan.ENV) >> 5;
                    break;

                case 0x02:
                    /* frequency step (LB) registers */
                    chan.Step_B &= 0xFF00;
                    chan.Step_B += Data;
                    chan.Step = (uint)((float)chan.Step_B * chip.Rate);

                    //LOG_MSG(pcm, LOG_MSG_LEVEL_DEBUG1,
                    //	"Step low = %.2X   Step calculated = %.8X",
                    //	Data, chan->Step);
                    break;

                case 0x03:
                    /* frequency step (HB) registers */
                    chan.Step_B &= 0x00FF;
                    chan.Step_B += Data << 8;
                    chan.Step = (uint)((float)chan.Step_B * chip.Rate);

                    //LOG_MSG(pcm, LOG_MSG_LEVEL_DEBUG1,
                    //	"Step high = %.2X   Step calculated = %.8X",
                    //	Data, chan->Step);
                    break;

                case 0x04:
                    chan.Loop_Addr &= 0xFF00;
                    chan.Loop_Addr += Data;

                    //LOG_MSG(pcm, LOG_MSG_LEVEL_DEBUG1,
                    //	"Loop low = %.2X   Loop = %.8X",
                    //	Data, chan->Loop_Addr);
                    break;

                case 0x05:
                    /* loop address registers */
                    chan.Loop_Addr &= 0x00FF;
                    chan.Loop_Addr += Data << 8;

                    //LOG_MSG(pcm, LOG_MSG_LEVEL_DEBUG1,
                    //	"Loop high = %.2X   Loop = %.8X",
                    //	Data, chan->Loop_Addr);
                    break;

                case 0x06:
                    /* start address registers */
                    chan.St_Addr = Data << (PCM_STEP_SHIFT + 8);
                    //chan->Addr = chan->St_Addr;

                    //LOG_MSG(pcm, LOG_MSG_LEVEL_DEBUG1,
                    //	"Start addr = %.2X   New Addr = %.8X",
                    //	Data, chan->Addr);
                    break;

                case 0x07:
                    /* control register */
                    /* mod is H */
                    if ((Data & 0x40) > 0)
                    {
                        /* select channel */
                        chip.Cur_Chan = (int)(Data & 0x07);
                    }
                    /* mod is L */
                    else
                    {
                        /* pcm ram bank select */
                        chip.Bank = (int)((Data & 0x0F) << 12);
                    }

                    /* sounding bit */
                    if ((Data & 0x80) > 0)
                        chip.Enable = 0xFF; // Used as mask
                    else
                        chip.Enable = 0;

                    //LOG_MSG(pcm, LOG_MSG_LEVEL_DEBUG1,
                    //	"General Enable = %.2X", Data);
                    break;

                case 0x08:
                    /* sound on/off register */
                    Data ^= 0xFF;

                    //LOG_MSG(pcm, LOG_MSG_LEVEL_DEBUG1,
                    //	"Channel Enable = %.2X", Data);

                    for (i = 0; i < 8; i++)
                    {
                        chan = chip.Channel[i];
                        if (chan.Enable == 0)
                            chan.Addr = chan.St_Addr;
                    }

                    for (i = 0; i < 8; i++)
                    {
                        chip.Channel[i].Enable = (uint)(Data & (1 << i));
                    }
                    break;
            }
        }

        /**
         * PCM_Update(): Update the PCM buffer.
         * @param buf PCM buffer.
         * @param Length Buffer length.
         */
        private int PCM_Update(byte ChipID, int[][] buf, int Length)
        {
            pcm_chip_ chip = PCM_Chip[ChipID];
            int i, j;
            int[] bufL, bufR;      //, *volL, *volR;
            uint Addr, k;
            pcm_chan_ CH;

            bufL = buf[0];
            bufR = buf[1];

            // clear buffers
            for (int d = 0; d < Length; d++)
            {
                bufL[d] = 0;
                bufR[d] = 0;
            }

            // if PCM disable, no sound
            if (chip.Enable == 0) return 1;

            // for long update
            for (i = 0; i < 8; i++)
            {
                CH = chip.Channel[i];

                // only loop when sounding and on
                if (CH.Enable > 0 && CH.Muted == 0)
                {
                    Addr = CH.Addr >> PCM_STEP_SHIFT;
                    //volL = &(PCM_Volume_Tab[CH->MUL_L << 8]);
                    //volR = &(PCM_Volume_Tab[CH->MUL_R << 8]);

                    for (j = 0; j < Length; j++)
                    {
                        // test for loop signal
                        if (chip.RAM[Addr] == 0xFF)
                        {
                            CH.Addr = (Addr = CH.Loop_Addr) << PCM_STEP_SHIFT;
                            if (chip.RAM[Addr] == 0xFF)
                                break;
                            else
                                j--;
                        }
                        else
                        {
                            if ((chip.RAM[Addr] & 0x80) != 0)
                            {
                                CH.Data = chip.RAM[Addr] & 0x7F;
                                bufL[j] -= (int)(CH.Data * CH.MUL_L);
                                bufR[j] -= (int)(CH.Data * CH.MUL_R);
                            }
                            else
                            {
                                CH.Data = chip.RAM[Addr];
                                // this improves the sound of Cosmic Fantasy Stories,
                                // although it's definately false behaviour
                                if (CH.Data == 0 && chip.Smpl0Patch != 0)
                                    CH.Data = -0x7F;
                                bufL[j] += (int)(CH.Data * CH.MUL_L);
                                bufR[j] += (int)(CH.Data * CH.MUL_R);
                            }

                            bufL[j] = MDSound.Limit(bufL[j], Int16.MaxValue , Int16.MinValue );
                            bufR[j] = MDSound.Limit(bufR[j], Int16.MaxValue , Int16.MinValue );

                            // update address register
                            k = Addr + 1;
                            CH.Addr = (CH.Addr + CH.Step) & 0x7FFFFFF;
                            Addr = CH.Addr >> PCM_STEP_SHIFT;

                            for (; k < Addr; k++)
                            {
                                if (chip.RAM[k] == 0xFF)
                                {
                                    CH.Addr = (Addr = CH.Loop_Addr) << PCM_STEP_SHIFT;
                                    break;
                                }
                            }
                        }
                    }

                    if (chip.RAM[Addr] == 0xFF)
                    {
                        CH.Addr = CH.Loop_Addr << PCM_STEP_SHIFT;
                    }
                }
            }

            return 0;
        }



        public override string Name { get { return "RF5C164"; } set { } }
        public override string ShortName { get { return "RF5C"; } set { } }

        public scd_pcm()
        {
            visVolume = new int[2][][] {
                new int[1][] { new int[2] { 0, 0 } }
                , new int[1][] { new int[2] { 0, 0 } }
            };
            //0..Main
        }

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {
            pcm_chip_ chip = PCM_Chip[ChipID];

            PCM_Update(ChipID, outputs, samples);

            visVolume[ChipID][0][0] = outputs[0][0];
            visVolume[ChipID][0][1] = outputs[1][0];
        }

        public override uint Start(byte ChipID,uint Samplingrate, uint clock,params object[] option)
        {
            //Samplingrate 未使用
            return Start(ChipID, clock);
        }

        public override uint Start(byte ChipID, uint clock)
        {
            /* allocate memory for the chip */
            //rf5c164_state *chip = get_safe_token(device);
            pcm_chip_ chip;
            uint rate;

            if (ChipID >= 0x02)//MAX_CHIPS)
                return 0;

            rate = (clock & 0x7FFFFFFF) / 384;
            if (((CHIP_SAMPLING_MODE & 0x01) != 0 && rate < CHIP_SAMPLE_RATE) ||
                CHIP_SAMPLING_MODE == 0x02)
                rate = (uint)CHIP_SAMPLE_RATE;

            PCM_Init(ChipID, rate);
            chip = PCM_Chip[ChipID];
            PCM_Chip[ChipID].Smpl0Patch = (int)((clock & 0x80000000) >> 31);

            /* allocate the stream */
            //chip->stream = stream_create(device, 0, 2, device->clock / 384, chip, rf5c68_update);

            return rate;
        }

        public override void Stop(byte ChipID)
        {
            pcm_chip_ chip = PCM_Chip[ChipID];
            //free(chip->RAM);
            //chip->RAM = NULL;

            return;
        }

        public override void Reset(byte ChipID)
        {
            //struct pcm_chip_ *chip = &PCM_Chip[ChipID];
            PCM_Reset(ChipID);
        }


        private void rf5c164_w(byte ChipID, uint offset, byte data)
        {
            //struct pcm_chip_ *chip = &PCM_Chip[ChipID];
            PCM_Write_Reg(ChipID, offset, data);
        }

        public void rf5c164_mem_w(byte ChipID, uint offset, byte data)
        {
            pcm_chip_ chip = PCM_Chip[ChipID];
            chip.RAM[(uint)chip.Bank | offset] = data;
        }

        public void rf5c164_write_ram(byte ChipID, uint DataStart, uint DataLength, byte[] RAMData)
        {
            pcm_chip_ chip = PCM_Chip[ChipID];

            DataStart |= (uint)chip.Bank;
            if (DataStart >= chip.RAMSize)
                return;
            if (DataStart + DataLength > chip.RAMSize)
                DataLength = (uint)(chip.RAMSize - DataStart);

            for (int i = 0; i < DataLength; i++)
            {
                chip.RAM[DataStart + i] = RAMData[i];
            }

            return;
        }

        public void rf5c164_write_ram2(byte ChipID, uint RAMStartAdr, uint RAMDataLength, byte[] SrcData, uint SrcStartAdr)
        {
            pcm_chip_ chip = PCM_Chip[ChipID];

            RAMStartAdr |= (uint)chip.Bank;
            if (RAMStartAdr >= chip.RAMSize)
                return;
            if (RAMStartAdr + RAMDataLength > chip.RAMSize)
                RAMDataLength = (uint)(chip.RAMSize - RAMStartAdr);

            for (int i = 0; i < RAMDataLength; i++)
            {
                chip.RAM[RAMStartAdr + i] = SrcData[SrcStartAdr + i];
            }

            return;
        }

        public void rf5c164_set_mute_mask(byte ChipID, uint MuteMask)
        {
            pcm_chip_ chip = PCM_Chip[ChipID];
            byte CurChn;

            for (CurChn = 0; CurChn < 8; CurChn++)
                chip.Channel[CurChn].Muted = (MuteMask >> CurChn) & 0x01;

            return;
        }

        public override int Write(byte ChipID, int port, int adr, int data)
        {
            rf5c164_w(ChipID, (uint)adr, (byte)data);
            return 0;
        }
    }
}
