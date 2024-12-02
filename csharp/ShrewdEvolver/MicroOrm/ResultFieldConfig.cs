using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AaronicSubstances.ShrewdEvolver.MicroOrm
{
    public delegate Task<bool> FieldValueConsumerType(FieldValueConsumerParam arg);

    public delegate Task<bool> DbCursorConsumerType(DbCursorConsumerParams arg);

    /**
     * Intended algorithm is:
     * 
     * this.Id is only needed during merging by ResultSubsetConfig. it's not needed during db cursor
     * iteration.
     * if this.DbCursorConsumer is set, hand over everything to it.
     * done.
     * else...
     * this.FieldType should be set by this time after the merging in ResultSubsetConfig.
     * Use it to determine the appropriate getter to call on the database result cursor to get a cell value.
     * if this.DestConverterType is set, locate an external function that can convert the database cell value
     * into a higher-level runtime type for the application.
     * If this.FieldValueConsumer is set, pass the database cell value to it and be done.
     * Else assume that there is a property to set on the return from the one-time call to
     * ResultSubsetConfig.BareResultSubsetGenerator, which is called this.DestName,
     * and call the setter accordngly and be done.
     */
    public class ResultFieldConfig
    {
        public string Id { get; set; }
        public DbType? FieldType { get; set; }
        public string DestName { get; set; }
        public object DestConverterType { get; set; }
        public IList<FieldValueConsumerType> FieldValueConsumerChain { get; set; }
        public IList<DbCursorConsumerType> DbCursorConsumerChain { get; set; }
        public IDictionary<string, object> Extra { get; set; }
        public bool DiscardFallbackFieldValueConsumerChain { get; set; }
        public bool DiscardFallbackDbCursorConsumerChain { get; set; }

        public ResultFieldConfig MakeDuplicate()
        {
            var duplicate = new ResultFieldConfig
            {
                Id = this.Id,
                FieldType = this.FieldType,
                DestName = this.DestName,
                DestConverterType = this.DestConverterType,
                DiscardFallbackFieldValueConsumerChain = this.DiscardFallbackFieldValueConsumerChain,
                DiscardFallbackDbCursorConsumerChain = this.DiscardFallbackDbCursorConsumerChain,
            };
            if (this.FieldValueConsumerChain?.Any() == true)
            {
                duplicate.FieldValueConsumerChain = new List<FieldValueConsumerType>(
                    this.FieldValueConsumerChain);
            }
            else
            {
                duplicate.FieldValueConsumerChain = new List<FieldValueConsumerType>();
            }
            if (this.DbCursorConsumerChain?.Any() == true)
            {
                duplicate.DbCursorConsumerChain = new List<DbCursorConsumerType>(
                    this.DbCursorConsumerChain);
            }
            else
            {
                duplicate.DbCursorConsumerChain = new List<DbCursorConsumerType>();
            }
            if (this.Extra?.Any() == true)
            {
                duplicate.Extra = new Dictionary<string, object>(this.Extra);
            }
            else
            {
                duplicate.Extra = new Dictionary<string, object>();
            }
            return duplicate;
        }

        public void TransferNonEmptyPropsAsideId(ResultFieldConfig dest)
        {
            if (this.FieldType != null)
            {
                dest.FieldType = this.FieldType;
            }
            if (!string.IsNullOrEmpty(this.DestName))
            {
                dest.DestName = this.DestName;
            }
            if (this.DestConverterType == null)
            {
                dest.DestConverterType = this.DestConverterType;
            }
            dest.FieldValueConsumerChain = MicroOrmHelpers.MergeChains(
                this.FieldValueConsumerChain, dest.FieldValueConsumerChain,
                DiscardFallbackFieldValueConsumerChain);
            dest.DbCursorConsumerChain = MicroOrmHelpers.MergeChains(
                this.DbCursorConsumerChain, dest.DbCursorConsumerChain,
                DiscardFallbackDbCursorConsumerChain);
            if (this.Extra != null)
            {
                if (dest.Extra == null)
                {
                    dest.Extra = this.Extra;
                }
                else
                {
                    foreach (var kvp in this.Extra)
                    {
                        // add or replace
                        dest.Extra[kvp.Key] = kvp.Value;
                    }
                }
            }
        }
    }
}
