using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/**********************************************************************************************
*
*   streaming ADPCM driver
*   by Aaron Giles
*
*   Library to transcode from an ADPCM source to raw PCM.
*   Written by Buffoni Mirko in 08/06/97
*   References: various sources and documents.
*
*   HJB 08/31/98
*   modified to use an automatically selected oversampling factor
*   for the current sample rate
*
*   Mish 21/7/99
*   Updated to allow multiple OKI chips with different sample rates
*
*   R. Belmont 31/10/2003
*   Updated to allow a driver to use both MSM6295s and "raw" ADPCM voices (gcpinbal)
*   Also added some error trapping for MAME_DEBUG builds
*
**********************************************************************************************/

namespace MDSound
{
	public class okim6295 : Instrument
	{

        public okim6295()
        {
            visVolume = new int[2][][] {
                new int[1][] { new int[2] { 0, 0 } }
                , new int[1][] { new int[2] { 0, 0 } }
            };
            //0..Main
        }

        override public uint Start(byte ChipID, uint clock)
		{
			return (uint)device_start_okim6295(ChipID, (int)clock);
		}

		public override uint Start(byte ChipID, uint samplingrate, uint clock, params object[] option)
		{
			return (uint)device_start_okim6295(ChipID, (int)clock);
		}

		override public void Stop(byte ChipID)
		{
			device_stop_okim6295(ChipID);
		}

		override public void Reset(byte ChipID)
		{
			device_reset_okim6295(ChipID);
		}

		override public void Update(byte ChipID, int[][] outputs, int samples)
		{
			okim6295_update(ChipID, outputs, samples);

            visVolume[ChipID][0][0] = outputs[0][0];
            visVolume[ChipID][0][1] = outputs[1][0];
        }

        public class adpcm_state
		{
			public int signal;
			public int step;
		};


		private const int MAX_SAMPLE_CHUNK = 0x10;  // that's enough for VGMPlay's update rate

		/* struct describing a single playing ADPCM voice */
		public class ADPCMVoice
		{
			public byte playing;          /* 1 if we are actively playing */

			public uint base_offset;     /* pointer to the base memory location */
			public uint sample;          /* current sample number */
			public uint count;           /* total samples to play */

			public adpcm_state adpcm=new adpcm_state();/* current ADPCM state */
			public uint volume;          /* output volume */
			public byte Muted;
		};

		//public okim6295_state okim6295_state;
		public class okim6295_state
		{
			public const int OKIM6295_VOICES = 4;
			public ADPCMVoice[] voice = new ADPCMVoice[4] { new ADPCMVoice(), new ADPCMVoice(), new ADPCMVoice(), new ADPCMVoice() };//[OKIM6295_VOICES];
			//running_device *device;
			public int command;
			public byte bank_installed;
			public int bank_offs;
			public byte pin7_state;
			public byte nmk_mode;
			public byte[] nmk_bank=new byte[4];
			//sound_stream *stream;	/* which stream are we playing on? */
			public uint master_clock;    /* master clock frequency */
			public uint initial_clock;

			public uint ROMSize = 0;
			public int ptrROM;
			public byte[] ROM;

            public dlgSRATE_CALLBACK SmpRateFunc;
            public MDSound.Chip SmpRateData;
        };

        public delegate void dlgSRATE_CALLBACK(MDSound.Chip chip,int clk);

        /* step size index shift table */
        private static int[] index_shift = new int[8] { -1, -1, -1, -1, 2, 4, 6, 8 };

		/* lookup table for the precomputed difference */
		private static int[] diff_lookup = new int[49 * 16];

		/* volume lookup table. The manual lists only 9 steps, ~3dB per step. Given the dB values,
		   that seems to map to a 5-bit volume control. Any volume parameter beyond the 9th index
		   results in silent playback. */
		private static int[] volume_table = new int[16]
		{
			0x20,	//   0 dB
			0x16,	//  -3.2 dB
			0x10,	//  -6.0 dB
			0x0b,	//  -9.2 dB
			0x08,	// -12.0 dB
			0x06,	// -14.5 dB
			0x04,	// -18.0 dB
			0x03,	// -20.5 dB
			0x02,	// -24.0 dB
			0x00,
			0x00,
			0x00,
			0x00,
			0x00,
			0x00,
			0x00,
		};

