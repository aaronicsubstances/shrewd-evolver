using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AaronicSubstances.ShrewdEvolver.MicroOrm
{
    public abstract class BaseResultFieldConfigFunctionParams
    {
        public int RowIndex { get; set; }
        public int TupleIndex { get; set; }
        public int AbsoluteFieldIndex { get; set; }
        public ResultFieldConfig FieldConfig { get; set; }
        public IDictionary<string, object> ExecutionEnvironment { get; set; }
    }
}
