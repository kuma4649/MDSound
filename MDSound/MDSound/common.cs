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

        /// <summary>
        /// ストリームから一括でバイナリを読み込む
        /// </summary>
        public static byte[] ReadAllBytes(Stream stream)
        {
            if (stream == null) return null;

            var buf = new byte[8192];
            using (var ms = new MemoryStream())
            {
                while (true)
                {
                    var r = stream.Read(buf, 0, buf.Length);
                    if (r < 1)
                    {
                        break;
                    }
                    ms.Write(buf, 0, r);
                }
                return ms.ToArray();
            }
        }

        public static UInt32 pow2_mask(UInt32 v)
        {
            if (v == 0)
                return 0;
            v--;
            v |= (v >> 1);
            v |= (v >> 2);
            v |= (v >> 4);
            v |= (v >> 8);
            v |= (v >> 16);
            return v;
        }

    }
}
