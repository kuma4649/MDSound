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
        private static uint samplingBuffer = 1024;
        private static short[] frames = new short[samplingBuffer * 4];
        private static MDSound.MDSound mds = null;
        
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

        private static byte[] bufYM2610AdpcmA = null;
        private static byte[] bufYM2610AdpcmB = null;

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

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (mds == null) return;
            int l = mds.getTotalVolumeL();
            int r = mds.getTotalVolumeR();

            label2.Location = new System.Drawing.Point(Math.Min((l / 600) * 3 - 174, 0), label2.Location.Y);
            label3.Location = new System.Drawing.Point(Math.Min((r / 600) * 3 - 174, 0), label3.Location.Y);
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            if (mds == null) return;
            mds.SetVolumeYM2612(((TrackBar)sender).Value);
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
            
            for(int i = 0; i < PCMBank.Length; i++)
            {
                PCMBank[i] = new VGM_PCM_BANK();
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
                chip.Clock = getLE32(0x0c);
                chip.Volume = 0;
                chip.Option = null;
                lstChip.Add(chip);
            }

            if (getLE32(0x10) != 0)
            {
                chip = new MDSound.MDSound.Chip();
                chip.type = MDSound.MDSound.enmInstrumentType.YM2413;
                chip.ID = 0;
                MDSound.ym2413 ym2413 = new MDSound.ym2413();
                chip.Instrument = ym2413;
                chip.Update = ym2413.Update;
                chip.Start = ym2413.Start;
                chip.Stop = ym2413.Stop;
                chip.Reset = ym2413.Reset;
                chip.SamplingRate = SamplingRate;
                chip.Clock = getLE32(0x10); 
                chip.Volume = 0;
                chip.Option = null;
                lstChip.Add(chip);
            }

            if (getLE32(0x2c) != 0)
            {
                chip = new MDSound.MDSound.Chip();
                chip.type = MDSound.MDSound.enmInstrumentType.YM2612;
                //chip.type = MDSound.MDSound.enmInstrumentType.YM3438;
                chip.ID = 0;
                //MDSound.ym2612 ym2612 = new MDSound.ym2612();
                MDSound.ym3438 ym2612 = new MDSound.ym3438();
                chip.Instrument = ym2612;
                chip.Update = ym2612.Update;
                chip.Start = ym2612.Start;
                chip.Stop = ym2612.Stop;
                chip.Reset = ym2612.Reset;
                chip.SamplingRate = SamplingRate;
                chip.Clock = getLE32(0x2c); 
                chip.Volume = 0;
                chip.Option = null;
                lstChip.Add(chip);
            }

            if (getLE32(0x30) != 0)
            {
                chip = new MDSound.MDSound.Chip();
                chip.type = MDSound.MDSound.enmInstrumentType.YM2151;
                chip.ID = 0;
                //MDSound.ym2151 ym2151 = new MDSound.ym2151();
                //MDSound.ym2151_mame ym2151 = new MDSound.ym2151_mame();
                MDSound.ym2151_x68sound ym2151 = new MDSound.ym2151_x68sound();
                chip.Instrument = ym2151;
                chip.Update = ym2151.Update;
                chip.Start = ym2151.Start;
                chip.Stop = ym2151.Stop;
                chip.Reset = ym2151.Reset;
                chip.SamplingRate = SamplingRate;
                chip.Clock = getLE32(0x30); 
                chip.Volume = 0;
                chip.Option = null;
                lstChip.Add(chip);
            }

            if (getLE32(0x38) != 0 && 0x38 < vgmDataOffset - 3)
            {
                chip = new MDSound.MDSound.Chip();
                chip.type = MDSound.MDSound.enmInstrumentType.SEGAPCM;
                chip.ID = 0;
                MDSound.segapcm segapcm = new MDSound.segapcm();
                chip.Instrument = segapcm;
                chip.Update = segapcm.Update;
                chip.Start = segapcm.Start;
                chip.Stop = segapcm.Stop;
                chip.Reset = segapcm.Reset;
                chip.SamplingRate = SamplingRate;
                chip.Clock = getLE32(0x38);
                chip.Option = new object[1] { (int)getLE32(0x3c) };
                chip.Volume = 0;
                
                lstChip.Add(chip);
            }

            if (getLE32(0x44) != 0 && 0x44 < vgmDataOffset - 3)
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
                chip.Clock = getLE32(0x44);
                chip.Volume = 0;
                chip.Option = null;
                lstChip.Add(chip);
            }

            //if (getLE32(0x48) != 0)
            //{
            //    chip = new MDSound.MDSound.Chip();
            //    chip.type = MDSound.MDSound.enmInstrumentType.YM2608;
            //    chip.ID = 0;
            //    MDSound.ym2608 ym2608 = new MDSound.ym2608();
            //    chip.Instrument = ym2608;
            //    chip.Update = ym2608.Update;
            //    chip.Start = ym2608.Start;
            //    chip.Stop = ym2608.Stop;
            //    chip.Reset = ym2608.Reset;
            //    chip.SamplingRate = SamplingRate;
            //    chip.Clock = getLE32(0x48);
            //    chip.Volume = 0;
            //    chip.Option = null;
            //    lstChip.Add(chip);
            //}
            if (getLE32(0x48) != 0 && 0x48 < vgmDataOffset - 3)
            {
                chip = new MDSound.MDSound.Chip();
                chip.type = MDSound.MDSound.enmInstrumentType.YM2609;
                chip.ID = 0;
                MDSound.ym2609 ym2609 = new MDSound.ym2609();
                chip.Instrument = ym2609;
                chip.Update = ym2609.Update;
                chip.Start = ym2609.Start;
                chip.Stop = ym2609.Stop;
                chip.Reset = ym2609.Reset;
                chip.SamplingRate = SamplingRate;
                chip.Clock = getLE32(0x48);
                chip.Volume = 0;
                chip.Option = null;
                lstChip.Add(chip);
            }

            if (getLE32(0x4c) != 0 && 0x4c < vgmDataOffset - 3)
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

            if (getLE32(0x5c) != 0 && 0x5c < vgmDataOffset - 3)
            {
                chip = new MDSound.MDSound.Chip();
                chip.type = MDSound.MDSound.enmInstrumentType.YMF262;
                chip.ID = 0;
                MDSound.ymf262 ymf262 = new MDSound.ymf262();
                chip.Instrument = ymf262;
                chip.Update = ymf262.Update;
                chip.Start = ymf262.Start;
                chip.Stop = ymf262.Stop;
                chip.Reset = ymf262.Reset;
                chip.SamplingRate = SamplingRate;
                chip.Clock = getLE32(0x5c) & 0x7fffffff;
                chip.Volume = 0;
                chip.Option = null;
                lstChip.Add(chip);

                //chip = new MDSound.MDSound.Chip();
                //chip.type = MDSound.MDSound.enmInstrumentType.YMF278B;
                //chip.ID = 0;
                //MDSound.ymf278b ymf278b = new MDSound.ymf278b();
                //chip.Instrument = ymf278b;
                //chip.Update = ymf278b.Update;
                //chip.Start = ymf278b.Start;
                //chip.Stop = ymf278b.Stop;
                //chip.Reset = ymf278b.Reset;
                //chip.SamplingRate = SamplingRate;
                //chip.Clock = getLE32(0x5c) & 0x7fffffff;
                //chip.Volume = 0;
                //chip.Option = null;
                //lstChip.Add(chip);
            }

            if (getLE32(0x58) != 0 && 0x58 < vgmDataOffset - 3)
            {
                chip = new MDSound.MDSound.Chip();
                chip.type = MDSound.MDSound.enmInstrumentType.Y8950;
                chip.ID = 0;
                MDSound.y8950 y8950 = new MDSound.y8950();
                chip.Instrument = y8950;
                chip.Update = y8950.Update;
                chip.Start = y8950.Start;
                chip.Stop = y8950.Stop;
                chip.Reset = y8950.Reset;
                chip.SamplingRate = SamplingRate;
                chip.Clock = getLE32(0x58) & 0x7fffffff;
                chip.Volume = 0;
                chip.Option = null;
                lstChip.Add(chip);
            }

            if (getLE32(0x60) != 0 && 0x60 < vgmDataOffset - 3)
            {
                chip = new MDSound.MDSound.Chip();
                chip.type = MDSound.MDSound.enmInstrumentType.YMF278B;
                chip.ID = 0;
                MDSound.ymf278b ymf278b = new MDSound.ymf278b();
                chip.Instrument = ymf278b;
                chip.Update = ymf278b.Update;
                chip.Start = ymf278b.Start;
                chip.Stop = ymf278b.Stop;
                chip.Reset = ymf278b.Reset;
                chip.SamplingRate = SamplingRate;
                chip.Clock = getLE32(0x60) & 0x7fffffff;
                chip.Volume = 0;
                chip.Option = null;
                lstChip.Add(chip);
            }

            if (getLE32(0x64) != 0 && 0x64 < vgmDataOffset - 3)
            {
                chip = new MDSound.MDSound.Chip();
                chip.type = MDSound.MDSound.enmInstrumentType.YMF271;
                chip.ID = 0;
                MDSound.ymf271 ymf271 = new MDSound.ymf271();
                chip.Instrument = ymf271;
                chip.Update = ymf271.Update;
                chip.Start = ymf271.Start;
                chip.Stop = ymf271.Stop;
                chip.Reset = ymf271.Reset;
                chip.SamplingRate = SamplingRate;
                chip.Clock = getLE32(0x64) & 0x7fffffff;
                chip.Volume = 0;
                chip.Option = null;
                lstChip.Add(chip);
            }

            if (getLE32(0x68) != 0 && 0x68 < vgmDataOffset - 3)
            {
                chip = new MDSound.MDSound.Chip();
                chip.type = MDSound.MDSound.enmInstrumentType.YMZ280B;
                chip.ID = 0;
                MDSound.ymz280b ymz280b = new MDSound.ymz280b();
                chip.Instrument = ymz280b;
                chip.Update = ymz280b.Update;
                chip.Start = ymz280b.Start;
                chip.Stop = ymz280b.Stop;
                chip.Reset = ymz280b.Reset;
                chip.SamplingRate = SamplingRate;
                chip.Clock = getLE32(0x68) & 0x7fffffff;
                chip.Volume = 0;
                chip.Option = null;
                lstChip.Add(chip);
            }

            if (getLE32(0x74) != 0 && 0x74 < vgmDataOffset - 3)
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

            if (version >= 0x0161 && 0x80 < vgmDataOffset - 3)
            {

                if (getLE32(0x80) != 0 && 0x80 < vgmDataOffset - 3)
                {
                    chip = new MDSound.MDSound.Chip();
                    chip.type = MDSound.MDSound.enmInstrumentType.DMG;
                    chip.ID = 0;
                    MDSound.gb gb = new MDSound.gb();
                    chip.Instrument = gb;
                    chip.Update = gb.Update;
                    chip.Start = gb.Start;
                    chip.Stop = gb.Stop;
                    chip.Reset = gb.Reset;
                    chip.SamplingRate = SamplingRate;
                    chip.Clock = getLE32(0x80);// & 0x7fffffff;
                    chip.Volume = 0;
                    chip.Option = null;
                    lstChip.Add(chip);
                }

                if (getLE32(0x84) != 0 && 0x84 < vgmDataOffset - 3)
                {
                    chip = new MDSound.MDSound.Chip();
                    chip.type = MDSound.MDSound.enmInstrumentType.Nes;
                    chip.ID = 0;
                    MDSound.nes_intf nes_intf = new MDSound.nes_intf();
                    chip.Instrument = nes_intf;
                    chip.Update = nes_intf.Update;
                    chip.Start = nes_intf.Start;
                    chip.Stop = nes_intf.Stop;
                    chip.Reset = nes_intf.Reset;
                    chip.SamplingRate = SamplingRate;
                    chip.Clock = getLE32(0x84);// & 0x7fffffff;
                    chip.Volume = 0;
                    chip.Option = null;
                    lstChip.Add(chip);
                }

                if (getLE32(0x88) != 0 && 0x88 < vgmDataOffset - 3)
                {
                    chip = new MDSound.MDSound.Chip();
                    chip.type = MDSound.MDSound.enmInstrumentType.MultiPCM;
                    chip.ID = 0;
                    MDSound.multipcm multipcm = new MDSound.multipcm();
                    chip.Instrument = multipcm;
                    chip.Update = multipcm.Update;
                    chip.Start = multipcm.Start;
                    chip.Stop = multipcm.Stop;
                    chip.Reset = multipcm.Reset;
                    chip.SamplingRate = SamplingRate;
                    chip.Clock = getLE32(0x88) & 0x7fffffff;
                    chip.Volume = 0;
                    chip.Option = null;
                    lstChip.Add(chip);
                }

                if (getLE32(0x98) != 0 && 0x98 < vgmDataOffset - 3)
                {
                    chip = new MDSound.MDSound.Chip();
                    chip.type = MDSound.MDSound.enmInstrumentType.OKIM6295;
                    chip.ID = 0;
                    MDSound.okim6295 okim6295 = new MDSound.okim6295();
                    chip.Instrument = okim6295;
                    chip.Update = okim6295.Update;
                    chip.Start = okim6295.Start;
                    chip.Stop = okim6295.Stop;
                    chip.Reset = okim6295.Reset;
                    chip.SamplingRate = SamplingRate;
                    chip.Clock = getLE32(0x98) & 0xbfffffff;
                    chip.Volume = 0;
                    chip.Option = null;
                    okim6295.okim6295_set_srchg_cb(0, ChangeChipSampleRate, chip);
                    lstChip.Add(chip);
                }

                if (getLE32(0x9c) != 0 && 0x9c < vgmDataOffset - 3)
                {
                    chip = new MDSound.MDSound.Chip();
                    chip.type = MDSound.MDSound.enmInstrumentType.K051649;
                    chip.ID = 0;
                    MDSound.K051649 k051649 = new MDSound.K051649();
                    chip.Instrument = k051649;
                    chip.Update = k051649.Update;
                    chip.Start = k051649.Start;
                    chip.Stop = k051649.Stop;
                    chip.Reset = k051649.Reset;
                    chip.SamplingRate = SamplingRate;
                    chip.Clock = getLE32(0x9c);
                    chip.Volume = 0;
                    chip.Option = null;
                    lstChip.Add(chip);
                }

                if (getLE32(0xa0) != 0 && 0xa0 < vgmDataOffset - 3)
                {
                    MDSound.K054539 k054539 = new MDSound.K054539();
                    int max = (getLE32(0xa0) & 0x40000000) != 0 ? 2 : 1;
                    for (int i = 0; i < max; i++)
                    {
                        chip = new MDSound.MDSound.Chip();
                        chip.type = MDSound.MDSound.enmInstrumentType.K054539;
                        chip.ID = (byte)i;
                        chip.Instrument = k054539;
                        chip.Update = k054539.Update;
                        chip.Start = k054539.Start;
                        chip.Stop = k054539.Stop;
                        chip.Reset = k054539.Reset;
                        chip.SamplingRate = SamplingRate;
                        chip.Clock = getLE32(0xa0) & 0x3fffffff;
                        chip.Volume = 0;
                        chip.Option = new object[] { vgmBuf[0x95] };

                        lstChip.Add(chip);
                    }
                }

                if (getLE32(0xa4) != 0 && 0xa4 < vgmDataOffset - 3)
                {
                    chip = new MDSound.MDSound.Chip();
                    chip.type = MDSound.MDSound.enmInstrumentType.HuC6280;
                    chip.ID = 0;
                    MDSound.Ootake_PSG huc8910 = new MDSound.Ootake_PSG();
                    chip.Instrument = huc8910;
                    chip.Update = huc8910.Update;
                    chip.Start = huc8910.Start;
                    chip.Stop = huc8910.Stop;
                    chip.Reset = huc8910.Reset;
                    chip.SamplingRate = SamplingRate;
                    chip.Clock = getLE32(0xa4);
                    chip.Volume = 0;
                    chip.Option = null;
                    lstChip.Add(chip);
                }

                if (getLE32(0xa8) != 0 && 0xa8 < vgmDataOffset - 3)
                {
                    chip = new MDSound.MDSound.Chip();
                    chip.type = MDSound.MDSound.enmInstrumentType.C140;
                    chip.ID = 0;
                    MDSound.c140 c140 = new MDSound.c140();
                    chip.Instrument = c140;
                    chip.Update = c140.Update;
                    chip.Start = c140.Start;
                    chip.Stop = c140.Stop;
                    chip.Reset = c140.Reset;
                    chip.SamplingRate = SamplingRate;
                    chip.Clock = getLE32(0xa8);
                    chip.Volume = 0;
                    chip.Option = new object[1] { (MDSound.c140.C140_TYPE)vgmBuf[0x96] }; 
                    lstChip.Add(chip);
                }

                if (getLE32(0xac) != 0 && 0xac < vgmDataOffset - 3)
                {
                    chip = new MDSound.MDSound.Chip();
                    chip.type = MDSound.MDSound.enmInstrumentType.K053260;
                    chip.ID = 0;
                    MDSound.K053260 k053260 = new MDSound.K053260();
                    chip.Instrument = k053260;
                    chip.Update = k053260.Update;
                    chip.Start = k053260.Start;
                    chip.Stop = k053260.Stop;
                    chip.Reset = k053260.Reset;
                    chip.SamplingRate = SamplingRate;
                    chip.Clock = getLE32(0xac);
                    chip.Volume = 0;
                    chip.Option = null;
                    lstChip.Add(chip);
                }

                if (getLE32(0xb4) != 0 && 0xb4 < vgmDataOffset - 3)
                {
                    chip = new MDSound.MDSound.Chip();
                    chip.type = MDSound.MDSound.enmInstrumentType.QSound;
                    chip.ID = 0;
                    MDSound.qsound qsound = new MDSound.qsound();
                    chip.Instrument = qsound;
                    chip.Update = qsound.Update;
                    chip.Start = qsound.Start;
                    chip.Stop = qsound.Stop;
                    chip.Reset = qsound.Reset;
                    chip.SamplingRate = SamplingRate;
                    chip.Clock = getLE32(0xb4);
                    chip.Volume = 0;
                    chip.Option = null;
                    lstChip.Add(chip);
                }

                if (version >= 0x170 && 0xdc < vgmDataOffset - 3)
                {
                    if (version >= 0x171)
                    {
                        if (getLE32(0xdc) != 0 && 0xdc < vgmDataOffset - 3)
                        {
                            chip = new MDSound.MDSound.Chip();
                            chip.type = MDSound.MDSound.enmInstrumentType.C352;
                            chip.ID = 0;
                            MDSound.c352 c352 = new MDSound.c352();
                            chip.Instrument = c352;
                            chip.Update = c352.Update;
                            chip.Start = c352.Start;
                            chip.Stop = c352.Stop;
                            chip.Reset = c352.Reset;
                            chip.SamplingRate = SamplingRate;
                            chip.Clock = getLE32(0xdc);
                            chip.Volume = 0;
                            chip.Option = new object[] { vgmBuf[0xd6] };

                            lstChip.Add(chip);
                        }

                        if (getLE32(0xe0) != 0 && 0xe0 < vgmDataOffset - 3)
                        {
                            chip = new MDSound.MDSound.Chip();
                            chip.type = MDSound.MDSound.enmInstrumentType.GA20;
                            chip.ID = 0;
                            MDSound.iremga20 ga20 = new MDSound.iremga20();
                            chip.Instrument = ga20;
                            chip.Update = ga20.Update;
                            chip.Start = ga20.Start;
                            chip.Stop = ga20.Stop;
                            chip.Reset = ga20.Reset;
                            chip.SamplingRate = SamplingRate;
                            chip.Clock = getLE32(0xe0);
                            chip.Volume = 0;
                            chip.Option = null;

                            lstChip.Add(chip);
                        }

                    }
                }
            }


            chips = lstChip.ToArray();
            mds = new MDSound.MDSound(SamplingRate, samplingBuffer, chips);

            sdlCbHandle = GCHandle.Alloc(sdlCb);
            sdlCbPtr = Marshal.GetFunctionPointerForDelegate(sdlCb);
            sdl = new SdlDotNet.Audio.AudioStream((int)SamplingRate, AudioFormat.Signed16Little, SoundChannel.Stereo, (short)samplingBuffer, sdlCb, null);
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

        private static void callback(IntPtr userData, IntPtr stream, int len)
        {

            for (int i = 0; i < len/4; i++)
            {
                short[] buf = new short[2];
                mds.Update(buf, 0, 2, oneFrameVGM);
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
                        mds.WriteSN76489(0, vgmBuf[vgmAdr + 1]);
                        vgmAdr += 2;
                        break;
                    case 0x51: //YM2413
                        rAdr = vgmBuf[vgmAdr + 1];
                        rDat = vgmBuf[vgmAdr + 2];
                        vgmAdr += 3;
                        mds.WriteYM2413(0, rAdr, rDat);
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
                        mds.WriteYM2203(0, rAdr, rDat);

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
                        mds.WriteYM2609(0, 0, rAdr, rDat);

                        break;
                    case 0x57: //YM2609 Port1
                        rAdr = vgmBuf[vgmAdr + 1];
                        rDat = vgmBuf[vgmAdr + 2];
                        vgmAdr += 3;
                        mds.WriteYM2609(0, 1, rAdr, rDat);

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
                    case 0x5c: //Y8950
                        rAdr = vgmBuf[vgmAdr + 1];
                        rDat = vgmBuf[vgmAdr + 2];
                        vgmAdr += 3;
                        mds.WriteY8950(0, rAdr, rDat);

                        break;
                    case 0x5D: //YMZ280B
                        rAdr = vgmBuf[vgmAdr + 1];
                        rDat = vgmBuf[vgmAdr + 2];
                        vgmAdr += 3;
                        mds.WriteYMZ280B(0, rAdr, rDat);

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
                        byte chipID = 0;
                        if ((bLen & 0x80000000) != 0)
                        {
                            bLen &= 0x7fffffff;
                            chipID = 1;
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
                                        //SEGA PCM
                                        mds.WriteSEGAPCMPCMData(chipID, romSize, startAddress, bLen - 8, vgmBuf, vgmAdr + 15);
                                        break;
                                    case 0x81:

                                        // YM2608

                                        //mds.WriteYM2608(0, 0x1, 0x00, 0x20);
                                        //mds.WriteYM2608(0, 0x1, 0x00, 0x21);
                                        //mds.WriteYM2608(0, 0x1, 0x00, 0x00);

                                        //mds.WriteYM2608(0, 0x1, 0x10, 0x00);
                                        //mds.WriteYM2608(0, 0x1, 0x10, 0x80);

                                        //mds.WriteYM2608(0, 0x1, 0x00, 0x61);
                                        //mds.WriteYM2608(0, 0x1, 0x00, 0x68);
                                        //mds.WriteYM2608(0, 0x1, 0x01, 0x00);

                                        //mds.WriteYM2608(0, 0x1, 0x02, (byte)((startAddress >> 2) & 0xff));
                                        //mds.WriteYM2608(0, 0x1, 0x03, (byte)((startAddress >> 10) & 0xff));
                                        //mds.WriteYM2608(0, 0x1, 0x04, 0xff);
                                        //mds.WriteYM2608(0, 0x1, 0x05, 0xff);
                                        //mds.WriteYM2608(0, 0x1, 0x0c, 0xff);
                                        //mds.WriteYM2608(0, 0x1, 0x0d, 0xff);

                                        //// データ転送
                                        //for (int cnt = 0; cnt < bLen - 8; cnt++)
                                        //{
                                        //    mds.WriteYM2608(0, 0x1, 0x08, vgmBuf[vgmAdr + 15 + cnt]);
                                        //}
                                        //mds.WriteYM2608(0, 0x1, 0x00, 0x00);
                                        //mds.WriteYM2608(0, 0x1, 0x10, 0x80);

                                        // YM2609

                                        mds.WriteYM2609(0, 0x1, 0x00, 0x20);
                                        mds.WriteYM2609(0, 0x1, 0x00, 0x21);
                                        mds.WriteYM2609(0, 0x1, 0x00, 0x00);

                                        mds.WriteYM2609(0, 0x1, 0x10, 0x00);
                                        mds.WriteYM2609(0, 0x1, 0x10, 0x80);

                                        mds.WriteYM2609(0, 0x1, 0x00, 0x61);
                                        mds.WriteYM2609(0, 0x1, 0x00, 0x68);
                                        mds.WriteYM2609(0, 0x1, 0x01, 0x00);

                                        mds.WriteYM2609(0, 0x1, 0x02, (byte)((startAddress >> 2) & 0xff));
                                        mds.WriteYM2609(0, 0x1, 0x03, (byte)((startAddress >> 10) & 0xff));
                                        mds.WriteYM2609(0, 0x1, 0x04, 0xff);
                                        mds.WriteYM2609(0, 0x1, 0x05, 0xff);
                                        mds.WriteYM2609(0, 0x1, 0x0c, 0xff);
                                        mds.WriteYM2609(0, 0x1, 0x0d, 0xff);

                                        // データ転送
                                        for (int cnt = 0; cnt < bLen - 8; cnt++)
                                        {
                                            mds.WriteYM2609(0, 0x1, 0x08, vgmBuf[vgmAdr + 15 + cnt]);
                                        }
                                        mds.WriteYM2609(0, 0x1, 0x00, 0x00);
                                        mds.WriteYM2609(0, 0x1, 0x10, 0x80);

                                        break;

                                    case 0x82:
                                        if (bufYM2610AdpcmA == null || bufYM2610AdpcmA.Length != romSize) bufYM2610AdpcmA = new byte[romSize];
                                        for (int cnt = 0; cnt < bLen - 8; cnt++)
                                        {
                                            bufYM2610AdpcmA[startAddress + cnt] = vgmBuf[vgmAdr + 15 + cnt];
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

                                    case 0x84:
                                        mds.WriteYMF278BPCMData(chipID, romSize, startAddress, bLen - 8, vgmBuf, vgmAdr + 15);
                                        break;

                                    case 0x85:
                                        mds.WriteYMF271PCMData(chipID, romSize, startAddress, bLen - 8, vgmBuf, vgmAdr + 15);
                                        break;

                                    case 0x86:
                                        mds.WriteYMZ280BPCMData(chipID, romSize, startAddress, bLen - 8, vgmBuf, vgmAdr + 15);
                                        break;

                                    case 0x88:
                                        mds.WriteY8950PCMData(chipID, romSize, startAddress, bLen - 8, vgmBuf, vgmAdr + 15);
                                        break;

                                    case 0x89:
                                        mds.WriteMultiPCMPCMData(chipID, romSize, startAddress, bLen - 8, vgmBuf, vgmAdr + 15);
                                        break;

                                    case 0x8b:
                                        mds.WriteOKIM6295PCMData(chipID, romSize, startAddress, bLen - 8, vgmBuf, vgmAdr + 15);
                                        break;

                                    case 0x8c:
                                        mds.WriteK054539PCMData(chipID, romSize, startAddress, bLen - 8, vgmBuf, vgmAdr + 15);
                                        break;

                                    case 0x8d:
                                        mds.WriteC140PCMData(chipID, romSize, startAddress, bLen - 8, vgmBuf, vgmAdr + 15);
                                        break;

                                    case 0x8e:
                                        mds.WriteK053260PCMData(chipID, romSize, startAddress, bLen - 8, vgmBuf, vgmAdr + 15);
                                        break;

                                    case 0x8f:
                                        mds.WriteQSoundPCMData(chipID, romSize, startAddress, bLen - 8, vgmBuf, vgmAdr + 15);
                                        break;

                                    case 0x92:
                                        mds.WriteC352PCMData(0, romSize, startAddress, bLen - 8, vgmBuf, vgmAdr + 15);
                                        break;

                                    case 0x93:
                                        mds.WriteGA20PCMData(0, romSize, startAddress, bLen - 8, vgmBuf, vgmAdr + 15);
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
                        uint chipReadOffset = getLE24(vgmAdr + 3);
                        uint chipWriteOffset = getLE24(vgmAdr + 6);
                        uint chipDataSize = getLE24(vgmAdr + 9);
                        if (chipDataSize == 0) chipDataSize = 0x1000000;
                        uint? pcmAdr = GetPCMAddressFromPCMBank(chipType, chipReadOffset);
                        if (pcmAdr != null && chipType == 0x01)
                        {
                            mds.WriteRF5C68PCMData(0, chipWriteOffset, chipDataSize, PCMBank[chipType].Data, (uint)pcmAdr);
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
                        mds.WriteYM2612(0, 0, 0x2a, vgmBuf[vgmPcmPtr++]);
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
                    case 0xb3: //GB DMG
                        rAdr = vgmBuf[vgmAdr + 1];
                        rDat = vgmBuf[vgmAdr + 2];
                        vgmAdr += 3;
                        mds.WriteDMG(0, rAdr, rDat);
                        break;
                    case 0xb4: //NES
                        rAdr = vgmBuf[vgmAdr + 1];
                        rDat = vgmBuf[vgmAdr + 2];
                        vgmAdr += 3;
                        mds.WriteNES(0, rAdr, rDat);
                        break;
                    case 0xb5: //MultiPCM
                        rAdr = (byte)(vgmBuf[vgmAdr + 1] & 0x7f);
                        rDat = vgmBuf[vgmAdr + 2];
                        vgmAdr += 3;
                        mds.WriteMultiPCM(0, rAdr, rDat);
                        break;
                    case 0xb7:
                        vgmAdr += 3;
                        break;
                    case 0xb8:
                        rAdr = vgmBuf[vgmAdr + 1];
                        rDat = vgmBuf[vgmAdr + 2];
                        vgmAdr += 3;
                        mds.WriteOKIM6295(0, rAdr, rDat);
                        break;
                    case 0xb9: //HuC6280
                        rAdr = vgmBuf[vgmAdr + 1];
                        rDat = vgmBuf[vgmAdr + 2];
                        vgmAdr += 3;
                        mds.WriteHuC6280(0, rAdr, rDat);
                        break;
                    case 0xba: //K053260
                        rAdr = vgmBuf[vgmAdr + 1];
                        rDat = vgmBuf[vgmAdr + 2];
                        vgmAdr += 3;
                        mds.WriteK053260(0, rAdr, rDat);

                        break;
                    case 0xbf: //GA20
                        rAdr = vgmBuf[vgmAdr + 1];
                        rDat = vgmBuf[vgmAdr + 2];
                        vgmAdr += 3;
                        mds.WriteGA20(0, rAdr, rDat);

                        break;
                    case 0xc0://segaPCM
                        mds.WriteSEGAPCM(0, (int)((vgmBuf[vgmAdr + 0x01] & 0xFF) | ((vgmBuf[vgmAdr + 0x02] & 0xFF) << 8)), vgmBuf[vgmAdr + 0x03]);
                        vgmAdr += 4;
                        break;
                    case 0xc3://MultiPCM
                        byte multiPCM_ch = (byte)(vgmBuf[vgmAdr + 1] & 0x7f);
                        int multiPCM_adr = vgmBuf[vgmAdr + 2] + vgmBuf[vgmAdr + 3] * 0x100;
                        vgmAdr += 4;
                        mds.WriteMultiPCMSetBank(0, multiPCM_ch, multiPCM_adr);
                        break;
                    case 0xc4://QSound
                        mds.WriteQSound(0, 0x00, vgmBuf[vgmAdr + 1]);
                        mds.WriteQSound(0, 0x01, vgmBuf[vgmAdr + 2]);
                        mds.WriteQSound(0, 0x02, vgmBuf[vgmAdr + 3]);
                        vgmAdr += 4;
                        break;
                    case 0xd0: //YMF278B
                        byte ymf278b_port = (byte)(vgmBuf[vgmAdr + 1] & 0x7f);
                        byte ymf278b_offset = vgmBuf[vgmAdr + 2];
                        rDat = vgmBuf[vgmAdr + 3];
                        byte ymf278b_chipid = (byte)((vgmBuf[vgmAdr + 1] & 0x80) != 0 ? 1 : 0);
                        vgmAdr += 4;
                        mds.WriteYMF278B(ymf278b_chipid, ymf278b_port, ymf278b_offset, rDat);
                        break;
                    case 0xd1: //YMF271
                        byte ymf271_port = (byte)(vgmBuf[vgmAdr + 1] & 0x7f);
                        byte ymf271_offset = vgmBuf[vgmAdr + 2];
                        rDat = vgmBuf[vgmAdr + 3];
                        byte ymf271_chipid = (byte)((vgmBuf[vgmAdr + 1] & 0x80) != 0 ? 1 : 0);
                        vgmAdr += 4;
                        mds.WriteYMF271(ymf271_chipid, ymf271_port, ymf271_offset, rDat);
                        break;
                    case 0xd2: //SCC1(K051649?)
                        int scc1_port = vgmBuf[vgmAdr + 1] & 0x7f;
                        byte scc1_offset = vgmBuf[vgmAdr + 2];
                        rDat = vgmBuf[vgmAdr + 3];
                        byte scc1_chipid = (byte)((vgmBuf[vgmAdr + 1] & 0x80) != 0 ? 1 : 0);
                        vgmAdr += 4;
                        mds.WriteK051649(scc1_chipid, (scc1_port << 1) | 0x00, scc1_offset);
                        mds.WriteK051649(scc1_chipid, (scc1_port << 1) | 0x01, rDat);
                        break;
                    case 0xd3: //K054539
                        int k054539_adr = (vgmBuf[vgmAdr + 1] & 0x7f) * 0x100 + vgmBuf[vgmAdr + 2];
                        rDat = vgmBuf[vgmAdr + 3];
                        byte chipid = (byte)((vgmBuf[vgmAdr + 1] & 0x80) != 0 ? 1 : 0);
                        vgmAdr += 4;
                        mds.WriteK054539(chipid, k054539_adr, rDat);
                        break;
                    case 0xd4: //C140
                        int c140_adr = (vgmBuf[vgmAdr + 1] & 0x7f) * 0x100 + vgmBuf[vgmAdr + 2];
                        rDat = vgmBuf[vgmAdr + 3];
                        byte c140_chipid = (byte)((vgmBuf[vgmAdr + 1] & 0x80) != 0 ? 1 : 0);
                        vgmAdr += 4;
                        mds.WriteC140(c140_chipid, (uint)c140_adr, rDat);
                        break;
                    case 0xe0: //seek to offset in PCM data bank
                        vgmPcmPtr = getLE32(vgmAdr + 1) + vgmPcmBaseAdr;
                        vgmAdr += 5;
                        break;
                    case 0xe1: //C352
                        uint adr = (uint)((vgmBuf[vgmAdr + 1] & 0xff) * 0x100 + (vgmBuf[vgmAdr + 2] & 0xff));
                        uint dat = (uint)((vgmBuf[vgmAdr + 3] & 0xff) * 0x100 + (vgmBuf[vgmAdr + 4] & 0xff));
                        vgmAdr += 5;
                        mds.WriteC352(0, adr, dat);

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
        private static uint? GetPCMAddressFromPCMBank(byte chipType, uint chipReadOffset)
        {
            if (chipType >= PCM_BANK_COUNT)
                return null;

            if (chipReadOffset >= PCMBank[chipType].DataSize)
                return null;

            return chipReadOffset;
        }

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


        private static UInt32 getLE16(UInt32 adr)
        {
            UInt32 dat;
            dat = (UInt32)vgmBuf[adr] + (UInt32)vgmBuf[adr + 1] * 0x100;

            return dat;
        }

        private static UInt32 getLE24(UInt32 adr)
        {
            UInt32 dat;
            dat = (UInt32)vgmBuf[adr] + (UInt32)vgmBuf[adr + 1] * 0x100 + (UInt32)vgmBuf[adr + 2] * 0x10000;

            return dat;
        }

        private static UInt32 getLE32(UInt32 adr)
        {
            UInt32 dat;
            dat = (UInt32)vgmBuf[adr] + (UInt32)vgmBuf[adr + 1] * 0x100 + (UInt32)vgmBuf[adr + 2] * 0x10000 + (UInt32)vgmBuf[adr + 3] * 0x1000000;

            return dat;
        }


    }
}
