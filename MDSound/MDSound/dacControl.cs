using System;
using System.Collections.Generic;
using System.Text;

namespace MDSound
{
    public class dacControl
    {

        public class VGM_PCM_DATA
        {
            public uint DataSize;
            public byte[] Data;
            public uint DataStart;
        }

        public class VGM_PCM_BANK
        {
            public uint BankCount;
            public List<VGM_PCM_DATA> Bank = new List<VGM_PCM_DATA>();
            public uint DataSize;
            public byte[] Data;
            public uint DataPos;
            public uint BnkPos;
        }

        public class DACCTRL_DATA
        {
            public bool Enable;
            public byte Bank;
        }

        public class PCMBANK_TBL
        {
            public byte ComprType;
            public byte CmpSubType;
            public byte BitDec;
            public byte BitCmp;
            public uint EntryCount;
            public byte[] Entries;// void* Entries;
        }

        public class dac_control
        {
            // Commands sent to dest-chip
            public byte DstChipType;
            public byte DstEmuType;
            public byte DstChipIndex;
            public byte DstChipID;
            public uint DstCommand;
            public byte CmdSize;

            public uint Frequency;   // Frequency (Hz) at which the commands are sent
            public uint DataLen;     // to protect from reading beyond End Of Data
            public byte[] Data;
            public uint DataStart;   // Position where to start
            public byte StepSize;     // usually 1, set to 2 for L/R interleaved data
            public byte StepBase;     // usually 0, set to 0/1 for L/R interleaved data
            public uint CmdsToSend;

            // Running Bits:	0 (01) - is playing
            //					2 (04) - loop sample (simple loop from start to end)
            //					4 (10) - already sent this command
            //					7 (80) - disabled
            public byte Running;
            public byte Reverse;
            public uint Step;        // Position in Player SampleRate
            public uint Pos;         // Position in Data SampleRate
            public uint RemainCmds;
            public uint RealPos;     // true Position in Data (== Pos, if Reverse is off)
            public byte DataStep;     // always StepSize * CmdSize
        }

        private const byte DCTRL_LMODE_IGNORE = 0x00;
        private const byte DCTRL_LMODE_CMDS = 0x01;
        private const byte DCTRL_LMODE_MSEC = 0x02;
        private const byte DCTRL_LMODE_TOEND = 0x03;
        private const byte DCTRL_LMODE_BYTES = 0x0F;
        private const int MAX_CHIPS = 0xFF;
        private readonly uint DAC_SMPL_RATE = 44100;
        private const int PCM_BANK_COUNT = 0x40;

        private dac_control[] DACData = new dac_control[MAX_CHIPS];
        private MDSound mds = null;
        private object lockObj = new object();
        private uint samplingRate;
        private double pcmStep;
        private double pcmExecDelta;
        private byte DacCtrlUsed;
        private byte[] DacCtrlUsg = new byte[MAX_CHIPS];
        private DACCTRL_DATA[] DacCtrl = new DACCTRL_DATA[0xFF];
        public VGM_PCM_BANK[] PCMBank = null;
        private PCMBANK_TBL PCMTbl = new PCMBANK_TBL();

        public dacControl(uint samplingRate,MDSound mds)
        {
            init(samplingRate, mds, null);
        }

        public void init(uint samplingRate,MDSound mds, VGM_PCM_BANK[] PCMBank)
        {
            this.mds = mds;
            this.samplingRate = samplingRate;
            pcmStep = samplingRate / (double)DAC_SMPL_RATE;
            pcmExecDelta = 0;
            this.PCMBank = PCMBank;
            refresh();
            DacCtrlUsed = 0x00;
            for (byte CurChip = 0x00; CurChip < 0xFF; CurChip++)
            {
                DacCtrl[CurChip] = new DACCTRL_DATA();
                DacCtrl[CurChip].Enable = false;
            }
        }

        public void update()
        {
            while ((int)pcmExecDelta <= 0)
            {
                for (int CurChip = 0x00; CurChip < DacCtrlUsed; CurChip++)
                {
                    update(DacCtrlUsg[CurChip], 1);
                }
                pcmExecDelta += pcmStep;
            }
            pcmExecDelta -= 1.0;
        }

        public void SetupStreamControl(byte si, byte ChipType, byte EmuType, byte ChipIndex, byte ChipID, byte port, byte cmd)
        {
            if (si == 0xFF) return;

            if (!DacCtrl[si].Enable)
            {
                device_start_daccontrol(si);
                device_reset_daccontrol(si);
                DacCtrl[si].Enable = true;
                DacCtrlUsg[DacCtrlUsed] = si;
                DacCtrlUsed++;
            }

            setup_chip(si, ChipType, EmuType, ChipIndex, (byte)(ChipID & 0x7F), (uint)(port * 0x100 + cmd));
        }

        public void SetStreamData(byte si,byte bank, byte StepSize, byte StepBase)
        {
            if (si == 0xFF) return;

            DacCtrl[si].Bank = bank;
            if (DacCtrl[si].Bank >= PCM_BANK_COUNT)
                DacCtrl[si].Bank = 0x00;

            VGM_PCM_BANK TempPCM = PCMBank[DacCtrl[si].Bank];
            //Last95Max = TempPCM->BankCount;
            set_data(si, TempPCM.Data, TempPCM.DataSize, StepSize, StepBase);
        }

        public void SetStreamFrequency(byte si,uint TempLng)
        {
            if (si == 0xFF || !DacCtrl[si].Enable) return;
            set_frequency(si, TempLng);
        }

