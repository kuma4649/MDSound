using System;

namespace MDSound
{
    public class visWaveBuffer
    {
        private short[][] buf;
        private int crntPos;
        private int size;

        public visWaveBuffer(int size = 2048)
        {
            buf = new short[2][];
            for (int i = 0; i < 2; i++)
            {
                buf[i] = new short[size];
            }
            crntPos = 0;
            this.size = size;
        }

        public void Enq(short l, short r)
        {
            buf[0][crntPos] = l;
            buf[1][crntPos] = r;
            crntPos++;
            crntPos %= size;
        }

        public void Copy(short[][] dest)
        {
            int pos = crntPos;
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < size; j++)
                    dest[i][j] = buf[i][(pos + j) % size];
        }
    }
}