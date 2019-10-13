using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDSound.ZM_1
{
    public class ChipElement
    {
        //from fmgen
        private static int[] kftable = new int[64];
        private static uint[] kctable = new uint[16]
        {
        5197, 5506, 5833, 6180, 6180, 6547, 6937, 7349,
        7349, 7786, 8249, 8740, 8740, 9259, 9810, 10394,
        };

        protected Operator @operator;

        public ChipElement(Operator @operator)
        {
            this.@operator = @operator;
            MakeTable();
        }

        public virtual void Write(int adress, int data)
        { 
        }

        /// <summary>
        /// from fmgen
        /// </summary>
        public void SetKCKF(uint kc, uint kf)
        {
            int oct = (int)(19 - ((kc >> 4) & 7));

            //printf("%p", this);
            uint kcv = kctable[kc & 0x0f];
            kcv = (kcv + 2) / 4 * 4;
            //printf(" %.4x", kcv);
            uint dp = (uint)(kcv * kftable[kf & 0x3f]);
            //printf(" %.4x %.4x %.8x", kcv, kftable[kf & 0x3f], dp >> oct);
            dp >>= 16 + 3;
            dp <<= 16 + 3;
            dp >>= oct;
            uint bn = (kc >> 2) & 31;
            //op[0].SetDPBN(dp, bn);
            //op[1].SetDPBN(dp, bn);
            //op[2].SetDPBN(dp, bn);
            //op[3].SetDPBN(dp, bn);
        }

        /// <summary>
        /// from fmgen
        /// </summary>
        public void MakeTable()
        {
            // 100/64 cent =  2^(i*100/64*1200)
            for (int i = 0; i < 64; i++)
            {
                kftable[i] = (int)(0x10000 * Math.Pow(2.0, i / 768.0));
            }
        }

    }
}
