using System;
using System.Collections.Generic;
using System.Text;

namespace MDSound
{
    class CMyFilter
    {

		// フィルタの係数
		private float a0, a1, a2, b0, b1, b2;
		// バッファ
		private float out1, out2;
		private float in1, in2;


		public CMyFilter()
		{
			// メンバー変数を初期化
			a0 = 1.0f; // 0以外にしておかないと除算でエラーになる
			a1 = 0.0f;
			a2 = 0.0f;
			b0 = 1.0f;
			b1 = 0.0f;
			b2 = 0.0f;

			in1 = 0.0f;
			in2 = 0.0f;

			out1 = 0.0f;
			out2 = 0.0f;
		}

		// --------------------------------------------------------------------------------
		// 入力信号にフィルタを適用する関数
		// --------------------------------------------------------------------------------
		public float Process(float in_)
		{
			// 入力信号にフィルタを適用し、出力信号変数に保存。
			float out_ = b0 / a0 * in_ +b1 / a0 * in1 + b2 / a0 * in2
				- a1 / a0 * out1 - a2 / a0 * out2;

			in2 = in1; // 2つ前の入力信号を更新
			in1 = in_;  // 1つ前の入力信号を更新

			out2 = out1; // 2つ前の出力信号を更新
			out1 = out_;  // 1つ前の出力信号を更新

			// 出力信号を返す
			return out_;
		}

		public void HighPass(float freq, float q, float samplerate)
		{
			// フィルタ係数計算で使用する中間値を求める。
			float omega = 2.0f * 3.14159265f * freq / samplerate;
			float alpha = (float)(Math.Sin(omega) / (2.0f * q));

			// フィルタ係数を求める。
			a0 = 1.0f + alpha;
			a1 = (float)(-2.0f * Math.Cos(omega));
			a2 = 1.0f - alpha;
			b0 = (float)((1.0f + Math.Cos(omega)) / 2.0f);
			b1 = (float)(-(1.0f + Math.Cos(omega)));
			b2 = (float)((1.0f + Math.Cos(omega)) / 2.0f);
		}
	}
}