		/* tables computed? */
		private static int tables_computed = 0;

		/* useful interfaces */
		//const okim6295_interface okim6295_interface_pin7high = { 1 };
		//const okim6295_interface okim6295_interface_pin7low = { 0 };

		/* default address map */
		/*static ADDRESS_MAP_START( okim6295, 0, 8 )
			AM_RANGE(0x00000, 0x3ffff) AM_ROM
		ADDRESS_MAP_END*/


		private const int MAX_CHIPS = 0x02;
		public static okim6295_state[] OKIM6295Data = new okim6295_state[MAX_CHIPS] { new okim6295_state(), new okim6295_state() };

		/*INLINE okim6295_state *get_safe_token(running_device *device)
		{
			assert(device != NULL);
			assert(device->token != NULL);
			assert(device->type == SOUND);
			assert(sound_get_type(device) == SOUND_OKIM6295);
			return (okim6295_state *)device->token;
		}*/


		/**********************************************************************************************

			 compute_tables -- compute the difference tables

		***********************************************************************************************/

		private void compute_tables()
		{
			/* nibble to bit map */
			int[][] nbl2bit = new int[16][] {
				new int[4]{ 1, 0, 0, 0},new int[4] { 1, 0, 0, 1},new int[4] { 1, 0, 1, 0},new int[4] { 1, 0, 1, 1},
				new int[4]{ 1, 1, 0, 0},new int[4] { 1, 1, 0, 1},new int[4] { 1, 1, 1, 0},new int[4] { 1, 1, 1, 1},
				new int[4]{-1, 0, 0, 0},new int[4] {-1, 0, 0, 1},new int[4] {-1, 0, 1, 0},new int[4] {-1, 0, 1, 1},
				new int[4]{-1, 1, 0, 0},new int[4] {-1, 1, 0, 1},new int[4] {-1, 1, 1, 0},new int[4] {-1, 1, 1, 1}
			};

			//int step, nib;

			/* loop over all possible steps */
			for (int step = 0; step <= 48; step++)
			{
				/* compute the step value */
				int stepval = (int)Math.Floor(16.0 * Math.Pow(11.0 / 10.0, (double)step));

				/* loop over all nibbles and compute the difference */
				for (int nib = 0; nib < 16; nib++)
				{
					diff_lookup[step * 16 + nib] = nbl2bit[nib][0] *
					(stepval * nbl2bit[nib][1] +
					stepval / 2 * nbl2bit[nib][2] +
					stepval / 4 * nbl2bit[nib][3] +
					stepval / 8);
				}
			}

			tables_computed = 1;
		}



		/**********************************************************************************************

			 reset_adpcm -- reset the ADPCM stream

		***********************************************************************************************/

		public void reset_adpcm(adpcm_state state)
		{
			/* make sure we have our tables */
			if (tables_computed == 0) compute_tables();

			/* reset the signal/step */
			state.signal = -2;
			state.step = 0;
		}



		/**********************************************************************************************

			 clock_adpcm -- clock the next ADPCM byte

		***********************************************************************************************/

		public short clock_adpcm(adpcm_state state, byte nibble)
		{
//            System.Console.Write("nibble={0} diff_lookup[{1}]={2}\n", nibble, state.step * 16 + (nibble & 15), diff_lookup[state.step * 16 + (nibble & 15)]);
//            System.Console.Write("1state.signal={0}\n", state.signal);
			state.signal += diff_lookup[state.step * 16 + (nibble & 15)];

			/* clamp to the maximum */
			if (state.signal > 2047)
				state.signal = 2047;
			else if (state.signal < -2048)
				state.signal = -2048;

//            System.Console.Write("2state.signal={0}\n", state.signal);
			/* adjust the step size and clamp */
			state.step += index_shift[nibble & 7];
//            System.Console.Write("3state.signal={0}\n", state.signal);
			if (state.step > 48)
				state.step = 48;
			else if (state.step < 0)
				state.step = 0;

//            System.Console.Write("4state.signal={0}\n", state.signal);
			/* return the signal */
			return (short)state.signal;
		}



