using System;
using System.Collections.Generic;
using System.Text;

namespace MDSound
{
    public class P86 : Instrument
    {
        public P86()
        {
        }

        public override string Name { get => "PC-9801-86"; set { } }
        public override string ShortName { get => "P86"; set { } }

        public override void Reset(byte ChipID)
        {
            _Init();
        }

        public override uint Start(byte ChipID, uint clock)
        {
            return Start(ChipID, clock, 0);
        }

        public override uint Start(byte ChipID, uint clock, uint ClockValue, params object[] option)
        {
            this.SamplingRate = (double)clock;
            Reset(ChipID);
            return clock;
        }

        public override void Stop(byte ChipID)
        {
            ;//none
        }

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {
            if (play86_flag == false) return;
            if (size <= 1)
            {       // 一次補間対策
                play86_flag = false;
                return;
            }
            ;
            switch (pcm86_pan_flag)
            {
                case 0: double_trans(outputs, samples); break;
                case 1: left_trans(outputs, samples); break;
                case 2: right_trans(outputs, samples); break;
                case 3: double_trans(outputs, samples); break;
                case 4: double_trans_g(outputs, samples); break;
                case 5: left_trans_g(outputs, samples); break;
                case 6: right_trans_g(outputs, samples); break;
                case 7: double_trans_g(outputs, samples); break;
            }
        }

        public override int Write(byte ChipID, int port, int adr, int data)
        {
            switch ((byte)port)
            {
                case 0x00:
                    //Init
                    break;
                case 0x01://LoadPcm
                    ;
                    break;
                case 0x02://音色
                    _start_ofs = inst[data].start;
                    _size = inst[data].size;
                    repeat_flag = false;
                    release_flag1 = false;
                    break;
                case 0x03://パン
                    pcm86_pan_flag = adr;
                    pcm86_pan_dat = data;
                    break;
                case 0x04://音量
                    vol = (byte)data;
                    break;
                case 0x05://ontei
                    int _srcrate = adr >> 5;
                    uint _ontei = (uint)((adr & 0x1f) * 0x10000 + data);
                    if (_srcrate < 0 || _srcrate > 7)
                        break;
                    if (_ontei > 0x1f_ffff)
                        break;

                    ontei = (uint)_ontei;
                    srcrate = ratetable[_srcrate];

                    //Console.WriteLine("_ontei:{0:x} srcrate:{1:x}", _ontei , srcrate);
                    _ontei = (uint)(_ontei * srcrate / (long)SamplingRate);

                    addsize2 = (int)((_ontei & 0xffff) >> 4);
                    addsize1 = (int)(_ontei >> 16);

                    break;
                case 0x06://loop
                    break;
                case 0x07://play
                    start_ofs = _start_ofs;
                    start_ofs_x = 0;
                    size = _size;
                    play86_flag = true;
                    release_flag2 = false;
                    break;
                case 0x08://stop
                    play86_flag = false;
                    break;
                case 0x09://keyoff
                    if (release_flag1 == true)
                    {       // リリースが設定されているか?
                        start_ofs = release_ofs;
                        size = release_size;
                        release_flag2 = true;       // リリースした
                    }
                    else
                    {
                        play86_flag = false;
                    }
                    break;
            }

            return 0;
        }



        private double SamplingRate = 44100.0;
        private byte[] pcmData = null;
        private int MAXInst = 256;
        private P86Inst[] inst = new P86Inst[256];


