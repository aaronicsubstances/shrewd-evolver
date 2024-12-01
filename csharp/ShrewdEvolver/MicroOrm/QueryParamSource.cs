using System;
using System.Collections.Generic;
using System.Text;

namespace AaronicSubstances.ShrewdEvolver.MicroOrm
{
    public class QueryParamSource
    {
        /// <summary>
        /// NB: it is possible to use this property without Config property below being set.
        /// It will be interpreted as an attempt to directly set the value of a database command
        /// parameter in a format immediately acceptable by database driver.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// NB: this property is to be ignored if Value property is null.
        /// </summary>
        public QueryParamSourceConfig Config { get; set; }
    }
}
