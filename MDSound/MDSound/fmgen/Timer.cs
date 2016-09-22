using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDSound.fmgen
{
    public class Timer
    {
        public void Reset()
        {
            timera_count = 0;
            timerb_count = 0;
        }

        public bool Count(int us)
        {
            bool Event = false;

            if (timera_count != 0)
            {
                timera_count -= us << 16;
                if (timera_count <= 0)
                {
                    Event = true;
                    TimerA();

                    while (timera_count <= 0)
                        timera_count += timera;

                    if ((regtc & 4) != 0)
                        SetStatus(1);
                }
            }
            if (timerb_count != 0)
            {
                timerb_count -= us << 12;
                if (timerb_count <= 0)
                {
                    Event = true;
                    while (timerb_count <= 0)
                        timerb_count += timerb;

                    if ((regtc & 8) != 0)
                        SetStatus(2);
                }
            }
            return Event;
        }

        public int GetNextEvent()
        {
            uint ta = (uint)(((timera_count + 0xffff) >> 16) - 1);
            uint tb = (uint)(((timerb_count + 0xfff) >> 12) - 1);
            return (int)((ta < tb ? ta : tb) + 1);
        }


        protected void SetStatus(uint bit)
        {
        }

        protected void ResetStatus(uint bit)
        {
        }

        protected void SetTimerBase(uint clock)
        {
            timer_step = (int)(1000000.0 * 65536 / clock);
        }

        protected void SetTimerA(uint addr, uint data)
        {
            uint tmp;
            regta[addr & 1] = (byte)(data);
            tmp = (uint)((regta[0] << 2) + (regta[1] & 3));
            timera = (int)((1024 - tmp) * timer_step);
            //	LOG2("Timer A = %d   %d us\n", tmp, timera >> 16);
        }

        protected void SetTimerB(uint data)
        {
            timerb = (int)((256 - data) * timer_step);
            //	LOG2("Timer B = %d   %d us\n", data, timerb >> 12);
        }

        protected void SetTimerControl(uint data)
        {
            uint tmp = regtc ^ data;
            regtc = (byte)(data);

            if ((data & 0x10) != 0)
                ResetStatus(1);
            if ((data & 0x20) != 0)
                ResetStatus(2);

            if ((tmp & 0x01) != 0)
                timera_count = (data & 1) != 0 ? timera : 0;
            if ((tmp & 0x02) != 0)
                timerb_count = (data & 2) != 0 ? timerb : 0;
        }


        protected byte status;
        protected byte regtc;

        private void TimerA()
        {
        }

        private byte[] regta = new byte[2];

        private int timera, timera_count;
        private int timerb, timerb_count;
        private int timer_step;

        //#else

        //        // ---------------------------------------------------------------------------
        //        //	タイマーA 周期設定
        //        //
        //        void Timer::SetTimerA(uint addr, uint data)
        //        {
        //            regta[addr & 1] = uint8(data);
        //            timera = (1024 - ((regta[0] << 2) + (regta[1] & 3))) << 16;
        //        }

        //        // ---------------------------------------------------------------------------
        //        //	タイマーB 周期設定
        //        //
        //        void Timer::SetTimerB(uint data)
        //        {
        //            timerb = (256 - data) << (16 + 4);
        //        }

        //        // ---------------------------------------------------------------------------
        //        //	タイマー時間処理
        //        //
        //        bool Timer::Count(int32 us)
        //        {
        //            bool event = false;

        //    int tick = us * timer_step;

        //	if (timera_count)
        //	{
        //		timera_count -= tick;
        //		if (timera_count <= 0)
        //		{
        //			event = true;
        //        TimerA();

        //			while (timera_count <= 0)
        //				timera_count += timera;

        //			if (regtc & 4)
        //				SetStatus(1);
        //        }
        //    }
        //	if (timerb_count)
        //	{
        //		timerb_count -= tick;
        //		if (timerb_count <= 0)
        //		{
        //			event = true;
        //			while (timerb_count <= 0)
        //				timerb_count += timerb;

        //			if (regtc & 8)
        //				SetStatus(2);
        //    }
        //}
        //	return event;
        //}

        //// ---------------------------------------------------------------------------
        ////	次にタイマーが発生するまでの時間を求める
        ////
        //int32 Timer::GetNextEvent()
        //{
        //    uint32 ta = timera_count - 1;
        //    uint32 tb = timerb_count - 1;
        //    uint32 t = (ta < tb ? ta : tb) + 1;

        //    return (t + timer_step - 1) / timer_step;
        //}

        //// ---------------------------------------------------------------------------
        ////	タイマー基準値設定
        ////
        //void Timer::SetTimerBase(uint clock)
        //{
        //    timer_step = clock * 1024 / 15625;
        //}

        //#endif

    }
}