        //from PMDWin
        private bool interpolation = false;                         // 補完するか？
        private int rate;                                   // 再生周波数
        private int srcrate;                                // 元データの周波数
        private uint ontei;                                 // 音程(fnum)
        private int vol;                                    // 音量
        private int p86_addr;                                // P86 保存用メモリポインタ
        private int start_ofs;                               // 発音中PCMデータ番地
        private int start_ofs_x;                            // 発音中PCMデータ番地（小数部）
        private int size;                                   // 残りサイズ
        private int _start_ofs;                          // 発音開始PCMデータ番地
        private int _size;                                  // PCMデータサイズ
        private int addsize1;                               // PCMアドレス加算値 (整数部)
        private int addsize2;                               // PCMアドレス加算値 (小数部)
        private int repeat_ofs;                          // リピート開始位置
        private int repeat_size;                            // リピート後のサイズ
        private int release_ofs;                         // リリース開始位置
        private int release_size;                           // リリース後のサイズ
        private bool repeat_flag;                           // リピートするかどうかのflag
        private bool release_flag1;                         // リリースするかどうかのflag
        private bool release_flag2;                         // リリースしたかどうかのflag

        private int pcm86_pan_flag;     // パンデータ１(bit0=左/bit1=右/bit2=逆)
        private int pcm86_pan_dat;      // パンデータ２(音量を下げるサイドの音量値)
        private bool play86_flag;                           // 発音中?flag

        private int AVolume;
        private int[][] VolumeTable;//[16][256];					// 音量テーブル
        private int[] ratetable = new int[] { 4135, 5513, 8270, 11025, 16540, 22050, 33080, 44100 };


        internal class P86Inst
        {
            public int start = 0;
            public int size = 0;
        }

        public int LoadPcm(int port, byte address, byte data, byte[] pcmData)
        {
            this.pcmData = pcmData;

            //from PMDWin p86drv.cpp

            for (int i = 0; i < MAXInst; i++)
            {
                inst[i] = new P86Inst();

                inst[i].start =
                    pcmData[i * 6 + 0 + 12 + 1 + 3] +
                    pcmData[i * 6 + 1 + 12 + 1 + 3] * 0x100 +
                    pcmData[i * 6 + 2 + 12 + 1 + 3] * 0x10000;// - 0x610;
                inst[i].size =
                    pcmData[i * 6 + 3 + 12 + 1 + 3] +
                    pcmData[i * 6 + 4 + 12 + 1 + 3] * 0x100 +
                    pcmData[i * 6 + 5 + 12 + 1 + 3] * 0x10000;
            }

            return 0;
        }



        //-----------------------------------------------------------------------------
        //	初期化(内部処理)
        //-----------------------------------------------------------------------------
        private void _Init()
        {

            interpolation = false;
            rate = (int)SamplingRate;
            srcrate = ratetable[4];     // 16.54kHz
            ontei = 0;
            vol = 0;

            start_ofs = 0;
            start_ofs_x = 0;
            size = 0;
            _start_ofs = 0;
            _size = 0;
            addsize1 = 0;
            addsize2 = 0;
            repeat_ofs = 0;
            repeat_size = 0;
            release_ofs = 0;
            release_size = 0;
            repeat_flag = false;
            release_flag1 = false;
            release_flag2 = false;

            pcm86_pan_flag = 0;
            pcm86_pan_dat = 0;
            play86_flag = false;

            AVolume = 0;
            SetVolume(0);
        }

        //-----------------------------------------------------------------------------
        //	音量調整用
        //-----------------------------------------------------------------------------
        private void SetVolume(int volume)
        {
            MakeVolumeTable(volume);
        }


        //-----------------------------------------------------------------------------
        //	音量テーブル作成
        //-----------------------------------------------------------------------------
        private void MakeVolumeTable(int volume)
        {
            int i, j;
            int AVolume_temp;
            double temp;

            VolumeTable = new int[16][];
            AVolume_temp = (int)(0x1000 * Math.Pow(10.0, volume / 40.0));
            if (AVolume != AVolume_temp)
            {
                AVolume = AVolume_temp;
                for (i = 0; i < 16; i++)
                {
                    VolumeTable[i] = new int[256];
                    //@			temp = pow(2.0, (i + 15) / 2.0) * AVolume / 0x18000;
                    temp = i * AVolume / 256;
                    for (j = 0; j < 256; j++)
                    {
                        VolumeTable[i][j] = (int)((sbyte)(byte)j * temp);
                    }
                }
            }
        }



