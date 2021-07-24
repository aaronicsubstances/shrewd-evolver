// tag: 20210724T0000
using System;
using System.Collections.Generic;
using System.Text;

namespace AaronicSubstances.ShrewdEvolver.Polling
{
    public class PollCallbackArg<T>
    {
        public T Value { get; set; }
        public long UptimeMillis { get; set; }
        public bool LastCall { get; set; }
    }
}
