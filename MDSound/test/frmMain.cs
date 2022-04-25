using System;
using System.IO;
using System.Windows.Forms;
using SdlDotNet.Audio;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Threading;
using SoundManager;

namespace test
{
    public partial class FrmMain : Form
    {

        private static uint SamplingRate = 44100;
        private static uint samplingBuffer = 1024*8;
        private static short[] frames = new short[samplingBuffer * 4];
        private static MDSound.MDSound mds = null;
        
        private static AudioStream sdl;
        private static AudioCallback sdlCb = new AudioCallback(EmuCallback);
        private static IntPtr sdlCbPtr;
        private static GCHandle sdlCbHandle;

        private static byte[] vgmBuf = null;
        private static uint vgmPcmPtr;
        private static uint vgmPcmBaseAdr;
        private static uint vgmAdr;
        private static uint vgmEof;
        private static bool vgmAnalyze;
        private static VgmStream[] vgmStreams = new VgmStream[0x100];

        private static byte[] bufYM2610AdpcmA = null;
        private static byte[] bufYM2610AdpcmB = null;

        private struct VgmStream
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

        private static short[] emuRenderBuf = new short[2];
        private static long PackCounter = 0;
        private static Pack Pack = new Pack();

        private static long DriverSeqCounter = 0;
        private static long EmuSeqCounter = 0;

        private const int PCM_BANK_COUNT = 0x40;
        private static VGM_PCM_BANK[] PCMBank = new VGM_PCM_BANK[PCM_BANK_COUNT];
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

        private static SoundManager.SoundManager sm;
        private static Enq enq;
        private static RealChip rc = null;
        private static RSoundChip rsc = null;
        private static RingBuffer emuRecvBuffer = null;


        public FrmMain()
        {

            InitializeComponent();

            //実チップ(OPNA)を探す(無ければrscはnull)
            rc = new RealChip();
            rsc = rc.SearchOPNA();
            if (rsc != null)
            {
                rsc.init();
            }

            Mount();

            mds = new MDSound.MDSound(SamplingRate, samplingBuffer, null);

            MDSound.Log.level = MDSound.LogLevel.TRACE;
            MDSound.Log.writeLine = LogWrite;

            sdlCbHandle = GCHandle.Alloc(sdlCb);
            sdlCbPtr = Marshal.GetFunctionPointerForDelegate(sdlCb);
            sdl = new SdlDotNet.Audio.AudioStream((int)SamplingRate, AudioFormat.Signed16Little, SoundChannel.Stereo, (short)samplingBuffer, sdlCb, null)
            {
                Paused = true
            };

        }

        private void LogWrite(MDSound.LogLevel level,string msg)
        {
            log.Write(msg);
        }


        private void BtnRef_Click(object sender, EventArgs e)
        {

            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter =
                "VGMファイル(*.vgm)|*.vgm",
                Title = "ファイルを選択してください",
                RestoreDirectory = true,
                CheckPathExists = true
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                tbFile.Text = ofd.FileName;
            }

        }

        private void BtnPlay_Click(object sender, EventArgs e)
        {
            btnPlay.Enabled = false;

            sm.RequestStop();
            while (sm.IsRunningAsync())
            {
                Thread.Sleep(1);
            }

            DriverSeqCounter = sm.GetDriverSeqCounterDelay();
            DriverSeqCounter = 0;

            Play(tbFile.Text);

            sm.RequestStart();
            while (!sm.IsRunningAsync()) ;

            if (rsc == null)
            {
                sm.RequestStopAtRealChipSender();
                while (sm.IsRunningAtRealChipSender()) ;
            }
            else
            {
                sm.RequestStopAtEmuChipSender();
                while (sm.IsRunningAtEmuChipSender()) ;
            }

            btnPlay.Enabled = true;
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            btnStop.Enabled = false;

            sm.RequestStop();
            while (sm.IsRunningAsync()) ;

            btnStop.Enabled = true;
        }

        private void FrmMain_FormClosed(object sender, FormClosedEventArgs e)
        {

            sm.RequestStop();
            while (sm.IsRunningAsync()) ;

            Unmount();

            if (rc != null)
            {
                rc.Close();
            }

            if (sdl == null) return;

            sdl.Paused = true;
            sdl.Close();
            sdl.Dispose();
            sdl = null;
            if (sdlCbHandle.IsAllocated) sdlCbHandle.Free();
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            if (mds == null) return;
            int l = mds.getTotalVolumeL();
            int r = mds.getTotalVolumeR();

            label2.Location = new System.Drawing.Point(Math.Min((l / 600) * 3 - 174, 0), label2.Location.Y);
            label3.Location = new System.Drawing.Point(Math.Min((r / 600) * 3 - 174, 0), label3.Location.Y);

            lblDriverSeqCounter.Text = DriverSeqCounter.ToString();
            lblEmuSeqCounter.Text = EmuSeqCounter.ToString();
            lblSeqCounter.Text = sm.GetSeqCounter().ToString();
            lblDataSenderBufferCounter.Text = sm.GetDataSenderBufferCounter().ToString();
            lblDataSenderBufferSize.Text = sm.GetDataSenderBufferSize().ToString();
            lblEmuChipSenderBufferSize.Text = sm.GetEmuChipSenderBufferSize().ToString();
            lblRealChipSenderBufferSize.Text = sm.GetRealChipSenderBufferSize().ToString();

            lblDataMakerIsRunning.Text = sm.IsRunningAtDataMaker() ? "Running" : "Stop";
            lblDataSenderIsRunning.Text = sm.IsRunningAtDataSender() ? "Running" : "Stop";
            lblEmuChipSenderIsRunning.Text = sm.IsRunningAtEmuChipSender() ? "Running" : "Stop";
            lblRealChipSenderIsRunning.Text = sm.IsRunningAtRealChipSender() ? "Running" : "Stop";

            lblInterrupt.Text = sm.GetInterrupt() ? "Enable" : "Disable";

            lblDebug.Text = mds.GetDebugMsg();
        }





        private void Mount()
        {
            sm = new SoundManager.SoundManager();
            DriverAction DriverAction = new DriverAction();
            DriverAction.Main = DriverActionMain;
            DriverAction.Final = DriverActionFinal;

            if (rsc == null)
            {
                sm.Setup(DriverAction, null, SoftInitYM2608(0x56), SoftResetYM2608(0x56));
            }
            else
            {
                sm.Setup(DriverAction, RealChipAction, SoftInitYM2608(-1), SoftResetYM2608(-1));
            }

            enq = sm.GetDriverDataEnqueue();
            emuRecvBuffer = sm.GetEmuRecvBuffer();
        }

        private void DriverActionMain()
        {
            //OneFrameVGM();
        }

        private void DriverActionFinal()
        {
            Pack[] data;

            if (rsc == null)
            {
                data = SoftResetYM2608(0x56);
                DataEnq(DriverSeqCounter, 0x56, 0, -1, -1, data);
            }
            else
            {
                data = SoftResetYM2608(-1);
                DataEnq(DriverSeqCounter, -1, 0, -1, -1, data);
            }
        }

        private void RealChipAction(long counter, int dev, int typ, int adr, int val, object[] ex)
        {
            if (adr >= 0)
            {
                rsc.setRegister(adr, val);
            }
            else
            {
                sm.SetInterrupt();
                try
                {
                    Pack[] data = (Pack[])ex;
                    foreach (Pack dat in data)
                    {
                        rsc.setRegister(dat.Adr, dat.Val);
                    }
                    rc.WaitOPNADPCMData(true);
                }
                finally
                {
                    sm.ResetInterrupt();
                }
            }
        }

        private void Unmount()
        {
            sm.RequestStop();
            while (sm.IsRunningAsync()) ;
            sm.Release();
        }

        public static void DataEnq(long counter, int dev, int typ, int adr, int val, object[] ex)
        {
            while (!enq(counter, dev, typ, adr, val, ex)) Thread.Sleep(1);
        }


        private static Pack[] SoftInitYM2608(int dev)
        {
            List<Pack> data = new List<Pack>();
            byte i;

            data.Add(new Pack(dev, 0, 0x2d, 0x00, null));
            data.Add(new Pack(dev, 0, 0x29, 0x82, null));
            data.Add(new Pack(dev, 0, 0x07, 0x38, null)); //PSG TONE でリセット
            for (i = 0xb4; i < 0xb4 + 3; i++)
            {
                data.Add(new Pack(dev, 0, i, 0xc0, null));
                data.Add(new Pack(dev, 0, 0x100 + i, 0xc0, null));
            }

            return data.ToArray();
        }