		/**********************************************************************************************

			 generate_adpcm -- general ADPCM decoding routine

		***********************************************************************************************/

		private const uint NMK_BNKTBLBITS = 8;
		private const uint NMK_BNKTBLSIZE = 0x100;//(1 << NMK_BNKTBLBITS);  // 0x100
		private const uint NMK_TABLESIZE = (4 * NMK_BNKTBLSIZE);    // 0x400
		private const uint NMK_TABLEMASK = (NMK_TABLESIZE - 1);     // 0x3FF

		private const uint NMK_BANKBITS = 16;
		private const uint NMK_BANKSIZE = 0x10000;//(1 << NMK_BANKBITS);      // 0x10000
		private const uint NMK_BANKMASK = (NMK_BANKSIZE - 1);       // 0xFFFF
		private const uint NMK_ROMBASE = (4 * NMK_BANKSIZE);        // 0x40000

        public override string Name { get { return "OKIM6295"; } set { } }
        public override string ShortName { get { return "OKI9"; } set { } }

        private static byte memory_raw_read_byte(okim6295_state chip, int offset)
		{
			int CurOfs;

			if (chip.nmk_mode==0)
			{
				CurOfs = chip.bank_offs | offset;
			}
			else
			{
				byte BankID;

				if (offset < NMK_TABLESIZE && (chip.nmk_mode & 0x80)!=0)
				{
					// pages sample table
					BankID = (byte)(offset >> (int)NMK_BNKTBLBITS);
					CurOfs = (int)(offset & NMK_TABLEMASK);    // 0x3FF, not 0xFF
				}
				else
				{
					BankID = (byte)(offset >> (int)NMK_BANKBITS);
					CurOfs = (int)(offset & NMK_BANKMASK);
				}
				CurOfs |= (chip.nmk_bank[BankID & 0x03] << (int)NMK_BANKBITS);
				// I modified MAME to write a clean sample ROM.
				// (Usually it moves the data by NMK_ROMBASE.)
				//CurOfs += NMK_ROMBASE;
			}
			if (CurOfs < chip.ROMSize)
				return chip.ROM[CurOfs];
			else
				return 0x00;
		}

		private void generate_adpcm(okim6295_state chip, ADPCMVoice voice, short[] buffer, int samples)
		{
			int ptrBuffer = 0;

			/* if this voice is active */
			if (voice.playing != 0)
			{
				//System.Console.Write("base_offset[{0:X}] sample[{1:X}] count[{2:X}]\n", voice.base_offset, voice.sample, voice.count);
				int iBase = (int)voice.base_offset;
				int sample = (int)voice.sample;
				int count = (int)voice.count;

				/* loop while we still have samples to generate */
				while (samples != 0)
				{
					/* compute the new amplitude and update the current step */
					//int nibble = memory_raw_read_byte(chip->device->space(), base + sample / 2) >> (((sample & 1) << 2) ^ 4);
					//System.Console.Write("nibblecal1[{0:d}]2[{1:d}]\n", iBase + sample / 2, (((sample & 1) << 2) ^ 4));
					byte nibble = (byte)(memory_raw_read_byte(chip, iBase + sample / 2) >> (((sample & 1) << 2) ^ 4));
					//System.Console.Write( "nibble[{0:X}]\n", nibble);

					/* output to the buffer, scaling by the volume */
					/* signal in range -2048..2047, volume in range 2..32 => signal * volume / 2 in range -32768..32767 */
					buffer[ptrBuffer++] = (short)(clock_adpcm(voice.adpcm, nibble) * voice.volume / 2);
					//System.Console.Write("*buffer[{0}]\n", buffer[ptrBuffer-1]);
					samples--;

					/* next! */
					if (++sample >= count)
					{
						voice.playing = 0;
						break;
					}
				}

				/* update the parameters */
				voice.sample = (uint)sample;
			}

			/* fill the rest with silence */
			while (samples-- != 0)
			{
				buffer[ptrBuffer++] = 0;
			}
		}



