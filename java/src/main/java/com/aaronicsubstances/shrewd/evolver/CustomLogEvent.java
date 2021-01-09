package com.aaronicsubstances.shrewd.evolver;

import org.apache.commons.beanutils.PropertyUtils;

import com.google.gson.Gson;

import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.function.Function;

public class CustomLogEvent {
    
    @FunctionalInterface
    public interface CustomLogEventMessageGenerator {
        String apply(Function<String, String> jsonSerializerFunc, Function<String, String> stringifyFunc);
    }

    private String message;
    private List<Object> arguments;
    private Exception error;
    private Object data;
    
    public CustomLogEvent()
    {
        this(null, null);
    }

    public CustomLogEvent(String message)
    {
        this(message, null);
    }

    public CustomLogEvent(String message, Exception error)
    {
        this.message = message;
        this.error = error;
    }

    public String getMessage() {
        return message;
    }
    public void setMessage(String message) {
        this.message = message;
    }
    
    public List<Object> getArguments() {
        return arguments;
    }
    public void setArguments(List<Object> arguments) {
        this.arguments = arguments;
    }
    
    public Exception getError() {
        return error;
    }
    public void setError(Exception error) {
        this.error = error;
    }

    public Object getData() {
        return data;
    }
    public void setData(Object data) {
        this.data = data;
    }

    public CustomLogEvent addProperty(String name, Object value) {
        if (data == null) {
            data = new LinkedHashMap<String, Object>();
        }
        ((Map<Object, Object>)data).put(name, value);
        return this;
    }
        
    public CustomLogEvent generateMessage(CustomLogEventMessageGenerator customLogEventMsgGenerator) {
        message = customLogEventMsgGenerator.apply(
            path -> fetchDataSliceAndStringify(path, true),
            path -> fetchDataSliceAndStringify(path, false));
        return this;
    }

    private String fetchDataSliceAndStringify(String path, boolean serializeAsJson) {
        Object dataSlice = fetchDataSlice(path);
        if (serializeAsJson) {
            return serializeAsJson(dataSlice);
        }
        else {
            return dataSlice != null ? dataSlice.toString() : "";
        }
    }

    Object fetchDataSlice(String path) {
        // split path and ensure no surrounding whitespace around 
        // individual segments.
        String[] pathSegments = path.split("/");
        for (int i = 0; i < pathSegments.length; i++) {
            pathSegments[i] = pathSegments[i].trim();
        }

        Object dataSlice = data;
        for (String pathSegment: pathSegments) {
            if (dataSlice == null) {
                break;
            }

            // skip empty path segments.
            if (pathSegment.length() == 0) {
                continue;
            }

            if (dataSlice instanceof List) {
                List<Object> jsonArray = (List<Object>) dataSlice;
                try {
                    int index = Integer.parseInt(pathSegment);
                    // Support Python style indexing.
                    if (Math.abs(index) >= jsonArray.size()) {
                        dataSlice = null;
                    }
                    else {
                        if (index < 0)
                        {
                            index += jsonArray.size();
                        }
                        dataSlice = jsonArray.get(index);
                    }
                    continue;
                }
                catch (NumberFormatException ignore) {
                    
                }

                // let Map branch of code handle pathSegments which are ints 
                // but correspond to data slices which
                // are not list instances.
            }

            if (dataSlice instanceof Map) {
                Map<Object, Object> dataSliceDict = (Map<Object, Object>) dataSlice;
                if (dataSliceDict.containsKey(pathSegment)) {
                    dataSlice = dataSliceDict.get(pathSegment);
                }
                else {
                    dataSlice = null;
                }
            }
            else {
                dataSlice = getTreeDataPropertyValue(dataSlice, pathSegment);
            }
        }
        return dataSlice;
    }

    protected Object getTreeDataPropertyValue(Object dataSlice, String pathSegment) {
        if (dataSlice == null) {
            return null;
        }
        try {
            // try declared property access
            return PropertyUtils.getSimpleProperty(dataSlice, pathSegment);
        }
        catch (Exception ignore) {
            return null;
        }
    }

    protected String serializeAsJson(Object o) {
        return new Gson().toJson(o);
    }
}