using System;

namespace MDSound
{
    public class ym2612 : Instrument
    {

        /***********************************************************/
        /*                                                         */
        /* YM2612.C : YM2612 emulator                              */
        /*                                                         */
        /* Almost constantes are taken from the MAME core          */
        /*                                                         */
        /* This source is a part of Gens project                   */
        /* Written by St駱hane Dallongeville (gens@consolemul.com) */
        /* Copyright (c) 2002 by St駱hane Dallongeville            */
        /*                                                         */
        /***********************************************************/

        /***********************************************************/
        /*                                                         */
        /* Modified by Maxim, Blargg                               */
        /* - removed non-sound-related functionality               */
        /* - added high-pass PCM filter                            */
        /* - added per-channel muting control                      */
        /* - made it use a context struct to allow multiple        */
        /*   instances                                             */
        /*                                                         */
        /***********************************************************/

        private const double PI = 3.14159265358979323846;

        private const int ATTACK = 0;
        private const int DECAY = 1;
        private const int SUBSTAIN = 2;
        private const int RELEASE = 3;

        private const int SIN_HBITS = 12;                // Sinus phase counter int part
        private const int SIN_LBITS = (26 - SIN_HBITS);          // Sinus phase counter float part (best setting)

        private const int ENV_HBITS = 12;         // Env phase counter int part
        private const int ENV_LBITS = (28 - ENV_HBITS);          // Env phase counter float part (best setting)

        private const int LFO_HBITS = 10;         // LFO phase counter int part
        private const int LFO_LBITS = (28 - LFO_HBITS);        // LFO phase counter float part (best setting)

        private const int SIN_LENGHT = (1 << SIN_HBITS);
        private const int ENV_LENGHT = (1 << ENV_HBITS);
        private const int LFO_LENGHT = (1 << LFO_HBITS);

        private const int TL_LENGHT = (ENV_LENGHT * 3);         // Env + TL scaling + LFO

        private const int SIN_MASK = (SIN_LENGHT - 1);
        private const int ENV_MASK = (ENV_LENGHT - 1);
        private const int LFO_MASK = (LFO_LENGHT - 1);

        private const double ENV_STEP = (96.0 / ENV_LENGHT);        // ENV_MAX = 96 dB

        private const int ENV_ATTACK = ((ENV_LENGHT * 0) << ENV_LBITS);
        private const int ENV_DECAY = ((ENV_LENGHT * 1) << ENV_LBITS);
        private const int ENV_END = ((ENV_LENGHT * 2) << ENV_LBITS);

        private const int MAX_OUT_BITS = (SIN_HBITS + SIN_LBITS + 2);    // Modulation = -4 <--> +4
        private const int MAX_OUT = ((1 << MAX_OUT_BITS) - 1);

        //Just for tests stuff...
        //
        //#define COEF_MOD       0.5
        //#define MAX_OUT        ((int) (((1 << MAX_OUT_BITS) - 1) * COEF_MOD))

        private const int OUT_BITS = (OUTPUT_BITS - 2);
        private const int OUT_SHIFT = (MAX_OUT_BITS - OUT_BITS);
        private const int LIMIT_CH_OUT = ((int)(((1 << OUT_BITS) * 1.5) - 1));

        private const int PG_CUT_OFF = ((int)(78.0 / ENV_STEP));
        private const int ENV_CUT_OFF = ((int)(68.0 / ENV_STEP));

        private const int AR_RATE = 399128;
        private const int DR_RATE = 5514396;

        //        private const int  AR_RATE        426136			// good rate ?
        //        private const int  DR_RATE        (AR_RATE * 12)

        private const int LFO_FMS_LBITS = 9; // FIXED (LFO_FMS_BASE gives somethink as 1)
        private const int LFO_FMS_BASE = ((int)(0.05946309436 * 0.0338 * (double)(1 << LFO_FMS_LBITS)));

        private const int S0 = 0;  // Stupid typo of the YM2612
        private const int S1 = 2;
        private const int S2 = 1;
        private const int S3 = 3;


        /********************************************
         *            Partie variables              *
         ********************************************/


        //struct ym2612__ YM2612;

        static int[] SIN_TAB = new int[SIN_LENGHT];          // SINUS TABLE (pointer on TL TABLE)
        static int[] TL_TAB = new int[TL_LENGHT * 2];          // TOTAL LEVEL TABLE (positif and minus)
        private static uint[] ENV_TAB = new uint[2 * ENV_LENGHT + 8];  // ENV CURVE TABLE (attack & decay)

        //unsigned int ATTACK_TO_DECAY[ENV_LENGHT];  // Conversion from attack to decay phase
        uint[] DECAY_TO_ATTACK = new uint[ENV_LENGHT];  // Conversion from decay to attack phase

        uint[] FINC_TAB = new uint[2048];        // Frequency step table

        uint[] AR_TAB = new uint[128];          // Attack rate table
        uint[] DR_TAB = new uint[96];          // Decay rate table
        uint[][] DT_TAB = new uint[8][] {
            new uint[32], new uint[32], new uint[32], new uint[32],
            new uint[32], new uint[32], new uint[32], new uint[32]
        };          // Detune table
        uint[] SL_TAB = new uint[16];          // Substain level table
        uint[] NULL_RATE = new uint[32];          // Table for NULL rate

        int[] LFO_ENV_TAB = new int[LFO_LENGHT];        // LFO AMS TABLE (adjusted for 11.8 dB)
        int[] LFO_FREQ_TAB = new int[LFO_LENGHT];        // LFO FMS TABLE

        // int INTER_TAB[MAX_UPDATE_LENGHT];      // Interpolation table

        int[] LFO_INC_TAB = new int[8];              // LFO step table

        delegate void dlgUpdateChan(ym2612_ YM2612, channel_ CH, int[][] buf, int length);
        private static dlgUpdateChan[] UPDATE_CHAN = new dlgUpdateChan[8 * 8]    // Update Channel functions pointer table
        {
  Update_Chan_Algo0,
  Update_Chan_Algo1,
  Update_Chan_Algo2,
  Update_Chan_Algo3,
  Update_Chan_Algo4,
  Update_Chan_Algo5,
  Update_Chan_Algo6,
  Update_Chan_Algo7,

  Update_Chan_Algo0_LFO,
  Update_Chan_Algo1_LFO,
  Update_Chan_Algo2_LFO,
  Update_Chan_Algo3_LFO,
  Update_Chan_Algo4_LFO,
  Update_Chan_Algo5_LFO,
  Update_Chan_Algo6_LFO,
  Update_Chan_Algo7_LFO,

  Update_Chan_Algo0_Int,
  Update_Chan_Algo1_Int,
  Update_Chan_Algo2_Int,
  Update_Chan_Algo3_Int,
  Update_Chan_Algo4_Int,
  Update_Chan_Algo5_Int,
  Update_Chan_Algo6_Int,
  Update_Chan_Algo7_Int,

  Update_Chan_Algo0_LFO_Int,
  Update_Chan_Algo1_LFO_Int,
  Update_Chan_Algo2_LFO_Int,
  Update_Chan_Algo3_LFO_Int,
  Update_Chan_Algo4_LFO_Int,
  Update_Chan_Algo5_LFO_Int,
  Update_Chan_Algo6_LFO_Int,
  Update_Chan_Algo7_LFO_Int,

  null,null,null,null,
  null,null,null,null,
  null,null,null,null,
  null,null,null,null,
  null,null,null,null,
  null,null,null,null,
  null,null,null,null,
  null,null,null,null
        };

        delegate void dlgEnvNextEvent(slot_ SL);
        private static dlgEnvNextEvent[] ENV_NEXT_EVENT = new dlgEnvNextEvent[8] // Next Enveloppe phase functions pointer table
        {
  Env_Attack_Next,
  Env_Decay_Next,
  Env_Substain_Next,
  Env_Release_Next,
  Env_NULL_Next,
  Env_NULL_Next,
  Env_NULL_Next,
  Env_NULL_Next
        };

        private uint[] DT_DEF_TAB = new uint[4 * 32]
        {
// FD = 0
  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,

// FD = 1
  0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2,
  2, 3, 3, 3, 4, 4, 4, 5, 5, 6, 6, 7, 8, 8, 8, 8,

// FD = 2
  1, 1, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3, 4, 4, 4, 5,
  5, 6, 6, 7, 8, 8, 9, 10, 11, 12, 13, 14, 16, 16, 16, 16,

// FD = 3
  2, 2, 2, 2, 2, 3, 3, 3, 4, 4, 4, 5, 5, 6, 6, 7,
  8 , 8, 9, 10, 11, 12, 13, 14, 16, 17, 19, 20, 22, 22, 22, 22
        };

        private uint[] FKEY_TAB = new uint[16]
        {
            0, 0, 0, 0,
            0, 0, 0, 1,
            2, 3, 3, 3,
            3, 3, 3, 3
        };

        private uint[] LFO_AMS_TAB = new uint[4]
        {
            31, 4, 1, 0
        };

        private uint[] LFO_FMS_TAB = new uint[8]
        {
            LFO_FMS_BASE * 0, LFO_FMS_BASE * 1,
            LFO_FMS_BASE * 2, LFO_FMS_BASE * 3,
            LFO_FMS_BASE * 4, LFO_FMS_BASE * 6,
            LFO_FMS_BASE * 12, LFO_FMS_BASE * 24
        };

        private static int int_cnt;                // Interpolation calculation


#if DEBUG            // Debug
        //FILE* debug_file = NULL;
#endif


        /* Gens */

        //extern unsigned int Sound_Extrapol[312][2];
        //extern int Seg_L[882], Seg_R[882];
        //extern int VDP_Current_Line;
        //extern int GYM_Dumping;
        //extern int YM2612_Enable;
        //extern int DAC_Enable=1;

        //int Update_GYM_Dump(char v0, char v1, char v2);

        private int YM2612_Enable;
        private int YM2612_Improv;
        //int DAC_Enable = 1;
        private int[][] YM_Buf = new int[2][];
        //private int YM_Len = 0;
        private static int YM2612_Enable_SSGEG = 1; // enable SSG-EG envelope (causes inacurate sound sometimes - rodrigo)
        private int DAC_Highpass_Enable = 1; // sometimes it creates a terrible noise

        public static int[] vol = new int[2];

        /* end */

        /**********************************************
        *           fonctions calcul param            *
        ***********************************************/


        private void CALC_FINC_SL(slot_ SL, int finc, int kc)
        {
            int ksr;

            SL.Finc = (int)((finc + SL.DT[kc]) * SL.MUL);

            ksr = kc >> SL.KSR_S;  // keycode atténuation

            //#if YM_DEBUG_LEVEL > 1
            //fprintf(debug_file, "FINC = %d  SL.Finc = %d\n", finc, SL.Finc);
            //#endif

            if (SL.KSR != ksr) // si le KSR a changé alors
            {                  // les différents taux pour l'enveloppe sont mis à jour
                SL.KSR = ksr;

                SL.EincA = (int)SL.AR[SL.ARindex + ksr];
                SL.EincD = (int)SL.DR[SL.DRindex + ksr];
                SL.EincS = (int)SL.SR[SL.SRindex + ksr];
                SL.EincR = (int)SL.RR[SL.RRindex + ksr];

                if (SL.Ecurp == ATTACK) SL.Einc = SL.EincA;
                else if (SL.Ecurp == DECAY) SL.Einc = SL.EincD;
                else if (SL.Ecnt < ENV_END)
                {
                    if (SL.Ecurp == SUBSTAIN) SL.Einc = SL.EincS;
                    else if (SL.Ecurp == RELEASE) SL.Einc = SL.EincR;
                }

                //#if YM_DEBUG_LEVEL > 1
                //  fprintf(debug_file, "KSR = %.4X  EincA = %.8X EincD = %.8X EincS = %.8X EincR = %.8X\n", ksr, SL.EincA, SL.EincD, SL.EincS, SL.EincR);
                //#endif
            }
        }


        private void CALC_FINC_CH(channel_ CH)
        {
            int finc, kc;

            finc = (int)(FINC_TAB[CH.FNUM[0]] >> (7 - CH.FOCT[0]));
            kc = CH.KC[0];

            CALC_FINC_SL(CH.SLOT[0], finc, kc);
            CALC_FINC_SL(CH.SLOT[1], finc, kc);
            CALC_FINC_SL(CH.SLOT[2], finc, kc);
            CALC_FINC_SL(CH.SLOT[3], finc, kc);
        }



        /***********************************************
         *             fonctions setting               *
         ***********************************************/


        private void KEY_ON(channel_ CH, int nsl)
        {
            slot_ SL = CH.SLOT[nsl];  // on recupère le bon pointeur de slot

            if (SL.Ecurp == RELEASE)    // la touche est-elle relâchée ?
            {
                SL.Fcnt = 0;

                // Fix Ecco 2 splash sound

                SL.Ecnt = (int)((DECAY_TO_ATTACK[ENV_TAB[SL.Ecnt >> ENV_LBITS]] + ENV_ATTACK) & SL.ChgEnM);
                SL.ChgEnM = -1;// 0xFFFFFFFF;

                //    SL.Ecnt = DECAY_TO_ATTACK[ENV_TAB[SL.Ecnt >> ENV_LBITS]] + ENV_ATTACK;
                //    SL.Ecnt = 0;

                SL.Einc = SL.EincA;
                SL.Ecmp = ENV_DECAY;
                SL.Ecurp = ATTACK;
            }
        }


        private void KEY_OFF(channel_ CH, int nsl)
        {
            slot_ SL = CH.SLOT[nsl];  // on recupère le bon pointeur de slot

            if (SL.Ecurp != RELEASE)    // la touche est-elle appuyée ?
            {
                if (SL.Ecnt < ENV_DECAY)  // attack phase ?
                {
                    SL.Ecnt = (int)((ENV_TAB[SL.Ecnt >> ENV_LBITS] << ENV_LBITS) + ENV_DECAY);
                }

                SL.Einc = SL.EincR;
                SL.Ecmp = ENV_END;
                SL.Ecurp = RELEASE;
            }
        }


        private void CSM_Key_Control(ym2612_ YM2612)
        {
            KEY_ON(YM2612.CHANNEL[2], 0);
            KEY_ON(YM2612.CHANNEL[2], 1);
            KEY_ON(YM2612.CHANNEL[2], 2);
            KEY_ON(YM2612.CHANNEL[2], 3);
        }


