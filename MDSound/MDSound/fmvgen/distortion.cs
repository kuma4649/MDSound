using System;
using System.Collections.Generic;
using System.Text;

namespace MDSound
{
    public class distortion
    {
        private CMyFilter highpassL = new CMyFilter(), highpassR = new CMyFilter(); // フィルタークラス(https://vstcpp.wpblog.jp/?page_id=728 より)
                                                                                    // エフェクターのパラメーター
        private float gain = 300.0f; // 増幅量。10～300程度(dB換算で20dB～50dB程度)
        private float volume = 0.1f;   // 出力信号の音量。0.0～1.0の範囲
        private int clock;
        private int ch;

        public distortion(int clock, int ch)
        {
            Init(clock,ch);
        }

        public void Init(int clock, int ch)
        {
            this.clock = clock;
            this.ch = ch;

            // 内部変数

            // 高音域のみ通す(低音域をカットする)フィルタ設定(左右分)
            // カットする周波数の目安は20Hz～300Hz程度
            // 増幅量が大きくなれば、カットオフ周波数も大きくするとよい
            highpassL.HighPass(200.0f, (float)(1.0f / Math.Sqrt(2.0f)), clock);
            highpassR.HighPass(200.0f, (float)(1.0f / Math.Sqrt(2.0f)), clock);

        }

        float[] fbuf = new float[2] { 0f, 0f };

        public void Mix(int[] in_, int wavelength)
        {
            fbuf[0] = in_[0] / 21474.83647f;
            fbuf[1] = in_[1] / 21474.83647f;

            // inL[]、inR[]、outL[]、outR[]はそれぞれ入力信号と出力信号のバッファ(左右)
            // wavelenghtはバッファのサイズ、サンプリング周波数は44100Hzとする

            // 入力信号にエフェクターを適用する
            for (int i = 0; i < wavelength * 2; i += 2)
            {
                // 入力信号にフィルタを適用する
                float tmpL = highpassL.Process(fbuf[i + 0]);

                // 入力信号にゲインを掛けて増幅する
                tmpL = gain * tmpL;

                // 振幅の最大値(ここでは-1.0～1.0)を超えたものをクリッピングする
                if (tmpL > 1.0) { tmpL = 1.0f; }
                if (tmpL < -1.0) { tmpL = -1.0f; }

                // 右側の入力信号も同様に処理
                float tmpR = highpassR.Process(fbuf[i + 1]);
                tmpR = gain * tmpR;
                if (tmpR > 1.0) { tmpR = 1.0f; }
                if (tmpR < -1.0) { tmpR = -1.0f; }

                // 入力信号にフィルタをかける
                fbuf[i + 0] = volume * tmpL;
                fbuf[i + 1] = volume * tmpR;
            }

            in_[0] = (int)(fbuf[0] * 21474.83647f);
            in_[1] = (int)(fbuf[1] * 21474.83647f);
        }

    }
}
