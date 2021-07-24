// tag: 20210724T0000
package com.aaronicsubstances.shrewd.evolver.logging;

import java.util.function.Supplier;

public interface CustomLogger {
    boolean isLogLevelEnbled(CustomLogLevel level);
    void log(CustomLogLevel level, String message, Object... args);
    void log(CustomLogLevel level, Throwable error, String message, Object... args);
    void log(CustomLogLevel level, CustomLogEvent logEvent);
    void log(CustomLogLevel level, Supplier<CustomLogEvent> logEventSupplier);
}
