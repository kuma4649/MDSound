
using System;

namespace MDSound
{
    public class c140 : Instrument
    {

        private const int MAX_VOICE = 24;

        //private struct voice_registers
        //{
        //    public byte volume_right;
        //    public byte volume_left;
        //    public byte frequency_msb;
        //    public byte frequency_lsb;
        //    public byte bank;
        //    public byte mode;
        //    public byte start_msb;
        //    public byte start_lsb;
        //    public byte end_msb;
        //    public byte end_lsb;
        //    public byte loop_msb;
        //    public byte loop_lsb;
        //    public byte[] reserved;//=new byte[4];
        //}

        public class VOICE
        {
            public long ptoffset;
            public long pos;
            public long key;
            //--work
            public long lastdt;
            public long prevdt;
            public long dltdt;
            //--reg
            public long rvol;
            public long lvol;
            public long frequency;
            public long bank;
            public long mode;

            public long sample_start;
            public long sample_end;
            public long sample_loop;
            public byte Muted;
        }

        public class c140_state
        {
            public int sample_rate;
            //sound_stream *stream;
            public C140_TYPE banking_type;
            /* internal buffers */
            public int[] mixer_buffer_left;
            public int[] mixer_buffer_right;

            public int baserate;
            public uint pRomSize;
            public byte[] pRom;
            public byte[] REG;//[0x200];

            public int[] pcmtbl;//[8];        //2000.06.26 CAB

            public VOICE[] voi;//[MAX_VOICE];
        }

        private const int MAX_CHIPS = 0x02;

        public c140_state[] C140Data = new c140_state[MAX_CHIPS] { new c140_state(), new c140_state() };

        private static void init_voice(VOICE v)
        {
            v.key = 0;
            v.ptoffset = 0;
            v.rvol = 0;
            v.lvol = 0;
            v.frequency = 0;
            v.bank = 0;
            v.mode = 0;
            v.sample_start = 0;
            v.sample_end = 0;
            v.sample_loop = 0;
        }

        public c140()
        {
            for (int i = 0; i < MAX_CHIPS; i++)
            {
                C140Data[i].REG = new byte[0x200];
                C140Data[i].pcmtbl = new int[8];
                C140Data[i].voi = new VOICE[MAX_VOICE];
                for (int j = 0; j < MAX_VOICE; j++)
                {
                    C140Data[i].voi[j] = new VOICE();
                    init_voice(C140Data[i].voi[j]);
                }
            }

            visVolume = new int[2][][] {
                new int[1][] { new int[2] { 0, 0 } }
                , new int[1][] { new int[2] { 0, 0 } }
            };
            //0..Main

        }

        /* C140.h */

        //#pragma once

        //public void c140_update(byte ChipID, int[][] outputs, int samples)
        //{

        //    //c140_state *info = (c140_state *)param;
        //    c140_state info = C140Data[ChipID];
        //    int i, j;

        //    int rvol, lvol;
        //    int dt;
        //    int sdt;
        //    int st, ed, sz;

        //    long pSampleData;
        //    int frequency, delta, offset, pos;
        //    int cnt, voicecnt;
        //    int lastdt, prevdt, dltdt;
        //    float pbase = (float)info.baserate * 2.0f / (float)info.sample_rate;
        //    //System.Console.Write("pbase={0:f6} info->baserate={1:d} info->sample_rate={2:d} \n", pbase, info.baserate, info.sample_rate);

        //    int[] lmix, rmix;

        //    if (samples > info.sample_rate) samples = info.sample_rate;

        //    /* zap the contents of the mixer buffer */
        //    for (int ind = 0; ind < samples; ind++)
        //    {
        //        info.mixer_buffer_left[ind] = 0;
        //        info.mixer_buffer_right[ind] = 0;
        //    }
        //    if (info.pRom == null)
        //        return;

        //    //System.Console.WriteLine("c140_update");

        //    /* get the number of voices to update */
        //    voicecnt = (info.banking_type == C140_TYPE.ASIC219) ? 16 : 24;

        //    //--- audio update
        //    for (i = 0; i < voicecnt; i++)
        //    {
        //        VOICE v = info.voi[i];
        //        //voice_registers vreg = (voice_registers)info.REG[i * 16];
        //        int vreg = i * 16;

