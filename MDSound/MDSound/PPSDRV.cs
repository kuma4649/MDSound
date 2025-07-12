using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MDSound
{
    public class PPSDRV : Instrument
    {
        public override string Name { get => "PPSDRV"; set { } }
        public override string ShortName { get => "PPSDRV"; set { } }

        public override void Reset(byte ChipID)
        {
            ppsDt = new byte[][] { null, null };
            ppsHd = new PPSHeader[][] { null, null };
            single_flag=new bool[2]; // 単音モードか？
            low_cpu_check_flag = new bool[2];// 周波数半分で再生か？
            keyon_flag = new bool[2]; // Keyon 中か？
            data_offset1 = new int[2] { -1, -1 };
            data_offset2 = new int[2] { -1, -1 };
            data_xor1 = new int[2];                              // 現在の位置(小数部)
            data_xor2 = new int[2];                              // 現在の位置(小数部)
            tick1 = new int[2];
            tick2 = new int[2];
            tick_xor1 = new int[2];
            tick_xor2 = new int[2];
            data_size1 = new int[2] { -1, -1 };
            data_size2 = new int[2] { -1, -1 };
            volume1 = new int[2];
            volume2 = new int[2];
            keyoff_vol = new int[2];
            EmitTable = new int[][] { new int[16], new int[16] };
            interpolation = new bool[] { true, true };
            SetVolume(ChipID, 0);
        }

        public override uint Start(byte ChipID, uint clock)
        {
            return Start(ChipID, clock, 0);
        }

        public override uint Start(byte ChipID, uint clock, uint ClockValue, params object[] option)
        {
            this.SamplingRate = (double)clock;
            Reset(ChipID);

            if (option != null && option.Length > 0)
            {
                real = true;
                psg = (Action<int, int>)option[0];
            }

            return clock;
        }

        public override void Stop(byte ChipID)
        {
        }

        public override int Write(byte ChipID, int port, int adr, int data)
        {
            switch ((byte)port)
            {
                case 0x00:
                    Reset(ChipID);
                    break;
                case 0x01:
                    Play(ChipID, (byte)(adr >> 8), (byte)adr, (byte)data);
                    break;
                case 0x02:
                    Stop(ChipID);
                    break;
                case 0x03:
                    return SetParam(ChipID, (byte)adr, (byte)data) ? 1 : 0;
                case 0x04:
                    int04();
                    break;
            }

            return 0;
        }


        public class PPSHeader
        {
            public int address;
            public int length;
            public byte toneofs;
            public byte volumeofs;
        }

        private double SamplingRate = 44100.0;
        private int MAX_PPS = 14;
        private byte[][] ppsDt = null;
        private PPSHeader[][] ppsHd = null;
        private bool[] single_flag; // 単音モードか？
        private bool[] low_cpu_check_flag;// 周波数半分で再生か？
        private bool[] keyon_flag = new bool[2]; // Keyon 中か？
        private int[] data_offset1;
        private int[] data_offset2;
        private int[] data_xor1;                              // 現在の位置(小数部)
        private int[] data_xor2;                              // 現在の位置(小数部)
        private int[] tick1;
        private int[] tick2;
        private int[] tick_xor1;
        private int[] tick_xor2;
        private int[] data_size1;
        private int[] data_size2;
        private int[] volume1;
        private int[] volume2;
        private int[] keyoff_vol = new int[2];
        private int[][] EmitTable = new int[][] { new int[16], new int[16] };
        private bool[] interpolation = new bool[] { true, true };
        private bool real = false;
        private Action<int, int> psg = null;
        private static int[] table=new int[16 * 16] {
         0, 0, 0, 5, 9,10,11,12,13,13,14,14,14,15,15,15,
         0, 0, 3, 5, 9,10,11,12,13,13,14,14,14,15,15,15,
         0, 3, 5, 7, 9,10,11,12,13,13,14,14,14,15,15,15,
         5, 5, 7, 9,10,11,12,13,13,13,14,14,14,15,15,15,
         9, 9, 9,10,11,12,12,13,13,14,14,14,15,15,15,15,
        10,10,10,11,12,12,13,13,13,14,14,14,15,15,15,15,
        11,11,11,12,12,13,13,13,14,14,14,14,15,15,15,15,
        12,12,12,12,13,13,13,14,14,14,14,15,15,15,15,15,
        13,13,13,13,13,13,14,14,14,14,14,15,15,15,15,15,
        13,13,13,13,14,14,14,14,14,14,15,15,15,15,15,15,
        14,14,14,14,14,14,14,14,14,15,15,15,15,15,15,15,
        14,14,14,14,14,14,14,15,15,15,15,15,15,15,15,15,
        14,14,14,14,15,15,15,15,15,15,15,15,15,15,15,15,
        15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,
        15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,
        15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15
        };

        //-----------------------------------------------------------------------------
        //	音量設定
        //-----------------------------------------------------------------------------
        private void SetVolume(byte ChipID,int vol)                 // 音量設定
        {
            //	psg.SetVolume(vol);

            double _base = 0x4000 * 2 / 3.0 * Math.Pow(10.0, vol / 40.0);
            for (int i = 15; i >= 1; i--)
            {
                EmitTable[ChipID][i] = (int)(_base);
                _base /= 1.189207115;
            }
            EmitTable[ChipID][0] = 0;

        }

        private void Play(byte ChipID, byte al, byte bh, byte bl)
        {
            int num = al;
            int shift = (sbyte)bh;
            //Console.WriteLine(bh);
            int volshift = (sbyte)bl;

            if (ppsHd[ChipID][num].address < 0) return;

            int a = 225 + ppsHd[ChipID][num].toneofs;
            a %= 256;
            a += shift;
            a = Math.Min(Math.Max(a, 1), 255);

            if ((byte)(ppsHd[ChipID][num].volumeofs + volshift) >= 15) return;
            // 音量が０以下の時は再生しない

            if (single_flag[ChipID] == false && keyon_flag[ChipID])
            {
                //	２重発音処理
                volume2[ChipID] = volume1[ChipID];                  // １音目を２音目に移動
                data_offset2[ChipID] = data_offset1[ChipID];
                data_size2[ChipID] = data_size1[ChipID];
                data_xor2[ChipID] = data_xor1[ChipID];
                tick2[ChipID] = tick1[ChipID];
                tick_xor2[ChipID] = tick_xor1[ChipID];
            }
            else
            {
                //	１音目で再生
                data_size2[ChipID] = -1;                     // ２音目は停止中
            }

            volume1[ChipID] = ppsHd[ChipID][num].volumeofs + volshift;
            data_offset1[ChipID] = ppsHd[ChipID][num].address;
            data_size1[ChipID] = ppsHd[ChipID][num].length;    // １音目を消して再生
            data_xor1[ChipID] = 0;
            if (low_cpu_check_flag[ChipID])
            {
                tick1[ChipID] = (int)(((8000 * a / 225) << 16) / SamplingRate);
                tick_xor1[ChipID] = tick1[ChipID] & 0xffff;
                tick1[ChipID] >>= 16;
            }
            else
            {
                tick1[ChipID] = (int)(((16000 * a / 225) << 16) / SamplingRate);
                tick_xor1[ChipID] = tick1[ChipID] & 0xffff;
                tick1[ChipID] >>= 16;
            }

            //	psg.SetReg(0x07, psg.GetReg(0x07) | 0x24);	// Tone/Noise C off
            keyon_flag[ChipID] = true;                      // 発音開始
            return;
        }

        private void stop(byte ChipID)
        {
            keyon_flag[ChipID] = false;
            data_offset1[ChipID] = data_offset2[ChipID] = -1;
            data_size1[ChipID] = data_size2[ChipID] = -1;
        }

        private bool SetParam(byte ChipID, byte paramno, byte data)
        {
            switch (paramno & 1)
            {
                case 0:
                    single_flag[ChipID] = data != 0;
                    return true;
                case 1:
                    low_cpu_check_flag[ChipID] = data != 0;
                    return true;
                default: return false;
            }
        }

        private void int04()
        {
            //TODO: 未実装
        }

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {
            int i, al1, al2, ah1, ah2;
            int data=0;

            if (keyon_flag[ChipID] == false && keyoff_vol[ChipID] == 0)
            {
                return;
            }

            for (i = 0; i < samples; i++)
            {

                if (data_size1[ChipID] > 1)
                {
                    al1 = ppsDt[ChipID][data_offset1[ChipID]] - volume1[ChipID];
                    al2 = ppsDt[ChipID][data_offset1[ChipID] + 1] - volume1[ChipID];
                    if (al1 < 0) al1 = 0;
                    if (al2 < 0) al2 = 0;
                }
                else
                {
                    al1 = al2 = 0;
                }

                if (data_size2[ChipID] > 1)
                {
                    ah1 = ppsDt[ChipID][data_offset2[ChipID]] - volume2[ChipID];
                    ah2 = ppsDt[ChipID][data_offset2[ChipID] + 1] - volume2[ChipID];
                    if (ah1 < 0) ah1 = 0;
                    if (ah2 < 0) ah2 = 0;
                }
                else
                {
                    ah1 = ah2 = 0;
                }

                if (real)
                {
                    al1 = table[(al1 << 4) + ah1];
                    psg(0x0a, al1);
                }
                else
                {
                    if (interpolation[ChipID])
                    {
                        data =
                            (EmitTable[ChipID][al1] * (0x10000 - data_xor1[ChipID]) + EmitTable[ChipID][al2] * data_xor1[ChipID] +
                            EmitTable[ChipID][ah1] * (0x10000 - data_xor2[ChipID]) + EmitTable[ChipID][ah2] * data_xor2[ChipID]) / 0x10000;

                    }
                    else
                    {
                        data = EmitTable[ChipID][al1] + EmitTable[ChipID][ah1];
                    }
                }

                keyoff_vol[ChipID] = (keyoff_vol[ChipID] * 255) / 258;

                if (!real)
                {
                    if (!keyon_flag[ChipID]) data += keyoff_vol[ChipID];
                    //if(keyoff_vol!=0) Console.WriteLine("keyoff_vol{0}", keyoff_vol);
                    outputs[0][i] = (short)Math.Max(Math.Min(outputs[0][i] + data, short.MaxValue), short.MinValue);
                    outputs[1][i] = (short)Math.Max(Math.Min(outputs[1][i] + data, short.MaxValue), short.MinValue);
                }

                //		psg.Mix(dest, 1);
                //		dest += 2;

                if (data_size2[ChipID] > 1)
                {   // ２音合成再生
                    data_xor2[ChipID] += tick_xor2[ChipID];
                    if (data_xor2[ChipID] >= 0x10000)
                    {
                        data_size2[ChipID]--;
                        data_offset2[ChipID]++;
                        data_xor2[ChipID] -= 0x10000;
                    }
                    data_size2[ChipID] -= tick2[ChipID];
                    data_offset2[ChipID] += tick2[ChipID];

                    if (low_cpu_check_flag[ChipID])
                    {
                        data_xor2[ChipID] += tick_xor2[ChipID];
                        if (data_xor2[ChipID] >= 0x10000)
                        {
                            data_size2[ChipID]--;
                            data_offset2[ChipID]++;
                            data_xor2[ChipID] -= 0x10000;
                        }
                        data_size2[ChipID] -= tick2[ChipID];
                        data_offset2[ChipID] += tick2[ChipID];
                    }
                }

                data_xor1[ChipID] += tick_xor1[ChipID];
                if (data_xor1[ChipID] >= 0x10000)
                {
                    data_size1[ChipID]--;
                    data_offset1[ChipID]++;
                    data_xor1[ChipID] -= 0x10000;
                }
                data_size1[ChipID] -= tick1[ChipID];
                data_offset1[ChipID] += tick1[ChipID];

                if (low_cpu_check_flag[ChipID])
                {
                    data_xor1[ChipID] += tick_xor1[ChipID];
                    if (data_xor1[ChipID] >= 0x10000)
                    {
                        data_size1[ChipID]--;
                        data_offset1[ChipID]++;
                        data_xor1[ChipID] -= 0x10000;
                    }
                    data_size1[ChipID] -= tick1[ChipID];
                    data_offset1[ChipID] += tick1[ChipID];
                }

                if (data_size1[ChipID] <= 1 && data_size2[ChipID] <= 1)
                {       // 両方停止
                    if (keyon_flag[ChipID])
                    {
                        int ad = data_size1[ChipID] - 1;
                        if (ad >= 0 && ad < ppsDt[ChipID].Length)
                            keyoff_vol[ChipID] += EmitTable[ChipID][ppsDt[ChipID][ad]] / 8;
                    }
                    keyon_flag[ChipID] = false;     // 発音停止
                    if (real)
                    {
                        psg(0x0a, 0);	// Volume を0に
                    }
                }
                else if (data_size1[ChipID] <= 1 && data_size2[ChipID] > 1)
                {   // １音目のみが停止
                    volume1[ChipID] = volume2[ChipID];
                    data_size1[ChipID] = data_size2[ChipID];
                    data_offset1[ChipID] = data_offset2[ChipID];
                    data_xor1[ChipID] = data_xor2[ChipID];
                    tick1[ChipID] = tick2[ChipID];
                    tick_xor1[ChipID] = tick_xor2[ChipID];
                    data_size2[ChipID] = -1;

                    int ad = data_size1[ChipID] - 1;
                    if (ad >= 0 && ad < ppsDt[ChipID].Length)
                        keyoff_vol[ChipID] += EmitTable[ChipID][ppsDt[ChipID][ad]] / 8;

                }
                else if (data_size1[ChipID] > 1 && data_size2[ChipID] < 1)
                {   // ２音目のみが停止
                    if (data_offset2[ChipID] != -1)
                    {
                        int ad = data_size2[ChipID] - 1;
                        if (ad >= 0 && ad < ppsDt[ChipID].Length)
                            keyoff_vol[ChipID] += EmitTable[ChipID][ppsDt[ChipID][ad]] / 8;
                        data_offset2[ChipID] = -1;
                    }
                }

            }
        }

        public int Load(byte ChipID, byte[] pcmData)
        {
            if (pcmData == null || pcmData.Length <= MAX_PPS * 6) return -1;

            List<byte> o = new List<byte>();

            //仮バッファに読み込み
            for (int i = MAX_PPS * 6; i < pcmData.Length; i++)
            {
                o.Add((byte)((pcmData[i] >> 4) & 0xf));
                o.Add((byte)((pcmData[i] >> 0) & 0xf));
            }

            //データの作成
            //	PPS 補正(プチノイズ対策）／160 サンプルで減衰させる
            for (int i = 0; i < MAX_PPS; i++)
            {
                int address = pcmData[i * 6 + 0] + pcmData[i * 6 + 1] * 0x100 - MAX_PPS * 6;
                int leng = pcmData[i * 6 + 2] + pcmData[i * 6 + 3] * 0x100;

                //仮バッファは２倍の大きさにしている為。
                address *= 2;
                leng *= 2;

                int end_pps = address + leng;
                int start_pps = end_pps - 160;//160サンプル
                if (start_pps < address) start_pps = address;

                for (int j = start_pps; j < end_pps; j++)
                {
                    //Console.WriteLine("before{0}",o[j]);
                    o[j] = (byte)(o[j] - (j - start_pps) * 16 / (end_pps - start_pps));
                    if ((sbyte)o[j] < 0)
                        o[j] = 0;
                    //Console.WriteLine("after{0}", o[j]);
                }

            }
            ppsDt[ChipID] = o.ToArray();

            //ヘッダの作成
            List<PPSHeader> h = new List<PPSHeader>();
            for (int i = 0; i < MAX_PPS; i++)
            {
                PPSHeader p = new PPSHeader();
                p.address = (pcmData[i * 6 + 0] + pcmData[i * 6 + 1] * 0x100 - MAX_PPS * 6) * 2;
                p.length = (pcmData[i * 6 + 2] + pcmData[i * 6 + 3] * 0x100) * 2;
                p.toneofs = pcmData[i * 6 + 4];
                p.volumeofs = pcmData[i * 6 + 5];

                h.Add(p);
            }
            ppsHd[ChipID] = h.ToArray();

            return 0;
        }

    }
}
