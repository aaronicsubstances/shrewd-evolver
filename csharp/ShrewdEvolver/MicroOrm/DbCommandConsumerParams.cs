using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AaronicSubstances.ShrewdEvolver.MicroOrm
{
    public class DbCommandConsumerParams : CommonConfigFunctionContext
    {
        public DbCommand DbCommand { get; set; }
        public object SourceValueContainer { get; set; }
    }
}
