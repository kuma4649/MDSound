using MDSound.np;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace MDSound
{
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
                    chip[ChipID].dma.int0bEnt = (Action)option[1];
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
            else
            {
                switch (adr)
                {
                    case 0x5:
                        c.dma.WriteReg(5, (byte)data);
                        break;
                    case 0x7:
                        c.dma.WriteReg(7, (byte)data);
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
            }
            return 0;
        }

        public void setFIFOBuf(byte ChipID,byte[] buf)
        {
            chip[ChipID].dma.fifoBuf = buf;
        }
        public void setInt0bEnt(byte ChipID,Action callback)
        {
            chip[ChipID].dma.int0bEnt = callback;
        }

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
            public byte[] fifoBuf;
            public Action int0bEnt;
            private bool latch = true;

            public DMA()
            {
                this.ptr = 0;
                this.cnt = 0;
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
                    int0bEnt?.Invoke();
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
        }
    }
}