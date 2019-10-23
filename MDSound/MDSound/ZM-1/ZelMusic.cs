using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDSound.ZM_1
{
    public class ZelMusic : Instrument
    {
        public override string Name { get { return "ZelMusic"; } set { } }
        public override string ShortName { get { return "ZM-1"; } set { } }

        public const int MAX_OPERATOR = 48;
        public const long MAX_PCMDATASIZE = 0x1_0000_0000;

        private Operator[][] ope = null;

        private commonParam[] cp = new commonParam[2] { new commonParam(), new commonParam() };

        public class commonParam
        {
            public List<byte> PCMData = null;
            public uint playClock;
            public uint chipClock;
            public int sysPcmVol;
        }

        public override void Reset(byte ChipID)
        {
            SetSystemVolumePCM(ChipID, 0);

            cp[ChipID].PCMData = new List<byte>();
            ope = new Operator[2][] { new Operator[MAX_OPERATOR], new Operator[MAX_OPERATOR] };
            for (int j = 0; j < 2; j++)
                for (int i = 0; i < MAX_OPERATOR; i++)
                    ope[j][i] = new Operator(i, cp[ChipID]);//, commonParam.PCMData[ChipID], commonParam.playClock, commonParam.chipClock);
        }

        public override uint Start(byte chipID, uint clock)
        {
            throw new NotImplementedException();
        }

        public override uint Start(byte chipID, uint clock, uint ClockValue, params object[] option)
        {
            cp[chipID].playClock = clock;
            cp[chipID].chipClock = ClockValue;
            return clock;
        }

        public override void Stop(byte chipID)
        {
            ope[chipID] = null;
            cp[chipID].PCMData.Clear();
        }

        public override void Update(byte chipID, int[][] outputs, int samples)
        {
            for(int op = 0; op < MAX_OPERATOR; op++)
            {
                ope[chipID][op].Update(outputs, samples);
            }
        }

        public override int Write(byte chipID, int bank, int adr, int data)
        {
            switch (bank)
            {
                case 0://BANK A
                    WriteBankA(chipID, adr, data);
                    break;
                case 1://BANK B
                    WriteBankB(chipID, adr, data);
                    break;
                case 2://BANK C
                    WriteBankC(chipID, adr, data);
                    break;
                case 3://BANK D
                    WriteBankD(chipID, adr, data);
                    break;
            }
            return 0;
        }

        public void SetPCMData(byte chipID, byte[] data)
        {
            cp[chipID].PCMData = new List<byte>(data);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="db"></param>
        public void SetSystemVolumePCM(byte chipID, int db)
        {
            db = Math.Min(db, 20);
            if (db > -192)
                cp[chipID].sysPcmVol = (int)(65536.0 * Math.Pow(10.0, db / 40.0));
            else
                cp[chipID].sysPcmVol = 0;
        }

        private void WriteBankA(byte chipID, int adr, int data)
        {
            throw new NotImplementedException();
        }

        private void WriteBankB(byte chipID, int adr, int data)
        {
            int opNum = adr / 0x100;
            int opAdr = adr % 0x100;
            int opTyp = opAdr < 0x80 ? 0 : (opAdr < 0xf0 ? 1 : 2);

            switch (opTyp)
            {
                case 0:
                    ope[chipID][opNum].fm.Write((byte)(opAdr - 0x00), (byte)data);
                    break;
                case 1:
                    ope[chipID][opNum].pcm.Write((byte)(opAdr - 0x80), data);
                    break;
                case 2:
                    ope[chipID][opNum].sc.Write((byte)(opAdr - 0xf0), (byte)data);
                    break;
            }
        }

        private void WriteBankC(byte chipID, int adr, int data)
        {
            if (adr >= cp[chipID].PCMData.Count)
            {
                long size = adr - cp[chipID].PCMData.Count + 1;
                for (int i = 0; i < size; i++)
                    cp[chipID].PCMData.Add(0);
            }

            cp[chipID].PCMData[adr] = (byte)data;
        }

        private void WriteBankD(byte chipID, int adr, int data)
        {
            int opNum = adr % 0x90;
            int opTyp = adr / 0x90;

            if (opTyp == 0)
            {
                uint d = ope[chipID][opNum / 3].NoteByteMatrix;
                d &= (uint)~(0x0000_00ff << ((adr % 3) * 8));
                d |= (uint)((byte)data << ((adr % 3) * 8));
                ope[chipID][opNum / 3].NoteByteMatrix = d;
            }
            else
            {
                Operator o = ope[chipID][opNum % 48];
                o.KeyFrqmode = (byte)data;
                if (!o.KeyOnFlg)
                {
                    // off > on  --> true
                    // off > off --> false
                    o.KeyOnFlg = ((data & 0x80) != 0);
                }
                else
                {
                    // on > on  --> false
                    // on > off --> false
                    o.KeyOnFlg = false;
                }

            }
        }


    }
}
