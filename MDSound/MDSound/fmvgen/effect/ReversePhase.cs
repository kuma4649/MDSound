using System;
using System.Collections.Generic;
using System.Text;

namespace MDSound.fmvgen.effect
{
    public class ReversePhase
    {
        public int[][][] SSG;
        public int[][][] FM;
        public int[][] Rhythm;
        public int[][] AdpcmA;
        public int[][] Adpcm;

        public ReversePhase()
        {
            Init();
        }

        private void Init()
        {
            SSG = new int[4][][]{
                new int[3][] { new int[2], new int[2], new int[2] },
                new int[3][] { new int[2], new int[2], new int[2] },
                new int[3][] { new int[2], new int[2], new int[2] },
                new int[3][] { new int[2], new int[2], new int[2] }
            };
            FM = new int[2][][]{
                new int[6][] { new int[2], new int[2], new int[2], new int[2], new int[2], new int[2] },
                new int[6][] { new int[2], new int[2], new int[2], new int[2], new int[2], new int[2] }
            };
            Rhythm = new int[6][] { new int[2], new int[2], new int[2], new int[2], new int[2], new int[2] };
            AdpcmA = new int[6][] { new int[2], new int[2], new int[2], new int[2], new int[2], new int[2] };
            Adpcm = new int[3][] { new int[2], new int[2], new int[2] };

            for (int i = 0; i < 6; i++)
            {
                SSG[0][i / 2][i % 2] = 1;
                SSG[1][i / 2][i % 2] = 1;
                SSG[2][i / 2][i % 2] = 1;
                SSG[3][i / 2][i % 2] = 1;

                FM[0][i / 2][i % 2] = 1;
                FM[0][i / 2 + 3][i % 2] = 1;
                FM[1][i / 2][i % 2] = 1;
                FM[1][i / 2 + 3][i % 2] = 1;

                Rhythm[i / 2][i % 2] = 1;
                Rhythm[i / 2 + 3][i % 2] = 1;

                AdpcmA[i / 2][i % 2] = 1;
                AdpcmA[i / 2 + 3][i % 2] = 1;

                Adpcm[i / 2][i % 2] = 1;
            }
        }

        public void SetReg(uint adr, byte data)
        {
            switch (adr)
            {
                case 0://$CC
                    for (int i = 0; i < 6; i++)
                        SSG[0][i / 2][(i + 1) & 1] = (data & (1 << i)) != 0 ? -1 : 1;
                    break;
                case 1://$CD
                    for (int i = 0; i < 6; i++)
                        SSG[1][i / 2][(i + 1) & 1] = (data & (1 << i)) != 0 ? -1 : 1;
                    break;
                case 2://$CE
                    for (int i = 0; i < 6; i++)
                        SSG[2][i / 2][(i + 1) & 1] = (data & (1 << i)) != 0 ? -1 : 1;
                    break;
                case 3://$CF
                    for (int i = 0; i < 6; i++)
                        SSG[3][i / 2][(i + 1) & 1] = (data & (1 << i)) != 0 ? -1 : 1;
                    break;

                case 4://$D0
                    for (int i = 0; i < 6; i++)
                        FM[0][i / 2][(i + 1) & 1] = (data & (1 << i)) != 0 ? -1 : 1;
                    break;
                case 5://$D1
                    for (int i = 0; i < 6; i++)
                        FM[0][i / 2 + 3][(i + 1) & 1] = (data & (1 << i)) != 0 ? -1 : 1;
                    break;
                case 6://$D2
                    for (int i = 0; i < 6; i++)
                        FM[1][i / 2][(i + 1) & 1] = (data & (1 << i)) != 0 ? -1 : 1;
                    break;
                case 7://$D3
                    for (int i = 0; i < 6; i++)
                        FM[1][i / 2 + 3][(i + 1) & 1] = (data & (1 << i)) != 0 ? -1 : 1;
                    break;

                case 8://$D4
                    for (int i = 0; i < 6; i++)
                        Rhythm[i / 2][(i + 1) & 1] = (data & (1 << i)) != 0 ? -1 : 1;
                    break;
                case 9://$D5
                    for (int i = 0; i < 6; i++)
                        Rhythm[i / 2 + 3][(i + 1) & 1] = (data & (1 << i)) != 0 ? -1 : 1;
                    break;

                case 10://$D6
                    for (int i = 0; i < 6; i++)
                        AdpcmA[i / 2][(i + 1) & 1] = (data & (1 << i)) != 0 ? -1 : 1;
                    break;
                case 11://$D7
                    for (int i = 0; i < 6; i++)
                        AdpcmA[i / 2 + 3][(i + 1) & 1] = (data & (1 << i)) != 0 ? -1 : 1;
                    break;

                case 12://$D8
                    for (int i = 0; i < 6; i++)
                        Adpcm[i / 2][(i + 1) & 1] = (data & (1 << i)) != 0 ? -1 : 1;
                    break;
            }
        }
    }
}
