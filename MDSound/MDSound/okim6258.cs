using System;
/**********************************************************************************************
 *
 *   OKI MSM6258 ADPCM
 *
 *   TODO:
 *   3-bit ADPCM support
 *   Recording?
 *
 **********************************************************************************************/

namespace MDSound
{
    public class okim6258 : Instrument
    {
        private const int FOSC_DIV_BY_1024 = 0;
        private const int FOSC_DIV_BY_768 = 1;
        private const int FOSC_DIV_BY_512 = 2;

        private const int TYPE_3BITS = 0;
        private const int TYPE_4BITS = 1;

        private const int OUTPUT_10BITS = 0;
        private const int OUTPUT_12BITS = 1;

        private const int COMMAND_STOP = (1 << 0);
        private const int COMMAND_PLAY = (1 << 1);
        private const int COMMAND_RECORD = (1 << 2);

        private const int STATUS_PLAYING = (1 << 1);
        private const int STATUS_RECORDING = (1 << 2);

        private static int[] dividers=new int[4]{ 1024, 768, 512, 512 };

        private const int QUEUE_SIZE = (1 << 1);
        private const int QUEUE_MASK = (QUEUE_SIZE - 1);

        public class okim6258_state
        {
            public byte status;

            public uint master_clock;    /* master clock frequency */
            public uint divider;         /* master clock divider */
            public byte adpcm_type;       /* 3/4 bit ADPCM select */
            public byte data_in;          /* ADPCM data-in register */
            public byte nibble_shift;     /* nibble select */
                                          //sound_stream *stream;	/* which stream are we playing on? */

            public byte output_bits;
            public uint output_mask;

            // Valley Bell: Added a small queue to prevent race conditions.
            public byte[] data_buf=new byte[8];
            public byte data_in_last;
            public byte data_buf_pos;
            // Data Empty Values:
            //	00 - data written, but not read yet
            //	01 - read data, waiting for next write
            //	02 - tried to read, but had no data
            public byte data_empty;
            // Valley Bell: Added pan
            public byte pan;
            public int last_smpl;

            public int signal;
            public int step;

            public byte[] clock_buffer=new byte[0x04];
            public uint initial_clock;
            public byte initial_div;

            public dlgSRATE_CALLBACK SmpRateFunc;
            public MDSound.Chip SmpRateData;
        };

        public delegate void dlgSRATE_CALLBACK(MDSound.Chip chip, int vclk);

        /* step size index shift table */
        private static int[] index_shift=new int[8]{ -1, -1, -1, -1, 2, 4, 6, 8 };

        /* lookup table for the precomputed difference */
        private static int[] diff_lookup=new int[49 * 16];

        /* tables computed? */
        private static int tables_computed = 0;

        private const int MAX_CHIPS = 0x02;
        public okim6258_state[] OKIM6258Data = new okim6258_state[MAX_CHIPS] { new okim6258_state(), new okim6258_state() };
        public static byte Iternal10Bit = 0x00;

        /*INLINE okim6258_state *get_safe_token(running_device *device)
        {
            assert(device != NULL);
            assert(device->type() == OKIM6258);
            return (okim6258_state *)downcast<legacy_device_base *>(device)->token();
        }*/

        /**********************************************************************************************

             compute_tables -- compute the difference tables

        ***********************************************************************************************/

