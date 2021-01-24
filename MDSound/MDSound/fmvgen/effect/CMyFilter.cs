using System;
using System.Collections.Generic;
using System.Text;

namespace MDSound
{
    class CMyFilter
    {
		public static float convInt = 21474.83647f;
		public static float[] freqTable;
		public static float[] gainTable;
		public static float[] QTable;

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

		public void LowPass(float freq, float q, float samplerate)
		{
			// フィルタ係数計算で使用する中間値を求める。
			float omega = 2.0f * 3.14159265f * freq / samplerate;
			float alpha = (float)(Math.Sin(omega) / (2.0f * q));

			// フィルタ係数を求める。
			a0 = 1.0f + alpha;
			a1 = (float)(-2.0f * Math.Cos(omega));
			a2 = 1.0f - alpha;
			b0 = (float)((1.0f - Math.Cos(omega)) / 2.0f);
			b1 = (float)(1.0f - Math.Cos(omega));
			b2 = (float)((1.0f - Math.Cos(omega)) / 2.0f);
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


		public void BandPass(float freq, float bw, float samplerate)
		{
			// フィルタ係数計算で使用する中間値を求める。
			float omega = 2.0f * 3.14159265f * freq / samplerate;
			float alpha = (float)(Math.Sin(omega) * Math.Sinh(Math.Log(2.0f) / 2.0 * bw * omega / Math.Sin(omega)));

			// フィルタ係数を求める。
			a0 = 1.0f + alpha;
			a1 = (float)(-2.0f * Math.Cos(omega));
			a2 = 1.0f - alpha;
			b0 = alpha;
			b1 = 0.0f;
			b2 = -alpha;
		}


		public void Notch(float freq, float bw, float samplerate)
		{
			// フィルタ係数計算で使用する中間値を求める。
			float omega = 2.0f * 3.14159265f * freq / samplerate;
			float alpha = (float)(Math.Sin(omega) * Math.Sinh(Math.Log(2.0f) / 2.0 * bw * omega / Math.Sin(omega)));

			// フィルタ係数を求める。
			a0 = 1.0f + alpha;
			a1 = (float)(-2.0f * Math.Cos(omega));
			a2 = 1.0f - alpha;
			b0 = 1.0f;
			b1 = (float)(-2.0f * Math.Cos(omega));
			b2 = 1.0f;
		}

		public void LowShelf(float freq, float q, float gain, float samplerate)
		{
			// フィルタ係数計算で使用する中間値を求める。
			float omega = 2.0f * 3.14159265f * freq / samplerate;
			float alpha = (float)(Math.Sin(omega) / (2.0f * q));
			float A = (float)(Math.Pow(10.0f, (gain / 40.0f)));
			float beta = (float)(Math.Sqrt(A) / q);

			// フィルタ係数を求める。
			a0 = (float)((A + 1.0f) + (A - 1.0f) * Math.Cos(omega) + beta * Math.Sin(omega));
			a1 = (float)(-2.0f * ((A - 1.0f) + (A + 1.0f) * Math.Cos(omega)));
			a2 = (float)((A + 1.0f) + (A - 1.0f) * Math.Cos(omega) - beta * Math.Sin(omega));
			b0 = (float)(A * ((A + 1.0f) - (A - 1.0f) * Math.Cos(omega) + beta * Math.Sin(omega)));
			b1 = (float)(2.0f * A * ((A - 1.0f) - (A + 1.0f) * Math.Cos(omega)));
			b2 = (float)(A * ((A + 1.0f) - (A - 1.0f) * Math.Cos(omega) - beta * Math.Sin(omega)));
		}

		public void HighShelf(float freq, float q, float gain, float samplerate)
		{
			// フィルタ係数計算で使用する中間値を求める。
			float omega = 2.0f * 3.14159265f * freq / samplerate;
			float alpha = (float)(Math.Sin(omega) / (2.0f * q));
			float A = (float)(Math.Pow(10.0f, (gain / 40.0f)));
			float beta = (float)(Math.Sqrt(A) / q);

			// フィルタ係数を求める。
			a0 = (float)((A + 1.0f) - (A - 1.0f) * Math.Cos(omega) + beta * Math.Sin(omega));
			a1 = (float)(2.0f * ((A - 1.0f) - (A + 1.0f) * Math.Cos(omega)));
			a2 = (float)((A + 1.0f) - (A - 1.0f) * Math.Cos(omega) - beta * Math.Sin(omega));
			b0 = (float)(A * ((A + 1.0f) + (A - 1.0f) * Math.Cos(omega) + beta * Math.Sin(omega)));
			b1 = (float)(-2.0f * A * ((A - 1.0f) + (A + 1.0f) * Math.Cos(omega)));
			b2 = (float)(A * ((A + 1.0f) + (A - 1.0f) * Math.Cos(omega) - beta * Math.Sin(omega)));
		}

		public void Peaking(float freq, float bw, float gain, float samplerate)
		{
			// フィルタ係数計算で使用する中間値を求める。
			float omega = 2.0f * 3.14159265f * freq / samplerate;
			float alpha = (float)(Math.Sin(omega) * Math.Sinh(Math.Log(2.0f) / 2.0 * bw * omega / Math.Sin(omega)));
			float A = (float)(Math.Pow(10.0f, (gain / 40.0f)));

			// フィルタ係数を求める。
			a0 = 1.0f + alpha / A;
			a1 = (float)(-2.0f * Math.Cos(omega));
			a2 = 1.0f - alpha / A;
			b0 = 1.0f + alpha * A;
			b1 = (float)(-2.0f * Math.Cos(omega));
			b2 = 1.0f - alpha * A;
		}

		public void AllPass(float freq, float q, float samplerate)
		{
			// フィルタ係数計算で使用する中間値を求める。
			float omega = 2.0f * 3.14159265f * freq / samplerate;
			float alpha = (float)(Math.Sin(omega) / (2.0f * q));

			// フィルタ係数を求める。
			a0 = 1.0f + alpha;
			a1 = (float)(-2.0f * Math.Cos(omega));
			a2 = 1.0f - alpha;
			b0 = 1.0f - alpha;
			b1 = (float)(-2.0f * Math.Cos(omega));
			b2 = 1.0f + alpha;
		}

		public static void makeTable()
		{
			freqTable = new float[256];
			gainTable = new float[256];
			QTable = new float[256];

			for (int i = 0; i < 256; i++)
			{
				//freqTableの作成(1～38500まで)
				if (i < 256 / 8 * 3)
				{
					freqTable[i] = i + 1;
				}
				else if (i < 256 / 8 * 5)
				{
					freqTable[i] = (i - 256 / 8 * 3) * 10 + 100;
				}
				else if (i < 256 / 8 * 7)
				{
					freqTable[i] = (i - 256 / 8 * 5) * 100 + 800;
				}
				else
				{
					freqTable[i] = (i - 256 / 8 * 7) * 1000 + 7500;
				}


				//gainTableの作成(-20～+19.84375まで)
				if (i < 128)
				{
					gainTable[i] = (float)(-20.0 / 128.0 * (128 - i));
				}
				else
				{
					gainTable[i] = (float)(20.0 / 128.0 * (i - 128));
				}


				//QTableの作成(0.1～20.0まで)
				if (i < 256 / 8 * 3)
				{
					QTable[i] = (float)(1.0 / (256 / 8 * 3) * (i + 1)); // 0-95 : 0.01041667 ～ 1.0
				}
				else if (i < 256 / 8 * 6)
				{
					QTable[i] = (float)(10.0 / (256 / 8 * 3) * (i + 1 - 256 / 8 * 3) + 1.0); // 96-191 : 1.104167 ～ 11.0
				}
				else
				{
					QTable[i] = (float)(10.0 / (256 / 8 * 2) * (i + 1 - 256 / 8 * 6) + 11.0); // 192-255 : 11.15625 ～ 21.0
				}
			}
		}

	}
}