        //        if (v.key != 0 && v.Muted == 0)
        //        {
        //            System.Console.Write("voicecnt={0:d} ", i);
        //            frequency = info.REG[vreg + 2] * 256 + info.REG[vreg + 3];

        //            /* Abort voice if no frequency value set */
        //            if (frequency == 0) continue;
        //            System.Console.Write("frequency={0:d} ", frequency);

        //            /* Delta =  frequency * ((8MHz/374)*2 / sample rate) */
        //            delta = (int)((float)frequency * pbase);
        //            System.Console.Write("delta={0:d} ", delta);

        //            /* Calculate left/right channel volumes */
        //            lvol = (info.REG[vreg + 1] * 32) / MAX_VOICE; //32ch -> 24ch
        //            rvol = (info.REG[vreg + 0] * 32) / MAX_VOICE;
        //            System.Console.Write("MAX_VOICE={0} lvol={1} vreg->volume_left={2} ", MAX_VOICE, lvol, info.REG[vreg + 1]);
        //            System.Console.Write("rvol={0} vreg->volume_right={1} ", rvol, info.REG[vreg + 0]);

        //            /* Set mixer outputs base pointers */
        //            lmix = info.mixer_buffer_left;
        //            rmix = info.mixer_buffer_right;

        //            /* Retrieve sample start/end and calculate size */
        //            st = (int)v.sample_start;
        //            ed = (int)v.sample_end;
        //            sz = ed - st;
        //            System.Console.Write("st={0} ed={1} ", st, ed);

        //            /* Retrieve base pointer to the sample data */
        //            //pSampleData=(signed char*)((FPTR)info->pRom + find_sample(info, st, v->bank, i));
        //            //pSampleData = info.pRom[find_sample(info, st, v.bank, i)];
        //            pSampleData = find_sample(info, st, v.bank, i);
        //            System.Console.Write("find_sample={0} ", find_sample(info, st, v.bank, i));

        //            /* Fetch back previous data pointers */
        //            offset = (int)v.ptoffset;
        //            pos = (int)v.pos;
        //            lastdt = (int)v.lastdt;
        //            prevdt = (int)v.prevdt;
        //            dltdt = (int)v.dltdt;
        //            System.Console.Write("offset={0} pos={1} lastdt={2} prevdt={3} dltdt={4} ", offset, pos, lastdt, prevdt, dltdt);

        //            System.Console.Write("v->mode={0} info->banking_type={1} ", v.mode, (int)info.banking_type);
        //            /* Switch on data type - compressed PCM is only for C140 */
        //            if ((v.mode & 8) != 0 && (info.banking_type != C140_TYPE.ASIC219))
        //            {
        //                //compressed PCM (maybe correct...)
        //                /* Loop for enough to fill sample buffer as requested */
        //                for (j = 0; j < samples; j++)
        //                {
        //                    offset += delta;
        //                    cnt = (offset >> 16) & 0x7fff;
        //                    offset &= 0xffff;
        //                    pos += cnt;
        //                    System.Console.Write("offset={0} cnt={1} pos={2} ", offset, cnt, pos);
        //                    //for(;cnt>0;cnt--)
        //                    {
        //                        /* Check for the end of the sample */
        //                        if (pos >= sz)
        //                        {
        //                            /* Check if its a looping sample, either stop or loop */
        //                            if ((v.mode & 0x10) != 0)
        //                            {
        //                                pos = (int)(v.sample_loop - st);
        //                            }
        //                            else
        //                            {
        //                                v.key = 0;
        //                                break;
        //                            }
        //                        }

        //                        /* Read the chosen sample byte */
        //                        dt = info.pRom[pSampleData + pos];
        //                        System.Console.Write("dt={0} ", dt);

        //                        /* decompress to 13bit range */        //2000.06.26 CAB
        //                        sdt = dt >> 3;              //signed
        //                        System.Console.Write("sdt={0} ", sdt);
        //                        if (sdt < 0) sdt = (sdt << (dt & 7)) - info.pcmtbl[dt & 7];
        //                        else sdt = (sdt << (dt & 7)) + info.pcmtbl[dt & 7];
        //                        System.Console.Write("sdt={0} info->pcmtbl[dt&7]={1} ", sdt, info.pcmtbl[dt & 7]);