        private static Pack[] SoftResetYM2608(int dev)
        {
            List<Pack> data = new List<Pack>();
            byte i;

            // FM全チャネルキーオフ
            data.Add(new Pack(dev, 0, 0x28, 0x00, null));
            data.Add(new Pack(dev, 0, 0x28, 0x01, null));
            data.Add(new Pack(dev, 0, 0x28, 0x02, null));
            data.Add(new Pack(dev, 0, 0x28, 0x04, null));
            data.Add(new Pack(dev, 0, 0x28, 0x05, null));
            data.Add(new Pack(dev, 0, 0x28, 0x06, null));

            // FM TL=127
            for (i = 0x40; i < 0x4F + 1; i++)
            {
                data.Add(new Pack(dev, 0, i, 0x7f, null));
                data.Add(new Pack(dev, 0, 0x100 + i, 0x7f, null));
            }
            // FM ML/DT
            for (i = 0x30; i < 0x3F + 1; i++)
            {
                data.Add(new Pack(dev, 0, i, 0x0, null));
                data.Add(new Pack(dev, 0, 0x100 + i, 0x0, null));
            }
            // FM AR,DR,SR,KS,AMON
            for (i = 0x50; i < 0x7F + 1; i++)
            {
                data.Add(new Pack(dev, 0, i, 0x0, null));
                data.Add(new Pack(dev, 0, 0x100 + i, 0x0, null));
            }
            // FM SL,RR
            for (i = 0x80; i < 0x8F + 1; i++)
            {
                data.Add(new Pack(dev, 0, i, 0xff, null));
                data.Add(new Pack(dev, 0, 0x100 + i, 0xff, null));
            }
            // FM F-Num, FB/CONNECT
            for (i = 0x90; i < 0xBF + 1; i++)
            {
                data.Add(new Pack(dev, 0, i, 0x0, null));
                data.Add(new Pack(dev, 0, 0x100 + i, 0x0, null));
            }
            // FM PAN/AMS/PMS
            for (i = 0xB4; i < 0xB6 + 1; i++)
            {
                data.Add(new Pack(dev, 0, i, 0xc0, null));
                data.Add(new Pack(dev, 0, 0x100 + i, 0xc0, null));
            }
            data.Add(new Pack(dev, 0, 0x22, 0x00, null)); // HW LFO
            data.Add(new Pack(dev, 0, 0x24, 0x00, null)); // Timer-A(1)
            data.Add(new Pack(dev, 0, 0x25, 0x00, null)); // Timer-A(2)
            data.Add(new Pack(dev, 0, 0x26, 0x00, null)); // Timer-B
            data.Add(new Pack(dev, 0, 0x27, 0x30, null)); // Timer Control
            data.Add(new Pack(dev, 0, 0x29, 0x80, null)); // FM4-6 Enable

            // SSG 音程(2byte*3ch)
            for (i = 0x00; i < 0x05 + 1; i++)
            {
                data.Add(new Pack(dev, 0, i, 0x00, null));
            }
            data.Add(new Pack(dev, 0, 0x06, 0x00, null));// SSG ノイズ周波数
            data.Add(new Pack(dev, 0, 0x07, 0x38, null)); // SSG ミキサ
                                                          // SSG ボリューム(3ch)
            for (i = 0x08; i < 0x0A + 1; i++)
            {
                data.Add(new Pack(dev, 0, i, 0x00, null));
            }
            // SSG Envelope
            for (i = 0x0B; i < 0x0D + 1; i++)
            {
                data.Add(new Pack(dev, 0, i, 0x00, null));
            }

            // RHYTHM
            data.Add(new Pack(dev, 0, 0x10, 0xBF, null)); // 強制発音停止
            data.Add(new Pack(dev, 0, 0x11, 0x00, null)); // Total Level
            data.Add(new Pack(dev, 0, 0x18, 0x00, null)); // BD音量
            data.Add(new Pack(dev, 0, 0x19, 0x00, null)); // SD音量
            data.Add(new Pack(dev, 0, 0x1A, 0x00, null)); // CYM音量
            data.Add(new Pack(dev, 0, 0x1B, 0x00, null)); // HH音量
            data.Add(new Pack(dev, 0, 0x1C, 0x00, null)); // TOM音量
            data.Add(new Pack(dev, 0, 0x1D, 0x00, null)); // RIM音量

            // ADPCM
            data.Add(new Pack(dev, 0, 0x100 + 0x00, 0x21, null)); // ADPCMリセット
            data.Add(new Pack(dev, 0, 0x100 + 0x01, 0x06, null)); // ADPCM消音
            data.Add(new Pack(dev, 0, 0x100 + 0x10, 0x9C, null)); // FLAGリセット        

            return data.ToArray();
        }

        //private static void Play(string fileName)
        //{
        //    sdl.Paused = true;

        //    try
        //    {
        //        vgmBuf = File.ReadAllBytes(fileName);
        //    }
        //    catch
        //    {
        //        MessageBox.Show("ファイルの読み込みに失敗しました。");
        //        return;
        //    }

        //    for(int i = 0; i < PCMBank.Length; i++)
        //    {
        //        PCMBank[i] = new VGM_PCM_BANK();
        //    }

        //    //ヘッダーを読み込めるサイズをもっているかチェック
        //    if (vgmBuf.Length < 0x40) return;

        //    //ヘッダーから情報取得

        //    uint vgm = GetLE32(0x00);
        //    if (vgm != 0x206d6756) return;

        //    uint version = GetLE32(0x08);
        //    //if (version < 0x0150) return;

        //    vgmEof = GetLE32(0x04);

        //    uint vgmDataOffset = GetLE32(0x34);
        //    if (vgmDataOffset == 0)
        //    {
        //        vgmDataOffset = 0x40;
        //    }
        //    else
        //    {
        //        vgmDataOffset += 0x34;
        //    }

        //    vgmAdr = vgmDataOffset;
        //    vgmAnalyze = true;

        //    MDSound.MDSound.Chip[] chips = null;
        //    List<MDSound.MDSound.Chip> lstChip = new List<MDSound.MDSound.Chip>();
        //    MDSound.MDSound.Chip chip = null;

        //    if (GetLE32(0x0c) != 0)
        //    {
        //        chip = new MDSound.MDSound.Chip
        //        {
        //            type = MDSound.MDSound.enmInstrumentType.SN76489,
        //            ID = 0
        //        };
        //        //MDSound.sn76489 sn76489 = new MDSound.sn76489();
        //        MDSound.SN76496 sn76489 = new MDSound.SN76496();
        //        chip.Instrument = sn76489;
        //        chip.Update = sn76489.Update;
        //        chip.Start = sn76489.Start;
        //        chip.Stop = sn76489.Stop;
        //        chip.Reset = sn76489.Reset;
        //        chip.SamplingRate = SamplingRate;
        //        chip.Clock = GetLE32(0x0c);
        //        chip.Volume = 0;
        //        if (version < 0x0150)
        //        {
        //            chip.Option = new object[]{
        //                        (byte)9,
        //                        (byte)0,
        //                        (byte)16,
        //                        (byte)0
        //            };
        //        }
        //        else
        //        {
        //            chip.Option = new object[]{
        //                        vgmBuf[0x28],
        //                        vgmBuf[0x29],
        //                        vgmBuf[0x2a],
        //                        vgmBuf[0x2b]
        //            };
        //        }
        //        lstChip.Add(chip);
        //    }

        //    //chip = new MDSound.MDSound.Chip
        //    //{
        //    //    type = MDSound.MDSound.enmInstrumentType.YM2413,
        //    //    ID = 0
        //    //};
        //    //MDSound.SinWave sin = new MDSound.SinWave();
        //    //chip.Instrument = sin;
        //    //chip.Update = sin.Update;
        //    //chip.Start = sin.Start;
        //    //chip.Stop = sin.Stop;
        //    //chip.Reset = sin.Reset;
        //    //chip.SamplingRate = SamplingRate;
        //    //chip.Clock = 0;
        //    //chip.Volume = 0;
        //    //chip.Option = null;
        //    //lstChip.Add(chip);

        //    if (GetLE32(0x10) != 0)
        //    {
        //        chip = new MDSound.MDSound.Chip
        //        {
        //            type = MDSound.MDSound.enmInstrumentType.YM2413,
        //            ID = 0
        //        };
        //        MDSound.ym2413 ym2413 = new MDSound.ym2413();
        //        chip.Instrument = ym2413;
        //        chip.Update = ym2413.Update;
        //        chip.Start = ym2413.Start;
        //        chip.Stop = ym2413.Stop;
        //        chip.Reset = ym2413.Reset;
        //        chip.SamplingRate = SamplingRate;
        //        chip.Clock = GetLE32(0x10); 
        //        chip.Volume = 0;
        //        chip.Option = null;
        //        lstChip.Add(chip);
        //    }

        //    if (GetLE32(0x2c) != 0)
        //    {
        //        chip = new MDSound.MDSound.Chip();
        //        chip.type = MDSound.MDSound.enmInstrumentType.YM2612;
        //        //chip.type = MDSound.MDSound.enmInstrumentType.YM3438;
        //        chip.ID = 0;
        //        //MDSound.ym2612 ym2612 = new MDSound.ym2612();
        //        //MDSound.ym3438 ym2612 = new MDSound.ym3438();
        //        MDSound.ym2612mame ym2612 = new MDSound.ym2612mame();
        //        chip.Instrument = ym2612;
        //        chip.Update = ym2612.Update;
        //        chip.Start = ym2612.Start;
        //        chip.Stop = ym2612.Stop;
        //        chip.Reset = ym2612.Reset;
        //        chip.SamplingRate = SamplingRate;
        //        chip.Clock = GetLE32(0x2c); 
        //        chip.Volume = 0;
        //        chip.Option = null;
        //        lstChip.Add(chip);
        //    }