		/**********************************************************************************************
		 *
		 *  OKIM 6295 ADPCM chip:
		 *
		 *  Command bytes are sent:
		 *
		 *      1xxx xxxx = start of 2-byte command sequence, xxxxxxx is the sample number to trigger
		 *      abcd vvvv = second half of command; one of the abcd bits is set to indicate which voice
		 *                  the v bits seem to be volumed
		 *
		 *      0abc d000 = stop playing; one or more of the abcd bits is set to indicate which voice(s)
		 *
		 *  Status is read:
		 *
		 *      ???? abcd = one bit per voice, set to 0 if nothing is playing, or 1 if it is active
		 *
		***********************************************************************************************/


		/**********************************************************************************************

			 okim6295_update -- update the sound chip so that it is in sync with CPU execution

		***********************************************************************************************/

		//static STREAM_UPDATE( okim6295_update )
		private void okim6295_update(byte ChipID, int[][] outputs, int samples)
		{
			//System.Console.Write("samples:{0}\n"        , samples);
			//okim6295_state *chip = (okim6295_state *)param;
			okim6295_state chip = OKIM6295Data[ChipID];
			int i;

			//memset(outputs[0], 0, samples * sizeof(*outputs[0]));
			for (i = 0; i < samples; i++)
			{
				outputs[0][i] = 0;
			}

			for (i = 0; i < okim6295_state.OKIM6295_VOICES; i++)
			//    for (i = 0; i < 1; i++)
			{
				ADPCMVoice voice = chip.voice[i];
				if (voice.Muted == 0)
				{
					int[][] buffer = outputs;
					int ptrBuffer = 0;
					short[] sample_data = new short[MAX_SAMPLE_CHUNK];
					int remaining = samples;

					/* loop while we have samples remaining */
					while (remaining != 0)
					{
						int Samples = (remaining > MAX_SAMPLE_CHUNK) ? MAX_SAMPLE_CHUNK : remaining;
						int samp;

						generate_adpcm(chip, voice, sample_data, Samples);
						for (samp = 0; samp < Samples; samp++)
						{
							buffer[0][ptrBuffer++] += sample_data[samp];
                            //if (sample_data[samp] != 0)
                            //{
                            //    System.Console.WriteLine("ch:{0} sampledata[{1}]={2} count:{3} sample:{4}"
                            //    , i, samp, sample_data[samp]
                            //    , voice.count, voice.sample);
                            //}
                        }

                        remaining -= samples;
					}
                }
            }

			//memcpy(outputs[1], outputs[0], samples * sizeof(*outputs[0]));
			for (i = 0; i < samples; i++)
			{
				outputs[1][i] = outputs[0][i];
            }

        }



		/**********************************************************************************************

			 state save support for MAME

		***********************************************************************************************/

		/*static void adpcm_state_save_register(struct ADPCMVoice *voice, running_device *device, int index)
		{
			state_save_register_device_item(device, index, voice->playing);
			state_save_register_device_item(device, index, voice->sample);
			state_save_register_device_item(device, index, voice->count);
			state_save_register_device_item(device, index, voice->adpcm.signal);
			state_save_register_device_item(device, index, voice->adpcm.step);
			state_save_register_device_item(device, index, voice->volume);
			state_save_register_device_item(device, index, voice->base_offset);
		}

		static STATE_POSTLOAD( okim6295_postload )
		{
			running_device *device = (running_device *)param;
			okim6295_state *info = get_safe_token(device);
			okim6295_set_bank_base(device, info->bank_offs);
		}

		static void okim6295_state_save_register(okim6295_state *info, running_device *device)
		{
			int j;

			state_save_register_device_item(device, 0, info->command);
			state_save_register_device_item(device, 0, info->bank_offs);
			for (j = 0; j < OKIM6295_VOICES; j++)
				adpcm_state_save_register(&info->voice[j], device, j);

			state_save_register_postload(device->machine, okim6295_postload, (void *)device);
		}*/