        public void StartStream(byte si, uint DataStart, byte TempByt, uint DataLen)
        {
            if (si == 0xFF || !DacCtrl[si].Enable || PCMBank[DacCtrl[si].Bank].BankCount == 0)
                return;

            start(si, DataStart, TempByt, DataLen);
        }

        public void StopStream(byte si)
        {
            if (!DacCtrl[si].Enable)
                return;

            if (si < 0xFF)
                stop(si);
            else
                for (si = 0x00; si < 0xFF; si++) stop(si);

        }

        public void StartStreamFastCall(byte CurChip, uint TempSht,byte mode)
        {
            if (CurChip == 0xFF || !DacCtrl[CurChip].Enable ||
                PCMBank[DacCtrl[CurChip].Bank].BankCount == 0)
            {
                return;
            }

            VGM_PCM_BANK TempPCM = PCMBank[DacCtrl[CurChip].Bank];
            if (TempSht >= TempPCM.BankCount)
                TempSht = 0x00;

            VGM_PCM_DATA TempBnk = TempPCM.Bank[(int)TempSht];

            byte TempByt = (byte)(dacControl.DCTRL_LMODE_BYTES |
                        (mode & 0x10) |         // Reverse Mode
                        ((mode & 0x01) << 7));   // Looping
            start(CurChip, TempBnk.DataStart, TempByt, TempBnk.DataSize);
        }

        public void AddPCMData(byte Type, uint DataSize, uint Adr,byte[] vgmBuf)
        {
            uint CurBnk;
            VGM_PCM_BANK TempPCM;
            VGM_PCM_DATA TempBnk;
            uint BankSize;
            //bool RetVal;
            byte BnkType;
            byte CurDAC;

            BnkType = (byte)(Type & 0x3F);
            if (BnkType >= PCM_BANK_COUNT)// || vgmCurLoop > 0)
                return;

            if (Type == 0x7F)
            {
                //ReadPCMTable(DataSize, Data);
                ReadPCMTable(vgmBuf, DataSize, Adr);
                return;
            }

            TempPCM = PCMBank[BnkType];// &PCMBank[BnkType];
            TempPCM.BnkPos++;
            if (TempPCM.BnkPos <= TempPCM.BankCount)
                return; // Speed hack for restarting playback (skip already loaded blocks)
            CurBnk = TempPCM.BankCount;
            TempPCM.BankCount++;
            //if (Last95Max != 0xFFFF) Last95Max = TempPCM.BankCount;
            TempPCM.Bank.Add(new VGM_PCM_DATA());// = (VGM_PCM_DATA*)realloc(TempPCM->Bank,
                                                 // sizeof(VGM_PCM_DATA) * TempPCM->BankCount);

            if ((Type & 0x40) == 0)
                BankSize = DataSize;
            else
                BankSize = getLE32(vgmBuf, Adr + 1);// ReadLE32(&Data[0x01]);

            byte[] newData = new byte[TempPCM.DataSize + BankSize];
            if (TempPCM.Data != null && TempPCM.Data.Length > 0)
                Array.Copy(TempPCM.Data, newData, TempPCM.Data.Length);
            TempPCM.Data = newData;

            //TempPCM.Data = new byte[TempPCM.DataSize + BankSize];// realloc(TempPCM->Data, TempPCM->DataSize + BankSize);
            TempBnk = TempPCM.Bank[(int)CurBnk];
            TempBnk.DataStart = TempPCM.DataSize;
            TempBnk.Data = new byte[BankSize];
            if ((Type & 0x40) == 0)
            {
                TempBnk.DataSize = DataSize;
                for (int i = 0; i < DataSize; i++)
                {
                    TempPCM.Data[i + TempBnk.DataStart] = vgmBuf[Adr + i];
                    TempBnk.Data[i] = vgmBuf[Adr + i];
                }
                //TempBnk.Data = TempPCM.Data + TempBnk.DataStart;
                //memcpy(TempBnk->Data, Data, DataSize);
            }
            else
            {
                //TempBnk.Data = TempPCM.Data + TempBnk.DataStart;
                bool RetVal = DecompressDataBlk(vgmBuf, TempBnk, DataSize, Adr);
                if (RetVal == false)
                {
                    TempBnk.Data = null;
                    TempBnk.DataSize = 0x00;
                    //return;
                    goto RefreshDACStrm;    // sorry for the goto, but I don't want to copy-paste the code
                }
                for (int i = 0; i < BankSize; i++)// DataSize; i++)
                {
                    TempPCM.Data[i + TempBnk.DataStart] = TempBnk.Data[i];
                }
            }
            //if (BankSize != TempBnk.DataSize) Console.Write("Error reading Data Block! Data Size conflict!\n");
            TempPCM.DataSize += BankSize;

        // realloc may've moved the Bank block, so refresh all DAC Streams
        RefreshDACStrm:
            for (CurDAC = 0x00; CurDAC < DacCtrlUsed; CurDAC++)
            {
                if (DacCtrl[DacCtrlUsg[CurDAC]].Bank == BnkType)
                    refresh_data(DacCtrlUsg[CurDAC], TempPCM.Data, TempPCM.DataSize);
            }

            return;
        }

        public byte GetDACFromPCMBank()
        {
            // for YM2612 DAC data only
            /*VGM_PCM_BANK* TempPCM;
            UINT32 CurBnk;*/
            uint DataPos;

            DataPos = PCMBank[0x00].DataPos;
            if (DataPos >= PCMBank[0x00].DataSize)
                return 0x80;

            PCMBank[0x00].DataPos++;
            return PCMBank[0x00].Bank[0].Data[DataPos];
        }

