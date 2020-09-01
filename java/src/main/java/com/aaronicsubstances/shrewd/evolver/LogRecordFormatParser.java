package com.aaronicsubstances.shrewd.evolver;

import java.util.ArrayList;
import java.util.List;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

public class LogRecordFormatParser {

    enum FormatTokenType {
        LITERAL_STRING_SECTION, BEGIN_REPLACEMENT, END_REPLACEMENT, DOT, 
        OPENING_SQUARE, CLOSING_SQUARE
    }
    
    private static final Pattern NEW_LINE_REGEX = Pattern.compile("\r\n|\r|\n");
    
    private final String source;

    // Token properties.
    int startPos, endPos;
    FormatTokenType tokenType;
    int partStart;

    private boolean inReplacementField = false;

    public LogRecordFormatParser(String source) {
        this.source = source;
    }

    private RuntimeException createParseError(String errorMessage) {
        return createParseError(errorMessage, false);
    }

    private RuntimeException createParseError(String errorMessage, boolean scanError) {
        StringBuilder fullMessage = new StringBuilder();
        int errorPosition = scanError ? endPos : startPos;
        int[] posInfo = calculateLineAndColumnNumbers(source, errorPosition);
        int lineNumber = posInfo[0], columnNumber = posInfo[1];
        String offendingLine = NEW_LINE_REGEX.split(source, -1)[lineNumber - 1];
        fullMessage.append("at index ").append(errorPosition);
        fullMessage.append(", line ").append(lineNumber);
        fullMessage.append(": ").append(errorMessage).append("\n\n");
        fullMessage.append(offendingLine).append("\n");
        appendUnderline(fullMessage, offendingLine.length(), columnNumber-1, ' ', '^');
        throw new RuntimeException(fullMessage.toString());
    }

    private RuntimeException creatEndOfStringError(String errorMessage) {
        throw new RuntimeException(errorMessage);
    }
    
    static int[] calculateLineAndColumnNumbers(String s, int position) {
        int lineNumber = 1; // NB: line number starts from 1.
        Matcher newLineMatcher = NEW_LINE_REGEX.matcher(s);
        int lastNewLineEnd = 0;
        while (newLineMatcher.find(lastNewLineEnd)) {
            if (newLineMatcher.end() > position) {
                break;
            }
            lastNewLineEnd = newLineMatcher.end();
            lineNumber++;
        }

        // use last match to calculate column number position.
        // NB: column number starts from 1.
        int columnNumber = position - lastNewLineEnd + 1;
        return new int[]{ lineNumber, columnNumber };
    }
    
    static void appendUnderline(StringBuilder sb, int lineLength, int indexInLine,
            char spacerCP, char positionMarkerCP) {
        int i = 0;
        for (; i < indexInLine; i++) {
            sb.append(spacerCP);
        }

        sb.append(positionMarkerCP);
        for (; i < lineLength - 1; i++) {
            sb.append(spacerCP);
        }
    }

    public List<Object> parse() {
        List<Object> parts = new ArrayList<>();
        Object part;
        while ((part = parseOnePart()) != null) {
            parts.add(part);
        }
        return parts;
    }

    Object parseOnePart() {
        String token = nextToken();
        if (token == null) {
            return null;
        }
        switch (tokenType) {
            case LITERAL_STRING_SECTION:
                return token;
            case BEGIN_REPLACEMENT:
                inReplacementField = true;
                Object part = parseReplacementField();
                inReplacementField = false;
                return part;
            case END_REPLACEMENT:
                throw createParseError("Single '}' encountered in format string");
            default:
                throw createParseError("Unexpected token: " + token);
        }
    }

