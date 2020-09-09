using System;
using System.Collections.Generic;
using System.Text;
using static AaronicSubstances.ShrewdEvolver.LogMessageTemplateParser;

namespace AaronicSubstances.ShrewdEvolver
{
    public class LogMessageTemplate
    {
        public class Unstructured
        {
            public Unstructured(string formatString, IList<object> formatArgs)
            {
                FormatString = formatString;
                FormatArgs = formatArgs;
            }
            public string FormatString { get; }
            public IList<object> FormatArgs { get; }
        }

        public class Structured
        {
            private readonly LogMessageTemplate _logRecord;
            private readonly object _treeDataSlice;
            public Structured(LogMessageTemplate logRecord, object treeDataSlice)
            {
                _logRecord = logRecord;
                _treeDataSlice = treeDataSlice;
            }
            public override string ToString()
            {
                return _logRecord.SerializeData(_treeDataSlice);
            }
        }

        private readonly string _rawFormatString;
        private readonly object _keywordArgs;
        private readonly IList<object> _positionalArgs;

        // Contents are literal string, index into positional arg, or
        // treeDataKey, which is list of objects as path into treeData.
        // Each part of treeDataKey in turn consists of JSON property name, or index into JSON array.
        private readonly IList<PartDescriptor> _parsedFormatString;

        public LogMessageTemplate(string formatString, object keywordArgs,
                IList<object> positionalArgs):
            this(formatString, new LogMessageTemplateParser(formatString).Parse(),
                keywordArgs, positionalArgs)
        { }

        public LogMessageTemplate(string rawFormatString, IList<PartDescriptor> parsedFormatString,
            object keywordArgs, IList<object> positionalArgs)
        {
            _rawFormatString = rawFormatString;
            _parsedFormatString = parsedFormatString;
            _keywordArgs = keywordArgs;
            _positionalArgs = positionalArgs;
        }

        public IList<PartDescriptor> ParsedFormatString
        {
            get
            {
                return _parsedFormatString;
            }
        }

        public override string ToString()
        {
            var formatArgs = new List<object>();
            string genericFormatString = GenerateFormatString(formatArgs, false);
            string msg = string.Format(genericFormatString, formatArgs.ToArray());
            return msg;
        }

        public object ToStructuredLogRecord()
        {
            return ToStructuredLogRecord(_keywordArgs);
        }

        public Unstructured ToUnstructuredLogRecord()
        {
            var formatArgs = new List<object>();
            string formatString = GenerateFormatString(formatArgs, true);
            return new Unstructured(formatString, formatArgs);
        }

        protected virtual object ToStructuredLogRecord(object treeDataSlice)
        {
            return new Structured(this, treeDataSlice);
        }

        protected virtual string SerializeData(object treeDataSlice)
        {
            throw new NotImplementedException();
        }

        protected virtual string EscapeLiteral(string literal, bool forLogger)
        {
            return literal.Replace("{", "{{").Replace("}", "}}");
        }

        protected virtual string GeneratePositionIndicator(int position, bool forLogger)
        {
            return "{" + position + "}";
        }

        protected internal virtual object GetPositionalArg(IList<object> args, PartDescriptor part)
        {
            if (args == null)
            {
                return HandleNonExistentPositionalArg(part);
            }
            // Support Python style indexing.
            int index = part.positionalArgIndex;
            if (Math.Abs(index) >= args.Count)
            {
                return HandleNonExistentPositionalArg(part);
            }
            if (index < 0)
            {
                index += args.Count;
            }
            return args[index];
        }

        protected internal virtual object GetTreeDataSlice(object treeData, PartDescriptor part)
        {
            object treeDataSlice = treeData;
            for (int i = 0; i < part.treeDataKey.Count; i++)
            {
                if (treeDataSlice == null)
                {
                    return HandleNonExistentTreeDataSlice(part, i);
                }
                object keyPart = part.treeDataKey[i];
                if (keyPart is int) 
                {
                    var index = (int)keyPart;
                    if (!(treeDataSlice is IList<object>))
                    {
                        treeDataSlice = GetTreeDataListItem(treeDataSlice, index);
                        continue;
                    }
                    var jsonArray = (IList<object>)treeDataSlice;
                    // Support Python style indexing.
                    if (Math.Abs(index) >= jsonArray.Count)
                    {
                        return HandleNonExistentTreeDataSlice(part, i);
                    }
                    if (index < 0)
                    {
                        index += jsonArray.Count;
                    }
                    treeDataSlice = jsonArray[index];
                }
                else
                {
                    var key = (string)keyPart;
                    if (!(treeDataSlice is IDictionary<string, object>))
                    {
                        treeDataSlice = GetTreeDataPropertyValue(treeDataSlice, keyPart);
                        continue;
                    }
                    var jsonObject = (IDictionary<string, object>)treeDataSlice;
                    if (!jsonObject.ContainsKey(key))
                    {
                        return HandleNonExistentTreeDataSlice(part, i);
                    }
                    treeDataSlice = jsonObject[key];
                }
            }
            return treeDataSlice;
        }

        protected virtual object HandleNonExistentTreeDataSlice(PartDescriptor part, int nonExistentIndex)
        {
            return _rawFormatString.Substring(part.startPos, part.endPos - part.startPos);
        }

        protected virtual object HandleNonExistentPositionalArg(PartDescriptor part)
        {
            return _rawFormatString.Substring(part.startPos, part.endPos - part.startPos);
        }

        protected virtual object GetTreeDataListItem(object treeData, int index)
        {
            return null;
        }

        protected virtual object GetTreeDataPropertyValue(object treeData, object propertyName)
        {
            try
            {
                var propHandle = treeData.GetType().GetProperty((string) propertyName);
                return propHandle.GetValue(treeData);
            }
            catch (Exception) 
            {
                return null;
            }
        }

        private string GenerateFormatString(IList<object> formatArgsReceiver, bool forLogger)
        {
            var logFormat = new StringBuilder();
            int uniqueIndex = 0;
            foreach (PartDescriptor part in _parsedFormatString)
            {
                if (part.literalSection != null)
                {
                    logFormat.Append(EscapeLiteral(part.literalSection, forLogger));
                }
                else
                {
                    logFormat.Append(GeneratePositionIndicator(uniqueIndex++, forLogger));
                    object formatArg;
                    if (part.treeDataKey != null)
                    {
                        formatArg = GetTreeDataSlice(_keywordArgs, part);
                    }
                    else
                    {
                        formatArg = GetPositionalArg(_positionalArgs, part);
                    }
                    if (part.serialize)
                    {
                        formatArgsReceiver.Add(ToStructuredLogRecord(formatArg));
                    }
                    else
                    {
                        formatArgsReceiver.Add(formatArg);
                    }
                }
            }
            return logFormat.ToString();
        }
    }
}
