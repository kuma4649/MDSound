using System;
using System.Collections.Generic;
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

		private float[] freqTable;
		private float[] gainTable;
		private float[] QTable;

		public eq3band(int samplerate = 44100)
		{
			this.samplerate = samplerate;
			makeTable();
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
					lowfreq = freqTable[data];
					break;
				case 2:
					lowgain = gainTable[data];
					break;
				case 3:
					lowQ = QTable[data];
					break;

				case 4:
					midSw = data != 0;
					break;
				case 5:
					midfreq = freqTable[data];
					break;
				case 6:
					midgain = gainTable[data];
					break;
				case 7:
					midQ = QTable[data];
					break;

				case 8:
					highSw = data != 0;
					break;
				case 9:
					highfreq = freqTable[data];
					break;
				case 10:
					highgain = gainTable[data];
					break;
				case 11:
					highQ = QTable[data];
					break;
			}

			updateParam();
		}



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

		private void makeTable()
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
					QTable[i] = (float)(1.0 / (256 / 8 * 3) * (i+1)); // 0-95 : 0.01041667 ～ 1.0
				}
				else if (i < 256 / 8 * 6)
				{
					QTable[i] = (float)(10.0 / (256 / 8 * 3) * (i+1 - 256 / 8 * 3) + 1.0); // 96-191 : 1.104167 ～ 11.0
				}
				else
				{
					QTable[i] = (float)(10.0 / (256 / 8 * 2) * (i + 1 - 256 / 8 * 6) + 11.0); // 192-255 : 11.15625 ～ 21.0
				}
			}
		}

	}
}