        //                        prevdt = lastdt;
        //                        lastdt = sdt;
        //                        dltdt = (lastdt - prevdt);
        //                        System.Console.Write("prevdt={0} lastdt={1} dltdt={2} ", prevdt, lastdt, dltdt);
        //                    }

        //                    /* Caclulate the sample value */
        //                    dt = ((dltdt * offset) >> 16) + prevdt;
        //                    System.Console.Write("dt={0} ", dt);

        //                    /* Write the data to the sample buffers */
        //                    lmix[j] += (dt * lvol) >> (5 + 5);
        //                    rmix[j] += (dt * rvol) >> (5 + 5);
        //                    System.Console.Write("(dt*lvol)>>(5+5)={0} ", (dt * lvol) >> (5 + 5));
        //                    System.Console.Write("(dt*rvol)>>(5+5)={0} ", (dt * rvol) >> (5 + 5));
        //                }
        //            }
        //            else
        //            {
        //                /* linear 8bit signed PCM */
        //                for (j = 0; j < samples; j++)
        //                {
        //                    offset += delta;
        //                    cnt = (offset >> 16) & 0x7fff;
        //                    offset &= 0xffff;
        //                    pos += cnt;
        //                    System.Console.Write("linear offset={0} cnt={1} pos={2} ", offset, cnt, pos);
        //                    /* Check for the end of the sample */
        //                    if (pos >= sz)
        //                    {
        //                        /* Check if its a looping sample, either stop or loop */
        //                        if ((v.mode & 0x10) != 0)
        //                        {
        //                            pos = (int)(v.sample_loop - st);
        //                        }
        //                        else
        //                        {
        //                            v.key = 0;
        //                            break;
        //                        }
        //                    }

        //                    if (cnt != 0)
        //                    {
        //                        prevdt = lastdt;

        //                        if (info.banking_type == C140_TYPE.ASIC219)
        //                        {
        //                            //lastdt = pSampleData[BYTE_XOR_BE(pos)];
        //                            lastdt = info.pRom[pSampleData + (pos ^ 0x01)];

        //                            // Sign + magnitude format
        //                            if ((v.mode & 0x01) != 0 && ((lastdt & 0x80) != 0))
        //                                lastdt = -(lastdt & 0x7f);

        //                            // Sign flip
        //                            if ((v.mode & 0x40) != 0)
        //                                lastdt = -lastdt;
        //                        }
        //                        else
        //                        {
        //                            lastdt = ((info.pRom[pSampleData + pos] & 0x80) != 0) ? (info.pRom[pSampleData + pos] - 256) : info.pRom[pSampleData + pos];
        //                        }

        //                        dltdt = (lastdt - prevdt);
        //                        System.Console.Write("prevdt={0} lastdt={1} dltdt={2} ", prevdt, lastdt, dltdt);
        //                    }

        //                    /* Caclulate the sample value */
        //                    dt = ((dltdt * offset) >> 16) + prevdt;
        //                    System.Console.Write("dt={0} ", dt);

        //                    /* Write the data to the sample buffers */
        //                    lmix[j] += (dt * lvol) >> 5;
        //                    rmix[j] += (dt * rvol) >> 5;
        //                    System.Console.Write("(dt*lvol)>>5={0} ", (dt * lvol) >> 5);
        //                    System.Console.Write("(dt*rvol)>>5={0} ", (dt * rvol) >> 5);
        //                }
        //            }

        //            /* Save positional data for next callback */
        //            v.ptoffset = offset;
        //            v.pos = pos;
        //            v.lastdt = lastdt;
        //            v.prevdt = prevdt;
        //            v.dltdt = dltdt;
        //            System.Console.Write("\n");
        //        }
        //    }

        //    /* render to MAME's stream buffer */
        //    lmix = info.mixer_buffer_left;
        //    rmix = info.mixer_buffer_right;
        //    {
        //        int[] dest1 = outputs[0];
        //        int[] dest2 = outputs[1];
        //        for (i = 0; i < samples; i++)
        //        {
        //            //*dest1++ = limit(8*(*lmix++));
        //            //*dest2++ = limit(8*(*rmix++));
        //            dest1[i] = 8 * lmix[i];
        //            dest2[i] = 8 * rmix[i];
        //        }
        //    }
        //}

