using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SdlDotNet.Audio;

namespace testChip
{
    public partial class Form1 : Form
    {
        private static uint SamplingRate = 44100;
        private static uint samplingBuffer = 1024;
        private static MDSound.MDSound mds = null;

        private static short[] frames = new short[samplingBuffer * 4];
        private static AudioStream sdl;
        private static AudioCallback sdlCb = null;
        private static IntPtr sdlCbPtr;
        private static GCHandle sdlCbHandle;
        private static Action<string> dispMsg;

        private short testD = 0xff;
        private int testCnt = 40;
        private long scenarioCnt = 0;
        private long scenarioPtr = 0;
        private long scenarioIndex = 0;
        private short[] emuRenderBuf = new short[2];


        public Form1()
        {
            InitializeComponent();
            dispMsg = dispMessage;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            button2.Enabled = true;

            scenarioCnt = 0;
            scenarioPtr = 0;
            scenarioIndex = 0;
            mds = new MDSound.MDSound(SamplingRate, samplingBuffer, null);
            sdlCb = new AudioCallback(EmuCallback);
            sdlCbHandle = GCHandle.Alloc(sdlCb);
            sdlCbPtr = Marshal.GetFunctionPointerForDelegate(sdlCb);
            sdl = new SdlDotNet.Audio.AudioStream((int)SamplingRate, AudioFormat.Signed16Little, SoundChannel.Stereo, (short)samplingBuffer, sdlCb, null)
            {
                Paused = true
            };

            MDSound.MDSound.Chip[] chips = null;
            List<MDSound.MDSound.Chip> lstChip = new List<MDSound.MDSound.Chip>();

            MDSound.MDSound.Chip chip = InitChip();

            lstChip.Add(chip);

            chips = lstChip.ToArray();
            mds.Init(SamplingRate, samplingBuffer, chips);

            //割り込み開始
            sdl.Paused = false;
            textBox1.Text = "";
            dispMsg?.Invoke("合成開始");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            button1.Enabled = true;
            button2.Enabled = false;

            //割り込み停止
            sdl.Paused = true;
            dispMsg?.Invoke("合成停止");

        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (sdl == null) return;

            sdl.Paused = true;
            sdl.Close();
            sdl.Dispose();
            sdl = null;
            if (sdlCbHandle.IsAllocated) sdlCbHandle.Free();
        }

        private MDSound.MDSound.Chip InitChip()
        {
            MDSound.MDSound.Chip chip = new MDSound.MDSound.Chip();
            chip.type = MDSound.MDSound.enmInstrumentType.ZelMusic;
            chip.ID = 0;
            MDSound.ZM_1.ZelMusic ZelMusic = new MDSound.ZM_1.ZelMusic();
            chip.Instrument = ZelMusic;
            chip.Update = ZelMusic.Update;
            chip.Start = ZelMusic.Start;
            chip.Stop = ZelMusic.Stop;
            chip.Reset = ZelMusic.Reset;
            chip.SamplingRate = SamplingRate;
            chip.Clock = 8000000;
            chip.Volume = 0;
            chip.Option = null;
            return chip;
        }

        private void EmuCallback(IntPtr userData, IntPtr stream, int len)
        {
            long bufCnt = len / 4;

            for (int i = 0; i < bufCnt; i++)
            {
                {
                    mds.Update(emuRenderBuf, 0, 2, OneFrameStream);
                    frames[i * 2 + 0] = emuRenderBuf[0];
                    frames[i * 2 + 1] = emuRenderBuf[1];
                }

                // ↓　上の処理と差し替えると矩形波が出力するようになります。
                //{
                //    frames[i * 2 + 0] = testD;
                //    frames[i * 2 + 1] = testD;
                //    testCnt--;
                //    if (testCnt < 1)
                //    {
                //        testD = (short)-testD;
                //        testCnt = 40;
                //    }
                //}

                //Console.WriteLine("{0} {1}", emuRenderBuf[0], emuRenderBuf[1]);
            }

            Marshal.Copy(frames, 0, stream, len / 2);
        }

        private void OneFrameStream()
        {
            if (scenarioIndex >= scenario.Length) return;

            scenarioCnt++;
            while (scenarioPtr < scenarioCnt)
            {
                while (scenario[scenarioIndex].Key == scenarioPtr)
                {
                    scenario[scenarioIndex++].Value();
                    if (scenarioIndex >= scenario.Length) return;
                }
                scenarioPtr++;
            }
        }

        public void dispMessage(string msg)
        {
            try
            {
                Action<string> act = textBox1.AppendText;
                if (this.Created)
                {
                    this.BeginInvoke(act, String.Format("{0:yyyy-MM-dd(ddd)HH:mm:ss.fff} : {1}\r\n", System.DateTime.Now, msg));
                }
            }
            catch
            {
                ;//握りつぶす
            }
        }



