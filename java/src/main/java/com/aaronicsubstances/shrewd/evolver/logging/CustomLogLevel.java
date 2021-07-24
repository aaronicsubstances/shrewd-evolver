// tag: 20210724T0000
package com.aaronicsubstances.shrewd.evolver.logging;

public enum CustomLogLevel {
    TRACE(1),
    DEBUG(3),
    INFO(5),
    WARN(7),
    ERROR(9);

    public final int level;

    private CustomLogLevel(int level) {
        this.level = level;
    }
}
