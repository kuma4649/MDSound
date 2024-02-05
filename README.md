# MDSound
メガドライブ サウンドチップエミュレーション.DLL
  
[概要]  
 このDLLは、VGM Playerのソースなどからメガドライブなどに搭載されている以下の音源チップの動作をC#向けコードに移植したものです。  
  FM音源  
    YM2612     OPN2  
	YM3438     OPN2(cmos)  
    YM2151(mame)     OPM  
    YM2151(FMGEN)    OPM  
    YM2151(X68sound) OPM  
    YM2203     OPN  
    YM2608     OPNA  
    YM2610/B   OPNB  
    YM2413     OPLL  
    YMF262     OPL3  
    YMF278B    OPL4  
  PCM音源  
    RF5C164    RF5C  
    PWM        PWM  
    C140       C140  
    OKIM6258   OKI65  
    MPCM(OKIM6258)   MPCM  
    OKIM6295   OKI69  
    SEGAPCM    SEGAPCM  
    C352       C352  
    K054539    K054  
    NES_DMC  
    PPZ8       PPZ8  
    PPSDRV     PPSDRV  
    PC-9801-86 P86  
  波形メモリ音源  
    HuC6280(FM的個所有)  HuC6  
    K051649    K051  
    NES_FDS(FM的個所有)  
  PSG音源  
    SN76489  
    AY8910  
    NES_APU  
  その他(仮想音源)  
    YM2609   OPNA2  
    AY8910-2 PSG2  
  
[機能、特徴]  
 ・割と.NETの文化に沿った記述が可能です。  
 ・マネージドなプログラムです。(testプログラムはSDLNETを使用していますが。)  
  
[著作権・免責]  
  MDSoundはフリーソフトです。著作権は作者が保有しています。  
  このソフトは無保証であり、このソフトを使用した事による  
  いかなる損害も作者は一切の責任を負いません。  
  ライセンスに関しては、GPLv3ライセンスに準ずるものとします。  
  
  MDSoundは、以下のソフトウェアのソースコードをC#向けに移植し使用しています。  
  これらのソースは各著作者が著作権を持ちます。  
  ライセンスに関しては、各ドキュメントを参照してください。  
  
 ・VGMPlay  
 ・MAME  
 ・Gens  
 ・Ootake  
 ・fmgen  
 ・NSFPlay  
 ・X68Sound.dll  
 ・TinyMPCM(仮)  
 ・Nuked-OPN2  
 ・PMDWin  
 ・うつぼかずら氏作フィルタ、エフェクター  
  
[SpecialThanks]  
 本ツールは以下の方々にお世話になっております。また以下のソフトウェア、ウェブページを参考、使用しています。  
 本当にありがとうございます。  
  
 ・Visual Studio Community 2015  
 ・SGDK  
 ・VGM Player  
 ・Nuked-OPN2  
 ・Git  
 ・SDL/SDLNET  
 ・SourceTree  
 ・さくらエディター  
 ・QUASI88のドキュメント  
  
 ・SMS Power!  
 ・DOBON.NET  
 ・C++でVST作り  
 ・Wikipedia  
  
