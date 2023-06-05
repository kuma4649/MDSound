using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MDSound.fmvgen
{
    //	YM2609(OPNA2) ---------------------------------------------------
    public class OPNA2 : fmgen.OPNABase
    {
        //プリセット保持向け
        public List<byte[]> presetRhythmPCMData = null;

        public static readonly float[] panTable = new float[4] { 1.0f, 0.7512f, 0.4512f, 0.0500f };

        // リズム音源関係
        private Rhythm[] rhythm = null;

        private sbyte rhythmtl;      // リズム全体の音量
        private int rhythmtvol;
        private byte rhythmkey;     // リズムのキー
        private byte chipID;

        protected FM6[] fm6 = null;
        protected PSG2[] psg2 = null;
        protected ADPCMB[] adpcmb = null;
        protected ADPCMA adpcma = null;

        protected new byte prescale;
        private reverb reverb;
        private distortion distortion;
        private chorus chorus;
        private effect.eq3band ep3band;
        private effect.ReversePhase reversePhase;
        private effect.HPFLPF hpflpf;
        private effect.Compressor compressor;
        public Dictionary<int, uint[]> dicOpeWav = new Dictionary<int, uint[]>();

        public class Rhythm
        {
            public byte pan;      // ぱん
            public sbyte level;     // おんりょう
            public int volume;     // おんりょうせってい
            public int[] sample;      // さんぷる
            public uint size;      // さいず
            public uint pos;       // いち
            public uint step;      // すてっぷち
            public uint rate;      // さんぷるのれーと
            public reverb reverb;
            public distortion distortion;
            public chorus chorus;
            public effect.HPFLPF hpflpf;
            public int efcCh;
            public int num;
            public effect.ReversePhase reversePhase;
            public effect.Compressor compressor;

            public Rhythm(int num, reverb reverb, distortion distortion,chorus chorus ,effect.HPFLPF hpflpf, effect.ReversePhase reversePhase, effect.Compressor compressor, int efcCh)
            {
                this.reverb = reverb;
                this.distortion = distortion;
                this.chorus = chorus;
                this.hpflpf = hpflpf;
                this.efcCh = efcCh;
                this.reversePhase = reversePhase;
                this.compressor = compressor;
                this.num = num;
            }
        };

        public class whdr
        {
            public uint chunksize;
            public uint tag;
            public uint nch;
            public uint rate;
            public uint avgbytes;
            public uint align;
            public uint bps;
            public uint size;
        }


        // ---------------------------------------------------------------------------
        //	構築
        //
        public OPNA2(byte ChipID,reverb reverb, distortion distortion, chorus chorus, effect.eq3band ep3band, effect.HPFLPF hpflpf, effect.ReversePhase reversePhase, effect.Compressor compressor)
        {
            this.reverb = reverb;
            this.distortion = distortion;
            this.chorus = chorus;
            this.ep3band = ep3band;
            this.hpflpf = hpflpf;
            this.reversePhase = reversePhase;
            this.compressor = compressor;

            fm6 = new FM6[2] {
                new FM6(0, reverb, distortion, chorus, hpflpf,reversePhase,compressor, 0,dicOpeWav),
                new FM6(1, reverb, distortion, chorus, hpflpf,reversePhase,compressor, 6,dicOpeWav) };
            psg2 = new PSG2[4] {
                new PSG2(0,reverb, distortion, chorus, hpflpf,reversePhase,compressor, 12),
                new PSG2(1,reverb, distortion, chorus, hpflpf,reversePhase,compressor, 15),
                new PSG2(2,reverb, distortion, chorus, hpflpf,reversePhase,compressor, 18),
                new PSG2(3,reverb, distortion, chorus, hpflpf,reversePhase,compressor, 21) };
            adpcmb = new ADPCMB[3] {
                new ADPCMB(0,reverb, distortion, chorus,hpflpf,reversePhase,compressor, 24),
                new ADPCMB(1,reverb, distortion, chorus,hpflpf,reversePhase,compressor, 25),
                new ADPCMB(2,reverb, distortion, chorus,hpflpf,reversePhase,compressor, 26) };
            rhythm = new Rhythm[6] {
                new Rhythm(0,reverb, distortion, chorus,hpflpf,reversePhase,compressor, 27),
                new Rhythm(1,reverb, distortion, chorus,hpflpf,reversePhase,compressor, 28),
                new Rhythm(2,reverb, distortion, chorus,hpflpf,reversePhase,compressor, 29),
                new Rhythm(3,reverb, distortion, chorus,hpflpf,reversePhase,compressor, 30),
                new Rhythm(4,reverb, distortion, chorus,hpflpf,reversePhase,compressor, 31),
                new Rhythm(5,reverb, distortion, chorus,hpflpf,reversePhase,compressor, 32) };
            adpcma = new ADPCMA(0, reverb, distortion, chorus, hpflpf, reversePhase, compressor, 33);

            for (int i = 0; i < 6; i++)
            {
                rhythm[i].sample = null;
                rhythm[i].pos = 0;
                rhythm[i].size = 0;
                rhythm[i].volume = 0;
            }
            rhythmtvol = 0;

            for (int i = 0; i < 2; i++)
            {
                fm6[i].parent = this;
            }

            for (int i = 0; i < 3; i++)
            {
                adpcmb[i].adpcmmask = (uint)((i == 0) ? 0x3ffff : 0xffffff);
                adpcmb[i].adpcmnotice = 4;
                adpcmb[i].deltan = 256;
                adpcmb[i].adpcmvol = 0;
                adpcmb[i].control2 = 0;
                adpcmb[i].shiftBit = (i == 0) ? 6 : 9;
                adpcmb[i].parent = this;
            }

            csmch = ch[2];
            this.chipID = ChipID;
        }

        ~OPNA2()
        {
            adpcmbuf = null;
            for (int i = 0; i < 6; i++)
            {
                rhythm[i].sample = null;
            }
        }

        public new bool Init(uint c, uint r)
        {
            return Init(c, r, false, "");
        }

        public bool Init(uint c, uint r, bool ipflag, string path)
        {
            return Init(c, r, ipflag, fname => CreateRhythmFileStream(path, fname), null, 0);
        }

        public bool Init(uint c, uint r, bool ipflag,
            Func<string, Stream> appendFileReaderCallback = null,
            byte[] _adpcma = null, int _adpcma_size = 0)
        {
            rate = 8000;
            LoadRhythmSample(appendFileReaderCallback);
            dicOpeWav.Clear();

            if (adpcmb[0].adpcmbuf == null)
                adpcmb[0].adpcmbuf = new byte[0x40000];
            if (adpcmb[0].adpcmbuf == null)
                return false;
            if (adpcmb[1].adpcmbuf == null)
                adpcmb[1].adpcmbuf = new byte[0x1000000];
            if (adpcmb[1].adpcmbuf == null)
                return false;
            if (adpcmb[2].adpcmbuf == null)
                adpcmb[2].adpcmbuf = new byte[0x1000000];
            if (adpcmb[2].adpcmbuf == null)
                return false;

            setAdpcmA(_adpcma, _adpcma_size);

            if (!SetRate(c, r, ipflag))
                return false;
            if (!base.Init(c, r, ipflag))
                return false;

            Reset();

            SetVolumeFM(0);
            SetVolumePSG(0);
            SetVolumeADPCM(0);
            SetVolumeRhythmTotal(0);
            for (int i = 0; i < 6; i++)
                SetVolumeRhythm(0, 0);
            SetChannelMask(0);

            return true;
        }

        public void setAdpcmA(byte[] _adpcma, int _adpcma_size)
        {
            adpcma.buf = _adpcma;
            adpcma.size = _adpcma_size;
        }

        public void setAdpcm012(int p,byte[] _adpcmb)
        {
            adpcmb[p].adpcmbuf = _adpcmb;
        }

        public void setOperatorWave(byte[] buf)
        {
            int waveCh=0;
            int wavetype=0;
            int wavecounter = 0;

            foreach (byte b in buf)
            {
                int cnt = wavecounter / 2;
                int d = wavecounter % 2;

                uint s;
                if (d == 0) s = b;
                else s = ((fmvgen.sinetable[waveCh][wavetype][cnt] & 0xff) | (uint)((b & 0x1f) << 8));

                fmvgen.sinetable[waveCh][wavetype][cnt] = s;
                wavecounter++;
                if (wavecounter > fmvgen.waveBufSize * 2)
                {
                    wavecounter = 0;
                    wavetype++;
                    if (wavetype > fmvgen.waveTypeSize)
                    {
                        wavetype = 0;
                        waveCh = 0;
                    }
                }
            }
        }

        public uint[] getOperatorWave(int waveCh,int wavetype)
        {
            return fmvgen.sinetable[waveCh][wavetype];
        }

        public void setOperatorWaveDic(int n, byte[] buf)
        {
            int wavecounter = 0;

            if (dicOpeWav.ContainsKey(n))
            {
                dicOpeWav.Remove(n);
            }

            uint[] ubuf = new uint[fmvgen.waveBufSize];
            uint s;
            foreach (byte b in buf)
            {
                int cnt = wavecounter / 2;
                int d = wavecounter % 2;

                if (d == 0) s = b;
                else s = ((ubuf[cnt] & 0xff) | (uint)((b & 0x1f) << 8));

                ubuf[cnt] = s;
                wavecounter++;
                if (wavecounter > fmvgen.waveBufSize * 2)
                {
                    wavecounter = 0;
                }
            }

            dicOpeWav.Add(n,ubuf);
        }

        public byte[] getPSGuserWave(int p, int n)
        {
            return psg2[p].GetUserWave(n);
        }

        // ---------------------------------------------------------------------------
        //	サンプリングレート変更
        //
        public new bool SetRate(uint c, uint r, bool ipflag = false)
        {
            if (!base.SetRate(c, r, ipflag))
                return false;

            RebuildTimeTable();
            for (int i = 0; i < 6; i++)
            {
                rhythm[i].step = rhythm[i].rate * 1024 / r;
            }

            for (int i = 0; i < 3; i++)
            {
                adpcmb[i].adplbase = (uint)((int)(8192.0 * (clock / 72.0) / r));
                adpcmb[i].adpld = (int)(adpcmb[i].deltan * adpcmb[i].adplbase >> 16);
            }

            adpcma.step = (int)((double)(c) / 54 * 8192 / r);
            return true;
        }

        // ---------------------------------------------------------------------------
        //	合成
        //	in:		buffer		合成先
        //			nsamples	合成サンプル数
        //
        public void Mix(int[] buffer, int nsamples)
        {

            fm6[0].Mix(buffer, nsamples, regtc);
            fm6[1].Mix(buffer, nsamples, regtc);
            psg2[0].Mix(buffer, nsamples);
            psg2[1].Mix(buffer, nsamples);
            psg2[2].Mix(buffer, nsamples);
            psg2[3].Mix(buffer, nsamples);
            adpcmb[0].Mix(buffer, nsamples);
            adpcmb[1].Mix(buffer, nsamples);
            adpcmb[2].Mix(buffer, nsamples);
            RhythmMix(buffer, nsamples);
            adpcma.Mix(buffer, (uint)nsamples);
            ep3band.Mix(buffer, nsamples);
            compressor.Mix(buffer, nsamples);
        }




        // ---------------------------------------------------------------------------
        //	リセット
        //
        public new void Reset()
        {
            reg29 = 0x1f;
            rhythmkey = 0;
            limitaddr = 0x3ffff;
            base.Reset();

            SetPrescaler(0);

            fm6[0].Reset();
            fm6[1].Reset();

            psg2[0].Reset();
            psg2[1].Reset();
            psg2[2].Reset();
            psg2[3].Reset();

            for (int i = 0; i < 3; i++)
            {
                adpcmb[i].statusnext = 0;
                adpcmb[i].memaddr = 0;
                adpcmb[i].adpcmd = 127;
                adpcmb[i].adpcmx = 0;
                adpcmb[i].adpcmplay = false;
                adpcmb[i].adplc = 0;
                adpcmb[i].adpld = 0x100;
            }
        }

        protected new void RebuildTimeTable()
        {
            base.RebuildTimeTable();

            int p = prescale;
            prescale = 0xff;//-1;
            SetPrescaler((uint)p);
        }

        public new void SetPrescaler(uint p)
        {
            base.SetPrescaler(p);

            sbyte[][] table = new sbyte[3][] { new sbyte[2] { 6, 4 }, new sbyte[2] { 3, 2 }, new sbyte[2] { 2, 1 } };
            byte[] table2 = new byte[8] { 108, 77, 71, 67, 62, 44, 8, 5 };
            // 512
            if (prescale != p)
            {
                prescale = (byte)p;
                //assert(0 <= prescale && prescale< 3);

                uint fmclock = (uint)(clock / table[p][0] / 12);

                rate = psgrate;

                // 合成周波数と出力周波数の比
                //assert(fmclock< (0x80000000 >> FM_RATIOBITS));
                uint ratio = ((fmclock << fmvgen.FM_RATIOBITS) + rate / 2) / rate;

                SetTimerBase(fmclock);
                //		MakeTimeTable(ratio);
                fm6[0].chip.SetRatio(ratio);
                fm6[1].chip.SetRatio(ratio);

                psg2[0].SetClock((int)(clock / table[p][1]), (int)psgrate);
                psg2[1].SetClock((int)(clock / table[p][1]), (int)psgrate);
                psg2[2].SetClock((int)(clock / table[p][1]), (int)psgrate);
                psg2[3].SetClock((int)(clock / table[p][1]), (int)psgrate);

                for (int i = 0; i < 8; i++)
                {
                    lfotable[i] = (ratio << (2 + fmvgen.FM_LFOCBITS - fmvgen.FM_RATIOBITS)) / table2[i];
                }
            }
        }

        // ---------------------------------------------------------------------------
        //	レジスタアレイにデータを設定
        //
        public new void SetReg(uint addr, uint data)
        {
            addr &= 0x3ff;

            if (addr < 0x10)
            {
                psg2[0].SetReg(addr, (byte)data);
                return;
            }
            else if (addr >= 0x10 && addr < 0x20)
            {
                RhythmSetReg(addr, (byte)data);
                return;
            }
            else if (addr >= 0xc0 && addr < 0xcc)
            {
                ep3band.SetReg(addr & 0xf, (byte)data);
                return;
            }
            else if (addr >= 0xcc && addr < 0xd9)
            {
                reversePhase.SetReg(addr - 0xcc, (byte)data);
                return;
            }
            else if (addr >= 0x100 && addr < 0x111)
            {
                AdpcmbSetReg(0, addr - 0x100, (byte)data);
                return;
            }
            else if (addr >= 0x111 && addr < 0x118)
            {
                adpcma.SetReg(addr - 0x111, (byte)data);
                return;
            }
            else if (addr >= 0x118 && addr < 0x120)
            {
                return;
            }
            else if (addr >= 0x120 && addr < 0x130)
            {
                psg2[1].SetReg(addr - 0x120, (byte)data);
                return;
            }
            else if (addr >= 0x1c0 && addr < 0x1c7)
            {
                compressor.SetReg(true, addr - 0x1c0 + 1, (byte)data);
                return;
            }
            else if (addr >= 0x200 && addr < 0x210)
            {
                psg2[2].SetReg(addr - 0x200, (byte)data);
                return;
            }
            else if (addr >= 0x210 && addr < 0x220)
            {
                psg2[3].SetReg(addr - 0x210, (byte)data);
                return;
            }
            else if (addr >= 0x300 && addr < 0x311)
            {
                AdpcmbSetReg(1, addr - 0x300, (byte)data);
                return;
            }
            else if (addr >= 0x311 && addr < 0x322)
            {
                AdpcmbSetReg(2, addr - 0x311, (byte)data);
                return;
            }
            else if (addr >= 0x322 && addr < 0x325)
            {
                reverb.SetReg(addr - 0x322, (byte)data);
                if (addr == 0x323)
                {
                    distortion.SetReg(0, (byte)data);//channel変更はアドレスを共有
                    chorus.SetReg(0, (byte)data);
                    hpflpf.SetReg(0, (byte)data);
                    compressor.SetReg(false,0, (byte)data);
                }
                return;
            }
            else if (addr >= 0x325 && addr < 0x328)
            {
                distortion.SetReg(addr - 0x324, (byte)data);//distortionのアドレス0はリバーブと共有
                return;
            }
            else if (addr >= 0x328 && addr < 0x32C)
            {
                chorus.SetReg(addr - 0x327, (byte)data);//chorusのアドレス0はリバーブと共有
                return;
            }
            else if (addr >= 0x32C && addr < 0x330)
            {
                return;
            }
            else if (addr >= 0x3c0 && addr < 0x3c6)
            {
                hpflpf.SetReg(addr - 0x3bf, (byte)data);
                return;
            }
            else if (addr >= 0x3c6 && addr < 0x3cd)
            {
                compressor.SetReg(false, addr - 0x3c5, (byte)data);
                return;
            }

            if (addr < 0x200)
            {
                FmSetReg(0, addr, (byte)data);
            }
            else
            {
                FmSetReg(1, addr - 0x200, (byte)data);
            }

        }

        public void FmSetReg(int ch, uint addr, byte data)
        {
            fm6[ch].SetReg(addr, data);
        }

        public void AdpcmbSetReg(int ch, uint addr, byte data)
        {
            adpcmb[ch].SetADPCMBReg(addr, data);
        }

        public void RhythmSetReg(uint addr, byte data)
        {
            switch (addr)
            {
                // Rhythm ----------------------------------------------------------------
                case 0x10:          // DM/KEYON
                    if ((data & 0x80) == 0)  // KEY ON
                    {
                        rhythmkey |= (byte)(data & 0x3f);
                        if ((data & 0x01) != 0) rhythm[0].pos = 0;
                        if ((data & 0x02) != 0) rhythm[1].pos = 0;
                        if ((data & 0x04) != 0) rhythm[2].pos = 0;
                        if ((data & 0x08) != 0) rhythm[3].pos = 0;
                        if ((data & 0x10) != 0) rhythm[4].pos = 0;
                        if ((data & 0x20) != 0) rhythm[5].pos = 0;
                    }
                    else
                    {                   // DUMP
                        rhythmkey &= (byte)(~(byte)data);
                    }
                    break;

                case 0x11:
                    rhythmtl = (sbyte)(~data & 63);
                    break;

                case 0x18:      // Bass Drum
                case 0x19:      // Snare Drum
                case 0x1a:      // Top Cymbal
                case 0x1b:      // Hihat
                case 0x1c:      // Tom-tom
                case 0x1d:      // Rim shot
                    rhythm[addr & 7].pan = (byte)((data >> 6) & 3);
                    rhythm[addr & 7].level = (sbyte)(~data & 31);
                    break;
            }
        }


        public new uint GetReg(uint addr)
        {
            return 0;
        }

        //	音量設定
        public new void SetVolumeFM(int db)
        {
            db = Math.Min(db, 20);
            if (db > -192)
            {
                fm6[0].fmvolume = (int)(16384.0 * Math.Pow(10.0, db / 40.0));
                fm6[1].fmvolume = (int)(16384.0 * Math.Pow(10.0, db / 40.0));
            }
            else
            {
                fm6[0].fmvolume = 0;
                fm6[1].fmvolume = 0;
            }
        }

        public new void SetVolumePSG(int db)
        {
            psg2[0].SetVolume(db);
            psg2[1].SetVolume(db);
            psg2[2].SetVolume(db);
            psg2[3].SetVolume(db);
        }

        public void SetVolumeADPCM(int db)
        {
            db = Math.Min(db, 20);
            if (db > -192)
            {
                adpcmb[0].adpcmvol = (int)(65536.0 * Math.Pow(10.0, db / 40.0));
                adpcmb[1].adpcmvol = (int)(65536.0 * Math.Pow(10.0, db / 40.0));
                adpcmb[2].adpcmvol = (int)(65536.0 * Math.Pow(10.0, db / 40.0));
            }
            else
            {
                adpcmb[0].adpcmvol = 0;
                adpcmb[1].adpcmvol = 0;
                adpcmb[2].adpcmvol = 0;
            }

            adpcmb[0].adpcmvolume = (adpcmb[0].adpcmvol * adpcmb[0].adpcmlevel) >> 12;
            adpcmb[1].adpcmvolume = (adpcmb[1].adpcmvol * adpcmb[1].adpcmlevel) >> 12;
            adpcmb[2].adpcmvolume = (adpcmb[2].adpcmvol * adpcmb[2].adpcmlevel) >> 12;
        }

        // ---------------------------------------------------------------------------
        //	チャンネルマスクの設定
        //
        public void SetChannelMask(int mask)
        {
            for (int i = 0; i < 6; i++)
            {
                fm6[0].ch[i].Mute(!((mask & (1 << i)) == 0));
                fm6[1].ch[i].Mute(!((mask & (1 << i)) == 0));
            }

            psg2[0].SetChannelMask((int)(mask >> 6));
            psg2[1].SetChannelMask((int)(mask >> 6));
            psg2[2].SetChannelMask((int)(mask >> 6));
            psg2[3].SetChannelMask((int)(mask >> 6));

            adpcmb[0].adpcmmask_ = (mask & (1 << 9)) != 0;
            adpcmb[1].adpcmmask_ = (mask & (1 << 9)) != 0;
            adpcmb[2].adpcmmask_ = (mask & (1 << 9)) != 0;

            rhythmmask_ = (int)((mask >> 10) & ((1 << 6) - 1));
        }

        public byte[] GetADPCMBuffer()
        {
            return adpcmbuf;
        }

        // ---------------------------------------------------------------------------
        //	リズム合成
        //
        private void RhythmMix(int[] buffer, int count)
        {
            if (rhythmtvol < 128 && rhythm[0].sample != null && ((rhythmkey & 0x3f) != 0))
            {
                int limit = (int)count * 2;
                visRtmVolume[0] = 0;
                visRtmVolume[1] = 0;
                for (int i = 0; i < 6; i++)
                {
                    Rhythm r = rhythm[i];
                    if ((rhythmkey & (1 << i)) != 0 && (byte)r.level < 128)
                    {
                        int db = fmvgen.Limit(rhythmtl + rhythmtvol + r.level + r.volume, 127, -31);
                        int vol = tltable[fmvgen.FM_TLPOS + (db << (fmvgen.FM_TLBITS - 7))] >> 4;
                        int maskl = -((r.pan >> 1) & 1);
                        int maskr = -(r.pan & 1);

                        if ((rhythmmask_ & (1 << i)) != 0)
                        {
                            maskl = maskr = 0;
                        }

                        for (int dest = 0; dest < limit && r.pos < r.size; dest += 2)
                        {
                            int sample = (r.sample[r.pos / 1024] * vol) >> 12;
                            r.pos += r.step;

                            int sL = sample;
                            int sR = sample;
                            distortion.Mix(r.efcCh, ref sL, ref sR);
                            chorus.Mix(r.efcCh, ref sL, ref sR);
                            hpflpf.Mix(r.efcCh, ref sL, ref sR);
                            compressor.Mix(r.efcCh, ref sL, ref sR);

                            sL = sL & maskl;
                            sR = sR & maskr;
                            sL *= reversePhase.Rhythm[i][0];
                            sR *= reversePhase.Rhythm[i][1];
                            int revSampleL = (int)(sL * reverb.SendLevel[r.efcCh]);
                            int revSampleR = (int)(sR * reverb.SendLevel[r.efcCh]);
                            fmvgen.StoreSample(ref buffer[dest + 0], sL);
                            fmvgen.StoreSample(ref buffer[dest + 1], sR);
                            reverb.StoreDataC(revSampleL, revSampleR);
                            visRtmVolume[0] += sample & maskl;
                            visRtmVolume[1] += sample & maskr;
                        }
                    }
                }
            }
        }

        private FileStream CreateRhythmFileStream(string dir, string fname)
        {
            string path = string.IsNullOrEmpty(dir) ? fname : Path.Combine(dir, fname);
            return File.Exists(path) ? new FileStream(path, FileMode.Open, FileAccess.Read) : null;
        }

        public bool LoadRhythmSample(string path)
        {
            return LoadRhythmSample(fname => CreateRhythmFileStream(path, fname));
        }

        // ---------------------------------------------------------------------------
        //	リズム音を読みこむ
        //
        public bool LoadRhythmSample(Func<string, Stream> appendFileReaderCallback)
        {
            string[] rhythmname = new string[6]
            {
                "BD", "SD", "TOP", "HH", "TOM", "RIM",
            };

            int i;
            for (i = 0; i < 6; i++)
                rhythm[i].pos = ~(uint)0;

            for (i = 0; i < 6; i++)
            {
                //byte[] buf = null;
                //int filePtr = 0;
                try
                {
                    uint fsize;
                    string buf1 = string.Format("2608_{0}_{1}.WAV", rhythmname[i], chipID);
                    string buf2 = string.Format("2608_{0}.WAV", rhythmname[i]);
                    string rymBuf1 = string.Format("2608_RYM_{0}.WAV", chipID);
                    string rymBuf2 = "2608_RYM.WAV";
                    byte[] file;

                    using (Stream st = appendFileReaderCallback?.Invoke(buf1))
                    {
                        file = common.ReadAllBytes(st);
                    }
                    //読み込めなかった場合はプリセットを参照してみる
                    if (file == null)
                    {
                        if (presetRhythmPCMData != null)
                        {
                            if (presetRhythmPCMData.Count > i)
                            {
                                file = presetRhythmPCMData[i];
                                if (file != null && file.Length < 1) file = null;
                            }
                        }
                    }
                    //読み込めなかった場合はいつものファイルを読んでみる
                    if (file == null)
                    {
                        using (Stream st = appendFileReaderCallback?.Invoke(buf2))
                        {
                            file = common.ReadAllBytes(st);
                        }
                    }

                    //リムショットのファイル名は2パターンあるので更に読み込みに挑戦する
                    if (file == null)
                    {
                        if (i == 5)
                        {
                            using (Stream st = appendFileReaderCallback?.Invoke(rymBuf1))
                            {
                                file = common.ReadAllBytes(st);
                            }
                            if (file == null)
                            {
                                using (Stream st = appendFileReaderCallback?.Invoke(rymBuf2))
                                {
                                    file = common.ReadAllBytes(st);
                                }
                            }
                        }
                    }

                    //読み込みができなかった場合は次のファイルの読み込みに挑戦する
                    if (file == null)
                    {
                        errMsg += string.Format(
                            "Failed to load 2608_{0}.wav / 2608_{0}_{1}.wav ... \r\n",
                            rhythmname[i], chipID);
                        continue;
                    }

                    whdr whdr = new whdr();

                    uint fInd = 0x10;
                    byte[] bufWhdr = new byte[4 + 2 + 2 + 4 + 4 + 2 + 2 + 2];
                    for (int j = 0; j < 4 + 2 + 2 + 4 + 4 + 2 + 2 + 2; j++) bufWhdr[j] = file[fInd++];

                    whdr.chunksize = (uint)(bufWhdr[0] + bufWhdr[1] * 0x100 + bufWhdr[2] * 0x10000 + bufWhdr[3] * 0x10000);
                    whdr.tag = (uint)(bufWhdr[4] + bufWhdr[5] * 0x100);
                    whdr.nch = (uint)(bufWhdr[6] + bufWhdr[7] * 0x100);
                    whdr.rate = (uint)(bufWhdr[8] + bufWhdr[9] * 0x100 + bufWhdr[10] * 0x10000 + bufWhdr[11] * 0x10000);
                    whdr.avgbytes = (uint)(bufWhdr[12] + bufWhdr[13] * 0x100 + bufWhdr[14] * 0x10000 + bufWhdr[15] * 0x10000);
                    whdr.align = (uint)(bufWhdr[16] + bufWhdr[17] * 0x100);
                    whdr.bps = (uint)(bufWhdr[18] + bufWhdr[19] * 0x100);
                    whdr.size = (uint)(bufWhdr[20] + bufWhdr[21] * 0x100);

                    byte[] subchunkname = new byte[4];
                    fsize = 4 + whdr.chunksize - (4 + 2 + 2 + 4 + 4 + 2 + 2 + 2);
                    do
                    {
                        fInd += fsize;
                        for (int j = 0; j < 4; j++) subchunkname[j] = file[fInd++];
                        for (int j = 0; j < 4; j++) bufWhdr[j] = file[fInd++];

                        fsize = (uint)(bufWhdr[0] + bufWhdr[1] * 0x100 + bufWhdr[2] * 0x10000 + bufWhdr[3] * 0x10000);
                    } while ('d' != subchunkname[0] || 'a' != subchunkname[1] || 't' != subchunkname[2] || 'a' != subchunkname[3]);

                    fsize /= 2;
                    if (fsize >= 0x100000 || whdr.tag != 1 || whdr.nch != 1)
                        break;
                    fsize = (uint)Math.Max(fsize, (1 << 31) / 1024);

                    rhythm[i].sample = null;
                    rhythm[i].sample = new int[fsize];
                    if (rhythm[i].sample == null)
                        break;
                    byte[] bufSample = new byte[fsize * 2];
                    for (int j = 0; j < fsize * 2; j++) bufSample[j] = file[fInd++];
                    for (int si = 0; si < fsize; si++)
                    {
                        rhythm[i].sample[si] = (short)(bufSample[si * 2] + bufSample[si * 2 + 1] * 0x100);
                    }

                    rhythm[i].rate = whdr.rate;
                    rhythm[i].step = rhythm[i].rate * 1024 / rate;
                    rhythm[i].pos = rhythm[i].size = fsize * 1024;
                }
                catch (Exception e)
                {
                    errMsg += string.Format(
                        "ExceptionMessage:{0} stacktrace:{1} \r\n",
                        e.Message, e.StackTrace);
                }
            }
            if (i != 6)
            {
                for (i = 0; i < 6; i++)
                {
                    rhythm[i].sample = null;
                }
                return false;
            }
            return true;
        }

        // ---------------------------------------------------------------------------
        //	音量設定
        //
        public void SetVolumeRhythmTotal(int db)
        {
            db = Math.Min(db, 20);
            rhythmtvol = -(db * 2 / 3);
        }

        public void SetVolumeRhythm(int index, int db)
        {
            db = Math.Min(db, 20);
            rhythm[index].volume = -(db * 2 / 3);
        }

        public new void SetTimerA(uint addr, uint data)
        {
            base.SetTimerA(addr, data);
        }

        public new void SetTimerB(uint data)
        {
            base.SetTimerB(data);
        }

        public new void SetTimerControl(uint data)
        {
            base.SetTimerControl(data);
        }

    };
}