        private int SLOT_SET(ym2612_ YM2612, int Adr, byte data)
        {
            channel_ CH;
            slot_ SL;
            int nch, nsl;

            if ((nch = Adr & 3) == 3) return 1;
            nsl = (Adr >> 2) & 3;

            if ((Adr & 0x100) != 0) nch += 3;

            CH = YM2612.CHANNEL[nch];
            SL = CH.SLOT[nsl];

            switch (Adr & 0xF0)
            {
                case 0x30:
                    if ((SL.MUL = (data & 0x0F)) != 0) SL.MUL <<= 1;
                    else SL.MUL = 1;

                    SL.DT = DT_TAB[(data >> 4) & 7];

                    CH.SLOT[0].Finc = -1;

                    //#if YM_DEBUG_LEVEL > 1
                    //      fprintf(debug_file, "CHANNEL[%d], SLOT[%d] DTMUL = %.2X\n", nch, nsl, data & 0x7F);
                    //#endif
                    break;

                case 0x40:
                    SL.TL = data & 0x7F;

                    // SOR2 do a lot of TL adjustement and this fix R.Shinobi jump sound...
                    YM2612_Special_Update(YM2612);

                    //#if ((ENV_HBITS - 7) < 0)
                    //      SL.TLL = SL.TL >> (7 - ENV_HBITS);
                    //#else
                    SL.TLL = SL.TL << (ENV_HBITS - 7);
                    //#endif

                    //#if YM_DEBUG_LEVEL > 1
                    //      fprintf(debug_file, "CHANNEL[%d], SLOT[%d] TL = %.2X\n", nch, nsl, SL.TL);
                    //#endif
                    break;

                case 0x50:
                    SL.KSR_S = 3 - (data >> 6);

                    CH.SLOT[0].Finc = -1;

                    if ((data &= 0x1F) != 0) { SL.AR = AR_TAB; SL.ARindex = data << 1; }
                    else { 
                        SL.AR = NULL_RATE;
                        SL.ARindex = 0; 
                    }

                    SL.EincA = (int)SL.AR[SL.ARindex + SL.KSR];
                    if (SL.Ecurp == ATTACK) SL.Einc = SL.EincA;

                    //#if YM_DEBUG_LEVEL > 1
                    //      fprintf(debug_file, "CHANNEL[%d], SLOT[%d] AR = %.2X  EincA = %.6X\n", nch, nsl, data, SL.EincA);
                    //#endif
                    break;

                case 0x60:
                    if ((SL.AMSon = (data & 0x80)) != 0) SL.AMS = CH.AMS;
                    else SL.AMS = 31;

                    if ((data &= 0x1F) > 0) { SL.DR = DR_TAB; SL.DRindex = data << 1; }
                    else { SL.DR = NULL_RATE; SL.DRindex = 0; }

                    SL.EincD = (int)SL.DR[SL.DRindex + SL.KSR];
                    if (SL.Ecurp == DECAY) SL.Einc = SL.EincD;

                    //#if YM_DEBUG_LEVEL > 1
                    //      fprintf(debug_file, "CHANNEL[%d], SLOT[%d] AMS = %d  DR = %.2X  EincD = %.6X\n", nch, nsl, SL.AMSon, data, SL.EincD);
                    //#endif
                    break;

                case 0x70:
                    if ((data &= 0x1F) != 0) { SL.SR = DR_TAB; SL.SRindex = data << 1; }
                    else { SL.SR = NULL_RATE; SL.SRindex = 0; }

                    SL.EincS = (int)SL.SR[SL.SRindex + SL.KSR];
                    if ((SL.Ecurp == SUBSTAIN) && (SL.Ecnt < ENV_END)) SL.Einc = SL.EincS;

                    //#if YM_DEBUG_LEVEL > 1
                    //      fprintf(debug_file, "CHANNEL[%d], SLOT[%d] SR = %.2X  EincS = %.6X\n", nch, nsl, data, SL.EincS);
                    //#endif
                    break;

                case 0x80:
                    SL.SLL = (int)SL_TAB[data >> 4];

                    SL.RR = DR_TAB; SL.RRindex = ((data & 0xF) << 2) + 2;

                    SL.EincR = (int)SL.RR[SL.RRindex + SL.KSR];
                    if ((SL.Ecurp == RELEASE) && (SL.Ecnt < ENV_END)) SL.Einc = SL.EincR;

                    //#if YM_DEBUG_LEVEL > 1
                    //      fprintf(debug_file, "CHANNEL[%d], SLOT[%d] SL = %.8X\n", nch, nsl, SL.SLL);
                    //      fprintf(debug_file, "CHANNEL[%d], SLOT[%d] RR = %.2X  EincR = %.2X\n", nch, nsl, ((data & 0xF) << 1) | 2, SL.EincR);
                    //#endif
                    break;

                case 0x90:
                    /* SSG-EG envelope shapes :
                    //
                    // E  At Al H
                    //
                    // 1  0  0  0  \\\\
                    //
                    // 1  0  0  1  \___
                    //
                    // 1  0  1  0  \/\/
                    //              ___
                    // 1  0  1  1  \
                    //
                    // 1  1  0  0  ////
                    //              ___
                    // 1  1  0  1  /
                    //
                    // 1  1  1  0  /\/\
                    //
                    // 1  1  1  1  /___
                    //
                    // E  = SSG-EG enable
                    // At = Start negate
                    // Al = Altern
                    // H  = Hold */
                    if (YM2612_Enable_SSGEG != 0)
                    {
                        if ((data & 0x08) != 0) SL.SEG = data & 0x0F;
                        else SL.SEG = 0;

                        //#if YM_DEBUG_LEVEL > 1
                        //        fprintf(debug_file, "CHANNEL[%d], SLOT[%d] SSG-EG = %.2X\n", nch, nsl, data);
                        //#endif
                    }
                    break;
            }

            return 0;
        }


        private int CHANNEL_SET(ym2612_ YM2612, int Adr, byte data)
        {
            channel_ CH;
            int num;

            if ((num = Adr & 3) == 3) return 1;

            switch (Adr & 0xFC)
            {
                case 0xA0:
                    if ((Adr & 0x100) != 0) num += 3;
                    CH = YM2612.CHANNEL[num];

                    YM2612_Special_Update(YM2612);

                    CH.FNUM[0] = (CH.FNUM[0] & 0x700) + data;
                    CH.KC[0] = (int)(((uint)CH.FOCT[0] << 2) | FKEY_TAB[CH.FNUM[0] >> 7]);

                    CH.SLOT[0].Finc = -1;

                    //#if YM_DEBUG_LEVEL > 1
                    //      fprintf(debug_file, "CHANNEL[%d] part1 FNUM = %d  KC = %d\n", num, CH.FNUM[0], CH.KC[0]);
                    //#endif
                    break;

                case 0xA4:
                    if ((Adr & 0x100) != 0) num += 3;
                    CH = YM2612.CHANNEL[num];

                    YM2612_Special_Update(YM2612);

                    CH.FNUM[0] = (CH.FNUM[0] & 0x0FF) + ((int)(data & 0x07) << 8);
                    CH.FOCT[0] = (data & 0x38) >> 3;
                    CH.KC[0] = (int)(((uint)CH.FOCT[0] << 2) | FKEY_TAB[CH.FNUM[0] >> 7]);

                    CH.SLOT[0].Finc = -1;

                    //#if YM_DEBUG_LEVEL > 1
                    //      fprintf(debug_file, "CHANNEL[%d] part2 FNUM = %d  FOCT = %d  KC = %d\n", num, CH.FNUM[0], CH.FOCT[0], CH.KC[0]);
                    //#endif
                    break;

                case 0xA8:
                    if (Adr < 0x100)
                    {
                        num++;

                        YM2612_Special_Update(YM2612);

                        YM2612.CHANNEL[2].FNUM[num] = (YM2612.CHANNEL[2].FNUM[num] & 0x700) + data;
                        YM2612.CHANNEL[2].KC[num] = (int)(((uint)YM2612.CHANNEL[2].FOCT[num] << 2) |
                            FKEY_TAB[YM2612.CHANNEL[2].FNUM[num] >> 7]);

                        YM2612.CHANNEL[2].SLOT[0].Finc = -1;

                        //#if YM_DEBUG_LEVEL > 1
                        //        fprintf(debug_file, "CHANNEL[2] part1 FNUM[%d] = %d  KC[%d] = %d\n", num, YM2612.CHANNEL[2].FNUM[num], num, YM2612.CHANNEL[2].KC[num]);
                        //#endif
                    }
                    break;

                case 0xAC:
                    if (Adr < 0x100)
                    {
                        num++;

                        YM2612_Special_Update(YM2612);

                        YM2612.CHANNEL[2].FNUM[num] = (YM2612.CHANNEL[2].FNUM[num] & 0x0FF) +
                            ((int)(data & 0x07) << 8);
                        YM2612.CHANNEL[2].FOCT[num] = (data & 0x38) >> 3;
                        YM2612.CHANNEL[2].KC[num] = (int)(((uint)YM2612.CHANNEL[2].FOCT[num] << 2) |
                            FKEY_TAB[YM2612.CHANNEL[2].FNUM[num] >> 7]);

                        YM2612.CHANNEL[2].SLOT[0].Finc = -1;

                        //#if YM_DEBUG_LEVEL > 1
                        //        fprintf(debug_file, "CHANNEL[2] part2 FNUM[%d] = %d  FOCT[%d] = %d  KC[%d] = %d\n", num, YM2612.CHANNEL[2].FNUM[num], num, YM2612.CHANNEL[2].FOCT[num], num, YM2612.CHANNEL[2].KC[num]);
                        //#endif
                    }
                    break;

                case 0xB0:
                    if ((Adr & 0x100) != 0) num += 3;
                    CH = YM2612.CHANNEL[num];

                    if (CH.ALGO != (data & 7))
                    {
                        // Fix VectorMan 2 heli sound (level 1)
                        YM2612_Special_Update(YM2612);

                        CH.ALGO = data & 7;

                        CH.SLOT[0].ChgEnM = 0;
                        CH.SLOT[1].ChgEnM = 0;
                        CH.SLOT[2].ChgEnM = 0;
                        CH.SLOT[3].ChgEnM = 0;
                    }

                    CH.FB = 9 - ((data >> 3) & 7);                // Real thing ?

                    //      if(CH.FB = ((data >> 3) & 7)) CH.FB = 9 - CH.FB;    // Thunder force 4 (music stage 8), Gynoug, Aladdin bug sound...
                    //      else CH.FB = 31;

                    //#if YM_DEBUG_LEVEL > 1
                    //      fprintf(debug_file, "CHANNEL[%d] ALGO = %d  FB = %d\n", num, CH.ALGO, CH.FB);
                    //#endif
                    break;

                case 0xB4:
                    if ((Adr & 0x100) != 0) num += 3;
                    CH = YM2612.CHANNEL[num];

                    YM2612_Special_Update(YM2612);

                    if ((data & 0x80) != 0) CH.LEFT = -1;// 0xFFFFFFFF;
                    else CH.LEFT = 0;

                    if ((data & 0x40) != 0) CH.RIGHT = -1;// 0xFFFFFFFF;
                    else CH.RIGHT = 0;

                    CH.AMS = (int)LFO_AMS_TAB[(data >> 4) & 3];
                    CH.FMS = (int)LFO_FMS_TAB[data & 7];

                    if (CH.SLOT[0].AMSon != 0) CH.SLOT[0].AMS = CH.AMS;
                    else CH.SLOT[0].AMS = 31;
                    if (CH.SLOT[1].AMSon != 0) CH.SLOT[1].AMS = CH.AMS;
                    else CH.SLOT[1].AMS = 31;
                    if (CH.SLOT[2].AMSon != 0) CH.SLOT[2].AMS = CH.AMS;
                    else CH.SLOT[2].AMS = 31;
                    if (CH.SLOT[3].AMSon != 0) CH.SLOT[3].AMS = CH.AMS;
                    else CH.SLOT[3].AMS = 31;

                    //#if YM_DEBUG_LEVEL > 0
                    //      fprintf(debug_file, "CHANNEL[%d] AMS = %d  FMS = %d\n", num, CH.AMS, CH.FMS);
                    //#endif
                    break;
            }

            return 0;
        }


        private int YM_SET(ym2612_ YM2612, int Adr, byte data)
        {
            channel_ CH;
            int nch;

            switch (Adr)
            {
                case 0x22:
                    if ((data & 8) != 0)
                    {
                        // Cool Spot music 1, LFO modified severals time which
                        // distord the sound, have to check that on a real genesis...

                        YM2612.LFOinc = LFO_INC_TAB[data & 7];

                        //#if YM_DEBUG_LEVEL > 0
                        //        fprintf(debug_file, "\nLFO Enable, LFOinc = %.8X   %d\n", YM2612.LFOinc, data & 7);
                        //#endif
                    }
                    else
                    {
                        YM2612.LFOinc = YM2612.LFOcnt = 0;

                        //#if YM_DEBUG_LEVEL > 0
                        //        fprintf(debug_file, "\nLFO Disable\n");
                        //#endif
                    }
                    break;

                case 0x24:
                    YM2612.TimerA = (YM2612.TimerA & 0x003) | (((int)data) << 2);

                    if (YM2612.TimerAL != (1024 - YM2612.TimerA) << 12)
                    {
                        YM2612.TimerAcnt = YM2612.TimerAL = (1024 - YM2612.TimerA) << 12;

                        //#if YM_DEBUG_LEVEL > 1
                        //        fprintf(debug_file, "Timer A Set = %.8X\n", YM2612.TimerAcnt);
                        //#endif
                    }
                    break;

                case 0x25:
                    YM2612.TimerA = (YM2612.TimerA & 0x3fc) | (data & 3);

                    if (YM2612.TimerAL != (1024 - YM2612.TimerA) << 12)
                    {
                        YM2612.TimerAcnt = YM2612.TimerAL = (1024 - YM2612.TimerA) << 12;

                        //#if YM_DEBUG_LEVEL > 1
                        //        fprintf(debug_file, "Timer A Set = %.8X\n", YM2612.TimerAcnt);
                        //#endif
                    }
                    break;

                case 0x26:
                    YM2612.TimerB = data;

                    if (YM2612.TimerBL != (256 - YM2612.TimerB) << (4 + 12))
                    {
                        YM2612.TimerBcnt = YM2612.TimerBL = (256 - YM2612.TimerB) << (4 + 12);

                        //#if YM_DEBUG_LEVEL > 1
                        //        fprintf(debug_file, "Timer B Set = %.8X\n", YM2612.TimerBcnt);
                        //#endif
                    }
                    break;

                case 0x27:
                    // Paramètre divers
                    // b7 = CSM MODE
                    // b6 = 3 slot mode
                    // b5 = reset b
                    // b4 = reset a
                    // b3 = timer enable b
                    // b2 = timer enable a
                    // b1 = load b
                    // b0 = load a

                    if (((data ^ YM2612.Mode) & 0x40) != 0)
                    {
                        // We changed the channel 2 mode, so recalculate phase step
                        // This fix the punch sound in Street of Rage 2

                        YM2612_Special_Update(YM2612);

                        YM2612.CHANNEL[2].SLOT[0].Finc = -1;    // recalculate phase step
                    }

                    //      if((data & 2) && (YM2612.Status & 2)) YM2612.TimerBcnt = YM2612.TimerBL;
                    //      if((data & 1) && (YM2612.Status & 1)) YM2612.TimerAcnt = YM2612.TimerAL;

                    //      YM2612.Status &= (~data >> 4);          // Reset du Status au cas ou c'est demand?
                    YM2612.Status &= (~data >> 4) & (data >> 2);  // Reset Status

                    YM2612.Mode = data;

                    //#if YM_DEBUG_LEVEL > 0
                    //      fprintf(debug_file, "Mode reg = %.2X\n", data);
                    //#endif
                    break;

                case 0x28:
                    if ((nch = data & 3) == 3) return 1;

                    if ((data & 4) != 0) nch += 3;
                    CH = YM2612.CHANNEL[nch];

                    YM2612_Special_Update(YM2612);

                    if ((data & 0x10) != 0) KEY_ON(CH, S0);  // On appuie sur la touche pour le slot 1
                    else KEY_OFF(CH, S0);        // On relâche la touche pour le slot 1
                    if ((data & 0x20) != 0) KEY_ON(CH, S1);  // On appuie sur la touche pour le slot 3
                    else KEY_OFF(CH, S1);        // On relâche la touche pour le slot 3
                    if ((data & 0x40) != 0) KEY_ON(CH, S2);  // On appuie sur la touche pour le slot 2
                    else KEY_OFF(CH, S2);        // On relâche la touche pour le slot 2
                    if ((data & 0x80) != 0) KEY_ON(CH, S3);  // On appuie sur la touche pour le slot 4
                    else KEY_OFF(CH, S3);        // On relâche la touche pour le slot 4

                    //#if YM_DEBUG_LEVEL > 0
                    //      fprintf(debug_file, "CHANNEL[%d]  KEY %.1X\n", nch, ((data & 0xf0) >> 4));
                    //#endif
                    break;

                case 0x2A:
                    YM2612.DACdata = ((int)(uint)data - 0x80) << DAC_SHIFT;  // donnée du DAC
                    break;

                case 0x2B:
                    if ((YM2612.DAC ^ (data & 0x80)) != 0) YM2612_Special_Update(YM2612);

                    YM2612.DAC = data & 0x80;  // activation/désactivation du DAC
                    break;
            }

            return 0;
        }