        //READ8_DEVICE_HANDLER( c140_r );
        //WRITE8_DEVICE_HANDLER( c140_w );

        public byte c140_r(byte ChipID, uint offset)
        {
            //c140_state *info = get_safe_token(device);
            c140_state info = C140Data[ChipID];
            offset &= 0x1ff;
            return info.REG[offset];
        }

        /*
        find_sample: compute the actual address of a sample given it's
        address and banking registers, as well as the board type.

        I suspect in "real life" this works like the Sega MultiPCM where the banking
        is done by a small PAL or GAL external to the sound chip, which can be switched
        per-game or at least per-PCB revision as addressing range needs grow.
        */

        private static int[] asic219banks = new int[4] { 0x1f7, 0x1f1, 0x1f3, 0x1f5 };

        private static long find_sample(c140_state info, long adrs, long bank, int voice)
        {
            adrs = (bank << 16) + adrs;

            switch (info.banking_type)
            {
                case C140_TYPE.SYSTEM2:
                    // System 2 banking
                    return ((adrs & 0x200000) >> 2) | (adrs & 0x7ffff);

                case C140_TYPE.SYSTEM21:
                    // System 21 banking.
                    // similar to System 2's.
                    return ((adrs & 0x300000) >> 1) | (adrs & 0x7ffff);

                case C140_TYPE.ASIC219:
                    // ASIC219's banking is fairly simple
                    return (long)((info.REG[asic219banks[voice / 4]] & 0x3) * 0x20000) | adrs;
            }

            return 0;
        }

        private void c140_w(byte ChipID, uint offset, byte data)
        {
            //c140_state *info = get_safe_token(device);
            c140_state info = C140Data[ChipID];
            //info->stream->update();

            offset &= 0x1ff;

            // mirror the bank registers on the 219, fixes bkrtmaq (and probably xday2 based on notes in the HLE)
            if ((offset >= 0x1f8) && (info.banking_type == C140_TYPE.ASIC219))
            {
                offset -= 8;
            }

            info.REG[offset] = data;
            if (offset < 0x180)
            {
                VOICE v = info.voi[offset >> 4];

                if ((offset & 0xf) == 0x5)
                {
                    if ((data & 0x80) != 0)
                    {
                        //voice_registers vreg = (voice_registers)info.REG[offset & 0x1f0];
                        int vreg = (int)(offset & 0x1f0);
                        v.key = 1;
                        v.ptoffset = 0;
                        v.pos = 0;
                        v.lastdt = 0;
                        v.prevdt = 0;
                        v.dltdt = 0;
                        v.bank = info.REG[vreg + 4];// vreg.bank;
                        v.mode = data;

                        // on the 219 asic, addresses are in words
                        if (info.banking_type == C140_TYPE.ASIC219)
                        {
                            v.sample_loop = ((info.REG[vreg + 10] * 256) | info.REG[vreg + 11]) * 2;
                            v.sample_start = ((info.REG[vreg + 6] * 256) | info.REG[vreg + 7]) * 2;
                            v.sample_end = ((info.REG[vreg + 8] * 256) | info.REG[vreg + 9]) * 2;

                            //#if 0
                            //logerror("219: play v %d mode %02x start %x loop %x end %x\n",
                            //	offset>>4, v->mode,
                            //	find_sample(info, v->sample_start, v->bank, offset>>4),
                            //	find_sample(info, v->sample_loop, v->bank, offset>>4),
                            //	find_sample(info, v->sample_end, v->bank, offset>>4));
                            //#endif
                        }
                        else
                        {
                            v.sample_loop = (info.REG[vreg + 10] << 8) | info.REG[vreg + 11];
                            v.sample_start = (info.REG[vreg + 6] << 8) | info.REG[vreg + 7];
                            v.sample_end = (info.REG[vreg + 8] << 8) | info.REG[vreg + 9];
                        }
                    }
                    else
                    {
                        v.key = 0;
                    }
                }
            }
        }

        //void c140_set_base(device_t *device, void *base);

        public void c140_set_base(byte ChipID, byte[] Base)
        {

            //c140_state *info = get_safe_token(device);
            c140_state info = C140Data[ChipID];
            info.pRom = Base;
        }

        public enum C140_TYPE : int
        {
            SYSTEM2 = 0,
            SYSTEM21 = 1,
            ASIC219 = 2
        }

