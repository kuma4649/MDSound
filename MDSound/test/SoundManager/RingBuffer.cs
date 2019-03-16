using System;
using System.Collections.Generic;

namespace SoundManager
{
    public class RingBuffer
    {

        private List<PPack> buf;
        private PPack enqPos = null;
        private PPack deqPos = null;
        private PPack srcPos = null;
        private PPack tmpPos = null;
        private int bLength = 0;

        public bool AutoExtend = true;

        private readonly object lockObj = new object();

        public RingBuffer(int size)
        {
            if (size < 2) return;

            Init(size);
        }

        public void Init(int size)
        {
            buf = new List<PPack>
            {
                new PPack()
            };
            for (int i = 1; i < size + 1; i++)
            {
                buf.Add(new PPack());
                buf[i].prev = buf[i - 1];
                buf[i - 1].next = buf[i];
            }
            buf[0].prev = buf[buf.Count - 1];
            buf[buf.Count - 1].next = buf[0];

            enqPos = buf[0];
            deqPos = buf[0];
            bLength = 0;
        }

        public bool Enq(long Counter, int Dev, int Typ, int Adr, int Val, object[] Ex)
        {
            lock (lockObj)
            {
                if (enqPos.next == deqPos)
                {
                    if (!AutoExtend)
                    {
                        return false;
                    }
                    //自動拡張
                    try
                    {
                        PPack p = new PPack();
                        buf.Add(p);
                        p.prev = enqPos;
                        p.next = enqPos.next;
                        enqPos.next = p;
                        p.next.prev = p;
                    }
                    catch
                    {
                        return false;
                    }
                }

                bLength++;

                //データをセット
                enqPos.Counter = Counter;
                enqPos.pack.Copy(Dev, Typ, Adr, Val, Ex);

                if (Counter >= enqPos.prev.Counter)
                {
                    enqPos = enqPos.next;

                    //debugDispBuffer();

                    return true;
                }

                PPack lastPos = enqPos.prev;
                //サーチ
                srcPos = enqPos.prev;
                while (Counter < srcPos.Counter && srcPos != deqPos)
                {
                    srcPos = srcPos.prev;
                }

                if (Counter < srcPos.Counter && srcPos == deqPos)
                {
                    srcPos = srcPos.prev;
                    deqPos = enqPos;
                }

                //enqPosをリングから切り出す。
                PPack nextPack = enqPos;
                enqPos.prev.next = enqPos.next;
                enqPos.next.prev = enqPos.prev;

                //enqPosを挿入する
                tmpPos = srcPos.next;
                tmpPos.prev = enqPos;
                srcPos.next = enqPos;
                enqPos.prev = srcPos;
                enqPos.next = tmpPos;

                enqPos = lastPos.next;

                //debugDispBuffer();

                return true;
            }
        }

        public bool Deq(ref long Counter, ref int Dev, ref int Typ, ref int Adr, ref int Val, ref object[] Ex)
        {
            lock (lockObj)
            {
                Counter = deqPos.Counter;

                Dev = deqPos.pack.Dev;
                Typ = deqPos.pack.Typ;
                Adr = deqPos.pack.Adr;
                Val = deqPos.pack.Val;
                Ex = deqPos.pack.Ex;

                if (enqPos == deqPos) return false;

                bLength--;
                deqPos.Counter = 0;
                deqPos = deqPos.next;

                //debugDispBuffer();

                return true;
            }
        }

        public int GetDataSize()
        {
            lock (lockObj)
            {
                return bLength;
            }
        }

        public long LookUpCounter()
        {
            lock (lockObj)
            {
                return deqPos.Counter;
            }
        }

#if DEBUG

        public void debugDispBuffer()
        {
            lock (lockObj)
            {
                PPack edbg = deqPos;
                do
                {
                    Console.Write("[{0}:{1}]::", edbg.Counter, edbg.pack.Dev);
                    edbg = edbg.next;
                } while (edbg != enqPos.next);
                Console.WriteLine("");
            }
        }

#endif 

    }
}
