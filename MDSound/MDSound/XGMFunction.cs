using System;
using System.Collections.Generic;
using System.Text;

namespace MDSound
{
    public class XGMFunction
    {
        public class XGMPCM
        {
            public uint Priority = 0;
            public uint startAddr = 0;
            public uint endAddr = 0;
            public uint addr = 0;
            public uint inst = 0;
            public bool isPlaying = false;
            public byte data = 0;
        }

        public class XGMSampleID
        {
            public uint addr = 0;
            public uint size = 0;
        }

        //各チャンネルの情報
        public XGMPCM[][] xgmpcm = new XGMPCM[][] { new XGMPCM[4], new XGMPCM[4] };
        //PCMデータ群
        public byte[][] pcmBuf = new byte[][] { null, null };
        //PCMテーブル
        public XGMSampleID[][] sampleID = new XGMSampleID[][] { new XGMSampleID[63], new XGMSampleID[63] };

        private double[] pcmStep = new double[2];
        private double[] pcmExecDelta = new double[2];
        private byte[] DACEnable = new byte[] { 0, 0 };
        private object[] lockobj = new object[] { new object(), new object() };
        private bool ox2b = false;

        public void Reset(byte ChipID,int sampleRate)
        {
            pcmStep[ChipID] = sampleRate / 14000.0;
            Stop(ChipID);
        }

        public void Stop(byte ChipID)
        {
            pcmExecDelta[ChipID] = 0.0;
            DACEnable[ChipID] = 0;
            ox2b = false;

            for (int i = 0; i < 4; i++)
            {
                if (xgmpcm[ChipID][i] == null) xgmpcm[ChipID][i] = new XGMPCM();
                xgmpcm[ChipID][i].isPlaying = false;
            }
            for (int i = 0; i < 63; i++) sampleID[ChipID][i] = new XGMSampleID();
        }

        public void Write(byte ChipID, int port, int adr, int data)
        {
            //
            // OPN2はアドレスとデータが二回に分けて送信されるタイプ
            // 一回目 アドレス (adr = 0)
            // 一回目 データ   (adr = 1)
            //

            if (port + adr == 0)
            {
                //0x2b : DACのスイッチが含まれるアドレス
                if (data == 0x2b) ox2b = true;
                else ox2b = false;
            }
            if (ox2b && port == 0 && adr == 1)
            {
                //0x80 : DACのスイッチを意味するbit7(1:ON 0:OFF)
                DACEnable[ChipID] = (byte)(data & 0x80);
                ox2b = false;
            }
        }

        public void Update(byte ChipID, int samples,Func<byte, int, int, int, int> Write)
        {
            for (int i = 0; i < samples; i++)
            {
                while ((int)pcmExecDelta[ChipID] <= 0)
                {
                    Write(ChipID, 0, 0, 0x2a);
                    Write(ChipID, 0, 1, oneFramePCM(ChipID));
                    pcmExecDelta[ChipID] += pcmStep[ChipID];
                }
                pcmExecDelta[ChipID] -= 1.0;
            }
        }

        public void PlayPCM(byte ChipID, byte X, byte id)
        {
            byte priority = (byte)(X & 0xc);
            byte channel = (byte)(X & 0x3);

            lock (lockobj[ChipID])
            {
                //優先度が高い場合または消音中の場合のみ発音できる
                if (xgmpcm[ChipID][channel].Priority > priority && xgmpcm[ChipID][channel].isPlaying) return;

                if (id == 0 || sampleID[ChipID][id - 1].size == 0)
                {
                    //IDが0の場合や、定義されていないIDが指定された場合は発音を停止する
                    xgmpcm[ChipID][channel].Priority = 0;
                    xgmpcm[ChipID][channel].isPlaying = false;
                    return;
                }

                //発音開始指示
                xgmpcm[ChipID][channel].Priority = priority;
                xgmpcm[ChipID][channel].startAddr = sampleID[ChipID][id - 1].addr;
                xgmpcm[ChipID][channel].endAddr = sampleID[ChipID][id - 1].addr + sampleID[ChipID][id - 1].size;
                xgmpcm[ChipID][channel].addr = sampleID[ChipID][id - 1].addr;
                xgmpcm[ChipID][channel].inst = id;
                xgmpcm[ChipID][channel].isPlaying = true;
            }
        }

        private short oneFramePCM(byte ChipID)
        {
            if (DACEnable[ChipID] == 0) return 0x80;//0x80 : 無音状態(...というよりも波形の中心となる場所?)

            //波形合成
            short o = 0;
            lock (lockobj[ChipID])
            {
                for (int i = 0; i < 4; i++)
                {
                    if (!xgmpcm[ChipID][i].isPlaying) continue;
                    sbyte d = xgmpcm[ChipID][i].addr < pcmBuf[ChipID].Length ? (sbyte)pcmBuf[ChipID][xgmpcm[ChipID][i].addr++] : (sbyte)0;
                    o += d;
                    xgmpcm[ChipID][i].data = (byte)Math.Abs((int)d);
                    if (xgmpcm[ChipID][i].addr >= xgmpcm[ChipID][i].endAddr)
                    {
                        xgmpcm[ChipID][i].isPlaying = false;
                        xgmpcm[ChipID][i].data = 0;
                    }
                }
            }

            o = Math.Min(Math.Max(o, (short)(sbyte.MinValue + 1)), (short)(sbyte.MaxValue)); //クリッピング
            o += 0x80;//OPN2での中心の位置に移動する

            return o;
        }

    }
}
