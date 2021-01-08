package com.aaronicsubstances.shrewd.evolver;

import java.util.List;
import java.util.Objects;
import java.util.function.Predicate;

public class LogNavigator<T> {
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
    
    public T next(Predicate<T> searchCondition) {
        return next(searchCondition, null);
    }
    
    public T next(Predicate<T> searchCondition, Predicate<T> stopCondition) {
        Objects.requireNonNull(searchCondition, "searchCondition");
        int stopIndex = logs.size();
        if (stopCondition != null) {
            for (int i = nextIndex; i < logs.size(); i++) {
                if (stopCondition.test(logs.get(i))) {
                    stopIndex = i;
                    break;
                }
            }
        }
        for (int i = nextIndex; i < stopIndex; i++) {
            T log = logs.get(i);
            if (searchCondition.test(log)) {
                nextIndex = i + 1;
                return log;
            }
        }
        return null;
    }
}