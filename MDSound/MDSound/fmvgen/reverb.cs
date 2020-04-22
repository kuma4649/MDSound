using System;
using System.Collections.Generic;
using System.Text;

namespace MDSound
{
    public class reverb
    {
        private int[] Buf = null;
        private int Pos = 0;
        private int Delta = 0;
        public double[] SendLevel = null;
        private int currentCh = 0;

        public reverb(int bufSize, int ch)
        {
            this.Buf = new int[bufSize];
            this.Pos = 0;
            this.currentCh = 0;
            SetDelta(64);

            this.SendLevel = new double[ch];
            for (int i = 0; i < ch; i++)
            {
                SetSendLevel(i, 0);
            }
        }

        public void SetDelta(int n)
        {
            this.Delta = (int)Buf.Length / 128 * Math.Max(Math.Min(n, 127), 0);
        }

        public void SetSendLevel(int ch, int n)
        {
            if (n == 0)
            {
                SendLevel[ch] = 0;
                return;
            }
            //SendLevel[ch] = 1.0 / (2 << Math.Max(Math.Min((15 - n), 15), 0));
            n = Math.Max(Math.Min(n, 15), 0);
            SendLevel[ch] = 1.0 * sl[n];
            Console.WriteLine("{0} {1}", ch, SendLevel[ch]);
        }

        private double[] sl = new double[16] {
            0.0050000 , 0.0150000 , 0.0300000 , 0.0530000 ,
            0.0680000 , 0.0800000 , 0.0960000 , 0.1300000 ,
            0.2000000 , 0.3000000 , 0.4000000 , 0.5000000 ,
            0.6000000 , 0.7000000 , 0.8000000 , 0.9000000
        };

        public int GetDataFromPos()
        {
            return Buf[Pos];
        }

        public void ClearDataAtPos()
        {
            Buf[Pos] = 0;
        }

        public void UpdatePos()
        {
            Pos = (1 + Pos) % Buf.Length;
        }

        //public void StoreData(int ch, int v)
        //{
        //int ptr = (Delta + Pos) % Buf.Length;
        //Buf[ptr] += (int)(v * SendLevel[ch]);
        //}

        public void StoreData(int v)
        {
            int ptr = (Delta + Pos) % Buf.Length;
            Buf[ptr] += (int)(v);
        }

        public void SetReg(uint adr, byte data)
        {
            if (adr == 0)
            {
                SetDelta(data & 0x7f);
            }
            else if (adr == 1)
            {
                currentCh = Math.Max(Math.Min(data & 0x3f, 30), 0);
            }
            else if (adr == 2)
            {
                SetSendLevel(currentCh, data & 0xf);
            }
        }
    }
}
