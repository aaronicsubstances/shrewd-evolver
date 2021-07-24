// tag: 20210724T0000
package com.aaronicsubstances.shrewd.evolver.logging;

import java.util.List;

public class CustomLogEvent {
    private String message;
    private List<Object> arguments;
    private Throwable error;
    private Object data;

    public String getMessage() {
        return message;
    }
    
    public void setMessage(String value) {
        message = value;
    }

    public List<Object> getArguments() {
        return arguments;
    }

    public void setArguments(List<Object> value) {
        arguments = value;
    }

    public Throwable getError() {
        return error;
    }

    public void setError(Throwable value) {
        error = value;
    }

    public Object getData() {
        return data;
    }

    public void setData(Object value) {
        data = value;
    }
}
