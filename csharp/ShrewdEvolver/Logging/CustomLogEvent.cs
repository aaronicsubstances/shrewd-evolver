﻿// tag: 20210724T0000
using System;
using System.Collections.Generic;
using System.Text;

namespace AaronicSubstances.ShrewdEvolver.Logging
{
    public class CustomLogEvent
    {
        public string Message { get; set; }
        public List<object> Arguments { get; set; }
        public Exception Error { get; set; }
        public object Data { get; set; }

    }
}
