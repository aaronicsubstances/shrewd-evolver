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

        private readonly object _keywordArgs;
        private readonly IList<object> _positionalArgs;

        // Contents are literal string, index into positional arg, or
        // treeDataKey, which is list of objects as path into treeData.
        // Each part of treeDataKey in turn consists of JSON property name, or index into JSON array.
        private readonly IList<PartDescriptor> _parsedFormatString;

        public LogMessageTemplate(string formatString, object keywordArgs,
                IList<object> positionalArgs)
        {
            _keywordArgs = keywordArgs;
            _positionalArgs = positionalArgs;
            _parsedFormatString = ParseFormatString(formatString);
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

        protected virtual object GetPositionalArg(IList<object> args, int index)
        {
            // Support Python style indexing.
            if (Math.Abs(index) >= args.Count)
            {
                return HandleNonExistentPositionalArg(index);
            }
            if (index < 0)
            {
                index += args.Count;
            }
            return args[index];
        }

        protected virtual object GetTreeDataSlice(object treeData, IList<object> treeDataKey)
        {
            object treeDataSlice = treeData;
            foreach (object keyPart in treeDataKey)
            {
                if (treeDataSlice == null)
                {
                    break;
                }
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
                        return HandleNonExistentTreeDataSlice(treeDataKey);
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
                        return HandleNonExistentTreeDataSlice(treeDataKey);
                    }
                    treeDataSlice = jsonObject[key];
                }
            }
            return treeDataSlice;
        }

        protected virtual object HandleNonExistentPositionalArg(int index)
        {
            return null;
        }

        protected virtual object HandleNonExistentTreeDataSlice(IList<object> treeDataKey)
        {
            return null;
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

        internal static IList<PartDescriptor> ParseFormatString(string formatString)
        {
            return new LogMessageTemplateParser(formatString).Parse();
        }

        internal IList<PartDescriptor> GetParsedFormatString()
        {
            return _parsedFormatString;
        }

        private string GenerateFormatString(IList<object> formatArgsReceiver, bool forLogger)
        {
            var logFormat = new StringBuilder();
            int uniqueIndex = 0;
            foreach (PartDescriptor part in _parsedFormatString)
            {
                if (part.treeDataKey != null)
                {
                    logFormat.Append(GeneratePositionIndicator(uniqueIndex++, forLogger));
                    object treeDataSlice = GetTreeDataSlice(_keywordArgs, part.treeDataKey);
                    if (part.serialize)
                    {
                        formatArgsReceiver.Add(ToStructuredLogRecord(treeDataSlice));
                    }
                    else
                    {
                        formatArgsReceiver.Add(treeDataSlice);
                    }
                }
                else if (part.literalSection != null)
                {
                    logFormat.Append(EscapeLiteral(part.literalSection, forLogger));
                }
                else
                {
                    logFormat.Append(GeneratePositionIndicator(uniqueIndex++, forLogger));
                    object item = GetPositionalArg(_positionalArgs, part.positionalArgIndex);
                    if (part.serialize)
                    {
                        formatArgsReceiver.Add(ToStructuredLogRecord(item));
                    }
                    else
                    {
                        formatArgsReceiver.Add(item);
                    }
                }
            }
            return logFormat.ToString();
        }
    }
}
