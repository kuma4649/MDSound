using System;

//
// Copyright (C) 2017 Alexey Khokholov (Nuke.YKT)
// 
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
//
//
//  Nuked OPN2(Yamaha YM3438) emulator.
//  Thanks:
//      Silicon Pr0n:
//          Yamaha YM3438 decap and die shot(digshadow).
//      OPLx decapsulated(Matthew Gambrell, Olli Niemitalo):
//          OPL2 ROMs.
//
// version: 1.0.7
//

//Created by Chromaryu.
//Based off YM3438 Shot-Die Reverse Nuked OPN2 Source.
// True = 1, False = 0


using Bit8u = System.Byte;
using Bit8s = System.SByte;
using Bit16u = System.UInt16;
using Bit16s = System.Int16;
using Bit32u = System.UInt32;
using Bit32s = System.Int32;
using Bit64u = System.UInt64;
using Bit64s = System.Int64;

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace MDSound
{
    enum EG_PARAM : int
    {
        eg_num_attack = 0,
        eg_num_decay = 1,
        eg_num_sustain = 2,
        eg_num_release = 3
    }

    public class ym3438 : Instrument
    {
        public override string Name { get { return "YM3438"; } set { } }
        public override string ShortName { get { return "OPN2cmos"; } set { } }

        private Bit32s[] dmyBuffer = new Bit32s[2];
        private Bit32s[] grBuffer = new Bit32s[2];
        private Bit32s[] gsBuffer = new Bit32s[2];
        private ym3438_[] ym3438_ = new ym3438_[2] { new ym3438_(), new ym3438_() };
        private int[] buf = new int[2];

        private void OPN2_DoIO(ym3438_ chip)
        {
            chip.write_a_en = (Bit8u)((chip.write_a & 0x03) == 0x01 ? 1 : 0);
            chip.write_d_en = (Bit8u)((chip.write_d & 0x03) == 0x01 ? 1 : 0);
            //mlog("aen:{0} den:{1}\n", chip.write_a_en, chip.write_d_en);
            chip.write_a <<= 1;
            chip.write_d <<= 1;
            //BUSY Counter
            chip.busy = chip.write_busy;
            chip.write_busy_cnt += chip.write_busy;
            chip.write_busy = (Bit8u)(((chip.write_busy != 0 && ((chip.write_busy_cnt >> 5) == 0)) || chip.write_d_en != 0) ? 1 : 0);
            chip.write_busy_cnt &= 0x1f;
        }

        private void OPN2_DoRegWrite(ym3438_ chip)
        {
            int i;
            Bit32u slot = chip.slot % 12;
            Bit32u address;
            Bit32u channel = chip.channel;
            if (chip.write_fm_data != 0)
            {
                if (ym3438_const.op_offset[slot] == (chip.address & 0x107))
                {
                    if ((chip.address & 0x08) != 0)
                    {
                        slot += 12; // OP2? OP4?
                    }
                    address = (Bit32u)(chip.address & 0xf0);
                    switch (address)
                    {
                        case 0x30: //DT MULTI
                            chip.multi[slot] = (Bit8u)(chip.data & 0x0f);
                            if (chip.multi[slot] == 0)
                            {
                                chip.multi[slot] = 1;
                            }
                            else
                            {
                                chip.multi[slot] <<= 1;
                            }
                            chip.dt[slot] = (Bit8u)((chip.data >> 4) & 0x07);
                            break;
                        case 0x40: //TL
                            chip.tl[slot] = (Bit8u)(chip.data & 0x7f);
                            break;
                        case 0x50: // KS AR
                            chip.ar[slot] = (Bit8u)(chip.data & 0x1f);
                            chip.ks[slot] = (Bit8u)((chip.data >> 6) & 0x03);
                            break;
                        case 0x60: // AM DR
                            chip.dr[slot] = (Bit8u)(chip.data & 0x1f);
                            chip.am[slot] = (Bit8u)((chip.data >> 7) & 0x01);
                            break;
                        case 0x70: //SR
                            chip.sr[slot] = (Bit8u)(chip.data & 0x1f);
                            break;
                        case 0x80: //SL RR
                            chip.rr[slot] = (Bit8u)(chip.data & 0x0f);
                            chip.sl[slot] = (Bit8u)((chip.data >> 4) & 0x0f);
                            chip.sl[slot] |= (Bit8u)((chip.sl[slot] + 1) & 0x10);
                            break;
                        case 0x90:
                            chip.ssg_eg[slot] = (Bit8u)(chip.data & 0x0f);
                            break;
                        default:
                            break;
                    }
                }

                if (ym3438_const.ch_offset[channel] == (chip.address & 0x103))
                {
                    address = (Bit32u)(chip.address & 0xfc);
                    switch (address)
                    {
                        case 0xa0: //Fnum, Block, kcode
                            chip.fnum[channel] = (Bit16u)((chip.data & 0xff) | ((chip.reg_a4 & 0x07) << 8));
                            chip.block[channel] = (Bit8u)((chip.reg_a4 >> 3) & 0x07);
                            chip.kcode[channel] = (Bit8u)((Bit8u)(chip.block[channel] << 2) | ym3438_const.fn_note[chip.fnum[channel] >> 7]);
                            break;
                        case 0xa4: // a4?
                            chip.reg_a4 = (Bit8u)(chip.data & 0xff);
                            break;
                        case 0xa8: // fnum, block, kcode 3ch
                            chip.fnum_3ch[channel] = (Bit16u)((chip.data & 0xff) | ((chip.reg_ac & 0x07) << 8));
                            chip.block_3ch[channel] = (Bit8u)((chip.reg_ac >> 3) & 0x07);
                            chip.kcode_3ch[channel] = (Bit8u)((Bit8u)(chip.block_3ch[channel] << 2) | ym3438_const.fn_note[chip.fnum_3ch[channel] >> 7]);
                            break;
                        case 0xac: //ac?
                            chip.reg_ac = (Bit8u)(chip.data & 0xff);
                            break;
                        case 0xb0: // Connect FeedBack
                            chip.connect[channel] = (Bit8u)(chip.data & 0x07);
                            chip.fb[channel] = (Bit8u)((chip.data >> 3) & 0x07);
                            break;
                        case 0xb4: //Modulate Pan
                            chip.pms[channel] = (Bit8u)(chip.data & 0x07);
                            chip.ams[channel] = (Bit8u)((chip.data >> 4) & 0x03);
                            chip.pan_l[channel] = (Bit8u)((chip.data >> 7) & 0x01);
                            chip.pan_r[channel] = (Bit8u)((chip.data >> 6) & 0x01);
                            break;
                        default:
                            break;
                    }
                }
            }
            if (chip.write_a_en != 0 || chip.write_d_en != 0)
            {
                if (chip.write_a_en != 0)
                { // True?
                    chip.write_fm_data = 0;
                }
                if (chip.write_fm_address != 0 && chip.write_d_en != 0)
                {
                    chip.write_fm_data = 1;
                }

                if (chip.write_a_en != 0)
                {
                    if ((chip.write_data & 0xf0) != 0x00)
                    {
                        chip.address = chip.write_data;
                        chip.write_fm_address = 1;
                    }
                    else
                    {
                        chip.write_fm_address = 0;
                    }
                }
                //mlog("d_en:{0} wdata:{1} adr:{2}\n", chip.write_d_en, chip.write_data, chip.address);
                if (chip.write_d_en != 0 && (chip.write_data & 0x100) == 0)
                {
                    switch (chip.address)
                    {
                        case 0x21: /* LSI test 1 */
                            for (i = 0; i < 8; i++)
                            {
                                chip.mode_test_21[i] = (Bit8u)((chip.write_data >> i) & 0x01);
                            }
                            break;
                        case 0x22: /* LFO control */
                            if (((chip.write_data >> 3) & 0x01) != 0)
                            {
                                chip.lfo_en = 0x7f;
                            }
                            else
                            {
                                chip.lfo_en = 0;
                            }
                            chip.lfo_freq = (Bit8u)(chip.write_data & 0x07);
                            break;
                        case 0x24: /* Timer A */
                            chip.timer_a_reg &= 0x03;
                            chip.timer_a_reg |= (Bit8u)((chip.write_data & 0xff) << 2);
                            break;
                        case 0x25:
                            chip.timer_a_reg &= 0x3fc;
                            chip.timer_a_reg |= (Bit8u)(chip.write_data & 0x03);
                            break;
                        case 0x26: /* Timer B */
                            chip.timer_b_reg = (Bit8u)(chip.write_data & 0xff);
                            break;
                        case 0x27: /* CSM, Timer control */
                            chip.mode_ch3 = (Bit8u)((chip.write_data & 0xc0) >> 6);
                            chip.mode_csm = (Bit8u)(chip.mode_ch3 == 2 ? 1 : 0);
                            chip.timer_a_load = (Bit8u)(chip.write_data & 0x01);
                            chip.timer_a_enable = (Bit8u)((chip.write_data >> 2) & 0x01);
                            chip.timer_a_reset = (Bit8u)((chip.write_data >> 4) & 0x01);
                            chip.timer_b_load = (Bit8u)((chip.write_data >> 1) & 0x01);
                            chip.timer_b_enable = (Bit8u)((chip.write_data >> 3) & 0x01);
                            chip.timer_b_reset = (Bit8u)((chip.write_data >> 5) & 0x01);
                            break;
                        case 0x28: /* Key on/off */
                            for (i = 0; i < 4; i++)
                            {
                                chip.mode_kon_operator[i] = (Bit8u)((chip.write_data >> (4 + i)) & 0x01);
                            }
                            if ((chip.write_data & 0x03) == 0x03)
                            {
                                /* Invalid address */
                                chip.mode_kon_channel = 0xff;
                            }
                            else
                            {
                                chip.mode_kon_channel = (Bit8u)((chip.write_data & 0x03) + ((chip.write_data >> 2) & 1) * 3);
                            }
                            //mlog("kon_ope:{0}:{1}:{2}:{3} kon_ch:{4}\n"
                                //, chip.mode_kon_operator[0]
                                //, chip.mode_kon_operator[1]
                                //, chip.mode_kon_operator[2]
                                //, chip.mode_kon_operator[3]
                                //, chip.mode_kon_channel
                                //);
                            break;
                        case 0x2a: /* DAC data */
                            chip.dacdata &= 0x01;
                            chip.dacdata |= (Bit16s)((chip.write_data ^ 0x80) << 1);
                            break;
                        case 0x2b: /* DAC enable */
                            chip.dacen = (Bit8u)(chip.write_data >> 7);
                            break;
                        case 0x2c: /* LSI test 2 */
                            for (i = 0; i < 8; i++)
                            {
                                chip.mode_test_2c[i] = (Bit8u)((chip.write_data >> i) & 0x01);
                            }
                            chip.dacdata &= 0x1fe;
                            chip.dacdata |= (Bit16s)(chip.mode_test_2c[3]);
                            chip.eg_custom_timer = (Bit8u)(((chip.mode_test_2c[7] == 0) && (chip.mode_test_2c[6] != 0)) ? 1 : 0); //todo
                            break;
                        default:
                            break;
                    }
                }
                if (chip.write_a_en != 0)
                {
                    chip.write_fm_mode_a = (Bit8u)(chip.write_data & 0xff);
                }
            }

            if (chip.write_fm_data != 0)
            {
                chip.data = (Bit8u)(chip.write_data & 0xff);
            }
        }

        public void OPN2_PhaseCalcIncrement(ym3438_ chip)
        {
            Bit32u fnum = chip.pg_fnum;
            Bit32u fnum_h = fnum >> 4;
            Bit32u fm;
            Bit32u basefreq;
            Bit8u lfo = chip.lfo_pm;
            Bit8u lfo_l = (Bit8u)(lfo & 0x0f);
            Bit8u pms = chip.pms[chip.channel];
            Bit8u dt = chip.dt[chip.slot];
            Bit8u dt_l = (Bit8u)(dt & 0x03);
            Bit8u detune = 0;
            Bit8u block, note;
            Bit8u sum, sum_h, sum_l;
            Bit8u kcode = (Bit8u)(chip.pg_kcode);

            fnum <<= 1;
            if ((lfo_l & 0x08) != 0)
            {
                lfo_l ^= 0x0f;
            }
            fm = (fnum_h >> (int)ym3438_const.pg_lfo_sh1[pms][lfo_l]) + (fnum_h >> (int)ym3438_const.pg_lfo_sh2[pms][lfo_l]);
            if (pms > 5)
            {
                fm <<= pms - 5;
            }
            fm >>= 2;
            if ((lfo & 0x10) != 0)
            {
                fnum -= fm;
            }
            else
            {
                fnum += fm;
            }
            fnum &= 0xfff;

            basefreq = (fnum << chip.pg_block) >> 2;
            //Console.Write("040   basefreq:{0} fnum:{1} chip.pg_block:{2}\n", basefreq, fnum, chip.pg_block);

            /* Apply detune */
            if (dt_l != 0)
            {
                if (kcode > 0x1c)
                {
                    kcode = 0x1c;
                }
                block = (Bit8u)(kcode >> 2);
                note = (Bit8u)(kcode & 0x03);
                sum = (Bit8u)(block + 9 + (((dt_l == 3) ? 1 : 0) | (dt_l & 0x02)));
                sum_h = (Bit8u)(sum >> 1);
                sum_l = (Bit8u)(sum & 0x01);
                detune = (Bit8u)(ym3438_const.pg_detune[(sum_l << 2) | note] >> (9 - sum_h));
            }
            if ((dt & 0x04) != 0)
            {
                basefreq -= detune;
            }
            else
            {
                basefreq += detune;
            }
            basefreq &= 0x1ffff;
            chip.pg_inc[chip.slot] = (basefreq * chip.multi[chip.slot]) >> 1;
            chip.pg_inc[chip.slot] &= 0xfffff;


        }

        public void OPN2_PhaseGenerate(ym3438_ chip)
        {
            Bit32u slot;
            /* Mask increment */
            slot = (chip.slot + 20) % 24;
            if (chip.pg_reset[slot] != 0)
            {
                chip.pg_inc[slot] = 0;
            }
            /* Phase step */
            slot = (chip.slot + 19) % 24;
            chip.pg_phase[slot] += chip.pg_inc[slot];
            chip.pg_phase[slot] &= 0xfffff;
            if (chip.pg_reset[slot] != 0 || chip.mode_test_21[3] != 0)
            {
                chip.pg_phase[slot] = 0;
            }
        }

        public void OPN2_EnvelopeSSGEG(ym3438_ chip)
        {
            Bit32u slot = chip.slot;
            Bit8u direction = 0;
            chip.eg_ssg_pgrst_latch[slot] = 0;
            chip.eg_ssg_repeat_latch[slot] = 0;
            chip.eg_ssg_hold_up_latch[slot] = 0;
            chip.eg_ssg_inv[slot] = 0;
            if ((chip.ssg_eg[slot] & 0x08) != 0)
            {
                direction = chip.eg_ssg_dir[slot];
                if ((chip.eg_level[slot] & 0x200) != 0)
                {
                    /* Reset */
                    if ((chip.ssg_eg[slot] & 0x03) == 0x00)
                    {
                        chip.eg_ssg_pgrst_latch[slot] = 1;
                    }
                    /* Repeat */
                    if ((chip.ssg_eg[slot] & 0x01) == 0x00)
                    {
                        chip.eg_ssg_repeat_latch[slot] = 1;
                    }
                    /* Inverse */
                    if ((chip.ssg_eg[slot] & 0x03) == 0x02)
                    {
                        direction ^= 1;
                    }
                    if ((chip.ssg_eg[slot] & 0x03) == 0x03)
                    {
                        direction = 1;
                    }
                }
                /* Hold up */
                if (chip.eg_kon_latch[slot] != 0
                 && ((chip.ssg_eg[slot] & 0x07) == 0x05 || (chip.ssg_eg[slot] & 0x07) == 0x03))
                {
                    chip.eg_ssg_hold_up_latch[slot] = 1;
                }
                direction &= chip.eg_kon[slot];
                chip.eg_ssg_inv[slot] = (Bit8u)(
                        (
                            chip.eg_ssg_dir[slot]
                            ^ ((chip.ssg_eg[slot] >> 2) & 0x01)
                        )
                        & chip.eg_kon[slot]
                );
            }
            chip.eg_ssg_dir[slot] = direction;
            chip.eg_ssg_enable[slot] = (Bit8u)((chip.ssg_eg[slot] >> 3) & 0x01);
        }

        private void OPN2_EnvelopeADSR(ym3438_ chip)
        {
            Bit32u slot = (chip.slot + 22) % 24;

            Bit8u nkon = chip.eg_kon_latch[slot];
            //mlog("nkon:{0}\n", nkon);
            Bit8u okon = chip.eg_kon[slot];
            Bit8u kon_event;
            Bit8u koff_event;
            Bit8u eg_off;
            Bit16s level;
            Bit16s nextlevel = 0;
            Bit16s ssg_level;
            Bit8u nextstate = chip.eg_state[slot];
            Bit16s inc = 0;
            chip.eg_read[0] = chip.eg_read_inc;
            chip.eg_read_inc = (Bit8u)(chip.eg_inc > 0 ? 1 : 0);

            /* Reset phase generator */
            chip.pg_reset[slot] = (Bit8u)(((nkon != 0 && okon == 0) || chip.eg_ssg_pgrst_latch[slot] != 0) ? 1 : 0);

            /* KeyOn/Off */
            kon_event = (Bit8u)(((nkon != 0 && okon == 0) || (okon != 0 && chip.eg_ssg_repeat_latch[slot] != 0)) ? 1 : 0);
            koff_event = (Bit8u)((okon != 0 && nkon == 0) ? 1 : 0);

            ssg_level = level = (Bit16s)chip.eg_level[slot];

            if (chip.eg_ssg_inv[slot] != 0)
            {
                /* Inverse */
                ssg_level = (Bit16s)(512 - level);
                ssg_level &= 0x3ff;
            }
            if (koff_event != 0)
            {
                level = ssg_level;
            }
            if (chip.eg_ssg_enable[slot] != 0)
            {
                eg_off = (Bit8u)(level >> 9);
            }
            else
            {
                eg_off = (Bit8u)((level & 0x3f0) == 0x3f0 ? 1 : 0);
            }
            nextlevel = level;
            //mlog("nextlevel:{0} chip.eg_state[slot]:{1} slot:{2}\n", nextlevel, chip.eg_state[slot],slot);
            if (kon_event != 0)
            {
                nextstate = (Bit8u)EG_PARAM.eg_num_attack;
                /* Instant attack */
                if (chip.eg_ratemax != 0)
                {
                    nextlevel = 0;
                }
                else if (chip.eg_state[slot] == (Bit8u)EG_PARAM.eg_num_attack && level != 0 && chip.eg_inc != 0 && nkon != 0)
                {
                    inc = (Bit16s)((~level << chip.eg_inc) >> 5);
                }
                //mlog("inc:{0}\n", inc);
            }
            else
            {
                switch (chip.eg_state[slot])
                {
                    case (Bit8u)EG_PARAM.eg_num_attack:
                        if (level == 0)
                        {
                            nextstate = (Bit8u)EG_PARAM.eg_num_decay;
                        }
                        else if (chip.eg_inc != 0 && chip.eg_ratemax == 0 && nkon != 0)
                        {
                            inc = (Bit16s)((~level << chip.eg_inc) >> 5);
                        }
                        //mlog("ainc:{0}\n", inc);
                        break;
                    case (Bit8u)EG_PARAM.eg_num_decay:
                        if ((level >> 5) == chip.eg_sl[1])
                        {
                            nextstate = (Bit8u)EG_PARAM.eg_num_sustain;
                        }
                        else if (eg_off == 0 && chip.eg_inc != 0)
                        {
                            inc = (Bit16s)(1 << (chip.eg_inc - 1));
                            if (chip.eg_ssg_enable[slot] != 0)
                            {
                                inc <<= 2;
                            }
                        }
                        //mlog("dinc:{0}\n", inc);
                        break;
                    case (Bit8u)EG_PARAM.eg_num_sustain:
                    case (Bit8u)EG_PARAM.eg_num_release:
                        if (eg_off == 0 && chip.eg_inc != 0)
                        {
                            inc = (Bit16s)(1 << (chip.eg_inc - 1));
                            if (chip.eg_ssg_enable[slot] != 0)
                            {
                                inc <<= 2;
                            }
                        }
                        //mlog("srinc:{0}\n", inc);
                        break;
                    default:
                        break;
                }
                if (nkon == 0)
                {
                    nextstate = (Bit8u)EG_PARAM.eg_num_release;
                    //mlog("1rel\n", inc);
                }
            }
            if (chip.eg_kon_csm[slot] != 0)
            {
                nextlevel |= (Bit16s)(chip.eg_tl[1] << 3);
            }

            /* Envelope off */
            if (kon_event == 0 && chip.eg_ssg_hold_up_latch[slot] == 0 && chip.eg_state[slot] != (Bit8u)EG_PARAM.eg_num_attack && eg_off != 0)
            {
                nextstate = (Bit8u)EG_PARAM.eg_num_release;
                nextlevel = 0x3ff;
                //mlog("2rel\n", inc);
            }

            nextlevel += inc;
            //mlog("nextlevel:{0}\n", nextlevel);

            chip.eg_kon[slot] = chip.eg_kon_latch[slot];
            chip.eg_level[slot] = (Bit16u)((Bit16u)nextlevel & 0x3ff);
            chip.eg_state[slot] = nextstate;
            //mlog("chip.eg_level[slot]:{0} slot:{1}\n", chip.eg_level[slot], slot);
        }

        private void OPN2_EnvelopePrepare(ym3438_ chip)
        {
            Bit8u rate;
            Bit8u sum;
            Bit8u inc = 0;
            Bit32u slot = chip.slot;
            Bit8u rate_sel;

            /* Prepare increment */
            rate = (Bit8u)((chip.eg_rate << 1) + chip.eg_ksv);

            if (rate > 0x3f)
            {
                rate = 0x3f;
            }

            sum = (Bit8u)(((rate >> 2) + chip.eg_shift_lock) & 0x0f);
            if (chip.eg_rate != 0 && chip.eg_quotient == 2)
            {
                if (rate < 48)
                {
                    switch (sum)
                    {
                        case 12:
                            inc = 1;
                            break;
                        case 13:
                            inc = (Bit8u)((rate >> 1) & 0x01);
                            break;
                        case 14:
                            inc = (Bit8u)(rate & 0x01);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    inc = (Bit8u)(ym3438_const.eg_stephi[rate & 0x03][chip.eg_timer_low_lock] + (rate >> 2) - 11);
                    if (inc > 4)
                    {
                        inc = 4;
                    }
                }
            }
            chip.eg_inc = inc;
            chip.eg_ratemax = (Bit8u)((rate >> 1) == 0x1f ? 1 : 0);

            /* Prepare rate & ksv */
            rate_sel = chip.eg_state[slot];
            if ((chip.eg_kon[slot] != 0 && chip.eg_ssg_repeat_latch[slot] != 0)
             || (chip.eg_kon[slot] == 0 && chip.eg_kon_latch[slot] != 0))
            {
                rate_sel = (Bit8u)EG_PARAM.eg_num_attack;
            }
            switch (rate_sel)
            {
                case (Bit8u)EG_PARAM.eg_num_attack:
                    chip.eg_rate = chip.ar[slot];
                    break;
                case (Bit8u)EG_PARAM.eg_num_decay:
                    chip.eg_rate = chip.dr[slot];
                    break;
                case (Bit8u)EG_PARAM.eg_num_sustain:
                    chip.eg_rate = chip.sr[slot];
                    break;
                case (Bit8u)EG_PARAM.eg_num_release:
                    chip.eg_rate = (Bit8u)((chip.rr[slot] << 1) | 0x01);
                    break;
                default:
                    break;
            }
            chip.eg_ksv = (Bit8u)(chip.pg_kcode >> (chip.ks[slot] ^ 0x03));
            if (chip.am[slot] != 0)
            {
                chip.eg_lfo_am = (Bit8u)(chip.lfo_am >> ym3438_const.eg_am_shift[chip.ams[chip.channel]]);
            }
            else
            {
                chip.eg_lfo_am = 0;
            }
            /* Delay TL & SL value */
            chip.eg_tl[1] = chip.eg_tl[0];
            chip.eg_tl[0] = chip.tl[slot];
            chip.eg_sl[1] = chip.eg_sl[0];
            chip.eg_sl[0] = chip.sl[slot];
        }

        private void OPN2_EnvelopeGenerate(ym3438_ chip)
        {
            Bit32u slot = (chip.slot + 23) % 24;
            Bit16u level;

            level = chip.eg_level[slot];
            //mlog("level:{0}\n", level);

            if (chip.eg_ssg_inv[slot] != 0)
            {
                /* Inverse */
                level = (Bit16u)(512 - level);
            }
            if (chip.mode_test_21[5] != 0)
            {
                level = 0;
            }
            level &= 0x3ff;

            /* Apply AM LFO */
            level += chip.eg_lfo_am;

            /* Apply TL */
            if (!(chip.mode_csm != 0 && chip.channel == 2 + 1))
            {
                level += (Bit16u)(chip.eg_tl[0] << 3);
            }
            if (level > 0x3ff)
            {
                level = 0x3ff;
            }
            chip.eg_out[slot] = level;
            //mlog("chip.eg_out[slot]:{0} slot:{1}\n", chip.eg_out[slot], slot);
        }

        private void OPN2_UpdateLFO(ym3438_ chip)
        {
            if ((chip.lfo_quotient & ym3438_const.lfo_cycles[chip.lfo_freq]) == ym3438_const.lfo_cycles[chip.lfo_freq])
            {
                chip.lfo_quotient = 0;
                chip.lfo_cnt++;
            }
            else
            {
                chip.lfo_quotient += chip.lfo_inc;
            }
            chip.lfo_cnt &= chip.lfo_en;
        }

        private void OPN2_FMPrepare(ym3438_ chip)
        {
            Bit32u slot = (chip.slot + 6) % 24;
            Bit32u channel = chip.channel;
            Bit16s mod, mod1, mod2;
            Bit32u op = slot / 6;
            Bit8u connect = chip.connect[channel];
            Bit32u prevslot = (chip.slot + 18) % 24;

            /* Calculate modulation */
            mod1 = mod2 = 0;

            if (ym3438_const.fm_algorithm[op][0][connect] != 0)
            {
                mod2 |= chip.fm_op1[channel][0];
            }
            if (ym3438_const.fm_algorithm[op][1][connect] != 0)
            {
                mod1 |= chip.fm_op1[channel][1];
            }
            if (ym3438_const.fm_algorithm[op][2][connect] != 0)
            {
                mod1 |= chip.fm_op2[channel];
            }
            if (ym3438_const.fm_algorithm[op][3][connect] != 0)
            {
                mod2 |= chip.fm_out[prevslot];
            }
            if (ym3438_const.fm_algorithm[op][4][connect] != 0)
            {
                mod1 |= chip.fm_out[prevslot];
            }
            mod = (Bit16s)(mod1 + mod2);
            if (op == 0)
            {
                /* Feedback */
                mod = (Bit16s)(mod >> (10 - chip.fb[channel]));
                if (chip.fb[channel] == 0)
                {
                    mod = 0;
                }
            }
            else
            {
                mod >>= 1;
            }
            chip.fm_mod[slot] = (Bit16u)mod;

            slot = (chip.slot + 18) % 24;
            /* OP1 */
            if (slot / 6 == 0)
            {
                chip.fm_op1[channel][1] = chip.fm_op1[channel][0];
                chip.fm_op1[channel][0] = chip.fm_out[slot];
            }
            /* OP2 */
            if (slot / 6 == 2)
            {
                chip.fm_op2[channel] = chip.fm_out[slot];
            }
        }

        private void OPN2_ChGenerate(ym3438_ chip)
        {
            Bit32u slot = (chip.slot + 18) % 24;
            Bit32u channel = chip.channel;
            Bit32u op = slot / 6;
            Bit32u test_dac = (Bit32u)(chip.mode_test_2c[5]);
            Bit16s acc = chip.ch_acc[channel];
            Bit16s add = (Bit16s)test_dac;
            Bit16s sum = 0;
            if (op == 0 && test_dac == 0)
            {
                acc = 0;
            }
            if (ym3438_const.fm_algorithm[op][5][chip.connect[channel]] != 0 && test_dac == 0)
            {
                add += (Bit16s)(chip.fm_out[slot] >> 5);
                //mlog("040   chip.fm_out[slot]:{0} slot:{1}\n", chip.fm_out[slot], slot);
            }
            sum = (Bit16s)(acc + add);
            //mlog("040   acc:{0} add:{1}\n", acc, add);
            /* Clamp */
            if (sum > 255)
            {
                sum = 255;
            }
            else if (sum < -256)
            {
                sum = -256;
            }

            if (op == 0 || test_dac != 0)
            {
                chip.ch_out[channel] = chip.ch_acc[channel];
            }
            chip.ch_acc[channel] = sum;
        }

        private void OPN2_ChOutput(ym3438_ chip)
        {
            Bit32u cycles = chip.cycles;
            Bit32u channel = chip.channel;
            Bit32u test_dac = (Bit32u)(chip.mode_test_2c[5]);
            Bit16s out_;
            Bit16s sign;
            Bit32u out_en;
            chip.ch_read = chip.ch_lock;
            if (chip.slot < 12)
            {
                /* Ch 4,5,6 */
                channel++;
            }
            if ((cycles & 3) == 0)
            {
                if (test_dac == 0)
                {
                    /* Lock value */
                    chip.ch_lock = chip.ch_out[channel];
                }
                chip.ch_lock_l = chip.pan_l[channel];
                chip.ch_lock_r = chip.pan_r[channel];
            }
            /* Ch 6 */
            if (((cycles >> 2) == 1 && chip.dacen != 0) || test_dac != 0)
            {
                out_ = (Bit16s)chip.dacdata;
                out_ <<= 7;
                out_ >>= 7;
            }
            else
            {
                out_ = chip.ch_lock;
            }

            //chip.mol = 0;
            //chip.mor = 0;
            if (ym3438_const.chip_type == ym3438_const.ym3438_type.ym2612)
            {

                out_en = (Bit32u)((((cycles & 3) == 3) || test_dac != 0) ? 1 : 0);
                /* YM2612 DAC emulation(not verified) */
                sign = (Bit16s)(out_ >> 8);
                if (out_ >= 0)
                {
                    out_++;
                    sign++;
                }

                chip.mol = sign;
                chip.mor = sign;

                if (chip.ch_lock_l != 0 && out_en != 0)
                {
                    chip.mol = out_;
                }
                //else
                //{
                //    chip.mol = sign;
                //}
                //Console.Write("040   out:{0} sign:{1}\n", out_, sign);
                if (chip.ch_lock_r != 0 && out_en != 0)
                {
                    chip.mor = out_;
                }
                //else
                //{
                //    chip.mor = sign;
                //}
                /* Amplify signal */
                chip.mol *= 3;
                chip.mor *= 3;
            }
            else
            {
                chip.mol = 0;
                chip.mor = 0;

                out_en = (Bit32u)((((cycles & 3) != 0) || test_dac != 0) ? 1 : 0);
                /* Discrete YM3438 seems has the ladder effect too */
                if (out_ >= 0 && ym3438_const.chip_type == ym3438_const.ym3438_type.discrete)
                {
                    out_++;
                }
                if (chip.ch_lock_l != 0 && out_en != 0)
                {
                    chip.mol = out_;
                }
                if (chip.ch_lock_r != 0 && out_en != 0)
                {
                    chip.mor = out_;
                }
            }
        }

        private void OPN2_FMGenerate(ym3438_ chip)
        {
            Bit32u slot = (chip.slot + 19) % 24;
            /* Calculate phase */
            Bit16u phase = (Bit16u)((chip.fm_mod[slot] + (chip.pg_phase[slot] >> 10)) & 0x3ff);
            //mlog("040   chip.fm_mod[slot]:{0} chip.pg_phase[slot]:{1}\n", chip.fm_mod[slot], chip.pg_phase[slot]);
            Bit16u quarter;
            Bit16u level;
            Bit16s output;
            if ((phase & 0x100) != 0)
            {
                quarter = (Bit16u)((phase ^ 0xff) & 0xff);
            }
            else
            {
                quarter = (Bit16u)(phase & 0xff);
            }
            level = ym3438_const.logsinrom[quarter];
            /* Apply envelope */
            level += (Bit16u)(chip.eg_out[slot] << 2);
            //mlog("040   quarter:{0} chip.eg_out[slot]:{1} slot:{2}\n", quarter, chip.eg_out[slot], slot);
            /* Transform */
            if (level > 0x1fff)
            {
                level = 0x1fff;
            }
            output = (Bit16s)(((ym3438_const.exprom[(level & 0xff) ^ 0xff] | 0x400) << 2) >> (level >> 8));
            //mlog("040   output:{0} level:{1}\n", output, level);
            if ((phase & 0x200) != 0)
            {
                output = (Bit16s)(((~output) ^ (chip.mode_test_21[4] << 13)) + 1);
            }
            else
            {
                output = (Bit16s)(output ^ (chip.mode_test_21[4] << 13));
            }
            output <<= 2;
            output >>= 2;
            chip.fm_out[slot] = output;
        }

        private void OPN2_DoTimerA(ym3438_ chip)
        {
            Bit16u time;
            Bit8u load;
            load = chip.timer_a_overflow;
            if (chip.cycles == 2)
            {
                /* Lock load value */
                load |= (Bit8u)((chip.timer_a_load_lock == 0 && chip.timer_a_load != 0) ? 1 : 0);
                chip.timer_a_load_lock = chip.timer_a_load;
                if (chip.mode_csm != 0)
                {
                    /* CSM KeyOn */
                    chip.mode_kon_csm = load;
                }
                else
                {
                    chip.mode_kon_csm = 0;
                }
            }
            /* Load counter */
            if (chip.timer_a_load_latch != 0)
            {
                time = chip.timer_a_reg;
            }
            else
            {
                time = chip.timer_a_cnt;
            }
            chip.timer_a_load_latch = load;
            /* Increase counter */
            if ((chip.cycles == 1 && chip.timer_a_load_lock != 0) || chip.mode_test_21[2] != 0)
            {
                time++;
            }
            /* Set overflow flag */
            if (chip.timer_a_reset != 0)
            {
                chip.timer_a_reset = 0;
                chip.timer_a_overflow_flag = 0;
            }
            else
            {
                chip.timer_a_overflow_flag |= (Bit8u)(chip.timer_a_overflow & chip.timer_a_enable);
            }
            chip.timer_a_overflow = (Bit8u)(time >> 10);
            chip.timer_a_cnt = (Bit16u)(time & 0x3ff);
        }

        private void OPN2_DoTimerB(ym3438_ chip)
        {
            Bit16u time;
            Bit8u load;
            load = chip.timer_b_overflow;
            if (chip.cycles == 2)
            {
                /* Lock load value */
                load |= (Bit8u)((chip.timer_b_load_lock == 0 && chip.timer_b_load != 0) ? 1 : 0);
                chip.timer_b_load_lock = chip.timer_b_load;
            }
            /* Load counter */
            if (chip.timer_b_load_latch != 0)
            {
                time = chip.timer_b_reg;
            }
            else
            {
                time = chip.timer_b_cnt;
            }
            chip.timer_b_load_latch = load;
            /* Increase counter */
            if (chip.cycles == 1)
            {
                chip.timer_b_subcnt++;
            }
            if ((chip.timer_b_subcnt == 0x10 && chip.timer_b_load_lock != 0) || chip.mode_test_21[2] != 0)
            {
                time++;
            }
            chip.timer_b_subcnt &= 0x0f;
            /* Set overflow flag */
            if (chip.timer_b_reset != 0)
            {
                chip.timer_b_reset = 0;
                chip.timer_b_overflow_flag = 0;
            }
            else
            {
                chip.timer_b_overflow_flag |= (Bit8u)(chip.timer_b_overflow & chip.timer_b_enable);
            }
            chip.timer_b_overflow = (Bit8u)(time >> 8);
            chip.timer_b_cnt = (Bit8u)(time & 0xff);
        }

        private void OPN2_KeyOn(ym3438_ chip)
        {
            /* Key On */
            chip.eg_kon_latch[chip.slot] = chip.mode_kon[chip.slot];
            chip.eg_kon_csm[chip.slot] = 0;
            //mlog("chip.eg_kon_latch[chip.slot]:{0} slot:{1}\n", chip.eg_kon_latch[chip.slot], chip.slot);
            if (chip.channel == 2 && chip.mode_kon_csm != 0)
            {
                /* CSM Key On */
                chip.eg_kon_latch[chip.slot] = 1;
                chip.eg_kon_csm[chip.slot] = 1;
            }
            if (chip.cycles == chip.mode_kon_channel)
            {
                /* OP1 */
                chip.mode_kon[chip.channel] = chip.mode_kon_operator[0];
                /* OP2 */
                chip.mode_kon[chip.channel + 12] = chip.mode_kon_operator[1];
                /* OP3 */
                chip.mode_kon[chip.channel + 6] = chip.mode_kon_operator[2];
                /* OP4 */
                chip.mode_kon[chip.channel + 18] = chip.mode_kon_operator[3];
            }
        }

        private void OPN2_Reset(ym3438_ chip, Bit32u rate, Bit32u clock)
        {
            Bit32u i, rateratio;
            rateratio = (Bit32u)chip.rateratio;
            //chip = new ym3438_();
            chip.eg_out = new Bit16u[24];
            chip.eg_level = new Bit16u[24];
            chip.eg_state = new Bit8u[24];
            chip.multi = new Bit8u[24];
            chip.pan_l = new Bit8u[6];
            chip.pan_r = new Bit8u[6];

            for (i = 0; i < 24; i++)
            {
                chip.eg_out[i] = 0x3ff;
                chip.eg_level[i] = 0x3ff;
                chip.eg_state[i] = (Bit8u)EG_PARAM.eg_num_release;
                chip.multi[i] = 1;
            }
            for (i = 0; i < 6; i++)
            {
                chip.pan_l[i] = 1;
                chip.pan_r[i] = 1;
            }
            if (rate != 0)
            {
                chip.rateratio = (Bit32s)((((Bit64u)(144 * rate)) << 10) / clock);// RSM_FRAC) / clock);
            }
            else
            {
                chip.rateratio = (Bit32s)rateratio;
            }
            //mlogsw = true;
            //mlog("rateratio{0} rate{1} clock{2}\n", chip.rateratio,rate,clock);
            //mlogsw = false;
        }

        public void OPN2_SetChipType(ym3438_const.ym3438_type type)
        {
            switch (type)
            {
                case ym3438_const.ym3438_type.asic:
                    ym3438_const.use_filter = 0;
                    break;
                case ym3438_const.ym3438_type.discrete:
                    ym3438_const.use_filter = 0;
                    break;
                case ym3438_const.ym3438_type.ym2612:
                    ym3438_const.use_filter = 1;
                    break;
                case ym3438_const.ym3438_type.ym2612_u:
                    type = ym3438_const.ym3438_type.ym2612;
                    ym3438_const.use_filter = 0;
                    break;
                case ym3438_const.ym3438_type.asic_lp:
                    type = ym3438_const.ym3438_type.asic;
                    ym3438_const.use_filter = 1;
                    break;
            }

            ym3438_const.chip_type = type;
        }

        private void OPN2_Clock(ym3438_ chip, Bit32s[] buffer)
        {
            //Console.Write("010 mol:{0} mor:{1}\n", chip.mol, chip.mor);

            chip.lfo_inc = (Bit8u)(chip.mode_test_21[1]);
            chip.pg_read >>= 1;
            chip.eg_read[1] >>= 1;
            chip.eg_cycle++;
            /* Lock envelope generator timer value */
            if (chip.cycles == 1 && chip.eg_quotient == 2)
            {
                if (chip.eg_cycle_stop != 0)
                {
                    chip.eg_shift_lock = 0;
                }
                else
                {
                    chip.eg_shift_lock = (Bit8u)(chip.eg_shift + 1);
                }
                chip.eg_timer_low_lock = (Bit8u)(chip.eg_timer & 0x03);
            }
            /* Cycle specific functions */
            switch (chip.cycles)
            {
                case 0:
                    chip.lfo_pm = (Bit8u)(chip.lfo_cnt >> 2);
                    if ((chip.lfo_cnt & 0x40) != 0)
                    {
                        chip.lfo_am = (Bit8u)(chip.lfo_cnt & 0x3f);
                    }
                    else
                    {
                        chip.lfo_am = (Bit8u)(chip.lfo_cnt ^ 0x3f);
                    }
                    chip.lfo_am <<= 1;
                    break;
                case 1:
                    chip.eg_quotient++;
                    chip.eg_quotient %= 3;
                    chip.eg_cycle = 0;
                    chip.eg_cycle_stop = 1;
                    chip.eg_shift = 0;
                    chip.eg_timer_inc |= (Bit8u)(chip.eg_quotient >> 1);
                    chip.eg_timer = (Bit16u)(chip.eg_timer + chip.eg_timer_inc);
                    chip.eg_timer_inc = (Bit8u)(chip.eg_timer >> 12);
                    chip.eg_timer &= 0xfff;
                    break;
                case 2:
                    chip.pg_read = chip.pg_phase[21] & 0x3ff;
                    chip.eg_read[1] = chip.eg_out[0];
                    break;
                case 13:
                    chip.eg_cycle = 0;
                    chip.eg_cycle_stop = 1;
                    chip.eg_shift = 0;
                    chip.eg_timer = (Bit16u)(chip.eg_timer + chip.eg_timer_inc);
                    chip.eg_timer_inc = (Bit8u)(chip.eg_timer >> 12);
                    chip.eg_timer &= 0xfff;
                    break;
                case 23:
                    chip.lfo_inc |= 1;
                    break;
            }


            chip.eg_timer &= (Bit16u)(~(chip.mode_test_21[5] << chip.eg_cycle));
            if ((((chip.eg_timer >> chip.eg_cycle) | (chip.pin_test_in & chip.eg_custom_timer)) & chip.eg_cycle_stop) != 0)
            {
                chip.eg_shift = chip.eg_cycle;
                chip.eg_cycle_stop = 0;
            }

            //Console.Write("020 mol:{0} mor:{1}\n", chip.mol, chip.mor);

            OPN2_DoIO(chip);

            //Console.Write("030 mol:{0} mor:{1}\n", chip.mol, chip.mor);

            OPN2_DoTimerA(chip);
            OPN2_DoTimerB(chip);
            OPN2_KeyOn(chip);

            //Console.Write("040 mol:{0} mor:{1}\n", chip.mol, chip.mor);

            OPN2_ChOutput(chip);
            //Console.Write("045 mol:{0} mor:{1}\n", chip.mol, chip.mor);
            OPN2_ChGenerate(chip);

            //Console.Write("050 mol:{0} mor:{1}\n", chip.mol, chip.mor);

            OPN2_FMPrepare(chip);
            OPN2_FMGenerate(chip);

            //Console.Write("060 mol:{0} mor:{1}\n", chip.mol, chip.mor);

            OPN2_PhaseGenerate(chip);
            OPN2_PhaseCalcIncrement(chip);

            //Console.Write("070 mol:{0} mor:{1}\n", chip.mol, chip.mor);

            OPN2_EnvelopeADSR(chip);
            OPN2_EnvelopeGenerate(chip);
            OPN2_EnvelopeSSGEG(chip);
            OPN2_EnvelopePrepare(chip);

            //Console.Write("080 mol:{0} mor:{1}\n", chip.mol, chip.mor);

            /* Prepare fnum & block */
            if (chip.mode_ch3 != 0)
            {
                /* Channel 3 special mode */
                switch (chip.slot)
                {
                    case 1: /* OP1 */
                        chip.pg_fnum = chip.fnum_3ch[1];
                        chip.pg_block = chip.block_3ch[1];
                        chip.pg_kcode = chip.kcode_3ch[1];
                        break;
                    case 7: /* OP3 */
                        chip.pg_fnum = chip.fnum_3ch[0];
                        chip.pg_block = chip.block_3ch[0];
                        chip.pg_kcode = chip.kcode_3ch[0];
                        break;
                    case 13: /* OP2 */
                        chip.pg_fnum = chip.fnum_3ch[2];
                        chip.pg_block = chip.block_3ch[2];
                        chip.pg_kcode = chip.kcode_3ch[2];
                        break;
                    case 19: /* OP4 */
                    default:
                        chip.pg_fnum = chip.fnum[(chip.channel + 1) % 6];
                        chip.pg_block = chip.block[(chip.channel + 1) % 6];
                        chip.pg_kcode = chip.kcode[(chip.channel + 1) % 6];
                        break;
                }
            }
            else
            {
                chip.pg_fnum = chip.fnum[(chip.channel + 1) % 6];
                chip.pg_block = chip.block[(chip.channel + 1) % 6];
                chip.pg_kcode = chip.kcode[(chip.channel + 1) % 6];
            }

            //Console.Write("090 mol:{0} mor:{1}\n", chip.mol, chip.mor);

            OPN2_UpdateLFO(chip);
            OPN2_DoRegWrite(chip);
            chip.cycles = (chip.cycles + 1) % 24;
            chip.slot = chip.cycles;
            chip.channel = chip.cycles % 6;

            //Console.Write("100 mol:{0} mor:{1}\n", chip.mol, chip.mor);

            buffer[0] = chip.mol;
            buffer[1] = chip.mor;

            //Console.Write("110 mol:{0} mor:{1}\n", chip.mol, chip.mor);
        }

        private void OPN2_Write(ym3438_ chip, Bit32u port, Bit8u data)
        {
            //if (port == 1 && data == 0xf1)
            //{
            //    mlogOn();
            //}
            //mlog("port:{0:x} data:{1:x}\n", port, data);

            port &= 3;
            chip.write_data = (Bit16u)(((port << 7) & 0x100) | data);
            if ((port & 1) != 0)
            {
                /* Data */
                chip.write_d |= 1;
            }
            else
            {
                /* Address */
                chip.write_a |= 1;
            }
        }

        private void OPN2_SetTestPin(ym3438_ chip, Bit32u value)
        {
            chip.pin_test_in = (Bit8u)(value & 1);
        }

        private Bit32u OPN2_ReadTestPin(ym3438_ chip)
        {
            if (chip.mode_test_2c[7] == 0)
            {
                return 0;
            }
            return (Bit32u)(chip.cycles == 23 ? 1 : 0);
        }

        private Bit32u OPN2_ReadIRQPin(ym3438_ chip)
        {
            return (Bit32u)(chip.timer_a_overflow_flag | chip.timer_b_overflow_flag);
        }

        private Bit8u OPN2_Read(ym3438_ chip, Bit32u port)
        {
            if ((port & 3) == 0 || ym3438_const.chip_type == ym3438_const.ym3438_type.asic)
            {
                if (chip.mode_test_21[6] != 0)
                {
                    /* Read test data */
                    //Bit32u slot = (chip.cycles + 18) % 24;
                    Bit16u testdata = (Bit16u)(((chip.pg_read & 0x01) << 15)
                                    | (((chip.eg_read[chip.mode_test_21[0]]) & 0x01) << 14));
                    if (chip.mode_test_2c[4] != 0)
                    {
                        testdata |= (Bit16u)(chip.ch_read & 0x1ff);
                    }
                    else
                    {
                        testdata |= (Bit16u)(chip.fm_out[(chip.slot + 18) % 24] & 0x3fff);
                    }
                    if (chip.mode_test_21[7] != 0)
                    {
                        return (Bit8u)(testdata & 0xff);
                    }
                    else
                    {
                        return (Bit8u)(testdata >> 8);
                    }
                }
                else
                {
                    return (Bit8u)((chip.busy << 7) | (chip.timer_b_overflow_flag << 1)
                         | chip.timer_a_overflow_flag);
                }
            }
            return 0;
        }

        private void OPN2_WriteBuffered(byte ChipID, Bit32u port, Bit8u data)
        {
            ym3438_ chip = ym3438_[ChipID];
            Bit64u time1, time2;
            Bit64u skip;

            if ((chip.writebuf[chip.writebuf_last].port & 0x04) != 0)
            {
                OPN2_Write(chip, (Bit32u)(chip.writebuf[chip.writebuf_last].port & 0X03),
                           chip.writebuf[chip.writebuf_last].data);

                chip.writebuf_cur = (chip.writebuf_last + 1) % 2048;// OPN_WRITEBUF_SIZE;
                skip = chip.writebuf[chip.writebuf_last].time - chip.writebuf_samplecnt;
                chip.writebuf_samplecnt = chip.writebuf[chip.writebuf_last].time;
                while (skip-- != 0)
                {
                    OPN2_Clock(chip, dmyBuffer);
                }
            }

            chip.writebuf[chip.writebuf_last].port = (Bit8u)((port & 0x03) | 0x04);
            chip.writebuf[chip.writebuf_last].data = data;
            time1 = chip.writebuf_lasttime + 15;// OPN_WRITEBUF_DELAY;
            time2 = chip.writebuf_samplecnt;

            if (time1 < time2)
            {
                time1 = time2;
            }

            chip.writebuf[chip.writebuf_last].time = time1;
            chip.writebuf_lasttime = time1;
            chip.writebuf_last = (chip.writebuf_last + 1) % 2048;// OPN_WRITEBUF_SIZE;
        }

        private void OPN2_GenerateResampled(byte ChipID, Bit32s[] buf)
        {
            ym3438_ chip = ym3438_[ChipID];
            Bit32u i;
            Bit32u mute;

            while (chip.samplecnt >= chip.rateratio)
            {
                chip.oldsamples[0] = chip.samples[0];
                chip.oldsamples[1] = chip.samples[1];
                chip.samples[0] = chip.samples[1] = 0;
                for (i = 0; i < 24; i++)
                {
                    switch (chip.cycles >> 2)
                    {
                        case 0: // Ch 2
                            mute = chip.mute[1];
                            break;
                        case 1: // Ch 6, DAC
                            mute = chip.mute[5 + chip.dacen];
                            break;
                        case 2: // Ch 4
                            mute = chip.mute[3];
                            break;
                        case 3: // Ch 1
                            mute = chip.mute[0];
                            break;
                        case 4: // Ch 5
                            mute = chip.mute[4];
                            break;
                        case 5: // Ch 3
                            mute = chip.mute[2];
                            break;
                        default:
                            mute = 0;
                            break;
                    }
                    OPN2_Clock(chip, grBuffer);
                    //Console.Write("l{0} r{1}\n", buffer[0], buffer[1]);
                    if (mute == 0)
                    {
                        chip.samples[0] += grBuffer[0];
                        chip.samples[1] += grBuffer[1];
                    }

                    while (chip.writebuf[chip.writebuf_cur].time <= chip.writebuf_samplecnt)
                    {
                        if ((chip.writebuf[chip.writebuf_cur].port & 0x04) == 0)
                        {
                            break;
                        }
                        chip.writebuf[chip.writebuf_cur].port &= 0x03;
                        OPN2_Write(chip, chip.writebuf[chip.writebuf_cur].port,
                                      chip.writebuf[chip.writebuf_cur].data);
                        chip.writebuf_cur = (chip.writebuf_cur + 1) % 2048;// OPN_WRITEBUF_SIZE;
                    }
                    chip.writebuf_samplecnt++;
                }
                if (ym3438_const.use_filter == 0)
                {
                    chip.samples[0] *= 11;// OUTPUT_FACTOR;
                    chip.samples[1] *= 11;// OUTPUT_FACTOR;
                }
                else
                {
                    //chip.samples[0] = chip.oldsamples[0] + FILTER_CUTOFF_I * (chip.samples[0] * OUTPUT_FACTOR_F - chip.oldsamples[0]);
                    //chip.samples[1] = chip.oldsamples[1] + FILTER_CUTOFF_I * (chip.samples[1] * OUTPUT_FACTOR_F - chip.oldsamples[1]);
                    chip.samples[0] = (int)(chip.oldsamples[0] + (1 - 0.512331301282628) * (chip.samples[0] * 12 - chip.oldsamples[0]));
                    chip.samples[1] = (int)(chip.oldsamples[1] + (1 - 0.512331301282628) * (chip.samples[1] * 12 - chip.oldsamples[1]));
                }
                chip.samplecnt -= chip.rateratio;
                //Console.Write("samplecnt{0}\n", chip.samplecnt);
            }
            buf[0] = (Bit32s)((chip.oldsamples[0] * (chip.rateratio - chip.samplecnt)
                             + chip.samples[0] * chip.samplecnt) / chip.rateratio);
            buf[1] = (Bit32s)((chip.oldsamples[1] * (chip.rateratio - chip.samplecnt)
                             + chip.samples[1] * chip.samplecnt) / chip.rateratio);
            //mlog("bl{0} br{1} chip.oldsamples[0]{2} chip.samples[0]{3}\n", buf[0], buf[1], chip.oldsamples[0], chip.samples[0]);
            chip.samplecnt += 1 << 10;// RSM_FRAC;
        }

        private void OPN2_GenerateStream(byte ChipID, Bit32s[][] sndptr, Bit32u numsamples)
        {
            Bit32u i;
            //Bit32s[] smpl, smpr;
            //smpl = sndptr[0];
            //smpr = sndptr[1];

            for (i = 0; i < numsamples; i++)
            {
                OPN2_GenerateResampled(ChipID, gsBuffer);
                //smpl[i] = gsBuffer[0];
                //smpr[i] = gsBuffer[1];
                sndptr[0][i] = gsBuffer[0];
                sndptr[1][i] = gsBuffer[1];
            }
        }

        public void OPN2_SetOptions(Bit8u flags)
        {
            switch ((flags >> 3) & 0x03)
            {
                case 0x00: // YM2612
                default:
                    OPN2_SetChipType(ym3438_const.ym3438_type.ym2612);
                    break;
                case 0x01: // ASIC YM3438
                    OPN2_SetChipType(ym3438_const.ym3438_type.asic);
                    break;
                case 0x02: // Discrete YM3438
                    OPN2_SetChipType(ym3438_const.ym3438_type.discrete);
                    break;
                case 0x03: // YM2612 without filter emulation
                    OPN2_SetChipType(ym3438_const.ym3438_type.ym2612_u);
                    break;
            }
        }

        public void OPN2_SetMute(byte ChipID, Bit32u mute)
        {
            Bit32u i;
            for (i = 0; i < 7; i++)
            {
                ym3438_[ChipID].mute[i] = (mute >> (int)i) & 0x01;
            }
        }
        public void OPN2_SetMute(byte ChipID, int ch,bool mute)
        {
            ym3438_[ChipID].mute[ch & 0x7] = (Bit32u)(mute ? 1 : 0);
        }




        public ym3438()
        {
            visVolume = new int[2][][] {
                new int[1][] { new int[2] { 0, 0 } }
                , new int[1][] { new int[2] { 0, 0 } }
            };

            for (int j = 0; j < 2; j++)
            {
                for (int i = 0; i < ym3438_[j].writebuf.Length; i++)
                {
                    ym3438_[j].writebuf[i] = new opn2_writebuf();
                }
            }
        }

        public override uint Start(byte ChipID, uint clock)
        {
            return Start(ChipID, clock, 0, null);
        }

        public override uint Start(byte ChipID, uint clock, uint clockValue, params object[] option)
        {
            //this.clock = clock;
            //this.clockValue = clockValue;
            //this.option = option;
            //OPN2_SetChipType(ym3438_const.ym3438_type.ym2612_u);//.discrete);//.asic);//.ym2612);
            OPN2_Reset(ym3438_[ChipID], clock, clockValue);
            return clock;
        }

        public override void Stop(byte ChipID)
        {
            OPN2_Reset(ym3438_[ChipID], 0, 0);
        }

        public override void Reset(byte ChipID)
        {
            OPN2_Reset(ym3438_[ChipID], 0, 0);
        }

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {
            OPN2_GenerateStream(ChipID, outputs, (uint)samples);

            visVolume[ChipID][0][0] = outputs[0][0];
            visVolume[ChipID][0][1] = outputs[1][0];
        }

        public override int Write(byte ChipID, int port, int adr, int data)
        {
            OPN2_WriteBuffered(ChipID, (Bit32u)adr, (Bit8u)data);
            return 0;
        }

    }




    public class ym3438_
    {
        public Bit32u cycles;
        public Bit32u slot;
        public Bit32u channel;
        public Bit16s mol, mor;
        /* IO */
        public Bit16u write_data;
        public Bit8u write_a;
        public Bit8u write_d;
        public Bit8u write_a_en;
        public Bit8u write_d_en;
        public Bit8u write_busy;
        public Bit8u write_busy_cnt;
        public Bit8u write_fm_address;
        public Bit8u write_fm_data;
        public Bit8u write_fm_mode_a;
        public Bit16u address;
        public Bit8u data;
        public Bit8u pin_test_in;
        public Bit8u pin_irq;
        public Bit8u busy;
        /* LFO */
        public Bit8u lfo_en;
        public Bit8u lfo_freq;
        public Bit8u lfo_pm;
        public Bit8u lfo_am;
        public Bit8u lfo_cnt;
        public Bit8u lfo_inc;
        public Bit8u lfo_quotient;
        /* Phase generator */
        public Bit16u pg_fnum;
        public Bit8u pg_block;
        public Bit8u pg_kcode;
        public Bit32u[] pg_inc = new Bit32u[24];
        public Bit32u[] pg_phase = new Bit32u[24];
        public Bit8u[] pg_reset = new Bit8u[24];
        public Bit32u pg_read;
        /* Envelope generator */
        public Bit8u eg_cycle;
        public Bit8u eg_cycle_stop;
        public Bit8u eg_shift;
        public Bit8u eg_shift_lock;
        public Bit8u eg_timer_low_lock;
        public Bit16u eg_timer;
        public Bit8u eg_timer_inc;
        public Bit16u eg_quotient;
        public Bit8u eg_custom_timer;
        public Bit8u eg_rate;
        public Bit8u eg_ksv;
        public Bit8u eg_inc;
        public Bit8u eg_ratemax;
        public Bit8u[] eg_sl = new Bit8u[2];
        public Bit8u eg_lfo_am;
        public Bit8u[] eg_tl = new Bit8u[2];
        public Bit8u[] eg_state = new Bit8u[24];
        public Bit16u[] eg_level = new Bit16u[24];
        public Bit16u[] eg_out = new Bit16u[24];
        public Bit8u[] eg_kon = new Bit8u[24];
        public Bit8u[] eg_kon_csm = new Bit8u[24];
        public Bit8u[] eg_kon_latch = new Bit8u[24];
        public Bit8u[] eg_csm_mode = new Bit8u[24];
        public Bit8u[] eg_ssg_enable = new Bit8u[24];
        public Bit8u[] eg_ssg_pgrst_latch = new Bit8u[24];
        public Bit8u[] eg_ssg_repeat_latch = new Bit8u[24];
        public Bit8u[] eg_ssg_hold_up_latch = new Bit8u[24];
        public Bit8u[] eg_ssg_dir = new Bit8u[24];
        public Bit8u[] eg_ssg_inv = new Bit8u[24];
        public Bit32u[] eg_read = new Bit32u[2];
        public Bit8u eg_read_inc;
        /* FM */
        public Bit16s[][] fm_op1 = new Bit16s[6][] { new Bit16s[2], new Bit16s[2], new Bit16s[2], new Bit16s[2], new Bit16s[2], new Bit16s[2] };
        public Bit16s[] fm_op2 = new Bit16s[6];
        public Bit16s[] fm_out = new Bit16s[24];
        public Bit16u[] fm_mod = new Bit16u[24];
        /* Channel */
        public Bit16s[] ch_acc = new Bit16s[6];
        public Bit16s[] ch_out = new Bit16s[6];
        public Bit16s ch_lock;
        public Bit8u ch_lock_l;
        public Bit8u ch_lock_r;
        public Bit16s ch_read;
        /* Timer */
        public Bit16u timer_a_cnt;
        public Bit16u timer_a_reg;
        public Bit8u timer_a_load_lock;
        public Bit8u timer_a_load;
        public Bit8u timer_a_enable;
        public Bit8u timer_a_reset;
        public Bit8u timer_a_load_latch;
        public Bit8u timer_a_overflow_flag;
        public Bit8u timer_a_overflow;

        public Bit16u timer_b_cnt;
        public Bit8u timer_b_subcnt;
        public Bit16u timer_b_reg;
        public Bit8u timer_b_load_lock;
        public Bit8u timer_b_load;
        public Bit8u timer_b_enable;
        public Bit8u timer_b_reset;
        public Bit8u timer_b_load_latch;
        public Bit8u timer_b_overflow_flag;
        public Bit8u timer_b_overflow;

        /* Register set */
        public Bit8u[] mode_test_21 = new Bit8u[8];
        public Bit8u[] mode_test_2c = new Bit8u[8];
        public Bit8u mode_ch3;
        public Bit8u mode_kon_channel;
        public Bit8u[] mode_kon_operator = new Bit8u[4];
        public Bit8u[] mode_kon = new Bit8u[24];
        public Bit8u mode_csm;
        public Bit8u mode_kon_csm;
        public Bit8u dacen;
        public Bit16s dacdata;

        public Bit8u[] ks = new Bit8u[24];
        public Bit8u[] ar = new Bit8u[24];
        public Bit8u[] sr = new Bit8u[24];
        public Bit8u[] dt = new Bit8u[24];
        public Bit8u[] multi = new Bit8u[24];
        public Bit8u[] sl = new Bit8u[24];
        public Bit8u[] rr = new Bit8u[24];
        public Bit8u[] dr = new Bit8u[24];
        public Bit8u[] am = new Bit8u[24];
        public Bit8u[] tl = new Bit8u[24];
        public Bit8u[] ssg_eg = new Bit8u[24];

        public Bit16u[] fnum = new Bit16u[6];
        public Bit8u[] block = new Bit8u[6];
        public Bit8u[] kcode = new Bit8u[6];
        public Bit16u[] fnum_3ch = new Bit16u[6];
        public Bit8u[] block_3ch = new Bit8u[6];
        public Bit8u[] kcode_3ch = new Bit8u[6];
        public Bit8u reg_a4;
        public Bit8u reg_ac;
        public Bit8u[] connect = new Bit8u[6];
        public Bit8u[] fb = new Bit8u[6];
        public Bit8u[] pan_l = new Bit8u[6], pan_r = new Bit8u[6];
        public Bit8u[] ams = new Bit8u[6];
        public Bit8u[] pms = new Bit8u[6];

        public Bit32u[] mute = new Bit32u[7];
        public Bit32s rateratio;
        public Bit32s samplecnt;
        public Bit32s[] oldsamples = new Bit32s[2];
        public Bit32s[] samples = new Bit32s[2];

        public Bit64u writebuf_samplecnt;
        public Bit32u writebuf_cur;
        public Bit32u writebuf_last;
        public Bit64u writebuf_lasttime;
        public opn2_writebuf[] writebuf = new opn2_writebuf[2048];// OPN_WRITEBUF_SIZE];
    }

    public class opn2_writebuf
    {
        public Bit64u time;
        public Bit8u port;
        public Bit8u data;
    }

}