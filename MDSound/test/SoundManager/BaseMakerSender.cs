using System;
using System.Threading;
using System.Threading.Tasks;

namespace SoundManager
{
    public abstract class BaseMakerSender : IDisposable
    {
        public const int DATA_SEQUENCE_FREQUENCE = 44100;

        private CancellationTokenSource tokenSource;
        private CancellationToken cancellationToken;

        protected Task task = null;
        protected RingBuffer ringBuffer = null;
        protected Action action = null;
        protected volatile bool Start = false;
        protected volatile bool isRunning = false;
        protected object lockObj = new object();

        public SoundManager parent = null;


        public long GetRingBufferCounter()
        {
            return ringBuffer.LookUpCounter();
        }

        public long GetRingBufferSize()
        {
            return ringBuffer.GetDataSize();
        }

        public bool Mount()
        {
            tokenSource = new CancellationTokenSource();
            cancellationToken = tokenSource.Token;

            task = new Task(action, cancellationToken);
            task.Start();

            return true;
        }

        public bool Unmount()
        {
            if (task.Status == TaskStatus.Running)
            {
                tokenSource.Cancel();
            }
            //task.Wait(1000, cancellationToken);
            return true;
        }

        public bool Enq(long Counter, int Dev, int Typ, int Adr, int Val, object[] Ex)
        {
            return ringBuffer.Enq(Counter, Dev, Typ, Adr, Val, Ex);
        }

        public bool Deq(ref long Counter, ref int Dev, ref int Typ, ref int Adr, ref int Val, ref object[] Ex)
        {
            return ringBuffer.Deq(ref Counter, ref Dev, ref Typ, ref Adr, ref Val, ref Ex);
        }

        public void RequestStart()
        {
            lock (lockObj)
            {
                Start = true;
            }
        }

        public void RequestStop()
        {
            lock (lockObj)
            {
                Start = false;
            }
        }

        protected bool GetStart()
        {
            lock (lockObj)
            {
                return Start;
            }
        }

        public bool IsRunning()
        {
            lock (lockObj)
            {
                return isRunning;
            }
        }




        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (tokenSource != null) tokenSource.Dispose();
                    if (task != null) task.Dispose();
                }

                // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // TODO: 大きなフィールドを null に設定します。

                disposedValue = true;
            }
        }

        // TODO: 上の Dispose(bool disposing) にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        // ~BaseMakerSender() {
        //   // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
        //   Dispose(false);
        // }

        public void Dispose()
        {
            Dispose(true);
            // TODO: 上のファイナライザーがオーバーライドされる場合は、次の行のコメントを解除してください。
            // GC.SuppressFinalize(this);
        }
        #endregion
    }

}
