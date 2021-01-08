using System;
using System.Collections.Generic;
using System.Text;

namespace MDSound
{
    public static class Log
    {
        public static Action<LogLevel, string> writeLine = null;
        public static LogLevel level = LogLevel.INFO;
        public static int off = (int)LogLevel.WARNING;
        public static Action<string> writeMethod;

        public static void WriteLine(LogLevel level, string msg)
        {
            //if ((off & (int)level) != 0) return;
            return;
            if (level <= Log.level)
            {
                if (writeMethod != null)
                    writeMethod(String.Format("[{0,-7}] {1}", level, msg));
                else
                    writeLine?.Invoke(level, msg);
            }
        }
    }
}
