package com.aaronicsubstances.shrewd.evolver;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;

public abstract class EmbeddableLogRecord {
    private final List<Object> positionalArgs;
    private final Object treeData;

    // Contents are literal string, index into positional arg, or
    // treeDataKey, which is list of objects as path into treeData.
    // Each part of treeDataKey in turn consists of JSON property name, or index into JSON array.
    private final List<Object> parsedFormatString;

    public EmbeddableLogRecord(String formatString, Object treeData,
            List<Object> positionalArgs) {
        this.treeData = treeData;
        this.positionalArgs = positionalArgs;
        this.parsedFormatString = parseFormatString(formatString);
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

    protected Object getPositionalArg(List<Object> args, int index) {
        // Support Python style indexing.
        if (Math.abs(index) >= args.size()) {
            return handleNonExistentPositionalArg(index);
        }
        if (index < 0) {
            index += args.size();
        }
        return args.get(index);
    }

    protected Object handleNonExistentPositionalArg(int index) {
        return null;
    }

    @SuppressWarnings("unchecked")
    protected Object getTreeDataSlice(Object treeData, List<Object> treeDataKey) {
        Object treeDataSlice = treeData;
        for (Object keyPart : treeDataKey) {
            if (keyPart instanceof Integer) {
                int index = (Integer) keyPart;
                if (!(treeDataSlice instanceof List)) {
                    return handleNonExistentTreeDataSlice(treeDataKey);
                }
                List<Object> jsonArray = (List) treeDataSlice;
                // Support Python style indexing.
                if (Math.abs(index) >= jsonArray.size()) {
                    return handleNonExistentTreeDataSlice(treeDataKey);
                }
                if (index < 0) {
                    index += jsonArray.size();
                }
                treeDataSlice = jsonArray.get(index);
            }
            else {
                assert keyPart instanceof String;
                if (!(treeDataSlice instanceof Map)) {
                    return handleNonExistentTreeDataSlice(treeDataKey);
                }
                Map<String, Object> jsonObject = (Map) treeDataSlice;
                if (!jsonObject.containsKey(keyPart)) {
                    return handleNonExistentTreeDataSlice(treeDataKey);
                }
                treeDataSlice = jsonObject.get(keyPart);
            }
        }
        return treeDataSlice;
    }

    protected Object handleNonExistentTreeDataSlice(List<Object> treeDataKey) {
        return null;
    }

    static List<Object> parseFormatString(String formatString) {
        return new LogRecordFormatParser(formatString).parse();
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
}