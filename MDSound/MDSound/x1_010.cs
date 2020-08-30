using System;
using System.Collections.Generic;
using System.Text;

namespace MDSound
{
	public class x1_010 : Instrument
	{
		public override string Name { get { return "X1-010"; } set { } }
		public override string ShortName { get { return "X1-010"; } set { } }

		public x1_010()
		{
		}

		public override void Reset(byte ChipID)
		{
			device_reset_x1_010(ChipID);
		}

		public override uint Start(byte ChipID, uint clock)
		{
			return (uint)device_start_x1_010(ChipID, (int)16000000);
		}

		public override uint Start(byte ChipID, uint clock, uint ClockValue, params object[] option)
		{
			return (uint)device_start_x1_010(ChipID, (int)ClockValue);
		}

		public override void Stop(byte ChipID)
		{
			device_stop_x1_010(ChipID);
		}

		public override void Update(byte ChipID, int[][] outputs, int samples)
		{
			seta_update(ChipID, outputs, samples);
		}

		public override int Write(byte ChipID, int port, int adr, int data)
		{
			seta_sound_w(ChipID, (port << 8) | adr, (byte)data);
			return 0;
		}




		/***************************************************************************

									-= Seta Hardware =-

							driver by   Luca Elia (l.elia@tin.it)

							rewrite by Manbow-J(manbowj@hamal.freemail.ne.jp)

							X1-010 Seta Custom Sound Chip (80 Pin PQFP)

		 Custom programmed Mitsubishi M60016 Gate Array, 3608 gates, 148 Max I/O ports

			The X1-010 is 16 Voices sound generator, each channel gets it's
			waveform from RAM (128 bytes per waveform, 8 bit unsigned data)
			or sampling PCM(8bit unsigned data).

		Registers:
			8 registers per channel (mapped to the lower bytes of 16 words on the 68K)

			Reg:    Bits:       Meaning:

			0       7654 3---
					---- -2--   PCM/Waveform repeat flag (0:Ones 1:Repeat) (*1)
					---- --1-   Sound out select (0:PCM 1:Waveform)
					---- ---0   Key on / off

			1       7654 ----   PCM Volume 1 (L?)
					---- 3210   PCM Volume 2 (R?)
								Waveform No.

			2                   PCM Frequency
								Waveform Pitch Lo

			3                   Waveform Pitch Hi

			4                   PCM Sample Start / 0x1000           [Start/End in bytes]
								Waveform Envelope Time

			5                   PCM Sample End 0x100 - (Sample End / 0x1000)    [PCM ROM is Max 1MB?]
								Waveform Envelope No.
			6                   Reserved
			7                   Reserved

			offset 0x0000 - 0x0fff  Wave form data
			offset 0x1000 - 0x1fff  Envelope data

			*1 : when 0 is specified, hardware interrupt is caused(allways return soon)

		***************************************************************************/

		//#include "emu.h"
		//#include <stdlib.h>
		//#include <stddef.h>	// for NULL
		//#include <string.h>	// for memset
		//#include "mamedef.h"
		//#include "x1_010.h"


		private int VERBOSE_SOUND = 0;
		private int VERBOSE_REGISTER_WRITE = 0;
		private int VERBOSE_REGISTER_READ = 0;

		//#define LOG_SOUND(x) do { if (VERBOSE_SOUND) logerror x; } while (0)
		//#define LOG_REGISTER_WRITE(x) do { if (VERBOSE_REGISTER_WRITE) logerror x; } while (0)
		//#define LOG_REGISTER_READ(x) do { if (VERBOSE_REGISTER_READ) logerror x; } while (0)

		private static int SETA_NUM_CHANNELS = 16;

		//#define FREQ_BASE_BITS		  8					// Frequency fixed decimal shift bits
		private int FREQ_BASE_BITS = 14;                    // Frequency fixed decimal shift bits
		private int ENV_BASE_BITS = 16;            // wave form envelope fixed decimal shift bits
		private int VOL_BASE = (2 * 32 * 256 / 30);                 // Volume base

		/* this structure defines the parameters for a channel */
		private class X1_010_CHANNEL
		{
			public byte status;
			public byte volume;                       //        volume / wave form no.
			public byte frequency;                    //     frequency / pitch lo
			public byte pitch_hi;                 //      reserved / pitch hi
			public byte start;                        // start address / envelope time
			public byte end;                      //   end address / envelope no.
			public byte[] reserve = new byte[2];
		};