        private static void compute_tables()
        {
            /* nibble to bit map */
            int[][] nbl2bit = new int[16][] {
                new int[4]{ 1, 0, 0, 0},new int[4] { 1, 0, 0, 1},new int[4] { 1, 0, 1, 0}, new int[4]{ 1, 0, 1, 1},
                new int[4]{ 1, 1, 0, 0},new int[4] { 1, 1, 0, 1},new int[4] { 1, 1, 1, 0},new int[4] { 1, 1, 1, 1},
                new int[4]{-1, 0, 0, 0},new int[4] {-1, 0, 0, 1},new int[4] {-1, 0, 1, 0}, new int[4]{-1, 0, 1, 1},
                new int[4]{-1, 1, 0, 0},new int[4] {-1, 1, 0, 1},new int[4] {-1, 1, 1, 0},new int[4] {-1, 1, 1, 1}
            };

            int step, nib;

            if (tables_computed != 0)
                return;

            /* loop over all possible steps */
            for (step = 0; step <= 48; step++)
            {
                /* compute the step value */
                int stepval = (int)Math.Floor(16.0 * Math.Pow(11.0 / 10.0, (double)step));

                /* loop over all nibbles and compute the difference */
                for (nib = 0; nib < 16; nib++)
                {
                    diff_lookup[step * 16 + nib] = nbl2bit[nib][0] *
                        (stepval * nbl2bit[nib][1] +
                         stepval / 2 * nbl2bit[nib][2] +
                         stepval / 4 * nbl2bit[nib][3] +
                         stepval / 8);
//                    System.Console.Write("diff_lookup[{0}]={1} ", step * 16 + nib, diff_lookup[step * 16 + nib]);
                }
            }

            tables_computed = 1;
        }

        private static short clock_adpcm(okim6258_state chip, byte nibble)
        {
            int max = (int)chip.output_mask - 1;
            int min = -(int)chip.output_mask;

            int sample = diff_lookup[chip.step * 16 + (nibble & 15)];
            chip.signal = ((sample << 8) + (chip.signal * 245)) >> 8;

            /* clamp to the maximum */
            if (chip.signal > max)
                chip.signal = max;
            else if (chip.signal < min)
                chip.signal = min;

            /* adjust the step size and clamp */
            chip.step += index_shift[nibble & 7];
            if (chip.step > 48)
                chip.step = 48;
            else if (chip.step < 0)
                chip.step = 0;

            /* return the signal scaled up to 32767 */
            return (short)(chip.signal << 4);
        }

        /**********************************************************************************************
         okim6258_update -- update the sound chip so that it is in sync with CPU execution
        ***********************************************************************************************/

        //static STREAM_UPDATE( okim6258_update )
        override public void Update(byte ChipID, int[][] outputs, int samples)
        {
            //okim6258_state *chip = (okim6258_state *)param;
            okim6258_state chip = OKIM6258Data[ChipID];
            //stream_sample_t *buffer = outputs[0];
            int[] bufL = outputs[0];
            int[] bufR = outputs[1];
            int ind = 0;

            //memset(outputs[0], 0, samples * sizeof(*outputs[0]));

            if ((chip.status & STATUS_PLAYING)!=0)
            {
                int nibble_shift = chip.nibble_shift;

                while (samples!=0)
                {
                    //System.Console.Write("status={0} chip.nibble_shift={1} ", chip.status, chip.nibble_shift);
                    /* Compute the new amplitude and update the current step */
                    //int nibble = (chip->data_in >> nibble_shift) & 0xf;
                    int nibble;
                    short sample;

                    //System.Console.Write("chip.data_empty={0} ", chip.data_empty);
                    if (nibble_shift==0)
                    {
                        // 1st nibble - get data
                        if (chip.data_empty==0)
                        {
                            chip.data_in = chip.data_buf[chip.data_buf_pos >> 4];
                            chip.data_buf_pos += 0x10;
                            chip.data_buf_pos &= 0x7f;
                            if ((chip.data_buf_pos >> 4) == (chip.data_buf_pos & 0x0F))
                                chip.data_empty++;
                        }
                        else
                        {
                            if (chip.data_empty < 0x80)
                                chip.data_empty++;
                        }
                    }
                    nibble = (chip.data_in >> nibble_shift) & 0xf;

                    /* Output to the buffer */
                    //INT16 sample = clock_adpcm(chip, nibble);
                    if (chip.data_empty < 0x02)
                    {
                        sample = clock_adpcm(chip, (byte)nibble);
                        chip.last_smpl = sample;
                    }
                    else
                    {
                        // Valley Bell: data_empty behaviour (loosely) ported from XM6
                        if (chip.data_empty >= 0x02+0x01)
                        {
                            chip.data_empty -= 0x01;
                            //if (chip.signal < 0)
                            //    chip.signal++;
                            //else if (chip.signal > 0)
                            //    chip.signal--;
                            chip.signal = chip.signal * 15 / 16;
                            chip.last_smpl = chip.signal << 4;
                        }
                        sample = (short)chip.last_smpl;
                    }

                    nibble_shift ^= 4;

                    //*buffer++ = sample;
                    //System.Console.WriteLine("chip.pan={0} sample={1} ", chip.pan, sample);
                    bufL[ind] = ((chip.pan & 0x02)!=0) ? 0x00 : sample;
                    bufR[ind] = ((chip.pan & 0x01)!=0) ? 0x00 : sample;
                    //Console.WriteLine("001  bufL[{0}]={1}  bufR[{2}]={3}", ind, bufL[ind], ind, bufR[ind]);
                    samples--;
                    ind++;
                }

                /* Update the parameters */
                chip.nibble_shift = (byte)nibble_shift;
            }
            else
            {
                /* Fill with 0 */
                while ((samples--)!=0)
                {
//                    System.Console.Write("passed ");
                    //*buffer++ = 0;
                    bufL[ind] = 0;
                    bufR[ind] = 0;
                    ind++;
                }
            }

            visVolume[ChipID][0][0] = outputs[0][0];
            visVolume[ChipID][0][1] = outputs[1][0];
        }