        public uint? GetPCMAddressFromPCMBank(byte Type, uint DataPos)
        {
            if (Type >= PCM_BANK_COUNT)
                return null;

            if (DataPos >= PCMBank[Type].DataSize)
                return null;

            return DataPos;
        }

        public void refresh()
        {
            lock (lockObj)
            {
                for (int i = 0; i < MAX_CHIPS; i++) DACData[i] = new dac_control();
            }
        }




        private UInt32 getLE16(byte[] vgmBuf, UInt32 adr)
        {
            UInt32 dat;
            dat = (UInt32)vgmBuf[adr] + (UInt32)vgmBuf[adr + 1] * 0x100;

            return dat;
        }

        private UInt32 getLE32(byte[] vgmBuf,UInt32 adr)
        {
            UInt32 dat;
            dat = (UInt32)vgmBuf[adr] + (UInt32)vgmBuf[adr + 1] * 0x100 + (UInt32)vgmBuf[adr + 2] * 0x10000 + (UInt32)vgmBuf[adr + 3] * 0x1000000;

            return dat;
        }

        private bool DecompressDataBlk(byte[] vgmBuf, VGM_PCM_DATA Bank, uint DataSize, uint Adr)
        {
            byte ComprType;
            byte BitDec;
            byte BitCmp;
            byte CmpSubType;
            uint AddVal;
            uint InPos;
            uint InDataEnd;
            uint OutPos;
            uint OutDataEnd;
            uint InVal;
            uint OutVal = 0;// FUINT16 OutVal;
            byte ValSize;
            byte InShift;
            byte OutShift;
            uint Ent1B = 0;// UINT8* Ent1B;
            uint Ent2B = 0;// UINT16* Ent2B;
            //#if defined(_DEBUG) && defined(WIN32)
            //	UINT32 Time;
            //#endif

            // ReadBits Variables
            byte BitsToRead;
            byte BitReadVal;
            byte InValB;
            byte BitMask;
            byte OutBit;

            // Variables for DPCM
            uint OutMask;

            //#if defined(_DEBUG) && defined(WIN32)
            //	Time = GetTickCount();
            //#endif
            ComprType = vgmBuf[Adr + 0];
            Bank.DataSize = getLE32(vgmBuf, Adr + 1);

            switch (ComprType)
            {
                case 0x00:  // n-Bit compression
                    BitDec = vgmBuf[Adr + 5];
                    BitCmp = vgmBuf[Adr + 6];
                    CmpSubType = vgmBuf[Adr + 7];
                    AddVal = getLE16(vgmBuf, Adr + 8);

                    if (CmpSubType == 0x02)
                    {
                        //Bank.DataSize = 0x00;
                        //return false;

                        Ent1B = 0;// (UINT8*)PCMTbl.Entries; // Big Endian note: Those are stored in LE and converted when reading.
                        Ent2B = 0;// (UINT16*)PCMTbl.Entries;
                        if (PCMTbl.EntryCount == 0)
                        {
                            Bank.DataSize = 0x00;
                            //printf("Error loading table-compressed data block! No table loaded!\n");
                            return false;
                        }
                        else if (BitDec != PCMTbl.BitDec || BitCmp != PCMTbl.BitCmp)
                        {
                            Bank.DataSize = 0x00;
                            //printf("Warning! Data block and loaded value table incompatible!\n");
                            return false;
                        }
                    }

                    ValSize = (byte)((BitDec + 7) / 8);
                    InPos = Adr + 0x0A;
                    InDataEnd = Adr + DataSize;
                    InShift = 0;
                    OutShift = (byte)(BitDec - BitCmp);
                    //                    OutDataEnd = Bank.Data + Bank.DataSize;
                    OutDataEnd = Bank.DataSize;

                    //for (OutPos = Bank->Data; OutPos < OutDataEnd && InPos < InDataEnd; OutPos += ValSize)
                    for (OutPos = 0; OutPos < OutDataEnd && InPos < InDataEnd; OutPos += ValSize)
                    {
                        //InVal = ReadBits(Data, InPos, &InShift, BitCmp);
                        // inlined - is 30% faster
                        OutBit = 0x00;
                        InVal = 0x0000;
                        BitsToRead = BitCmp;
                        while (BitsToRead != 0)
                        {
                            BitReadVal = (byte)((BitsToRead >= 8) ? 8 : BitsToRead);
                            BitsToRead -= BitReadVal;
                            BitMask = (byte)((1 << BitReadVal) - 1);

                            InShift += BitReadVal;
                            //InValB = (byte)((vgmBuf[InPos] << InShift >> 8) & BitMask);
                            InValB = (byte)((vgmBuf[InPos] << InShift >> 8) & BitMask);
                            if (InShift >= 8)
                            {
                                InShift -= 8;
                                InPos++;
                                if (InShift != 0)
                                    InValB |= (byte)((vgmBuf[InPos] << InShift >> 8) & BitMask);
                            }

                            InVal |= (uint)(InValB << OutBit);
                            OutBit += BitReadVal;
                        }

                        switch (CmpSubType)
                        {
                            case 0x00:  // Copy
                                OutVal = InVal + AddVal;
                                break;
                            case 0x01:  // Shift Left
                                OutVal = (InVal << OutShift) + AddVal;
                                break;
                            case 0x02:  // Table
                                switch (ValSize)
                                {
                                    case 0x01:
                                        OutVal = PCMTbl.Entries[Ent1B + InVal];
                                        break;
                                    case 0x02:
                                        //#ifndef BIG_ENDIAN
                                        //					OutVal = Ent2B[InVal];
                                        //#else
                                        OutVal = (uint)(PCMTbl.Entries[Ent2B + InVal * 2] + PCMTbl.Entries[Ent2B + InVal * 2 + 1] * 0x100);// ReadLE16((UINT8*)&Ent2B[InVal]);
                                                                                                                                           //#endif
                                        break;
                                }
                                break;
                        }

                        //#ifndef BIG_ENDIAN
                        //			//memcpy(OutPos, &OutVal, ValSize);
                        //			if (ValSize == 0x01)
                        //               *((UINT8*)OutPos) = (UINT8)OutVal;
                        //			else //if (ValSize == 0x02)
                        //                *((UINT16*)OutPos) = (UINT16)OutVal;
                        //#else
                        if (ValSize == 0x01)
                        {
                            Bank.Data[OutPos] = (byte)OutVal;
                        }
                        else //if (ValSize == 0x02)
                        {
                            Bank.Data[OutPos + 0x00] = (byte)((OutVal & 0x00FF) >> 0);
                            Bank.Data[OutPos + 0x01] = (byte)((OutVal & 0xFF00) >> 8);
                        }
                        //#endif
                    }
                    break;
                case 0x01:  // Delta-PCM
                    BitDec = vgmBuf[Adr + 5];// Data[0x05];
                    BitCmp = vgmBuf[Adr + 6];// Data[0x06];
                    OutVal = getLE16(vgmBuf, Adr + 8);// ReadLE16(&Data[0x08]);

                    Ent1B = 0;// (UINT8*)PCMTbl.Entries;
                    Ent2B = 0;// (UINT16*)PCMTbl.Entries;
                    if (PCMTbl.EntryCount == 0)
                    {
                        Bank.DataSize = 0x00;
                        //printf("Error loading table-compressed data block! No table loaded!\n");
                        return false;
                    }
                    else if (BitDec != PCMTbl.BitDec || BitCmp != PCMTbl.BitCmp)
                    {
                        Bank.DataSize = 0x00;
                        //printf("Warning! Data block and loaded value table incompatible!\n");
                        return false;
                    }

                    ValSize = (byte)((BitDec + 7) / 8);
                    OutMask = (uint)((1 << BitDec) - 1);
                    InPos = Adr + 0xa;
                    InDataEnd = Adr + DataSize;
                    InShift = 0;
                    OutShift = (byte)(BitDec - BitCmp);
                    OutDataEnd = Bank.DataSize;// Bank.Data + Bank.DataSize;
                    AddVal = 0x0000;

                    //                    for (OutPos = Bank.Data; OutPos < OutDataEnd && InPos < InDataEnd; OutPos += ValSize)
                    for (OutPos = 0; OutPos < OutDataEnd && InPos < InDataEnd; OutPos += ValSize)
                    {
                        //InVal = ReadBits(Data, InPos, &InShift, BitCmp);
                        // inlined - is 30% faster
                        OutBit = 0x00;
                        InVal = 0x0000;
                        BitsToRead = BitCmp;
                        while (BitsToRead != 0)
                        {
                            BitReadVal = (byte)((BitsToRead >= 8) ? 8 : BitsToRead);
                            BitsToRead -= BitReadVal;
                            BitMask = (byte)((1 << BitReadVal) - 1);

                            InShift += BitReadVal;
                            InValB = (byte)((vgmBuf[InPos] << InShift >> 8) & BitMask);
                            if (InShift >= 8)
                            {
                                InShift -= 8;
                                InPos++;
                                if (InShift != 0)
                                    InValB |= (byte)((vgmBuf[InPos] << InShift >> 8) & BitMask);
                            }

                            InVal |= (byte)(InValB << OutBit);
                            OutBit += BitReadVal;
                        }

                        switch (ValSize)
                        {
                            case 0x01:
                                AddVal = PCMTbl.Entries[Ent1B + InVal];
                                OutVal += AddVal;
                                OutVal &= OutMask;
                                Bank.Data[OutPos] = (byte)OutVal;// *((UINT8*)OutPos) = (UINT8)OutVal;
                                break;
                            case 0x02:
                                //#ifndef BIG_ENDIAN
                                //				AddVal = Ent2B[InVal];
                                //#else
                                AddVal = (uint)(PCMTbl.Entries[Ent2B + InVal] + PCMTbl.Entries[Ent2B + InVal + 1] * 0x100);
                                //AddVal = ReadLE16((UINT8*)&Ent2B[InVal]);
                                //#endif
                                OutVal += AddVal;
                                OutVal &= OutMask;
                                //#ifndef BIG_ENDIAN
                                //				*((UINT16*)OutPos) = (UINT16)OutVal;
                                //#else
                                Bank.Data[OutPos + 0x00] = (byte)((OutVal & 0x00FF) >> 0);
                                Bank.Data[OutPos + 0x01] = (byte)((OutVal & 0xFF00) >> 8);
                                //#endif
                                break;
                        }
                    }
                    break;
                default:
                    //printf("Error: Unknown data block compression!\n");
                    return false;
            }

            //#if defined(_DEBUG) && defined(WIN32)
            //	Time = GetTickCount() - Time;
            //	printf("Decompression Time: %lu\n", Time);
            //#endif

            return true;
        }