		//private x1_010_state x1_010_state = new _x1_010_state();
		private class x1_010_state
		{
			/* Variables only used here */
			public int rate;                               // Output sampling rate (Hz)
														   //sound_stream *	stream;					// Stream handle
														   //int	address;							// address eor data
														   //const UINT8 *region;					// region name
			public uint ROMSize;
			public byte[] rom;
			public int sound_enable;                       // sound output enable/disable
			public byte[] reg = new byte[0x2000];              // X1-010 Register & wave form area
															   //	UINT8	HI_WORD_BUF[0x2000];			// X1-010 16bit access ram check avoidance work
			public uint[] smp_offset = new uint[SETA_NUM_CHANNELS];
			public uint[] env_offset = new uint[SETA_NUM_CHANNELS];

			public uint base_clock;

			public byte[] Muted = new byte[SETA_NUM_CHANNELS];
		};

		/* mixer tables and internal buffers */
		//static short  *mixer_buffer = NULL;

		private byte CHIP_SAMPLING_MODE;
		private int CHIP_SAMPLE_RATE;

		private static int MAX_CHIPS = 0x02;
		private x1_010_state[] X1010Data = new x1_010_state[MAX_CHIPS];

		/*
		private x1_010_state get_safe_token(device_t device)
		{
			assert(device != null);
			assert(device->type() == X1_010);
			return (x1_010_state)downcast<legacy_device_base*>(device)->token();
		}
		*/


		/*--------------------------------------------------------------
		 generate sound to the mix buffer
		--------------------------------------------------------------*/
		//static STREAM_UPDATE( seta_update )
		private void seta_update(byte ChipID, int[][] outputs, int samples)
		{
			//x1_010_state *info = (x1_010_state *)param;
			x1_010_state info = X1010Data[ChipID];
			X1_010_CHANNEL reg;
			int ch, i, volL, volR, freq, div;
			int start, end;
			sbyte data;
			int env;
			uint smp_offs, smp_step, env_offs, env_step, delta;

			// mixer buffer zero clear
			for (i = 0; i < samples; i++)
			{
				outputs[0][i] = 0;
				outputs[1][i] = 0;
			}

			//  if( info->sound_enable == 0 ) return;

			for (ch = 0; ch < SETA_NUM_CHANNELS; ch++)
			{
				//reg = (X1_010_CHANNEL)info.reg[ch * 8];// sizeof(X1_010_CHANNEL)];
				if ((info.reg[ch * 8 + 0] & 1) != 0 && info.Muted[ch] == 0)// reg.status
				{       // Key On
					int[] bufL = outputs[0];
					int[] bufR = outputs[1];

					div = (info.reg[ch * 8 + 0] & 0x80) != 0 ? 1 : 0;
					if ((info.reg[ch * 8 + 0] & 2) == 0)
					{ // PCM sampling
						start = info.reg[ch * 8 + 4] * 0x1000;//+4 reg.start
						end = (0x100 - info.reg[ch * 8 + 5]) * 0x1000;//+5 reg.end
						volL = ((info.reg[ch * 8 + 1] >> 4) & 0xf) * VOL_BASE;//+1 reg.volume
						volR = ((info.reg[ch * 8 + 1] >> 0) & 0xf) * VOL_BASE;//+1 reg.volume
						smp_offs = info.smp_offset[ch];
						freq = info.reg[ch * 8 + 2] >> div;//+2 reg.frequency
														   // Meta Fox does write the frequency register, but this is a hack to make it "work" with the current setup
														   // This is broken for Arbalester (it writes 8), but that'll be fixed later.
						if (freq == 0) freq = 4;
						smp_step = (uint)((float)info.base_clock / 8192.0f
									* freq * (1 << FREQ_BASE_BITS) / (float)info.rate + 0.5f);
						if (smp_offs == 0)
						{
							//LOG_SOUND(("Play sample %p - %p, channel %X volume %d:%d freq %X step %X offset %X\n",
							//	start, end, ch, volL, volR, freq, smp_step, smp_offs));
						}
						for (i = 0; i < samples; i++)
						{
							delta = smp_offs >> FREQ_BASE_BITS;
							// sample ended?
							if (start + delta >= end)
							{
								info.reg[ch * 8 + 0] &= 0xfe;// ~0x01;                   // Key off//+0 reg.status
								break;
							}
							data = (sbyte)info.rom[start + delta];
							bufL[i] += (data * volL / 256);
							bufR[i] += (data * volR / 256);
							smp_offs += smp_step;
						}
						info.smp_offset[ch] = smp_offs;
					}
					else
					{ // Wave form
						start = info.reg[ch * 8 + 1] * 128 + 0x1000;
						smp_offs = info.smp_offset[ch];
						freq = ((info.reg[ch * 8 + 3] << 8) + info.reg[ch * 8 + 2]) >> div;
						smp_step = (uint)((float)info.base_clock / 128.0 / 1024.0 / 4.0 * freq * (1 << FREQ_BASE_BITS) / (float)info.rate + 0.5f);

						env = info.reg[ch * 8 + 5] * 128;
						env_offs = info.env_offset[ch];
						env_step = (uint)(
							(float)info.base_clock / 128.0 / 1024.0 / 4.0
							* info.reg[ch * 8 + 4] * (1 << ENV_BASE_BITS) / (float)info.rate + 0.5f
							);
						/* Print some more debug info */
						if (smp_offs == 0)
						{
							//LOG_SOUND(("Play waveform %X, channel %X volume %X freq %4X step %X offset %X\n",
							//reg->volume, ch, reg->end, freq, smp_step, smp_offs));
						}
						for (i = 0; i < samples; i++)
						{
							int vol;
							delta = env_offs >> ENV_BASE_BITS;
							// Envelope one shot mode
							if ((info.reg[ch * 8 + 0] & 4) != 0 && delta >= 0x80)
							{
								info.reg[ch * 8 + 0] &= 0xfe;// ~0x01;                   // Key off
								break;
							}
							vol = info.reg[env + (delta & 0x7f)];
							volL = ((vol >> 4) & 0xf) * VOL_BASE;
							volR = ((vol >> 0) & 0xf) * VOL_BASE;
							data = (sbyte)info.reg[start + ((smp_offs >> FREQ_BASE_BITS) & 0x7f)];
							bufL[i] += (data * volL / 256);
							bufR[i] += (data * volR / 256);
							smp_offs += smp_step;
							env_offs += env_step;
						}
						info.smp_offset[ch] = smp_offs;
						info.env_offset[ch] = env_offs;
					}
				}
			}
		}