        /**********************************************************************************************

             state save support for MAME

        ***********************************************************************************************/

        /*static void okim6258_state_save_register(okim6258_state *info, running_device *device)
        {
            state_save_register_device_item(device, 0, info->status);
            state_save_register_device_item(device, 0, info->master_clock);
            state_save_register_device_item(device, 0, info->divider);
            state_save_register_device_item(device, 0, info->data_in);
            state_save_register_device_item(device, 0, info->nibble_shift);
            state_save_register_device_item(device, 0, info->signal);
            state_save_register_device_item(device, 0, info->step);
        }*/


        private static int get_vclk(okim6258_state info)
        {
            int clk_rnd;

            clk_rnd = (int)info.master_clock;
            clk_rnd += (int)(info.divider / 2);    // for better rounding - should help some of the streams
            return (int)(clk_rnd / info.divider);
        }


        /**********************************************************************************************

             OKIM6258_start -- start emulation of an OKIM6258-compatible chip

        ***********************************************************************************************/

        //static DEVICE_START( okim6258 )
        private int device_start_okim6258(byte ChipID, uint clock, int divider, int adpcm_type, int output_12bits)
        {
            //System.Console.WriteLine("device_start_okim6258 ChipID{0} clock{1} divider{2} adpcm_type{3} output_12bits{4} ", ChipID, clock, divider, adpcm_type, output_12bits);
            //const okim6258_interface *intf = (const okim6258_interface *)device->baseconfig().static_config();
            //okim6258_state *info = get_safe_token(device);
            okim6258_state info;

            if (ChipID >= MAX_CHIPS)
                return 0;

            info = OKIM6258Data[ChipID];

            compute_tables();

            //info->master_clock = device->clock();
            info.initial_clock = clock;
            info.initial_div = (byte)divider;
            info.master_clock = clock;
            info.adpcm_type = /*intf->*/(byte)adpcm_type;
            info.clock_buffer[0x00] = (byte)((clock & 0x000000FF) >> 0);
            info.clock_buffer[0x01] = (byte)((clock & 0x0000FF00) >> 8);
            info.clock_buffer[0x02] = (byte)((clock & 0x00FF0000) >> 16);
            info.clock_buffer[0x03] = (byte)((clock & 0xFF000000) >> 24);
            //ここでnullは不要
            //info.SmpRateFunc = null;

            /* D/A precision is 10-bits but 12-bit data can be output serially to an external DAC */
            info.output_bits = /*intf->*/(byte)((output_12bits != 0) ? 12 : 10);
            if (Iternal10Bit!=0)
                info.output_mask = (uint)(1 << (info.output_bits - 1));
            else
                info.output_mask = (1 << (12 - 1));
            info.divider = (uint)dividers[/*intf->*/divider];

            //info->stream = stream_create(device, 0, 1, device->clock()/info->divider, info, okim6258_update);

            info.signal = -2;
            info.step = 0;

            //okim6258_state_save_register(info, device);

            //System.Console.WriteLine("get_vclk(info)={0} ", get_vclk(info));
            return get_vclk(info);// (int)(info.master_clock / info.divider);
        }