        /***********************************************
         *          fonctions de génération            *
         ***********************************************/
        private static void Env_NULL_Next(slot_ SL)
        {
        }

        private static void Env_Attack_Next(slot_ SL)
        {
            // Verified with Gynoug even in HQ (explode SFX)
            SL.Ecnt = ENV_DECAY;

            SL.Einc = SL.EincD;
            SL.Ecmp = SL.SLL;
            SL.Ecurp = DECAY;
        }

        private static void Env_Decay_Next(slot_ SL)
        {
            // Verified with Gynoug even in HQ (explode SFX)
            SL.Ecnt = SL.SLL;

            SL.Einc = SL.EincS;
            SL.Ecmp = ENV_END;
            SL.Ecurp = SUBSTAIN;
        }

        private static void Env_Substain_Next(slot_ SL)
        {
            if (YM2612_Enable_SSGEG != 0)
            {
                if ((SL.SEG & 8) != 0)  // SSG envelope type
                {
                    if ((SL.SEG & 1) != 0)
                    {
                        SL.Ecnt = ENV_END;
                        SL.Einc = 0;
                        SL.Ecmp = ENV_END + 1;
                    }
                    else
                    {
                        // re KEY ON

                        // SL.Fcnt = 0;
                        // SL.ChgEnM = 0xFFFFFFFF;

                        SL.Ecnt = 0;
                        SL.Einc = SL.EincA;
                        SL.Ecmp = ENV_DECAY;
                        SL.Ecurp = ATTACK;
                    }

                    SL.SEG ^= (SL.SEG & 2) << 1;
                }
                else
                {
                    SL.Ecnt = ENV_END;
                    SL.Einc = 0;
                    SL.Ecmp = ENV_END + 1;
                }
            }
            else
            {
                SL.Ecnt = ENV_END;
                SL.Einc = 0;
                SL.Ecmp = ENV_END + 1;
            }
        }

        private static void Env_Release_Next(slot_ SL)
        {
            SL.Ecnt = ENV_END;
            SL.Einc = 0;
            SL.Ecmp = ENV_END + 1;
        }

        private static void GET_CURRENT_PHASE(ym2612_ YM2612, channel_ CH)
        {
            YM2612.in0 = CH.SLOT[S0].Fcnt;
            YM2612.in1 = CH.SLOT[S1].Fcnt;
            YM2612.in2 = CH.SLOT[S2].Fcnt;
            YM2612.in3 = CH.SLOT[S3].Fcnt;
        }

        private static void UPDATE_PHASE(channel_ CH)
        {
            CH.SLOT[S0].Fcnt += CH.SLOT[S0].Finc;
            CH.SLOT[S1].Fcnt += CH.SLOT[S1].Finc;
            CH.SLOT[S2].Fcnt += CH.SLOT[S2].Finc;
            CH.SLOT[S3].Fcnt += CH.SLOT[S3].Finc;
        }

        private static void UPDATE_PHASE_LFO(ym2612_ YM2612, channel_ CH, int i, ref int freq_LFO)
        {
            if ((freq_LFO = (CH.FMS * YM2612.LFO_FREQ_UP[i]) >> (LFO_HBITS - 1)) != 0)
            {
                CH.SLOT[S0].Fcnt += CH.SLOT[S0].Finc + ((CH.SLOT[S0].Finc * freq_LFO) >> LFO_FMS_LBITS);
                CH.SLOT[S1].Fcnt += CH.SLOT[S1].Finc + ((CH.SLOT[S1].Finc * freq_LFO) >> LFO_FMS_LBITS);
                CH.SLOT[S2].Fcnt += CH.SLOT[S2].Finc + ((CH.SLOT[S2].Finc * freq_LFO) >> LFO_FMS_LBITS);
                CH.SLOT[S3].Fcnt += CH.SLOT[S3].Finc + ((CH.SLOT[S3].Finc * freq_LFO) >> LFO_FMS_LBITS);
            }
            else
            {
                CH.SLOT[S0].Fcnt += CH.SLOT[S0].Finc;
                CH.SLOT[S1].Fcnt += CH.SLOT[S1].Finc;
                CH.SLOT[S2].Fcnt += CH.SLOT[S2].Finc;
                CH.SLOT[S3].Fcnt += CH.SLOT[S3].Finc;
            }
        }

        private static void GET_CURRENT_ENV(ym2612_ YM2612, channel_ CH)
        {
            if ((CH.SLOT[S0].SEG & 4) != 0)
            {
                if ((YM2612.en0 = (int)ENV_TAB[(CH.SLOT[S0].Ecnt >> ENV_LBITS)] + CH.SLOT[S0].TLL) > ENV_MASK) YM2612.en0 = 0;
                else YM2612.en0 ^= ENV_MASK;
            }
            else YM2612.en0 = (int)ENV_TAB[(CH.SLOT[S0].Ecnt >> ENV_LBITS)] + CH.SLOT[S0].TLL;
            if ((CH.SLOT[S1].SEG & 4) != 0)
            {
                if ((YM2612.en1 = (int)ENV_TAB[(CH.SLOT[S1].Ecnt >> ENV_LBITS)] + CH.SLOT[S1].TLL) > ENV_MASK) YM2612.en1 = 0;
                else YM2612.en1 ^= ENV_MASK;
            }
            else YM2612.en1 = (int)ENV_TAB[(CH.SLOT[S1].Ecnt >> ENV_LBITS)] + CH.SLOT[S1].TLL;
            if ((CH.SLOT[S2].SEG & 4) != 0)
            {
                if ((YM2612.en2 = (int)ENV_TAB[(CH.SLOT[S2].Ecnt >> ENV_LBITS)] + CH.SLOT[S2].TLL) > ENV_MASK) YM2612.en2 = 0;
                else YM2612.en2 ^= ENV_MASK;
            }
            else YM2612.en2 = (int)ENV_TAB[(CH.SLOT[S2].Ecnt >> ENV_LBITS)] + CH.SLOT[S2].TLL;
            if ((CH.SLOT[S3].SEG & 4) != 0)
            {
                if ((YM2612.en3 = (int)ENV_TAB[(CH.SLOT[S3].Ecnt >> ENV_LBITS)] + CH.SLOT[S3].TLL) > ENV_MASK) YM2612.en3 = 0;
                else YM2612.en3 ^= ENV_MASK;
            }
            else YM2612.en3 = (int)ENV_TAB[(CH.SLOT[S3].Ecnt >> ENV_LBITS)] + CH.SLOT[S3].TLL;
        }

        private static void GET_CURRENT_ENV_LFO(ym2612_ YM2612, channel_ CH, int i, ref int env_LFO)
        {
            env_LFO = YM2612.LFO_ENV_UP[i];

            if ((CH.SLOT[S0].SEG & 4) != 0)
            {
                if ((YM2612.en0 = (int)ENV_TAB[(CH.SLOT[S0].Ecnt >> ENV_LBITS)] + CH.SLOT[S0].TLL) > ENV_MASK) YM2612.en0 = 0;
                else YM2612.en0 = (YM2612.en0 ^ ENV_MASK) + (env_LFO >> CH.SLOT[S0].AMS);
            }
            else YM2612.en0 = (int)ENV_TAB[(CH.SLOT[S0].Ecnt >> ENV_LBITS)] + CH.SLOT[S0].TLL + (env_LFO >> CH.SLOT[S0].AMS);
            if ((CH.SLOT[S1].SEG & 4) != 0)
            {
                if ((YM2612.en1 = (int)ENV_TAB[(CH.SLOT[S1].Ecnt >> ENV_LBITS)] + CH.SLOT[S1].TLL) > ENV_MASK) YM2612.en1 = 0;
                else YM2612.en1 = (YM2612.en1 ^ ENV_MASK) + (env_LFO >> CH.SLOT[S1].AMS);
            }
            else YM2612.en1 = (int)ENV_TAB[(CH.SLOT[S1].Ecnt >> ENV_LBITS)] + CH.SLOT[S1].TLL + (env_LFO >> CH.SLOT[S1].AMS);
            if ((CH.SLOT[S2].SEG & 4) != 0)
            {
                if ((YM2612.en2 = (int)ENV_TAB[(CH.SLOT[S2].Ecnt >> ENV_LBITS)] + CH.SLOT[S2].TLL) > ENV_MASK) YM2612.en2 = 0;
                else YM2612.en2 = (YM2612.en2 ^ ENV_MASK) + (env_LFO >> CH.SLOT[S2].AMS);
            }
            else YM2612.en2 = (int)ENV_TAB[(CH.SLOT[S2].Ecnt >> ENV_LBITS)] + CH.SLOT[S2].TLL + (env_LFO >> CH.SLOT[S2].AMS);
            if ((CH.SLOT[S3].SEG & 4) != 0)
            {
                if ((YM2612.en3 = (int)ENV_TAB[(CH.SLOT[S3].Ecnt >> ENV_LBITS)] + CH.SLOT[S3].TLL) > ENV_MASK) YM2612.en3 = 0;
                else YM2612.en3 = (YM2612.en3 ^ ENV_MASK) + (env_LFO >> CH.SLOT[S3].AMS);
            }
            else YM2612.en3 = (int)ENV_TAB[(CH.SLOT[S3].Ecnt >> ENV_LBITS)] + CH.SLOT[S3].TLL + (env_LFO >> CH.SLOT[S3].AMS);
        }

        private static void UPDATE_ENV(channel_ CH)
        {
            if ((CH.SLOT[S0].Ecnt += CH.SLOT[S0].Einc) >= CH.SLOT[S0].Ecmp)
                ENV_NEXT_EVENT[CH.SLOT[S0].Ecurp](CH.SLOT[S0]);

            if ((CH.SLOT[S1].Ecnt += CH.SLOT[S1].Einc) >= CH.SLOT[S1].Ecmp)
                ENV_NEXT_EVENT[CH.SLOT[S1].Ecurp](CH.SLOT[S1]);

            if ((CH.SLOT[S2].Ecnt += CH.SLOT[S2].Einc) >= CH.SLOT[S2].Ecmp)
                ENV_NEXT_EVENT[CH.SLOT[S2].Ecurp](CH.SLOT[S2]);

            if ((CH.SLOT[S3].Ecnt += CH.SLOT[S3].Einc) >= CH.SLOT[S3].Ecmp)
                ENV_NEXT_EVENT[CH.SLOT[S3].Ecurp](CH.SLOT[S3]);
        }

        private static void DO_LIMIT(channel_ CH)
        {
            if (CH.OUTd > LIMIT_CH_OUT) CH.OUTd = LIMIT_CH_OUT;
            else if (CH.OUTd < -LIMIT_CH_OUT) CH.OUTd = -LIMIT_CH_OUT;
        }

        private static void DO_FEEDBACK0(ym2612_ YM2612, channel_ CH)
        {
            YM2612.in0 += (CH.S0_OUT[0] + CH.S0_OUT[1]) >> CH.FB;
            CH.S0_OUT[0] = TL_TAB[SIN_TAB[(YM2612.in0 >> SIN_LBITS) & SIN_MASK] + YM2612.en0]; //SIN_TAB[(YM2612.in0 >> SIN_LBITS) & SIN_MASK][YM2612.en0];
        }

        private static void DO_FEEDBACK(ym2612_ YM2612, channel_ CH)
        {
            YM2612.in0 += (CH.S0_OUT[0] + CH.S0_OUT[1]) >> CH.FB;
            CH.S0_OUT[1] = CH.S0_OUT[0];
            CH.S0_OUT[0] = TL_TAB[SIN_TAB[(YM2612.in0 >> SIN_LBITS) & SIN_MASK] + YM2612.en0]; //SIN_TAB[(YM2612.in0 >> SIN_LBITS) & SIN_MASK][YM2612.en0];
        }

        private static void DO_FEEDBACK2(ym2612_ YM2612, channel_ CH)
        {
            YM2612.in0 += (CH.S0_OUT[0] + CH.S0_OUT[1]) >> CH.FB;
            CH.S0_OUT[1] = CH.S0_OUT[0] >> 2;
            CH.S0_OUT[0] = TL_TAB[SIN_TAB[(YM2612.in0 >> SIN_LBITS) & SIN_MASK] + YM2612.en0]; //SIN_TAB[(YM2612.in0 >> SIN_LBITS) & SIN_MASK][YM2612.en0];
        }

        private static void DO_FEEDBACK3(ym2612_ YM2612, channel_ CH)
        {
            YM2612.in0 += (CH.S0_OUT[0] + CH.S0_OUT[1] + CH.S0_OUT[2] + CH.S0_OUT[3]) >> CH.FB;
            CH.S0_OUT[3] = CH.S0_OUT[2] >> 1;
            CH.S0_OUT[2] = CH.S0_OUT[1] >> 1;
            CH.S0_OUT[1] = CH.S0_OUT[0] >> 1;
            CH.S0_OUT[0] = TL_TAB[SIN_TAB[(YM2612.in0 >> SIN_LBITS) & SIN_MASK] + YM2612.en0]; //SIN_TAB[(YM2612.in0 >> SIN_LBITS) & SIN_MASK][YM2612.en0];
        }

        private static void DO_ALGO_0(ym2612_ YM2612, channel_ CH)
        {
            DO_FEEDBACK(YM2612, CH);
            YM2612.in1 += CH.S0_OUT[1];
            YM2612.in2 += TL_TAB[SIN_TAB[(YM2612.in1 >> SIN_LBITS) & SIN_MASK] + YM2612.en1];// SIN_TAB[(YM2612.in1 >> SIN_LBITS) & SIN_MASK][YM2612.en1];
            YM2612.in3 += TL_TAB[SIN_TAB[(YM2612.in2 >> SIN_LBITS) & SIN_MASK] + YM2612.en2];//SIN_TAB[(YM2612.in2 >> SIN_LBITS) & SIN_MASK][YM2612.en2]
            CH.OUTd = (TL_TAB[SIN_TAB[(YM2612.in3 >> SIN_LBITS) & SIN_MASK] + YM2612.en3]) >> OUT_SHIFT; //(SIN_TAB[(YM2612.in3 >> SIN_LBITS) & SIN_MASK][YM2612.en3]) >> OUT_SHIFT;
        }

        private static void DO_ALGO_1(ym2612_ YM2612, channel_ CH)
        {
            DO_FEEDBACK(YM2612, CH);
            YM2612.in2 += CH.S0_OUT[1] + TL_TAB[SIN_TAB[(YM2612.in1 >> SIN_LBITS) & SIN_MASK] + YM2612.en1];// SIN_TAB[(YM2612.in1 >> SIN_LBITS) & SIN_MASK][YM2612.en1];
            YM2612.in3 += TL_TAB[SIN_TAB[(YM2612.in2 >> SIN_LBITS) & SIN_MASK] + YM2612.en2];//SIN_TAB[(YM2612.in2 >> SIN_LBITS) & SIN_MASK][YM2612.en2];
            CH.OUTd = (TL_TAB[SIN_TAB[(YM2612.in3 >> SIN_LBITS) & SIN_MASK] + YM2612.en3]) >> OUT_SHIFT;//(SIN_TAB[(YM2612.in3 >> SIN_LBITS) & SIN_MASK][YM2612.en3]) >> OUT_SHIFT;
        }