		//static DEVICE_START( x1_010 )
		private int device_start_x1_010(byte ChipID, int clock)
		{
			int i;
			//const x1_010_interface *intf = (const x1_010_interface *)device->static_config();
			//x1_010_state *info = get_safe_token(device);
			x1_010_state info;

			if (ChipID >= MAX_CHIPS)
				return 0;

			info = X1010Data[ChipID];

			//info->region		= *device->region();
			//info->base_clock	= device->clock();
			//info->rate			= device->clock() / 1024;
			//info->address		= intf->adr;
			info.ROMSize = 0x00;
			info.rom = null;
			info.base_clock = (uint)clock;
			info.rate = clock / 512;
			if (((CHIP_SAMPLING_MODE & 0x01) != 0 && info.rate < CHIP_SAMPLE_RATE) ||
				CHIP_SAMPLING_MODE == 0x02)
				info.rate = CHIP_SAMPLE_RATE;

			for (i = 0; i < SETA_NUM_CHANNELS; i++)
			{
				info.smp_offset[i] = 0;
				info.env_offset[i] = 0;
			}
			/* Print some more debug info */
			//LOG_SOUND(("masterclock = %d rate = %d\n", device->clock(), info->rate ));

			/* get stream channels */
			//info->stream = device->machine().sound().stream_alloc(*device,0,2,info->rate,info,seta_update);
			return info.rate;
		}

		private void device_stop_x1_010(byte ChipID)
		{
			x1_010_state info = X1010Data[ChipID];

			//free(info.rom);
			info.rom = null;

			return;
		}

		private void device_reset_x1_010(byte ChipID)
		{
			x1_010_state info = X1010Data[ChipID];

			for (int i = 0; i < 0x2000; i++) info.reg[i] = 0;
			//memset(info->HI_WORD_BUF, 0, 0x2000);
			for (int i = 0; i < SETA_NUM_CHANNELS; i++) info.smp_offset[i] = 0;
			for (int i = 0; i < SETA_NUM_CHANNELS; i++) info.env_offset[i] = 0;

			return;
		}


		/*void seta_sound_enable_w(device_t *device, int data)
		{
			x1_010_state *info = get_safe_token(device);
			info->sound_enable = data;
		}*/



		/* Use these for 8 bit CPUs */


		//READ8_DEVICE_HANDLER( seta_sound_r )
		private byte seta_sound_r(byte ChipID, int offset)
		{
			//x1_010_state *info = get_safe_token(device);
			x1_010_state info = X1010Data[ChipID];
			//offset ^= info->address;
			return info.reg[offset];
		}




