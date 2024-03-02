using System;
using System.Collections.Generic;
using System.Reflection;
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

        public class XGM2PCM
        {
            public uint Priority = 0;
            public uint Speed = 0;
            public uint SpeedWait = 0;
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
        public XGM2PCM[][] xgm2pcm = new XGM2PCM[][] { new XGM2PCM[4], new XGM2PCM[4] };
        //PCMデータ群
        public byte[][] pcmBuf = new byte[][] { null, null };
        //PCMテーブル
        public XGMSampleID[][] sampleID = new XGMSampleID[][] { new XGMSampleID[63], new XGMSampleID[63] };

        private double[] pcmStep = new double[2];
        private double[] pcmExecDelta = new double[2];
        private byte[] DACEnable = new byte[] { 0, 0 };
        private object[] lockobj = new object[] { new object(), new object() };
        private bool ox2b = false;
        private int mode = 0;
        private int sampleRate = 44100;

        public void Reset(byte ChipID,int sampleRate)
        {
            this.sampleRate = sampleRate;
            pcmStep[ChipID] = sampleRate / 14000.0;
            Stop(ChipID);
        }

        public void Stop(byte ChipID)
        {
            pcmExecDelta[ChipID] = 0.0;
            DACEnable[ChipID] = 0;
            ox2b = false;
            mode = 0;

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
                    Write(ChipID, 0, 1, mode == 0 ? oneFramePCM(ChipID) : oneFramePCMxgm2(ChipID));
                    pcmExecDelta[ChipID] += pcmStep[ChipID];
                }
                pcmExecDelta[ChipID] -= 1.0;
            }
        }

        public void PlayPCM(byte ChipID, byte p, byte X, byte id)
        {
            if (p == 10) PlayPCMxgm(ChipID, X, id);
            else if (p == 11) PlayPCMxgm2(ChipID, X, id);
        }

        public void PlayPCMxgm(byte ChipID, byte X, byte id)
        {
            byte priority = (byte)(X & 0xc);
            byte channel = (byte)(X & 0x3);
            mode = 0;
            pcmStep[ChipID] = sampleRate / 14000.0;

            lock (lockobj[ChipID])
            {
                //優先度が高い場合または消音中の場合のみ発音できる
                if (xgmpcm[ChipID][channel].Priority > priority && xgmpcm[ChipID][channel].isPlaying) return;

                if (id == 0 || id > sampleID[ChipID].Length || sampleID[ChipID][id - 1].size == 0)
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

        public void PlayPCMxgm2(byte ChipID, byte X, byte id)
        {
            mode = 1;
            pcmStep[ChipID] = sampleRate / 13300.0;
            if (id != 0 && sampleID[ChipID].Length <= (id - 1) && sampleID[ChipID][id - 1].addr == 0xffff00) return;

            byte priority = (byte)(X & 0x8);
            byte speed = (byte)(X & 0x4);
            byte channel = (byte)(X & 0x3);

            lock (lockobj[ChipID])
            {
                //優先度が高い場合または消音中の場合のみ発音できる
                if (xgm2pcm[ChipID][channel].Priority > priority && xgm2pcm[ChipID][channel].isPlaying) return;

                if (id == 0 || sampleID[ChipID][id - 1].size == 0)
                {
                    //IDが0の場合や、定義されていないIDが指定された場合は発音を停止する
                    xgm2pcm[ChipID][channel].Priority = 0;
                    xgm2pcm[ChipID][channel].isPlaying = false;
                    return;
                }

                xgm2pcm[ChipID][channel].Priority = priority;
                xgm2pcm[ChipID][channel].Speed = speed;
                xgm2pcm[ChipID][channel].SpeedWait = 0;
                xgm2pcm[ChipID][channel].startAddr = (uint)(sampleID[ChipID][id - 1].addr);
                xgm2pcm[ChipID][channel].endAddr = (uint)(sampleID[ChipID][id - 1].addr + sampleID[ChipID][id - 1].size);
                xgm2pcm[ChipID][channel].addr = sampleID[ChipID][id - 1].addr;
                xgm2pcm[ChipID][channel].inst = id;
                xgm2pcm[ChipID][channel].isPlaying = true;

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

        private short oneFramePCMxgm2(byte ChipID)
        {
            if (DACEnable[ChipID] == 0) return 0x80;

            short o = 0;
            lock (lockobj[ChipID])
            {

                for (int i = 0; i < 4; i++)
                {
                    if (!xgm2pcm[ChipID][i].isPlaying) continue;
                    if (xgm2pcm[ChipID][i].addr == pcmBuf[ChipID].Length)
                    {
                        xgm2pcm[ChipID][i].isPlaying = false;
                        xgm2pcm[ChipID][i].data = 0;
                        continue;
                    }
                    sbyte d = (sbyte)pcmBuf[ChipID][xgm2pcm[ChipID][i].addr];
                    if (xgm2pcm[ChipID][i].Speed == 0) xgm2pcm[ChipID][i].addr++;
                    else
                    {
                        xgm2pcm[ChipID][i].SpeedWait++;
                        xgm2pcm[ChipID][i].SpeedWait %= 2;
                        if (xgm2pcm[ChipID][i].SpeedWait == 0) xgm2pcm[ChipID][i].addr++;
                    }
                    o += (short)d;
                    xgm2pcm[ChipID][i].data = (byte)Math.Abs((int)d);
                    if (xgm2pcm[ChipID][i].addr >= xgm2pcm[ChipID][i].endAddr)
                    {
                        xgm2pcm[ChipID][i].isPlaying = false;
                        xgm2pcm[ChipID][i].data = 0;
                    }
                }
                o = Math.Min(Math.Max(o, (short)(sbyte.MinValue + 1)), (short)(sbyte.MaxValue));
                o += 0x80;
            }

            return o;
        }

    }
}