        private static void DO_ALGO_2(ym2612_ YM2612, channel_ CH)
        {
            DO_FEEDBACK(YM2612, CH);
            YM2612.in2 += TL_TAB[SIN_TAB[(YM2612.in1 >> SIN_LBITS) & SIN_MASK] + YM2612.en1];//SIN_TAB[(YM2612.in1 >> SIN_LBITS) & SIN_MASK][YM2612.en1];
            YM2612.in3 += CH.S0_OUT[1] + TL_TAB[SIN_TAB[(YM2612.in2 >> SIN_LBITS) & SIN_MASK] + YM2612.en2];//SIN_TAB[(YM2612.in2 >> SIN_LBITS) & SIN_MASK][YM2612.en2];
            CH.OUTd = (TL_TAB[SIN_TAB[(YM2612.in3 >> SIN_LBITS) & SIN_MASK] + YM2612.en3]) >> OUT_SHIFT;//(SIN_TAB[(YM2612.in3 >> SIN_LBITS) & SIN_MASK][YM2612.en3]) >> OUT_SHIFT;
        }

        private static void DO_ALGO_3(ym2612_ YM2612, channel_ CH)
        {
            DO_FEEDBACK(YM2612, CH);
            YM2612.in1 += CH.S0_OUT[1];
            YM2612.in3 += TL_TAB[SIN_TAB[(YM2612.in1 >> SIN_LBITS) & SIN_MASK] + YM2612.en1] +
                TL_TAB[SIN_TAB[(YM2612.in2 >> SIN_LBITS) & SIN_MASK] + YM2612.en2];//SIN_TAB[(YM2612.in1 >> SIN_LBITS) & SIN_MASK][YM2612.en1] + SIN_TAB[(YM2612.in2 >> SIN_LBITS) & SIN_MASK][YM2612.en2];
            CH.OUTd = (TL_TAB[SIN_TAB[(YM2612.in3 >> SIN_LBITS) & SIN_MASK] + YM2612.en3]) >> OUT_SHIFT;//(SIN_TAB[(YM2612.in3 >> SIN_LBITS) & SIN_MASK][YM2612.en3]) >> OUT_SHIFT;
        }

        private static void DO_ALGO_4(ym2612_ YM2612, channel_ CH)
        {
            DO_FEEDBACK(YM2612, CH);
            YM2612.in1 += CH.S0_OUT[1];
            YM2612.in3 += TL_TAB[SIN_TAB[(YM2612.in2 >> SIN_LBITS) & SIN_MASK] + YM2612.en2];//SIN_TAB[(YM2612.in2 >> SIN_LBITS) & SIN_MASK][YM2612.en2];
            CH.OUTd = ((int)TL_TAB[SIN_TAB[(YM2612.in3 >> SIN_LBITS) & SIN_MASK] + YM2612.en3] 
                + (int)TL_TAB[SIN_TAB[(YM2612.in1 >> SIN_LBITS) & SIN_MASK] + YM2612.en1]) >> OUT_SHIFT;//((int)SIN_TAB[(YM2612.in3 >> SIN_LBITS) & SIN_MASK][YM2612.en3] + (int)SIN_TAB[(YM2612.in1 >> SIN_LBITS) & SIN_MASK][YM2612.en1]) >> OUT_SHIFT;
            DO_LIMIT(CH);
        }

        private static void DO_ALGO_5(ym2612_ YM2612, channel_ CH)
        {
            DO_FEEDBACK(YM2612, CH);
            YM2612.in1 += CH.S0_OUT[1];
            YM2612.in2 += CH.S0_OUT[1];
            YM2612.in3 += CH.S0_OUT[1];
            CH.OUTd = ((int)TL_TAB[SIN_TAB[(YM2612.in3 >> SIN_LBITS) & SIN_MASK] + YM2612.en3] +
                (int)TL_TAB[SIN_TAB[(YM2612.in1 >> SIN_LBITS) & SIN_MASK] + YM2612.en1] +
                (int)TL_TAB[SIN_TAB[(YM2612.in2 >> SIN_LBITS) & SIN_MASK] + YM2612.en2]) >> OUT_SHIFT;
            //CH.OUTd = ((int)SIN_TAB[(YM2612.in3 >> SIN_LBITS) & SIN_MASK][YM2612.en3] + (int)SIN_TAB[(YM2612.in1 >> SIN_LBITS) & SIN_MASK][YM2612.en1] + (int)SIN_TAB[(YM2612.in2 >> SIN_LBITS) & SIN_MASK][YM2612.en2]) >> OUT_SHIFT;
            DO_LIMIT(CH);
        }

        private static void DO_ALGO_6(ym2612_ YM2612, channel_ CH)
        {
            DO_FEEDBACK(YM2612, CH);
            YM2612.in1 += CH.S0_OUT[1];
            CH.OUTd = ((int)TL_TAB[SIN_TAB[(YM2612.in3 >> SIN_LBITS) & SIN_MASK] + YM2612.en3] +
                (int)TL_TAB[SIN_TAB[(YM2612.in1 >> SIN_LBITS) & SIN_MASK] + YM2612.en1] +
                (int)TL_TAB[SIN_TAB[(YM2612.in2 >> SIN_LBITS) & SIN_MASK] + YM2612.en2]) >> OUT_SHIFT;
            //CH.OUTd = ((int)SIN_TAB[(YM2612.in3 >> SIN_LBITS) & SIN_MASK][YM2612.en3] + (int)SIN_TAB[(YM2612.in1 >> SIN_LBITS) & SIN_MASK][YM2612.en1] + (int)SIN_TAB[(YM2612.in2 >> SIN_LBITS) & SIN_MASK][YM2612.en2]) >> OUT_SHIFT;
            DO_LIMIT(CH);
        }

        private static void DO_ALGO_7(ym2612_ YM2612, channel_ CH)
        {
            DO_FEEDBACK(YM2612, CH);
            CH.OUTd = ((int)TL_TAB[SIN_TAB[(YM2612.in3 >> SIN_LBITS) & SIN_MASK] + YM2612.en3] + 
                (int)TL_TAB[SIN_TAB[(YM2612.in1 >> SIN_LBITS) & SIN_MASK] + YM2612.en1] + 
                (int)TL_TAB[SIN_TAB[(YM2612.in2 >> SIN_LBITS) & SIN_MASK] + YM2612.en2] + CH.S0_OUT[1]) >> OUT_SHIFT;
            //CH.OUTd = ((int)SIN_TAB[(YM2612.in3 >> SIN_LBITS) & SIN_MASK][YM2612.en3] + (int)SIN_TAB[(YM2612.in1 >> SIN_LBITS) & SIN_MASK][YM2612.en1] + (int)SIN_TAB[(YM2612.in2 >> SIN_LBITS) & SIN_MASK][YM2612.en2] + CH.S0_OUT[1]) >> OUT_SHIFT;
            DO_LIMIT(CH);
        }

        private static void DO_OUTPUT(channel_ CH, int[][] buf, int i)
        {
            buf[0][i] += CH.OUTd & CH.LEFT;
            buf[1][i] += CH.OUTd & CH.RIGHT;
            vol[0] = Math.Max(vol[0], Math.Abs(CH.OUTd & CH.LEFT));
            vol[1] = Math.Max(vol[1], Math.Abs(CH.OUTd & CH.RIGHT));
        }

        private static void DO_OUTPUT_INT0(ym2612_ YM2612, channel_ CH, int[][] buf, ref int i)
        {
            if (((int_cnt += (int)YM2612.Inter_Step) & 0x04000) != 0)
            {
                int_cnt &= 0x3FFF;
                buf[0][i] += CH.Old_OUTd & CH.LEFT;
                buf[1][i] += CH.Old_OUTd & CH.RIGHT;
                vol[0] = Math.Max(vol[0], Math.Abs(CH.Old_OUTd & CH.LEFT));
                vol[1] = Math.Max(vol[1], Math.Abs(CH.Old_OUTd & CH.RIGHT));
            }
            else i--;
        }

        private static void DO_OUTPUT_INT1(ym2612_ YM2612, channel_ CH, int[][] buf, ref int i)
        {
            CH.Old_OUTd = (CH.OUTd + CH.Old_OUTd) >> 1;
            if (((int_cnt += (int)YM2612.Inter_Step) & 0x04000) != 0)
            {
                int_cnt &= 0x3FFF;
                buf[0][i] += CH.Old_OUTd & CH.LEFT;
                buf[1][i] += CH.Old_OUTd & CH.RIGHT;
                vol[0] = Math.Max(vol[0], Math.Abs(CH.Old_OUTd & CH.LEFT));
                vol[1] = Math.Max(vol[1], Math.Abs(CH.Old_OUTd & CH.RIGHT));
            }
            else i--;
        }

        private static void DO_OUTPUT_INT2(ym2612_ YM2612, channel_ CH, int[][] buf, ref int i)
        {
            if (((int_cnt += (int)YM2612.Inter_Step) & 0x04000) != 0)
            {
                int_cnt &= 0x3FFF;
                CH.Old_OUTd = (CH.OUTd + CH.Old_OUTd) >> 1;
                buf[0][i] += CH.Old_OUTd & CH.LEFT;
                buf[1][i] += CH.Old_OUTd & CH.RIGHT;
                vol[0] = Math.Max(vol[0], Math.Abs(CH.Old_OUTd & CH.LEFT));
                vol[1] = Math.Max(vol[1], Math.Abs(CH.Old_OUTd & CH.RIGHT));
            }
            else i--;
            CH.Old_OUTd = CH.OUTd;
        }

        private static void DO_OUTPUT_INT(ym2612_ YM2612, channel_ CH, int[][] buf, ref int i)
        {
            if (((int_cnt += (int)YM2612.Inter_Step) & 0x04000) != 0)
            {
                int_cnt &= 0x3FFF;
                CH.Old_OUTd = (((int_cnt ^ 0x3FFF) * CH.OUTd) + 
                    (int_cnt * CH.Old_OUTd)) >> 14;
                buf[0][i] += CH.Old_OUTd & CH.LEFT;
                buf[1][i] += CH.Old_OUTd & CH.RIGHT;
                vol[0] = Math.Max(vol[0], Math.Abs(CH.Old_OUTd & CH.LEFT));
                vol[1] = Math.Max(vol[1], Math.Abs(CH.Old_OUTd & CH.RIGHT));
            }
            else i--;
            CH.Old_OUTd = CH.OUTd;
        }

        private static void Update_Chan_Algo0(ym2612_ YM2612, channel_ CH, int[][] buf, int lenght)
        {
            int i;

            if (CH.SLOT[S3].Ecnt == ENV_END) return;

#if DEBUG
            //fprintf(debug_file, "\n\nAlgo 0 len = %d\n\n", lenght);
#endif

            for (i = 0; i < lenght; i++)
            {
                GET_CURRENT_PHASE(YM2612, CH);
                UPDATE_PHASE(CH);
                GET_CURRENT_ENV(YM2612, CH);
                UPDATE_ENV(CH);
                DO_ALGO_0(YM2612, CH);
                DO_OUTPUT(CH, buf, i);
            }
        }

        private static void Update_Chan_Algo1(ym2612_ YM2612, channel_ CH, int[][] buf, int lenght)
        {
            int i;

            if (CH.SLOT[S3].Ecnt == ENV_END) return;

#if DEBUG
            //fprintf(debug_file, "\n\nAlgo 1 len = %d\n\n", lenght);
#endif

            for (i = 0; i < lenght; i++)
            {
                GET_CURRENT_PHASE(YM2612, CH);
                UPDATE_PHASE(CH);
                GET_CURRENT_ENV(YM2612, CH);
                UPDATE_ENV(CH);
                DO_ALGO_1(YM2612, CH);
                DO_OUTPUT(CH, buf, i);
            }
        }

        private static void Update_Chan_Algo2(ym2612_ YM2612, channel_ CH, int[][] buf, int lenght)
        {
            int i;

            if (CH.SLOT[S3].Ecnt == ENV_END) return;

#if DEBUG
            //fprintf(debug_file, "\n\nAlgo 2 len = %d\n\n", lenght);
#endif

            for (i = 0; i < lenght; i++)
            {
                GET_CURRENT_PHASE(YM2612, CH);
                UPDATE_PHASE(CH);
                GET_CURRENT_ENV(YM2612, CH);
                UPDATE_ENV(CH);
                DO_ALGO_2(YM2612, CH);
                DO_OUTPUT(CH, buf, i);
            }
        }

        private static void Update_Chan_Algo3(ym2612_ YM2612, channel_ CH, int[][] buf, int lenght)
        {
            int i;

            if (CH.SLOT[S3].Ecnt == ENV_END) return;

#if DEBUG
            //fprintf(debug_file, "\n\nAlgo 3 len = %d\n\n", lenght);
#endif

            for (i = 0; i < lenght; i++)
            {
                GET_CURRENT_PHASE(YM2612, CH);
                UPDATE_PHASE(CH);
                GET_CURRENT_ENV(YM2612, CH);
                UPDATE_ENV(CH);
                DO_ALGO_3(YM2612, CH);
                DO_OUTPUT(CH, buf, i);
            }
        }

        private static void Update_Chan_Algo4(ym2612_ YM2612, channel_ CH, int[][] buf, int lenght)
        {
            int i;

            if ((CH.SLOT[S1].Ecnt == ENV_END) && (CH.SLOT[S3].Ecnt == ENV_END)) return;

#if DEBUG
            //fprintf(debug_file, "\n\nAlgo 4 len = %d\n\n", lenght);
#endif

            for (i = 0; i < lenght; i++)
            {
                GET_CURRENT_PHASE(YM2612, CH);
                UPDATE_PHASE(CH);
                GET_CURRENT_ENV(YM2612, CH);
                UPDATE_ENV(CH);
                DO_ALGO_4(YM2612, CH);
                DO_OUTPUT(CH, buf, i);
            }
        }

        private static void Update_Chan_Algo5(ym2612_ YM2612, channel_ CH, int[][] buf, int lenght)
        {
            int i;

            if ((CH.SLOT[S1].Ecnt == ENV_END) && (CH.SLOT[S2].Ecnt == ENV_END) && (CH.SLOT[S3].Ecnt == ENV_END)) return;

#if DEBUG
            //fprintf(debug_file, "\n\nAlgo 5 len = %d\n\n", lenght);
#endif

            for (i = 0; i < lenght; i++)
            {
                GET_CURRENT_PHASE(YM2612, CH);
                UPDATE_PHASE(CH);
                GET_CURRENT_ENV(YM2612, CH);
                UPDATE_ENV(CH);
                DO_ALGO_5(YM2612, CH);
                DO_OUTPUT(CH, buf, i);
            }
        }

        private static void Update_Chan_Algo6(ym2612_ YM2612, channel_ CH, int[][] buf, int lenght)
        {
            int i;

            if ((CH.SLOT[S1].Ecnt == ENV_END) && (CH.SLOT[S2].Ecnt == ENV_END) && (CH.SLOT[S3].Ecnt == ENV_END)) return;

#if DEBUG
            //fprintf(debug_file, "\n\nAlgo 6 len = %d\n\n", lenght);
#endif

            for (i = 0; i < lenght; i++)
            {
                GET_CURRENT_PHASE(YM2612, CH);
                UPDATE_PHASE(CH);
                GET_CURRENT_ENV(YM2612, CH);
                UPDATE_ENV(CH);
                DO_ALGO_6(YM2612, CH);
                DO_OUTPUT(CH, buf, i);
            }
        }

        private static void Update_Chan_Algo7(ym2612_ YM2612, channel_ CH, int[][] buf, int lenght)
        {
            int i;

            if ((CH.SLOT[S0].Ecnt == ENV_END) && (CH.SLOT[S1].Ecnt == ENV_END) && (CH.SLOT[S2].Ecnt == ENV_END) && (CH.SLOT[S3].Ecnt == ENV_END)) return;

#if DEBUG
            //fprintf(debug_file, "\n\nAlgo 7 len = %d\n\n", lenght);
#endif

            for (i = 0; i < lenght; i++)
            {
                GET_CURRENT_PHASE(YM2612, CH);
                UPDATE_PHASE(CH);
                GET_CURRENT_ENV(YM2612, CH);
                UPDATE_ENV(CH);
                DO_ALGO_7(YM2612, CH);
                DO_OUTPUT(CH, buf, i);
            }
        }