		/**********************************************************************************************

			 DEVICE_START( okim6295 ) -- start emulation of an OKIM6295-compatible chip

		***********************************************************************************************/

		//static DEVICE_START( okim6295 )
		private int device_start_okim6295(byte ChipID, int clock)
		{
			//const okim6295_interface *intf = (const okim6295_interface *)device->baseconfig().static_config;
			//okim6295_state *info = get_safe_token(device);
			okim6295_state info;
			//int divisor = intf->pin7 ? 132 : 165;
			int divisor;
			//int voice;

			if (ChipID >= MAX_CHIPS)
				return 0;

			info = OKIM6295Data[ChipID];

			compute_tables();

			info.command = -1;
			//info.bank_installed = 0;// FALSE;
			info.bank_offs = 0;
			info.nmk_mode = 0x00;
			//memset(info->nmk_bank, 0x00, 4 * sizeof(UINT8));
			for (int i = 0; i < 4; i++)
			{
				info.nmk_bank[i] = 0x00;
			}
			//info->device = device;

			//info->master_clock = device->clock;
			info.initial_clock = (uint)clock;
			info.master_clock = (uint)clock & 0x7FFFFFFF;
			info.pin7_state = (byte)(((uint)clock & 0x80000000) >> 31);
			info.SmpRateFunc=null;

			/* generate the name and create the stream */
			divisor = info.pin7_state != 0 ? 132 : 165;
			//info->stream = stream_create(device, 0, 1, device->clock/divisor, info, okim6295_update);

			// moved to device_reset
			/*// initialize the voices //
			for (voice = 0; voice < OKIM6295_VOICES; voice++)
			{
				// initialize the rest of the structure //
				info->voice[voice].volume = 0;
				reset_adpcm(&info->voice[voice].adpcm);
			}*/

			//okim6295_state_save_register(info, device);

			return (int)(info.master_clock / divisor);
		}

		private void device_stop_okim6295(byte ChipID)
		{
			okim6295_state chip = OKIM6295Data[ChipID];

			chip.ROM = null;
			chip.ROMSize = 0x00;

			return;
		}

		/**********************************************************************************************

			 DEVICE_RESET( okim6295 ) -- stop emulation of an OKIM6295-compatible chip

		***********************************************************************************************/

		//static DEVICE_RESET( okim6295 )
		private void device_reset_okim6295(byte ChipID)
		{
			//okim6295_state *info = get_safe_token(device);
			okim6295_state info = OKIM6295Data[ChipID];
			int voice;

			//stream_update(info->stream);

			info.command = -1;
			info.bank_offs = 0;
			info.nmk_mode = 0x00;
			//memset(info->nmk_bank, 0x00, 4 * sizeof(UINT8));
			for (int i = 0; i < 4; i++)
			{
				info.nmk_bank[i] = 0x00;
			}
			info.master_clock = info.initial_clock & 0x7FFFFFFF;
			info.pin7_state = (byte)((info.initial_clock & 0x80000000) >> 31);

			for (voice = 0; voice < okim6295_state.OKIM6295_VOICES; voice++)
			{
				info.voice[voice].volume = 0;
				reset_adpcm(info.voice[voice].adpcm);

				info.voice[voice].playing = 0;
			}
		}



		/**********************************************************************************************

			 okim6295_set_bank_base -- set the base of the bank for a given voice on a given chip

		***********************************************************************************************/

		//void okim6295_set_bank_base(running_device *device, int base)
		private void okim6295_set_bank_base(okim6295_state info, int iBase)
		{
			//okim6295_state *info = get_safe_token(device);
			//stream_update(info->stream);

			/* if we are setting a non-zero base, and we have no bank, allocate one */
			//if (info.bank_installed == 0 && iBase != 0)
			//{
			/* override our memory map with a bank */
			//memory_install_read_bank(device->space(), 0x00000, 0x3ffff, 0, 0, device->tag());
			//info.bank_installed = 1;// TRUE;
			//}

			/* if we have a bank number, set the base pointer */
			//if (info.bank_installed != 0)
			//{
			//info.bank_offs = iBase;
			//memory_set_bankptr(device->machine, device->tag(), device->region->base.u8 + base);
			//}
			info.bank_offs = iBase;
		}



