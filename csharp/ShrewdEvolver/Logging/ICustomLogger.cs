// tag: 20210724T0000
using System;
using System.Collections.Generic;
using System.Text;

namespace AaronicSubstances.ShrewdEvolver.Logging
{
    public interface ICustomLogger
    {
        bool IsLogLevelEnbled(CustomLogLevel level);
        void Log(CustomLogLevel level, string message, params object[] args);
        void Log(CustomLogLevel level, Exception error, string message, params object[] args);
        void Log(CustomLogLevel level, CustomLogEvent logEvent);
        void Log(CustomLogLevel level, Func<CustomLogEvent> logEventSupplier);
    }
}
