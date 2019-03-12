using System.Threading;

namespace SoundManager
{
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
                    while (!GetStart())
                    {
                        Thread.Sleep(100);
                    }

                    lock (lockObj) isRunning = true;

                    while (true)
                    {
                        Thread.Sleep(1);
                        if (ringBuffer.GetDataSize() == 0)
                        {
                            //送信データが無く、停止指示がある場合のみ停止する
                            if (!GetStart()) break;
                            continue;
                        }

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
                    while (!GetStart())
                    {
                        Thread.Sleep(100);
                    }

                    lock (lockObj) isRunning = true;

                    while (true)
                    {
                        Thread.Sleep(1);
                        if (ringBuffer.GetDataSize() == 0)
                        {
                            //送信データが無く、停止指示がある場合のみ停止する
                            if (!GetStart()) break;
                            continue;
                        }

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
