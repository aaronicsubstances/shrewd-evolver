// tag: 20210724T0000
using System;
using System.Collections.Generic;
using System.Text;

namespace AaronicSubstances.ShrewdEvolver.Logging
{
    public static class CustomLoggerFacade
    {
        private static readonly NoOpLogger DefaultLogger = new NoOpLogger();
        public static Func<object[], ICustomLogger> LoggerFactoryFunction { get; set; }

        public static ICustomLogger GetLogger(object typeOrName)
        {
            if (LoggerFactoryFunction != null)
            {
                return LoggerFactoryFunction.Invoke(new object[] { typeOrName });
            }
            else
            {
                return DefaultLogger;
            }
        }

        public class NoOpLogger : ICustomLogger
        {
            public bool IsLogLevelEnbled(CustomLogLevel level)
            {
                return false;
            }

            public void Log(CustomLogLevel level, string message, params object[] args)
            {
            }

            public void Log(CustomLogLevel level, Exception error, string message, params object[] args)
            {
            }

            public void Log(CustomLogLevel level, CustomLogEvent logEvent)
            {
            }

            public void Log(CustomLogLevel level, Func<CustomLogEvent> logEventSupplier)
            {
            }
        }
    }
}
