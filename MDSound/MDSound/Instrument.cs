using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDSound
{
    abstract public class Instrument
    {

        public const byte CHIP_SAMPLING_MODE = 2;
        public const int CHIP_SAMPLE_RATE = 44100;

        public const string Name = "Instrument";

        abstract public uint Start(byte ChipID, uint clock);

        abstract public void Stop(byte ChipID);

        abstract public void Reset(byte ChipID);

        abstract public void Update(byte ChipID, int[][] outputs, int samples);

    }

}