        private void ReadPCMTable(byte[] vgmBuf,uint DataSize, uint Adr)
        {
            byte ValSize;
            uint TblSize;

            PCMTbl.ComprType = vgmBuf[Adr + 0];// Data[0x00];
            PCMTbl.CmpSubType = vgmBuf[Adr + 1];// Data[0x01];
            PCMTbl.BitDec = vgmBuf[Adr + 2];// Data[0x02];
            PCMTbl.BitCmp = vgmBuf[Adr + 3];// Data[0x03];
            PCMTbl.EntryCount = getLE16(vgmBuf, Adr + 4);// ReadLE16(&Data[0x04]);

            ValSize = (byte)((PCMTbl.BitDec + 7) / 8);
            TblSize = PCMTbl.EntryCount * ValSize;

            PCMTbl.Entries = new byte[TblSize];// realloc(PCMTbl.Entries, TblSize);
            for (int i = 0; i < TblSize; i++) PCMTbl.Entries[i] = vgmBuf[Adr + 6 + i];

            //if (DataSize < 0x06 + TblSize)
            //{
            //    //Console.Write("Warning! Bad PCM Table Length!\n");
            //    //printf("Warning! Bad PCM Table Length!\n");
            //}

            return;
        }



        private void sendCommand(dac_control chip)
        {
            //注意!! chipはlock中です

            byte Port;
            byte Command;
            byte Data;

            if ((chip.Running & 0x10) != 0)   // command already sent
                return;
            if (chip.DataStart + chip.RealPos >= chip.DataLen)
                return;

            //if (! chip->Reverse)
            //ChipData00 = chip.Data[(chip.DataStart + chip.RealPos)];
            //ChipData01 = chip.Data[(chip.DataStart + chip.RealPos+1)];
            //else
            //	ChipData = chip->Data + (chip->DataStart + chip->CmdsToSend - 1 - chip->Pos);
            switch (chip.DstChipType)
            {
                // Support for the important chips
                case 0x02:  // YM2612 (16-bit Register (actually 9 Bit), 8-bit Data)
                    Port = (byte)((chip.DstCommand & 0xFF00) >> 8);
                    Command = (byte)((chip.DstCommand & 0x00FF) >> 0);
                    Data = chip.Data[(chip.DataStart + chip.RealPos)];

                    chip_reg_write(chip.DstChipType
                        , chip.DstEmuType, chip.DstChipIndex, chip.DstChipID
                        , Port, Command, Data);
                    break;
                case 0x11:  // PWM (4-bit Register, 12-bit Data)
                    Port = (byte)((chip.DstCommand & 0x000F) >> 0);
                    Command = (byte)(chip.Data[chip.DataStart + chip.RealPos + 1] & 0x0F);
                    Data = chip.Data[chip.DataStart + chip.RealPos];

                    chip_reg_write(chip.DstChipType
                        , chip.DstEmuType, chip.DstChipIndex, chip.DstChipID
                        , Port, Command, Data);
                    break;
                // Support for other chips (mainly for completeness)
                case 0x00:  // SN76496 (4-bit Register, 4-bit/10-bit Data)
                    Command = (byte)((chip.DstCommand & 0x00F0) >> 0);
                    Data = (byte)(chip.Data[chip.DataStart + chip.RealPos] & 0x0F);

                    if ((Command & 0x10) != 0)
                    {
                        // Volume Change (4-Bit value)
                        chip_reg_write(chip.DstChipType
                            , chip.DstEmuType, chip.DstChipIndex, chip.DstChipID
                            , 0x00, 0x00, (byte)(Command | Data));
                    }
                    else
                    {
                        // Frequency Write (10-Bit value)
                        Port = (byte)(((chip.Data[chip.DataStart + chip.RealPos + 1] & 0x03) << 4) | ((chip.Data[chip.DataStart + chip.RealPos] & 0xF0) >> 4));
                        chip_reg_write(chip.DstChipType
                            , chip.DstEmuType, chip.DstChipIndex, chip.DstChipID
                            , 0x00, 0x00, (byte)(Command | Data));
                        chip_reg_write(chip.DstChipType
                            , chip.DstEmuType, chip.DstChipIndex, chip.DstChipID
                            , 0x00, 0x00, Port);
                    }
                    break;
                case 0x18:  // OKIM6295 - TODO: verify
                    Command = (byte)((chip.DstCommand & 0x00FF) >> 0);
                    Data = chip.Data[chip.DataStart + chip.RealPos];

                    if (Command == 0)
                    {
                        Port = (byte)((chip.DstCommand & 0x0F00) >> 8);
                        if ((Data & 0x80) > 0)
                        {
                            // Sample Start
                            // write sample ID
                            chip_reg_write(chip.DstChipType
                                , chip.DstEmuType, chip.DstChipIndex, chip.DstChipID
                                , 0x00, Command, Data);
                            // write channel(s) that should play the sample
                            chip_reg_write(chip.DstChipType
                                , chip.DstEmuType, chip.DstChipIndex, chip.DstChipID
                                , 0x00, Command, (byte)(Port << 4));
                        }
                        else
                        {
                            // Sample Stop
                            chip_reg_write(chip.DstChipType
                                , chip.DstEmuType, chip.DstChipIndex, chip.DstChipID
                                , 0x00, Command, (byte)(Port << 3));
                        }
                    }
                    else
                    {
                        chip_reg_write(chip.DstChipType
                            , chip.DstEmuType, chip.DstChipIndex, chip.DstChipID
                            , 0x00, Command, Data);
                    }
                    break;
                // Generic support: 8-bit Register, 8-bit Data
                case 0x01:  // YM2413
                case 0x03:  // YM2151
                case 0x06:  // YM2203
                case 0x09:  // YM3812
                case 0x0A:  // YM3526
                case 0x0B:  // Y8950
                case 0x0F:  // YMZ280B
                case 0x12:  // AY8910
                case 0x13:  // GameBoy DMG
                case 0x14:  // NES APU
                            //	case 0x15:	// MultiPCM
                case 0x16:  // UPD7759
                case 0x17:  // OKIM6258
                case 0x1D:  // K053260 - TODO: Verify
                case 0x1E:  // Pokey - TODO: Verify
                    Command = (byte)((chip.DstCommand & 0x00FF) >> 0);
                    Data = chip.Data[chip.DataStart + chip.RealPos];
                    chip_reg_write(chip.DstChipType
                        , chip.DstEmuType, chip.DstChipIndex, chip.DstChipID
                        , 0x00, Command, Data);
                    break;
                // Generic support: 16-bit Register, 8-bit Data
                case 0x07:  // YM2608
                case 0x08:  // YM2610/B
                case 0x0C:  // YMF262
                case 0x0D:  // YMF278B
                case 0x0E:  // YMF271
                case 0x19:  // K051649 - TODO: Verify
                case 0x1A:  // K054539 - TODO: Verify
                case 0x1C:  // C140 - TODO: Verify
                    Port = (byte)((chip.DstCommand & 0xFF00) >> 8);
                    Command = (byte)((chip.DstCommand & 0x00FF) >> 0);
                    Data = chip.Data[chip.DataStart + chip.RealPos];
                    chip_reg_write(chip.DstChipType
                        , chip.DstEmuType, chip.DstChipIndex, chip.DstChipID
                        , Port, Command, Data);
                    break;
                // Generic support: 8-bit Register with Channel Select, 8-bit Data
                case 0x05:  // RF5C68
                case 0x10:  // RF5C164
                case 0x1B:  // HuC6280
                    Port = (byte)((chip.DstCommand & 0xFF00) >> 8);
                    Command = (byte)((chip.DstCommand & 0x00FF) >> 0);
                    Data = chip.Data[chip.DataStart + chip.RealPos];

                    if (Port == 0xFF)   // Send Channel Select
                        chip_reg_write(chip.DstChipType
                            , chip.DstEmuType, chip.DstChipIndex, chip.DstChipID
                            , 0x00, (byte)(Command & 0x0f), Data);
                    else
                    {
                        byte prevChn;

                        prevChn = Port; // by default don't restore channel
                                        // get current channel for supported chips
                        if (chip.DstChipType == 0x05)
                        { }   // TODO
                        else if (chip.DstChipType == 0x05)
                        { }   // TODO
                        else if (chip.DstChipType == 0x1B)
                            prevChn = mds.ReadHuC6280(chip.DstChipIndex, chip.DstChipID, 0x00);

                        // Send Channel Select
                        chip_reg_write(chip.DstChipType
                            , chip.DstEmuType, chip.DstChipIndex, chip.DstChipID
                            , 0x00, (byte)(Command >> 4), Port);
                        // Send Data
                        chip_reg_write(chip.DstChipType
                            , chip.DstEmuType, chip.DstChipIndex, chip.DstChipID
                            , 0x00, (byte)(Command & 0x0F), Data);
                        // restore old channel
                        if (prevChn != Port)
                            chip_reg_write(chip.DstChipType
                                , chip.DstEmuType, chip.DstChipIndex, chip.DstChipID
                                , 0x00, (byte)(Command >> 4), prevChn);

                    }
                    break;
                // Generic support: 8-bit Register, 16-bit Data
                case 0x1F:  // QSound
                    Command = (byte)((chip.DstCommand & 0x00FF) >> 0);
                    chip_reg_write(chip.DstChipType
                        , chip.DstEmuType, chip.DstChipIndex, chip.DstChipID
                        , chip.Data[chip.DataStart + chip.RealPos], chip.Data[chip.DataStart + chip.RealPos + 1], Command);
                    break;
            }
            chip.Running |= 0x10;

            return;
        }