        private static void Update_Chan_Algo0_LFO(ym2612_ YM2612, channel_ CH, int[][] buf, int lenght)
        {
            int i, env_LFO = 0, freq_LFO = 0;

            if (CH.SLOT[S3].Ecnt == ENV_END) return;

#if DEBUG
            //fprintf(debug_file, "\n\nAlgo 0 LFO len = %d\n\n", lenght);
#endif

            for (i = 0; i < lenght; i++)
            {
                GET_CURRENT_PHASE(YM2612, CH);
                UPDATE_PHASE_LFO(YM2612, CH, i, ref freq_LFO);
                GET_CURRENT_ENV_LFO(YM2612, CH, i, ref env_LFO);
                UPDATE_ENV(CH);
                DO_ALGO_0(YM2612, CH);
                DO_OUTPUT(CH, buf, i);
            }
        }

        private static void Update_Chan_Algo1_LFO(ym2612_ YM2612, channel_ CH, int[][] buf, int lenght)
        {
            int i, env_LFO = 0, freq_LFO = 0;

            if (CH.SLOT[S3].Ecnt == ENV_END) return;

#if DEBUG
            //fprintf(debug_file, "\n\nAlgo 1 LFO len = %d\n\n", lenght);
#endif

            for (i = 0; i < lenght; i++)
            {
                GET_CURRENT_PHASE(YM2612, CH);
                UPDATE_PHASE_LFO(YM2612, CH, i, ref freq_LFO);
                GET_CURRENT_ENV_LFO(YM2612, CH, i, ref env_LFO);
                UPDATE_ENV(CH);
                DO_ALGO_1(YM2612, CH);
                DO_OUTPUT(CH, buf, i);
            }
        }

        private static void Update_Chan_Algo2_LFO(ym2612_ YM2612, channel_ CH, int[][] buf, int lenght)
        {
            int i, env_LFO = 0, freq_LFO = 0;

            if (CH.SLOT[S3].Ecnt == ENV_END) return;

#if DEBUG
            //fprintf(debug_file, "\n\nAlgo 2 LFO len = %d\n\n", lenght);
#endif

            for (i = 0; i < lenght; i++)
            {
                GET_CURRENT_PHASE(YM2612, CH);
                UPDATE_PHASE_LFO(YM2612, CH, i, ref freq_LFO);
                GET_CURRENT_ENV_LFO(YM2612, CH, i, ref env_LFO);
                UPDATE_ENV(CH);
                DO_ALGO_2(YM2612, CH);
                DO_OUTPUT(CH, buf, i);
            }
        }

        private static void Update_Chan_Algo3_LFO(ym2612_ YM2612, channel_ CH, int[][] buf, int lenght)
        {
            int i, env_LFO = 0, freq_LFO = 0;

            if (CH.SLOT[S3].Ecnt == ENV_END) return;

#if DEBUG
            //fprintf(debug_file, "\n\nAlgo 3 LFO len = %d\n\n", lenght);
#endif

            for (i = 0; i < lenght; i++)
            {
                GET_CURRENT_PHASE(YM2612, CH);
                UPDATE_PHASE_LFO(YM2612, CH, i, ref freq_LFO);
                GET_CURRENT_ENV_LFO(YM2612, CH, i, ref env_LFO);
                UPDATE_ENV(CH);
                DO_ALGO_3(YM2612, CH);
                DO_OUTPUT(CH, buf, i);
            }
        }

        private static void Update_Chan_Algo4_LFO(ym2612_ YM2612, channel_ CH, int[][] buf, int lenght)
        {
            int i, env_LFO = 0, freq_LFO = 0;

            if ((CH.SLOT[S1].Ecnt == ENV_END) && (CH.SLOT[S3].Ecnt == ENV_END)) return;

#if DEBUG
            //fprintf(debug_file, "\n\nAlgo 4 LFO len = %d\n\n", lenght);
#endif

            for (i = 0; i < lenght; i++)
            {
                GET_CURRENT_PHASE(YM2612, CH);
                UPDATE_PHASE_LFO(YM2612, CH, i, ref freq_LFO);
                GET_CURRENT_ENV_LFO(YM2612, CH, i, ref env_LFO);
                UPDATE_ENV(CH);
                DO_ALGO_4(YM2612, CH);
                DO_OUTPUT(CH, buf, i);
            }
        }

        private static void Update_Chan_Algo5_LFO(ym2612_ YM2612, channel_ CH, int[][] buf, int lenght)
        {
            int i, env_LFO = 0, freq_LFO = 0;

            if ((CH.SLOT[S1].Ecnt == ENV_END) && (CH.SLOT[S2].Ecnt == ENV_END) && (CH.SLOT[S3].Ecnt == ENV_END)) return;

#if DEBUG
            //fprintf(debug_file, "\n\nAlgo 5 LFO len = %d\n\n", lenght);
#endif

            for (i = 0; i < lenght; i++)
            {
                GET_CURRENT_PHASE(YM2612, CH);
                UPDATE_PHASE_LFO(YM2612, CH, i, ref freq_LFO);
                GET_CURRENT_ENV_LFO(YM2612, CH, i, ref env_LFO);
                UPDATE_ENV(CH);
                DO_ALGO_5(YM2612, CH);
                DO_OUTPUT(CH, buf, i);
            }
        }

        private static void Update_Chan_Algo6_LFO(ym2612_ YM2612, channel_ CH, int[][] buf, int lenght)
        {
            int i, env_LFO = 0, freq_LFO = 0;

            if ((CH.SLOT[S1].Ecnt == ENV_END) && (CH.SLOT[S2].Ecnt == ENV_END) && (CH.SLOT[S3].Ecnt == ENV_END)) return;

#if DEBUG
            // fprintf(debug_file, "\n\nAlgo 6 LFO len = %d\n\n", lenght);
#endif

            for (i = 0; i < lenght; i++)
            {
                GET_CURRENT_PHASE(YM2612, CH);
                UPDATE_PHASE_LFO(YM2612, CH, i, ref freq_LFO);
                GET_CURRENT_ENV_LFO(YM2612, CH, i, ref env_LFO);
                UPDATE_ENV(CH);
                DO_ALGO_6(YM2612, CH);
                DO_OUTPUT(CH, buf, i);
            }
        }

        private static void Update_Chan_Algo7_LFO(ym2612_ YM2612, channel_ CH, int[][] buf, int lenght)
        {
            int i, env_LFO = 0, freq_LFO = 0;

            if ((CH.SLOT[S0].Ecnt == ENV_END) && (CH.SLOT[S1].Ecnt == ENV_END) && (CH.SLOT[S2].Ecnt == ENV_END) && (CH.SLOT[S3].Ecnt == ENV_END)) return;

#if DEBUG
            //fprintf(debug_file, "\n\nAlgo 7 LFO len = %d\n\n", lenght);
#endif

            for (i = 0; i < lenght; i++)
            {
                GET_CURRENT_PHASE(YM2612, CH);
                UPDATE_PHASE_LFO(YM2612, CH, i, ref freq_LFO);
                GET_CURRENT_ENV_LFO(YM2612, CH, i, ref env_LFO);
                UPDATE_ENV(CH);
                DO_ALGO_7(YM2612, CH);
                DO_OUTPUT(CH, buf, i);
            }
        }

        private static void Update_Chan_Algo0_Int(ym2612_ YM2612, channel_ CH, int[][] buf, int lenght)
        {
            int i;

            if (CH.SLOT[S3].Ecnt == ENV_END) return;

#if DEBUG
            //fprintf(debug_file, "\n\nAlgo 0 len = %d\n\n", lenght);
#endif

            int_cnt = (int)YM2612.Inter_Cnt;

            for (i = 0; i < lenght; i++)
            {
                GET_CURRENT_PHASE(YM2612, CH);
                UPDATE_PHASE(CH);
                GET_CURRENT_ENV(YM2612, CH);
                UPDATE_ENV(CH);
                DO_ALGO_0(YM2612, CH);
                DO_OUTPUT_INT(YM2612, CH, buf, ref i);
            }
        }

        private static void Update_Chan_Algo1_Int(ym2612_ YM2612, channel_ CH, int[][] buf, int lenght)
        {
            int i;

            if (CH.SLOT[S3].Ecnt == ENV_END) return;

#if DEBUG
            //fprintf(debug_file, "\n\nAlgo 1 len = %d\n\n", lenght);
#endif

            int_cnt = (int)YM2612.Inter_Cnt;

            for (i = 0; i < lenght; i++)
            {
                GET_CURRENT_PHASE(YM2612, CH);
                UPDATE_PHASE(CH);
                GET_CURRENT_ENV(YM2612, CH);
                UPDATE_ENV(CH);
                DO_ALGO_1(YM2612, CH);
                DO_OUTPUT_INT(YM2612, CH, buf, ref i);
            }
        }

        private static void Update_Chan_Algo2_Int(ym2612_ YM2612, channel_ CH, int[][] buf, int lenght)
        {
            int i;

            if (CH.SLOT[S3].Ecnt == ENV_END) return;

#if DEBUG
            // fprintf(debug_file, "\n\nAlgo 2 len = %d\n\n", lenght);
#endif

            int_cnt = (int)YM2612.Inter_Cnt;

            for (i = 0; i < lenght; i++)
            {
                GET_CURRENT_PHASE(YM2612, CH);
                UPDATE_PHASE(CH);
                GET_CURRENT_ENV(YM2612, CH);
                UPDATE_ENV(CH);
                DO_ALGO_2(YM2612, CH);
                DO_OUTPUT_INT(YM2612, CH, buf, ref i);
            }
        }

        private static void Update_Chan_Algo3_Int(ym2612_ YM2612, channel_ CH, int[][] buf, int lenght)
        {
            int i;

            if (CH.SLOT[S3].Ecnt == ENV_END) return;

#if DEBUG
            //fprintf(debug_file, "\n\nAlgo 3 len = %d\n\n", lenght);
#endif

            int_cnt = (int)YM2612.Inter_Cnt;

            for (i = 0; i < lenght; i++)
            {
                GET_CURRENT_PHASE(YM2612, CH);
                UPDATE_PHASE(CH);
                GET_CURRENT_ENV(YM2612, CH);
                UPDATE_ENV(CH);
                DO_ALGO_3(YM2612, CH);
                DO_OUTPUT_INT(YM2612, CH, buf, ref i);
            }
        }

        private static void Update_Chan_Algo4_Int(ym2612_ YM2612, channel_ CH, int[][] buf, int lenght)
        {
            int i;

            if ((CH.SLOT[S1].Ecnt == ENV_END) && (CH.SLOT[S3].Ecnt == ENV_END)) return;

#if DEBUG
            //fprintf(debug_file, "\n\nAlgo 4 len = %d\n\n", lenght);
#endif

            int_cnt = (int)YM2612.Inter_Cnt;

            for (i = 0; i < lenght; i++)
            {
                GET_CURRENT_PHASE(YM2612, CH);
                UPDATE_PHASE(CH);
                GET_CURRENT_ENV(YM2612, CH);
                UPDATE_ENV(CH);
                DO_ALGO_4(YM2612, CH);
                DO_OUTPUT_INT(YM2612, CH, buf, ref i);
            }
        }

        private static void Update_Chan_Algo5_Int(ym2612_ YM2612, channel_ CH, int[][] buf, int lenght)
        {
            int i;

            if ((CH.SLOT[S1].Ecnt == ENV_END) && (CH.SLOT[S2].Ecnt == ENV_END) && (CH.SLOT[S3].Ecnt == ENV_END)) return;

#if DEBUG
            //fprintf(debug_file, "\n\nAlgo 5 len = %d\n\n", lenght);
#endif

            int_cnt = (int)YM2612.Inter_Cnt;

            for (i = 0; i < lenght; i++)
            {
                GET_CURRENT_PHASE(YM2612, CH);
                UPDATE_PHASE(CH);
                GET_CURRENT_ENV(YM2612, CH);
                UPDATE_ENV(CH);
                DO_ALGO_5(YM2612, CH);
                DO_OUTPUT_INT(YM2612, CH, buf, ref i);
            }
        }

        private static void Update_Chan_Algo6_Int(ym2612_ YM2612, channel_ CH, int[][] buf, int lenght)
        {
            int i;

            if ((CH.SLOT[S1].Ecnt == ENV_END) && (CH.SLOT[S2].Ecnt == ENV_END) && (CH.SLOT[S3].Ecnt == ENV_END)) return;

#if DEBUG
            //fprintf(debug_file, "\n\nAlgo 6 len = %d\n\n", lenght);
#endif

            int_cnt = (int)YM2612.Inter_Cnt;

            for (i = 0; i < lenght; i++)
            {
                GET_CURRENT_PHASE(YM2612, CH);
                UPDATE_PHASE(CH);
                GET_CURRENT_ENV(YM2612, CH);
                UPDATE_ENV(CH);
                DO_ALGO_6(YM2612, CH);
                DO_OUTPUT_INT(YM2612, CH, buf, ref i);
            }
        }

        private static void Update_Chan_Algo7_Int(ym2612_ YM2612, channel_ CH, int[][] buf, int lenght)
        {
            int i;

            if ((CH.SLOT[S0].Ecnt == ENV_END) && (CH.SLOT[S1].Ecnt == ENV_END) && (CH.SLOT[S2].Ecnt == ENV_END) && (CH.SLOT[S3].Ecnt == ENV_END)) return;

#if DEBUG
            //fprintf(debug_file, "\n\nAlgo 7 len = %d\n\n", lenght);
#endif

            int_cnt = (int)YM2612.Inter_Cnt;

            for (i = 0; i < lenght; i++)
            {
                GET_CURRENT_PHASE(YM2612, CH);
                UPDATE_PHASE(CH);
                GET_CURRENT_ENV(YM2612, CH);
                UPDATE_ENV(CH);
                DO_ALGO_7(YM2612, CH);
                DO_OUTPUT_INT(YM2612, CH, buf, ref i);
            }
        }

        private static void Update_Chan_Algo0_LFO_Int(ym2612_ YM2612, channel_ CH, int[][] buf, int lenght)
        {
            int i, env_LFO = 0, freq_LFO = 0;

            if (CH.SLOT[S3].Ecnt == ENV_END) return;

#if DEBUG
            //fprintf(debug_file, "\n\nAlgo 0 LFO len = %d\n\n", lenght);
#endif

            int_cnt = (int)YM2612.Inter_Cnt;

            for (i = 0; i < lenght; i++)
            {
                GET_CURRENT_PHASE(YM2612, CH);
                UPDATE_PHASE_LFO(YM2612, CH, i, ref freq_LFO);
                GET_CURRENT_ENV_LFO(YM2612, CH, i, ref env_LFO);
                UPDATE_ENV(CH);
                DO_ALGO_0(YM2612, CH);
                DO_OUTPUT_INT(YM2612, CH, buf, ref i);
            }
        }

        private static void Update_Chan_Algo1_LFO_Int(ym2612_ YM2612, channel_ CH, int[][] buf, int lenght)
        {
            int i, env_LFO = 0, freq_LFO = 0;

            if (CH.SLOT[S3].Ecnt == ENV_END) return;

#if DEBUG
            //fprintf(debug_file, "\n\nAlgo 1 LFO len = %d\n\n", lenght);
#endif

            int_cnt = (int)YM2612.Inter_Cnt;

            for (i = 0; i < lenght; i++)
            {
                GET_CURRENT_PHASE(YM2612, CH);
                UPDATE_PHASE_LFO(YM2612, CH, i, ref freq_LFO);
                GET_CURRENT_ENV_LFO(YM2612, CH, i, ref env_LFO);
                UPDATE_ENV(CH);
                DO_ALGO_1(YM2612, CH);
                DO_OUTPUT_INT(YM2612, CH, buf, ref i);
            }
        }

