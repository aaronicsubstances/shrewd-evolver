package com.aaronicsubstances.shrewd.evolver;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;

public abstract class EmbeddableLogRecord {
    private final List<Object> positionalArgs;
    private final Object treeData;

    // contents are Literal string, index into positional arg, or
    // list of objects as key into treeData.
    // each part of key in turn consists of JSON property name, or index into JSON array.
    private final List<Object> parsedFormatString = new ArrayList<>();

    public EmbeddableLogRecord(String formatString,
            List<Object> positionalArgs, Object treeData) {
        this.positionalArgs = positionalArgs;
        this.treeData = treeData;
        parseFormatString(formatString);
    }

    public String toLogFormatString(List<Object> formatArgsReceiver) {
        return generateFormatString(formatArgsReceiver, true);
    }

    @Override
    public String toString() {
        List<Object> formatArgs = new ArrayList<>();
        String genericFormatString = generateFormatString(formatArgs, false);
        String msg = String.format(genericFormatString, formatArgs.toArray());
        return msg;
    }

    public Object toStructuredLogRecord() {
        return toStructuredLogRecord(treeData);
    }

    protected Object toStructuredLogRecord(Object treeDataSlice) {
        return new Object() {
            @Override
            public String toString() {
                return serializeData(treeDataSlice);
            }
        };
    }

    protected abstract String serializeData(Object treeDataSlice);

    protected String escapeLiteral(String literal, boolean forLogger) {
        return literal.replace("%", "%%");
    }

    protected String generatePositionIndicator(int position, boolean forLogger) {
        return "%s";
    }

    private void parseFormatString(String formatString) {
        
    }

    List<Object> getParsedFormatString() {
        return parsedFormatString;
    }

    @SuppressWarnings("unchecked")
    private String generateFormatString(List<Object> formatArgsReceiver, boolean forLogger) {
        StringBuilder logFormat = new StringBuilder();
        int uniqueIndex = 0;
        for (Object part : parsedFormatString) {
            if (part instanceof Integer) {
                logFormat.append(generatePositionIndicator(uniqueIndex++, forLogger));
                int position = (Integer) part;
                Object item = getPositionalArg(positionalArgs, position);
                formatArgsReceiver.add(item);
            }
            else if (part instanceof List) {
                logFormat.append(generatePositionIndicator(uniqueIndex++, forLogger));
                List<Object> treeDataKey = (List) part;
                Object treeDataSlice = getTreeDataSlice(treeData, treeDataKey);
                formatArgsReceiver.add(toStructuredLogRecord(treeDataSlice));
            }
            else {
                logFormat.append(escapeLiteral((String) part, forLogger));
            }
        }
        return logFormat.toString();
    }

    static Object getPositionalArg(List<Object> args, int index) {
        if (index < 0) {
            // Support Python style indexing.
            index += args.size();
        }
        if (index >= 0 && index < args.size()) {
            return args.get(index);
        }
        else {
            return null;
        }
    }

    @SuppressWarnings("unchecked")
    static Object getTreeDataSlice(Object treeData, List<Object> treeDataKey) {
        Object treeDataSlice = treeData;
        for (Object keyPart : treeDataKey) {
            if (keyPart instanceof Integer) {
                int index = (Integer) keyPart;
                if (!(treeDataSlice instanceof List)) {
                    treeDataSlice = null;
                    break;
                }
                List<Object> jsonArray = (List) treeDataSlice;
                if (index < 0) {
                    // Support Python style indexing.
                    index += jsonArray.size();
                }
                if (index < 0 || index >= jsonArray.size()) {
                    treeDataSlice = null;
                    break;
                }
                treeDataSlice = jsonArray.get(index);
            }
            else {
                if (!(treeDataSlice instanceof Map)) {
                    treeDataSlice = null;
                    break;
                }
                Map<String, Object> jsonObject = (Map) treeDataSlice;
                if (!jsonObject.containsKey(keyPart)) {
                    treeDataSlice = null;
                    break;
                }
                treeDataSlice = jsonObject.get(keyPart);
            }
        }
        return treeDataSlice;
    }
}