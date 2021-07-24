// tag: 20210724T0000
package com.aaronicsubstances.shrewd.evolver.logging;

import java.util.function.Function;
import java.util.function.Supplier;

public class CustomLoggerFacade {
    private static final NoOpLogger DEFAULT_LOGGER = new NoOpLogger();
    private static Function<Object[], CustomLogger> loggerFactoryFunction;

    public static Function<Object[], CustomLogger> getLoggerFactoryFunction() {
        return loggerFactoryFunction;
    }

    public static void setLoggerFactoryFunction(Function<Object[], CustomLogger> value) {
        loggerFactoryFunction = value;
    }

    public static CustomLogger getLogger(Object classOrName) {
        if (loggerFactoryFunction != null) {
            return loggerFactoryFunction.apply(new Object[] { classOrName });
        }
        else {
            return DEFAULT_LOGGER;
        }
    }

    public static class NoOpLogger implements CustomLogger
    {
        public boolean isLogLevelEnbled(CustomLogLevel level) {
            return false;
        }

        public void log(CustomLogLevel level, String message, Object... args) {
        }

        public void log(CustomLogLevel level, Throwable error, String message, Object... args) {
        }

        public void log(CustomLogLevel level, CustomLogEvent logEvent) {
        }

        public void log(CustomLogLevel level, Supplier<CustomLogEvent> logEventSupplier) {
        }
    }
}