        private static void Update_Chan_Algo2_LFO_Int(ym2612_ YM2612, channel_ CH, int[][] buf, int lenght)
        {
            int i, env_LFO = 0, freq_LFO = 0;

            if (CH.SLOT[S3].Ecnt == ENV_END) return;

#if DEBUG
            //fprintf(debug_file, "\n\nAlgo 2 LFO len = %d\n\n", lenght);
#endif

            int_cnt = (int)YM2612.Inter_Cnt;

            for (i = 0; i < lenght; i++)
            {
                GET_CURRENT_PHASE(YM2612, CH);
                UPDATE_PHASE_LFO(YM2612, CH, i, ref freq_LFO);
                GET_CURRENT_ENV_LFO(YM2612, CH, i, ref env_LFO);
                UPDATE_ENV(CH);
                DO_ALGO_2(YM2612, CH);
                DO_OUTPUT_INT(YM2612, CH, buf, ref i);
            }
        }

        private static void Update_Chan_Algo3_LFO_Int(ym2612_ YM2612, channel_ CH, int[][] buf, int lenght)
        {
            int i, env_LFO = 0, freq_LFO = 0;

            if (CH.SLOT[S3].Ecnt == ENV_END) return;

#if DEBUG
            //fprintf(debug_file, "\n\nAlgo 3 LFO len = %d\n\n", lenght);
#endif

            int_cnt = (int)YM2612.Inter_Cnt;

            for (i = 0; i < lenght; i++)
            {
                GET_CURRENT_PHASE(YM2612, CH);
                UPDATE_PHASE_LFO(YM2612, CH, i, ref freq_LFO);
                GET_CURRENT_ENV_LFO(YM2612, CH, i, ref env_LFO);
                UPDATE_ENV(CH);
                DO_ALGO_3(YM2612, CH);
                DO_OUTPUT_INT(YM2612, CH, buf, ref i);
            }
        }

        private static void Update_Chan_Algo4_LFO_Int(ym2612_ YM2612, channel_ CH, int[][] buf, int lenght)
        {
            int i, env_LFO = 0, freq_LFO = 0;

            if ((CH.SLOT[S1].Ecnt == ENV_END) && (CH.SLOT[S3].Ecnt == ENV_END)) return;

#if DEBUG
            //fprintf(debug_file, "\n\nAlgo 4 LFO len = %d\n\n", lenght);
#endif

            int_cnt = (int)YM2612.Inter_Cnt;

            for (i = 0; i < lenght; i++)
            {
                GET_CURRENT_PHASE(YM2612, CH);
                UPDATE_PHASE_LFO(YM2612, CH, i, ref freq_LFO);
                GET_CURRENT_ENV_LFO(YM2612, CH, i, ref env_LFO);
                UPDATE_ENV(CH);
                DO_ALGO_4(YM2612, CH);
                DO_OUTPUT_INT(YM2612, CH, buf, ref i);
            }
        }

        private static void Update_Chan_Algo5_LFO_Int(ym2612_ YM2612, channel_ CH, int[][] buf, int lenght)
        {
            int i, env_LFO = 0, freq_LFO = 0;

            if ((CH.SLOT[S1].Ecnt == ENV_END) && (CH.SLOT[S2].Ecnt == ENV_END) && (CH.SLOT[S3].Ecnt == ENV_END)) return;

#if DEBUG
            //fprintf(debug_file, "\n\nAlgo 5 LFO len = %d\n\n", lenght);
#endif

            int_cnt = (int)YM2612.Inter_Cnt;

            for (i = 0; i < lenght; i++)
            {
                GET_CURRENT_PHASE(YM2612, CH);
                UPDATE_PHASE_LFO(YM2612, CH, i, ref freq_LFO);
                GET_CURRENT_ENV_LFO(YM2612, CH, i, ref env_LFO);
                UPDATE_ENV(CH);
                DO_ALGO_5(YM2612, CH);
                DO_OUTPUT_INT(YM2612, CH, buf, ref i);
            }
        }

        private static void Update_Chan_Algo6_LFO_Int(ym2612_ YM2612, channel_ CH, int[][] buf, int lenght)
        {
            int i, env_LFO = 0, freq_LFO = 0;

            if ((CH.SLOT[S1].Ecnt == ENV_END) && (CH.SLOT[S2].Ecnt == ENV_END) && (CH.SLOT[S3].Ecnt == ENV_END)) return;

#if DEBUG
            //fprintf(debug_file, "\n\nAlgo 6 LFO len = %d\n\n", lenght);
#endif

            int_cnt = (int)YM2612.Inter_Cnt;

            for (i = 0; i < lenght; i++)
            {
                GET_CURRENT_PHASE(YM2612, CH);
                UPDATE_PHASE_LFO(YM2612, CH, i, ref freq_LFO);
                GET_CURRENT_ENV_LFO(YM2612, CH, i, ref env_LFO);
                UPDATE_ENV(CH);
                DO_ALGO_6(YM2612, CH);
                DO_OUTPUT_INT(YM2612, CH, buf, ref i);
            }
        }

        private static void Update_Chan_Algo7_LFO_Int(ym2612_ YM2612, channel_ CH, int[][] buf, int lenght)
        {
            int i, env_LFO = 0, freq_LFO = 0;

            if ((CH.SLOT[S0].Ecnt == ENV_END) && (CH.SLOT[S1].Ecnt == ENV_END) && (CH.SLOT[S2].Ecnt == ENV_END) && (CH.SLOT[S3].Ecnt == ENV_END)) return;

#if DEBUG
            //fprintf(debug_file, "\n\nAlgo 7 LFO len = %d\n\n", lenght);
#endif

            int_cnt = (int)YM2612.Inter_Cnt;

            for (i = 0; i < lenght; i++)
            {
                GET_CURRENT_PHASE(YM2612, CH);
                UPDATE_PHASE_LFO(YM2612, CH, i, ref freq_LFO);
                GET_CURRENT_ENV_LFO(YM2612, CH, i, ref env_LFO);
                UPDATE_ENV(CH);
                DO_ALGO_7(YM2612, CH);
                DO_OUTPUT_INT(YM2612, CH, buf, ref i);
            }
        }


        /***********************************************
         *            fonctions publiques              *
         ***********************************************/

        // Initialisation de l'駑ulateur YM2612
        private ym2612_ YM2612_Init(uint Clock, uint Rate, int Interpolation)
        {
            ym2612_ YM2612;
            int i, j;
            double x;

            if ((Rate == 0)) return null;
            if (Clock == 0)
            {
                Clock = DefaultFMClockValue;
            }

            YM2612 = new ym2612_();

#if DEBUG
            //if(debug_file == NULL)
            //{
            //  debug_file = fopen("ym2612.log", "w");
            //  fprintf(debug_file, "YM2612 logging :\n\n");
            //}
#endif

            YM2612.Clock = (int)Clock;
            YM2612.Rate = (int)Rate;

            // 144 = 12 * (prescale * 2) = 12 * 6 * 2
            // prescale set to 6 by default

            YM2612.Frequence = ((double)YM2612.Clock / (double)YM2612.Rate) / 144.0;
            YM2612.TimerBase = (int)(YM2612.Frequence * 4096.0);

            if ((Interpolation != 0) && (YM2612.Frequence > 1.0))
            {
                YM2612.Inter_Step = (uint)((1.0 / YM2612.Frequence) * (double)(0x4000));
                YM2612.Inter_Cnt = 0;

                // We recalculate rate and frequence after interpolation

                YM2612.Rate = YM2612.Clock / 144;
                YM2612.Frequence = 1.0;
            }
            else
            {
                YM2612.Inter_Step = 0x4000;
                YM2612.Inter_Cnt = 0;
            }

#if DEBUG
            //fprintf(debug_file, "YM2612 frequence = %g rate = %d  interp step = %.8X\n\n", YM2612->Frequence, YM2612->Rate, YM2612->Inter_Step);
#endif

            // Tableau TL :
            // [0     -  4095] = +output  [4095  - ...] = +output overflow (fill with 0)
            // [12288 - 16383] = -output  [16384 - ...] = -output overflow (fill with 0)

            for (i = 0; i < TL_LENGHT; i++)
            {
                if (i >= PG_CUT_OFF)  // YM2612 cut off sound after 78 dB (14 bits output ?)
                {
                    TL_TAB[TL_LENGHT + i] = TL_TAB[i] = 0;
                }
                else
                {
                    x = MAX_OUT;                // Max output
                    x /= Math.Pow(10, (ENV_STEP * i) / 20);      // Decibel -> Voltage

                    TL_TAB[i] = (int)x;
                    TL_TAB[TL_LENGHT + i] = -TL_TAB[i];
                }

#if DEBUG
                //fprintf(debug_file, "TL_TAB[%d] = %.8X    TL_TAB[%d] = %.8X\n", i, TL_TAB[i], TL_LENGHT + i, TL_TAB[TL_LENGHT + i]);
#endif
            }

#if DEBUG
            //fprintf(debug_file, "\n\n\n\n");
#endif

            // Tableau SIN :
            // SIN_TAB[x][y] = sin(x) * y;
            // x = phase and y = volume

            SIN_TAB[0] = SIN_TAB[SIN_LENGHT / 2] = (int)PG_CUT_OFF;// TL_TAB[(int)PG_CUT_OFF];

            for (i = 1; i <= SIN_LENGHT / 4; i++)
            {
                x = Math.Sin(2.0 * PI * (double)(i) / (double)(SIN_LENGHT));  // Sinus
                x = 20 * Math.Log10(1 / x);                    // convert to dB

                j = (int)(x / ENV_STEP);            // Get TL range

                if (j > PG_CUT_OFF) j = (int)PG_CUT_OFF;

                SIN_TAB[i] = SIN_TAB[(SIN_LENGHT / 2) - i] = j;// TL_TAB[j];
                SIN_TAB[(SIN_LENGHT / 2) + i] = SIN_TAB[SIN_LENGHT - i] = TL_LENGHT + j;// TL_TAB[TL_LENGHT + j];

#if DEBUG
                //fprintf(debug_file, "SIN[%d][0] = %.8X    SIN[%d][0] = %.8X    SIN[%d][0] = %.8X    SIN[%d][0] = %.8X\n", i, SIN_TAB[i][0], (SIN_LENGHT / 2) - i, SIN_TAB[(SIN_LENGHT / 2) - i][0], (SIN_LENGHT / 2) + i, SIN_TAB[(SIN_LENGHT / 2) + i][0], SIN_LENGHT - i, SIN_TAB[SIN_LENGHT - i][0]);
#endif
            }

#if DEBUG
            //fprintf(debug_file, "\n\n\n\n");
#endif

            // Tableau LFO (LFO wav) :

            for (i = 0; i < LFO_LENGHT; i++)
            {
                x = Math.Sin(2.0 * PI * (double)(i) / (double)(LFO_LENGHT));  // Sinus
                x += 1.0;
                x /= 2.0;          // positive only
                x *= 11.8 / ENV_STEP;    // ajusted to MAX enveloppe modulation

                LFO_ENV_TAB[i] = (int)x;

                x = Math.Sin(2.0 * PI * (double)(i) / (double)(LFO_LENGHT));  // Sinus
                x *= (double)((1 << (LFO_HBITS - 1)) - 1);

                LFO_FREQ_TAB[i] = (int)x;

#if DEBUG
                //fprintf(debug_file, "LFO[%d] = %.8X\n", i, LFO_ENV_TAB[i]);
#endif
            }

#if DEBUG
            //fprintf(debug_file, "\n\n\n\n");
#endif

            // Tableau Enveloppe :
            // ENV_TAB[0] -> ENV_TAB[ENV_LENGHT - 1]        = attack curve
            // ENV_TAB[ENV_LENGHT] -> ENV_TAB[2 * ENV_LENGHT - 1]  = decay curve

            for (i = 0; i < ENV_LENGHT; i++)
            {
                // Attack curve (x^8 - music level 2 Vectorman 2)
                x = Math.Pow(((double)((ENV_LENGHT - 1) - i) / (double)(ENV_LENGHT)), 8);
                x *= ENV_LENGHT;

                ENV_TAB[i] = (uint)x;

                // Decay curve (just linear)
                x = Math.Pow(((double)(i) / (double)(ENV_LENGHT)), 1);
                x *= ENV_LENGHT;

                ENV_TAB[ENV_LENGHT + i] = (uint)x;

#if DEBUG
                //fprintf(debug_file, "ATTACK[%d] = %d   DECAY[%d] = %d\n", i, ENV_TAB[i], i, ENV_TAB[ENV_LENGHT + i]);
#endif
            }

            ENV_TAB[ENV_END >> ENV_LBITS] = ENV_LENGHT - 1;    // for the stopped state

            // Tableau pour la conversion Attack -> Decay and Decay -> Attack

            for (i = 0, j = ENV_LENGHT - 1; i < ENV_LENGHT; i++)
            {
                while (j > 0 && (ENV_TAB[j] < (uint)i)) j--;

                DECAY_TO_ATTACK[i] = (uint)(j << ENV_LBITS);
            }

            // Tableau pour le Substain Level

            for (i = 0; i < 15; i++)
            {
                x = i * 3;          // 3 and not 6 (Mickey Mania first music for test)
                x /= ENV_STEP;

                j = (int)x;
                j <<= ENV_LBITS;

                SL_TAB[i] = (uint)(j + ENV_DECAY);
            }

            j = ENV_LENGHT - 1;        // special case : volume off
            j <<= ENV_LBITS;
            SL_TAB[15] = (uint)(j + ENV_DECAY);

            // Tableau Frequency Step

            for (i = 0; i < 2048; i++)
            {
                x = (double)(i) * YM2612.Frequence;

                //#if ((SIN_LBITS + SIN_HBITS - (21 - 7)) < 0)
                //    x /= (double) (1 << ((21 - 7) - SIN_LBITS - SIN_HBITS));
                //#else
                x *= (double)(1 << (SIN_LBITS + SIN_HBITS - (21 - 7)));
                //#endif

                x /= 2.0;  // because MUL = value * 2

                FINC_TAB[i] = (uint)x;
            }

            // Tableaux Attack & Decay Rate

            for (i = 0; i < 4; i++)
            {
                AR_TAB[i] = 0;
                DR_TAB[i] = 0;
            }

            for (i = 0; i < 60; i++)
            {
                x = YM2612.Frequence;

                x *= 1.0 + ((i & 3) * 0.25);          // bits 0-1 : x1.00, x1.25, x1.50, x1.75
                x *= (double)(1 << ((i >> 2)));        // bits 2-5 : shift bits (x2^0 - x2^15)
                x *= (double)(ENV_LENGHT << ENV_LBITS);    // on ajuste pour le tableau ENV_TAB

                AR_TAB[i + 4] = (uint)(x / AR_RATE);
                DR_TAB[i + 4] = (uint)(x / DR_RATE);
            }

            for (i = 64; i < 96; i++)
            {
                AR_TAB[i] = AR_TAB[63];
                DR_TAB[i] = DR_TAB[63];

                NULL_RATE[i - 64] = 0;
            }

            // Tableau Detune

            for (i = 0; i < 4; i++)
            {
                for (j = 0; j < 32; j++)
                {
                    //#if ((SIN_LBITS + SIN_HBITS - 21) < 0)
                    //      x = (double) DT_DEF_TAB[(i << 5) + j] * YM2612->Frequence / (double) (1 << (21 - SIN_LBITS - SIN_HBITS));
                    //#else
                    x = (double)DT_DEF_TAB[(i << 5) + j] * YM2612.Frequence * (double)(1 << (SIN_LBITS + SIN_HBITS - 21));
                    //#endif

                    DT_TAB[i + 0][j] = (uint)x;
                    DT_TAB[i + 4][j] = (uint)-x;
                }
            }

            // Tableau LFO

            j = (int)((YM2612.Rate * YM2612.Inter_Step) / 0x4000);

            LFO_INC_TAB[0] = (int)(3.98 * (double)(1 << (LFO_HBITS + LFO_LBITS)) / j);
            LFO_INC_TAB[1] = (int)(5.56 * (double)(1 << (LFO_HBITS + LFO_LBITS)) / j);
            LFO_INC_TAB[2] = (int)(6.02 * (double)(1 << (LFO_HBITS + LFO_LBITS)) / j);
            LFO_INC_TAB[3] = (int)(6.37 * (double)(1 << (LFO_HBITS + LFO_LBITS)) / j);
            LFO_INC_TAB[4] = (int)(6.88 * (double)(1 << (LFO_HBITS + LFO_LBITS)) / j);
            LFO_INC_TAB[5] = (int)(9.63 * (double)(1 << (LFO_HBITS + LFO_LBITS)) / j);
            LFO_INC_TAB[6] = (int)(48.1 * (double)(1 << (LFO_HBITS + LFO_LBITS)) / j);
            LFO_INC_TAB[7] = (int)(72.2 * (double)(1 << (LFO_HBITS + LFO_LBITS)) / j);

            YM2612_Reset(YM2612);

            return YM2612;
        }

