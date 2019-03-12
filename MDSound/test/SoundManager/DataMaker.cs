using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SoundManager
{
    /// <summary>
    /// データ生成器
    /// ミュージックドライバーを駆動させ、生成するデータをDataSenderに送る
    /// </summary>
    public class DataMaker : BaseMakerSender
    {
        private readonly DriverAction ActionOfDriver;

        public DataMaker(DriverAction ActionOfDriver)
        {
            action = Main;
            this.ActionOfDriver = ActionOfDriver;
        }

        private void Main()
        {
            try
            {
                while (true)
                {
                    while (true)
                    {
                        Thread.Sleep(100);
                        if (GetStart())
                        {
                            break;
                        }
                    }

                    lock (lockObj) isRunning = true;

                    ActionOfDriver?.Init?.Invoke();

                    while (true)
                    {
                        if (!GetStart()) break;
                        Thread.Sleep(0);
                        ActionOfDriver?.Main?.Invoke();
                    }

                    ActionOfDriver?.Final?.Invoke();

                    lock (lockObj) isRunning = false;

                }
            }
            catch
            {
                lock (lockObj)
                {
                    isRunning = false;
                    Start = false;
                }
            }
        }

    }

}
