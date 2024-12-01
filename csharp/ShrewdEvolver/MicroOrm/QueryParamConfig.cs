using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace AaronicSubstances.ShrewdEvolver.MicroOrm
{
    /*
     * Intended algorithm is:
     * this.ParamId is only needed during merging by QueryParamSourceConfig. it's not needed during db command
     * execution.
     * if this.DbCommandConsumer is set, hand over everything to him. This requires the caller to
     * make parameters available on database command for this.DbCommandConsumer to append to.
     * done.
     * else...
     * if this.ParamValueSupplier is set, use it to get the param value
     * else assume that the param value is a property value on an external object (QueryParamConfig.Value)
     * whose name is this.SourceName.
     * If this.SourceConverterType is set, locate an external converter function to convert from param value
     * to a type hopefully acceptable by database driver.
     * Lastly, determne paramType to use, if this.paramType is not set. This search should disregard converter
     * functions and just use the param value runtime type (even if it is after applying a source converter)
     * to search among the default correspondence between
     * database types and .NET types as listed at 
     * https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/sql-server-data-type-mappings
     */
    public class QueryParamConfig
    {
        public string ParamId { get; set; }
        public string ParamName { get; set; }
        public DbType? ParamType { get; set; }

        public Func<QueryParamValueSupplierParams, Task<object>> ParamValueSupplier { get; set; }
        public Func<DbCommandConsumerParams, Task> DbCommandConsumer { get; set; }

        public string SourceName { get; set; }
        public object SourceConverterType { get; set; }

        public void OverwriteEmptyPropsAsideId(QueryParamConfig dest)
        {
            if (string.IsNullOrEmpty(dest.ParamName))
            {
                dest.ParamName = this.ParamName;
            }
            if (dest.ParamType == null)
            {
                dest.ParamType = this.ParamType;
            }
            if (dest.ParamValueSupplier == null)
            {
                dest.ParamValueSupplier = this.ParamValueSupplier;
            }
            if (dest.DbCommandConsumer == null)
            {
                dest.DbCommandConsumer = this.DbCommandConsumer;
            }
            if (string.IsNullOrEmpty(dest.SourceName))
            {
                dest.SourceName = this.SourceName;
            }
            if (dest.SourceConverterType == null)
            {
                dest.SourceConverterType = this.SourceConverterType;
            }
        }
    }
}