        //    if (GetLE32(0x30) != 0)
        //    {
        //        chip = new MDSound.MDSound.Chip();
        //        chip.type = MDSound.MDSound.enmInstrumentType.YM2151;
        //        chip.ID = 0;
        //        //MDSound.ym2151 ym2151 = new MDSound.ym2151();
        //        //MDSound.ym2151_mame ym2151 = new MDSound.ym2151_mame();
        //        MDSound.ym2151_x68sound ym2151 = new MDSound.ym2151_x68sound();
        //        chip.Instrument = ym2151;
        //        chip.Update = ym2151.Update;
        //        chip.Start = ym2151.Start;
        //        chip.Stop = ym2151.Stop;
        //        chip.Reset = ym2151.Reset;
        //        chip.SamplingRate = SamplingRate;
        //        chip.Clock = GetLE32(0x30); 
        //        chip.Volume = 0;
        //        chip.Option = null;
        //        lstChip.Add(chip);
        //    }

        //    if (GetLE32(0x38) != 0 && 0x38 < vgmDataOffset - 3)
        //    {
        //        chip = new MDSound.MDSound.Chip();
        //        chip.type = MDSound.MDSound.enmInstrumentType.SEGAPCM;
        //        chip.ID = 0;
        //        MDSound.segapcm segapcm = new MDSound.segapcm();
        //        chip.Instrument = segapcm;
        //        chip.Update = segapcm.Update;
        //        chip.Start = segapcm.Start;
        //        chip.Stop = segapcm.Stop;
        //        chip.Reset = segapcm.Reset;
        //        chip.SamplingRate = SamplingRate;
        //        chip.Clock = GetLE32(0x38);
        //        chip.Option = new object[1] { (int)GetLE32(0x3c) };
        //        chip.Volume = 0;

        //        lstChip.Add(chip);
        //    }

        //    if (GetLE32(0x44) != 0 && 0x44 < vgmDataOffset - 3)
        //    {
        //        chip = new MDSound.MDSound.Chip();
        //        chip.type = MDSound.MDSound.enmInstrumentType.YM2203;
        //        chip.ID = 0;
        //        MDSound.ym2203 ym2203 = new MDSound.ym2203();
        //        chip.Instrument = ym2203;
        //        chip.Update = ym2203.Update;
        //        chip.Start = ym2203.Start;
        //        chip.Stop = ym2203.Stop;
        //        chip.Reset = ym2203.Reset;
        //        chip.SamplingRate = SamplingRate;
        //        chip.Clock = GetLE32(0x44);
        //        chip.Volume = 0;
        //        chip.Option = null;
        //        lstChip.Add(chip);
        //    }

        //    if (GetLE32(0x48) != 0 && 0x48 < vgmDataOffset - 3)
        //    {
        //        chip = new MDSound.MDSound.Chip();
        //        chip.type = MDSound.MDSound.enmInstrumentType.YM2608;
        //        chip.ID = 0;
        //        MDSound.ym2608 ym2608 = new MDSound.ym2608();
        //        chip.Instrument = ym2608;
        //        chip.Update = ym2608.Update;
        //        chip.Start = ym2608.Start;
        //        chip.Stop = ym2608.Stop;
        //        chip.Reset = ym2608.Reset;
        //        chip.SamplingRate = SamplingRate;
        //        chip.Clock = GetLE32(0x48);
        //        chip.Volume = 0;
        //        chip.Option = null;
        //        lstChip.Add(chip);
        //    }
        //    //if (GetLE32(0x48) != 0 && 0x48 < vgmDataOffset - 3)
        //    //{
        //    //    chip = new MDSound.MDSound.Chip();
        //    //    chip.type = MDSound.MDSound.enmInstrumentType.YM2609;
        //    //    chip.ID = 0;
        //    //    MDSound.ym2609 ym2609 = new MDSound.ym2609();
        //    //    chip.Instrument = ym2609;
        //    //    chip.Update = ym2609.Update;
        //    //    chip.Start = ym2609.Start;
        //    //    chip.Stop = ym2609.Stop;
        //    //    chip.Reset = ym2609.Reset;
        //    //    chip.SamplingRate = SamplingRate;
        //    //    chip.Clock = GetLE32(0x48);
        //    //    chip.Volume = 0;
        //    //    chip.Option = null;
        //    //    lstChip.Add(chip);
        //    //}

        //    if (GetLE32(0x4c) != 0 && 0x4c < vgmDataOffset - 3)
        //    {
        //        chip = new MDSound.MDSound.Chip();
        //        chip.type = MDSound.MDSound.enmInstrumentType.YM2610;
        //        chip.ID = 0;
        //        MDSound.ym2610 ym2610 = new MDSound.ym2610();
        //        chip.Instrument = ym2610;
        //        chip.Update = ym2610.Update;
        //        chip.Start = ym2610.Start;
        //        chip.Stop = ym2610.Stop;
        //        chip.Reset = ym2610.Reset;
        //        chip.SamplingRate = SamplingRate;
        //        chip.Clock = GetLE32(0x4c) & 0x7fffffff;
        //        chip.Volume = 0;
        //        chip.Option = null;
        //        bufYM2610AdpcmA = null;
        //        bufYM2610AdpcmB = null;
        //        lstChip.Add(chip);
        //    }

        //    if (GetLE32(0x50) != 0 && 0x50 < vgmDataOffset - 3)
        //    {
        //        chip = new MDSound.MDSound.Chip();
        //        chip.type = MDSound.MDSound.enmInstrumentType.YM3812;
        //        chip.ID = 0;
        //        MDSound.ym3812 ym3812 = new MDSound.ym3812();
        //        chip.Instrument = ym3812;
        //        chip.Update = ym3812.Update;
        //        chip.Start = ym3812.Start;
        //        chip.Stop = ym3812.Stop;
        //        chip.Reset = ym3812.Reset;
        //        chip.SamplingRate = SamplingRate;
        //        chip.Clock = GetLE32(0x50) & 0x7fffffff;
        //        chip.Volume = 0;
        //        chip.Option = null;
        //        lstChip.Add(chip);
        //    }

        //    if (GetLE32(0x54) != 0 && 0x54 < vgmDataOffset - 3)
        //    {
        //        chip = new MDSound.MDSound.Chip();
        //        chip.type = MDSound.MDSound.enmInstrumentType.YM3526;
        //        chip.ID = 0;
        //        MDSound.ym3526 ym3526 = new MDSound.ym3526();
        //        chip.Instrument = ym3526;
        //        chip.Update = ym3526.Update;
        //        chip.Start = ym3526.Start;
        //        chip.Stop = ym3526.Stop;
        //        chip.Reset = ym3526.Reset;
        //        chip.SamplingRate = SamplingRate;
        //        chip.Clock = GetLE32(0x54) & 0x7fffffff;
        //        chip.Volume = 0;
        //        chip.Option = null;
        //        lstChip.Add(chip);
        //    }

        //    if (GetLE32(0x5c) != 0 && 0x5c < vgmDataOffset - 3)
        //    {
        //        chip = new MDSound.MDSound.Chip();
        //        chip.type = MDSound.MDSound.enmInstrumentType.YMF262;
        //        chip.ID = 0;
        //        MDSound.ymf262 ymf262 = new MDSound.ymf262();
        //        chip.Instrument = ymf262;
        //        chip.Update = ymf262.Update;
        //        chip.Start = ymf262.Start;
        //        chip.Stop = ymf262.Stop;
        //        chip.Reset = ymf262.Reset;
        //        chip.SamplingRate = SamplingRate;
        //        chip.Clock = GetLE32(0x5c) & 0x7fffffff;
        //        chip.Volume = 0;
        //        chip.Option = null;
        //        lstChip.Add(chip);

        //        //chip = new MDSound.MDSound.Chip();
        //        //chip.type = MDSound.MDSound.enmInstrumentType.YMF278B;
        //        //chip.ID = 0;
        //        //MDSound.ymf278b ymf278b = new MDSound.ymf278b();
        //        //chip.Instrument = ymf278b;
        //        //chip.Update = ymf278b.Update;
        //        //chip.Start = ymf278b.Start;
        //        //chip.Stop = ymf278b.Stop;
        //        //chip.Reset = ymf278b.Reset;
        //        //chip.SamplingRate = SamplingRate;
        //        //chip.Clock = getLE32(0x5c) & 0x7fffffff;
        //        //chip.Volume = 0;
        //        //chip.Option = null;
        //        //lstChip.Add(chip);
        //    }