        private uint muldiv64round(uint Multiplicand, uint Multiplier, uint Divisor)
        {
            // Yes, I'm correctly rounding the values.
            return (uint)(((ulong)Multiplicand * Multiplier + Divisor / 2) / Divisor);
        }

        private void update(byte ChipID, uint samples)
        {
            lock (lockObj)
            {
                dac_control chip = DACData[ChipID];
                uint NewPos;
                int RealDataStp;

                //System.Console.WriteLine("DAC update ChipID{0} samples{1} chip.Running{2} ", ChipID, samples, chip.Running);
                if ((chip.Running & 0x80) != 0)   // disabled
                    return;
                if ((chip.Running & 0x01) == 0)    // stopped
                    return;

                if (chip.Reverse == 0)
                    RealDataStp = chip.DataStep;
                else
                    RealDataStp = -chip.DataStep;

                if (samples > 0x20)
                {
                    // very effective Speed Hack for fast seeking
                    NewPos = chip.Step + (samples - 0x10);
                    NewPos = muldiv64round(NewPos * chip.DataStep, chip.Frequency, DAC_SMPL_RATE);
                    while (chip.RemainCmds != 0 && chip.Pos < NewPos)
                    {
                        chip.Pos += chip.DataStep;
                        chip.RealPos = (uint)((int)chip.RealPos + RealDataStp);
                        chip.RemainCmds--;
                    }
                }

                chip.Step += samples;
                // Formula: Step * Freq / SampleRate
                NewPos = muldiv64round(chip.Step * chip.DataStep, chip.Frequency, DAC_SMPL_RATE);
                //System.Console.Write("NewPos{0} chip.Step{1} chip.DataStep{2} chip.Frequency{3} DAC_SMPL_RATE{4} \n", NewPos, chip.Step, chip.DataStep, chip.Frequency, (UInt32)common.SampleRate);
                sendCommand(chip);

                while (chip.RemainCmds != 0 && chip.Pos < NewPos)
                {
                    sendCommand(chip);
                    chip.Pos += chip.DataStep;
                    //if(model== enmModel.RealModel)                log.Write(string.Format("datastep:{0}",chip.DataStep));
                    chip.RealPos = (uint)((int)chip.RealPos + RealDataStp);
                    chip.Running &= 0xef;// ~0x10;
                    chip.RemainCmds--;
                }

                if (chip.RemainCmds == 0 && ((chip.Running & 0x04) != 0))
                {
                    // loop back to start
                    chip.RemainCmds = chip.CmdsToSend;
                    chip.Step = 0x00;
                    chip.Pos = 0x00;
                    if (chip.Reverse == 0)
                        chip.RealPos = 0x00;
                    else
                        chip.RealPos = (chip.CmdsToSend - 0x01) * chip.DataStep;
                }

                if (chip.RemainCmds == 0)
                    chip.Running &= 0xfe;// ~0x01; // stop

                return;
            }
        }

