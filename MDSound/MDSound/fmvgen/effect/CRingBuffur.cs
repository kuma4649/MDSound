using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace MDSound.fmvgen
{
	public class CRingBuffur
	{
		private int rpos; // 読み込み位置
		private int wpos; // 書き込み位置
		private float[] buf = null;// RB_SIZE]; // 内部バッファ
		private int RB_SIZE = 44100 * 4;

		public CRingBuffur(int clock, float RB = 4.0f)
		{
			// 初期化を行う
			RB_SIZE = (int)(clock * RB);
			rpos = 0;
			wpos = (int)(RB_SIZE / 2.0); // とりあえずバッファサイズの半分ぐらいにしておく

			buf = new float[RB_SIZE];
		}

		// 読み込み位置と書き込み位置の間隔を設定する関数
		// ディレイエフェクターの場合はそのまま遅延時間(ディレイタイム)になる
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetInterval(int interval)
		{
			// 読み込み位置と書き込み位置の間隔を設定

			// 値が0以下やバッファサイズ以上にならないよう処理
			interval = interval % RB_SIZE;
			if (interval <= 0) { interval = 1; }

			// 書き込み位置を読み込み位置からinterval分だけ離して設定
			wpos = (rpos + interval) % RB_SIZE;
		}

		// 内部バッファの読み込み位置(rpos)のデータを読み込む関数
		// 引数のposは読み込み位置(rpos)からの相対位置
		// (相対位置(pos)はコーラスやピッチシフタなどのエフェクターで利用する)
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float Read(int pos = 0)
		{
			// 読み込み位置(rpos)と相対位置(pos)から実際に読み込む位置を計算する。
			int tmp = rpos + pos;
			while (tmp < 0)
			{
				tmp += RB_SIZE;
			}
			tmp %= RB_SIZE; // バッファサイズ以上にならないよう処理

			// 読み込み位置の値を返す
			return buf[tmp];
		}

		// 内部バッファの書き込み位置(wpos)にデータを書き込む関数
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Write(float in_)
		{
			// 書き込み位置(wpos)に値を書き込む
			buf[wpos] = in_;
		}

		// 内部バッファの読み込み位置(rpos)、書き込み位置(wpos)を一つ進める関数
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Update()
		{
			// 内部バッファの読み込み位置(rpos)、書き込み位置(wpos)を一つ進める
			rpos = (rpos + 1) % RB_SIZE;
			wpos = (wpos + 1) % RB_SIZE;
		}
	}
}
