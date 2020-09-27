using System;
using System.Collections.Generic;
using System.Text;

namespace PortableIPC.Abstractions
{
    public class VoidReturn
    {
        private VoidReturn() { }

        public static readonly VoidReturn Instance = new VoidReturn();
    }
}
