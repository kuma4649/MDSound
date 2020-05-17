using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MDSound
{
    public class common
    {
        public static Int32 SampleRate = 44100;
        public static Int32 NsfClock = 1789773;

        public static void write(string fmt,params object[] arg)
        {
#if DEBUG
            string msg = string.Format(fmt, arg);
            using(var writer=new StreamWriter("log.txt", true))
            {
                writer.WriteLine(msg);
            }
#endif
        }
    }
}
