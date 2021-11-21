using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDSound
{
	public class ws_audio : Instrument
	{
		private const uint DefaultWSwanClockValue = 3072000;
		private wsa_state[] chip = new wsa_state[2] { new wsa_state(DefaultWSwanClockValue), new wsa_state(DefaultWSwanClockValue) };
		private uint sampleRate = 44100;
		private uint masterClock = DefaultWSwanClockValue;
		private double sampleCounter = 0;
		private int[][] frm = new int[2][] { new int[1], new int[1] };
		private int[][] before = new int[2][] { new int[1], new int[1] };

		public override string Name { get { return "WonderSwan"; } set { } }
		public override string ShortName { get { return "WSwan"; } set { } }

		public override void Reset(byte ChipID)
		{
			ws_audio_reset(chip[ChipID]);
		}

		public override uint Start(byte ChipID, uint clock)
		{
			return Start(ChipID, clock, DefaultWSwanClockValue, null);
		}

		public override uint Start(byte ChipID, uint clock, uint ClockValue, params object[] option)
		{
			chip[ChipID] = ws_audio_init(clock, ClockValue);
			sampleRate = clock;
			masterClock = ClockValue;

			visVolume = new int[2][][];
			visVolume[0] = new int[2][];
			visVolume[1] = new int[2][];
			visVolume[0][0] = new int[2];
			visVolume[1][0] = new int[2];
			visVolume[0][1] = new int[2];
			visVolume[1][1] = new int[2];

			return clock;
		}

		public override void Stop(byte ChipID)
		{
			ws_audio_done(chip[ChipID]);
		}

		public override void Update(byte ChipID, int[][] outputs, int samples)
		{
			for (int i = 0; i < samples; i++)
			{
				outputs[0][i] = 0;
				outputs[1][i] = 0;

				sampleCounter += (masterClock / 128.0) / sampleRate;
				int upc = (int)sampleCounter;
				while (sampleCounter >= 1)
				{
					ws_audio_update(chip[ChipID], (uint)1, frm);

					outputs[0][i] += frm[0][0];
					outputs[1][i] += frm[1][0];

					sampleCounter -= 1.0;
				}

				if (upc != 0)
				{
					outputs[0][i] /= upc;
					outputs[1][i] /= upc;
					before[0][i] = outputs[0][i];
					before[1][i] = outputs[1][i];
				}
				else
				{
					outputs[0][i] = before[0][i];
					outputs[1][i] = before[1][i];
				}

				outputs[0][i] <<= 2;
				outputs[1][i] <<= 2;
			}

			visVolume[ChipID][0][0] = outputs[0][0];
			visVolume[ChipID][0][1] = outputs[1][0];
		}

		public override int Write(byte ChipID, int port, int adr, int data)
		{
			ws_audio_port_write(chip[ChipID], (byte)(adr + 0x80), (byte)data);
			return 0;
		}

		public int WriteMem(byte ChipID, int adr, int data)
		{
			ws_write_ram_byte(chip[ChipID], (ushort)adr, (byte)data);
			return 0;
		}

		public void SetMute(byte chipID, int v)
		{
		}






		//ws_initialio.h

		////////////////////////////////////////////////////////////////////////////////
		// Initial I/O values
		////////////////////////////////////////////////////////////////////////////////
		//
		//
		//
		//
		//
		//
		//////////////////////////////////////////////////////////////////////////////

		private byte[] initialIoValue = new byte[256]
		{
	0x00,//0
	0x00,//1
	0x9d,//2
	0xbb,//3
	0x00,//4
	0x00,//5
	0x00,//6
	0x26,//7
	0xfe,//8
	0xde,//9
	0xf9,//a
	0xfb,//b
	0xdb,//c
	0xd7,//d
	0x7f,//e
	0xf5,//f
	0x00,//10
	0x00,//11
	0x00,//12
	0x00,//13
	0x01,//14
	0x00,//15
	0x9e,//16
	0x9b,//17
	0x00,//18
	0x00,//19
	0x00,//1a
	0x00,//1b
	0x99,//1c
	0xfd,//1d
	0xb7,//1e
	0xdf,//1f
	0x30,//20
	0x57,//21
	0x75,//22
	0x76,//23
	0x15,//24
	0x73,//25
	0x77,//26
	0x77,//27
	0x20,//28
	0x75,//29
	0x50,//2a
	0x36,//2b
	0x70,//2c
	0x67,//2d
	0x50,//2e
	0x77,//2f
	0x57,//30
	0x54,//31
	0x75,//32
	0x77,//33
	0x75,//34
	0x17,//35
	0x37,//36
	0x73,//37
	0x50,//38
	0x57,//39
	0x60,//3a
	0x77,//3b
	0x70,//3c
	0x77,//3d
	0x10,//3e
	0x73,//3f
	0x00,//40
	0x00,//41
	0x00,//42
	0x00,//43
	0x00,//44
	0x00,//45
	0x00,//46
	0x00,//47
	0x00,//48
	0x00,//49
	0x00,//4a
	0x00,//4b
	0x00,//4c
	0x00,//4d
	0x00,//4e
	0x00,//4f
	0x00,//50
	0x00,//51
	0x00,//52
	0x00,//53
	0x00,//54
	0x00,//55
	0x00,//56
	0x00,//57
	0x00,//58
	0x00,//59
	0x00,//5a
	0x00,//5b
	0x00,//5c
	0x00,//5d
	0x00,//5e
	0x00,//5f
	0x0a,//60
	0x00,//61
	0x00,//62
	0x00,//63
	0x00,//64
	0x00,//65
	0x00,//66
	0x00,//67
	0x00,//68
	0x00,//69
	0x00,//6a
	0x0f,//6b
	0x00,//6c
	0x00,//6d
	0x00,//6e
	0x00,//6f
	0x00,//70
	0x00,//71
	0x00,//72
	0x00,//73
	0x00,//74
	0x00,//75
	0x00,//76
	0x00,//77
	0x00,//78
	0x00,//79
	0x00,//7a
	0x00,//7b
	0x00,//7c
	0x00,//7d
	0x00,//7e
	0x00,//7f
	0xFF,//80
	0x07,//81
	0xFF,//82
	0x07,//83
	0xFF,//84
	0x07,//85
	0xFF,//86
	0x07,//87
	0x00,//88
	0x00,//89
	0x00,//8a
	0x00,//8b
	0x00,//8c
	0x1f,//8d 1d ?
	0x00,//8e
	0x00,//8f
	0x00,//90
	0x00,//91
	0x00,//92
	0x00,//93
	0x00,//94
	0x00,//95
	0x00,//96
	0x00,//97
	0x00,//98
	0x00,//99
	0x00,//9a
	0x00,//9b
	0x00,//9c
	0x00,//9d
	0x03,//9e
	0x00,//9f
	0x87-2,//a0
	0x00,//a1
	0x00,//a2
	0x00,//a3
	0x0,//a4 2b
	0x0,//a5 7f
	0x4f,//a6
	0xff,//a7 cf ?
	0x00,//a8
	0x00,//a9
	0x00,//aa
	0x00,//ab
	0x00,//ac
	0x00,//ad
	0x00,//ae
	0x00,//af
	0x00,//b0
	0xdb,//b1
	0x00,//b2
	0x00,//b3
	0x00,//b4
	0x40,//b5
	0x00,//b6
	0x00,//b7
	0x00,//b8
	0x00,//b9
	0x01,//ba
	0x00,//bb
	0x42,//bc
	0x00,//bd
	0x83,//be
	0x00,//bf
	0x2f,//c0
	0x3f,//c1
	0xff,//c2
	0xff,//c3
	0x00,//c4
	0x00,//c5
	0x00,//c6
	0x00,//c7

	0xd1,//c8?
	0xd1,//c9
	0xd1,//ca
	0xd1,//cb
	0xd1,//cc
	0xd1,//cd
	0xd1,//ce
	0xd1,//cf
	0xd1,//d0
	0xd1,//d1
	0xd1,//d2
	0xd1,//d3
	0xd1,//d4
	0xd1,//d5
	0xd1,//d6
	0xd1,//d7
	0xd1,//d8
	0xd1,//d9
	0xd1,//da
	0xd1,//db
	0xd1,//dc
	0xd1,//dd
	0xd1,//de
	0xd1,//df
	0xd1,//e0
	0xd1,//e1
	0xd1,//e2
	0xd1,//e3
	0xd1,//e4
	0xd1,//e5
	0xd1,//e6
	0xd1,//e7
	0xd1,//e8
	0xd1,//e9
	0xd1,//ea
	0xd1,//eb
	0xd1,//ec
	0xd1,//ed
	0xd1,//ee
	0xd1,//ef
	0xd1,//f0
	0xd1,//f1
	0xd1,//f2
	0xd1,//f3
	0xd1,//f4
	0xd1,//f5
	0xd1,//f6
	0xd1,//f7
	0xd1,//f8
	0xd1,//f9
	0xd1,//fa
	0xd1,//fb
	0xd1,//fc
	0xd1,//fd
	0xd1,//fe
	0xd1 //ff
		};






		//ws_audio.c

		//# include <stdlib.h>
		//# include <string.h>	// for memset
		//# include <stddef.h>	// for NULL

		//# include "../../stdtype.h"
		//# include "../EmuStructs.h"
		//# include "../EmuCores.h"
		//# include "../snddef.h"
		//# include "../EmuHelper.h"
		//# include "../RatioCntr.h"
		//# include "ws_audio.h"


		//		typedef struct _wsa_state wsa_state;

		//static UINT8 ws_audio_init(const DEV_GEN_CFG* cfg, DEV_INFO* retDevInf);
		//static void ws_audio_reset(void* info);
		//		static void ws_audio_done(void* info);
		//		static void ws_audio_update(void* info, UINT32 length, DEV_SMPL** buffer);
		//		static void ws_audio_port_write(void* info, UINT8 port, UINT8 value);
		//		static UINT8 ws_audio_port_read(void* info, UINT8 port);
		//		static void ws_audio_process(wsa_state* chip);
		//		static void ws_audio_sounddma(wsa_state* chip);
		//		static void ws_write_ram_byte(void* info, UINT16 offset, UINT8 value);
		//		static UINT8 ws_read_ram_byte(void* info, UINT16 offset);
		//		static void ws_set_mute_mask(void* info, UINT32 MuteMask);
		//		static UINT32 ws_get_mute_mask(void* info);


		//		static DEVDEF_RWFUNC devFunc[] =
		//		{
		//	{RWF_REGISTER | RWF_WRITE, DEVRW_A8D8, 0, ws_audio_port_write},
		//	{RWF_REGISTER | RWF_READ, DEVRW_A8D8, 0, ws_audio_port_read},
		//	{RWF_MEMORY | RWF_WRITE, DEVRW_A16D8, 0, ws_write_ram_byte},
		//	{RWF_MEMORY | RWF_READ, DEVRW_A16D8, 0, ws_read_ram_byte},
		//	//{RWF_MEMORY | RWF_WRITE, DEVRW_BLOCK, 0, ws_write_ram_block},
		//	{RWF_CHN_MUTE | RWF_WRITE, DEVRW_ALL, 0, ws_set_mute_mask},
		//	{0x00, 0x00, 0, NULL}
		//};
		//		static DEV_DEF devDef =
		//		{
		//	"WonderSwan", "in_wsr", 0x00000000,

		//	ws_audio_init,
		//	ws_audio_done,
		//	ws_audio_reset,
		//	ws_audio_update,

		//	NULL,	// SetOptionBits
		//	ws_set_mute_mask,
		//	NULL,	// SetPanning
		//	NULL,	// SetSampleRateChangeCallback
		//	NULL,	// LinkDevice

		//	devFunc,	// rwFuncs
		//};

		//		const DEV_DEF* devDefList_WSwan[] =
		//		{
		//	&devDef,
		//	NULL
		//};


		//		typedef UINT8   byte;
		//#include "ws_initialIo.h"

		private enum wsIORam
		{
			SNDP = 0x80 //#define SNDP	chip->ws_ioRam[0x80]
			, SNDV = 0x88 //#define SNDV	chip->ws_ioRam[0x88]
			, SNDSWP = 0x8C //#define SNDSWP	chip->ws_ioRam[0x8C]
			, SWPSTP = 0x8D //#define SWPSTP	chip->ws_ioRam[0x8D]
			, NSCTL = 0x8E //#define NSCTL	chip->ws_ioRam[0x8E]
			, WAVDTP = 0x8F //#define WAVDTP	chip->ws_ioRam[0x8F]
			, SNDMOD = 0x90 //#define SNDMOD	chip->ws_ioRam[0x90]
			, SNDOUT = 0x91 //#define SNDOUT	chip->ws_ioRam[0x91]
			, PCSRL = 0x92 //#define PCSRL	chip->ws_ioRam[0x92]
			, PCSRH = 0x93 //#define PCSRH	chip->ws_ioRam[0x93]
			, DMASL = 0x40 //#define DMASL	chip->ws_ioRam[0x40]
			, DMASH = 0x41 //#define DMASH	chip->ws_ioRam[0x41]
			, DMASB = 0x42 //#define DMASB	chip->ws_ioRam[0x42]
			, DMADB = 0x43 //#define DMADB	chip->ws_ioRam[0x43]
			, DMADL = 0x44 //#define DMADL	chip->ws_ioRam[0x44]
			, DMADH = 0x45 //#define DMADH	chip->ws_ioRam[0x45]
			, DMACL = 0x46 //#define DMACL	chip->ws_ioRam[0x46]
			, DMACH = 0x47 //#define DMACH	chip->ws_ioRam[0x47]
			, DMACTL = 0x48 //#define DMACTL	chip->ws_ioRam[0x48]
			, SDMASL = 0x4A //#define SDMASL	chip->ws_ioRam[0x4A]
			, SDMASH = 0x4B //#define SDMASH	chip->ws_ioRam[0x4B]
			, SDMASB = 0x4C //#define SDMASB	chip->ws_ioRam[0x4C]
			, SDMACL = 0x4E //#define SDMACL	chip->ws_ioRam[0x4E]
			, SDMACH = 0x4F //#define SDMACH	chip->ws_ioRam[0x4F]
			, SDMACTL = 0x52 //#define SDMACTL	chip->ws_ioRam[0x52]
		}

		////SoundDMA の転送間隔
		//// 実際の数値が分からないので、予想です
		//// サンプリング周期から考えてみて以下のようにした
		//// 12KHz = 1.00HBlank = 256cycles間隔
		//// 16KHz = 0.75HBlank = 192cycles間隔
		//// 20KHz = 0.60HBlank = 154cycles間隔
		//// 24KHz = 0.50HBlank = 128cycles間隔
		private static ushort[] DMACycles = new ushort[4] { 256, 192, 154, 128 };

		private class WS_AUDIO
		{
			public ushort wave;
			public byte lvol;
			public byte rvol;
			public uint offset;
			public uint delta;
			public byte pos;
			public byte Muted;
		}
		//	WS_AUDIO;

		private class RATIO_CNTR
		{
			public ulong inc;    // counter increment
			public ulong val;    // current value
		}
		//RATIO_CNTR;
		private void RC_SET_RATIO(ref RATIO_CNTR rc, uint mul, uint div)
		{
			rc.inc = (ulong)((((ulong)mul << 20) + div / 2) / div);//RC_SHIFT=20
		}
		private void RC_STEP(ref RATIO_CNTR rc)
		{
			rc.val += rc.inc;
		}

		private void RC_RESET(ref RATIO_CNTR rc)
		{
			rc.val = 0;
		}

		private void RC_RESET_PRESTEP(ref RATIO_CNTR rc)
		{
			rc.val = ((ulong)1 << 20) - rc.inc;
		}

		private uint RC_GET_VAL(ref RATIO_CNTR rc)
		{
			return (uint)(rc.val >> 20);
		}

		private void RC_MASK(ref RATIO_CNTR rc)
		{
			rc.val &= (((ulong)1 << 20) - 1);
		}

		private class wsa_state
		{
			//		DEV_DATA _devData;

			public WS_AUDIO[] ws_audio = new WS_AUDIO[4] { new WS_AUDIO(), new WS_AUDIO(), new WS_AUDIO(), new WS_AUDIO() };
			public RATIO_CNTR HBlankTmr = new RATIO_CNTR();
			public short SweepTime;
			public sbyte SweepStep;
			public short SweepCount;
			public ushort SweepFreq;
			public byte NoiseType;
			public uint NoiseRng;
			public ushort MainVolume;
			public byte PCMVolumeLeft;
			public byte PCMVolumeRight;

			public byte[] ws_ioRam = new byte[0x100];
			public byte[] ws_internalRam;

			public uint clock = DEFAULT_CLOCK;
			public uint smplrate = DEFAULT_CLOCK / 128;
			public float ratemul;

			public wsa_state(uint masterClock)
            {
				clock = masterClock;
				smplrate = clock / 128;
            }
		};

		private const uint DEFAULT_CLOCK = 3072000;


		private wsa_state ws_audio_init(uint sampleRate,uint masterClock)//DEV_GEN_CFG cfg, DEV_INFO retDevInf)
		{
			wsa_state chip;

			chip = new wsa_state(masterClock);// (wsa_state)calloc(1, sizeof(wsa_state));

			// actual size is 64 KB, but the audio chip can only access 16 KB
			chip.ws_internalRam = new byte[0x4000];// (UINT8*)malloc(0x4000);

			//chip.clock = cfg.clock;
			//// According to http://daifukkat.su/docs/wsman/, the headphone DAC update is (clock / 128)
			//// and sound channels update during every master clock cycle. (clock / 1)
			//chip.smplrate = cfg.clock / 128;

			////SRATE_CUSTOM_HIGHEST(cfg.srMode, chip.smplrate, cfg.smplRate);
			//if (cfg.srMode == 0x01//DEVRI_SRMODE_CUSTOM 
			//	|| (cfg.srMode == 0x01//DEVRI_SRMODE_HIGHEST
			//						   && chip.smplrate < cfg.smplRate)
			//	) chip.smplrate = cfg.smplRate;

			chip.ratemul = (float)chip.clock * 65536.0f / (float)chip.smplrate;
			// one step every 256 cycles
			RC_SET_RATIO(ref chip.HBlankTmr, chip.clock, chip.smplrate * 256);

			ws_set_mute_mask(chip, 0x00);

			//chip._devData.chipInf = chip;
			//INIT_DEVINF(retDevInf, chip._devData, chip.smplrate, devDef);

			return chip;
		}

		private void ws_audio_reset(wsa_state info)
		{
			wsa_state chip = (wsa_state)info;
			uint muteMask;
			int i;

			muteMask = ws_get_mute_mask(chip);
			chip.ws_audio = new WS_AUDIO[4] { new WS_AUDIO(), new WS_AUDIO(), new WS_AUDIO(), new WS_AUDIO() };// memset(&chip->ws_audio, 0, sizeof(WS_AUDIO));
			ws_set_mute_mask(chip, muteMask);

			chip.SweepTime = 0;
			chip.SweepStep = 0;
			chip.NoiseType = 0;
			chip.NoiseRng = 1;
			chip.MainVolume = 0x02;    // 0x04
			chip.PCMVolumeLeft = 0;
			chip.PCMVolumeRight = 0;

			RC_RESET(ref chip.HBlankTmr);

			for (i = 0x80; i < 0xc9; i++)
				ws_audio_port_write(chip, (byte)i, initialIoValue[i]);
		}

		private void ws_audio_done(wsa_state info)
		{
			wsa_state chip = (wsa_state)info;

			//free(chip->ws_internalRam);
			//free(chip);

			return;
		}

		//OSWANの擬似乱数の処理と同等のつもり
		//#define BIT(n) (1<<n)
		private uint[] noise_mask = new uint[8]
		{
								0b11,//BIT(0)|BIT(1),
                                0b110011,//BIT(0)|BIT(1)|BIT(4)|BIT(5),
                                0b11011,//BIT(0)|BIT(1)|BIT(3)|BIT(4),
                                0b1010011,//BIT(0)|BIT(1)|BIT(4)|BIT(6),
                                0b101,//BIT(0)|BIT(2),
                                0b1001,//BIT(0)|BIT(3),
                                0b10001,//BIT(0)|BIT(4),
                                0b11101//BIT(0)|BIT(2)|BIT(3)|BIT(4)
		};

		private uint[] noise_bit = new uint[8]
		{
								0b1000_0000_0000_0000,//BIT(15),
                                0b0100_0000_0000_0000,//BIT(14),
                                0b0010_0000_0000_0000,//BIT(13),
                                0b0001_0000_0000_0000,//BIT(12),
                                0b0000_1000_0000_0000,//BIT(11),
                                0b0000_0100_0000_0000,//BIT(10),
                                0b0000_0010_0000_0000,//BIT(9),
                                0b0000_0001_0000_0000//BIT(8)
		};

		private void ws_audio_update(wsa_state info, uint length, int[][] buffer)
		{
			wsa_state chip = (wsa_state)info;
			int[] bufL;
			int[] bufR;
			uint i;
			byte ch, cnt;
			short w;    // could fit into INT8
			int l, r;

			bufL = buffer[0];
			bufR = buffer[1];
			for (i = 0; i < length; i++)
			{
				uint swpCount;

				RC_STEP(ref chip.HBlankTmr);
				for (swpCount = RC_GET_VAL(ref chip.HBlankTmr); swpCount > 0; swpCount--)
					ws_audio_process(chip);
				RC_MASK(ref chip.HBlankTmr);

				l = r = 0;

				for (ch = 0; ch < 4; ch++)
				{
					if (chip.ws_audio[ch].Muted != 0)
						continue;

					if ((ch == 1) && ((chip.ws_ioRam[(int)wsIORam.SNDMOD] & 0x20) != 0))
					{
						// Voice出力
						w = chip.ws_ioRam[0x89];
						w -= 0x80;
						l += chip.PCMVolumeLeft * w;
						r += chip.PCMVolumeRight * w;
					}
					else if ((chip.ws_ioRam[(int)wsIORam.SNDMOD] & (1 << ch)) != 0)
					{
						if ((ch == 3) && ((chip.ws_ioRam[(int)wsIORam.SNDMOD] & 0x80) != 0))
						{
							//Noise

							uint Masked, XorReg;

							chip.ws_audio[ch].offset += chip.ws_audio[ch].delta;
							cnt = (byte)(chip.ws_audio[ch].offset >> 16);
							chip.ws_audio[ch].offset &= 0xffff;
							while (cnt > 0)
							{
								cnt--;

								chip.NoiseRng &= noise_bit[chip.NoiseType] - 1;
								if (chip.NoiseRng == 0) chip.NoiseRng = noise_bit[chip.NoiseType] - 1;

								Masked = chip.NoiseRng & noise_mask[chip.NoiseType];
								XorReg = 0;
								while (Masked != 0)
								{
									XorReg ^= Masked & 1;
									Masked >>= 1;
								}
								if (XorReg != 0)
									chip.NoiseRng |= noise_bit[chip.NoiseType];
								chip.NoiseRng >>= 1;
							}

							chip.ws_ioRam[(int)wsIORam.PCSRL] = (byte)(chip.NoiseRng & 0xff);
							chip.ws_ioRam[(int)wsIORam.PCSRH] = (byte)((chip.NoiseRng >> 8) & 0x7f);

							w = (short)((chip.NoiseRng & 1) != 0 ? 0x7f : -0x80);
							l += chip.ws_audio[ch].lvol * w;
							r += chip.ws_audio[ch].rvol * w;
						}
						else
						{
							chip.ws_audio[ch].offset += chip.ws_audio[ch].delta;
							cnt = (byte)(chip.ws_audio[ch].offset >> 16);
							chip.ws_audio[ch].offset &= 0xffff;
							chip.ws_audio[ch].pos += cnt;
							chip.ws_audio[ch].pos &= 0x1f;
							w = chip.ws_internalRam[(chip.ws_audio[ch].wave & 0xFFF0) + (chip.ws_audio[ch].pos >> 1)];
							if ((chip.ws_audio[ch].pos & 1) == 0)
								w = (short)((w << 4) & 0xf0);    //下位ニブル
							else
								w = (short)(w & 0xf0);           //上位ニブル
							w -= 0x80;
							l += chip.ws_audio[ch].lvol * w;
							r += chip.ws_audio[ch].rvol * w;
						}
					}
				}

				bufL[i] = l * chip.MainVolume;
				bufR[i] = r * chip.MainVolume;
			}
		}

		static void ws_audio_port_write(wsa_state info, byte port, byte value)
		{
			wsa_state chip = (wsa_state)info;
			ushort i;
			float freq;

			chip.ws_ioRam[port] = value;

			switch (port)
			{
				// 0x80-0x87の周波数レジスタについて
				// - ロックマン&フォルテの0x0fの曲では、周波数=0xFFFF の音が不要
				// - デジモンディープロジェクトの0x0dの曲のノイズは 周波数=0x07FF で音を出す
				// →つまり、0xFFFF の時だけ音を出さないってことだろうか。
				//   でも、0x07FF の時も音を出さないけど、ノイズだけ音を出すのかも。
				case 0x80:
				case 0x81:
					i = (ushort)((((ushort)chip.ws_ioRam[0x81]) << 8) + ((ushort)chip.ws_ioRam[0x80]));
					if (i == 0xffff)
						freq = 0;
					else
						freq = 1.0f / (2048 - (i & 0x7ff));
					chip.ws_audio[0].delta = (uint)(freq * chip.ratemul);
					break;
				case 0x82:
				case 0x83:
					i = (ushort)((((ushort)chip.ws_ioRam[0x83]) << 8) + ((ushort)chip.ws_ioRam[0x82]));
					if (i == 0xffff)
						freq = 0;
					else
						freq = 1.0f / (2048 - (i & 0x7ff));
					chip.ws_audio[1].delta = (uint)(freq * chip.ratemul);
					break;
				case 0x84:
				case 0x85:
					i = (ushort)((((ushort)chip.ws_ioRam[0x85]) << 8) + ((ushort)chip.ws_ioRam[0x84]));
					chip.SweepFreq = i;
					if (i == 0xffff)
						freq = 0;
					else
						freq = 1.0f / (2048 - (i & 0x7ff));
					chip.ws_audio[2].delta = (uint)(freq * chip.ratemul);
					break;
				case 0x86:
				case 0x87:
					i = (ushort)((((ushort)chip.ws_ioRam[0x87]) << 8) + ((ushort)chip.ws_ioRam[0x86]));
					if (i == 0xffff)
						freq = 0;
					else
						freq = 1.0f / (2048 - (i & 0x7ff));
					chip.ws_audio[3].delta = (uint)(freq * chip.ratemul);
					break;
				case 0x88:
					chip.ws_audio[0].lvol = (byte)((value >> 4) & 0xf);
					chip.ws_audio[0].rvol = (byte)(value & 0xf);
					break;
				case 0x89:
					chip.ws_audio[1].lvol = (byte)((value >> 4) & 0xf);
					chip.ws_audio[1].rvol = (byte)(value & 0xf);
					break;
				case 0x8A:
					chip.ws_audio[2].lvol = (byte)((value >> 4) & 0xf);
					chip.ws_audio[2].rvol = (byte)(value & 0xf);
					break;
				case 0x8B:
					chip.ws_audio[3].lvol = (byte)((value >> 4) & 0xf);
					chip.ws_audio[3].rvol = (byte)(value & 0xf);
					break;
				case 0x8C:
					chip.SweepStep = (sbyte)value;
					break;
				case 0x8D:
					//Sweepの間隔は 1/375[s] = 2.666..[ms]
					//CPU Clockで言うと 3072000/375 = 8192[cycles]
					//ここの設定値をnとすると、8192[cycles]*(n+1) 間隔でSweepすることになる
					//
					//これを HBlank (256cycles) の間隔で言うと、
					//　8192/256 = 32
					//なので、32[HBlank]*(n+1) 間隔となる
					chip.SweepTime = (short)((((short)value) + 1) << 5);
					chip.SweepCount = chip.SweepTime;
					break;
				case 0x8E:
					chip.NoiseType = (byte)(value & 7);
					if ((value & 8) != 0) chip.NoiseRng = 1;  //ノイズカウンターリセット
					break;
				case 0x8F:
					chip.ws_audio[0].wave = (ushort)(value << 6);
					chip.ws_audio[1].wave = (ushort)(chip.ws_audio[0].wave + 0x10);
					chip.ws_audio[2].wave = (ushort)(chip.ws_audio[1].wave + 0x10);
					chip.ws_audio[3].wave = (ushort)(chip.ws_audio[2].wave + 0x10);
					break;
				case 0x90://SNDMOD
					break;
				case 0x91:
					//ここでのボリューム調整は、内蔵Speakerに対しての調整だけらしいので、
					//ヘッドフォン接続されていると認識させれば問題無いらしい。
					chip.ws_ioRam[port] |= 0x80;
					break;
				case 0x92:
				case 0x93:
					break;
				case 0x94:
					chip.PCMVolumeLeft = (byte)((value & 0xc) * 2);
					chip.PCMVolumeRight = (byte)(((value << 2) & 0xc) * 2);
					break;
				case 0x52:
					//if (value&0x80)
					//	ws_timer_set(2, DMACycles[value&3]);
					break;
			}
		}

		private byte ws_audio_port_read(wsa_state info, byte port)
		{
			wsa_state chip = (wsa_state)info;
			return (chip.ws_ioRam[port]);
		}

		// HBlank間隔で呼ばれる
		// Note: Must be called every 256 cycles (3072000 Hz clock), i.e. at 12000 Hz
		private void ws_audio_process(wsa_state chip)
		{
			float freq;

			if (chip.SweepStep != 0 && (chip.ws_ioRam[(int)wsIORam.SNDMOD] & 0x40) != 0)
			{
				if (chip.SweepCount < 0)
				{
					chip.SweepCount = chip.SweepTime;
					chip.SweepFreq += (ushort)chip.SweepStep;
					chip.SweepFreq &= 0x7FF;

					freq = 1.0f / (2048 - chip.SweepFreq);
					chip.ws_audio[2].delta = (uint)(freq * chip.ratemul);
				}
				chip.SweepCount--;
			}
		}

		private void ws_audio_sounddma(wsa_state chip)
		{
			ushort i;
			uint j;
			byte b;

			if ((chip.ws_ioRam[(int)wsIORam.SDMACTL] & 0x88) == 0x80)
			{
				i = (ushort)((chip.ws_ioRam[(int)wsIORam.SDMACH] << 8) | chip.ws_ioRam[(int)wsIORam.SDMACL]);
				j = (uint)((chip.ws_ioRam[(int)wsIORam.SDMASB] << 16) | (chip.ws_ioRam[(int)wsIORam.SDMASH] << 8) | chip.ws_ioRam[(int)wsIORam.SDMASL]);
				//b=cpu_readmem20(j);
				b = chip.ws_internalRam[j & 0x3FFF];

				chip.ws_ioRam[0x89] = b;
				i--;
				j++;
				if (i < 32)
				{
					i = 0;
					chip.ws_ioRam[(int)wsIORam.SDMACTL] &= 0x7F;
				}
				else
				{
					// set DMA timer
					//ws_timer_set(2, DMACycles[SDMACTL&3]);
				}
				chip.ws_ioRam[(int)wsIORam.SDMASB] = (byte)((j >> 16) & 0xFF);
				chip.ws_ioRam[(int)wsIORam.SDMASH] = (byte)((j >> 8) & 0xFF);
				chip.ws_ioRam[(int)wsIORam.SDMASL] = (byte)(j & 0xFF);
				chip.ws_ioRam[(int)wsIORam.SDMACH] = (byte)((i >> 8) & 0xFF);
				chip.ws_ioRam[(int)wsIORam.SDMACL] = (byte)(i & 0xFF);
			}
		}

		private void ws_write_ram_byte(wsa_state info, ushort offset, byte value)
		{
			wsa_state chip = (wsa_state)info;

			// RAM - 16 KB (WS) / 64 KB (WSC) internal RAM
			chip.ws_internalRam[offset & 0x3FFF] = value;
			return;
		}

		private byte ws_read_ram_byte(wsa_state info, ushort offset)
		{
			wsa_state chip = (wsa_state)info;

			return chip.ws_internalRam[offset & 0x3FFF];
		}

		private void ws_set_mute_mask(wsa_state info, uint MuteMask)
		{
			wsa_state chip = (wsa_state)info;
			byte CurChn;

			for (CurChn = 0; CurChn < 4; CurChn++)
				chip.ws_audio[CurChn].Muted = (byte)((MuteMask >> CurChn) & 0x01);

			return;
		}

		private uint ws_get_mute_mask(wsa_state info)
		{
			wsa_state chip = (wsa_state)info;
			uint muteMask;
			byte CurChn;

			muteMask = 0x00;
			for (CurChn = 0; CurChn < 4; CurChn++)
				muteMask |= (uint)(chip.ws_audio[CurChn].Muted << CurChn);

			return muteMask;
		}

    }
}