        private int YM2612_End(ym2612_ YM2612)
        {
            //free(YM2612);

            //#if YM_DEBUG_LEVEL > 0
            //  if(debug_file) fclose(debug_file);
            //  debug_file = NULL;
            //#endif

            return 0;
        }


        private int YM2612_Reset(ym2612_ YM2612)
        {
            int i, j;

            //#if YM_DEBUG_LEVEL > 0
            //  fprintf(debug_file, "\n\nStarting reseting YM2612 ...\n\n");
            //#endif

            YM2612.LFOcnt = 0;
            YM2612.TimerA = 0;
            YM2612.TimerAL = 0;
            YM2612.TimerAcnt = 0;
            YM2612.TimerB = 0;
            YM2612.TimerBL = 0;
            YM2612.TimerBcnt = 0;
            YM2612.DAC = 0;
            YM2612.DACdata = 0;
            YM2612.dac_highpass = 0;

            YM2612.Status = 0;

            YM2612.OPNAadr = 0;
            YM2612.OPNBadr = 0;
            YM2612.Inter_Cnt = 0;

            for (i = 0; i < 6; i++)
            {
                YM2612.CHANNEL[i].Old_OUTd = 0;
                YM2612.CHANNEL[i].OUTd = 0;
                YM2612.CHANNEL[i].LEFT = -1;// 0xFFFFFFFF;
                YM2612.CHANNEL[i].RIGHT = -1;// 0xFFFFFFFF;
                YM2612.CHANNEL[i].ALGO = 0; ;
                YM2612.CHANNEL[i].FB = 31;
                YM2612.CHANNEL[i].FMS = 0;
                YM2612.CHANNEL[i].AMS = 0;

                for (j = 0; j < 4; j++)
                {
                    YM2612.CHANNEL[i].S0_OUT[j] = 0;
                    YM2612.CHANNEL[i].FNUM[j] = 0;
                    YM2612.CHANNEL[i].FOCT[j] = 0;
                    YM2612.CHANNEL[i].KC[j] = 0;

                    YM2612.CHANNEL[i].SLOT[j].DT = DT_TAB[0];
                    YM2612.CHANNEL[i].SLOT[j].Fcnt = 0;
                    YM2612.CHANNEL[i].SLOT[j].Finc = 0;
                    YM2612.CHANNEL[i].SLOT[j].Ecnt = ENV_END;    // Put it at the end of Decay phase...
                    YM2612.CHANNEL[i].SLOT[j].Einc = 0;
                    YM2612.CHANNEL[i].SLOT[j].Ecmp = 0;
                    YM2612.CHANNEL[i].SLOT[j].Ecurp = RELEASE;

                    YM2612.CHANNEL[i].SLOT[j].ChgEnM = 0;
                }
            }

            for (i = 0; i < 0x100; i++)
            {
                YM2612.REG[0][i] = -1;
                YM2612.REG[1][i] = -1;
            }

            for (i = 0xB6; i >= 0xB4; i--)
            {
                YM2612_Write(YM2612, 0, (byte)i);
                YM2612_Write(YM2612, 1, 0xC0);
                YM2612_Write(YM2612, 2, (byte)i);
                YM2612_Write(YM2612, 3, 0xC0);
            }

            for (i = 0xB2; i >= 0x22; i--)
            {
                YM2612_Write(YM2612, 0, (byte)i);
                YM2612_Write(YM2612, 1, 0);
                YM2612_Write(YM2612, 2, (byte)i);
                YM2612_Write(YM2612, 3, 0);
            }

            YM2612_Write(YM2612, 0, 0x2A);
            YM2612_Write(YM2612, 1, 0x80);

            //#if YM_DEBUG_LEVEL > 0
            //  fprintf(debug_file, "\n\nFinishing reseting YM2612 ...\n\n");
            //#endif

            return 0;
        }


        private int YM2612_Read(ym2612_ YM2612)
        {
            /*  static int cnt = 0;

              if(cnt++ == 50)
              {
                cnt = 0;
                return YM2612->Status;
              }
              else return YM2612->Status | 0x80;
            */
            return YM2612.Status;
        }

        private int YM2612_Write(ym2612_ YM2612, byte adr, byte data)
        {
            int d;

            data &= 0xFF;
            adr &= 0x03;

            switch (adr)
            {
                case 0:
                    YM2612.OPNAadr = data;
                    break;

                case 1:
                    // Trivial optimisation
                    if (YM2612.OPNAadr == 0x2A)
                    {
                        //YM2612.DACdata = data << DAC_SHIFT; //((int)data - 0x80) << DAC_SHIFT;
                        YM2612.DACdata = ((int)data - 0x80) << DAC_SHIFT;
                        return 0;
                    }

                    d = YM2612.OPNAadr & 0xF0;

                    if (d >= 0x30)
                    {
                        if (YM2612.REG[0][YM2612.OPNAadr] == data) return 2;
                        YM2612.REG[0][YM2612.OPNAadr] = data;

                        //				if (GYM_Dumping) Update_GYM_Dump(1, YM2612.OPNAadr, data);

                        if (d < 0xA0)    // SLOT
                        {
                            SLOT_SET(YM2612, YM2612.OPNAadr, data);
                        }
                        else        // CHANNEL
                        {
                            CHANNEL_SET(YM2612, YM2612.OPNAadr, data);
                        }
                    }
                    else          // YM2612
                    {
                        YM2612.REG[0][YM2612.OPNAadr] = data;

                        //				if ((GYM_Dumping) && ((YM2612.OPNAadr == 0x22) || (YM2612.OPNAadr == 0x27) || (YM2612.OPNAadr == 0x28))) Update_GYM_Dump(1, YM2612.OPNAadr, data);

                        YM_SET(YM2612, YM2612.OPNAadr, data);
                    }
                    break;

                case 2:
                    YM2612.OPNBadr = data;
                    break;

                case 3:
                    d = YM2612.OPNBadr & 0xF0;

                    if (d >= 0x30)
                    {
                        if (YM2612.REG[1][YM2612.OPNBadr] == data) return 2;
                        YM2612.REG[1][YM2612.OPNBadr] = data;

                        //				if (GYM_Dumping) Update_GYM_Dump(2, YM2612.OPNBadr, data);

                        if (d < 0xA0)    // SLOT
                        {
                            SLOT_SET(YM2612, YM2612.OPNBadr + 0x100, data);
                        }
                        else        // CHANNEL
                        {
                            CHANNEL_SET(YM2612, YM2612.OPNBadr + 0x100, data);
                        }
                    }
                    else return 1;
                    break;
            }

            return 0;
        }

        private int YM2612_GetMute(ym2612_ YM2612)
        {
            int i, result = 0;
            for (i = 0; i < 6; ++i)
            {
                result |= YM2612.CHANNEL[i].Mute << i;
            }
            result |= YM2612.DAC_Mute << 6;
            //result &= !(YM2612_Enable_SSGEG);
            return result;
        }

        private void YM2612_SetMute(ym2612_ YM2612, int val)
        {
            int i;
            for (i = 0; i < 6; ++i)
            {
                YM2612.CHANNEL[i].Mute = (val >> i) & 1;
            }
            YM2612.DAC_Mute = (val >> 6) & 1;
            //YM2612_Enable_SSGEG = !(val & 1);
        }

        private void YM2612_SetOptions(int Flags)
        {
            DAC_Highpass_Enable = (Flags >> 0) & 0x01;
            YM2612_Enable_SSGEG = (Flags >> 1) & 0x01;
        }

        private void YM2612_ClearBuffer(int[][] buffer, int length)
        {
            // the MAME core does this before updating,
            // but the Gens core does this before mixing
            int[] bufL, bufR;
            int i;

            bufL = buffer[0];
            bufR = buffer[1];

            for (i = 0; i < length; i++)
            {
                bufL[i] = 0x0000;
                bufR[i] = 0x0000;
            }
        }

        private void YM2612_Update(ym2612_ YM2612, int[][] buf, int length)
        {
            int i, j, algo_type;

#if DEBUG
            //fprintf(debug_file, "\n\nStarting generating sound...\n\n");
#endif

            // Mise ?jour des pas des compteurs-fr駲uences s'ils ont 騁?modifi駸

            if (YM2612.CHANNEL[0].SLOT[0].Finc == -1) CALC_FINC_CH(YM2612.CHANNEL[0]);
            if (YM2612.CHANNEL[1].SLOT[0].Finc == -1) CALC_FINC_CH(YM2612.CHANNEL[1]);
            if (YM2612.CHANNEL[2].SLOT[0].Finc == -1)
            {
                if ((YM2612.Mode & 0x40) > 0)
                {
                    CALC_FINC_SL(YM2612.CHANNEL[2].SLOT[S0], (int)FINC_TAB[YM2612.CHANNEL[2].FNUM[2]] >> (7 - YM2612.CHANNEL[2].FOCT[2]), YM2612.CHANNEL[2].KC[2]);
                    CALC_FINC_SL(YM2612.CHANNEL[2].SLOT[S1], (int)FINC_TAB[YM2612.CHANNEL[2].FNUM[3]] >> (7 - YM2612.CHANNEL[2].FOCT[3]), YM2612.CHANNEL[2].KC[3]);
                    CALC_FINC_SL(YM2612.CHANNEL[2].SLOT[S2], (int)FINC_TAB[YM2612.CHANNEL[2].FNUM[1]] >> (7 - YM2612.CHANNEL[2].FOCT[1]), YM2612.CHANNEL[2].KC[1]);
                    CALC_FINC_SL(YM2612.CHANNEL[2].SLOT[S3], (int)FINC_TAB[YM2612.CHANNEL[2].FNUM[0]] >> (7 - YM2612.CHANNEL[2].FOCT[0]), YM2612.CHANNEL[2].KC[0]);
                }
                else
                {
                    CALC_FINC_CH(YM2612.CHANNEL[2]);
                }
            }
            if (YM2612.CHANNEL[3].SLOT[0].Finc == -1) CALC_FINC_CH(YM2612.CHANNEL[3]);
            if (YM2612.CHANNEL[4].SLOT[0].Finc == -1) CALC_FINC_CH(YM2612.CHANNEL[4]);
            if (YM2612.CHANNEL[5].SLOT[0].Finc == -1) CALC_FINC_CH(YM2612.CHANNEL[5]);

            /*
              CALC_FINC_CH(&YM2612.CHANNEL[0]);
              CALC_FINC_CH(&YM2612.CHANNEL[1]);
              if(YM2612.Mode & 0x40)
              {
                CALC_FINC_SL(&(YM2612.CHANNEL[2].SLOT[0]), FINC_TAB[YM2612.CHANNEL[2].FNUM[2]] >> (7 - YM2612.CHANNEL[2].FOCT[2]), YM2612.CHANNEL[2].KC[2]);
                CALC_FINC_SL(&(YM2612.CHANNEL[2].SLOT[1]), FINC_TAB[YM2612.CHANNEL[2].FNUM[3]] >> (7 - YM2612.CHANNEL[2].FOCT[3]), YM2612.CHANNEL[2].KC[3]);
                CALC_FINC_SL(&(YM2612.CHANNEL[2].SLOT[2]), FINC_TAB[YM2612.CHANNEL[2].FNUM[1]] >> (7 - YM2612.CHANNEL[2].FOCT[1]), YM2612.CHANNEL[2].KC[1]);
                CALC_FINC_SL(&(YM2612.CHANNEL[2].SLOT[3]), FINC_TAB[YM2612.CHANNEL[2].FNUM[0]] >> (7 - YM2612.CHANNEL[2].FOCT[0]), YM2612.CHANNEL[2].KC[0]);
              }
              else
              {
                CALC_FINC_CH(&YM2612.CHANNEL[2]);
              }
              CALC_FINC_CH(&YM2612.CHANNEL[3]);
              CALC_FINC_CH(&YM2612.CHANNEL[4]);
              CALC_FINC_CH(&YM2612.CHANNEL[5]);
            */

            if ((YM2612.Inter_Step & 0x04000) != 0) algo_type = 0;
            else algo_type = 16;

            if ((YM2612.LFOinc) != 0)
            {
                // Precalcul LFO wav

                for (i = 0; i < length; i++)
                {
                    j = ((YM2612.LFOcnt += YM2612.LFOinc) >> LFO_LBITS) & LFO_MASK;

                    YM2612.LFO_ENV_UP[i] = LFO_ENV_TAB[j];
                    YM2612.LFO_FREQ_UP[i] = LFO_FREQ_TAB[j];

#if DEBUG
                    //fprintf(debug_file, "LFO_ENV_UP[%d] = %d   LFO_FREQ_UP[%d] = %d\n", i, YM2612.LFO_ENV_UP[i], i, YM2612.LFO_FREQ_UP[i]);
#endif
                }

                algo_type |= 8;
            }


            if (YM2612.CHANNEL[0].Mute == 0)
            {
                vol[0] = 0;
                vol[1] = 0;
                UPDATE_CHAN[YM2612.CHANNEL[0].ALGO + algo_type](YM2612, YM2612.CHANNEL[0], buf, length);
                YM2612.CHANNEL[0].fmVol[0] = vol[0];
                YM2612.CHANNEL[0].fmVol[1] = vol[1];
            }
            if (YM2612.CHANNEL[1].Mute == 0)
            {
                vol[0] = 0;
                vol[1] = 0;
                UPDATE_CHAN[YM2612.CHANNEL[1].ALGO + algo_type](YM2612, YM2612.CHANNEL[1], buf, length);
                YM2612.CHANNEL[1].fmVol[0] = vol[0];
                YM2612.CHANNEL[1].fmVol[1] = vol[1];
            }
            if (YM2612.CHANNEL[2].Mute == 0)
            {
                vol[0] = 0;
                vol[1] = 0;
                UPDATE_CHAN[YM2612.CHANNEL[2].ALGO + algo_type](YM2612, YM2612.CHANNEL[2], buf, length);
                YM2612.CHANNEL[2].fmVol[0] = vol[0];
                YM2612.CHANNEL[2].fmVol[1] = vol[1];
            }
            YM2612.CHANNEL[2].fmSlotVol[0] = (TL_TAB[SIN_TAB[(YM2612.in0 >> SIN_LBITS) & SIN_MASK] + YM2612.en0]) >> OUT_SHIFT;
            YM2612.CHANNEL[2].fmSlotVol[1] = (TL_TAB[SIN_TAB[(YM2612.in1 >> SIN_LBITS) & SIN_MASK] + YM2612.en1]) >> OUT_SHIFT;
            YM2612.CHANNEL[2].fmSlotVol[2] = (TL_TAB[SIN_TAB[(YM2612.in2 >> SIN_LBITS) & SIN_MASK] + YM2612.en2]) >> OUT_SHIFT;
            YM2612.CHANNEL[2].fmSlotVol[3] = (TL_TAB[SIN_TAB[(YM2612.in3 >> SIN_LBITS) & SIN_MASK] + YM2612.en3]) >> OUT_SHIFT;

            if (YM2612.CHANNEL[3].Mute == 0)
            {
                vol[0] = 0;
                vol[1] = 0;
                UPDATE_CHAN[YM2612.CHANNEL[3].ALGO + algo_type](YM2612, YM2612.CHANNEL[3], buf, length);
                YM2612.CHANNEL[3].fmVol[0] = vol[0];
                YM2612.CHANNEL[3].fmVol[1] = vol[1];
            }
            if (YM2612.CHANNEL[4].Mute == 0)
            {
                vol[0] = 0;
                vol[1] = 0;
                UPDATE_CHAN[YM2612.CHANNEL[4].ALGO + algo_type](YM2612, YM2612.CHANNEL[4], buf, length);
                YM2612.CHANNEL[4].fmVol[0] = vol[0];
                YM2612.CHANNEL[4].fmVol[1] = vol[1];
            }
            if (YM2612.CHANNEL[5].Mute == 0
                && (YM2612.DAC == 0))
            {
                vol[0] = 0;
                vol[1] = 0;
                UPDATE_CHAN[YM2612.CHANNEL[5].ALGO + algo_type](YM2612, YM2612.CHANNEL[5], buf, length);
                YM2612.CHANNEL[5].fmVol[0] = vol[0];
                YM2612.CHANNEL[5].fmVol[1] = vol[1];
            }

            YM2612.Inter_Cnt = (uint)int_cnt;

#if DEBUG
            //fprintf(debug_file, "\n\nFinishing generating sound...\n\n");
#endif

        }