        /*typedef struct _c140_interface c140_interface;
        struct _c140_interface {
            int banking_type;
        };*/

        public void c140_write_rom(byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData)
        {
            c140_state info = C140Data[ChipID];

            if (info.pRomSize != ROMSize)
            {
                info.pRom = new byte[ROMSize];
                info.pRomSize = ROMSize;
                for (int i = 0; i < ROMSize; i++) info.pRom[i] = 0xff;
                //memset(info->pRom, 0xFF, ROMSize);
            }
            if (DataStart > ROMSize)
                return;
            if (DataStart + DataLength > ROMSize)
                DataLength = ROMSize - DataStart;

            for (int i = 0; i < DataLength; i++) info.pRom[i + DataStart] = ROMData[i];
            //memcpy((INT8*)info->pRom + DataStart, ROMData, DataLength);

            return;
        }

        public void c140_write_rom2(byte ChipID, uint ROMSize, uint DataStart, uint DataLength, byte[] ROMData, uint SrcStartAdr)
        {
            c140_state info = C140Data[ChipID];

            if (info.pRomSize != ROMSize)
            {
                info.pRom = new byte[ROMSize];
                info.pRomSize = ROMSize;
                for (int i = 0; i < ROMSize; i++) info.pRom[i] = 0xff;
                //memset(info->pRom, 0xFF, ROMSize);
            }
            if (DataStart > ROMSize)
                return;
            if (DataStart + DataLength > ROMSize)
                DataLength = ROMSize - DataStart;

            for (int i = 0; i < DataLength; i++) info.pRom[i + DataStart] = ROMData[i + SrcStartAdr];
            //memcpy((INT8*)info->pRom + DataStart, ROMData, DataLength);

            //System.Console.WriteLine("c140_write_rom2:{0}:{1}:{2}:{3}:{4}", ChipID, ROMSize, DataStart, DataLength, SrcStartAdr);
            return;
        }

        public void c140_set_mute_mask(byte ChipID, uint MuteMask)
        {
            c140_state info = C140Data[ChipID];
            byte CurChn;

            for (CurChn = 0; CurChn < MAX_VOICE; CurChn++)
                info.voi[CurChn].Muted = (byte)((MuteMask >> CurChn) & 0x01);

            return;
        }

        //DECLARE_LEGACY_SOUND_DEVICE(C140, c140);


        public override string Name { get { return "C140"; } set { } }
        public override string ShortName { get { return "C140"; } set { } }