        //    if (GetLE32(0x58) != 0 && 0x58 < vgmDataOffset - 3)
        //    {
        //        chip = new MDSound.MDSound.Chip();
        //        chip.type = MDSound.MDSound.enmInstrumentType.Y8950;
        //        chip.ID = 0;
        //        MDSound.y8950 y8950 = new MDSound.y8950();
        //        chip.Instrument = y8950;
        //        chip.Update = y8950.Update;
        //        chip.Start = y8950.Start;
        //        chip.Stop = y8950.Stop;
        //        chip.Reset = y8950.Reset;
        //        chip.SamplingRate = SamplingRate;
        //        chip.Clock = GetLE32(0x58) & 0x7fffffff;
        //        chip.Volume = 0;
        //        chip.Option = null;
        //        lstChip.Add(chip);
        //    }

        //    if (GetLE32(0x60) != 0 && 0x60 < vgmDataOffset - 3)
        //    {
        //        chip = new MDSound.MDSound.Chip();
        //        chip.type = MDSound.MDSound.enmInstrumentType.YMF278B;
        //        chip.ID = 0;
        //        MDSound.ymf278b ymf278b = new MDSound.ymf278b();
        //        chip.Instrument = ymf278b;
        //        chip.Update = ymf278b.Update;
        //        chip.Start = ymf278b.Start;
        //        chip.Stop = ymf278b.Stop;
        //        chip.Reset = ymf278b.Reset;
        //        chip.SamplingRate = SamplingRate;
        //        chip.Clock = GetLE32(0x60) & 0x7fffffff;
        //        chip.Volume = 0;
        //        chip.Option = null;
        //        lstChip.Add(chip);
        //    }

        //    if (GetLE32(0x64) != 0 && 0x64 < vgmDataOffset - 3)
        //    {
        //        chip = new MDSound.MDSound.Chip();
        //        chip.type = MDSound.MDSound.enmInstrumentType.YMF271;
        //        chip.ID = 0;
        //        MDSound.ymf271 ymf271 = new MDSound.ymf271();
        //        chip.Instrument = ymf271;
        //        chip.Update = ymf271.Update;
        //        chip.Start = ymf271.Start;
        //        chip.Stop = ymf271.Stop;
        //        chip.Reset = ymf271.Reset;
        //        chip.SamplingRate = SamplingRate;
        //        chip.Clock = GetLE32(0x64) & 0x7fffffff;
        //        chip.Volume = 0;
        //        chip.Option = null;
        //        lstChip.Add(chip);
        //    }

        //    if (GetLE32(0x68) != 0 && 0x68 < vgmDataOffset - 3)
        //    {
        //        chip = new MDSound.MDSound.Chip();
        //        chip.type = MDSound.MDSound.enmInstrumentType.YMZ280B;
        //        chip.ID = 0;
        //        MDSound.ymz280b ymz280b = new MDSound.ymz280b();
        //        chip.Instrument = ymz280b;
        //        chip.Update = ymz280b.Update;
        //        chip.Start = ymz280b.Start;
        //        chip.Stop = ymz280b.Stop;
        //        chip.Reset = ymz280b.Reset;
        //        chip.SamplingRate = SamplingRate;
        //        chip.Clock = GetLE32(0x68) & 0x7fffffff;
        //        chip.Volume = 0;
        //        chip.Option = null;
        //        lstChip.Add(chip);
        //    }

        //    if (GetLE32(0x74) != 0 && 0x74 < vgmDataOffset - 3)
        //    {
        //        chip = new MDSound.MDSound.Chip();
        //        chip.type = MDSound.MDSound.enmInstrumentType.AY8910;
        //        chip.ID = 0;
        //        //MDSound.ay8910 ay8910 = new MDSound.ay8910();
        //        MDSound.ay8910_mame ay8910 = new MDSound.ay8910_mame();
        //        chip.Instrument = ay8910;
        //        chip.Update = ay8910.Update;
        //        chip.Start = ay8910.Start;
        //        chip.Stop = ay8910.Stop;
        //        chip.Reset = ay8910.Reset;
        //        chip.SamplingRate = SamplingRate;
        //        chip.Clock = GetLE32(0x74) & 0x7fffffff;
        //        chip.Clock /= 2;
        //        if ((vgmBuf[0x79] & 0x10) != 0)
        //            chip.Clock /= 2;
        //        chip.Volume = 0;
        //        chip.Option = null;
        //        lstChip.Add(chip);
        //    }

        //    if (version >= 0x0161 && 0x80 < vgmDataOffset - 3)
        //    {

        //        if (GetLE32(0x80) != 0 && 0x80 < vgmDataOffset - 3)
        //        {
        //            chip = new MDSound.MDSound.Chip();
        //            chip.type = MDSound.MDSound.enmInstrumentType.DMG;
        //            chip.ID = 0;
        //            MDSound.gb gb = new MDSound.gb();
        //            chip.Instrument = gb;
        //            chip.Update = gb.Update;
        //            chip.Start = gb.Start;
        //            chip.Stop = gb.Stop;
        //            chip.Reset = gb.Reset;
        //            chip.SamplingRate = SamplingRate;
        //            chip.Clock = GetLE32(0x80);// & 0x7fffffff;
        //            chip.Volume = 0;
        //            chip.Option = null;
        //            lstChip.Add(chip);
        //        }

        //        if (GetLE32(0x84) != 0 && 0x84 < vgmDataOffset - 3)
        //        {
        //            chip = new MDSound.MDSound.Chip();
        //            chip.type = MDSound.MDSound.enmInstrumentType.Nes;
        //            chip.ID = 0;
        //            MDSound.nes_intf nes_intf = new MDSound.nes_intf();
        //            chip.Instrument = nes_intf;
        //            chip.Update = nes_intf.Update;
        //            chip.Start = nes_intf.Start;
        //            chip.Stop = nes_intf.Stop;
        //            chip.Reset = nes_intf.Reset;
        //            chip.SamplingRate = SamplingRate;
        //            chip.Clock = GetLE32(0x84);// & 0x7fffffff;
        //            chip.Volume = 0;
        //            chip.Option = null;
        //            lstChip.Add(chip);
        //        }

        //        if (GetLE32(0x88) != 0 && 0x88 < vgmDataOffset - 3)
        //        {
        //            chip = new MDSound.MDSound.Chip();
        //            chip.type = MDSound.MDSound.enmInstrumentType.MultiPCM;
        //            chip.ID = 0;
        //            MDSound.multipcm multipcm = new MDSound.multipcm();
        //            chip.Instrument = multipcm;
        //            chip.Update = multipcm.Update;
        //            chip.Start = multipcm.Start;
        //            chip.Stop = multipcm.Stop;
        //            chip.Reset = multipcm.Reset;
        //            chip.SamplingRate = SamplingRate;
        //            chip.Clock = GetLE32(0x88) & 0x7fffffff;
        //            chip.Volume = 0;
        //            chip.Option = null;
        //            lstChip.Add(chip);
        //        }

        //        if (GetLE32(0x90) != 0 && 0x90 < vgmDataOffset - 3)
        //        {
        //            chip = new MDSound.MDSound.Chip();
        //            chip.type = MDSound.MDSound.enmInstrumentType.OKIM6258;
        //            chip.ID = 0;
        //            MDSound.okim6258 okim6258 = new MDSound.okim6258();
        //            chip.Instrument = okim6258;
        //            chip.Update = okim6258.Update;
        //            chip.Start = okim6258.Start;
        //            chip.Stop = okim6258.Stop;
        //            chip.Reset = okim6258.Reset;
        //            chip.SamplingRate = SamplingRate;
        //            chip.Clock = GetLE32(0x90) & 0xbfffffff;
        //            chip.Volume = 0;
        //            chip.Option =new object[] { (int)vgmBuf[0x94] };
        //            okim6258.okim6258_set_srchg_cb(0, ChangeChipSampleRate, chip);
        //            lstChip.Add(chip);
        //        }

        //        if (GetLE32(0x98) != 0 && 0x98 < vgmDataOffset - 3)
        //        {
        //            chip = new MDSound.MDSound.Chip();
        //            chip.type = MDSound.MDSound.enmInstrumentType.OKIM6295;
        //            chip.ID = 0;
        //            MDSound.okim6295 okim6295 = new MDSound.okim6295();
        //            chip.Instrument = okim6295;
        //            chip.Update = okim6295.Update;
        //            chip.Start = okim6295.Start;
        //            chip.Stop = okim6295.Stop;
        //            chip.Reset = okim6295.Reset;
        //            chip.SamplingRate = SamplingRate;
        //            chip.Clock = GetLE32(0x98) & 0xbfffffff;
        //            chip.Volume = 0;
        //            chip.Option = null;
        //            okim6295.okim6295_set_srchg_cb(0, ChangeChipSampleRate, chip);
        //            lstChip.Add(chip);
        //        }

        //        if (GetLE32(0x9c) != 0 && 0x9c < vgmDataOffset - 3)
        //        {
        //            chip = new MDSound.MDSound.Chip();
        //            chip.type = MDSound.MDSound.enmInstrumentType.K051649;
        //            chip.ID = 0;
        //            MDSound.K051649 k051649 = new MDSound.K051649();
        //            chip.Instrument = k051649;
        //            chip.Update = k051649.Update;
        //            chip.Start = k051649.Start;
        //            chip.Stop = k051649.Stop;
        //            chip.Reset = k051649.Reset;
        //            chip.SamplingRate = SamplingRate;
        //            chip.Clock = GetLE32(0x9c);
        //            chip.Volume = 0;
        //            chip.Option = null;
        //            lstChip.Add(chip);
        //        }

