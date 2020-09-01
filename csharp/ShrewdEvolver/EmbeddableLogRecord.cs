using System;
using System.Collections.Generic;
using System.Text;

namespace AaronicSubstances.ShrewdEvolver
{
    public abstract class EmbeddableLogRecord
    {
        class TreeDataSliceWrapper
        {
            private readonly EmbeddableLogRecord _logRecord;
            private readonly object _treeDataSlice;
            public TreeDataSliceWrapper(EmbeddableLogRecord logRecord, object treeDataSlice)
            {
                _logRecord = logRecord;
                _treeDataSlice = treeDataSlice;
            }
            public override string ToString()
            {
                return _logRecord.SerializeData(_treeDataSlice);
            }
        }

        private readonly List<object> _positionalArgs;
        private readonly object _treeData;

        // Contents are literal string, index into positional arg, or
        // treeDataKey, which is list of objects as path into treeData.
        // Each part of treeDataKey in turn consists of JSON property name, or index into JSON array.
        private readonly List<object> _parsedFormatString;

        public EmbeddableLogRecord(string formatString, object treeData,
                List<object> positionalArgs)
        {
            _treeData = treeData;
            _positionalArgs = positionalArgs;
            _parsedFormatString = ParseFormatString(formatString);
        }

        public string ToLogFormatString(List<object> formatArgsReceiver)
        {
            return GenerateFormatString(formatArgsReceiver, true);
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
            return ToStructuredLogRecord(_treeData);
        }

        protected virtual object ToStructuredLogRecord(object treeDataSlice)
        {
            return new TreeDataSliceWrapper(this, treeDataSlice);
        }

        protected abstract string SerializeData(object treeDataSlice);

        protected virtual string EscapeLiteral(string literal, bool forLogger)
        {
            return literal.Replace("{", "{{").Replace("}", "}}");
        }

        protected virtual string GeneratePositionIndicator(int position, bool forLogger)
        {
            return "{" + position + "}";
        }

        protected virtual object GetPositionalArg(List<object> args, int index)
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

        protected virtual object HandleNonExistentPositionalArg(int index)
        {
            return null;
        }

        protected virtual object GetTreeDataSlice(object treeData, List<object> treeDataKey)
        {
            object treeDataSlice = treeData;
            foreach (object keyPart in treeDataKey)
            {
                if (keyPart is int) 
                {
                    var index = (int)keyPart;
                    if (!(treeDataSlice is List<object>))
                    {
                        return HandleNonExistentTreeDataSlice(treeDataKey);
                    }
                    var jsonArray = (List<object>)treeDataSlice;
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
                    if (!(treeDataSlice is Dictionary<string, object>))
                    {
                        return HandleNonExistentTreeDataSlice(treeDataKey);
                    }
                    var jsonObject = (Dictionary<string, object>)treeDataSlice;
                    if (!jsonObject.ContainsKey(key))
                    {
                        return HandleNonExistentTreeDataSlice(treeDataKey);
                    }
                    treeDataSlice = jsonObject[key];
                }
            }
            return treeDataSlice;
        }

        protected virtual object HandleNonExistentTreeDataSlice(List<object> treeDataKey)
        {
            return null;
        }

        internal static List<object> ParseFormatString(string formatString)
        {
            return new LogRecordFormatParser(formatString).Parse();
        }

        internal List<object> GetParsedFormatString()
        {
            return _parsedFormatString;
        }

        private string GenerateFormatString(List<object> formatArgsReceiver, bool forLogger)
        {
            var logFormat = new StringBuilder();
            int uniqueIndex = 0;
            foreach (object part in _parsedFormatString)
            {
                if (part is int) 
                {
                    logFormat.Append(GeneratePositionIndicator(uniqueIndex++, forLogger));
                    var position = (int)part;
                    object item = GetPositionalArg(_positionalArgs, position);
                    formatArgsReceiver.Add(item);
                }
                else if (part is List<object>)
                {
                    logFormat.Append(GeneratePositionIndicator(uniqueIndex++, forLogger));
                    var treeDataKey = (List<object>)part;
                    object treeDataSlice = GetTreeDataSlice(_treeData, treeDataKey);
                    formatArgsReceiver.Add(ToStructuredLogRecord(treeDataSlice));
                }
                else
                {
                    logFormat.Append(EscapeLiteral((string)part, forLogger));
                }
            }
            return logFormat.ToString();
        }
    }
}
