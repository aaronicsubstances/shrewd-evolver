using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace AaronicSubstances.ShrewdEvolver
{
    public class LogMessageTemplateParser
    {
        internal enum FormatTokenType
        {
            LITERAL_STRING_SECTION, BEGIN_REPLACEMENT, END_REPLACEMENT, DOT,
            OPENING_SQUARE, CLOSING_SQUARE, STRINGIFY, SERIALIZE,
            AT, DOLLAR, COMMA, COLON
        }

        public class PartDescriptor
        {
            public int startPos;
            public int endPos;
            public string literalSection;
            public int positionalArgIndex;
            public IList<object> treeDataKey;
            public bool serialize;

            public PartDescriptor(int startPos, int endPos, string literalSection)
            {
                this.startPos = startPos;
                this.endPos = endPos;
                this.literalSection = literalSection;
            }

            public PartDescriptor(int startPos, int endPos, int positionalArgIndex):
                this(startPos, endPos, positionalArgIndex, false)
            { }

            public PartDescriptor(int startPos, int endPos, int positionalArgIndex, bool serialize)
            {
                this.startPos = startPos;
                this.endPos = endPos;
                this.positionalArgIndex = positionalArgIndex;
                this.serialize = serialize;
            }

            public PartDescriptor(int startPos, int endPos, IList<object> treeDataKey):
                this(startPos, endPos, treeDataKey, true)
            { }

            public PartDescriptor(int startPos, int endPos, IList<object> treeDataKey, bool serializeTreeData)
            {
                this.startPos = startPos;
                this.endPos = endPos;
                this.treeDataKey = treeDataKey;
                this.serialize = serializeTreeData;
            }

            public override int GetHashCode()
            {
                int hash = 3;
                hash = 67 * hash + this.startPos;
                hash = 67 * hash + this.endPos;
                hash = 67 * hash + (literalSection != null ? literalSection.GetHashCode() : 0);
                hash = 67 * hash + positionalArgIndex;
                if (this.treeDataKey != null)
                {
                    foreach (var treeDataKeyItem in this.treeDataKey)
                    {
                        hash = 67 * hash + treeDataKeyItem.GetHashCode();
                    }
                }
                hash = 67 * hash + (serialize ? 1 : 0);
                return hash;
            }

            public override bool Equals(object obj)
            {
                if (this == obj)
                {
                    return true;
                }
                if (obj == null)
                {
                    return false;
                }
                if (GetType() != obj.GetType())
                {
                    return false;
                }
                var other = (PartDescriptor)obj;
                if (this.startPos != other.startPos)
                {
                    return false;
                }
                if (this.endPos != other.endPos)
                {
                    return false;
                }
                if (!Equals(this.literalSection, other.literalSection))
                {
                    return false;
                }
                if (this.positionalArgIndex != other.positionalArgIndex)
                {
                    return false;
                }
                if (this.treeDataKey == null && other.treeDataKey == null)
                {
                    // continue
                }
                else if (this.treeDataKey == null || other.treeDataKey == null)
                {
                    return false;
                }
                else
                {
                    if (this.treeDataKey.Count != other.treeDataKey.Count)
                    {
                        return false;
                    }
                    for (int i = 0; i < this.treeDataKey.Count; i++)
                    {
                        if (!Equals(this.treeDataKey[i], other.treeDataKey[i]))
                        {
                            return false;
                        }
                    }
                }
                if (this.serialize != other.serialize)
                {
                    return false;
                }
                return true;
            }

            public override string ToString()
            {
                var treeDataKeyRepr = new StringBuilder();
                if (treeDataKey != null)
                {
                    treeDataKeyRepr.Append("[");
                    treeDataKeyRepr.Append(string.Join(", ", treeDataKey));
                    treeDataKeyRepr.Append("]");
                }
                return "PartDescriptor{" + "startPos=" + startPos +
                    ", endPos=" + endPos + ", literalSection=" + literalSection +
                    ", positionalArgIndex=" + positionalArgIndex +
                    ", treeDataKey=" + treeDataKeyRepr +
                    ", serialize=" + serialize + '}';
            }
        }

        private static readonly Regex NEW_LINE_REGEX = new Regex("\r\n|\r|\n");
    
        private readonly string source;

        // Token properties.
        internal int startPos, endPos;
        internal FormatTokenType tokenType;
        internal int partStart;

        private bool _inReplacementField = false;

        public LogMessageTemplateParser(string source)
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

        public List<PartDescriptor> Parse()
        {
            var parts = new List<PartDescriptor>();
            PartDescriptor part;
            while ((part = ParseOnePart()) != null)
            {
                parts.Add(part);
            }
            return parts;
        }

        internal PartDescriptor ParseOnePart()
        {
            string token = NextToken();
            if (token == null)
            {
                return null;
            }
            switch (tokenType)
            {
                case FormatTokenType.LITERAL_STRING_SECTION:
                    return new PartDescriptor(partStart, endPos, token);
                case FormatTokenType.BEGIN_REPLACEMENT:
                case FormatTokenType.STRINGIFY:
                case FormatTokenType.SERIALIZE:
                    _inReplacementField = true;
                    PartDescriptor part = ParseReplacementField(tokenType);
                    _inReplacementField = false;
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
            if (_inReplacementField)
            {
                while (endPos < source.Length)
                {
                    char ch = source[endPos];
                    if (ch == '{' || ch == '}' || ch == '.' ||
                        ch == '[' || ch == ']' || ch == '$' ||
                        ch == '@' || ch == ',' || ch == ':')
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
                        case ':':
                            tokenType = FormatTokenType.COLON;
                            break;
                        case ',':
                            tokenType = FormatTokenType.COMMA;
                            break;
                        case '@':
                            tokenType = FormatTokenType.AT;
                            break;
                        case '$':
                            tokenType = FormatTokenType.DOLLAR;
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
                            if (endPos < source.Length)
                            {
                                switch (source[endPos])
                                {
                                    case '@':
                                        tokenType = FormatTokenType.SERIALIZE;
                                        endPos++;
                                        break;
                                    case '$':
                                        tokenType = FormatTokenType.STRINGIFY;
                                        endPos++;
                                        break;
                                    default:
                                        tokenType = FormatTokenType.BEGIN_REPLACEMENT;
                                        break;
                                }
                            }
                            else
                            {
                                tokenType = FormatTokenType.BEGIN_REPLACEMENT;
                            }
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

        private PartDescriptor ParseReplacementField(FormatTokenType startTokenType)
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
                        // stringify positional arguments by default.
                        return new PartDescriptor(partStart, endPos, notTreeDataKeyButIndex.Value,
                            startTokenType == FormatTokenType.SERIALIZE);
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
                    default:
                        throw CreateParseError("invalid '" + token + "' in replacement field");
                }
            }
            // serialize keyword arguments by default.
            return new PartDescriptor(partStart, endPos, treeDataKey, startTokenType != FormatTokenType.STRINGIFY);
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
                // treat token as keyword if it doesn't start like an integer does.
                if (!"+-0123456789".Contains(token[0]))
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
