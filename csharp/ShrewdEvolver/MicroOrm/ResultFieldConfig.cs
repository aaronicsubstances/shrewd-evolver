using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;

namespace AaronicSubstances.ShrewdEvolver.MicroOrm
{
    /**
     * Intended algorithm is:
     * 
     * this.FieldId is only needed during merging by ResultSubsetConfig. it's not needed during db cursor
     * iteration.
     * if this.DbCursorConsumer is set, hand over everything to him.
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
        public string FieldId { get; set; }
        public DbType? FieldType { get; set; }

        public string DestName { get; set; }
        public object DestConverterType { get; set; }

        public Func<FieldValueConsumerParam, Task> FieldValueConsumer { get; set; }
        public Func<DbCursorConsumerParams, Task> DbCursorConsumer { get; set; }

        public void OverwriteEmptyPropsAsideId(ResultFieldConfig dest)
        {
            if (dest.FieldType == null)
            {
                dest.FieldType = this.FieldType;
            }
            if (string.IsNullOrEmpty(dest.DestName))
            {
                dest.DestName = this.DestName;
            }
            if (dest.DestConverterType == null)
            {
                dest.DestConverterType = this.DestConverterType;
            }
            if (dest.FieldValueConsumer == null)
            {
                dest.FieldValueConsumer = this.FieldValueConsumer;
            }
            if (dest.DbCursorConsumer == null)
            {
                dest.DbCursorConsumer = this.DbCursorConsumer;
            }
        }
    }
}