        //        if (GetLE32(0xa0) != 0 && 0xa0 < vgmDataOffset - 3)
        //        {
        //            MDSound.K054539 k054539 = new MDSound.K054539();
        //            int max = (GetLE32(0xa0) & 0x40000000) != 0 ? 2 : 1;
        //            for (int i = 0; i < max; i++)
        //            {
        //                chip = new MDSound.MDSound.Chip();
        //                chip.type = MDSound.MDSound.enmInstrumentType.K054539;
        //                chip.ID = (byte)i;
        //                chip.Instrument = k054539;
        //                chip.Update = k054539.Update;
        //                chip.Start = k054539.Start;
        //                chip.Stop = k054539.Stop;
        //                chip.Reset = k054539.Reset;
        //                chip.SamplingRate = SamplingRate;
        //                chip.Clock = GetLE32(0xa0) & 0x3fffffff;
        //                chip.Volume = 0;
        //                chip.Option = new object[] { vgmBuf[0x95] };

        //                lstChip.Add(chip);
        //            }
        //        }

        //        if (GetLE32(0xa4) != 0 && 0xa4 < vgmDataOffset - 3)
        //        {
        //            chip = new MDSound.MDSound.Chip();
        //            chip.type = MDSound.MDSound.enmInstrumentType.HuC6280;
        //            chip.ID = 0;
        //            MDSound.Ootake_PSG huc8910 = new MDSound.Ootake_PSG();
        //            chip.Instrument = huc8910;
        //            chip.Update = huc8910.Update;
        //            chip.Start = huc8910.Start;
        //            chip.Stop = huc8910.Stop;
        //            chip.Reset = huc8910.Reset;
        //            chip.SamplingRate = SamplingRate;
        //            chip.Clock = GetLE32(0xa4);
        //            chip.Volume = 0;
        //            chip.Option = null;
        //            lstChip.Add(chip);
        //        }

        //        if (GetLE32(0xa8) != 0 && 0xa8 < vgmDataOffset - 3)
        //        {
        //            chip = new MDSound.MDSound.Chip();
        //            chip.type = MDSound.MDSound.enmInstrumentType.C140;
        //            chip.ID = 0;
        //            MDSound.c140 c140 = new MDSound.c140();
        //            chip.Instrument = c140;
        //            chip.Update = c140.Update;
        //            chip.Start = c140.Start;
        //            chip.Stop = c140.Stop;
        //            chip.Reset = c140.Reset;
        //            chip.SamplingRate = SamplingRate;
        //            chip.Clock = GetLE32(0xa8);
        //            chip.Volume = 0;
        //            chip.Option = new object[1] { (MDSound.c140.C140_TYPE)vgmBuf[0x96] }; 
        //            lstChip.Add(chip);
        //        }

        //        if (GetLE32(0xac) != 0 && 0xac < vgmDataOffset - 3)
        //        {
        //            chip = new MDSound.MDSound.Chip();
        //            chip.type = MDSound.MDSound.enmInstrumentType.K053260;
        //            chip.ID = 0;
        //            MDSound.K053260 k053260 = new MDSound.K053260();
        //            chip.Instrument = k053260;
        //            chip.Update = k053260.Update;
        //            chip.Start = k053260.Start;
        //            chip.Stop = k053260.Stop;
        //            chip.Reset = k053260.Reset;
        //            chip.SamplingRate = SamplingRate;
        //            chip.Clock = GetLE32(0xac);
        //            chip.Volume = 0;
        //            chip.Option = null;
        //            lstChip.Add(chip);
        //        }

        //        if (GetLE32(0xb4) != 0 && 0xb4 < vgmDataOffset - 3)
        //        {
        //            chip = new MDSound.MDSound.Chip();
        //            chip.type = MDSound.MDSound.enmInstrumentType.QSound;
        //            chip.ID = 0;
        //            //MDSound.qsound qsound = new MDSound.qsound();
        //            MDSound.Qsound_ctr qsound = new MDSound.Qsound_ctr();
        //            chip.Instrument = qsound;
        //            chip.Update = qsound.Update;
        //            chip.Start = qsound.Start;
        //            chip.Stop = qsound.Stop;
        //            chip.Reset = qsound.Reset;
        //            chip.SamplingRate = SamplingRate;
        //            chip.Clock = GetLE32(0xb4);
        //            chip.Volume = 0;
        //            chip.Option = null;
        //            lstChip.Add(chip);
        //        }

        //        if (version >= 0x170 && 0xdc < vgmDataOffset - 3)
        //        {
        //            if (version >= 0x171)
        //            {
        //                if (GetLE32(0xc0) != 0 && 0xc0 < vgmDataOffset - 3)
        //                {
        //                    chip = new MDSound.MDSound.Chip();
        //                    chip.type = MDSound.MDSound.enmInstrumentType.WSwan;
        //                    chip.ID = 0;
        //                    MDSound.ws_audio wswan = new MDSound.ws_audio();
        //                    chip.Instrument = wswan;
        //                    chip.Update = wswan.Update;
        //                    chip.Start = wswan.Start;
        //                    chip.Stop = wswan.Stop;
        //                    chip.Reset = wswan.Reset;
        //                    chip.SamplingRate = SamplingRate;
        //                    chip.Clock = GetLE32(0xc0);
        //                    chip.Volume = 0;
        //                    chip.Option = null;

        //                    lstChip.Add(chip);
        //                }

        //                if (GetLE32(0xdc) != 0 && 0xdc < vgmDataOffset - 3)
        //                {
        //                    chip = new MDSound.MDSound.Chip();
        //                    chip.type = MDSound.MDSound.enmInstrumentType.C352;
        //                    chip.ID = 0;
        //                    MDSound.c352 c352 = new MDSound.c352();
        //                    chip.Instrument = c352;
        //                    chip.Update = c352.Update;
        //                    chip.Start = c352.Start;
        //                    chip.Stop = c352.Stop;
        //                    chip.Reset = c352.Reset;
        //                    chip.SamplingRate = SamplingRate;
        //                    chip.Clock = GetLE32(0xdc);
        //                    chip.Volume = 0;
        //                    chip.Option = new object[] { vgmBuf[0xd6] };

        //                    lstChip.Add(chip);
        //                }

        //                if (GetLE32(0xe0) != 0 && 0xe0 < vgmDataOffset - 3)
        //                {
        //                    chip = new MDSound.MDSound.Chip();
        //                    chip.type = MDSound.MDSound.enmInstrumentType.GA20;
        //                    chip.ID = 0;
        //                    MDSound.iremga20 ga20 = new MDSound.iremga20();
        //                    chip.Instrument = ga20;
        //                    chip.Update = ga20.Update;
        //                    chip.Start = ga20.Start;
        //                    chip.Stop = ga20.Stop;
        //                    chip.Reset = ga20.Reset;
        //                    chip.SamplingRate = SamplingRate;
        //                    chip.Clock = GetLE32(0xe0);
        //                    chip.Volume = 0;
        //                    chip.Option = null;

        //                    lstChip.Add(chip);
        //                }

        //            }
        //        }
        //    }


        //    chips = lstChip.ToArray();
        //    mds.Init(SamplingRate, samplingBuffer, chips);

        //    sdl.Paused = false;

        //}
        private static void Play(string fileName)
        {
            MDSound.MDSound.Chip[] chips = null;
            List<MDSound.MDSound.Chip> lstChip = new List<MDSound.MDSound.Chip>();

            MDSound.MDSound.Chip chip = new MDSound.MDSound.Chip();
            chip.type = MDSound.MDSound.enmInstrumentType.Gigatron;
            chip.ID = 0;
            MDSound.gigatron gig = new MDSound.gigatron();
            chip.Instrument = gig;
            chip.Update = gig.Update;
            chip.Start = gig.Start;
            chip.Stop = gig.Stop;
            chip.Reset = gig.Reset;
            chip.SamplingRate = SamplingRate;
            chip.Clock = (int)(521.0 * 59.98);
            chip.Volume = 0;
            chip.Option = null;

            lstChip.Add(chip);

            chips = lstChip.ToArray();
            mds.Init(SamplingRate, samplingBuffer, chips);

            sdl.Paused = false;

        }

        public static void ChangeChipSampleRate(MDSound.MDSound.Chip chip, int NewSmplRate)
        {
            MDSound.MDSound.Chip CAA = chip;

            if (CAA.SamplingRate == NewSmplRate)
                return;

            // quick and dirty hack to make sample rate changes work
            CAA.SamplingRate = (uint)NewSmplRate;
            if (CAA.SamplingRate < 44100)//SampleRate)
                CAA.Resampler = 0x01;
            else if (CAA.SamplingRate == 44100)//SampleRate)
                CAA.Resampler = 0x02;
            else if (CAA.SamplingRate > 44100)//SampleRate)
                CAA.Resampler = 0x03;
            CAA.SmpP = 1;
            CAA.SmpNext -= CAA.SmpLast;
            CAA.SmpLast = 0x00;

            return;
        }

