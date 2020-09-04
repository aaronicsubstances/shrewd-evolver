package com.aaronicsubstances.shrewd.evolver;

import com.aaronicsubstances.shrewd.evolver.LogMessageTemplateParser.PartDescriptor;

import java.lang.reflect.Method;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;

public abstract class LogMessageTemplate {
    
    public static class Unstructured {
        private final String formatString;
        private final List<Object> formatArgs;

        public Unstructured(String formatString, List<Object> formatArgs) {
            this.formatString = formatString;
            this.formatArgs = formatArgs;
        }

        public String getFormatString() {
            return formatString;
        }

        public List<Object> getFormatArgs() {
            return formatArgs;
        }
    }

    public class Structured {
        private final Object treeDataSlice;

        public Structured(Object treeDataSlice) {
            this.treeDataSlice = treeDataSlice;
        }

        @Override
        public String toString() {
            return serializeData(treeDataSlice);
        }
    }

    private final List<Object> positionalArgs;
    private final Object treeData;

    // Contents are literal string, index into positional arg, or
    // treeDataKey, which is list of objects as path into treeData.
    // Each part of treeDataKey in turn consists of JSON property name, or index into JSON array.
    private final List<PartDescriptor> parsedFormatString;

    public LogMessageTemplate(String formatString, Object treeData,
            List<Object> positionalArgs) {
        this.treeData = treeData;
        this.positionalArgs = positionalArgs;
        this.parsedFormatString = parseFormatString(formatString);
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

    public Object toUnstructuredLogRecord() {
        List<Object> formatArgs = new ArrayList<>();
        String formatString = generateFormatString(formatArgs, true);
        return new Unstructured(formatString, formatArgs);
    }

    protected Object toStructuredLogRecord(Object treeDataSlice) {
        return new Structured(treeDataSlice);
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

    @SuppressWarnings("unchecked")
    protected Object getTreeDataSlice(Object treeData, List<Object> treeDataKey) {
        Object treeDataSlice = treeData;
        for (Object keyPart : treeDataKey) {
            if (treeDataSlice == null) {
                break;
            }
            if (keyPart instanceof Integer) {
                int index = (Integer) keyPart;
                if (!(treeDataSlice instanceof List)) {
                    treeDataSlice = getTreeDataListItem(treeDataSlice, index);
                    continue;
                }
                List<Object> jsonArray = (List<Object>) treeDataSlice;
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
                if (!(treeDataSlice instanceof Map)) {
                    treeDataSlice = getTreeDataPropertyValue(treeDataSlice, keyPart);
                    continue;
                }
                Map<Object, Object> jsonObject = (Map<Object, Object>) treeDataSlice;
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

    protected Object handleNonExistentPositionalArg(int index) {
        return null;
    }

    protected Object getTreeDataListItem(Object treeData, int index) {
        return null;
    }

    protected Object getTreeDataPropertyValue(Object treeData, Object propertyName) {
        try {
            Class<?> propAccessorType = Class.forName("org.apache.commons.beanutils.PropertyUtils");
            Method propAccessor = propAccessorType.getMethod("getSimpleProperty", Object.class,
                String.class);
            return propAccessor.invoke(null, treeData, propertyName);
        }
        catch (Exception ex) {
            return null;
        }
    }

    static List<PartDescriptor> parseFormatString(String formatString) {
        return new LogMessageTemplateParser(formatString).parse();
    }

    List<PartDescriptor> getParsedFormatString() {
        return parsedFormatString;
    }

    private String generateFormatString(List<Object> formatArgsReceiver, boolean forLogger) {
        StringBuilder logFormat = new StringBuilder();
        int uniqueIndex = 0;
        for (PartDescriptor part : parsedFormatString) {
            if (part.treeDataKey != null) {
                logFormat.append(generatePositionIndicator(uniqueIndex++, forLogger));
                Object treeDataSlice = getTreeDataSlice(treeData, part.treeDataKey);
                if (part.serializeTreeData) {
                    formatArgsReceiver.add(toStructuredLogRecord(treeDataSlice));
                }
                else {
                    formatArgsReceiver.add(treeDataSlice);
                }
            }
            else if (part.literalSection != null) {
                logFormat.append(escapeLiteral(part.literalSection, forLogger));
            }
            else {
                logFormat.append(generatePositionIndicator(uniqueIndex++, forLogger));
                Object item = getPositionalArg(positionalArgs, part.positionalArgIndex);
                formatArgsReceiver.add(item);
            }
        }
        return logFormat.toString();
    }
}