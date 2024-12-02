using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AaronicSubstances.ShrewdEvolver.MicroOrm
{
    public delegate Task<object[]> ParamValueSupplierType(QueryParamValueSupplierParams arg);

    public delegate Task<bool> DbCommandConsumerType(DbCommandConsumerParams arg);

    /*
     * Intended algorithm is:
     * this.Id is only needed during merging by QueryParamSourceConfig. It's not needed during db command
     * execution.
     * if this.DbCommandConsumer is set, hand over everything to it. This requires the caller to
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
        public string Id { get; set; }
        public string ParamName { get; set; }
        public DbType? ParamType { get; set; }
        public string SourceName { get; set; }
        public object SourceConverterType { get; set; }
        public IList<ParamValueSupplierType> ParamValueSupplierChain { get; set; }
        public IList<DbCommandConsumerType> DbCommandConsumerChain { get; set; }
        public IDictionary<string, object> Extra { get; set; }
        public bool DiscardFallbackParamName { get; set; }
        public bool DiscardFallbackParamType { get; set; }
        public bool DiscardFallbackSourceName { get; set; }
        public bool DiscardFallbackSourceConverterType { get; set; }
        public bool DiscardFallbackParamValueSupplierChain { get; set; }
        public bool DiscardFallbackDbCommandConsumerChain { get; set; }
        public bool DiscardFallbackExtra { get; set; }

        public QueryParamConfig MakeDuplicate()
        {
            var duplicate = new QueryParamConfig
            {
                Id = this.Id,
                ParamName = this.ParamName,
                ParamType = this.ParamType,
                SourceName = this.SourceName,
                SourceConverterType = this.SourceConverterType,
                ParamValueSupplierChain = this.ParamValueSupplierChain,
                DbCommandConsumerChain = this.DbCommandConsumerChain,
                Extra = this.Extra,
                DiscardFallbackParamName = this.DiscardFallbackParamName,
                DiscardFallbackParamType = this.DiscardFallbackParamType,
                DiscardFallbackSourceName = this.DiscardFallbackSourceName,
                DiscardFallbackSourceConverterType = this.DiscardFallbackSourceConverterType,
                DiscardFallbackParamValueSupplierChain = this.DiscardFallbackParamValueSupplierChain,
                DiscardFallbackDbCommandConsumerChain = this.DiscardFallbackDbCommandConsumerChain,
                DiscardFallbackExtra = this.DiscardFallbackExtra
            };
            return duplicate;
        }

        public void TransferNonEmptyPropsAsideId(QueryParamConfig dest)
        {
            if (DiscardFallbackParamName || !string.IsNullOrEmpty(this.ParamName))
            {
                dest.ParamName = this.ParamName;
            }
            if (DiscardFallbackParamType || this.ParamType != null)
            {
                dest.ParamType = this.ParamType;
            }
            if (DiscardFallbackSourceName || !string.IsNullOrEmpty(this.SourceName))
            {
                dest.SourceName = this.SourceName;
            }
            if (DiscardFallbackSourceConverterType || this.SourceConverterType != null)
            {
                dest.SourceConverterType = this.SourceConverterType;
            }
            dest.ParamValueSupplierChain = MicroOrmHelpers.MergeChains(
                this.ParamValueSupplierChain, dest.ParamValueSupplierChain,
                DiscardFallbackParamValueSupplierChain);
            dest.DbCommandConsumerChain = MicroOrmHelpers.MergeChains(
                this.DbCommandConsumerChain, dest.DbCommandConsumerChain,
                DiscardFallbackDbCommandConsumerChain);
            dest.Extra = MicroOrmHelpers.MergeEnvironments(this.Extra,
                dest.Extra, DiscardFallbackExtra);

            dest.DiscardFallbackParamName = false;
            dest.DiscardFallbackParamType = false;
            dest.DiscardFallbackSourceName = false;
            dest.DiscardFallbackSourceConverterType = false;
            dest.DiscardFallbackParamValueSupplierChain = false;
            dest.DiscardFallbackDbCommandConsumerChain = false;
            dest.DiscardFallbackExtra = false;
        }
    }
}