        static int dummy = 0;

        private static void EmuCallback(IntPtr userData, IntPtr stream, int len)
        {
            long bufCnt = len / 4;
            long seqcnt = sm.GetSeqCounter();
            EmuSeqCounter = seqcnt - bufCnt;
            EmuSeqCounter = Math.Max(EmuSeqCounter, 0);

            for (int i = 0; i < bufCnt; i++)
            {
                //mds.Update(emuRenderBuf, 0, 2, OneFrameVGMStream);
                mds.Update(emuRenderBuf, 0, 2, OneFrameVGMaaa);

                frames[i * 2 + 0] = emuRenderBuf[0];
                frames[i * 2 + 1] = emuRenderBuf[1];
                //Console.Write("Adr[{0:x8}] : Wait[{1:d8}] : [{2:d8}]/[{3:d8}]\r\n", vgmAdr,0,0,0);
                //dummy++;
                //dummy %= 500;
                //frames[i * 2 + 0] = (short)dummy;// (dummy < 100 ? 0xfff : 0x000);
            }

            Marshal.Copy(frames, 0, stream, len / 2);

        }

        private static ushort key = 0x449;//ROMv1.lst:notesTable 2127行目
        private static ushort wavA = 0x00;
        private static ushort wavX = 0x01;
        private static int waitCnt = 0;
        private static int seqPos = 0;
        private static ushort[] seq = new ushort[] { 0x449, 0x4d0, 0x567, 0x5b9 };

        private static void OneFrameVGMaaa()
        {
            if (DriverSeqCounter > 0)
            {
                DriverSeqCounter--;
                return;
            }

            mds.WriteGigatron(0, 0x100 + 250, (byte)wavA);//WavA:effect
            mds.WriteGigatron(0, 0x100 + 251, (byte)wavX);//WavX:0:noise 1:tri. 2:pulse 3:saw
            mds.WriteGigatron(0, 0x100 + 252, (byte)(key & 0x7f));
            mds.WriteGigatron(0, 0x100 + 253, (byte)((key & 0xff80) >> 7));

            if (waitCnt == 0)
            {
                key = seq[seqPos++];
                seqPos &= 3;
            }
            waitCnt++;
            waitCnt &= 0x7fff;

            //OneFrameVGM();
        }

