using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace PortableIPC.Abstractions
{
    public interface AbstractDatagramSocket
    {
        AbstractPromise<VoidReturn> HandleSend(IPEndPoint endpoint, byte[] data, int offset, int length);
    }
}
