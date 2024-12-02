using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AaronicSubstances.ShrewdEvolver.MicroOrm
{
    public class CommonConfigFunctionContext
    {
        public int RowIndex { get; set; }
        public int TupleIndex { get; set; }
        public int AbsoluteParamIndex { get; set; }
        public QueryParamConfig ParamConfig { get; set; }
        public int AbsoluteFieldIndex { get; set; }
        public ResultFieldConfig FieldConfig { get; set; }
        public IDictionary<string, object> ExecutionEnvironment { get; set; }
    }
}
