using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace AaronicSubstances.ShrewdEvolver
{
    public class LogRecordFormatParser
    {
        internal enum FormatTokenType
        {
            LITERAL_STRING_SECTION, BEGIN_REPLACEMENT, END_REPLACEMENT, DOT,
            OPENING_SQUARE, CLOSING_SQUARE
        }

        private static readonly Regex NEW_LINE_REGEX = new Regex("\r\n|\r|\n");
    
        private readonly string source;

        // Token properties.
        internal int startPos, endPos;
        internal FormatTokenType tokenType;
        internal int partStart;

        private bool inReplacementField = false;

        public LogRecordFormatParser(string source)
        {
            this.source = source;
        }

        private Exception CreateParseError(string errorMessage)
        {
            return CreateParseError(errorMessage, false);
        }

        private Exception CreateParseError(string errorMessage, bool scanError)
        {
            var fullMessage = new StringBuilder();
            int errorPosition = scanError ? endPos : startPos;
            int[] posInfo = CalculateLineAndColumnNumbers(source, errorPosition);
            int lineNumber = posInfo[0], columnNumber = posInfo[1];
            string offendingLine = NEW_LINE_REGEX.Split(source)[lineNumber - 1];
            fullMessage.Append("at index ").Append(errorPosition);
            fullMessage.Append(", line ").Append(lineNumber);
            fullMessage.Append(": ").Append(errorMessage).Append("\n\n");
            fullMessage.Append(offendingLine).Append("\n");
            AppendUnderline(fullMessage, offendingLine.Length, columnNumber - 1, ' ', '^');
            throw new Exception(fullMessage.ToString());
        }

        private Exception CreatEndOfStringError(string errorMessage)
        {
            throw new Exception(errorMessage);
        }

        internal static int[] CalculateLineAndColumnNumbers(string s, int position)
        {
            int lineNumber = 1; // NB: line number starts from 1.
            int lastNewLineEnd = 0;
            Match newLineMatcher = NEW_LINE_REGEX.Match(s, lastNewLineEnd);
            while (newLineMatcher.Success)
            {
                if (newLineMatcher.Index + newLineMatcher.Length > position)
                {
                    break;
                }
                lastNewLineEnd = newLineMatcher.Index + newLineMatcher.Length;
                lineNumber++;
                newLineMatcher = NEW_LINE_REGEX.Match(s, lastNewLineEnd);
            }

            // use last match to calculate column number position.
            // NB: column number starts from 1.
            int columnNumber = position - lastNewLineEnd + 1;
            return new int[] { lineNumber, columnNumber };
        }

        static void AppendUnderline(StringBuilder sb, int lineLength, int indexInLine,
                char spacerCP, char positionMarkerCP)
        {
            int i = 0;
            for (; i < indexInLine; i++)
            {
                sb.Append(spacerCP);
            }

            sb.Append(positionMarkerCP);
            for (; i < lineLength - 1; i++)
            {
                sb.Append(spacerCP);
            }
        }

        public List<object> Parse()
        {
            var parts = new List<object>();
            object part;
            while ((part = ParseOnePart()) != null)
            {
                parts.Add(part);
            }
            return parts;
        }

        internal object ParseOnePart()
        {
            string token = NextToken();
            if (token == null)
            {
                return null;
            }
            switch (tokenType)
            {
                case FormatTokenType.LITERAL_STRING_SECTION:
                    return token;
                case FormatTokenType.BEGIN_REPLACEMENT:
                    inReplacementField = true;
                    object part = ParseReplacementField();
                    inReplacementField = false;
                    return part;
                case FormatTokenType.END_REPLACEMENT:
                    throw CreateParseError("Single '}' encountered in format string");
                default:
                    throw CreateParseError("Unexpected token: " + token);
            }
        }

        private string NextToken()
        {
            if (endPos >= source.Length)
            {
                return null;
            }
            startPos = endPos;
            var token = new StringBuilder();
            if (inReplacementField)
            {
                while (endPos < source.Length)
                {
                    char ch = source[endPos];
                    if (ch == '{' || ch == '}' || ch == '.' ||
                            ch == '[' || ch == ']')
                    {
                        break;
                    }
                    token.Append(ch);
                    endPos++;
                }
                if (token.Length > 0)
                {
                    tokenType = FormatTokenType.LITERAL_STRING_SECTION;
                }
                else
                {
                    char ch = source[endPos++];
                    token.Append(ch);
                    switch (ch)
                    {
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
                            throw CreateParseError("Unexpected char: " + ch, true);
                    }
                }
            }
            else
            {
                partStart = startPos;
                while (endPos < source.Length)
                {
                    char ch = source[endPos];
                    if (ch == '{')
                    {
                        if (endPos + 1 < source.Length && source[endPos + 1] == '{')
                        {
                            endPos++; // unescape by skipping one of them.
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (ch == '}')
                    {
                        if (endPos + 1 < source.Length && source[endPos + 1] == '}')
                        {
                            endPos++; // unescape by skipping one of them.
                        }
                        else
                        {
                            break;
                        }
                    }
                    token.Append(ch);
                    endPos++;
                }
                if (token.Length > 0)
                {
                    tokenType = FormatTokenType.LITERAL_STRING_SECTION;
                }
                else
                {
                    char ch = source[endPos++];
                    token.Append(ch);
                    switch (ch)
                    {
                        case '{':
                            tokenType = FormatTokenType.BEGIN_REPLACEMENT;
                            break;
                        case '}':
                            tokenType = FormatTokenType.END_REPLACEMENT;
                            break;
                        default:
                            throw CreateParseError("Unexpected char: " + ch, true);
                    }
                }
            }
            return token.ToString();
        }

        private object ParseReplacementField()
        {
            var treeDataKey = new List<object>();
            while (true)
            {
                string token = NextToken();
                if (token == null)
                {
                    throw CreatEndOfStringError("expected '}' before end of string");
                }
                if (tokenType == FormatTokenType.END_REPLACEMENT)
                {
                    break;
                }
                if (treeDataKey.Count == 0 && tokenType == FormatTokenType.LITERAL_STRING_SECTION)
                {
                    int? notTreeDataKeyButIndex = ParsePositionalIndex(token);
                    if (notTreeDataKeyButIndex != null)
                    {
                        return notTreeDataKeyButIndex.Value;
                    }
                }
                switch (tokenType)
                {
                    case FormatTokenType.LITERAL_STRING_SECTION:
                        token = token.Trim();
                        if (token.Length > 0)
                        {
                            if (treeDataKey.Count == 0)
                            {
                                treeDataKey.Add(token);
                            }
                            else
                            {
                                throw CreateParseError("expected '.', '[' or '}'");
                            }
                        }
                        break;
                    case FormatTokenType.OPENING_SQUARE:
                        int arrayIndex = ParseArrayIndex();
                        treeDataKey.Add(arrayIndex);
                        break;
                    case FormatTokenType.DOT:
                        string propertyName = ParsePropertyName();
                        treeDataKey.Add(propertyName);
                        break;
                    case FormatTokenType.CLOSING_SQUARE:
                        throw CreateParseError("Single ']' encountered in replacement field");
                    case FormatTokenType.BEGIN_REPLACEMENT:
                        throw CreateParseError("invalid '{' in replacement field");
                    default:
                        throw CreateParseError("Unexpected token: " + token);
                }
            }
            return treeDataKey;
        }

        private int? ParsePositionalIndex(string token)
        {
            token = token.Trim();
            if (token.Length == 0)
            {
                return null;
            }
            bool positionalIndexValid = int.TryParse(token, out int positionalIndex);
            if (!positionalIndexValid)
            {
                if (!"0123456789".Contains(token[0]))
                {
                    return null;
                }
                throw CreateParseError("invalid positional index");
            }
            token = NextToken();
            if (token == null)
            {
                throw CreatEndOfStringError("expected '}' before end of string");
            }
            if (tokenType != FormatTokenType.END_REPLACEMENT)
            {
                throw CreateParseError("expected '}'");
            }
            return positionalIndex;
        }

        private string ParsePropertyName()
        {
            string token = NextToken();
            if (token == null)
            {
                throw CreatEndOfStringError("expected property name before end of string");
            }
            if (tokenType != FormatTokenType.LITERAL_STRING_SECTION)
            {
                throw CreateParseError("expected property name");
            }
            // require property name.
            token = token.Trim();
            if (token.Length == 0)
            {
                throw CreateParseError("expected property name");
            }
            return token;
        }

        private int ParseArrayIndex()
        {
            string token = NextToken();
            if (token == null)
            {
                throw CreatEndOfStringError("expected array index before end of string");
            }
            if (tokenType != FormatTokenType.LITERAL_STRING_SECTION)
            {
                throw CreateParseError("expected array index");
            }
            token = token.Trim();
            if (token.Length == 0)
            {
                throw CreateParseError("expected array index");
            }
            bool arrayIndexValid = int.TryParse(token, out int arrayIndex);
            if (!arrayIndexValid)
            {
                throw CreateParseError("invalid array index");
            }
            token = NextToken();
            if (token == null)
            {
                throw CreatEndOfStringError("expected ']' before end of string");
            }
            if (tokenType != FormatTokenType.CLOSING_SQUARE)
            {
                throw CreateParseError("expected ']'");
            }
            return arrayIndex;
        }
    }
}