        //経過時間ごとに行いたい処理をシナリオに書く
        private KeyValuePair<int, Action>[] scenario = new KeyValuePair<int, Action>[]
        {
            //                             frameCounter   実行する処理
            new KeyValuePair<int, Action>( 0            , vInit )// 開始時に初期化処理
            , new KeyValuePair<int, Action>( 44100*1    , vKeyonC ) //1秒後に vKeyonを実行
            , new KeyValuePair<int, Action>( 44100*2    , vEnd ) //2秒後に vEndを実行
            , new KeyValuePair<int, Action>( 44100*2    , vKeyonD ) //1秒後に vKeyonを実行
            , new KeyValuePair<int, Action>( 44100*3    , vEnd ) //2秒後に vEndを実行
            , new KeyValuePair<int, Action>( 44100*3    , vKeyonE ) //1秒後に vKeyonを実行
            , new KeyValuePair<int, Action>( 44100*4    , vEnd ) //2秒後に vEndを実行
        };

        private static void vInit()
        {
            dispMsg?.Invoke("初期化");

            byte[] dat=System.IO.File.ReadAllBytes("Guitar_8bit_8kHz_mono.raw");
            for (int i = 0; i < dat.Length; i++)
                mds.WriteZM1(0, 2, i, dat[i]);
        }

        private static void vKeyonC()
        {
            dispMsg?.Invoke("キーオン C");
            mds.WriteZM1(0, 1, 0x80 + 0x00, 0x00); //PCM Mode : 0
            mds.WriteZM1(0, 1, 0x80 + 0x01, 0x00); //Play Address : 0
            mds.WriteZM1(0, 1, 0x80 + 0x02, 0x00);
            mds.WriteZM1(0, 1, 0x80 + 0x03, 0x00);
            mds.WriteZM1(0, 1, 0x80 + 0x04, 0x00);
            mds.WriteZM1(0, 1, 0x80 + 0x05, 0x80); //Stop Address : 7999 ($0000_1f3f)
            mds.WriteZM1(0, 1, 0x80 + 0x06, 0x3e);
            mds.WriteZM1(0, 1, 0x80 + 0x07, 0x00);
            mds.WriteZM1(0, 1, 0x80 + 0x08, 0x00);
            mds.WriteZM1(0, 1, 0x80 + 0x12, 0x00); //PCM Config : 0

            mds.WriteZM1(0, 1, 0xf0 + 0x00, 0xff); // LEft  Volume:256
            mds.WriteZM1(0, 1, 0xf0 + 0x01, 0xff); // Right Volume:256

            mds.WriteZM1(0, 3, 0x00 + 0x00, 0x00); // key fraction : 0
            mds.WriteZM1(0, 3, 0x00 + 0x01, 0x00); // note : c
            mds.WriteZM1(0, 3, 0x00 + 0x02, 0x03); // octave : 4
            mds.WriteZM1(0, 3, 0x00 + 0x90, 0x80 | 0x00); //NoteOn | FreqMode:OPM(0) | KeyFraction:0
            // 0x00  144  O)ctave | N)ote | key (f)raction      00000OOO 0000NNNN 00ffffff
            //            or F)requency number               or FFFFFFFF FFFFFFFF FFFFFFFF
            // 0x90   48  N)ote on / frequency (M)ode           NM000000
        }

        private static void vKeyonD()
        {
            dispMsg?.Invoke("キーオン D");
            
            mds.WriteZM1(0, 1, 0xf0 + 0x00, 0x7f); // LEft  Volume:127
            mds.WriteZM1(0, 1, 0xf0 + 0x01, 0x7f); // Right Volume:127

            mds.WriteZM1(0, 3, 0x00 + 0x00, 0x00); // key fraction : 0
            mds.WriteZM1(0, 3, 0x00 + 0x01, 0x02); // note : D
            mds.WriteZM1(0, 3, 0x00 + 0x02, 0x03); // octave : 4
            mds.WriteZM1(0, 3, 0x00 + 0x90, 0x80 | 0x00); //NoteOn | FreqMode:OPM(0) | KeyFraction:0
        }

        private static void vKeyonE()
        {
            dispMsg?.Invoke("キーオン E");

            mds.WriteZM1(0, 1, 0xf0 + 0x00, 0x3f); // LEft  Volume:63
            mds.WriteZM1(0, 1, 0xf0 + 0x01, 0x3f); // Right Volume:63

            mds.WriteZM1(0, 3, 0x00 + 0x00, 0x00); // key fraction : 0
            mds.WriteZM1(0, 3, 0x00 + 0x01, 0x05); // note : D
            mds.WriteZM1(0, 3, 0x00 + 0x02, 0x03); // octave : 4
            mds.WriteZM1(0, 3, 0x00 + 0x90, 0x80 | 0x00); //NoteOn | FreqMode:OPM(0) | KeyFraction:0
        }

        private static void vEnd()
        {
            dispMsg?.Invoke("キーオフ");
            mds.WriteZM1(0, 3, 0x00 + 0x90, 0x00 | 0x00); // NoteOff | FreqMode:OPM(0)
        }

    }
}
