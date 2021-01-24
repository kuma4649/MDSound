using System;
using System.Collections.Generic;
using System.Text;

namespace MDSound.fmvgen.effect
{
    public class HPFLPF
    {
        // フィルタークラス(https://vstcpp.wpblog.jp/?page_id=728 より)
        // エフェクターのパラメーター
        private int clock;
        private int maxCh;
        private ChInfo[] chInfo = null;
        private float[] fbuf = new float[2] { 0f, 0f };
        private int currentCh = 0;


        private class ChInfo
        {
            public CMyFilter highpassL = new CMyFilter();
            public CMyFilter highpassR = new CMyFilter();
            
            public CMyFilter lowpassL = new CMyFilter();
            public CMyFilter lowpassR = new CMyFilter();

            public bool hsw = false;
            public float hFreq = 1000f;
            public float hQ = (float)(1.0f / Math.Sqrt(2.0f));

            public bool lsw = false;
            public float lFreq = 300f;
            public float lQ = (float)(1.0f / Math.Sqrt(2.0f));
        }

        public HPFLPF(int clock, int maxCh)
        {
            this.clock = clock;
            this.maxCh = maxCh;
            if (CMyFilter.freqTable == null) CMyFilter.makeTable();
            Init();
        }

        public void Init()
        {
            chInfo = new ChInfo[maxCh];
            for (int i = 0; i < chInfo.Length; i++)
            {
                chInfo[i] = new ChInfo();

                // 内部変数
                // 高音域のみ通す(低音域をカットする)フィルタ設定(左右分)
                // カットする周波数の目安は20Hz～300Hz程度
                // 増幅量が大きくなれば、カットオフ周波数も大きくするとよい
                chInfo[i].hFreq = 1000f;
                chInfo[i].hQ = (float)(1.0f / Math.Sqrt(2.0f));
                chInfo[i].lFreq = 300f;
                chInfo[i].lQ = (float)(1.0f / Math.Sqrt(2.0f));

                chInfo[i].hsw = false;
                chInfo[i].highpassL = new CMyFilter();
                chInfo[i].highpassR = new CMyFilter();

                chInfo[i].lsw = false;
                chInfo[i].lowpassL = new CMyFilter();
                chInfo[i].lowpassR = new CMyFilter();

                updateParam(i);
            }
        }

        private void updateParam(int ch)
        {
            chInfo[ch].highpassL.HighPass(chInfo[ch].hFreq, chInfo[ch].hQ, clock);
            chInfo[ch].highpassR.HighPass(chInfo[ch].hFreq, chInfo[ch].hQ, clock);
            chInfo[ch].lowpassL.LowPass(chInfo[ch].lFreq, chInfo[ch].lQ, clock);
            chInfo[ch].lowpassR.LowPass(chInfo[ch].lFreq, chInfo[ch].lQ, clock);
        }

        public void Mix(int ch, ref int inL, ref int inR, int wavelength = 1)
        {
            if (ch < 0) return;
            if (ch >= maxCh) return;
            if (chInfo == null) return;
            if (chInfo[ch] == null) return;

            fbuf[0] = inL / 21474.83647f;
            fbuf[1] = inR / 21474.83647f;

            // 入力信号にフィルタを適用する
            if (chInfo[ch].hsw)
            {
                fbuf[0] = chInfo[ch].highpassL.Process(fbuf[0]);
                fbuf[1] = chInfo[ch].highpassR.Process(fbuf[1]);
            }

            if (chInfo[ch].lsw)
            {
                fbuf[0] = chInfo[ch].lowpassL.Process(fbuf[0]);
                fbuf[1] = chInfo[ch].lowpassR.Process(fbuf[1]);
            }

            inL = (int)(fbuf[0] * 21474.83647f);
            inR = (int)(fbuf[1] * 21474.83647f);
        }

        public void SetReg(uint adr, byte data)
        {
            if (adr == 0)
            {
                currentCh = Math.Max(Math.Min(data & 0x3f, 38), 0);
                if ((data & 0x80) != 0) Init();
            }
            else if (adr == 1)
            {
                chInfo[currentCh].lsw = (data != 0);
            }
            else if (adr == 2)
            {
                chInfo[currentCh].lFreq = CMyFilter.freqTable[data];
            }
            else if (adr == 3)
            {
                chInfo[currentCh].lQ = CMyFilter.QTable[data];
            }
            else if (adr == 4)
            {
                chInfo[currentCh].hsw = (data != 0);
            }
            else if (adr == 5)
            {
                chInfo[currentCh].hFreq = CMyFilter.freqTable[data];
            }
            else if (adr == 6)
            {
                chInfo[currentCh].hQ = CMyFilter.QTable[data];
            }

            updateParam(currentCh);
        }

    }
}
