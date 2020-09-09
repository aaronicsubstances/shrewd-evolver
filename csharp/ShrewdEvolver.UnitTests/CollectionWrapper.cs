using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace AaronicSubstances.ShrewdEvolver.UnitTests
{
    public class CollectionWrapper
    {
        private readonly ICollection _wrapped;
        public CollectionWrapper(ICollection wrapped)
        {
            _wrapped = wrapped;
        }

        public override bool Equals(object obj)
        {
            return ComputeEquals(_wrapped, obj);
        }

        public override int GetHashCode()
        {
            return ComputeHashCode(_wrapped);
        }

        public override string ToString()
        {
            return ComputeStringRepr(_wrapped);
        }

        public static bool ComputeEquals(object obj1, object obj2)
        {
            if (obj1 is CollectionWrapper)
            {
                obj1 = ((CollectionWrapper)obj1)._wrapped;
            }
            if (obj2 is CollectionWrapper)
            {
                obj2 = ((CollectionWrapper)obj2)._wrapped;
            }
            if (!(obj1 is ICollection && obj2 is ICollection))
            {
                return (obj1 is null ? obj2 is null : obj1.Equals(obj2));
            }

            var collection1 = (ICollection)obj1;
            var collection2 = (ICollection)obj2;
            if (collection1.Count != collection2.Count)
            {
                return false;
            }
            var iterator1 = collection1.GetEnumerator();
            var iterator2 = collection2.GetEnumerator();
            while (iterator1.MoveNext() && iterator2.MoveNext())
            {
                var item1 = iterator1.Current;
                var item2 = iterator2.Current;
                if (!ComputeEquals(item1, item2))
                {
                    return false;
                }
            }
            return true;
        }

        public static int ComputeHashCode(object obj)
        {
            if (obj is CollectionWrapper)
            {
                obj = ((CollectionWrapper)obj)._wrapped;
            }
            if (!(obj is ICollection))
            {
                return obj is null ? 0 : obj.GetHashCode();
            }
            var collection = (ICollection)obj;
            int hashCode = 1;
            foreach (object item in collection)
            {
                var itemHash = ComputeHashCode(item);
                hashCode = 31 * hashCode + itemHash;
            }
            return hashCode;
        }

        public static string ComputeStringRepr(object obj)
        {
            if (obj is CollectionWrapper)
            {
                obj = ((CollectionWrapper)obj)._wrapped;
            }
            if (!(obj is ICollection))
            {
                return obj is null ? "" : obj.ToString();
            }
            var collection = (ICollection)obj;
            var str = new StringBuilder();
            var iterationStarted = false;
            var iterator = collection.GetEnumerator();
            while (iterator.MoveNext())
            {
                var item = iterator.Current;
                if (iterationStarted)
                {
                    str.Append(", ");
                }
                iterationStarted = true;
                if (item != null)
                {
                    if (item.GetType().FullName.StartsWith(typeof(KeyValuePair).FullName))
                    {
                        dynamic d = item;
                        str.Append(d.Key);
                        str.Append("=");
                        string dictVal = ComputeStringRepr(d.Value);
                        str.Append(dictVal);
                    }
                    else
                    {
                        str.Append(ComputeStringRepr(item));
                    }
                }
            }
            if (Regex.IsMatch(collection.GetType().FullName, "Dictionary|KeyValuePair"))
            {
                return $"{{{str}}}";
            }
            else
            {
                return $"[{str}]";
            }
        }
    }
}