        private byte device_start_daccontrol(byte ChipID)
        {
            dac_control chip;

            if (ChipID >= MAX_CHIPS)
                return 0;

            lock (lockObj)
            {
                chip = DACData[ChipID];

                chip.DstChipType = 0xFF;
                chip.DstChipID = 0x00;
                chip.DstCommand = 0x0000;

                chip.Running = 0xFF;   // disable all actions (except setup_chip)
            }

            return 1;
        }

        public void device_stop_daccontrol(byte ChipID)
        {
            lock (lockObj)
            {
                dac_control chip = DACData[ChipID];
                chip.Running = 0xFF;
            }
        }

        private void device_reset_daccontrol(byte ChipID)
        {
            lock (lockObj)
            {
                dac_control chip = DACData[ChipID];

                chip.DstChipType = 0x00;
                chip.DstChipID = 0x00;
                chip.DstCommand = 0x00;
                chip.CmdSize = 0x00;

                chip.Frequency = 0;
                chip.DataLen = 0x00;
                chip.Data = null;
                chip.DataStart = 0x00;
                chip.StepSize = 0x00;
                chip.StepBase = 0x00;

                chip.Running = 0x00;
                chip.Reverse = 0x00;
                chip.Step = 0x00;
                chip.Pos = 0x00;
                chip.RealPos = 0x00;
                chip.RemainCmds = 0x00;
                chip.DataStep = 0x00;
            }
        }

