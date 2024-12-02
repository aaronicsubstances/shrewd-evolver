using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace AaronicSubstances.ShrewdEvolver.MicroOrm
{
    // Resources:
    // https://github.com/aaberg/sql2o/
    // https://scala-slick.org/doc/3.0.0/orm-to-slick.html
    // https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/sql-server-data-type-mappings
    // https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/configuring-parameters-and-parameter-data-types
    public static class MicroOrmHelpers
    {
        public static object ApplyDefaultParamValueSupplier(object sourceValueContainer, string sourceName)
        {
            if (sourceValueContainer == null)
            {
                throw new ArgumentNullException(nameof(sourceValueContainer));
            }
            var prop = sourceValueContainer.GetType().GetProperty(sourceName);
            if (prop == null)
            {
                throw new Exception($"Property '{sourceName}' not found on object of type {sourceValueContainer.GetType()}");
            }
            return prop.GetValue(sourceValueContainer);
        }

        public static void ApplyDefaultFieldValueConsumer(object destValueContainer, string destName, object value)
        {
            if (destValueContainer == null)
            {
                throw new ArgumentNullException(nameof(destValueContainer));
            }
            var prop = destValueContainer.GetType().GetProperty(destName);
            if (prop == null)
            {
                throw new Exception($"Property '{destName}' not found on object of type {destValueContainer.GetType()}");
            }
            prop.SetValue(destValueContainer, value);
        }

        public static IList<T> MergeChains<T>(IList<T> main, IList<T> fallback, bool discardFallback)
        {
            if (discardFallback || fallback?.Any() != true)
            {
                return main;
            }
            else if (main?.Any() != true)
            {
                return fallback;
            }
            else
            {
                var merged = new List<T>(fallback);
                merged.AddRange(main);
                return merged;
            }
        }

        public static IDictionary<string, object> MergeEnvironments(IDictionary<string, object> main,
            IDictionary<string, object> fallback, bool discardFallback)
        {
            if (discardFallback || fallback?.Any() != true)
            {
                return main;
            }
            else if (main?.Any() != true)
            {
                return fallback;
            }
            else
            {
                var merged = new Dictionary<string, object>(fallback);
                foreach (var kvp in main)
                {
                    // add or replace
                    merged[kvp.Key] = kvp.Value;
                }
                return merged;
            }
        }
    }
}
