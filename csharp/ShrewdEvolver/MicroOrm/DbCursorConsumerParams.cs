using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AaronicSubstances.ShrewdEvolver.MicroOrm
{
    public class DbCursorConsumerParams : BaseResultFieldConfigFunctionParams
    {
        public DbDataReader DbCursor { get; set; }
        public object DestValueContainer { get; set; }
    }
}