        private void setup_chip(byte ChipID, byte ChType,byte EmuType,byte ChipIndex, byte ChNum, uint Command)
        {
            lock (lockObj)
            {
                dac_control chip = DACData[ChipID];

                chip.DstChipType = ChType; // TypeID (e.g. 0x02 for YM2612)
                chip.DstEmuType = EmuType;
                chip.DstChipIndex = ChipIndex;
                chip.DstChipID = ChNum;    // chip number (to send commands to 1st or 2nd chip)
                chip.DstCommand = Command; // Port and Command (would be 0x02A for YM2612)

                switch (chip.DstChipType)
                {
                    case 0x00:  // SN76496
                        if ((chip.DstCommand & 0x0010) > 0)
                            chip.CmdSize = 0x01;   // Volume Write
                        else
                            chip.CmdSize = 0x02;   // Frequency Write
                        break;
                    case 0x02:  // YM2612
                        chip.CmdSize = 0x01;
                        break;
                    case 0x11:  // PWM
                    case 0x1F:  // QSound
                        chip.CmdSize = 0x02;
                        break;
                    default:
                        chip.CmdSize = 0x01;
                        break;
                }
                chip.DataStep = (byte)(chip.CmdSize * chip.StepSize);
            }
        }

        private void set_data(byte ChipID, byte[] Data, uint DataLen, byte StepSize, byte StepBase)
        {
            lock (lockObj)
            {
                dac_control chip = DACData[ChipID];

                if ((chip.Running & 0x80) > 0)
                    return;

                if (DataLen > 0 && Data != null)
                {
                    chip.DataLen = DataLen;
                    chip.Data = Data;
                }
                else
                {
                    chip.DataLen = 0x00;
                    chip.Data = null;
                }
                chip.StepSize = (byte)(StepSize > 0 ? StepSize : 1);
                chip.StepBase = StepBase;
                chip.DataStep = (byte)(chip.CmdSize * chip.StepSize);
            }
        }

        private void refresh_data(byte ChipID, byte[] Data, uint DataLen)
        {
            lock (lockObj)
            {
                // Should be called to fix the data pointer. (e.g. after a realloc)
                dac_control chip = DACData[ChipID];

                if ((chip.Running & 0x80) != 0)
                    return;

                if (DataLen > 0 && Data != null)
                {
                    chip.DataLen = DataLen;
                    chip.Data = Data;
                }
                else
                {
                    chip.DataLen = 0x00;
                    chip.Data = null;
                }
            }
        }

        private void set_frequency(byte ChipID, uint Frequency)
        {
            lock (lockObj)
            {
                //System.Console.WriteLine("ChipID{0} Frequency{1}", ChipID, Frequency);
                dac_control chip = DACData[ChipID];

                if ((chip.Running & 0x80) != 0)
                    return;

                if (Frequency != 0)
                    chip.Step = chip.Step * chip.Frequency / Frequency;
                chip.Frequency = Frequency;

                return;
            }
        }

