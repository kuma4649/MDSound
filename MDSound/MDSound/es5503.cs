﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MDSound
{
	public class Es5503 : Instrument
	{
		public override string Name { get { return "ES5503"; } set { } }
		public override string ShortName { get { return "ES5503"; } set { } }

		private ES5503Chip[] es5503 = new ES5503Chip[2] { new ES5503Chip(), new ES5503Chip() };

		public override void Reset(byte ChipID)
		{
			device_reset_es5503(es5503[ChipID]);
		}

		public override uint Start(byte ChipID, uint clock)
		{
			return device_start_es5503(es5503[ChipID], clock, 0);
		}

		public override uint Start(byte ChipID, uint clock, uint ClockValue, params object[] option)
		{
			return device_start_es5503(es5503[ChipID], clock, 0);
		}

		public override void Stop(byte ChipID)
		{
			device_stop_es5503(es5503[ChipID]);
		}

		public override void Update(byte ChipID, int[][] outputs, int samples)
		{
			es5503_pcm_update(es5503[ChipID], (uint)samples, outputs);
		}

		public override int Write(byte ChipID, int port, int adr, int data)
		{
			es5503_w(es5503[ChipID], (byte)adr, (byte)data);
			return 0;
		}

        public void SetMute(byte chipID, int v)
        {
			es5503_set_mute_mask(es5503[chipID], (uint)v);
        }

        public void WriteMem(byte chipID, int adr, byte data)
        {
			es5503_write_ram(es5503[chipID], (uint)adr, 1, new byte[] { data });
        }





        // license:BSD-3-Clause
        // copyright-holders:R. Belmont
        /*
		  ES5503 - Ensoniq ES5503 "DOC" emulator v2.1.2
		  By R. Belmont.
		  Copyright R. Belmont.
		  History: the ES5503 was the next design after the famous C64 "SID" by Bob Yannes.
		  It powered the legendary Mirage sampler (the first affordable pro sampler) as well
		  as the ESQ-1 synth/sequencer.  The ES5505 (used in Taito's F3 System) and 5506
		  (used in the "Soundscape" series of ISA PC sound cards) followed on a fundamentally
		  similar architecture.
		  Bugs: On the real silicon, oscillators 30 and 31 have random volume fluctuations and are
		  unusable for playback.  We don't attempt to emulate that. :-)
		  Additionally, in "swap" mode, there's one cycle when the switch takes place where the
		  oscillator's output is 0x80 (centerline) regardless of the sample data.  This can
		  cause audible clicks and a general degradation of audio quality if the correct sample
		  data at that point isn't 0x80 or very near it.
		  Changes:
		  0.2 (RB) - improved behavior for volumes > 127, fixes missing notes in Nucleus & missing voices in Thexder
		  0.3 (RB) - fixed extraneous clicking, improved timing behavior for e.g. Music Construction Set & Music Studio
		  0.4 (RB) - major fixes to IRQ semantics and end-of-sample handling.
		  0.5 (RB) - more flexible wave memory hookup (incl. banking) and save state support.
		  1.0 (RB) - properly respects the input clock
		  2.0 (RB) - C++ conversion, more accurate oscillator IRQ timing
		  2.1 (RB) - Corrected phase when looping; synthLAB, Arkanoid, and Arkanoid II no longer go out of tune
		  2.1.1 (RB) - Fixed issue introduced in 2.0 where IRQs were delayed
		  2.1.2 (RB) - Fixed SoundSmith POLY.SYNTH inst where one-shot on the even oscillator and swap on the odd should loop.
					   Conversely, the intro voice in FTA Delta Demo has swap on the even and one-shot on the odd and doesn't
					   want to loop.
		*/

        //# include <stdlib.h>
        //# include <string.h>

        //# include "../../stdtype.h"
        //# include "../EmuStructs.h"
        //# include "../EmuCores.h"
        //# include "../snddef.h"
        //# include "../EmuHelper.h"
        //# include "es5503.h"


        //		static void es5503_w(void* info, byte offset, byte data);
        //		static byte es5503_r(void* info, byte offset);

        //		static void es5503_pcm_update(void* param, UInt32 samples, DEV_SMPL** outputs);
        //		static byte device_start_es5503(const DEV_GEN_CFG* cfg, DEV_INFO* retDevInf);
        //static void device_stop_es5503(void* info);
        //		static void device_reset_es5503(void* info);

        //		static void es5503_write_ram(void* info, UInt32 offset, UInt32 length, const byte* data);

        //		static void es5503_set_mute_mask(void* info, UInt32 MuteMask);
        //		static void es5503_set_srchg_cb(void* info, DEVCB_SRATE_CHG CallbackFunc, void* DataPtr);


        //		static DEVDEF_RWFUNC devFunc[] =
        //		{
        //	{RWF_REGISTER | RWF_WRITE, DEVRW_A8D8, 0, es5503_w},
        //	{RWF_REGISTER | RWF_READ, DEVRW_A8D8, 0, es5503_r},
        //	{RWF_MEMORY | RWF_WRITE, DEVRW_BLOCK, 0, es5503_write_ram},
        //	{RWF_CHN_MUTE | RWF_WRITE, DEVRW_ALL, 0, es5503_set_mute_mask},
        //	{0x00, 0x00, 0, NULL}
        //};
        //		static DEV_DEF devDef =
        //		{
        //	"ES5503", "MAME", FCC_MAME,

        //	device_start_es5503,
        //	device_stop_es5503,
        //	device_reset_es5503,
        //	es5503_pcm_update,

        //	NULL,	// SetOptionBits
        //	es5503_set_mute_mask,
        //	NULL,	// SetPanning
        //	es5503_set_srchg_cb,	// SetSampleRateChangeCallback
        //	NULL,	// SetLoggingCallback
        //	NULL,	// LinkDevice

        //	devFunc,	// rwFuncs
        //};

        //		const DEV_DEF* devDefList_ES5503[] =
        //		{
        //	&devDef,
        //	NULL
        //};


        private enum MODE
		{
			FREE = 0,
			ONESHOT = 1,
			SYNCAM = 2,
			SWAP = 3
		};

		private class ES5503Osc
		{
			public UInt16 freq;
			public UInt16 wtsize;
			public byte control;
			public byte vol;
			public byte data;
			public UInt32 wavetblpointer;
			public byte wavetblsize;
			public byte resolution;

			public UInt32 accumulator;
			public byte irqpend;

			public byte Muted;
		}

		private class ES5503Chip
		{
			//public DEV_DATA _devData;

			public UInt32 dramsize;
			public byte[] docram;

			public Action<object, byte> irq_func;   // IRQ callback
			public object irq_param;

			public Func<object, byte, byte> adc_func;   // callback for the 5503's built-in analog to digital converter
			public object adc_param;

			public ES5503Osc[] oscillators = new ES5503Osc[32] {
				new ES5503Osc(), new ES5503Osc(), new ES5503Osc(), new ES5503Osc(),
				new ES5503Osc(), new ES5503Osc(), new ES5503Osc(), new ES5503Osc(),
				new ES5503Osc(), new ES5503Osc(), new ES5503Osc(), new ES5503Osc(),
				new ES5503Osc(), new ES5503Osc(), new ES5503Osc(), new ES5503Osc(),
				new ES5503Osc(), new ES5503Osc(), new ES5503Osc(), new ES5503Osc(),
				new ES5503Osc(), new ES5503Osc(), new ES5503Osc(), new ES5503Osc(),
				new ES5503Osc(), new ES5503Osc(), new ES5503Osc(), new ES5503Osc(),
				new ES5503Osc(), new ES5503Osc(), new ES5503Osc(), new ES5503Osc()
			};

			public byte oscsenabled;      // # of oscillators enabled
			public byte rege0;            // contents of register 0xe0

			public byte channel_strobe;

			public UInt32 clock;
			public byte output_channels;
			public byte outchn_mask;
			public UInt32 output_rate;

			//public DEVCB_SRATE_CHG SmpRateFunc;
			//public void* SmpRateData;
		}

		// useful constants
		private static UInt16[] wavesizes = new UInt16[8] { 256, 512, 1024, 2048, 4096, 8192, 16384, 32768 };
		private static UInt32[] wavemasks = new uint[8] { 0x1ff00, 0x1fe00, 0x1fc00, 0x1f800, 0x1f000, 0x1e000, 0x1c000, 0x18000 };
		private static UInt32[] accmasks = new uint[8] { 0xff, 0x1ff, 0x3ff, 0x7ff, 0xfff, 0x1fff, 0x3fff, 0x7fff };
		private static int[] resshifts = new int[8] { 9, 10, 11, 12, 13, 14, 15, 16 };

		// halt_osc: handle halting an oscillator
		// chip = chip ptr
		// onum = oscillator #
		// type = 1 for 0 found in sample data, 0 for hit end of table size
		private void es5503_halt_osc(ES5503Chip chip, int onum, int type, ref UInt32 accumulator, int resshift)
		{
			ES5503Osc pOsc = chip.oscillators[onum];
			ES5503Osc pPartner = chip.oscillators[onum ^ 1];
			int mode = (pOsc.control >> 1) & 3;
			int partnerMode = (pPartner.control >> 1) & 3;

			// if 0 found in sample data or mode is not free-run, halt this oscillator
			if ((mode != (int)MODE.FREE) || (type != 0))
			{
				pOsc.control |= 1;
			}
			else    // preserve the relative phase of the oscillator when looping
			{
				UInt16 wtsize = (UInt16)(pOsc.wtsize - 1);
				UInt32 altram = accumulator >> resshift;

				if (altram > wtsize)
				{
					altram -= wtsize;
				}
				else
				{
					altram = 0;
				}

				accumulator = altram << resshift;
			}

			// if we're in swap mode or we're the even oscillator and the partner is in swap mode,
			// start the partner.
			if ((mode == (int)MODE.SWAP) || ((partnerMode == (int)MODE.SWAP) && ((onum & 1) == 0)))
			{
				pPartner.control &= 0xff;// ~1;    // clear the halt bit
				pPartner.accumulator = 0;  // and make sure it starts from the top (does this also need phase preservation?)
			}

			// IRQ enabled for this voice?
			if ((pOsc.control & 0x08) != 0)
			{
				pOsc.irqpend = 1;

				if (chip.irq_func != null)
					chip.irq_func(chip.irq_param, 1);
			}
		}

		private void es5503_pcm_update(ES5503Chip param, UInt32 samples, int[][] outputs)
		{
			byte osc;
			UInt32 snum;
			UInt32 ramptr;
			ES5503Chip chip = param;
			byte chnsStereo, chan;

			for (int i = 0; i < samples; i++)
			{
				outputs[0][i] = 0;
				outputs[1][i] = 0;
			}
			if (chip.docram == null)
				return;

			chnsStereo = (byte)(chip.output_channels & 0xff);// ~1;
			for (osc = 0; osc < chip.oscsenabled; osc++)
			{
				ES5503Osc pOsc = chip.oscillators[osc];

				if ((pOsc.control & 1) == 0 && pOsc.Muted == 0)
				{
					UInt32 wtptr = pOsc.wavetblpointer & wavemasks[pOsc.wavetblsize];
					UInt32 altram;
					UInt32 acc = pOsc.accumulator;
					UInt16 wtsize = (UInt16)(pOsc.wtsize - 1);
					UInt16 freq = pOsc.freq;
					Int16 vol = pOsc.vol;
					byte chnMask = (byte)((pOsc.control >> 4) & 0x0F);
					int resshift = resshifts[pOsc.resolution] - pOsc.wavetblsize;
					UInt32 sizemask = accmasks[pOsc.wavetblsize];
					Int32 outData;

					chnMask &= chip.outchn_mask;
					for (snum = 0; snum < samples; snum++)
					{
						altram = acc >> resshift;
						ramptr = altram & sizemask;

						acc += freq;

						// channel strobe is always valid when reading; this allows potentially banking per voice
						chip.channel_strobe = (byte)((pOsc.control >> 4) & 0xf);
						pOsc.data = chip.docram[ramptr + wtptr];

						if (pOsc.data == 0x00)
						{
							es5503_halt_osc(chip, osc, 1, ref acc, resshift);
						}
						else
						{
							outData = (pOsc.data - 0x80) * vol;

							// send groups of 2 channels to L or R
							for (chan = 0; chan < chnsStereo; chan++)
							{
								if (chan == chnMask)
									outputs[chan & 1][snum] += outData;
							}
							outData = (outData * 181) >> 8; // outData *= sqrt(2)
															// send remaining channels to L+R
							for (; chan < chip.output_channels; chan++)
							{
								if (chan == chnMask)
								{
									outputs[0][snum] += outData;
									outputs[1][snum] += outData;
								}
							}

							if (altram >= wtsize)
							{
								es5503_halt_osc(chip, osc, 0, ref acc, resshift);
							}
						}

						// if oscillator halted, we've got no more samples to generate
						if ((pOsc.control & 1) != 0)
						{
							pOsc.control |= 1;
							break;
						}
					}

					pOsc.accumulator = acc;
				}
			}
		}


		private byte device_start_es5503(ES5503Chip info, uint clock,byte flags)//(const DEV_GEN_CFG* cfg, DEV_INFO* retDevInf)
		{
			ES5503Chip chip;
			chip = info;//	 (ES5503Chip)calloc(1, sizeof(ES5503Chip));
			if (chip == null)
				return 0xFF;

			chip.irq_func = null;
			chip.irq_param = null;
			chip.adc_func = null;
			chip.adc_param = null;

			chip.dramsize = 0x20000;   // 128 KB
			chip.docram = new byte[chip.dramsize];// (byte*)malloc(chip.dramsize);
			chip.clock = clock;// cfg.clock;
			chip.output_channels = flags;// cfg.flags;
			if (chip.output_channels == 0)
				chip.output_channels = 1;
			chip.outchn_mask = (byte)pow2_mask(chip.output_channels);

			chip.oscsenabled = 1;
			chip.output_rate = (uint)((chip.clock / 8) / (2 + chip.oscsenabled));  // (input clock / 8) / # of oscs. enabled + 2

			es5503_set_mute_mask(chip, 0x00000000);

			//chip._devData.chipInf = chip;
			//INIT_DEVINF(retDevInf, &chip._devData, chip.output_rate, &devDef);

			return 0x00;
		}

		private UInt32 pow2_mask(UInt32 v)
		{
			if (v == 0)
				return 0;
			v--;
			v |= (v >> 1);
			v |= (v >> 2);
			v |= (v >> 4);
			v |= (v >> 8);
			v |= (v >> 16);
			return v;
		}

		private void device_stop_es5503(ES5503Chip info)
		{
			ES5503Chip chip = info;
			chip = null;
			//free(chip.docram);
			//free(chip);

			return;
		}

		private void device_reset_es5503(ES5503Chip info)
		{
			ES5503Chip chip = info;
			int osc;
			ES5503Osc tempOsc;

			chip.rege0 = 0xff;

			for (osc = 0; osc < 32; osc++)
			{
				tempOsc = chip.oscillators[osc];
				tempOsc.freq = 0;
				tempOsc.wtsize = 0;
				tempOsc.control = 0;
				tempOsc.vol = 0;
				tempOsc.data = 0x80;
				tempOsc.wavetblpointer = 0;
				tempOsc.wavetblsize = 0;
				tempOsc.resolution = 0;
				tempOsc.accumulator = 0;
				tempOsc.irqpend = 0;
			}

			chip.oscsenabled = 1;

			chip.channel_strobe = 0;
			for (int i = 0; i < chip.dramsize; i++) chip.docram[i] = 0x00;

			chip.output_rate = (uint)((chip.clock / 8) / (2 + chip.oscsenabled));  // (input clock / 8) / # of oscs. enabled + 2
			//if (chip.SmpRateFunc != null)
				//chip.SmpRateFunc(chip.SmpRateData, chip.output_rate);

			return;
		}


		private byte es5503_r(ES5503Chip info, byte offset)
		{
			byte retval;
			int i;
			ES5503Chip chip = info;

			if (offset < 0xe0)
			{
				byte osc = (byte)(offset & 0x1f);

				switch (offset & 0xe0)
				{
					case 0:     // freq lo
						return (byte)(chip.oscillators[osc].freq & 0xff);

					case 0x20:      // freq hi
						return (byte)(chip.oscillators[osc].freq >> 8);

					case 0x40:  // volume
						return chip.oscillators[osc].vol;

					case 0x60:  // data
						return chip.oscillators[osc].data;

					case 0x80:  // wavetable pointer
						return (byte)((chip.oscillators[osc].wavetblpointer >> 8) & 0xff);

					case 0xa0:  // oscillator control
						return chip.oscillators[osc].control;

					case 0xc0:  // bank select / wavetable size / resolution
						retval = 0;
						if ((chip.oscillators[osc].wavetblpointer & 0x10000) != 0)
						{
							retval |= 0x40;
						}

						retval |= (byte)(chip.oscillators[osc].wavetblsize << 3);
						retval |= chip.oscillators[osc].resolution;
						return retval;
				}
			}
			else     // global registers
			{
				switch (offset)
				{
					case 0xe0:  // interrupt status
						retval = chip.rege0;

						if (chip.irq_func != null)
							chip.irq_func(chip.irq_param, 0);

						// scan all oscillators
						for (i = 0; i < chip.oscsenabled; i++)
						{
							if (chip.oscillators[i].irqpend != 0)
							{
								// signal this oscillator has an interrupt
								retval = (byte)(i << 1);

								chip.rege0 = (byte)(retval | 0x80);

								// and clear its flag
								chip.oscillators[i].irqpend = 0;
								break;
							}
						}

						// if any oscillators still need to be serviced, assert IRQ again immediately
						for (i = 0; i < chip.oscsenabled; i++)
						{
							if (chip.oscillators[i].irqpend != 0)
							{
								if (chip.irq_func != null)
									chip.irq_func(chip.irq_param, 1);
								break;
							}
						}

						return (byte)(retval | 0x41);

					case 0xe1:  // oscillator enable
						return (byte)((chip.oscsenabled - 1) << 1);

					case 0xe2:  // A/D converter
						if (chip.adc_func != null)
							return chip.adc_func(chip.adc_param, 0);
						break;
				}
			}

			return 0;
		}

		private void es5503_w(ES5503Chip info, byte offset, byte data)
		{
			ES5503Chip chip = info;

			if (offset < 0xe0)
			{
				int osc = offset & 0x1f;

				switch (offset & 0xe0)
				{
					case 0:     // freq lo
						chip.oscillators[osc].freq &= 0xff00;
						chip.oscillators[osc].freq |= data;
						break;

					case 0x20:      // freq hi
						chip.oscillators[osc].freq &= 0x00ff;
						chip.oscillators[osc].freq |= (ushort)(data << 8);
						break;

					case 0x40:  // volume
						chip.oscillators[osc].vol = data;
						break;

					case 0x60:  // data - ignore writes
						break;

					case 0x80:  // wavetable pointer
						chip.oscillators[osc].wavetblpointer = (uint)(data << 8);
						break;

					case 0xa0:  // oscillator control
								// if a fresh key-on, reset the accumulator
						if ((chip.oscillators[osc].control & 1) != 0 && ((data & 1) == 0))
						{
							chip.oscillators[osc].accumulator = 0;
						}
						chip.oscillators[osc].control = data;
						break;

					case 0xc0:  // bank select / wavetable size / resolution
						if ((data & 0x40) != 0)    // bank select - not used on the Apple IIgs
						{
							chip.oscillators[osc].wavetblpointer |= 0x10000;
						}
						else
						{
							chip.oscillators[osc].wavetblpointer &= 0xffff;
						}

						chip.oscillators[osc].wavetblsize = (byte)((data >> 3) & 7);
						chip.oscillators[osc].wtsize = wavesizes[chip.oscillators[osc].wavetblsize];
						chip.oscillators[osc].resolution = (byte)(data & 7);
						break;
				}
			}
			else     // global registers
			{
				switch (offset)
				{
					case 0xe0:  // interrupt status
						break;

					case 0xe1:  // oscillator enable
						chip.oscsenabled = (byte)(1 + ((data >> 1) & 0x1f));

						chip.output_rate = (uint)((chip.clock / 8) / (2 + chip.oscsenabled));
						//if (chip.SmpRateFunc != null)
							//chip.SmpRateFunc(chip.SmpRateData, chip.output_rate);
						break;

					case 0xe2:  // A/D converter
						break;
				}
			}
		}

		private void es5503_write_ram(ES5503Chip info, UInt32 offset, UInt32 length, byte[] data)
		{
			ES5503Chip chip = info;

			if (offset >= chip.dramsize)
				return;
			if (offset + length > chip.dramsize)
				length = chip.dramsize - offset;

			for (int i = 0; i < length; i++)
			{
				chip.docram[offset + i] = data[i];
			}

			return;
		}

		private void es5503_set_mute_mask(ES5503Chip info, UInt32 MuteMask)
		{
			ES5503Chip chip = info;
			byte CurChn;

			for (CurChn = 0; CurChn < 32; CurChn++)
				chip.oscillators[CurChn].Muted = (byte)((MuteMask >> CurChn) & 0x01);

			return;
		}

		private void es5503_set_srchg_cb()//(ES5503Chip info, DEVCB_SRATE_CHG CallbackFunc, void* DataPtr)
		{
			//ES5503Chip chip = info;

			//// set Sample Rate Change Callback routine
			//chip.SmpRateFunc = CallbackFunc;
			//chip.SmpRateData = DataPtr;

			return;
		}

	}
}
