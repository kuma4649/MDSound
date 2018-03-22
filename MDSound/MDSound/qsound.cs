using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDSound
{
    public class qsound : Instrument
    {
        public override void Reset(byte ChipID)
        {
            device_reset_qsound(ChipID);
        }

        public override uint Start(byte ChipID, uint clock)
        {
            return (UInt32)device_start_qsound(ChipID, (Int32)4000000);
        }

        public override uint Start(byte ChipID, uint clock, uint ClockValue, params object[] option)
        {
            return (UInt32)device_start_qsound(ChipID, (Int32)ClockValue);
        }

        public override void Stop(byte ChipID)
        {
            device_stop_qsound(ChipID);
        }

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {
            qsound_update(ChipID, outputs, samples);
        }





        /*********************************************************

            Capcom Q-Sound system

        *********************************************************/

        //#pragma once

        //#include "devlegcy.h"

        private const Int32 QSOUND_CLOCK = 4000000;   /* default 4MHz clock */

        //private void qsound_update(byte ChipID, Int32[][] outputs, Int32 samples) { }
        //private Int32 device_start_qsound(byte ChipID, Int32 clock) { return 0; }
        //private void device_stop_qsound(byte ChipID) { }
        //private void device_reset_qsound(byte ChipID) { }

        //WRITE8_DEVICE_HANDLER( qsound_w );
        //READ8_DEVICE_HANDLER( qsound_r );
        //private void qsound_w(byte ChipID, Int32 offset, byte data) { }
        //private byte qsound_r(byte ChipID, Int32 offset) { return 0; }

        //private void qsound_write_rom(byte ChipID, Int32 ROMSize, Int32 DataStart, Int32 DataLength, byte[] ROMData) { }
        //private void qsound_set_mute_mask(byte ChipID, UInt32 MuteMask) { }

        //DECLARE_LEGACY_SOUND_DEVICE(QSOUND, qsound);






        /***************************************************************************

            Capcom System QSound(tm)
        ========================

          Driver by Paul Leaman (paul@vortexcomputing.demon.co.uk)
        and Miguel Angel Horna (mahorna@teleline.es)

          A 16 channel stereo sample player.

          QSpace position is simulated by panning the sound in the stereo space.

          Register
          0  xxbb   xx = unknown bb = start high address
          1  ssss   ssss = sample start address
          2  pitch
          3  unknown (always 0x8000)
          4  loop offset from end address
          5  end
          6  master channel volume
          7  not used
          8  Balance (left=0x0110  centre=0x0120 right=0x0130)
          9  unknown (most fixed samples use 0 for this register)

          Many thanks to CAB (the author of Amuse), without whom this probably would
          never have been finished.

          If anybody has some information about this hardware, please send it to me
          to mahorna@teleline.es or 432937@cepsz.unizar.es.
          http://teleline.terra.es/personal/mahorna

        ***************************************************************************/

        //#include "emu.h"
        //# include "mamedef.h"
        //# ifdef _DEBUG
        //# include <stdio.h>
        //#endif
        //# include <stdlib.h>
        //# include <string.h>	// for memset
        //# include <stddef.h>	// for NULL
        //# include <math.h>
        //# include "qsound.h"

        /*
        Debug defines
        */
        private const Int32 LOG_WAVE = 0;
        private const Int32 VERBOSE = 0;
        private Int32 LOG(Int32 x)
        {
            //do { if (VERBOSE!=0) logerror x; } while (0!=0);}
            return 0;
        }

        /* 8 bit source ROM samples */
        //typedef INT8 sbyte;


        private const Int32 QSOUND_CLOCKDIV = 166;       /* Clock divider */
        private const Int32 QSOUND_CHANNELS = 16;
        //typedef stream_sample_t Int32;

        public class QSOUND_CHANNEL
        {
            public UInt32 bank;        // bank
            public UInt32 address;     // start/cur address
            public UInt16 loop;        // loop address
            public UInt16 end;         // end address
            public UInt32 freq;        // frequency
            public UInt16 vol;         // master volume

            // work variables
            public byte enabled;      // key on / key off
            public int lvol;           // left volume
            public int rvol;           // right volume
            public UInt32 step_ptr;    // current offset counter

            public byte Muted;
        };

        public class qsound_state
        {
            /* Private variables */
            //sound_stream * stream;				/* Audio stream */
            public QSOUND_CHANNEL[] channel = new qsound.QSOUND_CHANNEL[QSOUND_CHANNELS];

            public UInt16 data;            /* register latch data */
            public sbyte[] sample_rom;  /* Q sound sample ROM */
            public UInt32 sample_rom_length;

            public Int32[] pan_table = new Int32[33];      /* Pan volume table */

            //FILE *fpRawDataL;
            //FILE *fpRawDataR;
        };

        private const Int32 MAX_CHIPS = 0x02;
        private qsound_state[] QSoundData = new qsound_state[MAX_CHIPS] { new qsound_state(), new qsound_state() };

        public override string Name { get { return "QSound"; } set { } }
        public override string ShortName { get { return "QSND"; } set { } }

        /*INLINE qsound_state *get_safe_token(device_t *device)
        {
            assert(device != NULL);
            assert(device->type() == QSOUND);
            return (qsound_state *)downcast<legacy_device_base *>(device)->token();
        }*/


        /* Function prototypes */
        //static STREAM_UPDATE( qsound_update );
        //private void qsound_set_command(qsound_state chip, byte address, UInt16 data) { }

        //static DEVICE_START( qsound )
        public Int32 device_start_qsound(byte ChipID, Int32 clock)
        {
            //qsound_state *chip = get_safe_token(device);
            qsound_state chip;
            Int32 i;

            if (ChipID >= MAX_CHIPS)
                return 0;

            chip = QSoundData[ChipID];

            //chip.sample_rom = (sbyte *)*device->region();
            //chip.sample_rom_length = device->region()->bytes();
            chip.sample_rom = null;
            chip.sample_rom_length = 0x00;

            /* Create pan table */
            for (i = 0; i < 33; i++)
                chip.pan_table[i] = (int)((256 / Math.Sqrt(32.0)) * Math.Sqrt((double)i));

            // init sound regs
            for (i = 0; i < chip.channel.Length; i++) chip.channel[i] = new QSOUND_CHANNEL();
            //memset(chip.channel, 0, sizeof(chip.channel));

            //	LOG(("Pan table\n"));
            //	for (i=0; i<33; i++)
            //		LOG(("%02x ", chip.pan_table[i]));

            /* Allocate stream */
            /*chip.stream = device->machine().sound().stream_alloc(
                *device, 0, 2,
                device->clock() / QSOUND_CLOCKDIV,
                chip,
                qsound_update );*/

            /*if (LOG_WAVE)
            {
                chip.fpRawDataR=fopen("qsoundr.raw", "w+b");
                chip.fpRawDataL=fopen("qsoundl.raw", "w+b");
            }*/

            /* state save */
            /*for (i=0; i<QSOUND_CHANNELS; i++)
            {
                device->save_item(NAME(chip.channel[i].bank), i);
                device->save_item(NAME(chip.channel[i].address), i);
                device->save_item(NAME(chip.channel[i].pitch), i);
                device->save_item(NAME(chip.channel[i].loop), i);
                device->save_item(NAME(chip.channel[i].end), i);
                device->save_item(NAME(chip.channel[i].vol), i);
                device->save_item(NAME(chip.channel[i].pan), i);
                device->save_item(NAME(chip.channel[i].key), i);
                device->save_item(NAME(chip.channel[i].lvol), i);
                device->save_item(NAME(chip.channel[i].rvol), i);
                device->save_item(NAME(chip.channel[i].lastdt), i);
                device->save_item(NAME(chip.channel[i].offset), i);
            }*/

            for (i = 0; i < QSOUND_CHANNELS; i++)
            {
                chip.channel[i].Muted = 0x00;
            }

            return clock / QSOUND_CLOCKDIV;
        }

        //static DEVICE_STOP( qsound )
        public void device_stop_qsound(byte ChipID)
        {
            //qsound_state *chip = get_safe_token(device);
            qsound_state chip = QSoundData[ChipID];
            /*if (chip.fpRawDataR)
            {
                fclose(chip.fpRawDataR);
            }
            chip.fpRawDataR = NULL;
            if (chip.fpRawDataL)
            {
                fclose(chip.fpRawDataL);
            }
            chip.fpRawDataL = NULL;*/
            //free(chip.sample_rom);
            chip.sample_rom = null;
        }

        public void device_reset_qsound(byte ChipID)
        {
            qsound_state chip = QSoundData[ChipID];
            Int32 adr;

            // init sound regs
            for (Int32 i = 0; i < chip.channel.Length; i++) chip.channel[i] = new QSOUND_CHANNEL();
            //memset(chip.channel, 0, sizeof(chip.channel));

            for (adr = 0x7f; adr >= 0; adr--)
                qsound_set_command(chip, (byte)adr, 0);
            for (adr = 0x80; adr < 0x90; adr++)
                qsound_set_command(chip, (byte)adr, 0x120);

            return;
        }

        //WRITE8_DEVICE_HANDLER( qsound_w )
        public void qsound_w(byte ChipID, Int32 offset, byte data)
        {
            //qsound_state *chip = get_safe_token(device);
            qsound_state chip = QSoundData[ChipID];
            switch (offset)
            {
                case 0:
                    chip.data = (UInt16)((chip.data & 0xff) | (data << 8));
                    break;

                case 1:
                    chip.data = (UInt16)((chip.data & 0xff00) | data);
                    break;

                case 2:
                    qsound_set_command(chip, data, chip.data);
                    break;

                default:
                    //logerror("%s: unexpected qsound write to offset %d == %02X\n", device->machine().describe_context(), offset, data);
                    //logerror("QSound: unexpected qsound write to offset %d == %02X\n", offset, data);
                    break;
            }
        }

        //READ8_DEVICE_HANDLER( qsound_r )
        public byte qsound_r(byte ChipID, Int32 offset)
        {
            /* Port ready bit (0x80 if ready) */
            return 0x80;
        }

        public void qsound_set_command(qsound_state chip, byte address, UInt16 data)
        {
            Int32 ch = 0, reg = 0;

            // direct sound reg
            if (address < 0x80)
            {
                ch = address >> 3;
                reg = address & 0x07;
            }
            // >= 0x80 is probably for the dsp?
            else if (address < 0x90)
            {
                ch = address & 0x0F;
                reg = 8;
            }
            else if (address >= 0xba && address < 0xca)
            {
                ch = address - 0xba;
                reg = 9;
            }
            else
            {
                /* Unknown registers */
                ch = 99;
                reg = 99;
            }

            switch (reg)
            {
                case 0:
                    // bank, high bits unknown
                    ch = (ch + 1) & 0x0f;   /* strange ... */
                    chip.channel[ch].bank = (UInt32)((data & 0x7f) << 16);   // Note: The most recent MAME doesn't do "& 0x7F"
                                                                             //# ifdef _DEBUG
                                                                             //if (data && !(data & 0x8000))
                                                                             //fprintf(stderr, "QSound Ch %u: Bank = %04x\n", ch, data);
                                                                             //#endif
                    break;
                case 1:
                    // start/cur address
                    chip.channel[ch].address = data;
                    break;
                case 2:
                    // frequency
                    chip.channel[ch].freq = data;
                    // This was working with the old code, but breaks the songs with the new one.
                    // And I'm pretty sure the hardware won't do this. -Valley Bell
                    /*if (!data)
                    {
                        // key off
                        chip.channel[ch].enabled = 0;
                    }*/
                    break;
                case 3:
                    //# ifdef _DEBUG
                    //                    if (chip.channel[ch].enabled && data != 0x8000)
                    //                        fprintf(stderr, "QSound Ch %u: KeyOn = %04x\n", ch, data);
                    //#endif
                    // key on (does the value matter? it always writes 0x8000)
                    //chip.channel[ch].enabled = 1;
                    chip.channel[ch].enabled = (byte)((data & 0x8000) >> 15);
                    chip.channel[ch].step_ptr = 0;
                    break;
                case 4:
                    // loop address
                    chip.channel[ch].loop = data;
                    break;
                case 5:
                    // end address
                    chip.channel[ch].end = data;
                    break;
                case 6:
                    // master volume
                    //# ifdef _DEBUG
                    //                    if (!chip.channel[ch].enabled && data)
                    //                        fprintf(stderr, "QSound update warning - please report!\n");
                    //#endif
                    chip.channel[ch].vol = data;
                    break;
                case 7:
                    // unused?
                    //# ifdef MAME_DEBUG
                    //                    popmessage("UNUSED QSOUND REG 7=%04x", data);
                    //#endif
                    break;
                case 8:
                    {
                        // panning (left=0x0110, centre=0x0120, right=0x0130)
                        // looks like it doesn't write other values than that
                        int pan = (data & 0x3f) - 0x10;
                        if (pan > 0x20)
                            pan = 0x20;
                        if (pan < 0)
                            pan = 0;

                        chip.channel[ch].rvol = chip.pan_table[pan];
                        chip.channel[ch].lvol = chip.pan_table[0x20 - pan];
                    }
                    break;
                case 9:
                    // unknown
                    /*
                    #ifdef MAME_DEBUG
                                popmessage("QSOUND REG 9=%04x",data);
                    #endif
                    */
                    break;
                default:
                    //logerror("%s: write_data %02x = %04x\n", machine().describe_context(), address, data);
                    break;
            }
            //LOG(("QSOUND WRITE %02x CH%02d-R%02d =%04x\n", address, ch, reg, data));
        }


        //static STREAM_UPDATE( qsound_update )
        public void qsound_update(byte ChipID, Int32[][] outputs, Int32 samples)
        {
            //qsound_state *chip = (qsound_state *)param;
            qsound_state chip = QSoundData[ChipID];
            Int32 i, j;
            UInt32 offset;
            UInt32 advance;
            sbyte sample;

            QSOUND_CHANNEL pC = chip.channel[0];

            for (i = 0; i < samples; i++)
            {
                outputs[0][i] = 0x00;
                outputs[1][i] = 0x00;
            }
            //memset(outputs[0], 0x00, samples * sizeof(* outputs[0]) );
            //memset(outputs[1], 0x00, samples * sizeof(* outputs[1]) );
            if (chip.sample_rom_length == 0)
                return;

            for (i = 0; i < QSOUND_CHANNELS; i++)//, pC++)
            {
                pC = chip.channel[i];
                if (pC.enabled != 0 && pC.Muted == 0)
                {
                    Int32[] pOutL = outputs[0];
                    Int32[] pOutR = outputs[1];

                    Int32 ptr = 0;
                    for (j = samples - 1; j >= 0; j--)
                    {
                        advance = (pC.step_ptr >> 12);
                        pC.step_ptr &= 0xfff;
                        pC.step_ptr += pC.freq;

                        if (advance != 0)
                        {
                            pC.address += advance;
                            if (pC.freq != 0 && pC.address >= pC.end)
                            {
                                if (pC.loop != 0)
                                {
                                    // Reached the end, restart the loop
                                    pC.address -= pC.loop;

                                    // Make sure we don't overflow (what does the real chip do in this case?)
                                    if (pC.address >= pC.end)
                                        pC.address = (UInt32)(pC.end - pC.loop);

                                    pC.address &= 0xffff;
                                }
                                else
                                {
                                    // Reached the end of a non-looped sample
                                    //pC->enabled = 0;
                                    pC.address--;   // ensure that old ripped VGMs still work
                                    pC.step_ptr += 0x1000;
                                    break;
                                }
                            }
                        }

                        offset = (pC.bank | pC.address) % chip.sample_rom_length;
                        sample = chip.sample_rom[offset];

                        pOutL[ptr] += ((sample * pC.lvol * pC.vol) >> 14);
                        pOutR[ptr] += ((sample * pC.rvol * pC.vol) >> 14);
                        ptr++;
                    }
                }
            }

            /*if (chip.fpRawDataL)
                fwrite(outputs[0], samples*sizeof(Int32), 1, chip.fpRawDataL);
            if (chip.fpRawDataR)
                fwrite(outputs[1], samples*sizeof(Int32), 1, chip.fpRawDataR);*/
        }

        public void qsound_write_rom(byte ChipID, Int32 ROMSize, Int32 DataStart, Int32 DataLength, byte[] ROMData)
        {
            qsound_state info = QSoundData[ChipID];

            if (info.sample_rom_length != ROMSize)
            {
                info.sample_rom = new sbyte[ROMSize];// (sbyte*)realloc(info.sample_rom, ROMSize);
                info.sample_rom_length = (UInt32)ROMSize;
                for (Int32 i = 0; i < ROMSize; i++) info.sample_rom[i] = (sbyte)(-1);// 0xff;
                //memset(info.sample_rom, 0xFF, ROMSize);
            }
            if (DataStart > ROMSize)
                return;
            if (DataStart + DataLength > ROMSize)
                DataLength = ROMSize - DataStart;

            for (Int32 i = 0; i < DataLength; i++) info.sample_rom[i + DataStart] = (sbyte)ROMData[i];
            //memcpy(info.sample_rom + DataStart, ROMData, DataLength);

            return;
        }

        public void qsound_write_rom(byte ChipID, Int32 ROMSize, Int32 DataStart, Int32 DataLength, byte[] ROMData, Int32 srcStartAddress)
        {
            qsound_state info = QSoundData[ChipID];

            if (info.sample_rom_length != ROMSize)
            {
                info.sample_rom = new sbyte[ROMSize];// (sbyte*)realloc(info.sample_rom, ROMSize);
                info.sample_rom_length = (UInt32)ROMSize;
                for (Int32 i = 0; i < ROMSize; i++) info.sample_rom[i] = (sbyte)(-1);// 0xff;
                //memset(info.sample_rom, 0xFF, ROMSize);
            }
            if (DataStart > ROMSize)
                return;
            if (DataStart + DataLength > ROMSize)
                DataLength = ROMSize - DataStart;

            for (Int32 i = 0; i < DataLength; i++) info.sample_rom[i + DataStart] = (sbyte)ROMData[i+srcStartAddress];
            //memcpy(info.sample_rom + DataStart, ROMData, DataLength);

            return;
        }


        public void qsound_set_mute_mask(byte ChipID, UInt32 MuteMask)
        {
            qsound_state info = QSoundData[ChipID];
            byte CurChn;

            for (CurChn = 0; CurChn < QSOUND_CHANNELS; CurChn++)
                info.channel[CurChn].Muted = (byte)((MuteMask >> CurChn) & 0x01);

            return;
        }

        public override int Write(byte ChipID, int port, int adr, int data)
        {
            qsound_w(ChipID, adr, (byte)data);
            return 0;
        }



        /**************************************************************************
         * Generic get_info
         **************************************************************************/

        /*DEVICE_GET_INFO( qsound )
        {
            switch (state)
            {
                // --- the following bits of info are returned as 64-bit signed integers --- //
                case DEVINFO_INT_TOKEN_BYTES:					info.i = sizeof(qsound_state);			break;

                // --- the following bits of info are returned as pointers to data or functions --- //
                case DEVINFO_FCT_START:							info.start = DEVICE_START_NAME( qsound );			break;
                case DEVINFO_FCT_STOP:							info.stop = DEVICE_STOP_NAME( qsound );			break;
                case DEVINFO_FCT_RESET:							// Nothing //									break;

                // --- the following bits of info are returned as NULL-terminated strings --- //
                case DEVINFO_STR_NAME:							strcpy(info.s, "Q-Sound");						break;
                case DEVINFO_STR_FAMILY:					strcpy(info.s, "Capcom custom");				break;
                case DEVINFO_STR_VERSION:					strcpy(info.s, "1.0");							break;
                case DEVINFO_STR_SOURCE_FILE:						strcpy(info.s, __FILE__);						break;
                case DEVINFO_STR_CREDITS:					strcpy(info.s, "Copyright Nicola Salmoria and the MAME Team"); break;
            }
        }*/

        /**************** end of file ****************/

        //DEFINE_LEGACY_SOUND_DEVICE(QSOUND, qsound);

    }
}
