using System;
using System.Collections.Generic;
using System.Text;

namespace MDSound.np
{
    //
    // filter.h から抜粋
    //
    //



    //  dcf.SetParam(270,(*config)["HPF"]);    //DCFilter
    //  lpf.SetParam(4700.0,(*config)["LPF"]); //Filter


    public class DCFilter//HPF
    {
        private double R, C;
        private double a;
        private double[] _in = new double[2], _out = new double[2];
        private double rate;

        public DCFilter()
        {
            R = C = 0.0;
            Reset();
        }

        ~DCFilter()
        {
        }

        public void UpdateFactor()
        {
            if (C == 0.0 || R == 0.0)
            {
                a = 2.0; // disable
            }
            else
            {
                a = (R * C) / ((R * C) + (1.0 / rate));
            }
        }

        public double GetFactor() { return a; }

        public void SetRate(double r)
        {
            // カットオフ周波数 : 2pi*R*C
            rate = r;
            UpdateFactor();
        }

        public void SetParam(double r, int c) // c = 0-256, 256 = off, 0 = max
        {
            R = r;
            //C = c;

            if (c > 255)
                C = 0.0; // disable
            else
                C = 2.0e-4 * (1.0 - Math.Pow(1.0 - ((double)(c + 1) / 256.0), 0.05));

            // the goal of this curve is to have a wide range of practical use,
            // though it may look a little complicated. points of interest:
            //   HPF = 163 ~ my NES
            //   HPF = 228 ~ my Famicom
            //   low values vary widely and have an audible effect
            //   high values vary slowly and have a visible effect on DC offset

            UpdateFactor();
        }

        // 非virtualなRender
        public uint FastRender(int[] b)//[2])
        {
            if (a < 1.0)
            {
                _out[0] = a * (_out[0] + b[0] - _in[0]);
                _in[0] = b[0];
                b[0] = (int)_out[0];

                _out[1] = a * (_out[1] + b[1] - _in[1]);
                _in[1] = b[1];
                b[1] = (int)_out[1];
            }
            return 2;
        }

        public uint Render(int[] b)//[2])
        {
            return FastRender(b);
        }

        public void Tick(uint clocks)
        {
        }

        public void Reset()
        {
            _in[0] = _in[1] = 0;
            _out[0] = _out[1] = 0;
        }

        public void SetLevel(int[] b)//[2])
        {
            _in[0] = b[0];
            _in[1] = b[1];
        }
    }



}
