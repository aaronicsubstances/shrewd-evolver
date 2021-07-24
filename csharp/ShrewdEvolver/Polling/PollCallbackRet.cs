// tag: 20210724T0000
using System;
using System.Collections.Generic;
using System.Text;

namespace AaronicSubstances.ShrewdEvolver.Polling
{
    public class PollCallbackRet<T>
    {
        public T NextValue { get; set; }
        public bool Stop { get; set; }
    }
}
