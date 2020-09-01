package com.aaronicsubstances.shrewd.evolver;

import java.util.List;
import java.util.Objects;

public class LogNavigator<T extends LogPositionHolder> {
    private final List<T> logs;
    private int nextIndex = 0;

    public LogNavigator(List<T> logs) {
        this.logs = logs;
    }
    
    public int nextIndex() {
        return nextIndex;
    }
    
    public boolean hasNext() {
        return nextIndex < logs.size();
    }
    
    public T next() {
        return logs.get(nextIndex++);
    }
    
    public T next(List<String> searchIds) {
        return next(searchIds, null);
    }
    
    public T next(List<String> searchIds, List<String> limitIds) {
        Objects.requireNonNull(searchIds, "searchIds");
        int stopIndex = logs.size();
        if (limitIds != null) {
            for (int i = nextIndex; i < logs.size(); i++) {
                if (limitIds.contains(logs.get(i).loadPositionId())) {
                    stopIndex = i;
                    break;
                }
            }
        }
        for (int i = nextIndex; i < stopIndex; i++) {
            T log = logs.get(i);
            if (searchIds.contains(log.loadPositionId())) {
                nextIndex = i + 1;
                return log;
            }
        }
        return null;
    }
}