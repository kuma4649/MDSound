using System;
using System.Collections.Generic;
using System.Text;

namespace MDSound
{
    public class YM2612X : ym2612
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

        public XGMPCM[][] xgmpcm = new XGMPCM[][] { new XGMPCM[4], new XGMPCM[4] };
        public byte[] pcmBuf = null;
        public XGMSampleID[][] sampleID = new XGMSampleID[][] { new XGMSampleID[63], new XGMSampleID[63] };

        private double[] pcmStep=new double[2];
        private double[] pcmExecDelta=new double[2];
        private byte[] DACEnable = new byte[] { 0, 0 };
        private object[] lockobj = new object[] { new object(), new object() };

        public override void Reset(byte ChipID)
        {
            pcmStep[ChipID] = YM2612_Chip[ChipID].Rate / 14000.0;
            pcmExecDelta[ChipID] = 0.0;
            DACEnable[ChipID] = 0;

            for (int i = 0; i < 4; i++) xgmpcm[ChipID][i] = new XGMPCM();
            for (int i = 0; i < 63; i++) sampleID[ChipID][i] = new XGMSampleID();
        }

        public override int Write(byte ChipID, int port, int adr, int data)
        {
            if (adr == 0x2b) DACEnable[ChipID] = (byte)(data & 0x80);
            return base.Write(ChipID, port, (byte)adr, (byte)data);
        }

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {
            for (int i = 0; i < samples; i++)
            {
                if (pcmExecDelta[ChipID] <= 0.0)
                {
                    oneFramePCM(ChipID);
                    pcmExecDelta[ChipID] += pcmStep[ChipID];
                }
                else
                {
                    pcmExecDelta[ChipID] -= 1.0;
                }
            }

            base.Update(ChipID, outputs, samples);
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

                xgmpcm[ChipID][channel].Priority = priority;
                xgmpcm[ChipID][channel].startAddr = sampleID[ChipID][id - 1].addr;
                xgmpcm[ChipID][channel].endAddr = sampleID[ChipID][id - 1].addr + sampleID[ChipID][id - 1].size;
                xgmpcm[ChipID][channel].addr = sampleID[ChipID][id - 1].addr;
                xgmpcm[ChipID][channel].inst = id;
                xgmpcm[ChipID][channel].isPlaying = true;
            }
        }

        private void oneFramePCM(byte ChipID)
        {
            if (DACEnable[ChipID] == 0) return;

            short o = 0;

            lock (lockobj[ChipID])
            {
                for (int i = 0; i < 4; i++)
                {
                    if (!xgmpcm[ChipID][i].isPlaying) continue;
                    sbyte d = xgmpcm[ChipID][i].addr < pcmBuf.Length ? (sbyte)pcmBuf[xgmpcm[ChipID][i].addr++] : (sbyte)0;
                    o += d;
                    xgmpcm[ChipID][i].data = (byte)Math.Abs((int)d);
                    if (xgmpcm[ChipID][i].addr >= xgmpcm[ChipID][i].endAddr)
                    {
                        xgmpcm[ChipID][i].isPlaying = false;
                        xgmpcm[ChipID][i].data = 0;
                    }
                }
            }

            o = Math.Min(Math.Max(o, (short)(sbyte.MinValue + 1)), (short)(sbyte.MaxValue));
            o += 0x80;

            base.Write(ChipID, 0, 0x2a, o);
        }

    }
}
