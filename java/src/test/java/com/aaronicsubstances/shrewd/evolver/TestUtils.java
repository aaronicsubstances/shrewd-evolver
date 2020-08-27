package com.aaronicsubstances.shrewd.evolver;

import java.util.LinkedHashMap;
import java.util.Map;

public class TestUtils {

    static Map<String, Object> toMap(Object... args) {
        Map<String, Object> m = new LinkedHashMap<>();
        for (int i = 0; i < args.length; i+=2) {
            String key = args[i] != null ? args[i].toString() : null;
            Object value = (i+1) < args.length ? args[i+1] : null;
            m.put(key, value);
        }
        return m;
    }
}