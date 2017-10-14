using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDSound
{
    public class multipcm : Instrument
    {
        public override void Reset(byte ChipID)
        {
            device_reset_multipcm(ChipID);
        }

        public override uint Start(byte ChipID, uint clock)
        {
            return (uint)device_start_multipcm(ChipID, (int)clock);
        }

        public uint Start(byte ChipID, uint samplingrate, uint clock, params object[] option)
        {
            return (uint)device_start_multipcm(ChipID, (int)clock);
        }

        public override void Stop(byte ChipID)
        {
            device_stop_multipcm(ChipID);
        }

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {
            MultiPCM_update(ChipID, outputs, samples);
        }

        //#pragma once

        //#include "devlegcy.h"

        //WRITE8_DEVICE_HANDLER( multipcm_w );
        //READ8_DEVICE_HANDLER( multipcm_r );

        //void multipcm_set_bank(running_device *device, UINT32 leftoffs, UINT32 rightoffs);

        //DECLARE_LEGACY_SOUND_DEVICE(MULTIPCM, multipcm);




        /*
 * Sega System 32 Multi/Model 1/Model 2 custom PCM chip (315-5560) emulation.
 *
 * by Miguel Angel Horna (ElSemi) for Model 2 Emulator and MAME.
 * Information by R.Belmont and the YMF278B (OPL4) manual.
 *
 * voice registers:
 * 0: Pan
 * 1: Index of sample
 * 2: LSB of pitch (low 2 bits seem unused so)
 * 3: MSB of pitch (ooooppppppppppxx) (o=octave (4 bit signed), p=pitch (10 bits), x=unused?
 * 4: voice control: top bit = 1 for key on, 0 for key off
 * 5: bit 0: 0: interpolate volume changes, 1: direct set volume,
      bits 1-7 = volume attenuate (0=max, 7f=min)
 * 6: LFO frequency + Phase LFO depth
 * 7: Amplitude LFO size
 *
 * The first sample ROM contains a variable length table with 12
 * bytes per instrument/sample. This is very similar to the YMF278B.
 *
 * The first 3 bytes are the offset into the file (big endian).
 * The next 2 are the loop start offset into the file (big endian)
 * The next 2 are the 2's complement of the total sample size (big endian)
 * The next byte is LFO freq + depth (copied to reg 6 ?)
 * The next 3 are envelope params (Attack, Decay1 and 2, sustain level, release, Key Rate Scaling)
 * The next byte is Amplitude LFO size (copied to reg 7 ?)
 *
 * TODO
 * - The YM278B manual states that the chip supports 512 instruments. The MultiPCM probably supports them
 * too but the high bit position is unknown (probably reg 2 low bit). Any game use more than 256?
 *
 */

        //#include "emu.h"
        //#include "streams.h"
        //# include "mamedef.h"
        //# include <math.h>
        //# include <stdlib.h>
        //# include <string.h>	// for memset
        //# include <stddef.h>	// for NULL
        //# include "multipcm.h"

        //????
        private const double MULTIPCM_CLOCKDIV = (180.0);

        private class _Sample
        {
            public UInt32 Start;
            public UInt32 Loop;
            public UInt32 End;
            public byte AR, DR1, DR2, DL, RR;
            public byte KRS;
            public byte LFOVIB;
            public byte AM;
        };

        private enum _STATE { ATTACK, DECAY1, DECAY2, RELEASE }

        private class _EG
        {
            public Int32 volume; //
            public _STATE state;
            public Int32 step=0;
            //step vals
            public Int32 AR;     //Attack
            public Int32 D1R;    //Decay1
            public Int32 D2R;    //Decay2
            public Int32 RR;     //Release
            public Int32 DL;     //Decay level
        };

        private class _LFO
        {
            public UInt16 phase;
            public UInt32 phase_step;
            public Int32[] table;
            public Int32[] scale;
        };


        private class _SLOT
        {
            public byte Num;
            public byte[] Regs = new byte[8];
            public Int32 Playing;
            public _Sample Sample;
            public UInt32 Base;
            public UInt32 offset;
            public UInt32 step;
            public UInt32 Pan, TL;
            public UInt32 DstTL;
            public Int32 TLStep;
            public Int32 Prev;
            public _EG EG=null;
            public _LFO PLFO = null;   //Phase lfo
            public _LFO ALFO = null;   //AM lfo

            public byte Muted;
        };

        //private _MultiPCM MultiPCM;
        private class _MultiPCM
        {
            //sound_stream * stream;
            public _Sample[] Samples = new _Sample[0x200];        //Max 512 samples
            public _SLOT[] Slots = new _SLOT[28];
            public UInt32 CurSlot;
            public UInt32 Address;
            public UInt32 BankR, BankL;
            public float Rate;
            public UInt32 ROMMask;
            public UInt32 ROMSize;
            public sbyte[] ROM;
            //I include these in the chip because they depend on the chip clock
            public UInt32[] ARStep = new UInt32[0x40], DRStep = new UInt32[0x40];    //Envelope step table
            public UInt32[] FNS_Table = new UInt32[0x400];      //Frequency step table
        };


        private byte IsInit = 0x00;
        private Int32[] LPANTABLE = new Int32[0x800], RPANTABLE = new Int32[0x800];

        private UInt32 FIX(float v) { return ((UInt32)((float)(1 << SHIFT) * (v))); }

        private Int32[] val2chan = new Int32[]{
    0, 1, 2, 3, 4, 5, 6 , -1,
    7, 8, 9, 10,11,12,13, -1,
    14,15,16,17,18,19,20, -1,
    21,22,23,24,25,26,27, -1,
        };


        private Int32 SHIFT = 12;


        //private double MULTIPCM_RATE = 44100.0;


        private Int32 MAX_CHIPS = 0x02;
        private _MultiPCM[] MultiPCMData = new _MultiPCM[2] { new _MultiPCM(), new _MultiPCM() };// MAX_CHIPS];

        /*INLINE MultiPCM *get_safe_token(running_device *device)
        {
            assert(device != NULL);
            assert(device->type() == MULTIPCM);
            return (MultiPCM *)downcast<legacy_device_base *>(device)->token();
        }*/


        /*******************************
                ENVELOPE SECTION
        *******************************/

        //Times are based on a 44100Hz timebase. It's adjusted to the actual sampling rate on startup

        private double[] BaseTimes = new double[64] {
            0,0,0,0,6222.95,4978.37,4148.66,3556.01,3111.47,2489.21,2074.33,1778.00,1555.74,1244.63,1037.19,889.02,
            777.87,622.31,518.59,444.54,388.93,311.16,259.32,222.27,194.47,155.60,129.66,111.16,97.23,77.82,64.85,55.60,
            48.62,38.91,32.43,27.80,24.31,19.46,16.24,13.92,12.15,9.75,8.12,6.98,6.08,4.90,4.08,3.49,
            3.04,2.49,2.13,1.90,1.72,1.41,1.18,1.04,0.91,0.73,0.59,0.50,0.45,0.45,0.45,0.45
        };

        private double AR2DR = 14.32833;
        private Int32[] lin2expvol = new Int32[0x400];
        private Int32[] TLSteps = new Int32[2];

        private const Int32 EG_SHIFT = 16;

        private Int32 EG_Update(_SLOT slot)
        {
            switch (slot.EG.state)
            {
                case _STATE.ATTACK:
                    slot.EG.volume += slot.EG.AR;
                    if (slot.EG.volume >= (0x3ff << EG_SHIFT))
                    {
                        slot.EG.state = _STATE.DECAY1;
                        if (slot.EG.D1R >= (0x400 << EG_SHIFT)) //Skip DECAY1, go directly to DECAY2
                            slot.EG.state = _STATE.DECAY2;
                        slot.EG.volume = 0x3ff << EG_SHIFT;
                    }
                    break;
                case _STATE.DECAY1:
                    slot.EG.volume -= slot.EG.D1R;
                    if (slot.EG.volume <= 0)
                        slot.EG.volume = 0;
                    if (slot.EG.volume >> EG_SHIFT <= (slot.EG.DL << (10 - 4)))
                        slot.EG.state = _STATE.DECAY2;
                    break;
                case _STATE.DECAY2:
                    slot.EG.volume -= slot.EG.D2R;
                    if (slot.EG.volume <= 0)
                        slot.EG.volume = 0;
                    break;
                case _STATE.RELEASE:
                    slot.EG.volume -= slot.EG.RR;
                    if (slot.EG.volume <= 0)
                    {
                        slot.EG.volume = 0;
                        slot.Playing = 0;
                    }
                    break;
                default:
                    return 1 << SHIFT;
            }
            return lin2expvol[slot.EG.volume >> EG_SHIFT];
        }

        private UInt32 Get_RATE(UInt32[] Steps, UInt32 rate, UInt32 val)
        {
            Int32 r = (Int32)(4 * val + rate);
            if (val == 0)
                return Steps[0];
            if (val == 0xf)
                return Steps[0x3f];
            if (r > 0x3f)
                r = 0x3f;
            return Steps[r];
        }

        private void EG_Calc(_MultiPCM ptChip, _SLOT slot)
        {
            Int32 octave = ((slot.Regs[3] >> 4) - 1) & 0xf;
            Int32 rate;
            if ((octave & 8) != 0) octave = octave - 16;
            if (slot.Sample.KRS != 0xf)
                rate = (octave + slot.Sample.KRS) * 2 + ((slot.Regs[3] >> 3) & 1);
            else
                rate = 0;

            slot.EG.AR = (Int32)Get_RATE(ptChip.ARStep, (UInt32)rate, slot.Sample.AR);
            slot.EG.D1R = (Int32)Get_RATE(ptChip.DRStep, (UInt32)rate, slot.Sample.DR1);
            slot.EG.D2R = (Int32)Get_RATE(ptChip.DRStep, (UInt32)rate, slot.Sample.DR2);
            slot.EG.RR = (Int32)Get_RATE(ptChip.DRStep, (UInt32)rate, slot.Sample.RR);
            slot.EG.DL = 0xf - slot.Sample.DL;

        }

        /*****************************
                LFO  SECTION
        *****************************/

        private const Int32 LFO_SHIFT = 8;


        private UInt32 LFIX(float v) { return ((UInt32)((float)(1 << LFO_SHIFT) * (v))); }

        //Convert DB to multiply amplitude
        private UInt32 DB(float v) { return LFIX((float)Math.Pow(10.0, (v / 20.0))); }

        //Convert cents to step increment
        private UInt32 CENTS(float v) { return LFIX((float)Math.Pow(2.0, v / 1200.0)); }

        private Int32[] PLFO_TRI = new Int32[256];
        private Int32[] ALFO_TRI = new Int32[256];

        private float[] LFOFreq = new float[8] { 0.168f, 2.019f, 3.196f, 4.206f, 5.215f, 5.888f, 6.224f, 7.066f };  //Hz;
        private float[] PSCALE = new float[8] { 0.0f, 3.378f, 5.065f, 6.750f, 10.114f, 20.170f, 40.180f, 79.307f }; //cents
        private float[] ASCALE = new float[8] { 0.0f, 0.4f, 0.8f, 1.5f, 3.0f, 6.0f, 12.0f, 24.0f };                 //DB
        private Int32[][] PSCALES = new Int32[8][] { new Int32[256], new Int32[256], new Int32[256], new Int32[256], new Int32[256], new Int32[256], new Int32[256], new Int32[256] };
        private Int32[][] ASCALES = new Int32[8][] { new Int32[256], new Int32[256], new Int32[256], new Int32[256], new Int32[256], new Int32[256], new Int32[256], new Int32[256] };

        private void LFO_Init()
        {
            Int32 i, s;
            for (i = 0; i < 256; ++i)
            {
                int a;  //amplitude
                int p;  //phase

                //Tri
                if (i < 128)
                    a = 255 - (i * 2);
                else
                    a = (i * 2) - 256;
                if (i < 64)
                    p = i * 2;
                else if (i < 128)
                    p = 255 - i * 2;
                else if (i < 192)
                    p = 256 - i * 2;
                else
                    p = i * 2 - 511;
                ALFO_TRI[i] = a;
                PLFO_TRI[i] = p;
            }

            for (s = 0; s < 8; ++s)
            {
                float limit = PSCALE[s];
                for (i = -128; i < 128; ++i)
                {
                    PSCALES[s][i + 128] = (Int32)CENTS((float)((limit * (float)i) / 128.0F));
                }
                limit = -ASCALE[s];
                for (i = 0; i < 256; ++i)
                {
                    ASCALES[s][i] = (Int32)DB((float)((limit * (float)i) / 256.0F));
                }
            }
        }

        private Int32 PLFO_Step(_LFO LFO)
        {
            int p;
            LFO.phase += (UInt16)LFO.phase_step;
            p = LFO.table[(LFO.phase >> LFO_SHIFT) & 0xff];
            p = LFO.scale[p + 128];
            return p << (SHIFT - LFO_SHIFT);
        }

        private Int32 ALFO_Step(_LFO LFO)
        {
            Int32 p;
            LFO.phase += (UInt16)LFO.phase_step;
            p = LFO.table[(LFO.phase >> LFO_SHIFT) & 0xff];
            p = LFO.scale[p];
            return p << (SHIFT - LFO_SHIFT);
        }

        private void LFO_ComputeStep(_MultiPCM ptChip, _LFO LFO, UInt32 LFOF, UInt32 LFOS, Int32 ALFO)
        {
            float step = (float)(LFOFreq[LFOF] * 256.0 / (float)ptChip.Rate);
            LFO.phase_step = (UInt32)((float)(1 << LFO_SHIFT) * step);
            if (ALFO != 0)
            {
                LFO.table = ALFO_TRI;
                LFO.scale = ASCALES[LFOS];
            }
            else
            {
                LFO.table = PLFO_TRI;
                LFO.scale = PSCALES[LFOS];
            }
        }



        private void WriteSlot(_MultiPCM ptChip, _SLOT slot, Int32 reg, byte data)
        {
            slot.Regs[reg] = data;

            switch (reg)
            {
                case 0: //PANPOT
                    slot.Pan = (UInt32)((data >> 4) & 0xf);
                    break;
                case 1: //Sample
                        //according to YMF278 sample write causes some base params written to the regs (envelope+lfos)
                        //the game should never change the sample while playing.
                    {
                        _Sample Sample = ptChip.Samples[slot.Regs[1]];
                        WriteSlot(ptChip, slot, 6, Sample.LFOVIB);
                        WriteSlot(ptChip, slot, 7, Sample.AM);
                    }
                    break;
                case 2: //Pitch
                case 3:
                    {
                        UInt32 oct = (UInt32)(((slot.Regs[3] >> 4) - 1) & 0xf);
                        UInt32 pitch = (UInt32)(((slot.Regs[3] & 0xf) << 6) | (slot.Regs[2] >> 2));
                        pitch = ptChip.FNS_Table[pitch];
                        if ((oct & 0x8) != 0)
                            pitch >>= (Int32)(16 - oct);
                        else
                            pitch <<= (Int32)oct;
                        slot.step = (UInt32)(pitch / ptChip.Rate);
                    }
                    break;
                case 4:     //KeyOn/Off (and more?)
                    {
                        if ((data & 0x80) != 0) //KeyOn
                        {
                            slot.Sample = ptChip.Samples[slot.Regs[1]];
                            slot.Playing = 1;
                            slot.Base = slot.Sample.Start;
                            slot.offset = 0;
                            slot.Prev = 0;
                            slot.TL = slot.DstTL << SHIFT;

                            EG_Calc(ptChip, slot);
                            slot.EG.state = _STATE.ATTACK;
                            slot.EG.volume = 0;

                            if (slot.Base >= 0x100000)
                            {
                                if ((slot.Pan & 8) != 0)
                                    slot.Base = (slot.Base & 0xfffff) | (ptChip.BankL);
                                else
                                    slot.Base = (slot.Base & 0xfffff) | (ptChip.BankR);
                            }

                        }
                        else
                        {
                            if (slot.Playing != 0)
                            {
                                if (slot.Sample.RR != 0xf)
                                    slot.EG.state = _STATE.RELEASE;
                                else
                                    slot.Playing = 0;
                            }
                        }
                    }
                    break;
                case 5: //TL+Interpolation
                    {
                        slot.DstTL = (UInt32)((data >> 1) & 0x7f);
                        if ((data & 1) == 0)    //Interpolate TL
                        {
                            if ((slot.TL >> SHIFT) > slot.DstTL)
                                slot.TLStep = TLSteps[0];       //decrease
                            else
                                slot.TLStep = TLSteps[1];       //increase
                        }
                        else
                            slot.TL = slot.DstTL << SHIFT;
                    }
                    break;
                case 6: //LFO freq+PLFO
                    {
                        if (data != 0)
                        {
                            LFO_ComputeStep(ptChip, slot.PLFO, (UInt32)(slot.Regs[6] >> 3) & 7, (UInt32)(slot.Regs[6] & 7), 0);
                            LFO_ComputeStep(ptChip, slot.ALFO, (UInt32)(slot.Regs[6] >> 3) & 7, (UInt32)(slot.Regs[7] & 7), 1);
                        }
                    }
                    break;
                case 7: //ALFO
                    {
                        if (data != 0)
                        {
                            LFO_ComputeStep(ptChip, slot.PLFO, (UInt32)(slot.Regs[6] >> 3) & 7, (UInt32)(slot.Regs[6] & 7), 0);
                            LFO_ComputeStep(ptChip, slot.ALFO, (UInt32)(slot.Regs[6] >> 3) & 7, (UInt32)(slot.Regs[7] & 7), 1);
                        }
                    }
                    break;
            }
        }

        //static STREAM_UPDATE( MultiPCM_update )
        public void MultiPCM_update(byte ChipID, Int32[][] outputs, Int32 samples)
        {
            //MultiPCM *ptChip = (MultiPCM *)param;
            _MultiPCM ptChip = MultiPCMData[ChipID];
            Int32[][] datap = new Int32[2][];
            Int32 i, sl;

            datap[0] = outputs[0];
            datap[1] = outputs[1];

            for (int j = 0; j < samples; j++)
            {
                datap[0][j] = 0;
                datap[1][j] = 0;
            }
            //memset(datap[0], 0, sizeof(*datap[0]) * samples);
            //memset(datap[1], 0, sizeof(*datap[1]) * samples);


            for (i = 0; i < samples; ++i)
            {
                Int32 smpl = 0;
                Int32 smpr = 0;
                for (sl = 0; sl < 28; ++sl)
                {
                    _SLOT slot = ptChip.Slots[sl];
                    if (slot.Playing != 0 && slot.Muted == 0)
                    {
                        UInt32 vol = (slot.TL >> SHIFT) | (slot.Pan << 7);
                        UInt32 adr = slot.offset >> SHIFT;
                        Int32 sample;
                        UInt32 step = slot.step;
                        Int32 csample = (Int16)(ptChip.ROM[(slot.Base + adr) & ptChip.ROMMask] << 8);
                        Int32 fpart = (Int32)(slot.offset & ((1 << SHIFT) - 1));
                        sample = (csample * fpart + slot.Prev * ((1 << SHIFT) - fpart)) >> SHIFT;

                        if ((slot.Regs[6] & 7) != 0)    //Vibrato enabled
                        {
                            step = (UInt32)(step * PLFO_Step(slot.PLFO));
                            step >>= SHIFT;
                        }

                        slot.offset += step;
                        if (slot.offset >= (slot.Sample.End << SHIFT))
                        {
                            slot.offset = slot.Sample.Loop << SHIFT;
                        }
                        if ((adr ^ (slot.offset >> SHIFT)) != 0)
                        {
                            slot.Prev = csample;
                        }

                        if ((slot.TL >> SHIFT) != slot.DstTL)
                            slot.TL += (UInt32)slot.TLStep;

                        if ((slot.Regs[7] & 7) != 0)    //Tremolo enabled
                        {
                            sample = sample * ALFO_Step(slot.ALFO);
                            sample >>= SHIFT;
                        }

                        sample = (sample * EG_Update(slot)) >> 10;

                        smpl += (LPANTABLE[vol] * sample) >> SHIFT;
                        smpr += (RPANTABLE[vol] * sample) >> SHIFT;
                    }
                }
                /*#define ICLIP16(x) (x<-32768)?-32768:((x>32767)?32767:x)
                        datap[0][i]=ICLIP16(smpl);
                        datap[1][i]=ICLIP16(smpr);*/
                datap[0][i] = smpl;
                datap[1][i] = smpr;
            }
        }

        //READ8_DEVICE_HANDLER( multipcm_r )
        public byte multipcm_r(byte ChipID, Int32 offset)
        {
            //  MultiPCM *ptChip = get_safe_token(device);
            //	MultiPCM *ptChip = &MultiPCMData[ChipID];
            return 0;
        }

        //static DEVICE_START( multipcm )
        public Int32 device_start_multipcm(byte ChipID, int clock)
        {
            //MultiPCM *ptChip = get_safe_token(device);
            _MultiPCM ptChip;
            int i;

            if (ChipID >= MAX_CHIPS)
                return 0;

            ptChip = MultiPCMData[ChipID];

            for (i = 0; i < ptChip.Slots.Length; i++)
            {
                ptChip.Slots[i] = new _SLOT();
                ptChip.Slots[i].EG = new _EG();
                ptChip.Slots[i].ALFO = new _LFO();
                ptChip.Slots[i].PLFO = new _LFO();
            }
            for (i = 0; i < ptChip.Samples.Length; i++)
            {
                ptChip.Samples[i] = new _Sample();
            }
            //ptChip->ROM=*device->region();
            ptChip.ROMMask = 0x00;
            ptChip.ROMSize = 0x00;
            ptChip.ROM = null;
            //ptChip->Rate=(float) device->clock() / MULTIPCM_CLOCKDIV;
            ptChip.Rate = (float)(clock / MULTIPCM_CLOCKDIV);

            //ptChip->stream = stream_create(device, 0, 2, ptChip->Rate, ptChip, MultiPCM_update);

            if (IsInit == 0)
            {
                //Volume+pan table
                for (i = 0; i < 0x800; ++i)
                {
                    float SegaDB = 0;
                    float TL;
                    float LPAN, RPAN;

                    byte iTL = (byte)(i & 0x7f);
                    byte iPAN = (byte)((i >> 7) & 0xf);

                    SegaDB = (float)(iTL * (-24.0) / (float)0x40);

                    TL = (float)Math.Pow(10.0, SegaDB / 20.0);


                    if (iPAN == 0x8)
                    {
                        LPAN = RPAN = 0.0F;
                    }
                    else if (iPAN == 0x0)
                    {
                        LPAN = RPAN = 1.0F;
                    }
                    else if ((iPAN & 0x8) != 0)
                    {
                        LPAN = 1.0F;

                        iPAN = (byte)(0x10 - iPAN);

                        SegaDB = (float)(iPAN * (-12.0) / (float)0x4);

                        RPAN = (float)Math.Pow(10.0, SegaDB / 20.0);

                        if ((iPAN & 0x7) == 7)
                            RPAN = 0.0F;
                    }
                    else
                    {
                        RPAN = 1.0F;

                        SegaDB = (float)(iPAN * (-12.0) / (float)0x4);

                        LPAN = (float)Math.Pow(10.0, SegaDB / 20.0);
                        if ((iPAN & 0x7) == 7)
                            LPAN = 0.0F;
                    }

                    TL /= 4.0F;

                    LPANTABLE[i] = (Int32)FIX(LPAN * TL);
                    RPANTABLE[i] = (Int32)FIX(RPAN * TL);
                }

                IsInit = 0x01;
            }

            //Pitch steps
            for (i = 0; i < 0x400; ++i)
            {
                float fcent = (float)(ptChip.Rate * (1024.0 + (float)i) / 1024.0);
                ptChip.FNS_Table[i] = (UInt32)((float)(1 << SHIFT) * fcent);
            }

            //Envelope steps
            for (i = 0; i < 0x40; ++i)
            {
                //Times are based on 44100 clock, adjust to real chip clock
                ptChip.ARStep[i] = (UInt32)((float)(0x400 << EG_SHIFT) / (BaseTimes[i] * 44100.0 / (1000.0)));
                ptChip.DRStep[i] = (UInt32)((float)(0x400 << EG_SHIFT) / (BaseTimes[i] * AR2DR * 44100.0 / (1000.0)));
            }
            ptChip.ARStep[0] = ptChip.ARStep[1] = ptChip.ARStep[2] = ptChip.ARStep[3] = 0;
            ptChip.ARStep[0x3f] = 0x400 << EG_SHIFT;
            ptChip.DRStep[0] = ptChip.DRStep[1] = ptChip.DRStep[2] = ptChip.DRStep[3] = 0;

            //TL Interpolation steps
            //lower
            TLSteps[0] = -(Int32)((float)(0x80 << SHIFT) / (78.2 * 44100.0 / 1000.0));
            //raise
            TLSteps[1] = (Int32)((float)(0x80 << SHIFT) / (78.2 * 2 * 44100.0 / 1000.0));

            //build the linear->exponential ramps
            for (i = 0; i < 0x400; ++i)
            {
                float db = -(float)((96.0 - (96.0 * (float)i / (float)0x400)));
                lin2expvol[i] = (Int32)(Math.Pow(10.0, db / 20.0) * (float)(1 << SHIFT));
            }


            /*for(i=0;i<512;++i)
            {
                UINT8 *ptSample=(UINT8 *) ptChip->ROM+i*12;
                ptChip->Samples[i].Start=(ptSample[0]<<16)|(ptSample[1]<<8)|(ptSample[2]<<0);
                ptChip->Samples[i].Loop=(ptSample[3]<<8)|(ptSample[4]<<0);
                ptChip->Samples[i].End=0xffff-((ptSample[5]<<8)|(ptSample[6]<<0));
                ptChip->Samples[i].LFOVIB=ptSample[7];
                ptChip->Samples[i].DR1=ptSample[8]&0xf;
                ptChip->Samples[i].AR=(ptSample[8]>>4)&0xf;
                ptChip->Samples[i].DR2=ptSample[9]&0xf;
                ptChip->Samples[i].DL=(ptSample[9]>>4)&0xf;
                ptChip->Samples[i].RR=ptSample[10]&0xf;
                ptChip->Samples[i].KRS=(ptSample[10]>>4)&0xf;
                ptChip->Samples[i].AM=ptSample[11];
            }*/

            /*state_save_register_device_item(device, 0, ptChip->CurSlot);
            state_save_register_device_item(device, 0, ptChip->Address);
            state_save_register_device_item(device, 0, ptChip->BankL);
            state_save_register_device_item(device, 0, ptChip->BankR);*/

            // reset is done via DEVICE_RESET
            /*for(i=0;i<28;++i)
            {
                ptChip->Slots[i].Num=i;
                ptChip->Slots[i].Playing=0;

                state_save_register_device_item(device, i, ptChip->Slots[i].Num);
                state_save_register_device_item_array(device, i, ptChip->Slots[i].Regs);
                state_save_register_device_item(device, i, ptChip->Slots[i].Playing);
                state_save_register_device_item(device, i, ptChip->Slots[i].Base);
                state_save_register_device_item(device, i, ptChip->Slots[i].offset);
                state_save_register_device_item(device, i, ptChip->Slots[i].step);
                state_save_register_device_item(device, i, ptChip->Slots[i].Pan);
                state_save_register_device_item(device, i, ptChip->Slots[i].TL);
                state_save_register_device_item(device, i, ptChip->Slots[i].DstTL);
                state_save_register_device_item(device, i, ptChip->Slots[i].TLStep);
                state_save_register_device_item(device, i, ptChip->Slots[i].Prev);
                state_save_register_device_item(device, i, ptChip->Slots[i].EG.volume);
                state_save_register_device_item(device, i, ptChip->Slots[i].EG.state);
                state_save_register_device_item(device, i, ptChip->Slots[i].EG.step);
                state_save_register_device_item(device, i, ptChip->Slots[i].EG.AR);
                state_save_register_device_item(device, i, ptChip->Slots[i].EG.D1R);
                state_save_register_device_item(device, i, ptChip->Slots[i].EG.D2R);
                state_save_register_device_item(device, i, ptChip->Slots[i].EG.RR);
                state_save_register_device_item(device, i, ptChip->Slots[i].EG.DL);
                state_save_register_device_item(device, i, ptChip->Slots[i].PLFO.phase);
                state_save_register_device_item(device, i, ptChip->Slots[i].PLFO.phase_step);
                state_save_register_device_item(device, i, ptChip->Slots[i].ALFO.phase);
                state_save_register_device_item(device, i, ptChip->Slots[i].ALFO.phase_step);
            }*/

            LFO_Init();

            multipcm_set_bank(ChipID, 0x00, 0x00);

            return (Int32)(ptChip.Rate + 0.5);
        }


        public void device_stop_multipcm(byte ChipID)
        {
            _MultiPCM ptChip = MultiPCMData[ChipID];

            //free(ptChip->ROM); 
            ptChip.ROM = null;

            return;
        }

        public void device_reset_multipcm(byte ChipID)
        {
            _MultiPCM ptChip = MultiPCMData[ChipID];
            int i;

            for (i = 0; i < 28; ++i)
            {
                ptChip.Slots[i].Num = (byte)i;
                ptChip.Slots[i].Playing = 0;
            }

            return;
        }


        //WRITE8_DEVICE_HANDLER( multipcm_w )
        public void multipcm_w(byte ChipID, Int32 offset, byte data)
        {
            //MultiPCM *ptChip = get_safe_token(device);
            _MultiPCM ptChip = MultiPCMData[ChipID];
            switch (offset)
            {
                case 0:     //Data write
                    WriteSlot(ptChip, ptChip.Slots[ptChip.CurSlot], (Int32)ptChip.Address, data);
                    break;
                case 1:
                    ptChip.CurSlot = (UInt32)val2chan[data & 0x1f];
                    //Console.WriteLine("CurSlot{0}", ptChip.CurSlot);
                    break;
                case 2:
                    ptChip.Address = (UInt32)((data > 7) ? 7 : data);
                    break;
            }
            /*ptChip->CurSlot = val2chan[(offset >> 3) & 0x1F];
            ptChip->Address = offset & 0x07;
            WriteSlot(ptChip, ptChip->Slots + ptChip->CurSlot, ptChip->Address, data);*/
        }

        /* MAME/M1 access functions */

        //void multipcm_set_bank(running_device *device, UINT32 leftoffs, UINT32 rightoffs)
        public void multipcm_set_bank(byte ChipID, UInt32 leftoffs, UInt32 rightoffs)
        {
            //MultiPCM *ptChip = get_safe_token(device);
            _MultiPCM ptChip = MultiPCMData[ChipID];
            ptChip.BankL = leftoffs;
            ptChip.BankR = rightoffs;
        }

        public void multipcm_bank_write(byte ChipID, byte offset, UInt16 data)
        {
            _MultiPCM ptChip = MultiPCMData[ChipID];

            if ((offset & 0x01) != 0)
                ptChip.BankL = (UInt32)(data << 16);
            if ((offset & 0x02) != 0)
                ptChip.BankR = (UInt32)(data << 16);

            return;
        }

        public void multipcm_write_rom(byte ChipID, Int32 ROMSize, Int32 DataStart, Int32 DataLength, byte[] ROMData)
        {
            _MultiPCM ptChip = MultiPCMData[ChipID];
            UInt16 CurSmpl;
            _Sample TempSmpl;
            //byte[] ptSample;
            Int32 ptSample;

            if (ptChip.ROMSize != ROMSize)
            {
                ptChip.ROM = new sbyte[ROMSize];// (INT8*)realloc(ptChip.ROM, ROMSize);
                ptChip.ROMSize = (UInt32)ROMSize;

                for (ptChip.ROMMask = 1; ptChip.ROMMask < ROMSize; ptChip.ROMMask <<= 1)
                    ;
                ptChip.ROMMask--;

                for (int i = 0; i < ROMSize; i++) ptChip.ROM[i] = -1;//0xff;
                //memset(ptChip.ROM, 0xFF, ROMSize);
            }
            if (DataStart > ROMSize)
                return;
            if (DataStart + DataLength > ROMSize)
                DataLength = ROMSize - DataStart;

            for (int i = 0; i < DataLength; i++) ptChip.ROM[i + DataStart] = (sbyte)ROMData[i];
            //memcpy(ptChip.ROM + DataStart, ROMData, DataLength);

            if (DataStart < 0x200 * 12)
            {
                for (CurSmpl = 0; CurSmpl < 512; CurSmpl++)
                {
                    TempSmpl = ptChip.Samples[CurSmpl];
                    //ptSample = (byte*)ptChip.ROM + CurSmpl * 12;
                    ptSample = CurSmpl * 12;
                    TempSmpl.Start = (UInt32)((ptChip.ROM[ptSample + 0] << 16) | (ptChip.ROM[ptSample + 1] << 8) | (ptChip.ROM[ptSample + 2] << 0));
                    TempSmpl.Loop = (UInt32)((ptChip.ROM[ptSample + 3] << 8) | (ptChip.ROM[ptSample + 4] << 0));
                    TempSmpl.End = (UInt32)(0xffff - ((ptChip.ROM[ptSample + 5] << 8) | (ptChip.ROM[ptSample + 6] << 0)));
                    TempSmpl.LFOVIB = (byte)ptChip.ROM[ptSample + 7];
                    TempSmpl.DR1 = (byte)(ptChip.ROM[ptSample + 8] & 0xf);
                    TempSmpl.AR = (byte)((ptChip.ROM[ptSample + 8] >> 4) & 0xf);
                    TempSmpl.DR2 = (byte)(ptChip.ROM[ptSample + 9] & 0xf);
                    TempSmpl.DL = (byte)((ptChip.ROM[ptSample + 9] >> 4) & 0xf);
                    TempSmpl.RR = (byte)(ptChip.ROM[ptSample + 10] & 0xf);
                    TempSmpl.KRS = (byte)((ptChip.ROM[ptSample + 10] >> 4) & 0xf);
                    TempSmpl.AM = (byte)(ptChip.ROM[ptSample + 11]);
                }
            }

            return;
        }

        public void multipcm_write_rom2(byte ChipID, Int32 ROMSize, Int32 DataStart, Int32 DataLength, byte[] ROMData, Int32 srcStartAddress)
        {
            _MultiPCM ptChip = MultiPCMData[ChipID];
            UInt16 CurSmpl;
            _Sample TempSmpl;
            //byte[] ptSample;
            Int32 ptSample;

            if (ptChip.ROMSize != ROMSize)
            {
                ptChip.ROM = new sbyte[ROMSize];// (INT8*)realloc(ptChip.ROM, ROMSize);
                ptChip.ROMSize = (UInt32)ROMSize;

                for (ptChip.ROMMask = 1; ptChip.ROMMask < ROMSize; ptChip.ROMMask <<= 1)
                    ;
                ptChip.ROMMask--;

                for (int i = 0; i < ROMSize; i++) ptChip.ROM[i] = -1;//0xff;
                //memset(ptChip.ROM, 0xFF, ROMSize);
            }
            if (DataStart > ROMSize)
                return;
            if (DataStart + DataLength > ROMSize)
                DataLength = ROMSize - DataStart;

            for (int i = 0; i < DataLength; i++) ptChip.ROM[i + DataStart] = (sbyte)ROMData[i+srcStartAddress];
            //memcpy(ptChip.ROM + DataStart, ROMData, DataLength);

            if (DataStart < 0x200 * 12)
            {
                for (CurSmpl = 0; CurSmpl < 512; CurSmpl++)
                {
                    TempSmpl = ptChip.Samples[CurSmpl];
                    //ptSample = (byte*)ptChip.ROM + CurSmpl * 12;
                    ptSample = CurSmpl * 12;
                    TempSmpl.Start = (UInt32)(((byte)ptChip.ROM[ptSample + 0] << 16) | ((byte)ptChip.ROM[ptSample + 1] << 8) | ((byte)ptChip.ROM[ptSample + 2] << 0));
                    TempSmpl.Loop = (UInt32)(((byte)ptChip.ROM[ptSample + 3] << 8) | ((byte)ptChip.ROM[ptSample + 4] << 0));
                    TempSmpl.End = (UInt32)(0xffff - (((byte)ptChip.ROM[ptSample + 5] << 8) | ((byte)ptChip.ROM[ptSample + 6] << 0)));
                    TempSmpl.LFOVIB = (byte)ptChip.ROM[ptSample + 7];
                    TempSmpl.DR1 = (byte)((byte)ptChip.ROM[ptSample + 8] & 0xf);
                    TempSmpl.AR = (byte)(((byte)ptChip.ROM[ptSample + 8] >> 4) & 0xf);
                    TempSmpl.DR2 = (byte)((byte)ptChip.ROM[ptSample + 9] & 0xf);
                    TempSmpl.DL = (byte)(((byte)ptChip.ROM[ptSample + 9] >> 4) & 0xf);
                    TempSmpl.RR = (byte)((byte)ptChip.ROM[ptSample + 10] & 0xf);
                    TempSmpl.KRS = (byte)(((byte)ptChip.ROM[ptSample + 10] >> 4) & 0xf);
                    TempSmpl.AM = (byte)(ptChip.ROM[ptSample + 11]);
                    //Console.WriteLine("LFOVIB{0}  AM{1}", ptChip.ROM[ptSample + 7], ptChip.ROM[ptSample + 11]);
                }
            }

            return;
        }

        public void multipcm_set_mute_mask(byte ChipID, UInt32 MuteMask)
        {
            _MultiPCM ptChip = MultiPCMData[ChipID];
            byte CurChn;

            for (CurChn = 0; CurChn < 28; CurChn++)
                ptChip.Slots[CurChn].Muted = (byte)((MuteMask >> CurChn) & 0x01);

            return;
        }

        //#if 0	// for debugging only
        //UINT8 multipcm_get_channels(UINT8 ChipID, UINT32* ChannelMask)
        //{
        //	MultiPCM* ptChip = &MultiPCMData[ChipID];
        //	UINT8 CurChn;
        //	UINT8 UsedChns;
        //	UINT32 ChnMask;

        //	ChnMask = 0x00000000;
        //	UsedChns = 0x00;
        //	for (CurChn = 0; CurChn < 28; CurChn ++)
        //	{
        //		if (ptChip->Slots[CurChn].Playing)
        //		{
        //			ChnMask |= (1 << CurChn);
        //			UsedChns ++;
        //		}
        //	}
        //	if (ChannelMask != NULL)
        //		*ChannelMask = ChnMask;

        //	return UsedChns;
        //}
        //#endif



        /**************************************************************************
         * Generic get_info
         **************************************************************************/

        /*DEVICE_GET_INFO( multipcm )
        {
            switch (state)
            {
                // --- the following bits of info are returned as 64-bit signed integers ---
                case DEVINFO_INT_TOKEN_BYTES:					info->i = sizeof(MultiPCM);				break;

                // --- the following bits of info are returned as pointers to data or functions ---
                case DEVINFO_FCT_START:							info->start = DEVICE_START_NAME( multipcm );		break;
                case DEVINFO_FCT_STOP:							// Nothing										break;
                case DEVINFO_FCT_RESET:							// Nothing										break;

                // --- the following bits of info are returned as NULL-terminated strings ---
                case DEVINFO_STR_NAME:							strcpy(info->s, "Sega/Yamaha 315-5560");		break;
                case DEVINFO_STR_FAMILY:					strcpy(info->s, "Sega custom");					break;
                case DEVINFO_STR_VERSION:					strcpy(info->s, "2.0");							break;
                case DEVINFO_STR_SOURCE_FILE:						strcpy(info->s, __FILE__);						break;
                case DEVINFO_STR_CREDITS:					strcpy(info->s, "Copyright Nicola Salmoria and the MAME Team"); break;
            }
        }*/


        //DEFINE_LEGACY_SOUND_DEVICE(MULTIPCM, multipcm);

    }
}
