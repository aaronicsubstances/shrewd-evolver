using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AaronicSubstances.ShrewdEvolver.MicroOrm
{
    public struct DbCursorConsumerParams
    {
        public DbDataReader DbCursor { get; set; }
        public int AbsoluteFieldIndex { get; set; }
        public string DestName { get; set; }
        public object DestValueContainer { get; set; }
    }
}