        /**********************************************************************************************
         OKIM6258_stop -- stop emulation of an OKIM6258-compatible chip
        ***********************************************************************************************/
        private void device_stop_okim6258(byte ChipID)
        {
            //okim6258_state info = OKIM6258Data[ChipID];
            OKIM6258Data[ChipID] = null;
        }

        //static DEVICE_RESET( okim6258 )
        private void device_reset_okim6258(byte ChipID)
        {
            //okim6258_state *info = get_safe_token(device);
            okim6258_state info = OKIM6258Data[ChipID];

            //stream_update(info->stream);

            info.master_clock = info.initial_clock;
            info.clock_buffer[0x00] = (byte)((info.initial_clock & 0x000000FF) >> 0);
            info.clock_buffer[0x01] = (byte)((info.initial_clock & 0x0000FF00) >> 8);
            info.clock_buffer[0x02] = (byte)((info.initial_clock & 0x00FF0000) >> 16);
            info.clock_buffer[0x03] = (byte)((info.initial_clock & 0xFF000000) >> 24);
            info.divider = (uint)dividers[info.initial_div];
            if (info.SmpRateFunc != null)
            {
                info.SmpRateFunc(info.SmpRateData, get_vclk(info));
                //Console.WriteLine("passed");
            }


            info.signal = -2;
            info.step = 0;
            info.status = 0;

            // Valley Bell: Added reset of the Data In register.
            info.data_in = 0x00;
            info.data_buf[0] = info.data_buf[1] = 0x00;
            info.data_buf_pos = 0x00;
            info.data_empty = 0xFF;
            info.pan = 0x00;
        }

        /**********************************************************************************************

             okim6258_set_divider -- set the master clock divider

        ***********************************************************************************************/
        //void okim6258_set_divider(running_device *device, int val)
        private void okim6258_set_divider(byte ChipID, int val)
        {
            //okim6258_state *info = get_safe_token(device);
            okim6258_state info = OKIM6258Data[ChipID];
            int divider = dividers[val];

            info.divider = (uint)dividers[val];
            //stream_set_sample_rate(info->stream, info->master_clock / divider);
            if (info.SmpRateFunc != null)
                info.SmpRateFunc(info.SmpRateData, get_vclk(info));
        }

        /**********************************************************************************************

             okim6258_set_clock -- set the master clock

        ***********************************************************************************************/

        //void okim6258_set_clock(running_device *device, int val)
        private void okim6258_set_clock(byte ChipID, int val)
        {
            //okim6258_state *info = get_safe_token(device);
            okim6258_state info = OKIM6258Data[ChipID];

            if (val!=0)
            {
                info.master_clock = (uint)val;
            }
            else
            {
                info.master_clock = (uint)((info.clock_buffer[0x00] << 0) |
                                        (info.clock_buffer[0x01] << 8) |
                                        (info.clock_buffer[0x02] << 16) |
                                        (info.clock_buffer[0x03] << 24));
            }
            //stream_set_sample_rate(info->stream, info->master_clock / info->divider);
            if (info.SmpRateFunc != null)
                info.SmpRateFunc(info.SmpRateData, get_vclk(info));
        }

        /**********************************************************************************************

             okim6258_get_vclk -- get the VCLK/sampling frequency

        ***********************************************************************************************/

        //int okim6258_get_vclk(running_device *device)
        private int okim6258_get_vclk(byte ChipID)
        {
            //okim6258_state *info = get_safe_token(device);
            okim6258_state info = OKIM6258Data[ChipID];

            return get_vclk(info); //(int)(info.master_clock / info.divider);
        }