		/**********************************************************************************************

			 okim6295_set_pin7 -- adjust pin 7, which controls the internal clock division

		***********************************************************************************************/

		private static void okim6295_clock_changed(okim6295_state info)
		{
			int divisor;
			divisor = info.pin7_state != 0 ? 132 : 165;
			//stream_set_sample_rate(info->stream, info->master_clock/divisor);
			if (info.SmpRateFunc != null)
			    info.SmpRateFunc(info.SmpRateData, (int)info.master_clock / divisor);

		}

		//void okim6295_set_pin7(running_device *device, int pin7)
		private static void okim6295_set_pin7(okim6295_state info, int pin7)
		{
			//okim6295_state *info = get_safe_token(device);
			//int divisor = pin7 ? 132 : 165;

			info.pin7_state = (byte)pin7;
			//stream_set_sample_rate(info->stream, info->master_clock/divisor);
			okim6295_clock_changed(info);
		}


		/**********************************************************************************************

			 okim6295_status_r -- read the status port of an OKIM6295-compatible chip

		***********************************************************************************************/

		//READ8_DEVICE_HANDLER( okim6295_r )
		private byte okim6295_r(byte ChipID, int offset)
		{
			//okim6295_state *info = get_safe_token(device);
			okim6295_state info = OKIM6295Data[ChipID];
			int i, result;

			result = 0xf0;  /* naname expects bits 4-7 to be 1 */

			/* set the bit to 1 if something is playing on a given channel */
			//stream_update(info->stream);
			for (i = 0; i < okim6295_state.OKIM6295_VOICES; i++)
			{
				ADPCMVoice voice = info.voice[i];

				/* set the bit if it's playing */
				if (voice.playing != 0)
					result |= 1 << i;
			}

			return (byte)result;
		}



		/**********************************************************************************************

			 okim6295_data_w -- write to the data port of an OKIM6295-compatible chip

		***********************************************************************************************/

		//WRITE8_DEVICE_HANDLER( okim6295_w )
		private void okim6295_write_command(okim6295_state info, byte data)
		{
			//okim6295_state *info = get_safe_token(device);

			/* if a command is pending, process the second half */
			if (info.command != -1)
			{
				int temp = data >> 4, i, start, stop;
				int iBase;

				/* the manual explicitly says that it's not possible to start multiple voices at the same time */
//				if (temp != 0 && temp != 1 && temp != 2 && temp != 4 && temp != 8)
//					System.Console.Write("OKI6295 start %x contact MAMEDEV\n", temp);

				/* update the stream */
				//stream_update(info->stream);

				/* determine which voice(s) (voice is set by a 1 bit in the upper 4 bits of the second byte) */
				for (i = 0; i < okim6295_state.OKIM6295_VOICES; i++, temp >>= 1)
				{
					if ((temp & 1) != 0)
					{
						ADPCMVoice voice = info.voice[i];

						/* determine the start/stop positions */
						iBase = info.command * 8;

						//start  = memory_raw_read_byte(device->space(), base + 0) << 16;
						start = memory_raw_read_byte(info, iBase + 0) << 16;
						start |= memory_raw_read_byte(info, iBase + 1) << 8;
						start |= memory_raw_read_byte(info, iBase + 2) << 0;
						start &= 0x3ffff;

						stop = memory_raw_read_byte(info, iBase + 3) << 16;
						stop |= memory_raw_read_byte(info, iBase + 4) << 8;
						stop |= memory_raw_read_byte(info, iBase + 5) << 0;
						stop &= 0x3ffff;

						/* set up the voice to play this sample */
						if (start < stop)
						{
							if (voice.playing == 0) /* fixes Got-cha and Steel Force */
							{
								voice.playing = 1;
								voice.base_offset = (uint)start;
								voice.sample = 0;
								voice.count = (uint)(2 * (stop - start + 1));

								/* also reset the ADPCM parameters */
								reset_adpcm(voice.adpcm);
								voice.volume = (uint)volume_table[data & 0x0f];
							}
							else
							{
								//logerror("OKIM6295:'%s' requested to play sample %02x on non-stopped voice\n",device->tag(),info->command);
								// just displays warnings when seeking
								//logerror("OKIM6295: Voice %u requested to play sample %02x on non-stopped voice\n",i,info->command);
							}
						}
						/* invalid samples go here */
						else
						{
							//logerror("OKIM6295:'%s' requested to play invalid sample %02x\n",device->tag(),info->command);
							//System.Console.Write("OKIM6295: Voice {0}  requested to play invalid sample {1:X2} StartAddr {2:X} StopAdr {3:X} \n", i, info.command, start, stop);
							voice.playing = 0;
						}
					}
				}

				/* reset the command */
				info.command = -1;
			}

			/* if this is the start of a command, remember the sample number for next time */
			else if ((data & 0x80) != 0)
			{
				info.command = data & 0x7f;
			}

			/* otherwise, see if this is a silence command */
			else
			{
				int temp = data >> 3, i;

				/* update the stream, then turn it off */
				//stream_update(info->stream);

				/* determine which voice(s) (voice is set by a 1 bit in bits 3-6 of the command */
				for (i = 0; i < okim6295_state.OKIM6295_VOICES; i++, temp >>= 1)
				{
					if ((temp & 1) != 0)
					{
						ADPCMVoice voice = info.voice[i];

						voice.playing = 0;
					}
				}
			}
		}

