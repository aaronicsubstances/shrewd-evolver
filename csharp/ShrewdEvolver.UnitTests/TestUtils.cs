using System;
using System.Collections.Generic;
using System.Text;

namespace AaronicSubstances.ShrewdEvolver.UnitTests
{
    static class TestUtils
    {
        public static Dictionary<string, object> ToDict(params object[] args)
        {
            var m = new Dictionary<string, object>();
            for (int i = 0; i < args.Length; i += 2)
            {
                string key = args[i]?.ToString();
                object value = (i + 1) < args.Length ? args[i + 1] : null;
                m[key] = value;
            }
            return m;
        }

        public static List<object> ToList(params object[] args)
        {
            var list = new List<object>(args);
            return list;
        }
    }
}
