using System;
using System.Collections.Generic;
using System.Text;

namespace PortableIPC.Abstractions
{
    public delegate U AbstractExecutableCode<in T, out U>(T param);
}