		private void okim6295_w(byte ChipID, int offset, byte data)
		{
			okim6295_state chip = OKIM6295Data[ChipID];

			switch (offset)
			{
				case 0x00:
					okim6295_write_command(chip, data);
					break;
				case 0x08:
					chip.master_clock &= ~((uint)0x000000FF);
					chip.master_clock |= (uint)(data << 0);
					break;
				case 0x09:
					chip.master_clock &= ~((uint)0x0000FF00);
					chip.master_clock |= (uint)(data << 8);
					break;
				case 0x0A:
					chip.master_clock &= ~((uint)0x00FF0000);
					chip.master_clock |= (uint)(data << 16);
					break;
				case 0x0B:
                    data &= 0x7F;
                    chip.master_clock &= ~((uint)0xFF000000);
					chip.master_clock |= (uint)(data << 24);
					okim6295_clock_changed(chip);
					break;
				case 0x0C:
					okim6295_set_pin7(chip, data);
					break;
				case 0x0E:  // NMK112 bank switch enable
					chip.nmk_mode = data;
					break;
				case 0x0F:
					okim6295_set_bank_base(chip, data << 18);
					break;
				case 0x10:
				case 0x11:
				case 0x12:
				case 0x13:
					chip.nmk_bank[offset & 0x03] = data;
					break;
			}

			return;
		}

		public void okim6295_write_rom(byte ChipID, int ROMSize, int DataStart, int DataLength, byte[] ROMData)
		{
			okim6295_state chip = OKIM6295Data[ChipID];

			if (chip.ROMSize != ROMSize)
			{
				chip.ROM = new byte[ROMSize];// (byte*)realloc(chip.ROM, ROMSize);
				chip.ROMSize = (uint)ROMSize;
				//printf("OKIM6295: New ROM Size: 0x%05X\n", ROMSize);
				//memset(chip->ROM, 0xFF, ROMSize);
				for (int i = 0; i < ROMSize; i++)
				{
					chip.ROM[i] = 0xff;
				}
			}
			if (DataStart > ROMSize)
				return;
			if (DataStart + DataLength > ROMSize)
				DataLength = ROMSize - DataStart;

			//memcpy(chip->ROM + DataStart, ROMData, DataLength);
			for (int i = 0; i < DataLength; i++)
			{
				chip.ROM[i + DataStart] = ROMData[i];
			}

			return;
		}

