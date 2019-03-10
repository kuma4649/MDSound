using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace test
{
    public static class Common
    {
        public static string settingFilePath = "";
    }

    public enum EnmRealChipType : int
    {
        YM2608 = 1
        , YM2151 = 2
        , YM2610 = 3
        , YM2203 = 4
        , YM2612 = 5
        , SN76489 = 7
        , SPPCM = 42
        , C140 = 43
        , SEGAPCM = 44
    }
}
