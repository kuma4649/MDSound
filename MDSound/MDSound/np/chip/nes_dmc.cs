
using MDSound.np.cpu;
using System;

namespace MDSound.np.chip
{
    public class nes_dmc : ISoundChip
    {
        public np_nes_dmc dmc = new np_nes_dmc();
        public np_nes_dmc.NES_DMC chip;
        private km6502 cpu = null;

        public nes_dmc()
        {
        }

        public override bool Read(uint adr, ref uint val, uint id = 0)
        {
            return dmc.NES_DMC_np_Read(chip, adr, ref val);
        }

        public override uint Render(int[] b)
        {
            uint ret = dmc.NES_DMC_org_Render(chip, b);
            MDSound.np_nes_dmc_volume = Math.Abs(b[0]);
            return ret;
        }

        public override void Reset()
        {
        }

        public void Reset(km6502 cpu)
        {
            this.cpu = cpu;
            dmc.SetCPU(chip, cpu);
            dmc.NES_DMC_np_Reset(chip);
        }

        public override void SetClock(double clock)
        {
            dmc.NES_DMC_np_SetClock(chip,clock);
        }

        public override void SetMask(int mask)
        {
            dmc.NES_DMC_np_SetMask(chip, mask);
        }

        public override void SetOption(int id, int val)
        {
            dmc.NES_DMC_np_SetOption(chip, id, val);
        }

        public override void SetRate(double rate)
        {
            dmc.NES_DMC_np_SetRate(chip, rate);
        }

        public override void SetStereoMix(int trk, short mixl, short mixr)
        {
            dmc.NES_DMC_np_SetStereoMix(chip, trk, mixl, mixr);
        }

        public override void Tick(uint clocks)
        {
            dmc.org_Tick(chip, clocks);
        }

        public override bool Write(uint adr, uint val, uint id = 0)
        {
            return dmc.NES_DMC_np_Write(chip, adr, val);
        }

        public void SetMemory(IDevice r)
        {
            dmc.NES_DMC_org_SetMemory(chip, r);
        }
    }
}
