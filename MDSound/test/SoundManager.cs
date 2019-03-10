using System;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace test
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

        private Action OfDriver;
        private Snd OfEmu;
        private Snd OfReal;


        public void Setup(Action ActionOfDriver, Snd ActionOfEmuDevice, Snd ActionOfRealDevice)
        {
            this.OfDriver = ActionOfDriver;
            this.OfEmu = ActionOfEmuDevice;
            this.OfReal = ActionOfRealDevice;

            dataMaker = new DataMaker(OfDriver);
            emuChipSender = new EmuChipSender(ActionOfEmuDevice, DATA_SEQUENCE_FREQUENCE);
            realChipSender = new RealChipSender(ActionOfRealDevice, DATA_SEQUENCE_FREQUENCE);
            dataSender = new DataSender(emuChipSender.Enq, realChipSender.Enq);

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
            dataMaker.RequestStart();
            dataSender.RequestStart();
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

        public RingBuffer GetRecvBuffer()
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

    /// <summary>
    /// データ生成器
    /// ミュージックドライバーを駆動させ、生成するデータをDataSenderに送る
    /// </summary>
    public class DataMaker : BaseMakerSender
    {
        private readonly Action ActionOfDriver;

        public DataMaker(Action ActionOfDriver)
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
                    while (true)
                    {
                        if (!GetStart()) break;
                        Thread.Sleep(0);
                        ActionOfDriver?.Invoke();
                    }
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

    public class BaseSender : BaseMakerSender
    {
        protected long Counter = 0;
        protected int Dev = 0;
        protected int Typ = 0;
        protected int Adr = 0;
        protected int Val = 0;
        protected object[] Ex = null;

        protected int ringBufferSize;
    }

    public class DataSender : BaseSender
    {
        private static Stopwatch sw = Stopwatch.StartNew();
        private static readonly double swFreq = Stopwatch.Frequency;
        private readonly int Frq = DATA_SEQUENCE_FREQUENCE;
        private const long Def_SeqCounter = -500;
        private long SeqCounter = Def_SeqCounter;

        private readonly Enq EmuEnq = null;
        private readonly Enq RealEnq = null;


        public DataSender(Enq EmuEnq, Enq RealEnq, int BufferSize = DATA_SEQUENCE_FREQUENCE, int Frq = DATA_SEQUENCE_FREQUENCE)
        {
            action = Main;
            this.Frq = Frq;
            ringBuffer = new RingBuffer(BufferSize);
            this.ringBufferSize = BufferSize;
            SeqCounter = Def_SeqCounter;
            this.EmuEnq = EmuEnq;
            this.RealEnq = RealEnq;
        }

        public void ResetSeqCounter()
        {
            lock (lockObj)
            {
                SeqCounter = Def_SeqCounter;
            }
        }

        public long GetSeqCounter()
        {
            lock (lockObj)
            {
                return SeqCounter;
            }
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

                    double o = sw.ElapsedTicks / swFreq;
                    double step = 1 / (double)Frq;

                    while (true)
                    {
                        if (!GetStart()) break;
                        Thread.Sleep(0);

                        double el1 = sw.ElapsedTicks / swFreq;
                        if (el1 - o < step) continue;
                        if (el1 - o >= step * Frq / 100.0)//閾値10ms
                        {
                            do
                            {
                                o += step;
                            } while (el1 - o >= step);
                        }
                        else
                        {
                            o += step;
                        }

                        lock (lockObj)
                        {
                            SeqCounter++;
                            if (ringBuffer.GetDataSize() == 0)
                            {
                                if (!parent.IsRunningAtDataMaker())
                                {
                                    //RequestStop();
                                }
                                continue;
                            }
                            if (SeqCounter < ringBuffer.LookUpCounter()) continue;
                            //continue;
                        }

                        //dataが貯まってます！
                        ringBuffer.Deq(ref Counter, ref Dev, ref Typ, ref Adr, ref Val, ref Ex);

                        //振り分けてEnqueue
                        if (Dev >= 0)
                        {
                            while (!EmuEnq(Counter, Dev, Typ, Adr, Val, Ex))
                            {
                                Thread.Sleep(1);
                            }
                        }
                        else
                        {
                            while (!RealEnq(Counter, Dev, Typ, Adr, Val, Ex))
                            {
                                Thread.Sleep(1);
                            }
                        }
                    }

                    lock (lockObj)
                    {
                        isRunning = false;
                        SeqCounter = Def_SeqCounter;
                        ringBuffer.Init(ringBufferSize);
                    }
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

    public class ChipSender : BaseSender
    {
        protected readonly Snd ActionOfChip = null;
        protected bool busy = false;

        public ChipSender(Snd ActionOfChip, int BufferSize = DATA_SEQUENCE_FREQUENCE)
        {
            action = Main;
            ringBuffer = new RingBuffer(BufferSize)
            {
                AutoExtend = false
            };
            this.ringBufferSize = BufferSize;
            this.ActionOfChip = ActionOfChip;
        }

        public bool IsBusy()
        {
            lock (lockObj)
            {
                return busy;
            }
        }

        protected virtual void Main() { }
    }

    public class EmuChipSender : ChipSender
    {
        public EmuChipSender(Snd ActionOfEmuChip, int BufferSize = DATA_SEQUENCE_FREQUENCE)
            : base(ActionOfEmuChip, BufferSize)
        {
        }

        public RingBuffer recvBuffer = new RingBuffer(DATA_SEQUENCE_FREQUENCE);

        protected override void Main()
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

                    while (true)
                    {
                        if (!GetStart()) break;
                        Thread.Sleep(1);
                        if (ringBuffer.GetDataSize() == 0) continue;

                        //dataが貯まってます！
                        lock (lockObj)
                        {
                            busy = true;
                        }

                        try
                        {
                            while (ringBuffer.Deq(ref Counter, ref Dev, ref Typ, ref Adr, ref Val, ref Ex))
                            {
                                ActionOfChip?.Invoke(Counter, Dev, Typ, Adr, Val, Ex);
                                //while (!recvBuffer.Enq(Counter, Dev, Typ, Adr, Val, Ex)) { }
                            }
                        }
                        catch
                        {

                        }

                        lock (lockObj)
                        {
                            busy = false;
                        }
                    }

                    lock (lockObj)
                    {
                        isRunning = false;
                        ringBuffer.Init(ringBufferSize);
                    }

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

    public class RealChipSender : ChipSender
    {
        public RealChipSender(Snd ActionOfRealChip, int BufferSize = DATA_SEQUENCE_FREQUENCE)
            : base(ActionOfRealChip, BufferSize)
        {
        }

        protected override void Main()
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

                    while (true)
                    {
                        if (!GetStart()) break;
                        Thread.Sleep(1);
                        if (ringBuffer.GetDataSize() == 0) continue;

                        //dataが貯まってます！
                        lock (lockObj)
                        {
                            busy = true;
                        }

                        try
                        {
                            while (ringBuffer.Deq(ref Counter, ref Dev, ref Typ, ref Adr, ref Val, ref Ex))
                            {
                                ActionOfChip?.Invoke(Counter, Dev, Typ, Adr, Val, Ex);
                            }
                        }
                        catch
                        {

                        }

                        lock (lockObj)
                        {
                            busy = false;
                        }
                    }

                    lock (lockObj)
                    {
                        isRunning = false;
                        ringBuffer.Init(ringBufferSize);
                    }

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
