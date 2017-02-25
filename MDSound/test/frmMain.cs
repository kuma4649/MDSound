using System;
using System.IO;
using System.Windows.Forms;
using SdlDotNet.Audio;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace test
{
    public partial class frmMain : Form
    {

        private static uint SamplingRate = 44100;
        //private static uint PSGClockValue = 3579545;
        //private static uint FMClockValue = 7670454;
        //private static uint YM2151ClockValue = 3579545;
        //private static uint YM2203ClockValue = 3000000;
        //private static uint rf5c164ClockValue = 12500000;
        //private static uint pwmClockValue = 23011361;
        //private static uint c140ClockValue = 21390;
        //private static MDSound.c140.C140_TYPE c140Type = MDSound.c140.C140_TYPE.ASIC219;

        private static uint samplingBuffer = 1024;
        private static short[] frames = new short[samplingBuffer * 4];
        private static MDSound.MDSound mds = null;//new MDSound.MDSound(SamplingRate, samplingBuffer, FMClockValue, PSGClockValue, rf5c164ClockValue, pwmClockValue, c140ClockValue, c140Type);

        private static AudioStream sdl;
        private static AudioCallback sdlCb = new AudioCallback(callback);
        private static IntPtr sdlCbPtr;
        private static GCHandle sdlCbHandle;

        private static byte[] vgmBuf = null;
        private static uint vgmPcmPtr;
        private static uint vgmPcmBaseAdr;
        private static uint vgmAdr;
        private static int vgmWait;
        private static uint vgmEof;
        private static bool vgmAnalyze;
        private static vgmStream[] vgmStreams = new vgmStream[0x100];


        public frmMain()
        {

            InitializeComponent();

        }


        private void btnRef_Click(object sender, EventArgs e)
        {

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter =
                "VGMファイル(*.vgm)|*.vgm";
            ofd.Title = "ファイルを選択してください";
            ofd.RestoreDirectory = true;
            ofd.CheckPathExists = true;

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                tbFile.Text = ofd.FileName;
            }

        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            btnPlay.Enabled = false;
            stop();
            play(tbFile.Text);

        }

        private void btnStop_Click(object sender, EventArgs e)
        {

            btnPlay.Enabled = true;
            stop();

        }

        private void frmMain_FormClosed(object sender, FormClosedEventArgs e)
        {

            stop();

        }


        private static void stop()
        {

            if (sdl == null) return;

            sdl.Paused = true;
            sdl.Close();
            sdl.Dispose();
            sdl = null;
            if (sdlCbHandle.IsAllocated) sdlCbHandle.Free();


        }

        private static void play(string fileName)
        {

            try
            {

                vgmBuf = File.ReadAllBytes(fileName);

            }
            catch
            {

                MessageBox.Show("ファイルの読み込みに失敗しました。");
                return;

            }
            

            //ヘッダーを読み込めるサイズをもっているかチェック
            if (vgmBuf.Length < 0x40) return;

            //ヘッダーから情報取得

            uint vgm = getLE32(0x00);
            if (vgm != 0x206d6756) return;

            uint version = getLE32(0x08);
            if (version < 0x0150) return;

            vgmEof = getLE32(0x04);

            uint vgmDataOffset = getLE32(0x34);
            if (vgmDataOffset == 0)
            {
                vgmDataOffset = 0x40;
            }
            else
            {
                vgmDataOffset += 0x34;
            }

            vgmAdr = vgmDataOffset;
            vgmWait = 0;
            vgmAnalyze = true;

            MDSound.MDSound.Chip[] chips = null;
            List<MDSound.MDSound.Chip> lstChip = new List<MDSound.MDSound.Chip>();
            MDSound.MDSound.Chip chip = null;

            if (getLE32(0x0c) != 0)
            {
                chip = new MDSound.MDSound.Chip();
                chip.type = MDSound.MDSound.enmInstrumentType.SN76489;
                chip.ID = 0;
                MDSound.sn76489 sn76489 = new MDSound.sn76489();
                chip.Instrument = sn76489;
                chip.Update = sn76489.Update;
                chip.Start = sn76489.Start;
                chip.Stop = sn76489.Stop;
                chip.Reset = sn76489.Reset;
                chip.SamplingRate = SamplingRate;
                chip.Clock = getLE32(0x0c);// PSGClockValue;
                chip.Volume = -50;
                chip.Option = null;
                lstChip.Add(chip);
            }

            if (getLE32(0x2c) != 0)
            {
                chip = new MDSound.MDSound.Chip();
                chip.type = MDSound.MDSound.enmInstrumentType.YM2612;
                chip.ID = 0;
                MDSound.ym2612 ym2612 = new MDSound.ym2612();
                chip.Instrument = ym2612;
                chip.Update = ym2612.Update;
                chip.Start = ym2612.Start;
                chip.Stop = ym2612.Stop;
                chip.Reset = ym2612.Reset;
                chip.SamplingRate = SamplingRate;
                chip.Clock = getLE32(0x2c); //FMClockValue;
                chip.Volume = 0;
                chip.Option = null;
                lstChip.Add(chip);
            }

            if (getLE32(0x30) != 0)
            {
                chip = new MDSound.MDSound.Chip();
                chip.type = MDSound.MDSound.enmInstrumentType.YM2151;
                chip.ID = 0;
                MDSound.ym2151 ym2151 = new MDSound.ym2151();
                chip.Instrument = ym2151;
                chip.Update = ym2151.Update;
                chip.Start = ym2151.Start;
                chip.Stop = ym2151.Stop;
                chip.Reset = ym2151.Reset;
                chip.SamplingRate = SamplingRate;
                chip.Clock = getLE32(0x30); //YM2151ClockValue;
                chip.Volume = 0;
                chip.Option = null;
                lstChip.Add(chip);
            }

            if (getLE32(0x44) != 0)
            {
                chip = new MDSound.MDSound.Chip();
                chip.type = MDSound.MDSound.enmInstrumentType.YM2203;
                chip.ID = 0;
                MDSound.ym2203 ym2203 = new MDSound.ym2203();
                chip.Instrument = ym2203;
                chip.Update = ym2203.Update;
                chip.Start = ym2203.Start;
                chip.Stop = ym2203.Stop;
                chip.Reset = ym2203.Reset;
                chip.SamplingRate = SamplingRate;
                chip.Clock = getLE32(0x44); //YM2203ClockValue;
                chip.Volume = 0;
                chip.Option = null;
                lstChip.Add(chip);
            }

            if (getLE32(0x48) != 0)
            {
                chip = new MDSound.MDSound.Chip();
                chip.type = MDSound.MDSound.enmInstrumentType.YM2608;
                chip.ID = 0;
                MDSound.ym2608 ym2608 = new MDSound.ym2608();
                chip.Instrument = ym2608;
                chip.Update = ym2608.Update;
                chip.Start = ym2608.Start;
                chip.Stop = ym2608.Stop;
                chip.Reset = ym2608.Reset;
                chip.SamplingRate = SamplingRate;
                chip.Clock = getLE32(0x48);
                chip.Volume = 0;
                chip.Option = null;
                lstChip.Add(chip);
            }

            if (getLE32(0x4c) != 0)
            {
                chip = new MDSound.MDSound.Chip();
                chip.type = MDSound.MDSound.enmInstrumentType.YM2610;
                chip.ID = 0;
                MDSound.ym2610 ym2610 = new MDSound.ym2610();
                chip.Instrument = ym2610;
                chip.Update = ym2610.Update;
                chip.Start = ym2610.Start;
                chip.Stop = ym2610.Stop;
                chip.Reset = ym2610.Reset;
                chip.SamplingRate = SamplingRate;
                chip.Clock = getLE32(0x4c) & 0x7fffffff;
                chip.Volume = 0;
                chip.Option = null;
                bufYM2610AdpcmA = null;
                bufYM2610AdpcmB = null;
                lstChip.Add(chip);
            }

            if (getLE32(0x74) != 0)
            {
                chip = new MDSound.MDSound.Chip();
                chip.type = MDSound.MDSound.enmInstrumentType.AY8910;
                chip.ID = 0;
                MDSound.ay8910 ay8910 = new MDSound.ay8910();
                chip.Instrument = ay8910;
                chip.Update = ay8910.Update;
                chip.Start = ay8910.Start;
                chip.Stop = ay8910.Stop;
                chip.Reset = ay8910.Reset;
                chip.SamplingRate = SamplingRate;
                chip.Clock = getLE32(0x74) & 0x7fffffff;
                chip.Clock /= 2;
                if ((vgmBuf[0x79] & 0x10) != 0)
                    chip.Clock /= 2;
                chip.Volume = 0;
                chip.Option = null;
                lstChip.Add(chip);
            }

            //chips[2] = new MDSound.MDSound.Chip();
            //chips[2].type = MDSound.MDSound.enmInstrumentType.RF5C164;
            //chips[2].ID = 0;
            //MDSound.scd_pcm rf5c164 = new MDSound.scd_pcm();
            //chips[2].Instrument = rf5c164;
            //chips[2].Update = rf5c164.Update;
            //chips[2].Start = rf5c164.Start;
            //chips[2].Stop = rf5c164.Stop;
            //chips[2].Reset = rf5c164.Reset;
            //chips[2].SamplingRate = SamplingRate;
            //chips[2].Clock = rf5c164ClockValue;
            //chips[2].Volume = 50;
            //chips[2].Option = null;

            //chips[3] = new MDSound.MDSound.Chip();
            //chips[3].type = MDSound.MDSound.enmInstrumentType.PWM;
            //chips[3].ID = 0;
            //MDSound.pwm pwm = new MDSound.pwm();
            //chips[3].Instrument = pwm;
            //chips[3].Update = pwm.Update;
            //chips[3].Start = pwm.Start;
            //chips[3].Stop = pwm.Stop;
            //chips[3].Reset = pwm.Reset;
            //chips[3].SamplingRate = SamplingRate;
            //chips[3].Clock = pwmClockValue;
            //chips[3].Volume = 100;
            //chips[3].Option = null;

            //chips[4] = new MDSound.MDSound.Chip();
            //chips[4].type = MDSound.MDSound.enmInstrumentType.C140;
            //chips[4].ID = 0;
            //MDSound.c140 c140 = new MDSound.c140();
            //chips[4].Instrument = c140;
            //chips[4].Update = c140.Update;
            //chips[4].Start = c140.Start;
            //chips[4].Stop = c140.Stop;
            //chips[4].Reset = c140.Reset;
            //chips[4].SamplingRate = SamplingRate;
            //chips[4].Clock = c140ClockValue;
            //chips[4].Volume = 100;
            //chips[4].Option = new object[1] { c140Type };

            chips = lstChip.ToArray();
            mds = new MDSound.MDSound(SamplingRate, samplingBuffer, chips);

            sdlCbHandle = GCHandle.Alloc(sdlCb);
            sdlCbPtr = Marshal.GetFunctionPointerForDelegate(sdlCb);
            sdl = new SdlDotNet.Audio.AudioStream((int)SamplingRate, AudioFormat.Signed16Little, SoundChannel.Stereo, (short)samplingBuffer, sdlCb, null);
            sdl.Paused = false;

        }

        static short v = 1200;
        static short vy = -100;
        private static void callback(IntPtr userData, IntPtr stream, int len)
        {

            for (int i = 0; i < len/4; i++)
            {
                short[] buf = new short[2];
                mds.Update(buf, 0, 2, oneFrameVGM);
                //buf[0] = v;
                //buf[1] = v;
                //v += vy;
                //if (v < -1200 || v > 1200)
                //{
                //    vy = (short)-vy;
                //}
                frames[i * 2 + 0] = buf[0];
                frames[i * 2 + 1] = buf[1];
                //Console.Write("Adr[{0:x8}] : Wait[{1:d8}] : [{2:d8}]/[{3:d8}]\r\n", vgmAdr, vgmWait, buf[0], buf[1]);
            }

            Marshal.Copy(frames, 0, stream, len / 2);

        }

        private static void oneFrameVGM()
        {

            if (vgmWait > 0)
            {
                oneFrameVGMStream();
                vgmWait--;
                return;
            }

            if (!vgmAnalyze)
            {
                stop();
                return;
            }

            byte p = 0;
            byte si = 0;
            byte rAdr = 0;
            byte rDat = 0;

            while (vgmWait <= 0)
            {
                if (vgmAdr == vgmBuf.Length || vgmAdr == vgmEof)
                {
                    vgmAnalyze = false;
                    return;
                }

                byte cmd = vgmBuf[vgmAdr];
                //Console.Write(" Adr[{0:x}]:cmd[{1:x}]\r\n", vgmAdr, cmd);
                switch (cmd)
                {
                    case 0x4f: //GG PSG
                    case 0x50: //PSG
                        mds.WriteSN76489(0,vgmBuf[vgmAdr + 1]);
                        vgmAdr += 2;
                        break;
                    case 0x52: //YM2612 Port0
                    case 0x53: //YM2612 Port1
                        p = (byte)((cmd == 0x52) ? 0 : 1);
                        rAdr = vgmBuf[vgmAdr + 1];
                        rDat = vgmBuf[vgmAdr + 2];
                        vgmAdr += 3;
                        mds.WriteYM2612(0,p, rAdr, rDat);

                        break;
                    case 0x54: //YM2151
                        rAdr = vgmBuf[vgmAdr + 1];
                        rDat = vgmBuf[vgmAdr + 2];
                        vgmAdr += 3;
                        //Console.Write(" Adr[{0:x}]:cmd[{1:x}]:Adr[{2:x}]:Dar[{3:x}]\r\n", vgmAdr, cmd,rAdr,rDat);
                        mds.WriteYM2151(0, rAdr, rDat);
                        break;
                    case 0x55: //YM2203
                        rAdr = vgmBuf[vgmAdr + 1];
                        rDat = vgmBuf[vgmAdr + 2];
                        vgmAdr += 3;
                        mds.WriteYM2203(0, rAdr, rDat);

                        break;
                    case 0x56: //YM2608 Port0
                        rAdr = vgmBuf[vgmAdr + 1];
                        rDat = vgmBuf[vgmAdr + 2];
                        vgmAdr += 3;
                        mds.WriteYM2608(0, 0, rAdr, rDat);

                        break;
                    case 0x57: //YM2608 Port1
                        rAdr = vgmBuf[vgmAdr + 1];
                        rDat = vgmBuf[vgmAdr + 2];
                        vgmAdr += 3;
                        mds.WriteYM2608(0, 1, rAdr, rDat);

                        break;
                    case 0x58: //YM2610 Port0
                        rAdr = vgmBuf[vgmAdr + 1];
                        rDat = vgmBuf[vgmAdr + 2];
                        vgmAdr += 3;
                        mds.WriteYM2610(0, 0, rAdr, rDat);

                        break;
                    case 0x59: //YM2610 Port1
                        rAdr = vgmBuf[vgmAdr + 1];
                        rDat = vgmBuf[vgmAdr + 2];
                        vgmAdr += 3;
                        mds.WriteYM2610(0, 1, rAdr, rDat);

                        break;
                    case 0x61: //Wait n samples
                        vgmAdr++;
                        vgmWait += (int)getLE16(vgmAdr);
                        vgmAdr += 2;
                        break;
                    case 0x62: //Wait 735 samples
                        vgmAdr++;
                        vgmWait += 735;
                        break;
                    case 0x63: //Wait 882 samples
                        vgmAdr++;
                        vgmWait += 882;
                        break;
                    case 0x64: //override length of 0x62/0x63
                        vgmAdr += 4;
                        break;
                    case 0x66: //end of sound data
                        vgmAdr = (uint)vgmBuf.Length;
                        break;
                    case 0x67: //data block
                        vgmPcmBaseAdr = vgmAdr + 7;
                        uint bAdr = vgmAdr + 7;
                        byte bType = vgmBuf[vgmAdr + 2];
                        uint bLen = getLE32(vgmAdr + 3);
                        if ((bLen & 0x80000000) != 0)
                        {
                            bLen &= 0x7fffffff;
                            //CurrentChip 1
                        }

                        switch (bType & 0xc0)
                        {
                            case 0x00:
                            case 0x40:
                                //AddPCMData(bType, bLen, bAdr);
                                vgmAdr += (uint)bLen + 7;
                                break;
                            case 0x80:
                                uint romSize = getLE32(vgmAdr + 7);
                                uint startAddress = getLE32(vgmAdr + 0x0B);
                                switch (bType)
                                {
                                    case 0x80:
                                        break;
                                    case 0x81:

                                        // YM2608

                                        mds.WriteYM2608(0, 0x1, 0x00, 0x20);
                                        mds.WriteYM2608(0, 0x1, 0x00, 0x21);
                                        mds.WriteYM2608(0, 0x1, 0x00, 0x00);

                                        mds.WriteYM2608(0, 0x1, 0x10, 0x00);
                                        mds.WriteYM2608(0, 0x1, 0x10, 0x80);

                                        mds.WriteYM2608(0, 0x1, 0x00, 0x61);
                                        mds.WriteYM2608(0, 0x1, 0x00, 0x68);
                                        mds.WriteYM2608(0, 0x1, 0x01, 0x00);

                                        mds.WriteYM2608(0, 0x1, 0x02, (byte)((startAddress >> 2) & 0xff));
                                        mds.WriteYM2608(0, 0x1, 0x03, (byte)((startAddress >> 10) & 0xff));
                                        mds.WriteYM2608(0, 0x1, 0x04, 0xff);
                                        mds.WriteYM2608(0, 0x1, 0x05, 0xff);
                                        mds.WriteYM2608(0, 0x1, 0x0c, 0xff);
                                        mds.WriteYM2608(0, 0x1, 0x0d, 0xff);

                                        // データ転送
                                        for (int cnt = 0; cnt < bLen - 8; cnt++)
                                        {
                                            mds.WriteYM2608(0, 0x1, 0x08, vgmBuf[vgmAdr + 15 + cnt]);
                                        }
                                        mds.WriteYM2608(0, 0x1, 0x00, 0x00);
                                        mds.WriteYM2608(0, 0x1, 0x10, 0x80);

                                        break;

                                    case 0x82:
                                        if (bufYM2610AdpcmA == null || bufYM2610AdpcmA.Length!=romSize) bufYM2610AdpcmA = new byte[romSize];
                                        for (int cnt = 0; cnt < bLen - 8; cnt++)
                                        {
                                            bufYM2610AdpcmA[startAddress+ cnt] = vgmBuf[vgmAdr + 15 + cnt];
                                        }
                                        mds.WriteYM2610_SetAdpcmA(0, bufYM2610AdpcmA);
                                        break;
                                    case 0x83:
                                        if (bufYM2610AdpcmB == null || bufYM2610AdpcmB.Length != romSize) bufYM2610AdpcmB = new byte[romSize];
                                        for (int cnt = 0; cnt < bLen - 8; cnt++)
                                        {
                                            bufYM2610AdpcmB[startAddress + cnt] = vgmBuf[vgmAdr + 15 + cnt];
                                        }
                                        mds.WriteYM2610_SetAdpcmB(0, bufYM2610AdpcmB);
                                        break;

                                    case 0x8b:
                                        break;

                                    case 0x8d:
                                        break;
                                }
                                vgmAdr += (uint)bLen + 7;
                                break;
                            default:
                                vgmAdr += bLen + 7;
                                break;
                        }
                        break;
                    case 0x70: //Wait 1 sample
                    case 0x71: //Wait 2 sample
                    case 0x72: //Wait 3 sample
                    case 0x73: //Wait 4 sample
                    case 0x74: //Wait 5 sample
                    case 0x75: //Wait 6 sample
                    case 0x76: //Wait 7 sample
                    case 0x77: //Wait 8 sample
                    case 0x78: //Wait 9 sample
                    case 0x79: //Wait 10 sample
                    case 0x7a: //Wait 11 sample
                    case 0x7b: //Wait 12 sample
                    case 0x7c: //Wait 13 sample
                    case 0x7d: //Wait 14 sample
                    case 0x7e: //Wait 15 sample
                    case 0x7f: //Wait 16 sample
                        vgmWait += (int)(cmd - 0x6f);
                        vgmAdr++;
                        break;
                    case 0x80: //Write adr2A and Wait 0 sample
                    case 0x81: //Write adr2A and Wait 1 sample
                    case 0x82: //Write adr2A and Wait 2 sample
                    case 0x83: //Write adr2A and Wait 3 sample
                    case 0x84: //Write adr2A and Wait 4 sample
                    case 0x85: //Write adr2A and Wait 5 sample
                    case 0x86: //Write adr2A and Wait 6 sample
                    case 0x87: //Write adr2A and Wait 7 sample
                    case 0x88: //Write adr2A and Wait 8 sample
                    case 0x89: //Write adr2A and Wait 9 sample
                    case 0x8a: //Write adr2A and Wait 10 sample
                    case 0x8b: //Write adr2A and Wait 11 sample
                    case 0x8c: //Write adr2A and Wait 12 sample
                    case 0x8d: //Write adr2A and Wait 13 sample
                    case 0x8e: //Write adr2A and Wait 14 sample
                    case 0x8f: //Write adr2A and Wait 15 sample
                        mds.WriteYM2612(0,0, 0x2a, vgmBuf[vgmPcmPtr++]);
                        vgmWait += (int)(cmd - 0x80);
                        vgmAdr++;
                        break;
                    case 0x90:
                        vgmAdr++;
                        si = vgmBuf[vgmAdr++];
                        vgmStreams[si].chipId = vgmBuf[vgmAdr++];
                        vgmStreams[si].port = vgmBuf[vgmAdr++];
                        vgmStreams[si].cmd = vgmBuf[vgmAdr++];
                        break;
                    case 0x91:
                        vgmAdr++;
                        si = vgmBuf[vgmAdr++];
                        vgmStreams[si].databankId = vgmBuf[vgmAdr++];
                        vgmStreams[si].stepsize = vgmBuf[vgmAdr++];
                        vgmStreams[si].stepbase = vgmBuf[vgmAdr++];
                        break;
                    case 0x92:
                        vgmAdr++;
                        si = vgmBuf[vgmAdr++];
                        vgmStreams[si].frequency = getLE32(vgmAdr);
                        vgmAdr += 4;
                        break;
                    case 0x93:
                        vgmAdr++;
                        si = vgmBuf[vgmAdr++];
                        vgmStreams[si].dataStartOffset = getLE32(vgmAdr);
                        vgmAdr += 4;
                        vgmStreams[si].lengthMode = vgmBuf[vgmAdr++];
                        vgmStreams[si].dataLength = getLE32(vgmAdr);
                        vgmAdr += 4;

                        vgmStreams[si].sw = true;
                        vgmStreams[si].wkDataAdr = vgmStreams[si].dataStartOffset;
                        vgmStreams[si].wkDataLen = vgmStreams[si].dataLength;
                        vgmStreams[si].wkDataStep = 1.0;

                        break;
                    case 0x94:
                        vgmAdr++;
                        si = vgmBuf[vgmAdr++];
                        vgmStreams[si].sw = false;
                        break;
                    case 0x95:
                        vgmAdr++;
                        si = vgmBuf[vgmAdr++];
                        vgmStreams[si].blockId = getLE16(vgmAdr);
                        vgmAdr += 2;
                        p = vgmBuf[vgmAdr++];
                        if ((p & 1) > 0)
                        {
                            vgmStreams[si].lengthMode |= 0x80;
                        }
                        if ((p & 16) > 0)
                        {
                            vgmStreams[si].lengthMode |= 0x10;
                        }

                        vgmStreams[si].sw = true;
                        vgmStreams[si].wkDataAdr = vgmStreams[si].dataStartOffset;
                        vgmStreams[si].wkDataLen = vgmStreams[si].dataLength;
                        vgmStreams[si].wkDataStep = 1.0;

                        break;
                    case 0xa0: //AY8910
                        rAdr = vgmBuf[vgmAdr + 1];
                        rDat = vgmBuf[vgmAdr + 2];
                        vgmAdr += 3;
                        mds.WriteAY8910(0, rAdr, rDat);

                        break;
                    case 0xb7:
                        vgmAdr += 3;
                        break;
                    case 0xe0: //seek to offset in PCM data bank
                        vgmPcmPtr = getLE32(vgmAdr + 1) + vgmPcmBaseAdr;
                        vgmAdr += 5;
                        break;
                    default:
                        //わからんコマンド
                        Console.WriteLine("{0:X}", vgmBuf[vgmAdr]);
                        return;
                }
            }

            oneFrameVGMStream();
            vgmWait--;

        }

        static byte[] bufYM2610AdpcmA = null;
        static byte[] bufYM2610AdpcmB = null;

        private static void oneFrameVGMStream()
        {
            for (int i = 0; i < 0x100; i++)
            {

                if (!vgmStreams[i].sw) continue;
                if (vgmStreams[i].chipId!=0x02) continue;//とりあえずYM2612のみ

                while (vgmStreams[i].wkDataStep >= 1.0)
                {
                    mds.WriteYM2612(0,vgmStreams[i].port, vgmStreams[i].cmd, vgmBuf[vgmPcmBaseAdr + vgmStreams[i].wkDataAdr]);
                    vgmStreams[i].wkDataAdr++;
                    vgmStreams[i].dataLength--;
                    vgmStreams[i].wkDataStep -= 1.0;
                }
                vgmStreams[i].wkDataStep += (double)vgmStreams[i].frequency / (double)SamplingRate;

                if (vgmStreams[i].dataLength <= 0)
                {
                    vgmStreams[i].sw = false;
                }

            }
        }


        private struct vgmStream
        {

            public byte chipId;
            public byte port;
            public byte cmd;

            public byte databankId;
            public byte stepsize;
            public byte stepbase;

            public uint frequency;

            public uint dataStartOffset;
            public byte lengthMode;
            public uint dataLength;

            public bool sw;

            public uint blockId;

            public uint wkDataAdr;
            public uint wkDataLen;
            public double wkDataStep;
        }

        private static UInt32 getLE16(UInt32 adr)
        {
            UInt32 dat;
            dat = (UInt32)vgmBuf[adr] + (UInt32)vgmBuf[adr + 1] * 0x100;

            return dat;
        }

        private static UInt32 getLE32(UInt32 adr)
        {
            UInt32 dat;
            dat = (UInt32)vgmBuf[adr] + (UInt32)vgmBuf[adr + 1] * 0x100 + (UInt32)vgmBuf[adr + 2] * 0x10000 + (UInt32)vgmBuf[adr + 3] * 0x1000000;

            return dat;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (mds == null) return;

            label2.Location = new System.Drawing.Point(Math.Min((mds.getTotalVolumeL() / 600) * 3 - 174, 0), label2.Location.Y);
            label3.Location = new System.Drawing.Point(Math.Min((mds.getTotalVolumeR() / 600) * 3 - 174, 0), label3.Location.Y);
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            if (mds == null) return;
            mds.SetVolumeYM2612( ((TrackBar)sender).Value);
        }
    }
}