        //-----------------------------------------------------------------------------
        //	真ん中（一次補間なし）
        //-----------------------------------------------------------------------------
        private void double_trans(int[][] dest, int nsamples)
        {
            int i;
            int data;

            for (i = 0; i < nsamples; i++)
            {
                data = VolumeTable[vol][pcmData[start_ofs]];

                data = (short)Math.Max(Math.Min(data, short.MaxValue), short.MinValue);
                dest[0][i] += data;
                dest[1][i] += data;

                if (add_address())
                {
                    play86_flag = false;
                    return;
                }
            }
        }



        //-----------------------------------------------------------------------------
        //	真ん中（逆相、一次補間なし）
        //-----------------------------------------------------------------------------
        private void double_trans_g(int[][] dest, int nsamples)
        {
            int i;
            int data;

            for (i = 0; i < nsamples; i++)
            {
                data = VolumeTable[vol][pcmData[start_ofs]];

                dest[0][i] += data;
                dest[1][i] -= data;

                if (add_address())
                {
                    play86_flag = false;
                    return;
                }
            }
        }



        //-----------------------------------------------------------------------------
        //	左寄り（一次補間なし）
        //-----------------------------------------------------------------------------
        private void left_trans(int[][] dest, int nsamples)
        {
            int i;
            int data;

            for (i = 0; i < nsamples; i++)
            {
                data = VolumeTable[vol][pcmData[start_ofs]];

                dest[0][i] += data;
                data = data * pcm86_pan_dat / (256 / 2);
                dest[1][i] += data;

                if (add_address())
                {
                    play86_flag = false;
                    return;
                }
            }
        }



        //-----------------------------------------------------------------------------
        //	左寄り（逆相、一次補間なし）
        //-----------------------------------------------------------------------------
        private void left_trans_g(int[][] dest, int nsamples)
        {
            int i;
            int data;

            for (i = 0; i < nsamples; i++)
            {
                data = VolumeTable[vol][pcmData[start_ofs]];

                dest[0][i] += data;
                data = data * pcm86_pan_dat / (256 / 2);
                dest[1][i] -= data;

                if (add_address())
                {
                    play86_flag = false;
                    return;
                }
            }
        }



        //-----------------------------------------------------------------------------
        //	右寄り（一次補間なし）
        //-----------------------------------------------------------------------------
        private void right_trans(int[][] dest, int nsamples)
        {
            int i;
            int data;

            for (i = 0; i < nsamples; i++)
            {
                data = VolumeTable[vol][pcmData[start_ofs]];

                dest[1][i] += data;
                data = data * pcm86_pan_dat / (256 / 2);
                dest[0][i] += data;

                if (add_address())
                {
                    play86_flag = false;
                    return;
                }
            }
        }



        //-----------------------------------------------------------------------------
        //	右寄り（逆相、一次補間なし）
        //-----------------------------------------------------------------------------
        private void right_trans_g(int[][] dest, int nsamples)
        {
            int i;
            int data;

            for (i = 0; i < nsamples; i++)
            {
                data = VolumeTable[vol][pcmData[start_ofs]];

                dest[1][i] -= data;
                data = data * pcm86_pan_dat / (256 / 2);
                dest[0][i] += data;

                if (add_address())
                {
                    play86_flag = false;
                    return;
                }
            }
        }








        private bool add_address()
        {
            start_ofs_x += addsize2;
            if (start_ofs_x >= 0x1000)
            {
                start_ofs_x -= 0x1000;
                start_ofs++;
                size--;
            }
            start_ofs += addsize1;
            size -= addsize1;

            if (size > 1)
            {       // 一次補間対策
                return false;
            }
            else if (repeat_flag == false || release_flag2)
            {
                return true;
            }

            size = repeat_size;
            start_ofs = repeat_ofs;
            return false;
        }
    }
}
