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

    private final String rawFormatString;
    private final Object keywordArgs;
    private final List<Object> positionalArgs;

    // Contents are literal string, index into positional arg, or
    // treeDataKey, which is list of objects as path into treeData.
    // Each part of treeDataKey in turn consists of JSON property name, or index into JSON array.
    private final List<PartDescriptor> parsedFormatString;

    public LogMessageTemplate(String formatString, Object keywordArgs,
            List<Object> positionalArgs) {
        this(formatString, new LogMessageTemplateParser(formatString).parse(),
            keywordArgs, positionalArgs);
    }

    public LogMessageTemplate(String rawFormatString, List<PartDescriptor> parsedFormatString,
            Object keywordArgs, List<Object> positionalArgs) {
        this.rawFormatString = rawFormatString;
        this.parsedFormatString = parsedFormatString;
        this.keywordArgs = keywordArgs;
        this.positionalArgs = positionalArgs;
    }

    public List<PartDescriptor> getParsedFormatString() {
        return parsedFormatString;
    }

    @Override
    public String toString() {
        List<Object> formatArgs = new ArrayList<>();
        String genericFormatString = generateFormatString(formatArgs, false);
        String msg = String.format(genericFormatString, formatArgs.toArray());
        return msg;
    }

    public Object toStructuredLogRecord() {
        return toStructuredLogRecord(keywordArgs);
    }

    public Unstructured toUnstructuredLogRecord() {
        List<Object> formatArgs = new ArrayList<>();
        String formatString = generateFormatString(formatArgs, true);
        return new Unstructured(formatString, formatArgs);
    }

    private Object toStructuredLogRecord(Object treeDataSlice) {
        return new Structured(treeDataSlice);
    }

    protected abstract String serializeData(Object treeDataSlice);/* {
        try {
            Class<?> gsonCls = Class.forName("com.google.gson.Gson");
            Object gsonInstance = gsonCls.newInstance();
            Method method = gsonCls.getMethod("toJson", Object.class);
            return (String)method.invoke(gsonInstance, treeDataSlice);
        }
        catch (Exception ex) {
            if (ex instanceof RuntimeException) {
                throw (RuntimeException)ex;
            }
            throw new RuntimeException(ex);
        }
    }*/

    protected String escapeLiteral(String literal, boolean forLogger) {
        return literal.replace("%", "%%");
    }

    protected String generatePositionIndicator(int position, boolean forLogger) {
        return "%s";
    }

    Object getPositionalArg(List<Object> args, PartDescriptor part) {
        if (args == null) {
            return handleNonExistentPositionalArg(part);
        }
        // Support Python style indexing.
        int index = part.positionalArgIndex;
        if (Math.abs(index) >= args.size()) {
            return handleNonExistentPositionalArg(part);
        }
        if (index < 0) {
            index += args.size();
        }
        return args.get(index);
    }

    @SuppressWarnings("unchecked")
    Object getTreeDataSlice(Object treeData, PartDescriptor part) {
        Object treeDataSlice = treeData;
        for (int i = 0; i < part.treeDataKey.size(); i++) {
            if (treeDataSlice == null) {
                return handleNonExistentTreeDataSlice(part, i);
            }
            Object keyPart = part.treeDataKey.get(i);
            if (keyPart instanceof Integer) {
                int index = (Integer) keyPart;
                if (!(treeDataSlice instanceof List)) {
                    treeDataSlice = getTreeDataListItem(treeDataSlice, index);
                    continue;
                }
                List<Object> jsonArray = (List<Object>) treeDataSlice;
                // Support Python style indexing.
                if (Math.abs(index) >= jsonArray.size()) {
                    return handleNonExistentTreeDataSlice(part, i);
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
                    return handleNonExistentTreeDataSlice(part, i);
                }
                treeDataSlice = jsonObject.get(keyPart);
            }
        }
        return treeDataSlice;
    }

    protected Object handleNonExistentTreeDataSlice(PartDescriptor part, int nonExistentIndex) {
        return rawFormatString.substring(part.startPos, part.endPos);
    }

    protected Object handleNonExistentPositionalArg(PartDescriptor part) {
        return rawFormatString.substring(part.startPos, part.endPos);
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

    private String generateFormatString(List<Object> formatArgsReceiver, boolean forLogger) {
        StringBuilder logFormat = new StringBuilder();
        int uniqueIndex = 0;
        for (PartDescriptor part : parsedFormatString) {
            if (part.literalSection != null) {
                logFormat.append(escapeLiteral(part.literalSection, forLogger));
            }
            else {
                logFormat.append(generatePositionIndicator(uniqueIndex++, forLogger));
                Object formatArg;
                if (part.treeDataKey != null) {
                    formatArg = getTreeDataSlice(keywordArgs, part);
                }
                else {
                    formatArg = getPositionalArg(positionalArgs, part);
                }
                if (part.serialize) {
                    formatArgsReceiver.add(toStructuredLogRecord(formatArg));
                }
                else {
                    formatArgsReceiver.add(formatArg);
                }
            }
        }
        return logFormat.toString();
    }
}