        //enum highpass : uint
        //{
        //    fract = 15
        //, highpass_shift = 9
        //}; // higher values reduce highpass on DAC

        private void YM2612_DacAndTimers_Update(ym2612_ YM2612, int[][] buffer, int length)
        {
            int[] bufL, bufR;
            int i;

            if (YM2612.DAC != 0 && YM2612.DACdata != 0 && YM2612.DAC_Mute == 0)
            {

                bufL = buffer[0];
                bufR = buffer[1];

                for (i = 0; i < length; i++)
                {
                    //long dac = ((uint)YM2612.DACdata << highpass.fract) - YM2612.dac_highpass;
                    int dac = (int)(((uint)YM2612.DACdata << 15) - YM2612.dac_highpass);
                    if (DAC_Highpass_Enable != 0)    // else it's left at 0 and doesn't affect the sound
                        //YM2612.dac_highpass += dac >> highpass.shift;
                        YM2612.dac_highpass += dac >> 9;
                    //dac >>= highpass.fract;
                    dac >>= 15;
                    bufL[i] += (int)(dac & YM2612.CHANNEL[5].LEFT);
                    bufR[i] += (int)(dac & YM2612.CHANNEL[5].RIGHT);
                    YM2612.CHANNEL[5].fmVol[0] = (int)(dac & YM2612.CHANNEL[5].LEFT);
                    YM2612.CHANNEL[5].fmVol[1] = (int)(dac & YM2612.CHANNEL[5].RIGHT);
                }
            }

            i = YM2612.TimerBase * length;

            if ((YM2612.Mode & 1) != 0)              // Timer A ON ?
            {
                //    if((YM2612.TimerAcnt -= 14073) <= 0)    // 13879=NTSC (old: 14475=NTSC  14586=PAL)
                if ((YM2612.TimerAcnt -= i) <= 0)
                {
                    YM2612.Status |= (YM2612.Mode & 0x04) >> 2;
                    YM2612.TimerAcnt += YM2612.TimerAL;

#if DEBUG
                    //fprintf(debug_file, "Counter A overflow\n");
#endif

                    if ((YM2612.Mode & 0x80) != 0) CSM_Key_Control(YM2612);
                }
            }

            if ((YM2612.Mode & 2) != 0)              // Timer B ON ?
            {
                //    if((YM2612.TimerBcnt -= 14073) <= 0)    // 13879=NTSC (old: 14475=NTSC  14586=PAL)
                if ((YM2612.TimerBcnt -= i) <= 0)
                {
                    YM2612.Status |= (YM2612.Mode & 0x08) >> 2;
                    YM2612.TimerBcnt += YM2612.TimerBL;

#if DEBUG
                    //fprintf(debug_file, "Counter B overflow\n");
#endif
                }
            }
        }

        /* Gens */

        private void YM2612_Special_Update(ym2612_ YM2612)
        {
            /*
            if (YM_Len && YM2612_Enable)
            {
                YM2612_Update(YM_Buf, YM_Len);

                    YM_Buf[0] = Seg_L + Sound_Extrapol[VDP_Current_Line + 1][0];
                    YM_Buf[1] = Seg_R + Sound_Extrapol[VDP_Current_Line + 1][0];
                YM_Len = 0;
            }
            */
        }




        private const int MAX_CHIPS = 2;
        private const uint DefaultFMClockValue = 7670454;

        public ym2612_[] YM2612_Chip = new ym2612_[MAX_CHIPS] { null, null };

        public override string Name { get { return "YM2612"; } set { } }
        public override string ShortName { get { return "OPN2"; } set { } }

        public ym2612()
        {
            visVolume = new int[2][][] {
                new int[1][] { new int[2] { 0, 0 } }
                , new int[1][] { new int[2] { 0, 0 } }
            };
            //0..Main
        }


        public override uint Start(byte ChipID, uint clock)
        {
            ym2612_ ym2612 = YM2612_Init(DefaultFMClockValue, clock, 0);
            YM2612_Chip[ChipID] = ym2612;
            Reset(ChipID);

            return clock;
        }

        public override uint Start(byte ChipID, uint clock, uint FMClockValue, params object[] option)
        {
            ym2612_ ym2612 = YM2612_Init(FMClockValue, clock, (int)FMClockValue);
            YM2612_Chip[ChipID] = ym2612;
            Reset(ChipID);

            //動作オプション設定
            if (option != null && option is object[])
            {
                if (((object[])option).Length > 0)
                {
                    object[] ops = (object[])option;
                    if (ops[0] is int)
                    {
                        int optFlags = (int)ops[0];
                        //bit0:DAC_Highpass_Enable
                        //bit1:SSGEG_Enable
                        YM2612_SetOptions(optFlags & 0x3);
                    }
                }
            }

            return clock;
        }

        public override void Stop(byte ChipID)
        {
            YM2612_Chip[ChipID] = null;

            //free(YM2612);

            //#if YM_DEBUG_LEVEL > 0
            //  if(debug_file) fclose(debug_file);
            //  debug_file = NULL;
            //#endif

        }

        public override void Reset(byte ChipID)
        {
            ym2612_ YM2612 = YM2612_Chip[ChipID];
            if (YM2612 == null) return;

            YM2612_Reset(YM2612);
        }

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {
            ym2612_ YM2612 = YM2612_Chip[ChipID];
            if (YM2612 == null) return;

            YM2612_Update(YM2612, outputs, samples);
            YM2612_DacAndTimers_Update(YM2612, outputs, samples);

            visVolume[ChipID][0][0] = outputs[0][0];
            visVolume[ChipID][0][1] = outputs[1][0];
        }


        public override int Write(byte ChipID, int port, int adr, int data)
        {
            ym2612_ YM2612 = YM2612_Chip[ChipID];
            if (YM2612 == null) return 0;

            return YM2612_Write(YM2612, (byte)adr, (byte)data);
        }

        public void YM2612_SetMute(byte ChipID, int v)
        {
            ym2612_ YM2612 = YM2612_Chip[ChipID];
            if (YM2612 == null) return;

            YM2612_SetMute(YM2612, v);
        }




        //
        // ym2612.h
        //




        // Change it if you need to do long update
        //#define	MAX_UPDATE_LENGHT   4000
        private const int MAX_UPDATE_LENGHT = 0x100;
        //#define	MAX_UPDATE_LENGHT   1	// for in_vgm

        // Gens always uses 16 bits sound (in 32 bits buffer) and do the convertion later if needed.
        private const int OUTPUT_BITS = 15;
        // OUTPUT_BITS 15 is MAME's volume level
        //private const int DAC_SHIFT = (OUTPUT_BITS - 9);
        private const int DAC_SHIFT = (OUTPUT_BITS - 9);
        // DAC_SHIFT makes sure that FM and DAC volume has the same volume

        public class slot_
        {
            public uint[] DT;    // param鑼re detune
            public int MUL;    // param鑼re "multiple de fr駲uence"
            public int TL;     // Total Level = volume lorsque l'enveloppe est au plus haut
            public int TLL;    // Total Level ajusted
            public int SLL;    // Sustin Level (ajusted) = volume o� l'enveloppe termine sa premi鑽e phase de r馮ression
            public int KSR_S;  // Key Scale Rate Shift = facteur de prise en compte du KSL dans la variations de l'enveloppe
            public int KSR;    // Key Scale Rate = cette valeur est calcul馥 par rapport � la fr駲uence actuelle, elle va influer
                               // sur les diff駻ents param鑼res de l'enveloppe comme l'attaque, le decay ...  comme dans la r饌lit� !
            public int SEG;    // Type enveloppe SSG
            public uint[] AR;    // Attack Rate (table pointeur) = Taux d'attaque (AR[KSR])
            public int ARindex;
            public uint[] DR;    // Decay Rate (table pointeur) = Taux pour la r馮ression (DR[KSR])
            public int DRindex;
            public uint[] SR;    // Sustin Rate (table pointeur) = Taux pour le maintien (SR[KSR])
            public int SRindex;
            public uint[] RR;    // Release Rate (table pointeur) = Taux pour le rel稍hement (RR[KSR])
            public int RRindex;
            public int Fcnt;   // Frequency Count = compteur-fr駲uence pour d騁erminer l'amplitude actuelle (SIN[Finc >> 16])
            public int Finc;   // frequency step = pas d'incr駑entation du compteur-fr駲uence
                               // plus le pas est grand, plus la fr駲uence est a��u (ou haute)
            public int Ecurp;  // Envelope current phase = cette variable permet de savoir dans quelle phase
                               // de l'enveloppe on se trouve, par exemple phase d'attaque ou phase de maintenue ...
                               // en fonction de la valeur de cette variable, on va appeler une fonction permettant
                               // de mettre � jour l'enveloppe courante.
            public int Ecnt;   // Envelope counter = le compteur-enveloppe permet de savoir o� l'on se trouve dans l'enveloppe
            public int Einc;   // Envelope step courant
            public int Ecmp;   // Envelope counter limite pour la prochaine phase
            public int EincA;  // Envelope step for Attack = pas d'incr駑entation du compteur durant la phase d'attaque
                               // cette valeur est 馮al � AR[KSR]
            public int EincD;  // Envelope step for Decay = pas d'incr駑entation du compteur durant la phase de regression
                               // cette valeur est 馮al � DR[KSR]
            public int EincS;  // Envelope step for Sustain = pas d'incr駑entation du compteur durant la phase de maintenue
                               // cette valeur est 馮al � SR[KSR]
            public int EincR;  // Envelope step for Release = pas d'incr駑entation du compteur durant la phase de rel稍hement
                               // cette valeur est 馮al � RR[KSR]
            public int[] OUTp;  // pointeur of SLOT output = pointeur permettant de connecter la sortie de ce slot � l'entr馥
                                // d'un autre ou carrement � la sortie de la voie
            public int INd;    // input data of the slot = donn馥s en entr馥 du slot
            public int ChgEnM; // Change envelop mask.
            public int AMS;    // AMS depth level of this SLOT = degr� de modulation de l'amplitude par le LFO
            public int AMSon;  // AMS enable flag = drapeau d'activation de l'AMS
        }

        public class channel_
        {
            public int[] S0_OUT = new int[4];          // anciennes sorties slot 0 (pour le feed back)
            public int Old_OUTd;           // ancienne sortie de la voie (son brut)
            public int OUTd;               // sortie de la voie (son brut)
            public int LEFT;               // LEFT enable flag
            public int RIGHT;              // RIGHT enable flag
            public int ALGO;               // Algorythm = d騁ermine les connections entre les op駻ateurs
            public int FB;                 // shift count of self feed back = degr� de "Feed-Back" du SLOT 1 (il est son unique entr馥)
            public int FMS;                // Fr駲uency Modulation Sensitivity of channel = degr� de modulation de la fr駲uence sur la voie par le LFO
            public int AMS;                // Amplitude Modulation Sensitivity of channel = degr� de modulation de l'amplitude sur la voie par le LFO
            public int[] FNUM = new int[4];            // hauteur fr駲uence de la voie (+ 3 pour le mode sp馗ial)
            public int[] FOCT = new int[4];            // octave de la voie (+ 3 pour le mode sp馗ial)
            public int[] KC = new int[4];              // Key Code = valeur fonction de la fr駲uence (voir KSR pour les slots, KSR = KC >> KSR_S)
            public slot_[] SLOT = new slot_[4] { new slot_(), new slot_(), new slot_(), new slot_() }; // four slot.operators = les 4 slots de la voie
            public int FFlag;              // Frequency step recalculation flag
            public int Mute;         // Maxim: channel mute flag

            public int KeyOn;
            public int[] fmVol = new int[2];
            public int[] fmSlotVol = new int[4];

        }

        public class ym2612_
        {
            public int Clock;          // Horloge YM2612
            public int Rate;           // Sample Rate (11025/22050/44100)
            public int TimerBase;      // TimerBase calculation
            public int Status;         // YM2612 Status (timer overflow)
            public int OPNAadr;        // addresse pour l'馗riture dans l'OPN A (propre � l'駑ulateur)
            public int OPNBadr;        // addresse pour l'馗riture dans l'OPN B (propre � l'駑ulateur)
            public int LFOcnt;         // LFO counter = compteur-fr駲uence pour le LFO
            public int LFOinc;         // LFO step counter = pas d'incr駑entation du compteur-fr駲uence du LFO
                                       // plus le pas est grand, plus la fr駲uence est grande
            public int TimerA;         // timerA limit = valeur jusqu'� laquelle le timer A doit compter
            public int TimerAL;
            public int TimerAcnt;      // timerA counter = valeur courante du Timer A
            public int TimerB;         // timerB limit = valeur jusqu'� laquelle le timer B doit compter
            public int TimerBL;
            public int TimerBcnt;      // timerB counter = valeur courante du Timer B
            public int Mode;           // Mode actuel des voie 3 et 6 (normal / sp馗ial)
            public int DAC;            // DAC enabled flag
            public int DACdata;        // DAC data
            public int dac_highpass;
            public double Frequence;   // Fr駲uence de base, se calcul par rapport � l'horlage et au sample rate
            public uint Inter_Cnt;         // Interpolation Counter
            public uint Inter_Step;        // Interpolation Step
            public channel_[] CHANNEL = new channel_[6] { new channel_(), new channel_(), new channel_(), new channel_(), new channel_(), new channel_() }; // Les 6 voies du YM2612
            public int[][] REG = new int[2][] { new int[0x100], new int[0x100] }; // Sauvegardes des valeurs de tout les registres, c'est facultatif
                                                                                  // cela nous rend le d饕uggage plus facile
            private const int MAX_UPDATE_LENGHT = 0x100;

            public int[] LFO_ENV_UP = new int[MAX_UPDATE_LENGHT];      // Temporary calculated LFO AMS (adjusted for 11.8 dB) *
            public int[] LFO_FREQ_UP = new int[MAX_UPDATE_LENGHT];      // Temporary calculated LFO FMS *

            public int in0, in1, in2, in3;            // current phase calculation *
            public int en0, en1, en2, en3;            // current enveloppe calculation *

            public int DAC_Mute;
        }

    }
}