        private static void OneFrameVGM()
        {

            if (!vgmAnalyze)
            {
                return;
            }

            byte p = 0;
            byte si = 0;
            byte rAdr = 0;
            byte rDat = 0;

            if (vgmAdr == vgmBuf.Length || vgmAdr == vgmEof)
            {
                vgmAnalyze = false;
                sm.RequestStopAtDataMaker();
                return;
            }

            byte cmd = vgmBuf[vgmAdr];
            //Console.Write(" Adr[{0:x}]:cmd[{1:x}]\r\n", vgmAdr, cmd);
            switch (cmd)
            {
                case 0x4f: //GG PSG
                case 0x50: //PSG
                    mds.WriteSN76489(0, vgmBuf[vgmAdr + 1]);
                    //mds.WriteSN76496(0, vgmBuf[vgmAdr + 1]);
                    vgmAdr += 2;
                    break;
                case 0x51: //YM2413
                    rAdr = vgmBuf[vgmAdr + 1];
                    rDat = vgmBuf[vgmAdr + 2];
                    vgmAdr += 3;
                    //mds.WriteYM2413(0, rAdr, rDat);
                    break;
                case 0x52: //YM2612 Port0
                case 0x53: //YM2612 Port1
                    p = (byte)((cmd == 0x52) ? 0 : 1);
                    rAdr = vgmBuf[vgmAdr + 1];
                    rDat = vgmBuf[vgmAdr + 2];
                    vgmAdr += 3;
                    mds.WriteYM2612(0, p, rAdr, rDat);

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
                    //mds.WriteYM2203(0, rAdr, rDat);

                    break;
                //case 0x56: //YM2608 Port0
                //    rAdr = vgmBuf[vgmAdr + 1];
                //    rDat = vgmBuf[vgmAdr + 2];
                //    vgmAdr += 3;
                //    mds.WriteYM2608(0, 0, rAdr, rDat);

                //    break;
                //case 0x57: //YM2608 Port1
                //    rAdr = vgmBuf[vgmAdr + 1];
                //    rDat = vgmBuf[vgmAdr + 2];
                //    vgmAdr += 3;
                //    mds.WriteYM2608(0, 1, rAdr, rDat);

                //    break;
                case 0x56: //YM2609 Port0
                    rAdr = vgmBuf[vgmAdr + 1];
                    rDat = vgmBuf[vgmAdr + 2];
                    vgmAdr += 3;
                    if (rsc == null) DataEnq(DriverSeqCounter, 0x56, 0, 0 * 0x100 + rAdr, rDat, null);
                    else DataEnq(DriverSeqCounter, -1, 0, 0 * 0x100 + rAdr, rDat, null);
                    //mds.WriteYM2609(0, 0, rAdr, rDat);
                    break;
                case 0x57: //YM2609 Port1
                    rAdr = vgmBuf[vgmAdr + 1];
                    rDat = vgmBuf[vgmAdr + 2];
                    vgmAdr += 3;
                    if (rsc == null) DataEnq(DriverSeqCounter, 0x56, 0, 1 * 0x100 + rAdr, rDat, null);
                    else DataEnq(DriverSeqCounter, -1, 0, 1 * 0x100 + rAdr, rDat, null);
                    //mds.WriteYM2609(0, 1, rAdr, rDat);

                    break;
                case 0x58: //YM2610 Port0
                    rAdr = vgmBuf[vgmAdr + 1];
                    rDat = vgmBuf[vgmAdr + 2];
                    vgmAdr += 3;
                    //mds.WriteYM2610(0, 0, rAdr, rDat);

                    break;
                case 0x59: //YM2610 Port1
                    rAdr = vgmBuf[vgmAdr + 1];
                    rDat = vgmBuf[vgmAdr + 2];
                    vgmAdr += 3;
                    //mds.WriteYM2610(0, 1, rAdr, rDat);

                    break;
                case 0x5a: //YM3812
                    rAdr = vgmBuf[vgmAdr + 1];
                    rDat = vgmBuf[vgmAdr + 2];
                    vgmAdr += 3;
                    //mds.WriteYM3812(0, rAdr, rDat);

                    break;
                case 0x5b: //YM3526
                    rAdr = vgmBuf[vgmAdr + 1];
                    rDat = vgmBuf[vgmAdr + 2];
                    vgmAdr += 3;
                    //mds.WriteYM3526(0, rAdr, rDat);

                    break;
                case 0x5c: //Y8950
                    rAdr = vgmBuf[vgmAdr + 1];
                    rDat = vgmBuf[vgmAdr + 2];
                    vgmAdr += 3;
                    //mds.WriteY8950(0, rAdr, rDat);

                    break;
                case 0x5D: //YMZ280B
                    rAdr = vgmBuf[vgmAdr + 1];
                    rDat = vgmBuf[vgmAdr + 2];
                    vgmAdr += 3;
                    //mds.WriteYMZ280B(0, rAdr, rDat);

                    break;
                case 0x5e: //YMF262 Port0
                    rAdr = vgmBuf[vgmAdr + 1];
                    rDat = vgmBuf[vgmAdr + 2];
                    vgmAdr += 3;
                    
                    mds.WriteYMF262(0, 0, rAdr, rDat);
                    //mds.WriteYMF278B(0, 0, rAdr, rDat);
                    //Console.WriteLine("P0:adr{0:x2}:dat{1:x2}", rAdr, rDat);
                    break;
                case 0x5f: //YMF262 Port1
                    rAdr = vgmBuf[vgmAdr + 1];
                    rDat = vgmBuf[vgmAdr + 2];
                    vgmAdr += 3;
                    
                    mds.WriteYMF262(0, 1, rAdr, rDat);
                    //mds.WriteYMF278B(0, 1, rAdr, rDat);
                    //Console.WriteLine("P1:adr{0:x2}:dat{1:x2}", rAdr, rDat);

                    break;
                case 0x61: //Wait n samples
                    vgmAdr++;
                    DriverSeqCounter += (long)GetLE16(vgmAdr);
                    vgmAdr += 2;
                    break;
                case 0x62: //Wait 735 samples
                    vgmAdr++;
                    DriverSeqCounter += 735;
                    break;
                case 0x63: //Wait 882 samples
                    vgmAdr++;
                    DriverSeqCounter += 882;
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
                    uint bLen = GetLE32(vgmAdr + 3);
                    //byte chipID = 0;
                    if ((bLen & 0x80000000) != 0)
                    {
                        bLen &= 0x7fffffff;
                        //chipID = 1;
                    }

                    switch (bType & 0xc0)
                    {
                        case 0x00:
                        case 0x40:
                            //AddPCMData(bType, bLen, bAdr);
                            vgmAdr += (uint)bLen + 7;
                            break;
                        case 0x80:
                            uint romSize = GetLE32(vgmAdr + 7);
                            uint startAddress = GetLE32(vgmAdr + 0x0B);
                            switch (bType)
                            {
                                case 0x80:
                                    //SEGA PCM
                                    //mds.WriteSEGAPCMPCMData(chipID, romSize, startAddress, bLen - 8, vgmBuf, vgmAdr + 15);
                                    break;
                                case 0x81:

                                    // YM2608/YM2609
                                    List<Pack> data = new List<Pack>
                                    {
                                        new Pack(0,0,0x100+ 0x00, 0x20,null),
                                        new Pack(0,0,0x100+ 0x00, 0x21,null),
                                        new Pack(0,0,0x100+ 0x00, 0x00,null),

                                        new Pack(0,0,0x100+ 0x10, 0x00,null),
                                        new Pack(0,0,0x100+ 0x10, 0x80,null),

                                        new Pack(0,0,0x100+ 0x00, 0x61,null),
                                        new Pack(0,0,0x100+ 0x00, 0x68,null),
                                        new Pack(0,0,0x100+ 0x01, 0x00,null),

                                        new Pack(0,0,0x100+ 0x02, (byte)(startAddress >> 2),null),
                                        new Pack(0,0,0x100+ 0x03, (byte)(startAddress >> 10),null),
                                        new Pack(0,0,0x100+ 0x04, 0xff,null),
                                        new Pack(0,0,0x100+ 0x05, 0xff,null),
                                        new Pack(0,0,0x100+ 0x0c, 0xff,null),
                                        new Pack(0,0,0x100+ 0x0d, 0xff,null)
                                    };

                                    // データ転送
                                    for (int cnt = 0; cnt < bLen - 8; cnt++)
                                    {
                                        data.Add(new Pack(0, 0, 0x100 + 0x08, vgmBuf[vgmAdr + 15 + cnt], null));
                                    }
                                    data.Add(new Pack(0, 0, 0x100 + 0x00, 0x00, null));
                                    data.Add(new Pack(0, 0, 0x100 + 0x10, 0x80, null));

                                    if (rsc == null) DataEnq(DriverSeqCounter, 0x56, 0, -1, -1, data.ToArray());
                                    else
                                    {
                                        DataEnq(DriverSeqCounter, -1, 0, -1, -1, data.ToArray());
                                        DriverSeqCounter += bLen;
                                    }

                                    break;

                                case 0x82:
                                    if (bufYM2610AdpcmA == null || bufYM2610AdpcmA.Length != romSize) bufYM2610AdpcmA = new byte[romSize];
                                    for (int cnt = 0; cnt < bLen - 8; cnt++)
                                    {
                                        bufYM2610AdpcmA[startAddress + cnt] = vgmBuf[vgmAdr + 15 + cnt];
                                    }
                                    //mds.WriteYM2610_SetAdpcmA(0, bufYM2610AdpcmA);
                                    break;
                                case 0x83:
                                    if (bufYM2610AdpcmB == null || bufYM2610AdpcmB.Length != romSize) bufYM2610AdpcmB = new byte[romSize];
                                    for (int cnt = 0; cnt < bLen - 8; cnt++)
                                    {
                                        bufYM2610AdpcmB[startAddress + cnt] = vgmBuf[vgmAdr + 15 + cnt];
                                    }
                                    //mds.WriteYM2610_SetAdpcmB(0, bufYM2610AdpcmB);
                                    break;

                                case 0x84:
                                    //mds.WriteYMF278BPCMData(chipID, romSize, startAddress, bLen - 8, vgmBuf, vgmAdr + 15);
                                    break;

                                case 0x85:
                                    //mds.WriteYMF271PCMData(chipID, romSize, startAddress, bLen - 8, vgmBuf, vgmAdr + 15);
                                    break;

                                case 0x86:
                                    //mds.WriteYMZ280BPCMData(chipID, romSize, startAddress, bLen - 8, vgmBuf, vgmAdr + 15);
                                    break;

                                case 0x88:
                                    //mds.WriteY8950PCMData(chipID, romSize, startAddress, bLen - 8, vgmBuf, vgmAdr + 15);
                                    break;

                                case 0x89:
                                    //mds.WriteMultiPCMPCMData(chipID, romSize, startAddress, bLen - 8, vgmBuf, vgmAdr + 15);
                                    break;

                                case 0x8b:
                                    //mds.WriteOKIM6295PCMData(chipID, romSize, startAddress, bLen - 8, vgmBuf, vgmAdr + 15);
                                    break;

                                case 0x8c:
                                    //mds.WriteK054539PCMData(chipID, romSize, startAddress, bLen - 8, vgmBuf, vgmAdr + 15);
                                    break;

                                case 0x8d:
                                    //mds.WriteC140PCMData(chipID, romSize, startAddress, bLen - 8, vgmBuf, vgmAdr + 15);
                                    break;

                                case 0x8e:
                                    //mds.WriteK053260PCMData(chipID, romSize, startAddress, bLen - 8, vgmBuf, vgmAdr + 15);
                                    break;

                                case 0x8f:
                                    mds.WriteQSoundPCMData(0, romSize, startAddress, bLen - 8, vgmBuf, vgmAdr + 15);
                                    break;

                                case 0x92:
                                    //mds.WriteC352PCMData(0, romSize, startAddress, bLen - 8, vgmBuf, vgmAdr + 15);
                                    break;

                                case 0x93:
                                    //mds.WriteGA20PCMData(0, romSize, startAddress, bLen - 8, vgmBuf, vgmAdr + 15);
                                    break;
                            }
                            vgmAdr += (uint)bLen + 7;
                            break;
                        default:
                            vgmAdr += bLen + 7;
                            break;
                    }
                    break;
                case 0x68: //PCM RAM writes
                    byte chipType = vgmBuf[vgmAdr + 2];
                    uint chipReadOffset = GetLE24(vgmAdr + 3);
                    uint chipWriteOffset = GetLE24(vgmAdr + 6);
                    uint chipDataSize = GetLE24(vgmAdr + 9);
                    if (chipDataSize == 0) chipDataSize = 0x1000000;
                    uint? pcmAdr = GetPCMAddressFromPCMBank(chipType, chipReadOffset);
                    if (pcmAdr != null && chipType == 0x01)
                    {
                        //mds.WriteRF5C68PCMData(0, chipWriteOffset, chipDataSize, PCMBank[chipType].Data, (uint)pcmAdr);
                    }

                    vgmAdr += 12;
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
                    DriverSeqCounter += (long)(cmd - 0x6f);
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
                    mds.WriteYM2612(0, 0, 0x2a, vgmBuf[vgmPcmPtr++]);
                    DriverSeqCounter += (long)(cmd - 0x80);
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
                    vgmStreams[si].frequency = GetLE32(vgmAdr);
                    vgmAdr += 4;
                    break;
                case 0x93:
                    vgmAdr++;
                    si = vgmBuf[vgmAdr++];
                    vgmStreams[si].dataStartOffset = GetLE32(vgmAdr);
                    vgmAdr += 4;
                    vgmStreams[si].lengthMode = vgmBuf[vgmAdr++];
                    vgmStreams[si].dataLength = GetLE32(vgmAdr);
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
                    vgmStreams[si].blockId = GetLE16(vgmAdr);
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
                case 0xb3: //GB DMG
                    rAdr = vgmBuf[vgmAdr + 1];
                    rDat = vgmBuf[vgmAdr + 2];
                    vgmAdr += 3;
                    //mds.WriteDMG(0, rAdr, rDat);
                    break;
                case 0xb4: //NES
                    rAdr = vgmBuf[vgmAdr + 1];
                    rDat = vgmBuf[vgmAdr + 2];
                    vgmAdr += 3;
                    //mds.WriteNES(0, rAdr, rDat);
                    break;
                case 0xb5: //MultiPCM
                    rAdr = (byte)(vgmBuf[vgmAdr + 1] & 0x7f);
                    rDat = vgmBuf[vgmAdr + 2];
                    vgmAdr += 3;
                    //mds.WriteMultiPCM(0, rAdr, rDat);
                    break;
                case 0xb7:
                    rAdr = vgmBuf[vgmAdr + 1];
                    rDat = vgmBuf[vgmAdr + 2];
                    vgmAdr += 3;
                    mds.WriteOKIM6258(0, rAdr, rDat);
                    break;
                case 0xb8:
                    rAdr = vgmBuf[vgmAdr + 1];
                    rDat = vgmBuf[vgmAdr + 2];
                    vgmAdr += 3;
                    //mds.WriteOKIM6295(0, rAdr, rDat);
                    break;
                case 0xb9: //HuC6280
                    rAdr = vgmBuf[vgmAdr + 1];
                    rDat = vgmBuf[vgmAdr + 2];
                    vgmAdr += 3;
                    //mds.WriteHuC6280(0, rAdr, rDat);
                    break;
                case 0xba: //K053260
                    rAdr = vgmBuf[vgmAdr + 1];
                    rDat = vgmBuf[vgmAdr + 2];
                    vgmAdr += 3;
                    //mds.WriteK053260(0, rAdr, rDat);

                    break;
                case 0xbc: //WSwan
                    rAdr = vgmBuf[vgmAdr + 1];
                    rDat = vgmBuf[vgmAdr + 2];
                    vgmAdr += 3;
                    mds.WriteWSwan(0, rAdr, rDat);

                    break;
                case 0xc6: //WSwan write memory
                    int wsOfs = vgmBuf[vgmAdr + 1] * 0x100 + vgmBuf[vgmAdr + 2];
                    rDat = vgmBuf[vgmAdr + 3];
                    vgmAdr += 4;
                    mds.WriteWSwanMem(0, wsOfs, rDat);

                    break;
                case 0xbf: //GA20
                    rAdr = vgmBuf[vgmAdr + 1];
                    rDat = vgmBuf[vgmAdr + 2];
                    vgmAdr += 3;
                    //mds.WriteGA20(0, rAdr, rDat);

                    break;
                case 0xc0://segaPCM
                          //mds.WriteSEGAPCM(0, (int)((vgmBuf[vgmAdr + 0x01] & 0xFF) | ((vgmBuf[vgmAdr + 0x02] & 0xFF) << 8)), vgmBuf[vgmAdr + 0x03]);
                    vgmAdr += 4;
                    break;
                case 0xc3://MultiPCM
                    byte multiPCM_ch = (byte)(vgmBuf[vgmAdr + 1] & 0x7f);
                    int multiPCM_adr = vgmBuf[vgmAdr + 2] + vgmBuf[vgmAdr + 3] * 0x100;
                    vgmAdr += 4;
                    //mds.WriteMultiPCMSetBank(0, multiPCM_ch, multiPCM_adr);
                    break;
                case 0xc4://QSound
                          mds.WriteQSound(0, 0x00, vgmBuf[vgmAdr + 1]);
                          mds.WriteQSound(0, 0x01, vgmBuf[vgmAdr + 2]);
                          mds.WriteQSound(0, 0x02, vgmBuf[vgmAdr + 3]);
                    //rDat = vgmBuf[vgmAdr + 3];
                    //if (rsc == null) DataEnq(DriverSeqCounter, 0xc4, 0, vgmBuf[vgmAdr + 1] * 0x100 + vgmBuf[vgmAdr + 2], rDat, null);
                    vgmAdr += 4;
                    break;
                case 0xd0: //YMF278B
                    byte ymf278b_port = (byte)(vgmBuf[vgmAdr + 1] & 0x7f);
                    byte ymf278b_offset = vgmBuf[vgmAdr + 2];
                    rDat = vgmBuf[vgmAdr + 3];
                    byte ymf278b_chipid = (byte)((vgmBuf[vgmAdr + 1] & 0x80) != 0 ? 1 : 0);
                    vgmAdr += 4;
                    //mds.WriteYMF278B(ymf278b_chipid, ymf278b_port, ymf278b_offset, rDat);
                    break;
                case 0xd1: //YMF271
                    byte ymf271_port = (byte)(vgmBuf[vgmAdr + 1] & 0x7f);
                    byte ymf271_offset = vgmBuf[vgmAdr + 2];
                    rDat = vgmBuf[vgmAdr + 3];
                    byte ymf271_chipid = (byte)((vgmBuf[vgmAdr + 1] & 0x80) != 0 ? 1 : 0);
                    vgmAdr += 4;
                    //mds.WriteYMF271(ymf271_chipid, ymf271_port, ymf271_offset, rDat);
                    break;
                case 0xd2: //SCC1(K051649?)
                    int scc1_port = vgmBuf[vgmAdr + 1] & 0x7f;
                    byte scc1_offset = vgmBuf[vgmAdr + 2];
                    rDat = vgmBuf[vgmAdr + 3];
                    byte scc1_chipid = (byte)((vgmBuf[vgmAdr + 1] & 0x80) != 0 ? 1 : 0);
                    vgmAdr += 4;
                    //mds.WriteK051649(scc1_chipid, (scc1_port << 1) | 0x00, scc1_offset);
                    //mds.WriteK051649(scc1_chipid, (scc1_port << 1) | 0x01, rDat);
                    break;
                case 0xd3: //K054539
                    int k054539_adr = (vgmBuf[vgmAdr + 1] & 0x7f) * 0x100 + vgmBuf[vgmAdr + 2];
                    rDat = vgmBuf[vgmAdr + 3];
                    byte chipid = (byte)((vgmBuf[vgmAdr + 1] & 0x80) != 0 ? 1 : 0);
                    vgmAdr += 4;
                    //mds.WriteK054539(chipid, k054539_adr, rDat);
                    break;
                case 0xd4: //C140
                    int c140_adr = (vgmBuf[vgmAdr + 1] & 0x7f) * 0x100 + vgmBuf[vgmAdr + 2];
                    rDat = vgmBuf[vgmAdr + 3];
                    byte c140_chipid = (byte)((vgmBuf[vgmAdr + 1] & 0x80) != 0 ? 1 : 0);
                    vgmAdr += 4;
                    //mds.WriteC140(c140_chipid, (uint)c140_adr, rDat);
                    break;
                case 0xe0: //seek to offset in PCM data bank
                    vgmPcmPtr = GetLE32(vgmAdr + 1) + vgmPcmBaseAdr;
                    vgmAdr += 5;
                    break;
                case 0xe1: //C352
                    uint adr = (uint)((vgmBuf[vgmAdr + 1] & 0xff) * 0x100 + (vgmBuf[vgmAdr + 2] & 0xff));
                    uint dat = (uint)((vgmBuf[vgmAdr + 3] & 0xff) * 0x100 + (vgmBuf[vgmAdr + 4] & 0xff));
                    vgmAdr += 5;
                    //mds.WriteC352(0, adr, dat);

                    break;
                default:
                    //わからんコマンド
                    Console.WriteLine("{0:X}", vgmBuf[vgmAdr++]);
                    return;
            }

        }