        //private int debugCnt = 0;

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {

            //c140_state *info = (c140_state *)param;
            c140_state info = C140Data[ChipID];
            int i, j;

            int rvol, lvol;
            int dt;
            int sdt;
            int st, ed, sz;

            long pSampleData;
            int frequency, delta, offset, pos;
            int cnt, voicecnt;
            int lastdt, prevdt, dltdt;
            float pbase = (float)info.baserate * 2.0f / (float)info.sample_rate;

            int[] lmix, rmix;

            if (samples > info.sample_rate) samples = info.sample_rate;

            /* zap the contents of the mixer buffer */
            for (int ind = 0; ind < samples; ind++)
            {
                info.mixer_buffer_left[ind] = 0;
                info.mixer_buffer_right[ind] = 0;
            }
            if (info.pRom == null)
                return;

            /* get the number of voices to update */
            voicecnt = (info.banking_type == C140_TYPE.ASIC219) ? 16 : 24;

            //--- audio update
            for (i = 0; i < voicecnt; i++)
            {
                VOICE v = info.voi[i];
                //voice_registers vreg = (voice_registers)info.REG[i * 16];
                int vreg = i * 16;

                if (v.key == 0 || v.Muted != 0) continue;
                frequency = (info.REG[vreg + 2] << 8) | info.REG[vreg + 3];

                /* Abort voice if no frequency value set */
                if (frequency == 0) continue;

                /* Delta =  frequency * ((8MHz/374)*2 / sample rate) */
                delta = (int)(frequency * pbase);

                /* Calculate left/right channel volumes */
                lvol = (info.REG[vreg + 1] << 5) / MAX_VOICE; //32ch -> 24ch
                rvol = (info.REG[vreg + 0] << 5) / MAX_VOICE;

                /* Set mixer outputs base pointers */
                lmix = info.mixer_buffer_left;
                rmix = info.mixer_buffer_right;

                /* Retrieve sample start/end and calculate size */
                st = (int)v.sample_start;
                ed = (int)v.sample_end;
                sz = ed - st;

                /* Retrieve base pointer to the sample data */
                //pSampleData=(signed char*)((FPTR)info->pRom + find_sample(info, st, v->bank, i));
                //pSampleData = info.pRom[find_sample(info, st, v.bank, i)];
                pSampleData = find_sample(info, st, v.bank, i);

                /* Fetch back previous data pointers */
                offset = (int)v.ptoffset;
                pos = (int)v.pos;
                lastdt = (int)v.lastdt;
                prevdt = (int)v.prevdt;
                dltdt = (int)v.dltdt;

                /* Switch on data type - compressed PCM is only for C140 */
                if ((v.mode & 8) != 0 && (info.banking_type != C140_TYPE.ASIC219))
                {
                    //compressed PCM (maybe correct...)
                    /* Loop for enough to fill sample buffer as requested */
                    for (j = 0; j < samples; j++)
                    {
                        offset += delta;
                        cnt = (offset >> 16) & 0x7fff;
                        offset &= 0xffff;
                        pos += cnt;
                        /* Check for the end of the sample */
                        if (pos >= sz)
                        {
                            //Console.WriteLine("c140 pos[{0:x}]",pos);
                            //debugCnt = 20;
                            /* Check if its a looping sample, either stop or loop */
                            if ((v.mode & 0x10) != 0)
                            {
                                pos = (int)(v.sample_loop - st);
                            }
                            else
                            {
                                v.key = 0;
                                break;
                            }
                        }

                        /* Read the chosen sample byte */
                        dt = (sbyte)info.pRom[pSampleData + pos];

                        /* decompress to 13bit range */        //2000.06.26 CAB
                        sdt = dt >> 3;              //signed
                        if (sdt < 0) sdt = (sdt << (dt & 7)) - info.pcmtbl[dt & 7];
                        else sdt = (sdt << (dt & 7)) + info.pcmtbl[dt & 7];

                        prevdt = lastdt;
                        lastdt = sdt;
                        dltdt = (lastdt - prevdt);

                        /* Caclulate the sample value */
                        dt = ((dltdt * offset) >> 16) + prevdt;

                        /* Write the data to the sample buffers */
                        lmix[j] += (dt * lvol) >> (5 + 5);
                        rmix[j] += (dt * rvol) >> (5 + 5);
                    }
                }
                else
                {
                    /* linear 8bit signed PCM */
                    for (j = 0; j < samples; j++)
                    {
                        offset += delta;
                        cnt = (offset >> 16) & 0x7fff;
                        offset &= 0xffff;
                        pos += cnt;
                        /* Check for the end of the sample */
                        if (pos >= sz)
                        {
                            //Console.WriteLine("c140 pos[{0:x}]", pos);
                            //debugCnt = 20;
                            /* Check if its a looping sample, either stop or loop */
                            if ((v.mode & 0x10) != 0)
                            {
                                pos = (int)(v.sample_loop - st);
                            }
                            else
                            {
                                v.key = 0;
                                break;
                            }
                        }

                        if (cnt != 0)
                        {
                            prevdt = lastdt;

                            if (info.banking_type == C140_TYPE.ASIC219)
                            {
                                lastdt = (sbyte)info.pRom[pSampleData + (pos ^ 0x01)];

                                // Sign + magnitude format
                                if ((v.mode & 0x01) != 0 && ((lastdt & 0x80) != 0))
                                {
                                    lastdt = -(lastdt & 0x7f);
                                }
                                // Sign flip
                                if ((v.mode & 0x40) != 0)
                                    lastdt = -lastdt;

                            }
                            else
                            {
                                lastdt = (sbyte)info.pRom[pSampleData + pos];
                            }

                            dltdt = (lastdt - prevdt);
                        }

                        /* Caclulate the sample value */
                        dt = ((dltdt * offset) >> 16) + prevdt;

                        /* Write the data to the sample buffers */
                        lmix[j] += (dt * lvol) >> 5;
                        rmix[j] += (dt * rvol) >> 5;
                    }
                }

                /* Save positional data for next callback */
                v.ptoffset = offset;
                v.pos = pos;
                v.lastdt = lastdt;
                v.prevdt = prevdt;
                v.dltdt = dltdt;
            }

            /* render to MAME's stream buffer */
            lmix = info.mixer_buffer_left;
            rmix = info.mixer_buffer_right;
            {
                int[] dest1 = outputs[0];
                int[] dest2 = outputs[1];
                for (i = 0; i < samples; i++)
                {
                    dest1[i] = lmix[i] << 3;
                    dest2[i] = rmix[i] << 3;
                    //if (debugCnt > 0)
                    //{
                    //    debugCnt--;
                    //    Console.WriteLine("{0:x}  {0:d}", lmix[i]);
                    //}
                }
            }

            visVolume[ChipID][0][0] = outputs[0][0];
            visVolume[ChipID][0][1] = outputs[1][0];
        }

