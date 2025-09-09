using MDSound.np;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace MDSound
{
    //
    // このCS4231エミュレータは、以下の資料,ソースを参考,移植しています。
    //
    // みゅあっぷ/iv  主にPLAY4
    // CS4231Aのデータシート
    //

    public class CS4231 : Instrument
    {
        private Chip[] chip = new Chip[] { new Chip(), new Chip() };
        private class Chip
        {
            public byte indexAddress;
            public byte indexData;
            public byte status;
            public byte PIOData;
            public byte[] reg = new byte[32];
            public byte dmaInt;
            public int renderingFreq;
            public short[] sound=new short[2];
            public short[][] sound2;
            public int[] xtal = new int[] { 24_576_000, 16_934_400 };
            public int[] divTbl = new int[] { 3072, 1536, 896, 768, 448, 384, 512, 2560 };
            public DMA dma = new DMA();
            public byte IMR = 0;
            public double step = 0;
            public double counter = 0;

        }

        public override string Name { get { return "CS4231"; } set { } }
        public override string ShortName { get { return "CS4231"; } set { } }


        public CS4231()
        {
            visVolume = new int[2][][] {
                new int[1][] { new int[2] { 0, 0 } }
                , new int[1][] { new int[2] { 0, 0 } }
            };
            //0..Main
        }

        public override void Reset(byte ChipID)
        {
        }

        public override uint Start(byte ChipID, uint clock)
        {
            throw new ArgumentOutOfRangeException();
        }

        public override uint Start(byte ChipID, uint samplingClock, uint chipClock, params object[] option)
        {
            try
            {
                chip[ChipID] = new Chip();
                chip[ChipID].renderingFreq = (int)samplingClock;
                chip[ChipID].sound2 = new short[][]{
                new short[2], new short[2], new short[2], new short[2], new short[2],
                new short[2], new short[2], new short[2], new short[2], new short[2]};
                if (option != null && option.Length > 1)
                {
                    chip[ChipID].dma.fifoBuf = (byte[])option[0];
                    //chip[ChipID].dma.int0bEnt = (Action)option[1];
                }

                return samplingClock;
            }
            catch (Exception ex)
            {
                throw new ArgumentException();
            }
        }

        public override void Stop(byte ChipID)
        {
        }

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {
            Chip c = chip[ChipID & 1];

            for (int i = 0; i < samples; i++)
            {
                c.step = ((double)c.xtal[c.reg[8] & 1] / c.divTbl[(c.reg[8] & 0xe) >> 1]) / c.renderingFreq;

                c.counter += c.step;
                short rcnt = 0;
                while (c.counter >= 1.0)
                {
                    c.counter -= 1.0;
                    Exec(c, rcnt);
                    rcnt++;
                    //ch[0]._stat = 1;
                }
                synth(c, rcnt);

                outputs[0][i] = c.sound[0];
                outputs[1][i] = c.sound[1];
            }

            visVolume[ChipID][0][0] = outputs[0][0];
            visVolume[ChipID][0][1] = outputs[1][0];
        }

        private bool latch = true;
        public override int Write(byte ChipID, int port, int adr, int data)
        {
            Chip c = chip[ChipID & 1];
            if (port == 0)
            {
                switch (adr)
                {
                    case 0:
                        c.indexAddress = (byte)data;
                        break;
                    case 1:
                        c.indexData = (byte)data;
                        c.reg[c.indexAddress & 0x1f] = c.indexData;
                        break;
                    case 2:
                        //status = dat;
                        ResetINTFlag();
                        break;
                    case 3:
                        c.PIOData = (byte)data;
                        break;
                    case 4:
                        c.dmaInt = (byte)data;
                        break;
                }
            }
            else if (port == 1)
            {
                switch (adr)
                {
                    case 0x2:
                        c.IMR = (byte)data;
                        break;
                    case 0x5:
                        c.dma.WriteReg(5, (byte)data);
                        break;
                    case 0x7:
                        c.dma.WriteReg(7, (byte)data);
                        break;
                }

            }
            else if (port == 2)
            {
                if (adr >= 200)
                {
                    switch(adr)
                    {
                        case 200:
                            if (latch) c.dma.freq2 = (byte)data;
                            else c.dma.freq2 |= (ushort)((byte)data << 8);
                            latch = !latch;
                            return 0;
                        case 201:
                            if (latch) c.dma.jump1_ = (byte)data;
                            else c.dma.jump1_ |= (ushort)((byte)data << 8);
                            latch = !latch;
                            return 0;
                        case 202:
                            c.dma.jump2_ = (byte)data;
                            return 0;
                    }
                }

                int ch = adr / (5 * 2);
                int prm= (adr % (5 * 2)) / 2;
                int ind = adr % 2;
                byte dat= (byte)data;
                switch (prm)
                {
                    case 0://開始番地
                        if (latch) c.dma.pcm0work[ch].pcm0adrs[ind] = dat;
                        else c.dma.pcm0work[ch].pcm0adrs[ind] |= (ushort)(dat << 8);
                        latch = !latch;
                        break;
                    case 1://再生長さ
                        if (latch) c.dma.pcm0work[ch].pcm0cnt[ind] = dat;
                        else c.dma.pcm0work[ch].pcm0cnt[ind] |= (ushort)(dat << 8);
                        latch = !latch;
                        break;
                    case 2://周波数
                        if (latch) c.dma.pcm0work[ch].pcm0freq[ind] = dat;
                        else c.dma.pcm0work[ch].pcm0freq[ind] |= (ushort)(dat << 8);
                        latch = !latch;
                        break;
                    case 3://パン
                        if (latch) c.dma.pcm0work[ch].pcm0pan[ind] = dat;
                        else c.dma.pcm0work[ch].pcm0pan[ind] |= (ushort)(dat << 8);
                        latch = !latch;
                        break;
                    case 4://音量
                        if (latch) c.dma.pcm0work[ch].pcm0vol[ind] = dat;
                        else c.dma.pcm0work[ch].pcm0vol[ind] |= (ushort)(dat << 8);
                        latch = !latch;
                        break;
                }
            }

            return 0;
        }

        public byte ReadReg(byte ChipID, int adr)
        {
            Chip c = chip[ChipID & 1];
            switch (adr)
            {
                case 0:
                    return c.indexAddress;
                case 1:
                    return c.reg[c.indexAddress & 0x1f];
                case 2:
                    return c.status;
                case 3:
                    return c.PIOData;
                case 4:
                    return c.dmaInt;
                case 5:
                    return c.IMR;
            }
            return 0;
        }

        public void setFIFOBuf(byte ChipID,byte[] buf)
        {
            chip[ChipID].dma.fifoBuf = buf;
        }

        public byte[] EMS_GetCrntMapBuf(byte ChipID)
        {
            return chip[ChipID].dma.ems.GetCrntMapBuf();
        }

        public void EMS_Map(byte ChipID,byte al, ref byte ah, ushort bx, ushort dx)
        {
            chip[ChipID].dma.ems.Map(al, ref ah, bx, dx);
        }

        public ushort EMS_GetPageMap(byte ChipID)
        {
            return chip[ChipID].dma.ems.GetPageMap();
        }

        public void EMS_GetHandleName(byte ChipID, ref byte ah, ushort dx, ref string sbuf)
        {
            chip[ChipID].dma.ems.GetHandleName(ref ah, dx, ref sbuf);
        }

        public void EMS_SetHandleName(byte ChipID, ref byte ah, ushort dx, string emsname2)
        {
            chip[ChipID].dma.ems.SetHandleName(ref ah, dx, emsname2);
        }

        public void EMS_AllocMemory(byte ChipID, ref byte ah, ref ushort dx, ushort bx)
        {
            chip[ChipID].dma.ems.AllocMemory(ref ah, ref dx, bx);
            chip[ChipID].dma.phandle = dx;
        }

        //public void setInt0bEnt(byte ChipID,Action callback)
        //{
        //chip[ChipID].dma.int0bEnt = callback;
        //}

        private void Exec(Chip c, short rcnt)
        {
            short dat0 = (short)((c.dma.GetData() - 0x80) * 380);
            short dat1 = (short)((c.dma.GetData() - 0x80) * 380);
            c.sound2[rcnt][0] = dat0;
            c.sound2[rcnt][1] = dat1;
        }

        private void synth(Chip c, short rcnt)
        {
            if (rcnt <= 0) return;
            int s0 = 0;
            int s1 = 0;
            for (int i = 0; i < Math.Min(rcnt, c.sound2.Length); i++)
            {
                s0 += c.sound2[i][0];
                s1 += c.sound2[i][1];
            }
            c.sound[0] = (short)(s0 / rcnt);
            c.sound[1] = (short)(s1 / rcnt);
        }

        private void ResetINTFlag()
        {
            //
        }

        private class DMA
        {
            //private Work work;
            private int ptr;
            private int cnt;
            public byte[] fifoBuf = new byte[FIFO_SIZE * MAXBUF * 2];
            //public Action int0bEnt;
            private bool latch = true;

            public DMA()
            {
                this.ptr = 0;
                this.cnt = 0;
                for (int i = 0; i < fifoBuf.Length; i++) fifoBuf[i] = 0x80;
                //this.fifoBuf = fifoBuf;
                //this.int0bEnt = int0bEnt;
            }

            public byte GetData()
            {
                if (fifoBuf == null) return 0x80;

                byte? dat = fifoBuf?[ptr];
                if (dat == null) return 0x80;

                ptr++;
                cnt--;

                if (ptr == fifoBuf.Length || cnt <= 0)
                {
                    //int0bEnt?.Invoke();
                    int0bent();
                    if (ptr == fifoBuf.Length)
                    {
                        ptr = 0;
                    }
                }

                return (byte)dat;
            }


            public void WriteReg(byte l, byte al)
            {
                if (l == 5)
                {
                    if (latch)
                    {
                        ptr = al;
                    }
                    else
                    {
                        ptr = (byte)ptr | (al * 0x100);
                    }
                    latch = !latch;
                }
                else if (l == 7)
                {
                    if (latch)
                    {
                        cnt = al;
                    }
                    else
                    {
                        cnt = (byte)cnt | (al * 0x100);
                    }
                    latch = !latch;
                }
            }



            private void int0bent()
            {
                //byte al = pcmrecmode;
                //if ((al & 1) != 0)// 録音モード?
                //{
                    //record3();
                //}
                //else
                {
                    program_dma();
                    clear_fint();
                    change_buffer();
                    put_fifo_data();
                }

                //EOI発行(多分不要)
                //nax.pc98.OutportB(0, 0x20);
            }

            private ushort fifoseg = 0;
            private ushort fifoptr1 = 0;
            private static int MAXBUF = 18;
            private static int FIFO_SIZE = 128;
            private const int PWORKE = 1;//18;
            private ushort fifoend1 = (ushort)(FIFO_SIZE * 2);
            private ushort fifoptr2 = (ushort)(FIFO_SIZE * 2);
            private ushort fifoend2 = (ushort)(FIFO_SIZE * 4);
            private ushort fifofin = (ushort)(FIFO_SIZE * 2 * MAXBUF);
            private ushort dma_adr = 0;
            private ushort dma_bank = 0;
            private ushort dma_count = 0;
            private ushort dma_data = (ushort)(FIFO_SIZE * 2);
            private byte dma_chan = 3;
            private ushort panl1_ = 0xc008;//or al,al
            private ushort panl2_ = 0xc008;//or al,al
            private ushort level1_ = 0x007f;
            private byte level2_ = 0x7f;
            private byte level3_ = 0x7f;
            public ushort jump1_ = 0;
            public ushort jump2_ = 0;
            public ushort phandle = 0xffff;// PCM用EMSハンドル
            private byte[] pemsbuf = new byte[32];// EMSマップ情報保存用
            public ushort freq2 = 0x987;
            public EMS ems = new EMS();

            public class Pcm0work
            {
                public ushort[] pcm0adrs = new ushort[] { 0, 0 };// 拡張PCM発音開始番地・EMSページ
                public ushort[] pcm0cnt = new ushort[] { 0, 0 };// 発音用減算カウンタ*4
                public ushort[] pcm0freq = new ushort[] { 0, 0 };// 周波数
                public ushort[] pcm0pan = new ushort[] { 0, 0 };// right+leftのパンandデータ(0/FFFF)
                public ushort[] pcm0vol = new ushort[] { 0, 0 };// 音量
            }
            public Pcm0work[] pcm0work = new Pcm0work[]{
                new Pcm0work(),new Pcm0work(),new Pcm0work(),new Pcm0work(),
                new Pcm0work(),new Pcm0work(),new Pcm0work(),new Pcm0work(),
                new Pcm0work(),new Pcm0work(),new Pcm0work(),new Pcm0work(),
                new Pcm0work(),new Pcm0work(),new Pcm0work(),new Pcm0work(),
                new Pcm0work()
            };

            private void program_dma()
            {
                byte al = 0b0000_0100;// DMAマスクビットをセット
                //al |= dma_chan;
                //nax.pc98.OutportB(0x15, al);// SingleMaskSet
                //al = 0b0100_1000;// DMAモードを設定
                //al |= dma_chan;
                //nax.pc98.OutportB(0x17, al);// ModeReg.

                ushort ax = fifoseg;// DMA専用セグメントを使用
                uint eax = (uint)((ax << 4) + fifoptr1);

                //progdma_sub:
                //nax.pc98.OutportB(0x19, (byte)eax);// ClearByteF/F
                // DMAアドレスの設定
                WriteReg(5, (byte)eax);// nax.pc98.OutportB(dma_adr, (byte)eax);
                WriteReg(5, (byte)(eax >> 8));//nax.pc98.OutportB(dma_adr, (byte)(eax >> 8));// DMA adr.

                // DMAバンクレジスタの設定
                eax >>= 16;
                //nax.pc98.OutportB(dma_bank, (byte)eax);// DMA bank adr.

                // DMAカウンタの設定
                WriteReg(7, (byte)dma_data);//nax.pc98.OutportB(dma_count, (byte)dma_data);
                WriteReg(7, (byte)(dma_data >> 8));//nax.pc98.OutportB(dma_count, (byte)(dma_data >> 8));// DMA count

                al = 0;// DMAマスクビットをクリア
                al |= dma_chan;
                //nax.pc98.OutportB(0x15, al);// SingleMaskClear
                //nax.pc98.OutportB(0x5f, al);
            }

            private void clear_fint()
            {
                //wss専用処理のみ
                //clear_fintwss:

                //ResetINTFlagを呼び出すが実際は何もしない
                //WriteReg(0x0f46, 0xfe);// R2に書き込み

                return;
            }

            private void change_buffer()
            {
                ushort ax = fifoptr1;
                change_ptr(ref ax);
                fifoptr1 = ax;
                ax += (ushort)(FIFO_SIZE * 2);
                fifoend1 = ax;

                ax = fifoptr2;
                change_ptr(ref ax);
                fifoptr2 = ax;
                ax += (ushort)(FIFO_SIZE * 2);
                fifoend2 = ax;

            }

            private void change_ptr(ref ushort ax)
            {
                ax += (ushort)(FIFO_SIZE * 2);
                if (ax >= fifofin)
                {
                    ax = 0;
                }
                //change_ptr1:
            }

            private void put_fifo_data()
            {
                //uint edxbk = nax.reg.edx;
                //ushort fsbk = nax.reg.fs;

                ushort bx = 0;
                save_extpcm(ref bx);// EMSのマップ保存
                ushort ax = fifoseg;// ES,FS = FIFOセグメント
                ushort es = ax;
                ushort fs = ax;
                //sign3:
                ax = 0x8080;
                ushort cx = (ushort)FIFO_SIZE;
                ushort di = fifoptr1;

                // FIFO転送前バッファの初期化
                do
                {
                    fifoBuf[di++] = (byte)ax;
                    fifoBuf[di++] = (byte)(ax >> 8);
                    cx--;
                } while (cx > 0);

                //segad3:
                ax = 0xc000;
                es = ax;

                //EMSのマッピング

                cx = 17;
                ushort si = 0;//ofs:pcm0work
                ushort dx, bp;
                //fifo_map1:
                do
                {
                    if (pcm0work[si].pcm0cnt[0] == 0 && pcm0work[si].pcm0cnt[1] == 0)
                    {
                        si += PWORKE;// 次のチャネルへ
                        cx--;
                        continue;
                    }

                    ushort cxbk = cx;
                    bx = pcm0work[si].pcm0adrs[1];// BX = EMS論理ページ
                                                  //Log.writeLine(LogLevel.INFO, string.Format("{0:X}", r.bx*0x4000+pcm0work[nax.reg.si].pcm0adrs[0]));
                                                  //naxad1:
                    dx = phandle;
                    ax = 0x4400;// EMSマッピング
                    byte ah = 0;
                    ems.Map(0x00,ref ah, bx, dx);
                    byte[] emsMem = ems.GetCrntMapBuf();

                    //FIFO転送前バッファへの書き込み
                    ax = pcm0work[si].pcm0pan[0];// パンの命令(L)
                    panl1_ = ax;
                    ax = pcm0work[si].pcm0pan[1];//	パンの命令(R)
                    panl2_ = ax;
                    //	jmp	$+2
                    bp = pcm0work[si].pcm0adrs[0];// BP = PCMデータの番地
                    cx = pcm0work[si].pcm0freq[0];// CX = PCM周波数カウンタ
                    bx = pcm0work[si].pcm0freq[1];// BX = 周波数データを加算
                    di = fifoptr1;// FS:DI = FIFO転送前バッファ
                    uint edx = pcm0work[si].pcm0cnt[0]
                        + (uint)pcm0work[si].pcm0cnt[1] * 0x1_0000;
                    //fifo_lop1:
                    do
                    {
                        byte al = emsMem[bp];
                        ax = (ushort)((sbyte)al * (sbyte)pcm0work[si].pcm0vol[0]);
                        ax <<= 2;
                        al = (byte)(ax >> 8);
                        ah = (byte)(ax >> 8);
                        //debug
                        //nax.reg.ah = nax.reg.al = emsMem[nax.reg.bp];

                        //panl1:
                        //熊:自己書き換えで切り替えています
                        switch (panl1_)
                        {
                            case 0xc008://or al,al
                                al |= al;
                                break;
                            case 0xc030://xor al,al
                                al ^= al;
                                break;
                        }
                        //panl2: パンのマスクを実行
                        switch (panl2_)
                        {
                            case 0xc008://or al,al
                                al |= al;
                                break;
                            case 0xe430://xor ah,ah
                                ah ^= ah;
                                break;
                        }

                    fifo_lop2:
                        fifoBuf[di++] += al;// L,Rの値を加算して格納
                        fifoBuf[di++] += ah;
                        cx += bx;

                        if ((cx & 0x8000) != 0)
                        {
                            //fifo_freq1:
                            if (di >= fifoend1)
                            {
                                goto fifo_end1;// 低周波数で同じ値を出力する場合の処理
                            }
                            goto fifo_lop2;
                        }
                        //fifo_freq2:
                        do
                        {
                            bp++;
                            if (bp >= 16384)// EMSの次のページに切り換わるか
                            {
                                //fifo_freq3:
                                bp = 0;
                                uint edxbk1 = edx;
                                ushort bxbk1 = bx;
                                bx = pcm0work[si].pcm0adrs[1];// BX = EMS論理ページ
                                bx++;
                                pcm0work[si].pcm0adrs[1] = bx;
                                //naxad2:
                                dx = phandle;
                                ax = 0x4400;// EMSマッピング
                                ems.Map(00,ref ah, bx, dx);
                                emsMem = ems.GetCrntMapBuf();
                                bx = bxbk1;
                                edx = edxbk1;
                            }
                            //fifo_freq5:
                            edx--;
                            if (edx == 0)
                            {
                                pcm0work[si].pcm0cnt[0] = 0;
                                pcm0work[si].pcm0cnt[1] = 0;
                                goto fifo_skip1_;
                            }
                            //freq2:
                            cx -= (ushort)freq2;// (O4CDATA*1.5);// freq2;// O4CDATA;
                        } while ((cx & 0x8000) == 0);
                    } while (di < fifoend1); // FIFOバイト数ループする

                fifo_end1:
                    pcm0work[si].pcm0cnt[0] = (ushort)edx;
                    pcm0work[si].pcm0cnt[1] = (ushort)(edx >> 16);
                    pcm0work[si].pcm0freq[0] = cx;
                    pcm0work[si].pcm0adrs[0] = bp;

                fifo_skip1_:
                    si += PWORKE;// 次のチャネルへ
                    cx = cxbk;
                    cx--;

                } while (cx > 0);

                remove_extpcm();

                //DSP処理

                //jump1:
                if (jump1_ == 0x3e3e)
                {
                    si = fifoptr2;
                    di = fifoptr1;
                    cx = (ushort)FIFO_SIZE;
                    //jump2:
                    switch (jump2_)
                    {
                        case 0:
                            //test_lop1:
                            do
                            {
                                //	segfs
                                ax = (ushort)(fifoBuf[si] + fifoBuf[si + 1] * 0x100);
                                si += 2;
                                //sign1:
                                byte al = (byte)ax;
                                byte ah = (byte)(ax >> 8);
                                al -= 0x80;// none
                                ah -= 0x80;
                                dx = 0;
                                dx = ah;
                                ax = (ushort)(sbyte)al;
                                ushort tmp = ax;
                                ax = dx;
                                dx = tmp;
                                ax = (ushort)(sbyte)(byte)ax;
                                ax += dx;
                                //level1:
                                dx = level1_;// 0x007f;
                                int ans = (short)ax * (short)dx;
                                dx = (ushort)(ans >> 16);
                                ax = (ushort)ans;
                                fifoBuf[di] += (byte)(ax >> 8);
                                fifoBuf[di + 1] -= (byte)(ax >> 8);
                                di += 2;
                                cx--;
                            } while (cx > 0);
                            break;
                        case 1:
                            //test_lop2:
                            do
                            {
                                //	segfs
                                ax = (ushort)(fifoBuf[si] + fifoBuf[si + 1] * 0x100);
                                si += 2;
                                byte al = (byte)((byte)ax - (byte)(ax >> 8));
                                //level2:
                                byte ah = level2_;//0x7f;
                                ax = (ushort)((sbyte)al * (sbyte)ah);
                                fifoBuf[di + 1] += (byte)(ax >> 8);
                                fifoBuf[di] -= (byte)(ax >> 8);
                                di += 2;
                                cx--;
                            } while (cx > 0);
                            break;
                        case 2:
                            //test_entry3:
                            cx <<= 1;
                            //test_lop3:
                            do
                            {
                                //	segfs
                                byte al = fifoBuf[si++];
                                //sign2:
                                al -= 0x80;// none
                                           //level3:
                                byte ah = level3_;//0x7f
                                ax = (ushort)((sbyte)al * (sbyte)ah);
                                ax <<= 1;
                                fifoBuf[di] -= (byte)(ax >> 8);
                                di++;
                                cx--;
                            } while (cx > 0);
                            break;
                    }
                }

            }



            public void save_extpcm(ref ushort bx)
            {
                bx = ems.GetPageMap(); // EMSマップ保存
            }

            public void remove_extpcm()
            {
                ems.SetPageMap(0, pemsbuf);
            }


        }

        public class EMS
        {
            private int crntEmsHandle = 0;
            private int crntPageMap = 0;
            private int pPageNo = 0;
            private int lPageNo = 0;
            private Dictionary<int, bool> useEMSList;
            private Dictionary<int, string> handleName;
            private Dictionary<int, byte[][]> emsBuff;
            private Dictionary<int, int[]> mappedPage;

            public EMS()
            {
                crntEmsHandle = 0;
                useEMSList = new Dictionary<int, bool>();
                handleName = new Dictionary<int, string>();
                emsBuff = new Dictionary<int, byte[][]>();
                mappedPage = new Dictionary<int, int[]>();
            }

            public void GetHandleName(ref byte ah,ushort dx, ref string sbuf)
            {
                ah = 0;
                if (handleName.ContainsKey(dx))
                {
                    sbuf = handleName[dx];
                    return;
                }

                sbuf = "";//熊:一致するものがなくてもahは0になるっぽい
            }

            public void SetHandleName(ref byte ah, ushort dx, string emsname2)
            {
                ah = 0;
                if (!handleName.ContainsKey(dx))
                    handleName.Add(dx, emsname2);
                else
                    handleName[dx] = emsname2;
            }

            public void AllocMemory(ref byte ah, ref ushort dx, ushort bx)
            {
                //未使用のハンドルを探す
                int cnt = 0;
                while (cnt < 0x10000)
                {
                    if (!useEMSList.ContainsKey(crntEmsHandle) || !useEMSList[crntEmsHandle]) break;
                    crntEmsHandle++;
                    crntEmsHandle &= 0xffff;
                    cnt++;
                }

                if (cnt == 0x10000)
                {
                    ah = 1;
                    return;
                }

                dx = (ushort)crntEmsHandle;
                if (!useEMSList.ContainsKey(crntEmsHandle)) useEMSList.Add(crntEmsHandle, true);
                else useEMSList[crntEmsHandle] = true;
                if (!emsBuff.ContainsKey(crntEmsHandle)) emsBuff.Add(crntEmsHandle, null);
                emsBuff[crntEmsHandle] = new byte[bx][];
                if (!mappedPage.ContainsKey(crntEmsHandle)) mappedPage.Add(crntEmsHandle, null);
                mappedPage[crntEmsHandle] = new int[bx];

                for (int i = 0; i < bx; i++)
                {
                    emsBuff[crntEmsHandle][i] = new byte[16 * 1024];//alloc 16Kbyte
                    for (int j = 0; j < 16 * 1024; j++) emsBuff[crntEmsHandle][i][j] = 0x80;
                    mappedPage[crntEmsHandle][i] = 0xffff;//ummap状態
                }
                ah = 0;
            }

            public ushort GetPageMap()//x86Register reg, byte[] pemsbuf)
            {
                //reg.bx = (ushort)crntPageMap;
                return (ushort)crntPageMap;
            }

            public void Map(byte al,ref byte ah, ushort bx, ushort dx)
            {
                pPageNo = al;//物理ページ番号
                lPageNo = bx;//論理ページ番号

                try
                {
                    //マップ
                    mappedPage[dx][pPageNo] = lPageNo;//0xffffの場合はアンマップ状態

                    ah = 0x00;
                    return;//正常実行
                }
                catch
                {
                    ah = 0x80;
                    return;
                }
            }

            public void SetPageMap(ushort si, byte[] pemsbuf)
            {
                crntPageMap = pemsbuf[si];// reg.bx;
            }

            public byte[] GetCrntMapBuf()
            {
                return emsBuff[crntEmsHandle][mappedPage[crntEmsHandle][crntPageMap]];
            }

            public byte[] GetEmsArray(int stPage, int endPage)
            {
                List<byte> lst = new List<byte>();
                for (int i = stPage; i < endPage; i++)
                {
                    lst.AddRange(emsBuff[crntEmsHandle][i]);
                }
                return lst.ToArray();
            }
        }

    }
}