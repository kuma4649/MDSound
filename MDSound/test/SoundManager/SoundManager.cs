using System;

namespace SoundManager
{
    public delegate bool Enq(long Counter, int Dev, int Typ, int Adr, int Val, object[] Ex);
    public delegate bool Deq(ref long Counter, ref int Dev, ref int Typ, ref int Adr, ref int Val, ref object[] Ex);
    public delegate void Snd(long Counter, int Dev, int Typ, int Adr, int Val, object[] Ex);

    public class SoundManager : IDisposable
    {

        public const int DATA_SEQUENCE_FREQUENCE = 44100;

        /// <summary>
        /// ミュージックデータ解析(周期無し。データが来たら即送信)
        /// </summary>
        private DataMaker dataMaker;

        /// <summary>
        /// データ送信(指定周期(Def.44100Hz)によるループ)
        /// </summary>
        private DataSender dataSender;

        /// <summary>
        /// エミュチップ専門データ送信(周期無し。データが来たら即送信)
        /// </summary>
        private EmuChipSender emuChipSender;

        /// <summary>
        /// 実チップ専門データ送信(周期無し。データが来たら即送信)
        /// </summary>
        private RealChipSender realChipSender;

        private DriverAction OfDriver;
        private Snd OfEmu;
        private Snd OfReal;

        private object lockObj = new object();
        private int interruptCounter = 0;


        public void Setup(DriverAction ActionOfDriver, Snd ActionOfEmuDevice, Snd ActionOfRealDevice, Pack[] startData, Pack[] stopData)
        {
            this.OfDriver = ActionOfDriver;
            this.OfEmu = ActionOfEmuDevice;
            this.OfReal = ActionOfRealDevice;

            dataMaker = new DataMaker(OfDriver);
            emuChipSender = new EmuChipSender(ActionOfEmuDevice, DATA_SEQUENCE_FREQUENCE);
            realChipSender = new RealChipSender(ActionOfRealDevice, DATA_SEQUENCE_FREQUENCE);
            dataSender = new DataSender(emuChipSender.Enq, realChipSender.Enq, startData, stopData);

            dataMaker.parent = this;
            emuChipSender.parent = this;
            realChipSender.parent = this;
            dataSender.parent = this;

            dataMaker.Mount();
            dataSender.Mount();
            emuChipSender.Mount();
            realChipSender.Mount();
        }

        public void Release()
        {
            dataMaker.Unmount();
            dataSender.Unmount();
            emuChipSender.Unmount();
            realChipSender.Unmount();
        }

        public void RequestStart()
        {
            dataSender.Init();

            dataMaker.RequestStart();
            while (!dataMaker.IsRunning()) ;
            dataSender.RequestStart();
            while (!dataSender.IsRunning()) ;

            emuChipSender.RequestStart();
            realChipSender.RequestStart();
        }

        public void RequestStop()
        {
            while (dataMaker.IsRunning()) dataMaker.RequestStop();
            while (dataSender.IsRunning()) dataSender.RequestStop();
            while (emuChipSender.IsRunning()) emuChipSender.RequestStop();
            while (realChipSender.IsRunning()) realChipSender.RequestStop();
        }

        public void RequestStopAtDataMaker()
        {
            dataMaker.RequestStop();
        }

        public void RequestStopAtEmuChipSender()
        {
            emuChipSender.RequestStop();
        }

        public void RequestStopAtRealChipSender()
        {
            realChipSender.RequestStop();
        }

        public bool IsRunningAtDataMaker()
        {
            return dataMaker.IsRunning();
        }

        public bool IsRunningAtRealChipSender()
        {
            return realChipSender.IsRunning();
        }

        public bool IsRunningAtEmuChipSender()
        {
            return emuChipSender.IsRunning();
        }

        /// <summary>
        /// DriverのデータをEnqueueするメソッドを取得する
        /// </summary>
        /// <returns></returns>
        public Enq GetDriverDataEnqueue()
        {
            return dataSender.Enq;
        }

        /// <summary>
        /// EmuのデータをDequeueするメソッドを取得する
        /// </summary>
        /// <returns></returns>
        public Deq GetEmuDataDequeue()
        {
            return emuChipSender.Deq;
        }

        /// <summary>
        /// RealのデータをDequeueするメソッドを取得する
        /// </summary>
        /// <returns></returns>
        public Deq GetRealDataDequeue()
        {
            return realChipSender.Deq;
        }

        public RingBuffer GetEmuRecvBuffer()
        {
            return emuChipSender.recvBuffer;
        }

        public bool IsRunningAsync()
        {
            if (dataMaker.IsRunning()) return true;
            if (dataSender.IsRunning()) return true;
            if (emuChipSender.IsRunning()) return true;
            if (realChipSender.IsRunning()) return true;

            return false;
        }

        public void SetInterrupt()
        {
            lock (lockObj)
            {
                interruptCounter++;
            }
        }

        public void ResetInterrupt()
        {
            lock (lockObj)
            {
                if (interruptCounter > 0) interruptCounter--;
            }
        }

        public bool GetInterrupt()
        {
            lock (lockObj)
            {
                return (interruptCounter > 0);
            }
        }

        public long GetSeqCounter()
        {
            return dataSender.GetSeqCounter();
        }

        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    dataMaker.Dispose();
                    dataSender.Dispose();
                    emuChipSender.Dispose();
                    realChipSender.Dispose();
                }

                // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // TODO: 大きなフィールドを null に設定します。

                disposedValue = true;
            }
        }

        // TODO: 上の Dispose(bool disposing) にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        // ~Player() {
        //   // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
        //   Dispose(false);
        // }

        // このコードは、破棄可能なパターンを正しく実装できるように追加されました。
        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            Dispose(true);
            // TODO: 上のファイナライザーがオーバーライドされる場合は、次の行のコメントを解除してください。
            // GC.SuppressFinalize(this);
        }

        #endregion
    }

}
