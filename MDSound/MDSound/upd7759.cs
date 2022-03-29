using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDSound
{
	public class upd7759 : Instrument
	{

		public override string Name { get { return "uPD7759"; } set { } }
		public override string ShortName { get { return "uPD7759"; } set { } }


		public upd7759()
		{
			visVolume = new int[2][][] {
				new int[1][] { new int[2] { 0, 0 } }
				, new int[1][] { new int[2] { 0, 0 } }
			};
			//0..Main
		}

		override public uint Start(byte ChipID, uint clock)
		{
			return (uint)device_start_upd7759(ChipID, (int)clock);
		}

		public override uint Start(byte ChipID, uint samplingrate, uint clock, params object[] option)
		{
			return (uint)device_start_upd7759(ChipID, (int)clock);
		}

		override public void Stop(byte ChipID)
		{
			device_stop_upd7759(ChipID);
		}

		override public void Reset(byte ChipID)
		{
			device_reset_upd7759(ChipID);
		}

		override public void Update(byte ChipID, int[][] outputs, int samples)
		{
			upd7759_update(ChipID, outputs, samples);

			visVolume[ChipID][0][0] = outputs[0][0];
			visVolume[ChipID][0][1] = outputs[1][0];
		}

		private void logerror(string msg)
        {

        }



		//upd7759.h


		//#pragma once

		//#include "devlegcy.h"

		/* There are two modes for the uPD7759, selected through the !MD pin.
           This is the mode select input.  High is stand alone, low is slave.
           We're making the assumption that nobody switches modes through
           software. */

		private readonly uint UPD7759_STANDARD_CLOCK = 640000;

		//private class _upd7759_interface { }
		//struct _upd7759_interface
		//{
		//            //void (*drqcallback)(running_device *device, int param);	/* drq callback (per chip, slave mode only) */
		//            void (* drqcallback) (int param);	/* drq callback (per chip, slave mode only) */
		//};

		//void upd7759_update(UINT8 ChipID, stream_sample_t** outputs, int samples);
		//void device_reset_upd7759(UINT8 ChipID);
		//int device_start_upd7759(UINT8 ChipID, int clock);
		//void device_stop_upd7759(UINT8 ChipID);

		////void upd7759_set_bank_base(running_device *device, offs_t base);

		////void upd7759_reset_w(running_device *device, UINT8 data);
		////void upd7759_start_w(running_device *device, UINT8 data);
		////int upd7759_busy_r(running_device *device);
		////WRITE8_DEVICE_HANDLER( upd7759_port_w );

		//void upd7759_set_bank_base(UINT8 ChipID, offs_t base);

		//void upd7759_reset_w(UINT8 ChipID, UINT8 data);
		//void upd7759_start_w(UINT8 ChipID, UINT8 data);
		//int upd7759_busy_r(UINT8 ChipID);
		//void upd7759_port_w(UINT8 ChipID, offs_t offset, UINT8 data);

		//void upd7759_write(UINT8 ChipID, UINT8 Port, UINT8 Data);
		//void upd7759_write_rom(UINT8 ChipID, offs_t ROMSize, offs_t DataStart, offs_t DataLength,

		//               const UINT8* ROMData);

		////DECLARE_LEGACY_SOUND_DEVICE(UPD7759, upd7759);



	

		//upd7759.c



		/************************************************************

			NEC UPD7759 ADPCM Speech Processor
			by: Juergen Buchmueller, Mike Balfour, Howie Cohen,
				Olivier Galibert, and Aaron Giles

		*************************************************************

			Description:

			The UPD7759 is a speech processing LSI that utilizes ADPCM to produce
			speech or other sampled sounds.  It can directly address up to 1Mbit
			(128k) of external data ROM, or the host CPU can control the speech
			data transfer.  The UPD7759 is usually hooked up to a 640 kHz clock and
			has one 8-bit input port, a start pin, a busy pin, and a clock output.

			The chip is composed of 3 parts:
			- a clock divider
			- a rom-reading engine
			- an adpcm engine
			- a 4-to-9 bit adpcm converter

			The clock divider takes the base 640KHz clock and divides it first
			by a fixed divisor of 4 and then by a value between 9 and 32.  The
			result gives a clock between 5KHz and 17.78KHz.  It's probably
			possible, but not recommended and certainly out-of-spec, to push the
			chip harder by reducing the divider.

			The rom-reading engine reads one byte every two divided clock cycles.
			The factor two comes from the fact that a byte has two nibbles, i.e.
			two samples.

			The apdcm engine takes bytes and interprets them as commands:

				00000000                    sample end
				00dddddd                    silence
				01ffffff                    send the 256 following nibbles to the converter
				10ffffff nnnnnnnn           send the n+1 following nibbles to the converter
				11---rrr --ffffff nnnnnnnn  send the n+1 following nibbles to the converter, and repeat r+1 times

			"ffffff" is sent to the clock divider to be the base clock for the
			adpcm converter, i.e., it's the sampling rate.  If the number of
			nibbles to send is odd the last nibble is ignored.  The commands
			are always 8-bit aligned.

			"dddddd" is the duration of the silence.  The base speed is unknown,
			1ms sounds reasonably.  It does not seem linked to the adpcm clock
			speed because there often is a silence before any 01 or 10 command.

			The adpcm converter converts nibbles into 9-bit DAC values.  It has
			an internal state of 4 bits that's used in conjunction with the
			nibble to lookup which of the 256 possible steps is used.  Then
			the state is changed according to the nibble value.  Essentially, the
			higher the state, the bigger the steps are, and using big steps
			increase the state.  Conversely, using small steps reduces the state.
			This allows the engine to be a little more adaptative than a
			classical ADPCM algorithm.

			The UPD7759 can run in two modes, master (also known as standalone)
			and slave.  The mode is selected through the "md" pin.  No known
			game changes modes on the fly, and it's unsure if that's even
			possible to do.


			Master mode:

			The output of the rom reader is directly connected to the adpcm
			converter.  The controlling cpu only sends a sample number and the
			7759 plays it.

			The sample rom has a header at the beginning of the form

				nn 5a a5 69 55

			where nn is the number of the last sample.  This is then followed by
			a vector of 2-bytes msb-first values, one per sample.  Multiplying
			them by two gives the sample start offset in the rom.  A 0x00 marks
			the end of each sample.

			It seems that the UPD7759 reads at least part of the rom header at
			startup.  Games doing rom banking are careful to reset the chip after
			each change.


			Slave mode:

			The rom reader is completely disconnected.  The input port is
			connected directly to the adpcm engine.  The first write to the input
			port activates the engine (the value itself is ignored).  The engine
			activates the clock output and waits for commands.  The clock speed
			is unknown, but its probably a divider of 640KHz.  We use 40KHz here
			because 80KHz crashes altbeast.  The chip probably has an internal
			fifo to the converter and suspends the clock when the fifo is full.
			The first command is always 0xFF.  A second 0xFF marks the end of the
			sample and the engine stops.  OTOH, there is a 0x00 at the end too.
			Go figure.

		*************************************************************/

		//#include "emu.h"
		//#include "streams.h"
		//# ifdef _DEBUG
		//# include <stdio.h>
		//#endif
		//# include <stdlib.h>
		//# include <string.h>	// for memset
		//# include <stddef.h>	// for NULL
		//# include "mamedef.h"
		//# include "upd7759.h"


#if DEBUG
		private int DEBUG_STATES = 0;
		private Action<string> DEBUG_METHOD = null;
#endif
		//#define DEBUG_STATES	(0)
		//		//#define DEBUG_METHOD	mame_printf_debug
		//#define DEBUG_METHOD	logerror



		/************************************************************

			Constants

		*************************************************************/

		/* step value fractional bits */
		private const int FRAC_BITS = 20;
		private const int FRAC_ONE = (1 << FRAC_BITS);
		private const int FRAC_MASK = (FRAC_ONE - 1);

		/* chip states */
		public enum STATE
		{
			IDLE,
			DROP_DRQ,
			START,
			FIRST_REQ,
			LAST_SAMPLE,
			DUMMY1,
			ADDR_MSB,
			ADDR_LSB,
			DUMMY2,
			BLOCK_HEADER,
			NIBBLE_COUNT,
			NIBBLE_MSN,
			NIBBLE_LSN
		};



		/************************************************************

			Type definitions

		*************************************************************/

		public class _upd7759_state
		{
			//running_device *device;
			//sound_stream *channel;					/* stream channel for playback */

			/* internal clock to output sample rate mapping */
			public UInt32 pos;                     /* current output sample position */
			public UInt32 step;                        /* step value per output sample */
			//attotime	clock_period;				/* clock period */
			//emu_timer *timer;						/* timer */

			/* I/O lines */
			public byte fifo_in;                  /* last data written to the sound chip */
			public byte reset;                        /* current state of the RESET line */
			public byte start;                        /* current state of the START line */
			public byte drq;                      /* current state of the DRQ line */
			//void (*drqcallback)(running_device *device, int param);			/* drq callback */
			//void (*drqcallback)(int param);			/* drq callback */

			/* internal state machine */
			public STATE state;                     /* current overall chip state */
			public Int32 clocks_left;              /* number of clocks left in this state */
			public UInt16 nibbles_left;                /* number of ADPCM nibbles left to process */
			public byte repeat_count;             /* number of repeats remaining in current repeat block */
			public STATE post_drq_state;                /* state we will be in after the DRQ line is dropped */
			public Int32 post_drq_clocks;          /* clocks that will be left after the DRQ line is dropped */
			public byte req_sample;                   /* requested sample number */
			public byte last_sample;              /* last sample number available */
			public byte block_header;             /* header byte */
			public byte sample_rate;              /* number of UPD clocks per ADPCM nibble */
			public byte first_valid_header;           /* did we get our first valid header yet? */
			public UInt32 offset;                      /* current ROM offset */
			public UInt32 repeat_offset;               /* current ROM repeat offset */

			/* ADPCM processing */
			public sbyte adpcm_state;               /* ADPCM state index */
			public byte adpcm_data;                   /* current byte of ADPCM data */
			public Int16 sample;                       /* current sample value */

			/* ROM access */
			public UInt32 romsize;
			public byte[] rom;                     /* pointer to ROM data or NULL for slave mode */
			public int romPtr = 0;
			public byte[] rombase;                 /* pointer to ROM data or NULL for slave mode */
			public UInt32 romoffset;                   /* ROM offset to make save/restore easier */
			public byte ChipMode;                 // 0 - Master, 1 - Slave

			// Valley Bell: Added a FIFO buffer based on Sega Pico.
			public byte[] data_buf = new byte[0x40];
			public byte dbuf_pos_read;
			public byte dbuf_pos_write;
		};



		/************************************************************

			Local variables

		*************************************************************/

		private readonly static int[][] upd7759_step = new int[16][]
		{
			new int[]{ 0,  0,  1,  2,  3,   5,   7,  10,  0,   0,  -1,  -2,  -3,   -5,   -7,  -10 },
			new int[]{ 0,  1,  2,  3,  4,   6,   8,  13,  0,  -1,  -2,  -3,  -4,   -6,   -8,  -13 },
			new int[]{ 0,  1,  2,  4,  5,   7,  10,  15,  0,  -1,  -2,  -4,  -5,   -7,  -10,  -15 },
			new int[]{ 0,  1,  3,  4,  6,   9,  13,  19,  0,  -1,  -3,  -4,  -6,   -9,  -13,  -19 },
			new int[]{ 0,  2,  3,  5,  8,  11,  15,  23,  0,  -2,  -3,  -5,  -8,  -11,  -15,  -23 },
			new int[]{ 0,  2,  4,  7, 10,  14,  19,  29,  0,  -2,  -4,  -7, -10,  -14,  -19,  -29 },
			new int[]{ 0,  3,  5,  8, 12,  16,  22,  33,  0,  -3,  -5,  -8, -12,  -16,  -22,  -33 },
			new int[]{ 1,  4,  7, 10, 15,  20,  29,  43, -1,  -4,  -7, -10, -15,  -20,  -29,  -43 },
			new int[]{ 1,  4,  8, 13, 18,  25,  35,  53, -1,  -4,  -8, -13, -18,  -25,  -35,  -53 },
			new int[]{ 1,  6, 10, 16, 22,  31,  43,  64, -1,  -6, -10, -16, -22,  -31,  -43,  -64 },
			new int[]{ 2,  7, 12, 19, 27,  37,  51,  76, -2,  -7, -12, -19, -27,  -37,  -51,  -76 },
			new int[]{ 2,  9, 16, 24, 34,  46,  64,  96, -2,  -9, -16, -24, -34,  -46,  -64,  -96 },
			new int[]{ 3, 11, 19, 29, 41,  57,  79, 117, -3, -11, -19, -29, -41,  -57,  -79, -117 },
			new int[]{ 4, 13, 24, 36, 50,  69,  96, 143, -4, -13, -24, -36, -50,  -69,  -96, -143 },
			new int[]{ 4, 16, 29, 44, 62,  85, 118, 175, -4, -16, -29, -44, -62,  -85, -118, -175 },
			new int[]{ 6, 20, 36, 54, 76, 104, 144, 214, -6, -20, -36, -54, -76, -104, -144, -214 }
		};

		private readonly static int[] upd7759_state_table = new int[16] { -1, -1, 0, 0, 1, 2, 2, 3, -1, -1, 0, 0, 1, 2, 2, 3 };



		/*INLINE upd7759_state *get_safe_token(running_device *device)
		{
			assert(device != NULL);
			assert(device->type() == UPD7759);
			return (upd7759_state *)downcast<legacy_device_base *>(device)->token();
		}*/


		private const int MAX_CHIPS = 0x02;
		private static _upd7759_state[] UPD7759Data = new _upd7759_state[MAX_CHIPS] { new _upd7759_state(), new _upd7759_state() };

        /************************************************************

			ADPCM sample updater

		*************************************************************/

        private void update_adpcm(_upd7759_state chip, int data)
		{
			/* update the sample and the state */
			chip.sample += (Int16)upd7759_step[chip.adpcm_state][data];
			chip.adpcm_state += (sbyte)upd7759_state_table[data];

			/* clamp the state to 0..15 */
			if (chip.adpcm_state < 0)
				chip.adpcm_state = 0;
			else if (chip.adpcm_state > 15)
				chip.adpcm_state = 15;
		}



		/************************************************************

			Master chip state machine

		*************************************************************/

		private void get_fifo_data(_upd7759_state chip)
		{
			if (chip.dbuf_pos_read == chip.dbuf_pos_write)
			{
				logerror("Warning: UPD7759 reading empty FIFO!\n");
				return;
			}

			chip.fifo_in = chip.data_buf[chip.dbuf_pos_read];
			chip.dbuf_pos_read++;
			chip.dbuf_pos_read &= 0x3F;

			return;
		}

		private void advance_state(_upd7759_state chip)
		{
			switch (chip.state)
			{
				/* Idle state: we stick around here while there's nothing to do */
				case STATE.IDLE:
					chip.clocks_left = 4;
					break;

				/* drop DRQ state: update to the intended state */
				case STATE.DROP_DRQ:
					chip.drq = 0;

					if (chip.ChipMode != 0)
						get_fifo_data(chip);    // Slave Mode only
					chip.clocks_left = chip.post_drq_clocks;
					chip.state = chip.post_drq_state;
					break;

				/* Start state: we begin here as soon as a sample is triggered */
				case STATE.START:
					chip.req_sample = (byte)(chip.rom != null ? chip.fifo_in : 0x10);
#if DEBUG
					if (DEBUG_STATES != 0) DEBUG_METHOD(string.Format("UPD7759: req_sample = {0:02X}\n", chip.req_sample));
#endif
					/* 35+ cycles after we get here, the /DRQ goes low
					 *     (first byte (number of samples in ROM) should be sent in response)
					 *
					 * (35 is the minimum number of cycles I found during heavy tests.
					 * Depending on the state the chip was in just before the /MD was set to 0 (reset, standby
					 * or just-finished-playing-previous-sample) this number can range from 35 up to ~24000).
					 * It also varies slightly from test to test, but not much - a few cycles at most.) */
					chip.clocks_left = 70; /* 35 - breaks cotton */
					chip.state = STATE.FIRST_REQ;
					break;

				/* First request state: issue a request for the first byte */
				/* The expected response will be the index of the last sample */
				case STATE.FIRST_REQ:
#if DEBUG
					if (DEBUG_STATES != 0) DEBUG_METHOD("UPD7759: first data request\n");
#endif
					chip.drq = 1;

					/* 44 cycles later, we will latch this value and request another byte */
					chip.clocks_left = 44;
					chip.state = STATE.LAST_SAMPLE;
					break;

				/* Last sample state: latch the last sample value and issue a request for the second byte */
				/* The second byte read will be just a dummy */
				case STATE.LAST_SAMPLE:
					chip.last_sample = chip.rom != null ? chip.rom[chip.romPtr + 0] : chip.fifo_in;
#if DEBUG
					if (DEBUG_STATES != 0) DEBUG_METHOD(string.Format("UPD7759: last_sample = {0:02X}, requesting dummy 1\n", chip.last_sample));
#endif
					chip.drq = 1;

					/* 28 cycles later, we will latch this value and request another byte */
					chip.clocks_left = 28; /* 28 - breaks cotton */
					chip.state = ((chip.req_sample > chip.last_sample) ? STATE.IDLE : STATE.DUMMY1);
					break;

				/* First dummy state: ignore any data here and issue a request for the third byte */
				/* The expected response will be the MSB of the sample address */
				case STATE.DUMMY1:
#if DEBUG
					if (DEBUG_STATES != 0) DEBUG_METHOD("UPD7759: dummy1, requesting offset_hi\n");
#endif
					chip.drq = 1;

					/* 32 cycles later, we will latch this value and request another byte */
					chip.clocks_left = 32;
					chip.state = STATE.ADDR_MSB;
					break;

				/* Address MSB state: latch the MSB of the sample address and issue a request for the fourth byte */
				/* The expected response will be the LSB of the sample address */
				case STATE.ADDR_MSB:
					chip.offset = (uint)((chip.rom != null ? chip.rom[chip.romPtr+(chip.req_sample * 2 + 5)] : chip.fifo_in) << 9);
#if DEBUG
					if (DEBUG_STATES != 0) DEBUG_METHOD(string.Format("UPD7759: offset_hi = {0:02X}, requesting offset_lo\n", chip.offset >> 9));
#endif
					chip.drq = 1;

					/* 44 cycles later, we will latch this value and request another byte */
					chip.clocks_left = 44;
					chip.state = STATE.ADDR_LSB;
					break;

				/* Address LSB state: latch the LSB of the sample address and issue a request for the fifth byte */
				/* The expected response will be just a dummy */
				case STATE.ADDR_LSB:
					chip.offset |= (uint)((chip.rom != null ? chip.rom[chip.romPtr+(chip.req_sample * 2 + 6)] : chip.fifo_in) << 1);
#if DEBUG
					if (DEBUG_STATES != 0) DEBUG_METHOD(string.Format("UPD7759: offset_lo = {0:02X}, requesting dummy 2\n", (chip.offset >> 1) & 0xff));
#endif
					chip.drq = 1;

					/* 36 cycles later, we will latch this value and request another byte */
					chip.clocks_left = 36;
					chip.state = STATE.DUMMY2;
					break;

				/* Second dummy state: ignore any data here and issue a request for the the sixth byte */
				/* The expected response will be the first block header */
				case STATE.DUMMY2:
					chip.offset++;
					chip.first_valid_header = 0;
#if DEBUG
					if (DEBUG_STATES != 0) DEBUG_METHOD("UPD7759: dummy2, requesting block header\n");
#endif
					chip.drq = 1;

					/* 36?? cycles later, we will latch this value and request another byte */
					chip.clocks_left = 36;
					chip.state = STATE.BLOCK_HEADER;
					break;

				/* Block header state: latch the header and issue a request for the first byte afterwards */
				case STATE.BLOCK_HEADER:

					/* if we're in a repeat loop, reset the offset to the repeat point and decrement the count */
					if (chip.repeat_count != 0)
					{
						chip.repeat_count--;
						chip.offset = chip.repeat_offset;
					}
					chip.block_header = chip.rom != null ? chip.rom[chip.romPtr + (chip.offset++ & 0x1ffff)] : chip.fifo_in;
#if DEBUG
					if (DEBUG_STATES != 0) DEBUG_METHOD(string.Format("UPD7759: header (@{0:05X}) = {1:02X}, requesting next byte\n", chip.offset, chip.block_header));
#endif
					chip.drq = 1;

					/* our next step depends on the top two bits */
					switch (chip.block_header & 0xc0)
					{
						case 0x00:  /* silence */
							chip.clocks_left = 1024 * ((chip.block_header & 0x3f) + 1);
							chip.state = (chip.block_header == 0 && chip.first_valid_header != 0) ? STATE.IDLE : STATE.BLOCK_HEADER;
							chip.sample = 0;
							chip.adpcm_state = 0;
							break;

						case 0x40:  /* 256 nibbles */
							chip.sample_rate = (byte)((chip.block_header & 0x3f) + 1);
							chip.nibbles_left = 256;
							chip.clocks_left = 36; /* just a guess */
							chip.state = STATE.NIBBLE_MSN;
							break;

						case 0x80:  /* n nibbles */
							chip.sample_rate = (byte)((chip.block_header & 0x3f) + 1);
							chip.clocks_left = 36; /* just a guess */
							chip.state = STATE.NIBBLE_COUNT;
							break;

						case 0xc0:  /* repeat loop */
							chip.repeat_count = (byte)((chip.block_header & 7) + 1);
							chip.repeat_offset = chip.offset;
							chip.clocks_left = 36; /* just a guess */
							chip.state = STATE.BLOCK_HEADER;
							break;
					}

					/* set a flag when we get the first non-zero header */
					if (chip.block_header != 0)
						chip.first_valid_header = 1;
					break;

				/* Nibble count state: latch the number of nibbles to play and request another byte */
				/* The expected response will be the first data byte */
				case STATE.NIBBLE_COUNT:
					chip.nibbles_left = (UInt16)((chip.rom != null ? chip.rom[chip.romPtr + (chip.offset++ & 0x1ffff)] : chip.fifo_in) + 1);
#if DEBUG
					if (DEBUG_STATES != 0)
						DEBUG_METHOD(string.Format("UPD7759: nibble_count = {0}, requesting next byte\n", chip.nibbles_left));
#endif
					chip.drq = 1;

					/* 36?? cycles later, we will latch this value and request another byte */
					chip.clocks_left = 36; /* just a guess */
					chip.state = STATE.NIBBLE_MSN;
					break;

				/* MSN state: latch the data for this pair of samples and request another byte */
				/* The expected response will be the next sample data or another header */
				case STATE.NIBBLE_MSN:
					chip.adpcm_data = chip.rom != null ? chip.rom[chip.romPtr + (chip.offset++ & 0x1ffff)] : chip.fifo_in;
					update_adpcm(chip, chip.adpcm_data >> 4);
					chip.drq = 1;

					/* we stay in this state until the time for this sample is complete */
					chip.clocks_left = chip.sample_rate * 4;
					if (--chip.nibbles_left == 0)
						chip.state = STATE.BLOCK_HEADER;
					else
						chip.state = STATE.NIBBLE_LSN;
					break;

				/* LSN state: process the lower nibble */
				case STATE.NIBBLE_LSN:
					update_adpcm(chip, chip.adpcm_data & 15);

					/* we stay in this state until the time for this sample is complete */
					chip.clocks_left = chip.sample_rate * 4;
					if (--chip.nibbles_left == 0)
						chip.state =STATE.BLOCK_HEADER;
					else
						chip.state =STATE.NIBBLE_MSN;
					break;
			}

			/* if there's a DRQ, fudge the state */
			if (chip.drq != 0)
			{
				chip.post_drq_state = chip.state;
				chip.post_drq_clocks = chip.clocks_left - 21;
				chip.state = STATE.DROP_DRQ;
				chip.clocks_left = 21;
			}
		}



		/************************************************************

			Stream callback

		*************************************************************/

		//static STREAM_UPDATE( upd7759_update )
		private void upd7759_update(byte ChipID, int[][] outputs, int samples)
		{
			//upd7759_state *chip = (upd7759_state *)param;
			_upd7759_state chip = UPD7759Data[ChipID];
			Int32 clocks_left = chip.clocks_left;
			Int16 sample = chip.sample;
			UInt32 step = chip.step;
			UInt32 pos = chip.pos;
			int[] buffer = outputs[0];
			int[] buffer2 = outputs[1];
			int bufferPtr = 0;
			int bufferPtr2 = 0;

			/* loop until done */
			if (chip.state != (sbyte)STATE.IDLE)
				while (samples != 0)
				{
					/* store the current sample */
					buffer[bufferPtr++] = sample << 7;
					buffer2[bufferPtr2++] = sample << 7;
					samples--;

					/* advance by the number of clocks/output sample */
					pos += step;

					/* handle clocks, but only in standalone mode */
					if (chip.ChipMode == 0)
					{
						while (chip.rom != null && pos >= FRAC_ONE)
						{
							int clocks_this_time = (int)(pos >> FRAC_BITS);
							if (clocks_this_time > clocks_left)
								clocks_this_time = clocks_left;

							/* clock once */
							pos -= (uint)(clocks_this_time * FRAC_ONE);
							clocks_left -= clocks_this_time;

							/* if we're out of clocks, time to handle the next state */
							if (clocks_left == 0)
							{
								/* advance one state; if we hit idle, bail */
								advance_state(chip);
								if (chip.state == (sbyte)STATE.IDLE)
									break;

								/* reimport the variables that we cached */
								clocks_left = chip.clocks_left;
								sample = chip.sample;
							}
						}
					}
					else
					{
						byte CntFour;

						if (clocks_left == 0)
						{
							advance_state(chip);
							clocks_left = chip.clocks_left;
						}

						// advance the state (4x because of Clock Divider /4)
						for (CntFour = 0; CntFour < 4; CntFour++)
						{
							clocks_left--;
							if (clocks_left == 0)
							{
								advance_state(chip);
								clocks_left = chip.clocks_left;
							}
						}
					}
				}

			/* if we got out early, just zap the rest of the buffer */
			if (samples != 0)
			{
				for (int i = 0; i < buffer.Length; i++) buffer[i] = 0;
				for (int i = 0; i < buffer2.Length; i++) buffer2[i] = 0;
			}

			/* flush the state back */
			chip.clocks_left = clocks_left;
			chip.pos = pos;
		}



		/************************************************************

			DRQ callback

		*************************************************************/

		/*static TIMER_CALLBACK( upd7759_slave_update )
		{
			upd7759_state *chip = (upd7759_state *)ptr;
			UINT8 olddrq = chip.drq;

			// update the stream
			//stream_update(chip.channel);

			// advance the state
			advance_state(chip);

			// if the DRQ changed, update it
			logerror("slave_update: DRQ %d->%d\n", olddrq, chip.drq);
			if (olddrq != chip.drq && chip.drqcallback)
				//(*chip.drqcallback)(chip.device, chip.drq);
				(*chip.drqcallback)(chip.drq);

			// set a timer to go off when that is done
			//if (chip.state != STATE_IDLE)
			//	timer_adjust_oneshot(chip.timer, attotime_mul(chip.clock_period, chip.clocks_left), 0);
		}*/


		/************************************************************

			Sound startup

		*************************************************************/

		private void upd7759_reset(_upd7759_state chip)
		{
			chip.pos = 0;
			chip.fifo_in = 0;
			chip.drq = 0;
			chip.state = (sbyte)STATE.IDLE;
			chip.clocks_left = 0;
			chip.nibbles_left = 0;
			chip.repeat_count = 0;
			chip.post_drq_state = (sbyte)STATE.IDLE;
			chip.post_drq_clocks = 0;
			chip.req_sample = 0;
			chip.last_sample = 0;
			chip.block_header = 0;
			chip.sample_rate = 0;
			chip.first_valid_header = 0;
			chip.offset = 0;
			chip.repeat_offset = 0;
			chip.adpcm_state = 0;
			chip.adpcm_data = 0;
			chip.sample = 0;

			// Valley Bell: reset buffer
			chip.data_buf[0] = chip.data_buf[1] = 0x00;
			chip.dbuf_pos_read = 0x00;
			chip.dbuf_pos_write = 0x00;

			/* turn off any timer */
			//if (chip.timer)
			//	timer_adjust_oneshot(chip.timer, attotime_never, 0);
			if (chip.ChipMode != 0)
				chip.clocks_left = -1;
		}


		//static DEVICE_RESET( upd7759 )
		private void device_reset_upd7759(byte ChipID)
		{
			_upd7759_state chip = UPD7759Data[ChipID];
			//upd7759_reset(get_safe_token(device));
			upd7759_reset(chip);
		}


		//static STATE_POSTLOAD( upd7759_postload )
		/*static void upd7759_postload(void* param)
		{
			upd7759_state *chip = (upd7759_state *)param;
			chip.rom = chip.rombase + chip.romoffset;
		}*/


		/*static void register_for_save(upd7759_state *chip, running_device *device)
		{
			state_save_register_device_item(device, 0, chip.pos);
			state_save_register_device_item(device, 0, chip.step);

			state_save_register_device_item(device, 0, chip.fifo_in);
			state_save_register_device_item(device, 0, chip.reset);
			state_save_register_device_item(device, 0, chip.start);
			state_save_register_device_item(device, 0, chip.drq);

			state_save_register_device_item(device, 0, chip.state);
			state_save_register_device_item(device, 0, chip.clocks_left);
			state_save_register_device_item(device, 0, chip.nibbles_left);
			state_save_register_device_item(device, 0, chip.repeat_count);
			state_save_register_device_item(device, 0, chip.post_drq_state);
			state_save_register_device_item(device, 0, chip.post_drq_clocks);
			state_save_register_device_item(device, 0, chip.req_sample);
			state_save_register_device_item(device, 0, chip.last_sample);
			state_save_register_device_item(device, 0, chip.block_header);
			state_save_register_device_item(device, 0, chip.sample_rate);
			state_save_register_device_item(device, 0, chip.first_valid_header);
			state_save_register_device_item(device, 0, chip.offset);
			state_save_register_device_item(device, 0, chip.repeat_offset);

			state_save_register_device_item(device, 0, chip.adpcm_state);
			state_save_register_device_item(device, 0, chip.adpcm_data);
			state_save_register_device_item(device, 0, chip.sample);

			state_save_register_device_item(device, 0, chip.romoffset);
			state_save_register_postload(device->machine, upd7759_postload, chip);
		}*/


		//static DEVICE_START( upd7759 )
		private int device_start_upd7759(byte ChipID, int clock)
		{
			//static const upd7759_interface defintrf = { 0 };
			//const upd7759_interface *intf = (device->baseconfig().static_config() != NULL) ? (const upd7759_interface *)device->baseconfig().static_config() : &defintrf;
			//const upd7759_interface* intf = &defintrf;
			//upd7759_state *chip = get_safe_token(device);
			_upd7759_state chip;

			if (ChipID >= MAX_CHIPS)
				return 0;

			chip = UPD7759Data[ChipID];
			//chip.device = device;
			chip.ChipMode = (byte)((clock & 0x80000000) >> 31);
			clock &= 0x7FFFFFFF;

			/* allocate a stream channel */
			//chip.channel = stream_create(device, 0, 1, device->clock()/4, chip, upd7759_update);

			/* compute the stepping rate based on the chip's clock speed */
			chip.step = 4 * FRAC_ONE;

			/* compute the clock period */
			//chip.clock_period = ATTOTIME_IN_HZ(device->clock());

			/* set the intial state */
			chip.state = (sbyte)STATE.IDLE;

			/* compute the ROM base or allocate a timer */
			//chip.rom = chip.rombase = *device->region();
			chip.romsize = 0x00;
			chip.rom = chip.rombase = null;
			chip.romPtr = 0;
			//if (chip.rom == NULL)
			//	chip.timer = timer_alloc(device->machine, upd7759_slave_update, chip);
			chip.romoffset = 0x00;

			/* set the DRQ callback */
			//chip.drqcallback = intf->drqcallback;

			/* assume /RESET and /START are both high */
			chip.reset = 1;
			chip.start = 1;

			/* toggle the reset line to finish the reset */
			upd7759_reset(chip);

			//register_for_save(chip, device);

			return clock / 4;
		}

		private void device_stop_upd7759(byte ChipID)
		{
			_upd7759_state chip = UPD7759Data[ChipID];

			//free(chip.rombase); chip.rombase = NULL;
			chip.rom = null;
			chip.romPtr = 0;
			chip.rombase = null;

			return;
		}



		/************************************************************

			I/O handlers

		*************************************************************/

		//void upd7759_reset_w(running_device *device, UINT8 data)
		private void upd7759_reset_w(byte ChipID, byte data)
		{
			/* update the reset value */
			//upd7759_state *chip = get_safe_token(device);
			_upd7759_state chip = UPD7759Data[ChipID];
			byte oldreset = chip.reset;
			chip.reset = (byte)((data != 0) ? 1 : 0);

			/* update the stream first */
			//stream_update(chip.channel);

			/* on the falling edge, reset everything */
			if (oldreset != 0 && chip.reset == 0)
				upd7759_reset(chip);
		}

		//void upd7759_start_w(running_device *device, UINT8 data)
		private void upd7759_start_w(byte ChipID, byte data)
		{
			/* update the start value */
			//upd7759_state *chip = get_safe_token(device);
			_upd7759_state chip = UPD7759Data[ChipID];
			byte oldstart = chip.start;
			chip.start = (byte)((data != 0) ? 1 : 0);

#if DEBUG
			if (DEBUG_STATES != 0) logerror(string.Format("upd7759_start_w: {0}->{1}\n", oldstart, chip.start));
#endif
			/* update the stream first */
			//stream_update(chip.channel);

			/* on the rising edge, if we're idle, start going, but not if we're held in reset */
			if (chip.state == STATE.IDLE && oldstart == 0 && chip.start != 0 && chip.reset != 0)
			{
				chip.state = STATE.START;

				/* for slave mode, start the timer going */
				//if (chip.timer)
				//	timer_adjust_oneshot(chip.timer, attotime_zero, 0);
				chip.clocks_left = 0;
			}
		}


		//WRITE8_DEVICE_HANDLER( upd7759_port_w )
		private void upd7759_port_w(byte ChipID, int offset, byte data)
		{
			/* update the FIFO value */
			//upd7759_state *chip = get_safe_token(device);
			_upd7759_state chip = UPD7759Data[ChipID];

			if (chip.ChipMode == 0)
			{
				chip.fifo_in = data;
			}
			else
			{
				// Valley Bell: added FIFO buffer for Slave mode
				chip.data_buf[chip.dbuf_pos_write] = data;
				chip.dbuf_pos_write++;
				chip.dbuf_pos_write &= 0x3F;
			}
		}


		//int upd7759_busy_r(running_device *device)
		private int upd7759_busy_r(byte ChipID)
		{
			/* return /BUSY */
			//upd7759_state *chip = get_safe_token(device);
			_upd7759_state chip = UPD7759Data[ChipID];
			return (chip.state == (sbyte)STATE.IDLE) ? 1 : 0;
		}


		//void upd7759_set_bank_base(running_device *device, UINT32 base)
		private void upd7759_set_bank_base(byte ChipID, UInt32 base_)
		{
			//upd7759_state *chip = get_safe_token(device);
			_upd7759_state chip = UPD7759Data[ChipID];
			chip.rom = chip.rombase;
			chip.romPtr = (int)base_;
			chip.romoffset = base_;
		}

		private void upd7759_write(byte ChipID, byte Port, byte Data)
		{
			switch (Port)
			{
				case 0x00:
					upd7759_reset_w(ChipID, Data);
					break;
				case 0x01:
					upd7759_start_w(ChipID, Data);
					break;
				case 0x02:
					upd7759_port_w(ChipID, 0x00, Data);
					break;
				case 0x03:
					upd7759_set_bank_base(ChipID, (uint)(Data * 0x20000));
					break;
			}

			return;
		}

		private void upd7759_write_rom(byte ChipID, int ROMSize, int DataStart, int DataLength,
					   byte[] ROMData)
		{
			_upd7759_state chip = UPD7759Data[ChipID];

			if (chip.romsize != ROMSize)
			{
				chip.rombase = new byte[ROMSize];// (UINT8*) realloc(chip.rombase, ROMSize);
				chip.romsize = (uint)ROMSize;
				for (int i = 0; i < ROMSize; i++) chip.rombase[i] = 0xff;

				chip.rom = chip.rombase;
				chip.romPtr = (int)chip.romoffset;
			}
			if (DataStart > ROMSize)
				return;
			if (DataStart + DataLength > ROMSize)
				DataLength = ROMSize - DataStart;

			for (int i = 0; i < DataLength; i++) chip.rombase[i + DataStart] = ROMData[i];

			return;
		}

		public void uPD7759_write_rom2(byte ChipID, int ROMSize, int DataStart, int DataLength,  byte[] ROMData,int SrcStartAdr)
		{
			_upd7759_state chip = UPD7759Data[ChipID];

			if (chip.romsize != ROMSize)
			{
				chip.rombase = new byte[ROMSize];// (UINT8*) realloc(chip.rombase, ROMSize);
				chip.romsize = (uint)ROMSize;
				for (int i = 0; i < ROMSize; i++) chip.rombase[i] = 0xff;

				chip.rom = chip.rombase;
				chip.romPtr = (int)chip.romoffset;
			}
			if (DataStart > ROMSize)
				return;
			if (DataStart + DataLength > ROMSize)
				DataLength = ROMSize - DataStart;

			for (int i = 0; i < DataLength; i++) chip.rombase[i + DataStart] = ROMData[i + SrcStartAdr];

			return;
		}
		
		public override int Write(byte ChipID, int port, int adr, int data)
        {
			upd7759_write(ChipID, (byte)adr, (byte)data);
			return 0;
        }

		public _upd7759_state uPD7759_r(int ChipID)
		{
			return UPD7759Data[ChipID];
		}


		/**************************************************************************
		 * Generic get_info
		 **************************************************************************/

		/*DEVICE_GET_INFO( upd7759 )
		{
			switch (state)
			{
				// --- the following bits of info are returned as 64-bit signed integers ---
				case DEVINFO_INT_TOKEN_BYTES:					info->i = sizeof(upd7759_state);			break;

				// --- the following bits of info are returned as pointers to data or functions ---
				case DEVINFO_FCT_START:							info->start = DEVICE_START_NAME( upd7759 );		break;
				case DEVINFO_FCT_STOP:							// Nothing										break;
				case DEVINFO_FCT_RESET:							info->reset = DEVICE_RESET_NAME( upd7759 );		break;

				// --- the following bits of info are returned as NULL-terminated strings ---
				case DEVINFO_STR_NAME:							strcpy(info->s, "UPD7759");						break;
				case DEVINFO_STR_FAMILY:					strcpy(info->s, "NEC ADPCM");					break;
				case DEVINFO_STR_VERSION:					strcpy(info->s, "1.0");							break;
				case DEVINFO_STR_SOURCE_FILE:						strcpy(info->s, __FILE__);						break;
				case DEVINFO_STR_CREDITS:					strcpy(info->s, "Copyright Nicola Salmoria and the MAME Team"); break;
			}
		}*/


		//DEFINE_LEGACY_SOUND_DEVICE(UPD7759, upd7759);

	}
}
