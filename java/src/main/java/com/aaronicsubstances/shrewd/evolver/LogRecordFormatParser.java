package com.aaronicsubstances.shrewd.evolver;

import java.util.ArrayList;
import java.util.List;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

public class LogRecordFormatParser {

    enum FormatTokenType {
        LITERAL_STRING_SECTION, LITERAL_BEGIN_REPLACEMENT, LITERAL_END_REPLACEMENT,
        BEGIN_REPLACEMENT, END_REPLACEMENT, DOT, 
        OPENING_SQUARE, CLOSING_SQUARE
    }
    
    private static final Pattern NEW_LINE_REGEX = Pattern.compile("\r\n|\r|\n");
    
    private final String source;

    // Token properties. Assign defaults.
    private int startPos, endPos;
    private FormatTokenType tokenType;
    private boolean inReplacementField = false;

    public LogRecordFormatParser(String source) {
        this.source = source;
    }

    private RuntimeException createParseError(String errorMessage) {
        return createParseError(errorMessage, false);
    }

    private RuntimeException createParseError(String errorMessage, boolean scanError) {
        StringBuilder fullMessage = new StringBuilder();
        int[] posInfo = calculateLineAndColumnNumbers(source, scanError ? endPos : startPos);
        int lineNumber = posInfo[0], columnNumber = posInfo[1];
        String offendingLine = NEW_LINE_REGEX.split(source, -1)[lineNumber - 1];
        fullMessage.append("at line ").append(lineNumber);
        fullMessage.append(": ").append(errorMessage).append("\n\n");
        fullMessage.append(offendingLine).append("\n");
        appendUnderline(fullMessage, offendingLine.length(), columnNumber-1, ' ', '^');
        throw new RuntimeException(fullMessage.toString());
    }

    private RuntimeException creatEndOfStringError(String errorMessage) {
        String fullMessage = errorMessage;
        throw new RuntimeException(fullMessage);
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

    String nextToken() {
        if (endPos >= source.length()) {
            return null;
        }
        startPos = endPos;
        StringBuilder token = new StringBuilder();
        if (inReplacementField) {
            while (endPos < source.length()) {
                char ch = source.charAt(endPos++);
                if (ch == '{' || ch == '}' || ch == '.' ||
                        ch == '[' || ch == ']') {
                    break;
                }
                token.append(ch);
            }
            if (token.length() > 0) {
                tokenType = FormatTokenType.LITERAL_STRING_SECTION;
            }
            else {
                char ch = source.charAt(startPos);
                token.append(ch);
                switch (ch) {
                    case '{':
                        if (endPos < source.length() && source.charAt(endPos) == '{') {
                            endPos++; // unescape by skipping one of them.
                            tokenType = FormatTokenType.LITERAL_BEGIN_REPLACEMENT;
                        }
                        else {
                            tokenType = FormatTokenType.BEGIN_REPLACEMENT;
                        }
                        break;
                    case '}':
                        if (endPos < source.length() && source.charAt(endPos) == '}') {
                            endPos++; // unescape by skipping one of them.
                            tokenType = FormatTokenType.LITERAL_END_REPLACEMENT;
                        }
                        else {
                            tokenType = FormatTokenType.END_REPLACEMENT;
                        }
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
            while (endPos < source.length()) {
                char ch = source.charAt(endPos++);
                if (ch == '{') {
                    if (endPos < source.length() && source.charAt(endPos) == '{') {
                        endPos++; // unescape by skipping one of them.
                    }
                    else {
                        break;
                    }
                }
                if (ch == '}') {
                    if (endPos < source.length() && source.charAt(endPos) == '}') {
                        endPos++; // unescape by skipping one of them.
                    }
                    else {
                        break;
                    }
                }
                token.append(ch);
            }
            if (token.length() > 0) {
                tokenType = FormatTokenType.LITERAL_STRING_SECTION;
            }
            else {
                char ch = source.charAt(startPos);
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

    public List<Object> parse() {
        List<Object> parts = new ArrayList<>();
        String token;
        while ((token = nextToken()) != null) {
            switch (tokenType) {
                case LITERAL_STRING_SECTION:
                    parts.add(token);
                    break;
                case BEGIN_REPLACEMENT:
                    parseReplacementField(parts);
                    break;
                case END_REPLACEMENT:
                    throw createParseError("Single '}' encountered in format string");
                default:
                    throw createParseError("Unexpected token: " + token);
            }
        }
        return parts;
    }

    private void parseReplacementField(List<Object> parts) {
        List<Object> treeDataKey = new ArrayList<>();
        inReplacementField = true;
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
                    parts.add(notTreeDataKeyButIndex);
                    treeDataKey = null;
                    break;
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
                    throw createParseError("expected '}' before beginning of another replacement field");
                case LITERAL_BEGIN_REPLACEMENT:
                    throw createParseError("unexpected '{' in replacement field");
                case LITERAL_END_REPLACEMENT:
                    throw createParseError("unexpected '}' in replacement field");
                default:
                    throw createParseError("Unexpected token: " + token);
            }
        }
        inReplacementField = false;
        if (treeDataKey != null) {
            parts.add(treeDataKey);
        }
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
            if ("0123456789".contains("" + token.charAt(0))) {
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