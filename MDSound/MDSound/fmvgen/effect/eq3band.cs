using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace MDSound.fmvgen.effect
{
	//
	// 3バンドイコライザー(https://vstcpp.wpblog.jp/?p=1417 より)
	//

	public class eq3band
	{
		private float fl, fr;
		private int samplerate = 44100;

		// エフェクターのパラメーター
		private bool lowSw = false;
		private float lowfreq = 400.0f; // 低音域の周波数。50Hz～1kHz程度
		private float lowgain = 2.0f;   // 低音域のゲイン(増幅値)。-15～15dB程度
		private float lowQ = (float)(1.0f / Math.Sqrt(2.0f));

		private bool midSw = false;
		private float midfreq = 1000.0f; // 中音域の周波数。500Hz～4kHz程度
		private float midgain = -4.0f;   // 中音域のゲイン(増幅値)。-15～15dB程度
		private float midQ = (float)(1.0f / Math.Sqrt(2.0f));

		private bool highSw = false;
		private float highfreq = 4000.0f; // 高音域の周波数。1kHz～12kHz程度
		private float highgain = 4.0f;    // 高音域のゲイン(増幅値)。-15～15dB程度
		private float highQ = (float)(1.0f / Math.Sqrt(2.0f));

		//パラメータのdefault値は
		//low
		// freq:126
		// gain:141
		// Q:67
		//mid
		// freq:162
		// gain:102
		// Q:67
		//high
		// freq:192
		// gain:154
		// Q:67


		// 内部変数
		private CMyFilter lowL = new CMyFilter(), lowR = new CMyFilter();
		private CMyFilter midL = new CMyFilter(), midR = new CMyFilter();
		private CMyFilter highL = new CMyFilter(), highR = new CMyFilter(); // フィルタークラス(https://vstcpp.wpblog.jp/?page_id=728 より)


		public eq3band(int samplerate = 44100)
		{
			this.samplerate = samplerate;
			if (CMyFilter.freqTable == null) CMyFilter.makeTable();
			updateParam();
		}

		public void Mix(int[] buffer, int nsamples)
		{
			for (int i = 0; i < nsamples; i++)
			{
				fl = buffer[i * 2 + 0] / CMyFilter.convInt;
				fr = buffer[i * 2 + 1] / CMyFilter.convInt;


				// inL[]、inR[]、outL[]、outR[]はそれぞれ入力信号と出力信号のバッファ(左右)
				// wavelenghtはバッファのサイズ、サンプリング周波数は44100Hzとする
				// 入力信号にエフェクトをかける
				// 入力信号にフィルタをかける
				if (lowSw)
				{
					fl = lowL.Process(fl);
					fr = lowR.Process(fr);
				}
				if (midSw)
				{
					fl = midL.Process(fl);
					fr = midR.Process(fr);
				}
				if (highSw)
				{
					fl = highL.Process(fl);
					fr = highR.Process(fr);
				}


				buffer[i * 2 + 0] = (int)(fl * CMyFilter.convInt);
				buffer[i * 2 + 1] = (int)(fr * CMyFilter.convInt);
			}
		}

		public void SetReg(uint adr, byte data)
		{
			switch (adr & 0xf)
			{
				case 0:
					lowSw = data != 0;
					break;
				case 1:
					lowfreq = CMyFilter.freqTable[data];
					break;
				case 2:
					lowgain = CMyFilter.gainTable[data];
					break;
				case 3:
					lowQ = CMyFilter.QTable[data];
					break;

				case 4:
					midSw = data != 0;
					break;
				case 5:
					midfreq = CMyFilter.freqTable[data];
					break;
				case 6:
					midgain = CMyFilter.gainTable[data];
					break;
				case 7:
					midQ = CMyFilter.QTable[data];
					break;

				case 8:
					highSw = data != 0;
					break;
				case 9:
					highfreq = CMyFilter.freqTable[data];
					break;
				case 10:
					highgain = CMyFilter.gainTable[data];
					break;
				case 11:
					highQ = CMyFilter.QTable[data];
					break;
			}

			updateParam();
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void updateParam()
		{
			// 低音域を持ち上げる(ローシェルフ)フィルタ設定(左右分)
			lowL.LowShelf(lowfreq, lowQ, lowgain, samplerate);
			lowR.LowShelf(lowfreq, lowQ, lowgain, samplerate);
			// 中音域を持ち上げる(ピーキング)フィルタ設定(左右分)
			midL.Peaking(midfreq, midQ, midgain, samplerate);
			midL.Peaking(midfreq, midQ, midgain, samplerate);
			// 高音域を持ち上げる(ローシェルフ)フィルタ設定(左右分)
			highL.HighShelf(highfreq, highQ, highgain, samplerate);
			highR.HighShelf(highfreq, highQ, highgain, samplerate);
		}


	}
}
