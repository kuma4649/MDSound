using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SoundManager
{
    public class Pack
    {
        public int Dev;
        public int Typ;
        public int Adr;
        public int Val;
        public object[] Ex;

        public Pack()
        {

        }

        public Pack(int Dev, int Typ, int Adr, int Val, object[] Ex)
        {
            this.Dev = Dev;
            this.Typ = Typ;
            this.Adr = Adr;
            this.Val = Val;
            this.Ex = Ex;
        }

        public void Copy(Pack pack)
        {
            Dev = pack.Dev;
            Typ = pack.Typ;
            Adr = pack.Adr;
            Val = pack.Val;
            Ex = pack.Ex;
        }

        public void Copy(int Dev, int Typ, int Adr, int Val, object[] Ex)
        {
            this.Dev = Dev;
            this.Typ = Typ;
            this.Adr = Adr;
            this.Val = Val;
            this.Ex = Ex;
        }
    }

    public class PPack
    {
        public PPack prev;
        public PPack next;

        public long Counter;

        public Pack pack = new Pack();
    }

}
