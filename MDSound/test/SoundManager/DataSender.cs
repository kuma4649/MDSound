using System.Diagnostics;
using System.Threading;

namespace SoundManager
{
    public class DataSender : BaseSender
    {
        private static Stopwatch sw = Stopwatch.StartNew();
        private static readonly double swFreq = Stopwatch.Frequency;
        private readonly int Frq = DATA_SEQUENCE_FREQUENCE;
        private const long Def_SeqCounter = -500;
        private long SeqCounter = Def_SeqCounter;

        private readonly Enq EmuEnq = null;
        private readonly Enq RealEnq = null;
        private readonly Pack[] stopData = null;

        public DataSender(Enq EmuEnq, Enq RealEnq, Pack[] stopData, int BufferSize = DATA_SEQUENCE_FREQUENCE, int Frq = DATA_SEQUENCE_FREQUENCE)
        {
            action = Main;
            this.Frq = Frq;
            ringBuffer = new RingBuffer(BufferSize);
            this.ringBufferSize = BufferSize;
            SeqCounter = Def_SeqCounter;
            this.EmuEnq = EmuEnq;
            this.RealEnq = RealEnq;
            this.stopData = stopData;
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

        public void Init()
        {
            SeqCounter = Def_SeqCounter;
            ringBuffer.Init(ringBufferSize);
        }

        private void Main()
        {
            try
            {

                while (true)
                {
                    while (!GetStart())
                    {
                        Thread.Sleep(100);
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
                            //待ち合わせ割り込み
                            if (parent.GetInterrupt())
                            {
                                Thread.Sleep(1);
                                continue;
                            }

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
                        if (Dev >= 0) while (!EmuEnq(Counter, Dev, Typ, Adr, Val, Ex)) Thread.Sleep(1);
                        else while (!RealEnq(Counter, Dev, Typ, Adr, Val, Ex)) Thread.Sleep(1);
                    }

                    //停止時のデータの送信
                    if (stopData != null)
                    {
                        foreach (Pack dat in stopData)
                        {
                            //振り分けてEnqueue
                            if (dat.Dev >= 0) while (!EmuEnq(Counter, dat.Dev, dat.Typ, dat.Adr, dat.Val, null)) Thread.Sleep(1);
                            else while (!RealEnq(Counter, dat.Dev, dat.Typ, dat.Adr, dat.Val, null)) Thread.Sleep(1);
                        }
                    }

                    lock (lockObj)
                    {
                        isRunning = false;
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
