using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AaronicSubstances.ShrewdEvolver.MicroOrm
{
    public struct FieldValueConsumerParam
    {
        public string DestName { get; set; }
        public object DestValue { get; set; }
        public object DestValueContainer { get; set; }
    }
}
