using System;
using System.Collections.Generic;
using System.Text;

namespace MDSound.fmvgen.effect
{
	public class Compressor
	{
        //フィルタークラス(https://vstcpp.wpblog.jp/?p=1939 より)

        private int samplerate = 44100;
        private int currentCh = 0;
        private int maxCh;
        private ChInfo[] chInfo = null;
        private float[] fbuf = new float[2];

        private class ChInfo
        {
            public bool sw = false;

            // エフェクターのパラメーター
            public float threshold = 0.3f;  // 圧縮が始まる音圧。0.1～1.0程度
            public float ratio = 2.0f; // 圧縮する割合。2.0～10.0程度
            public float volume = 2.0f; // 最終的な音量。1.0～3.0程度

            // 内部変数
            // フィルタークラス(https://vstcpp.wpblog.jp/?page_id=728 より)
            public CMyFilter envfilterL, envfilterR;   // 音圧を検知するために使うローパスフィルタ
            public CMyFilter gainfilterL, gainfilterR; // 急激な音量変化を避けるためのローパスフィルタ
            public float envFreq = 30.0f;
            public float envQ = 1.0f;
            public float gainFreq = 5.0f;
            public float gainQ = 1.0f;

        }



        public Compressor(int samplerate, int maxCh)
        {
            this.samplerate = samplerate;
            this.maxCh = maxCh;
            Init();
        }

        public void Init()
        {
            currentCh = 0;
            chInfo = new ChInfo[maxCh];
            for (int i = 0; i < chInfo.Length; i++)
            {
                chInfo[i] = new ChInfo();
                chInfo[i].sw = false;

                chInfo[i].threshold = 0.3f;
                chInfo[i].ratio = 2.0f;
                chInfo[i].volume = 2.0f;

                chInfo[i].envFreq = 30.0f;
                chInfo[i].envQ = 1.0f;
                chInfo[i].gainFreq = 5.0f;
                chInfo[i].gainQ = 1.0f;
                chInfo[i].envfilterL = new CMyFilter();
                chInfo[i].envfilterR = new CMyFilter();   // 音圧を検知するために使うローパスフィルタ
                chInfo[i].gainfilterL = new CMyFilter();
                chInfo[i].gainfilterR = new CMyFilter(); // 急激な音量変化を避けるためのローパスフィルタ
                SetLowPass(i, 30.0f, 1.0f, 5.0f, 1.0f);
            }

        }

        public void Mix(int ch, ref int inL, ref int inR, int wavelength = 1)
        {
            if (ch < 0) return;
            if (ch >= maxCh) return;
            if (chInfo == null) return;
            if (chInfo[ch] == null) return;
            if (!chInfo[ch].sw) return;

            fbuf[0] = inL / 21474.83647f;
            fbuf[1] = inR / 21474.83647f;

            // inL[]、inR[]、outL[]、outR[]はそれぞれ入力信号と出力信号のバッファ(左右)
            // wavelenghtはバッファのサイズ、サンプリング周波数は44100Hzとする

            // 入力信号にエフェクトをかける
            // 入力信号の絶対値をとったものをローパスフィルタにかけて音圧を検知する
            float tmpL = chInfo[ch].envfilterL.Process(Math.Abs(fbuf[0]));
            float tmpR = chInfo[ch].envfilterR.Process(Math.Abs(fbuf[1]));

            // 音圧をもとに音量(ゲイン)を調整(左)
            float gainL = 1.0f;

            if (tmpL > chInfo[ch].threshold)
            {
                // スレッショルドを超えたので音量(ゲイン)を調節(圧縮)
                gainL = chInfo[ch].threshold + (tmpL - chInfo[ch].threshold) / chInfo[ch].ratio;
            }
            // 音量(ゲイン)が急激に変化しないようローパスフィルタを通す
            gainL = chInfo[ch].gainfilterL.Process(gainL);

            // 左と同様に右も音圧をもとに音量(ゲイン)を調整
            float gainR = 1.0f;
            if (tmpR > chInfo[ch].threshold)
            {
                gainR = chInfo[ch].threshold + (tmpR - chInfo[ch].threshold) / chInfo[ch].ratio;
            }
            gainR = chInfo[ch].gainfilterR.Process(gainL);


            // 入力信号に音量(ゲイン)をかけ、さらに最終的な音量を調整し出力する
            fbuf[0] = chInfo[ch].volume * gainL * fbuf[0];
            fbuf[1] = chInfo[ch].volume * gainR * fbuf[1];
            inL = (int)(fbuf[0] * 21474.83647f);
            inR = (int)(fbuf[1] * 21474.83647f);
        }

        private void SetLowPass(int ch, float envFreq, float envQ, float gainFreq, float gainQ)
        {
            // ローパスフィルターを設定

            // カットオフ周波数が高いほど音圧変化に敏感になる。目安は10～50Hz程度
            chInfo[ch].envfilterL.LowPass(envFreq, envQ, samplerate);
            chInfo[ch].envfilterR.LowPass(envFreq, envQ, samplerate);
            // カットオフ周波数が高いほど急激な音量変化になる。目安は5～50Hz程度
            chInfo[ch].gainfilterL.LowPass(gainFreq, gainQ, samplerate);
            chInfo[ch].gainfilterR.LowPass(gainFreq, gainQ, samplerate);
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
                chInfo[currentCh].sw = ((data & 0x80) != 0);
                chInfo[currentCh].volume = (data & 0x7f) / (127.0f / 3.0f);
            }
            else if (adr == 2)
            {
                chInfo[currentCh].threshold = Math.Max(data / 255.0f, 0.1f);
            }
            else if (adr == 3)
            {
                chInfo[currentCh].ratio = Math.Max(data / (255.0f / 10.0f), 1.0f);
            }
            else if (adr == 4)
            {
                chInfo[currentCh].envFreq = data / (255.0f / 50.0f);
                chInfo[currentCh].envfilterL.LowPass(chInfo[currentCh].envFreq, chInfo[currentCh].envQ, samplerate);
                chInfo[currentCh].envfilterR.LowPass(chInfo[currentCh].envFreq, chInfo[currentCh].envQ, samplerate);
            }
            else if (adr == 5)
            {
                chInfo[currentCh].envQ = CMyFilter.QTable[data];
                chInfo[currentCh].envfilterL.LowPass(chInfo[currentCh].envFreq, chInfo[currentCh].envQ, samplerate);
                chInfo[currentCh].envfilterR.LowPass(chInfo[currentCh].envFreq, chInfo[currentCh].envQ, samplerate);
            }
            else if (adr == 6)
            {
                chInfo[currentCh].gainFreq = data / (255.0f / 50.0f);
                chInfo[currentCh].gainfilterL.LowPass(chInfo[currentCh].gainFreq, chInfo[currentCh].gainQ, samplerate);
                chInfo[currentCh].gainfilterR.LowPass(chInfo[currentCh].gainFreq, chInfo[currentCh].gainQ, samplerate);
            }
            else if (adr == 7)
            {
                chInfo[currentCh].gainQ = CMyFilter.QTable[data];
                chInfo[currentCh].gainfilterL.LowPass(chInfo[currentCh].gainFreq, chInfo[currentCh].gainQ, samplerate);
                chInfo[currentCh].gainfilterR.LowPass(chInfo[currentCh].gainFreq, chInfo[currentCh].gainQ, samplerate);
            }
        }

    }
}
