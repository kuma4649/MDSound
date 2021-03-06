﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDSound
{
    abstract public class Instrument
    {

        public byte CHIP_SAMPLING_MODE = 2;
        public int CHIP_SAMPLE_RATE = 44100;
        public int[][][] visVolume = null;// chipid , type , LR

        abstract public string Name { get; set; }
        abstract public string ShortName { get; set; }

        abstract public uint Start(byte ChipID, uint clock);

        abstract public uint Start(byte ChipID, uint clock, uint ClockValue, params object[] option);

        abstract public void Stop(byte ChipID);

        abstract public void Reset(byte ChipID);

        abstract public void Update(byte ChipID, int[][] outputs, int samples);

        abstract public int Write(byte ChipID, int port, int adr, int data);

    }

}