        public override uint Start(byte ChipID, uint clock)
        {
            return Start(ChipID, 44100, clock, C140_TYPE.SYSTEM2);
        }

        public override uint Start(byte ChipID, uint Samplingrate, uint clock, params object[] option)
        {
            //const c140_interface *intf = (const c140_interface *)device->static_config();
            //c140_state *info = get_safe_token(device);
            c140_state info;
            int i;

            if (ChipID >= MAX_CHIPS)
                return 0;

            info = C140Data[ChipID];

            //info->sample_rate=info->baserate=device->clock();
            if (clock < 1000000)
                info.baserate = (int)clock;
            else
                info.baserate = (int)clock / 384;   // based on MAME's notes on Namco System II
            info.sample_rate = info.baserate;
            if ((CHIP_SAMPLING_MODE == 0x01 && info.sample_rate < CHIP_SAMPLE_RATE) ||
                CHIP_SAMPLING_MODE == 0x02)
                info.sample_rate = CHIP_SAMPLE_RATE;
            if (info.sample_rate >= 0x1000000) // limit to 16 MHz sample rate (32 MB buffer)
                return 0;

            //info->banking_type = intf->banking_type;
            info.banking_type = (C140_TYPE)option[0];

            //info->stream = device->machine().sound().stream_alloc(*device,0,2,info->sample_rate,info,update_stereo);

            //info->pRom=*device->region();
            info.pRomSize = 0x00;
            info.pRom = null;

            /* make decompress pcm table */        //2000.06.26 CAB
            {
                int segbase = 0;
                for (i = 0; i < 8; i++)
                {
                    info.pcmtbl[i] = segbase;  //segment base value
                    segbase += 16 << i;
                }
            }

            // done at device_reset
            /*memset(info->REG,0,sizeof(info->REG));
            {
                int i;
                for(i=0;i<MAX_VOICE;i++) init_voice( &info->voi[i] );
            }*/

            for (i = 0; i < MAX_VOICE; i++) init_voice(info.voi[i]);

            /* allocate a pair of buffers to mix into - 1 second's worth should be more than enough */
            //info->mixer_buffer_left = auto_alloc_array(device->machine(), INT16, 2 * info->sample_rate);
            info.mixer_buffer_left = new int[info.sample_rate];// (INT16*)malloc(sizeof(INT16) * 2 * info->sample_rate);
            info.mixer_buffer_right = new int[info.sample_rate];// info->mixer_buffer_left + info->sample_rate;

            for (i = 0; i < MAX_VOICE; i++)
                info.voi[i].Muted = 0x00;

            return (uint)info.sample_rate;
        }

        public override void Stop(byte ChipID)
        {
            c140_state info = C140Data[ChipID];

            //free(info->pRom);
            info.pRom = null;
            //free(info->mixer_buffer_left);
            info.mixer_buffer_left = null;
            info.mixer_buffer_right = null;

            //return;
        }

        public override void Reset(byte ChipID)
        {
            //c140_state info = C140Data[ChipID];

            //free(info->pRom);
            //info.pRom = null;
            //free(info->mixer_buffer_left);
            //info.mixer_buffer_left = null;
            //info.mixer_buffer_right = null;

            //return;
        }

        public override int Write(byte ChipID, int port, int adr, int data)
        {
            c140_w(ChipID, (uint)adr, (byte)data);
            return 0;
        }
    }
}