        private static uint? GetPCMAddressFromPCMBank(byte chipType, uint chipReadOffset)
        {
            if (chipType >= PCM_BANK_COUNT)
                return null;

            if (chipReadOffset >= PCMBank[chipType].DataSize)
                return null;

            return chipReadOffset;
        }

        private static void OneFrameVGMStream()
        {
            while ((long)emuRecvBuffer.LookUpCounter() <= EmuSeqCounter)//&& recvBuffer.LookUpCounter() != 0)
            {
                bool ret = emuRecvBuffer.Deq(ref PackCounter, ref Pack.Dev, ref Pack.Typ, ref Pack.Adr, ref Pack.Val, ref Pack.Ex);
                if (!ret) break;
                SendEmuData(PackCounter, Pack.Dev, Pack.Typ, Pack.Adr, Pack.Val, Pack.Ex);
            }
            EmuSeqCounter++;

            for (int i = 0; i < 0x100; i++)
            {

                if (!vgmStreams[i].sw) continue;
                if (vgmStreams[i].chipId != 0x02) continue;//とりあえずYM2612のみ

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

        private static void SendEmuData(long Counter, int Dev, int Typ, int Adr, int Val, object[] Ex)
        {
            switch (Dev)
            {
                case 0x56:
                    if (Adr >= 0)
                    {
                        mds.WriteYM2609(0, (byte)(Adr >> 8), (byte)Adr, (byte)Val);
                    }
                    else
                    {
                        sm.SetInterrupt();
                        try
                        {
                            Pack[] data = (Pack[])Ex;
                            foreach (Pack dat in data)
                            {
                                mds.WriteYM2609(0, (byte)(dat.Adr >> 8), (byte)dat.Adr, (byte)dat.Val);
                            }
                        }
                        finally
                        {
                            sm.ResetInterrupt();
                        }
                    }
                    break;
            }
        }



        private static UInt32 GetLE16(UInt32 adr)
        {
            UInt32 dat;
            dat = (UInt32)vgmBuf[adr] + (UInt32)vgmBuf[adr + 1] * 0x100;

            return dat;
        }

        private static UInt32 GetLE24(UInt32 adr)
        {
            UInt32 dat;
            dat = (UInt32)vgmBuf[adr] + (UInt32)vgmBuf[adr + 1] * 0x100 + (UInt32)vgmBuf[adr + 2] * 0x10000;

            return dat;
        }

        private static UInt32 GetLE32(UInt32 adr)
        {
            UInt32 dat;
            dat = (UInt32)vgmBuf[adr] + (UInt32)vgmBuf[adr + 1] * 0x100 + (UInt32)vgmBuf[adr + 2] * 0x10000 + (UInt32)vgmBuf[adr + 3] * 0x1000000;

            return dat;
        }

        private void FrmMain_Shown(object sender, EventArgs e)
        {
            string[] cmds = System.Environment.GetCommandLineArgs();

            if (cmds.Length > 1)
            {
                tbFile.Text = cmds[1];
                BtnPlay_Click(null, null);
            }
        }
    }
}