        private void start(byte ChipID, uint DataPos, byte LenMode, uint Length)
        {
            lock (lockObj)
            {
                dac_control chip = DACData[ChipID];

                uint CmdStepBase;

                if ((chip.Running & 0x80) != 0)
                    return;

                CmdStepBase = (uint)(chip.CmdSize * chip.StepBase);
                if (DataPos != 0xFFFFFFFF)  // skip setting DataStart, if Pos == -1
                {
                    chip.DataStart = DataPos + CmdStepBase;
                    if (chip.DataStart > chip.DataLen)    // catch bad value and force silence
                        chip.DataStart = chip.DataLen;
                }

                switch (LenMode & 0x0F)
                {
                    case DCTRL_LMODE_IGNORE:    // Length is already set - ignore
                        break;
                    case DCTRL_LMODE_CMDS:      // Length = number of commands
                        chip.CmdsToSend = Length;
                        break;
                    case DCTRL_LMODE_MSEC:      // Length = time in msec
                        chip.CmdsToSend = 1000 * Length / chip.Frequency;
                        break;
                    case DCTRL_LMODE_TOEND:     // play unti stop-command is received (or data-end is reached)
                        chip.CmdsToSend = (chip.DataLen - (chip.DataStart - CmdStepBase)) / chip.DataStep;
                        break;
                    case DCTRL_LMODE_BYTES:     // raw byte count
                        chip.CmdsToSend = Length / chip.DataStep;
                        break;
                    default:
                        chip.CmdsToSend = 0x00;
                        break;
                }
                chip.Reverse = (byte)((LenMode & 0x10) >> 4);

                chip.RemainCmds = chip.CmdsToSend;
                chip.Step = 0x00;
                chip.Pos = 0x00;
                if (chip.Reverse == 0)
                    chip.RealPos = 0x00;
                else
                    chip.RealPos = (chip.CmdsToSend - 0x01) * chip.DataStep;

                chip.Running &= 0xfb;// ~0x04;
                chip.Running |= (byte)((LenMode & 0x80) != 0 ? 0x04 : 0x00);    // set loop mode

                chip.Running |= 0x01;  // start
                chip.Running &= 0xef;// ~0x10; // command isn't yet sent
            }
        }


        private void stop(byte ChipID)
        {
            lock (lockObj)
            {
                dac_control chip = DACData[ChipID];

                if ((chip.Running & 0x80) != 0)
                    return;

                chip.Running &= 0xfe;// ~0x01; // stop
            }
        }

        private void chip_reg_write(byte ChipType,byte EmuType,byte ChipIndex, byte ChipID, byte Port, byte Offset, byte Data)
        {
            switch (ChipType)
            {
                case 0x00:  // SN76489
                    if (EmuType == 0) mds.WriteSN76489(ChipIndex, ChipID, Data);
                    else if (EmuType == 1) mds.WriteSN76496(ChipIndex, ChipID, Data);
                    break;
                case 0x01:  // YM2413+
                    mds.WriteYM2413(ChipIndex, ChipID, Offset, Data);
                    break;
                case 0x02:  // YM2612
                    if (EmuType == 0) mds.WriteYM2612(ChipIndex, ChipID, Port, Offset, Data);
                    else if (EmuType == 1) mds.WriteYM3438(ChipIndex, ChipID, Port, Offset, Data);
                    else if (EmuType == 2) mds.WriteYM2612mame(ChipIndex, ChipID, Port, Offset, Data);
                    break;
                case 0x03:  // YM2151+
                    mds.WriteYM2151(ChipIndex, ChipID, Offset, Data);
                    break;
                case 0x06:  // YM2203+
                    mds.WriteYM2203(ChipIndex, ChipID, Offset, Data);
                    break;
                case 0x07:  // YM2608+
                    mds.WriteYM2608(ChipIndex, ChipID, Port, Offset, Data);
                    break;
                case 0x08:  // YM2610+
                    mds.WriteYM2610(ChipIndex, ChipID, Port, Offset, Data);
                    break;
                case 0x09:  // YM3812+
                    mds.WriteYM3812(ChipIndex, ChipID, Offset, Data);
                    break;
                case 0x0A:  // YM3526+
                    mds.WriteYM3526(ChipIndex, ChipID, Offset, Data);
                    break;
                case 0x0B:  // Y8950+
                    mds.WriteY8950(ChipIndex, ChipID, Offset, Data);
                    break;
                case 0x0C:  // YMF262+
                    mds.WriteYMF262(ChipIndex, ChipID, Port, Offset, Data);
                    break;
                case 0x0D:  // YMF278B+
                    mds.WriteYMF278B(ChipIndex, ChipID, Port, Offset, Data);
                    break;
                case 0x0E:  // YMF271+
                    mds.WriteYMF271(ChipIndex, ChipID, Port, Offset, Data);
                    break;
                case 0x0F:  // YMZ280B+
                    mds.WriteYMZ280B(ChipIndex, ChipID, Offset, Data);
                    break;
                case 0x10:
                    mds.WriteRF5C164(ChipIndex, ChipID, Offset, Data);
                    break;
                case 0x11:  // PWM
                    mds.WritePWM(ChipIndex, ChipID, Port, (uint)((Offset << 8) | (Data << 0)));
                    break;
                case 0x12:  // AY8910+
                    mds.WriteAY8910(ChipIndex, ChipID, Offset, Data);
                    break;
                case 0x13:  // DMG+
                    mds.WriteDMG(ChipIndex, ChipID, Offset, Data);
                    break;
                case 0x14:  // NES+
                    mds.WriteNES(ChipIndex, ChipID, Offset, Data);
                    break;
                case 0x17:  // OKIM6258
                    //System.Console.Write("[DAC]");
                    mds.WriteOKIM6258(ChipIndex, ChipID, Offset, Data);
                    break;
                case 0x1b:  // HuC6280
                    mds.WriteHuC6280(ChipIndex, ChipID, Offset, Data);
                    break;
            }
        }

    }
}