		//WRITE8_DEVICE_HANDLER( seta_sound_w )
		private void seta_sound_w(byte ChipID, int offset, byte data)
		{
			//x1_010_state *info = get_safe_token(device);
			x1_010_state info = X1010Data[ChipID];
			int channel, reg;
			//offset ^= info->address;

			channel = offset / 8;// sizeof(X1_010_CHANNEL);
			reg = offset % 8;// sizeof(X1_010_CHANNEL);

			if (channel < SETA_NUM_CHANNELS && reg == 0
			 && (info.reg[offset] & 1) == 0 && (data & 1) != 0)
			{
				info.smp_offset[channel] = 0;
				info.env_offset[channel] = 0;
			}
			//LOG_REGISTER_WRITE(("%s: offset %6X : data %2X\n", device->machine().describe_context(), offset, data ));
			info.reg[offset] = data;
		}

		public void x1_010_write_rom(byte ChipID, int ROMSize, int DataStart, int DataLength, byte[] ROMData, int ROMDataStartAddress = 0)
		{
			x1_010_state info = X1010Data[ChipID];

			if (info.ROMSize != ROMSize)
			{
				info.rom = new byte[ROMSize];// (byte[])realloc(info.rom, ROMSize);
				info.ROMSize = (uint)ROMSize;
				for (int i = 0; i < ROMSize; i++) info.rom[i] = 0xff;
			}
			if (DataStart > ROMSize)
				return;
			if (DataStart + DataLength > ROMSize)
				DataLength = ROMSize - DataStart;

			for (int i = 0; i < DataLength; i++) info.rom[i + DataStart] = ROMData[i + ROMDataStartAddress];

			return;
		}


		public void x1_010_set_mute_mask(byte ChipID, uint MuteMask)
		{
			x1_010_state info = X1010Data[ChipID];
			byte CurChn;

			for (CurChn = 0; CurChn < SETA_NUM_CHANNELS; CurChn++)
				info.Muted[CurChn] = (byte)((MuteMask >> CurChn) & 0x01);

			return;
		}




		/* Use these for 16 bit CPUs */

		/*READ16_DEVICE_HANDLER( seta_sound_word_r )
		{
			//x1_010_state *info = get_safe_token(device);
			x1_010_state *info = &X1010Data[ChipID];
			UINT16	ret;

			ret = info->HI_WORD_BUF[offset]<<8;
			ret += (seta_sound_r( device, offset )&0xff);
			LOG_REGISTER_READ(( "%s: Read X1-010 Offset:%04X Data:%04X\n", device->machine().describe_context(), offset, ret ));
			return ret;
		}

		WRITE16_DEVICE_HANDLER( seta_sound_word_w )
		{
			//x1_010_state *info = get_safe_token(device);
			x1_010_state *info = &X1010Data[ChipID];
			info->HI_WORD_BUF[offset] = (data>>8)&0xff;
			seta_sound_w( device, offset, data&0xff );
			LOG_REGISTER_WRITE(( "%s: Write X1-010 Offset:%04X Data:%04X\n", device->machine().describe_context(), offset, data ));
		}*/



		/**************************************************************************
		 * Generic get_info
		 **************************************************************************/

		/*DEVICE_GET_INFO( x1_010 )
		{
			switch (state)
			{
				// --- the following bits of info are returned as 64-bit signed integers ---
				case DEVINFO_INT_TOKEN_BYTES:					info->i = sizeof(x1_010_state); 			break;

				// --- the following bits of info are returned as pointers to data or functions ---
				case DEVINFO_FCT_START:							info->start = DEVICE_START_NAME( x1_010 );			break;
				case DEVINFO_FCT_STOP:							// Nothing //									break;
				case DEVINFO_FCT_RESET:							// Nothing //									break;

				// --- the following bits of info are returned as NULL-terminated strings ---
				case DEVINFO_STR_NAME:							strcpy(info->s, "X1-010");						break;
				case DEVINFO_STR_FAMILY:					strcpy(info->s, "Seta custom");					break;
				case DEVINFO_STR_VERSION:					strcpy(info->s, "1.0");							break;
				case DEVINFO_STR_SOURCE_FILE:						strcpy(info->s, __FILE__);						break;
				case DEVINFO_STR_CREDITS:					strcpy(info->s, "Copyright Nicola Salmoria and the MAME Team"); break;
			}
		}


		DEFINE_LEGACY_SOUND_DEVICE(X1_010, x1_010);*/





	}
}