        /**********************************************************************************************
         okim6258_status_r -- read the status port of an OKIM6258-compatible chip
        ***********************************************************************************************/

        //READ8_DEVICE_HANDLER( okim6258_status_r )
        /*UINT8 okim6258_status_r(UINT8 ChipID, offs_t offset)
        {
            //okim6258_state *info = get_safe_token(device);
            okim6258_state *info = &OKIM6258Data[ChipID];

            //stream_update(info->stream);

            return (info->status & STATUS_PLAYING) ? 0x00 : 0x80;
        }*/

        /**********************************************************************************************

             okim6258_data_w -- write to the control port of an OKIM6258-compatible chip

        ***********************************************************************************************/
        //WRITE8_DEVICE_HANDLER( okim6258_data_w )
        private void okim6258_data_w(byte ChipID, /*offs_t offset, */byte data)
        {
            //okim6258_state *info = get_safe_token(device);
            okim6258_state info = OKIM6258Data[ChipID];

            /* update the stream */
            //stream_update(info->stream);

            //info->data_in = data;
            //info->nibble_shift = 0;

            if (info.data_empty >= 0x02)
            {
                info.data_buf_pos = 0x00;
            }
            info.data_in_last = data;
            info.data_buf[info.data_buf_pos & 0x0F] = data;
            info.data_buf_pos += 0x01;
            info.data_buf_pos &= 0xf7;
            if ((info.data_buf_pos >> 4) == (info.data_buf_pos & 0x0F))
            {
                //logerror("Warning: FIFO full!\n");
                info.data_buf_pos = (byte)((info.data_buf_pos & 0xF0) | ((info.data_buf_pos - 1) & 0x07));
            }
            info.data_empty = 0x00;
        }


        /**********************************************************************************************

             okim6258_ctrl_w -- write to the control port of an OKIM6258-compatible chip

        ***********************************************************************************************/

        //WRITE8_DEVICE_HANDLER( okim6258_ctrl_w )
        private void okim6258_ctrl_w(byte ChipID, /*offs_t offset, */byte data)
        {
            //okim6258_state *info = get_safe_token(device);
            okim6258_state info = OKIM6258Data[ChipID];

            //stream_update(info->stream);

            if ((data & COMMAND_STOP)!=0)
            {
                //Console.WriteLine("COMMAND:STOP");
                //info.status &= (byte)(~((byte)STATUS_PLAYING | (byte)STATUS_RECORDING)));
                info.status &= (byte)(0x2+0x4);
                return;
            }

            if ((data & COMMAND_PLAY)!=0)
            {
                //Console.WriteLine("COMMAND:PLAY");
                if ((info.status & STATUS_PLAYING)==0)
                {
                    info.status |= STATUS_PLAYING;

                    /* Also reset the ADPCM parameters */
                    info.signal = -2;
                    info.step = 0;
                    info.nibble_shift = 0;

                    info.data_buf[0x00] = data;
                    info.data_buf_pos = 0x01;  // write pos 01, read pos 00
                    info.data_empty = 0x00;
                }
                info.step = 0;
                info.nibble_shift = 0;
            }
            else
            {
                //info.status &= ~STATUS_PLAYING;
                info.status &= 0xd;
            }

            if ((data & COMMAND_RECORD)!=0)
            {
                //logerror("M6258: Record enabled\n");
                info.status |= STATUS_RECORDING;
            }
            else
            {
                //info.status &= ~STATUS_RECORDING;
                info.status &= 0xb;
            }
        }

        private void okim6258_set_clock_byte(byte ChipID, byte Byte, byte val)
        {
            okim6258_state info = OKIM6258Data[ChipID];

            info.clock_buffer[Byte] = val;

            return;
        }

        private void okim6258_pan_w(byte ChipID, byte data)
        {
            okim6258_state info = OKIM6258Data[ChipID];

            info.pan = data;

            return;
        }

        /**************************************************************************
         * Generic get_info
         **************************************************************************/

