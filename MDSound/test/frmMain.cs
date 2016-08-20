using System;
using System.IO;
using System.Windows.Forms;
using SdlDotNet.Audio;
using System.Runtime.InteropServices;

namespace test
{
    public partial class frmMain : Form
    {

        private static uint SamplingRate = 44100;
        private static uint PSGClockValue = 3579545;
        private static uint FMClockValue = 7670454;
        private static uint rf5c164ClockValue = 12500000;
        private static uint pwmClockValue = 23011361;
        private static uint c140ClockValue = 21390;
        private static MDSound.c140.C140_TYPE c140Type = MDSound.c140.C140_TYPE.ASIC219;

        private static uint samplingBuffer = 1024;
        private static short[] frames = new short[samplingBuffer * 2];
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

            stop();
            play(tbFile.Text);

        }

        private void btnStop_Click(object sender, EventArgs e)
        {

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

            MDSound.MDSound.Chip[] chips = new MDSound.MDSound.Chip[2];

            chips[0] = new MDSound.MDSound.Chip();
            chips[0].type = MDSound.MDSound.enmInstrumentType.SN76489;
            chips[0].ID = 0;
            MDSound.sn76489 sn76489 = new MDSound.sn76489();
            chips[0].Instrument = sn76489;
            chips[0].Update = sn76489.Update;
            chips[0].Start = sn76489.Start;
            chips[0].Stop = sn76489.Stop;
            chips[0].Reset = sn76489.Reset;
            chips[0].SamplingRate = SamplingRate;
            chips[0].Clock = PSGClockValue;
            chips[0].Volume =50;
            chips[0].Option = null;

            chips[1] = new MDSound.MDSound.Chip();
            chips[1].type = MDSound.MDSound.enmInstrumentType.YM2612;
            chips[1].ID = 0;
            MDSound.ym2612 ym2612 = new MDSound.ym2612();
            chips[1].Instrument = ym2612;
            chips[1].Update = ym2612.Update;
            chips[1].Start = ym2612.Start;
            chips[1].Stop = ym2612.Stop;
            chips[1].Reset = ym2612.Reset;
            chips[1].SamplingRate = SamplingRate;
            chips[1].Clock = FMClockValue;
            chips[1].Volume = 100;
            chips[1].Option = null;

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

            mds = new MDSound.MDSound(SamplingRate, samplingBuffer, chips);

            sdlCbHandle = GCHandle.Alloc(sdlCb);
            sdlCbPtr = Marshal.GetFunctionPointerForDelegate(sdlCb);
            sdl = new SdlDotNet.Audio.AudioStream((int)SamplingRate, AudioFormat.Signed16Little, SoundChannel.Stereo, (short)samplingBuffer, sdlCb, null);
            sdl.Paused = false;

        }

        private static void callback(IntPtr userData, IntPtr stream, int len)
        {
            mds.Update(frames, 0, frames.Length, oneFrameVGM);

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
                switch (cmd)
                {
                    case 0x4f: //GG PSG
                    case 0x50: //PSG
                        mds.WriteSN76489(vgmBuf[vgmAdr + 1]);
                        vgmAdr += 2;
                        break;
                    case 0x52: //YM2612 Port0
                    case 0x53: //YM2612 Port1
                        p = (byte)((cmd == 0x52) ? 0 : 1);
                        rAdr = vgmBuf[vgmAdr + 1];
                        rDat = vgmBuf[vgmAdr + 2];
                        vgmAdr += 3;
                        mds.WriteYM2612(p, rAdr, rDat);

                        break;
                    case 0x55: //YM2203
                        vgmAdr += 3;

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
                        vgmAdr += getLE32(vgmAdr + 3) + 7;
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
                        mds.WriteYM2612(0, 0x2a, vgmBuf[vgmPcmPtr++]);
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
                        vgmStreams[si].lengthMode = vgmBuf[vgmAdr++];//用途がいまいちわかってません
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
                        //使い方がいまいちわかってません
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

        private static void oneFrameVGMStream()
        {
            for (int i = 0; i < 0x100; i++)
            {

                if (!vgmStreams[i].sw) continue;
                if (vgmStreams[i].chipId!=0x02) continue;//とりあえずYM2612のみ

                while (vgmStreams[i].wkDataStep >= 1.0)
                {
                    mds.WriteYM2612(vgmStreams[i].port, vgmStreams[i].cmd, vgmBuf[vgmPcmBaseAdr + vgmStreams[i].wkDataAdr]);
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
    }
}