    private String nextToken() {
        if (endPos >= source.length()) {
            return null;
        }
        startPos = endPos;
        StringBuilder token = new StringBuilder();
        if (inReplacementField) {
            while (endPos < source.length()) {
                char ch = source.charAt(endPos);
                if (ch == '{' || ch == '}' || ch == '.' ||
                        ch == '[' || ch == ']') {
                    break;
                }
                token.append(ch);
                endPos++;
            }
            if (token.length() > 0) {
                tokenType = FormatTokenType.LITERAL_STRING_SECTION;
            }
            else {
                char ch = source.charAt(endPos++);
                token.append(ch);
                switch (ch) {
                    case '{':
                        tokenType = FormatTokenType.BEGIN_REPLACEMENT;
                        break;
                    case '}':
                        tokenType = FormatTokenType.END_REPLACEMENT;
                        break;
                    case '[':
                        tokenType = FormatTokenType.OPENING_SQUARE;
                        break;
                    case ']':
                        tokenType = FormatTokenType.CLOSING_SQUARE;
                        break;
                    case '.':
                        tokenType = FormatTokenType.DOT;
                        break;
                    default:
                        throw createParseError("Unexpected char: " + ch, true);
                }
            }
        }
        else {
            partStart = startPos;
            while (endPos < source.length()) {
                char ch = source.charAt(endPos);
                if (ch == '{') {
                    if (endPos+1 < source.length() && source.charAt(endPos+1) == '{') {
                        endPos++; // unescape by skipping one of them.
                    }
                    else {
                        break;
                    }
                }
                if (ch == '}') {
                    if (endPos+1 < source.length() && source.charAt(endPos+1) == '}') {
                        endPos++; // unescape by skipping one of them.
                    }
                    else {
                        break;
                    }
                }
                token.append(ch);
                endPos++;
            }
            if (token.length() > 0) {
                tokenType = FormatTokenType.LITERAL_STRING_SECTION;
            }
            else {
                char ch = source.charAt(endPos++);
                token.append(ch);
                switch (ch) {
                    case '{':
                        tokenType = FormatTokenType.BEGIN_REPLACEMENT;
                        break;
                    case '}':
                        tokenType = FormatTokenType.END_REPLACEMENT;
                        break;
                    default:
                        throw createParseError("Unexpected char: " + ch, true);
                }
            }
        }
        return token.toString();
    }

    private Object parseReplacementField() {
        List<Object> treeDataKey = new ArrayList<>();
        while (true) {
            String token = nextToken();
            if (token == null) {
                throw creatEndOfStringError("expected '}' before end of string");
            }
            if (tokenType == FormatTokenType.END_REPLACEMENT) {
                break;
            }
            if (treeDataKey.isEmpty() && tokenType == FormatTokenType.LITERAL_STRING_SECTION) {
                Integer notTreeDataKeyButIndex = parsePositionalIndex(token);
                if (notTreeDataKeyButIndex != null) {
                    return notTreeDataKeyButIndex;
                }
            }
            switch (tokenType) { 
                case LITERAL_STRING_SECTION:
                    token = token.trim();
                    if (!token.isEmpty()) {
                        if (treeDataKey.isEmpty()) {
                            treeDataKey.add(token);
                        }
                        else {
                            throw createParseError("expected '.', '[' or '}'");
                        }
                    }
                    break;
                case OPENING_SQUARE:
                    int arrayIndex = parseArrayIndex();
                    treeDataKey.add(arrayIndex);
                    break;
                case DOT:
                    String propertyName = parsePropertyName();
                    treeDataKey.add(propertyName);
                    break;
                case CLOSING_SQUARE:
                    throw createParseError("Single ']' encountered in replacement field");
                case BEGIN_REPLACEMENT:
                    throw createParseError("invalid '{' in replacement field");
                default:
                    throw createParseError("Unexpected token: " + token);
            }
        }
        return treeDataKey;
    }

    private Integer parsePositionalIndex(String token) {
        token = token.trim();
        if (token.isEmpty()) {
            return null;
        }
        int positionalIndex;
        try {
            positionalIndex = Integer.parseInt(token);
        }
        catch (NumberFormatException ex) {
            if (!"0123456789".contains("" + token.charAt(0))) {
                return null;
            }
            throw createParseError("invalid positional index");
        }
        token = nextToken();
        if (token == null) {
            throw creatEndOfStringError("expected '}' before end of string");
        }
        if (tokenType != FormatTokenType.END_REPLACEMENT) {
            throw createParseError("expected '}'");
        }
        return positionalIndex;
    }

    private String parsePropertyName() {
        String token = nextToken();
        if (token == null) {
            throw creatEndOfStringError("expected property name before end of string");
        }
        if (tokenType != FormatTokenType.LITERAL_STRING_SECTION) {
            throw createParseError("expected property name");
        }
        // require property name.
        token = token.trim();
        if (token.isEmpty()) {
            throw createParseError("expected property name");
        }
        return token;
    }

    private int parseArrayIndex() {
        String token = nextToken();
        if (token == null) {
            throw creatEndOfStringError("expected array index before end of string");
        }
        if (tokenType != FormatTokenType.LITERAL_STRING_SECTION) {
            throw createParseError("expected array index");
        }
        token = token.trim();
        if (token.isEmpty()) {
            throw createParseError("expected array index");
        }
        int arrayIndex;
        try {
            arrayIndex = Integer.parseInt(token);
        }
        catch (NumberFormatException ex) {
            throw createParseError("invalid array index");
        }
        token = nextToken();
        if (token == null) {
            throw creatEndOfStringError("expected ']' before end of string");
        }
        if (tokenType != FormatTokenType.CLOSING_SQUARE) {
            throw createParseError("expected ']'");
        }
        return arrayIndex;
    }
}