		public void okim6295_write_rom2(byte ChipID, int ROMSize, int DataStart, int DataLength, byte[] ROMData,uint srcStartAddr)
		{
			//System.Console.Write("OKIM6295:okim6295_write_rom2: ChipID:{0} ROMSize:{1:X} DataStart:{2:X} DataLength:{3:X} srcStartAddr:{4:X}\n", ChipID, ROMSize, DataStart, DataLength, srcStartAddr);

			okim6295_state chip = OKIM6295Data[ChipID];

			if (chip.ROMSize != ROMSize)
			{
				chip.ROM = new byte[ROMSize];// (byte*)realloc(chip.ROM, ROMSize);
				chip.ROMSize = (uint)ROMSize;
				//printf("OKIM6295: New ROM Size: 0x%05X\n", ROMSize);
				//memset(chip->ROM, 0xFF, ROMSize);
				for (int i = 0; i < ROMSize; i++)
				{
					chip.ROM[i] = 0xff;
				}
			}
			if (DataStart > ROMSize)
				return;
			if (DataStart + DataLength > ROMSize)
				DataLength = ROMSize - DataStart;

			//memcpy(chip->ROM + DataStart, ROMData, DataLength);
			for (int i = 0; i < DataLength; i++)
			{
				chip.ROM[i + DataStart] = ROMData[i + srcStartAddr];

				//Console.Write("{0:X02} ", chip.ROM[i + DataStart]);
			}

			return;
		}


		public void okim6295_set_mute_mask(byte ChipID, uint MuteMask)
		{
			okim6295_state chip = OKIM6295Data[ChipID];
			byte CurChn;

			for (CurChn = 0; CurChn < okim6295_state.OKIM6295_VOICES; CurChn++)
				chip.voice[CurChn].Muted = (byte)((MuteMask >> CurChn) & 0x01);

			return;
		}

        public void okim6295_set_srchg_cb(byte ChipID, dlgSRATE_CALLBACK CallbackFunc, MDSound.Chip DataPtr)
        {
            okim6295_state info = OKIM6295Data[ChipID];

            // set Sample Rate Change Callback routine
            info.SmpRateFunc = CallbackFunc;
            info.SmpRateData = DataPtr;

            return;
        }

        public override int Write(byte ChipID, int port, int adr, int data)
        {
            okim6295_w(ChipID, adr, (byte)data);
            return 0;
        }


        /**************************************************************************
		 * Generic get_info
		 **************************************************************************/

        /*DEVICE_GET_INFO( okim6295 )
		{
			switch (state)
			{
				// --- the following bits of info are returned as 64-bit signed integers --- //
				case DEVINFO_INT_TOKEN_BYTES:				info->i = sizeof(okim6295_state);				break;
				case DEVINFO_INT_DATABUS_WIDTH_0:			info->i = 8;									break;
				case DEVINFO_INT_ADDRBUS_WIDTH_0:			info->i = 18;									break;
				case DEVINFO_INT_ADDRBUS_SHIFT_0:			info->i = 0;									break;

				// --- the following bits of info are returned as pointers to data --- //
				case DEVINFO_PTR_DEFAULT_MEMORY_MAP_0:		info->default_map8 = ADDRESS_MAP_NAME(okim6295);break;

				// --- the following bits of info are returned as pointers to functions --- //
				case DEVINFO_FCT_START:						info->start = DEVICE_START_NAME( okim6295 );	break;
				case DEVINFO_FCT_RESET:						info->reset = DEVICE_RESET_NAME( okim6295 );	break;

				// --- the following bits of info are returned as NULL-terminated strings --- //
				case DEVINFO_STR_NAME:						strcpy(info->s, "OKI6295");						break;
				case DEVINFO_STR_FAMILY:					strcpy(info->s, "OKI ADPCM");					break;
				case DEVINFO_STR_VERSION:					strcpy(info->s, "1.0");							break;
				case DEVINFO_STR_SOURCE_FILE:				strcpy(info->s, __FILE__);						break;
				case DEVINFO_STR_CREDITS:					strcpy(info->s, "Copyright Nicola Salmoria and the MAME Team"); break;
			}
		}*/


    }
}