        /*DEVICE_GET_INFO( okim6258 )
        {
            switch (state)
            {
                // --- the following bits of info are returned as 64-bit signed integers --- //
                case DEVINFO_INT_TOKEN_BYTES:					info->i = sizeof(okim6258_state);			break;

                // --- the following bits of info are returned as pointers to data or functions --- //
                case DEVINFO_FCT_START:							info->start = DEVICE_START_NAME(okim6258);		break;
                case DEVINFO_FCT_STOP:							// nothing //								break;
                case DEVINFO_FCT_RESET:							info->reset = DEVICE_RESET_NAME(okim6258);		break;

                // --- the following bits of info are returned as NULL-terminated strings --- //
                case DEVINFO_STR_NAME:							strcpy(info->s, "OKI6258");					break;
                case DEVINFO_STR_FAMILY:					strcpy(info->s, "OKI ADPCM");				break;
                case DEVINFO_STR_VERSION:					strcpy(info->s, "1.0");						break;
                case DEVINFO_STR_SOURCE_FILE:						strcpy(info->s, __FILE__);					break;
                case DEVINFO_STR_CREDITS:					strcpy(info->s, "Copyright Nicola Salmoria and the MAME Team"); break;
            }
        }


        DEFINE_LEGACY_SOUND_DEVICE(OKIM6258, okim6258);*/




        public okim6258()
        {
            visVolume = new int[2][][] {
                new int[1][] { new int[2] { 0, 0 } }
                , new int[1][] { new int[2] { 0, 0 } }
            };
            //0..Main
        }

        override public uint Start(byte ChipID, uint clock)
        {
            return Start(ChipID, 44100, clock, 0);
        }

        public override uint Start(byte ChipID,uint samplingrate, uint clock,params object[] option) {
            int divider = ((int)option[0] & 0x03) >> 0;
            int adpcm_type = ((int)option[0] & 0x04) >> 2;
            int output_12bits = ((int)option[0] & 0x08) >> 3;
            return (uint)device_start_okim6258(ChipID, clock, divider, adpcm_type, output_12bits);
        }

        public override string Name { get { return "OKIM6258"; } set { } }
        public override string ShortName { get { return "OKI5"; } set { } }

        override public void Stop(byte ChipID)
        {
            device_stop_okim6258(ChipID);
        }

        override public void Reset(byte ChipID)
        {
            device_reset_okim6258(ChipID);
        }

        private void okim6258_write(byte ChipID, byte Port, byte Data)
        {
            //System.Console.Write("port={0:X2} data={1:X2} \n", Port, Data);
            switch (Port)
            {
                case 0x00:
                    okim6258_ctrl_w(ChipID, /*0x00, */Data);
                    break;
                case 0x01:
                    okim6258_data_w(ChipID, /*0x00, */Data);
                    break;
                case 0x02:
                    okim6258_pan_w(ChipID, Data);
                    break;
                case 0x08:
                case 0x09:
                case 0x0A:
                    okim6258_set_clock_byte(ChipID, (byte)(Port & 0x03), Data);
                    break;
                case 0x0B:
                    okim6258_set_clock_byte(ChipID, (byte)(Port & 0x03), Data);
                    okim6258_set_clock(ChipID, 0);
                    break;
                case 0x0C:
                    okim6258_set_divider(ChipID, Data);
                    break;
            }

            return;
        }

        public void okim6258_set_options(ushort Options)
        {
            Iternal10Bit = (byte)((Options >> 0) & 0x01);

            return;
        }

        public void okim6258_set_srchg_cb(byte ChipID, dlgSRATE_CALLBACK CallbackFunc, MDSound.Chip chip)
        {
            okim6258_state info = OKIM6258Data[ChipID];

            // set Sample Rate Change Callback routine
            info.SmpRateFunc = CallbackFunc;
            info.SmpRateData = chip;

            return;
        }

        public override int Write(byte ChipID, int port, int adr, int data)
        {
            okim6258_write(ChipID, (byte)adr, (byte)data);
            return 0;
        }
    }
}
