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
        private List<byte>[] PCMData = null;


        public override void Reset(byte ChipID)
        {
            PCMData = new List<byte>[2] { new List<byte>(), new List<byte>() };
            ope = new Operator[2][] { new Operator[MAX_OPERATOR], new Operator[MAX_OPERATOR] };
            for (int j = 0; j < 2; j++)
                for (int i = 0; i < MAX_OPERATOR; i++)
                    ope[j][i] = new Operator(i, PCMData[ChipID]);
        }

        public override uint Start(byte chipID, uint clock)
        {
            throw new NotImplementedException();
        }

        public override uint Start(byte chipID, uint clock, uint ClockValue, params object[] option)
        {
            return clock;
        }

        public override void Stop(byte chipID)
        {
            ope[chipID] = null;
            PCMData[chipID].Clear();
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
            PCMData[chipID] = new List<byte>(data);
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
            if (adr >= PCMData[chipID].Count)
            {
                long size = adr - PCMData[chipID].Count + 1;
                for (int i = 0; i < size; i++)
                    PCMData[chipID].Add(0);
            }

            PCMData[chipID][adr] = (byte)data;
        }

        private void WriteBankD(byte chipID, int adr, int data)
        {
            int opNum = adr % 0x30;
            int opTyp = adr / 0x30;

            if (opTyp == 0) ope[chipID][opNum].NoteByteMatrix = (byte)data;
            else ope[chipID][opNum].KeyFraction = (byte)data;
        }

    }
}
