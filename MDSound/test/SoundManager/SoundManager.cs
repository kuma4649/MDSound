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
        /// ミュージックデータ解析
        ///     処理周期 : 無し
        ///     データ受取時 : DataSenderへ即送信
        ///                  DataSenderが受け取ることができない状態の場合は、待ち合わせする
        /// </summary>
        private DataMaker dataMaker;

        /// <summary>
        /// データ送信
        ///     処理周期 : 44100Hz(Default)
        ///     SeqCounter値に合わせて各ChipSenderへデータを振り分けながら送信。
        ///     ChipSenderが受け取ることができない状態の場合は、待ち合わせする
        /// </summary>
        private DataSender dataSender;

        /// <summary>
        /// エミュチップ専門データ送信
        ///     処理周期 : 無し
        ///     データが来たら、エミュレーションむけリングバッファにEnqueue
        ///     Enqueueできない場合は、待ち合わせする
        /// </summary>
        private EmuChipSender emuChipSender;

        /// <summary>
        /// 実チップ専門データ送信
        ///     処理周期 : 無し
        ///     データが来たら、実チップ向けコールバックを実施
        ///     待ち合わせ無し
        /// </summary>
        private RealChipSender realChipSender;

        /// <summary>
        /// 割り込み処理カウンタ
        /// 割り込みが発生している(1以上の)間、DataSenderは各チップへデータを送信しない
        /// </summary>
        private int interruptCounter = 0;

        private volatile object lockObj = new object();


        /// <summary>
        /// セットアップ
        /// </summary>
        /// <param name="DriverAction">ミュージックドライバーの1フレームあたりの処理を指定してください</param>
        /// <param name="RealChipAction">実チップ向けデータ送信処理を指定してください</param>
        /// <param name="startData">DataSenderが初期化を行うときに出力するデータを指定してください</param>
        /// <param name="stopData">DataSenderが演奏停止を行うときに出力するデータを指定してください</param>
        public void Setup(DriverAction DriverAction, Snd RealChipAction, Pack[] startData, Pack[] stopData)
        {
            dataMaker = new DataMaker(DriverAction);
            emuChipSender = new EmuChipSender(DATA_SEQUENCE_FREQUENCE);
            realChipSender = new RealChipSender(RealChipAction, DATA_SEQUENCE_FREQUENCE);
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

        public bool IsRunningAtDataSender()
        {
            return dataSender.IsRunning();
        }

        public bool IsRunningAtRealChipSender()
        {
            return realChipSender.IsRunning();
        }

        public long GetDriverSeqCounterDelay()
        {
            return (long)(DATA_SEQUENCE_FREQUENCE * 0.1);
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

        public long GetDataSenderBufferCounter()
        {
            return dataSender.GetRingBufferCounter();
        }

        public long GetDataSenderBufferSize()
        {
            return dataSender.GetRingBufferSize();
        }

        public long GetEmuChipSenderBufferSize()
        {
            return emuChipSender.GetRingBufferSize();
        }

        public long GetRealChipSenderBufferSize()
        {
            return realChipSender.GetRingBufferSize();
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
