using System;
using System.Collections.Generic;
using System.Text;

namespace MDSound
{
    public enum LogLevel : int
    {
        FATAL = 1
        , ERROR = 2
        , WARNING = 4
        , INFO = 8
        , DEBUG = 16
        , TRACE = 32
    }
}
