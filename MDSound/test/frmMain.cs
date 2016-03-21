using System;
using System.IO;
using System.Windows.Forms;
using SdlDotNet.Audio;
using System.Runtime.InteropServices;

namespace test
{
    public partial class frmMain : Form
    {

        private static int SamplingRate = 44100;
        private static int PSGClockValue = 3579545;
        private static int FMClockValue = 7670454;
        private static int samplingBuffer = 1024;

        private static short[] frames = new short[samplingBuffer * 2];
        private static MDSound.MDSound mds = new MDSound.MDSound(SamplingRate, samplingBuffer, FMClockValue, PSGClockValue);

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

            mds.Init(SamplingRate, samplingBuffer, FMClockValue, PSGClockValue);

            sdlCbHandle = GCHandle.Alloc(sdlCb);
            sdlCbPtr = Marshal.GetFunctionPointerForDelegate(sdlCb);
            sdl = new SdlDotNet.Audio.AudioStream(SamplingRate, AudioFormat.Signed16Little, SoundChannel.Stereo, (short)samplingBuffer, sdlCb, null);
            sdl.Paused = false;

        }

        private static void callback(IntPtr userData, IntPtr stream, int len)
        {

            int[][] buf = mds.Update2(oneFrameVGM);

            for (int i = 0; i < len / 4; i++)
            {
                frames[i * 2 + 0] = (short)buf[0][i];
                frames[i * 2 + 1] = (short)buf[1][i];
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
                switch (cmd)
                {
                    case 0x4f: //GG PSG
                    case 0x50: //PSG
                        mds.WritePSG(vgmBuf[vgmAdr + 1]);
                        vgmAdr += 2;
                        break;
                    case 0x52: //YM2612 Port0
                    case 0x53: //YM2612 Port1
                        p = (byte)((cmd == 0x52) ? 0 : 1);
                        rAdr = vgmBuf[vgmAdr + 1];
                        rDat = vgmBuf[vgmAdr + 2];
                        vgmAdr += 3;
                        mds.WriteFM(p, rAdr, rDat);

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
                        mds.WriteFM(0, 0x2a, vgmBuf[vgmPcmPtr++]);
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
                    mds.WriteFM(vgmStreams[i].port, vgmStreams[i].cmd, vgmBuf[vgmPcmBaseAdr + vgmStreams[i].wkDataAdr]);
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
            label2.Location = new System.Drawing.Point(Math.Min((mds.getTotalVolumeL() / 600) * 3 - 174, 0), label2.Location.Y);
            label3.Location = new System.Drawing.Point(Math.Min((mds.getTotalVolumeR() / 600) * 3 - 174, 0), label3.Location.Y);
        }
    }
}
