using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("AaronicSubstances.ShrewdEvolver.UnitTests")]

namespace AaronicSubstances.ShrewdEvolver
{
    public class TreeDataMatcher
    {
        public enum TreeNodeType
        {
            OBJECT, ARRAY, NUMBER, BOOLEAN, STRING, NULL
        }

        public static TreeNodeType? GetTreeNodeType(object node, bool validate)
        {
            if (node == null)
            {
                return TreeNodeType.NULL;
            }
            if (IsNumber(node))
            {
                return TreeNodeType.NUMBER;
            }
            if (node is bool)
            {
                return TreeNodeType.BOOLEAN;
            }
            if (node is string)
            {
                return TreeNodeType.STRING;
            }
            if (node is IList<object>)
            {
                return TreeNodeType.ARRAY;
            }
            if (node is IDictionary<string, object>)
            {
                return TreeNodeType.OBJECT;
            }
            if (!validate)
            {
                return null;
            }
            throw new Exception("Unsupported node type: " + node.GetType());
        }

        private readonly object _expected;
        private readonly string _expectedDescription;

        public TreeDataMatcher(object expected):
            this(expected, false)
        {}

        public TreeDataMatcher(object expected, bool generateDescription):
            this(expected, generateDescription ? null : "")
        {}

        public TreeDataMatcher(object expected, string expectedDescription)
        {
            _expected = expected;
            _expectedDescription = expectedDescription;
        }

        public void AssertEquivalentTo(object actual)
        {
            AssertEquivalentTo(actual, 10);
        }

        public void AssertEquivalentTo(object actual, int maxRecursionDepth)
        {
            var pathExpectations = new Dictionary<string, string>();
            string actualDescription = SerializeTreeNode(actual);
            string startPath = "";
            pathExpectations[startPath] = "actual: " + actualDescription;
            AssertEquivalentTo(actual, startPath, pathExpectations, maxRecursionDepth);
        }

        protected virtual string SerializeTreeNode(object node)
        {
            return node != null ? node.ToString() : "null";
        }

        protected virtual void WorkOnEquivalenceAssertion(object expected, object actual,
                string pathToActual, Dictionary<string, string> pathExpectations,
                int recursionDepthRemaining)
        {
            if (expected is TreeDataMatcher)
            {
                // It is up to nested matcher to include full node path and expectations
                // encountered along the way in any assertion error it raises.
                var nestedMatcher = (TreeDataMatcher)expected;
                nestedMatcher.AssertEquivalentTo(actual, pathToActual, pathExpectations,
                    recursionDepthRemaining - 1);
                return;
            }
            /* Possible errors:
                - types not the same
                - non object, non array and non equal.
                - arrays, but different lengths
                - objects, key not found
            */
            TreeNodeType? expectedType = GetTreeNodeType(expected, false);
            if (expectedType == null)
            {
                expected = NormalizeTreeNode(expected);
                expectedType = GetTreeNodeType(expected, true);
            }
            TreeNodeType? actualType = GetTreeNodeType(actual, false);
            if (actualType == null)
            {
                actual = NormalizeTreeNode(actual);
                actualType = GetTreeNodeType(actual, true);
            }
            if (expectedType != actualType)
            {
                // mismatch found
                string message = string.Format("expected type {0} but found {1}",
                    expectedType, actualType);
                ReportError(message, pathToActual, pathExpectations);
            }
            else if (expectedType == TreeNodeType.OBJECT)
            {
                var expectedMap = (IDictionary<string, object>) expected;
                var actualMap = (IDictionary<string, object>) actual;
                foreach (KeyValuePair<string, object> expectedEntry in expectedMap)
                {
                    if (!actualMap.ContainsKey(expectedEntry.Key))
                    {
                        // mismatch found
                        string message = string.Format("expected object property [{0}] but was not found",
                            expectedEntry.Key);
                        ReportError(message, pathToActual, pathExpectations);
                        continue;
                    }
                    object correspondingExpected = expectedEntry.Value;
                    object correspondingActual = actualMap[expectedEntry.Key];
                    WorkOnEquivalenceAssertion(correspondingExpected, correspondingActual,
                        string.Format("{0}{1}{2}", pathToActual,
                            (pathToActual.Length == 0 ? "" : "."), expectedEntry.Key),
                        pathExpectations, recursionDepthRemaining - 1);
                }
            }
            else if (expectedType == TreeNodeType.ARRAY)
            {
                var expectedList = (IList<object>) expected;
                var actualList = (IList<object>) actual;
                if (expectedList.Count != actualList.Count)
                {
                    // mismatch found
                    string message = string.Format("expected array length {0} but found {1}",
                        expectedList.Count, actualList.Count);
                    ReportError(message, pathToActual, pathExpectations);
                }
                int commonSectionLength = Math.Min(expectedList.Count, actualList.Count);
                for (int i = 0; i < commonSectionLength; i++)
                {
                    object correspondingExpected = expectedList[i];
                    object correspondingActual = actualList[i];
                    WorkOnEquivalenceAssertion(correspondingExpected, correspondingActual,
                        string.Format("{0}[{1}]", pathToActual, i), pathExpectations,
                        recursionDepthRemaining - 1);
                }
            }
            else
            {
                if (!AreLeafNodesEqual(actual, expected))
                {
                    // mismatch found
                    string message = string.Format("expected [{0}] but found [{1}]",
                        expected, actual);
                    ReportError(message, pathToActual, pathExpectations);
                }
            }
        }

        protected virtual object NormalizeTreeNode(object node)
        {
            var props = new Dictionary<string, object>();
            foreach (var p in node.GetType().GetProperties())
            {
                props[p.Name] = p.GetValue(node);
            }
            return props;
        }

        private bool AreLeafNodesEqual(object actual, object expected)
        {
            if (actual == null || expected == null)
            {
                return actual == expected;
            }
            if (!IsNumber(actual)) 
            {
                return actual.Equals(expected);
            }

            // At this stage leaf nodes are numbers convertible to the following:
            // integer, floating point, decimal.
            // To check, 
            //   a. if floating point is involved, convert to double and use tolerance to compare.
            //   b. if actual and expected are of the same type, then compare directly.
            //   c. else if big decimal is involved, convert to big decimal and compare.
            //   d. else convert to big integer and compare
            if (IsFloatingPoint(actual) || IsFloatingPoint(expected))
            {
                double actualFloat = ConvertToFloatingPoint(actual);
                double expectedFloat = ConvertToFloatingPoint(expected);
                return AreFloatingPointNumbersCloseEnough(actualFloat, expectedFloat);
            }
            else if (actual.GetType() == expected.GetType())
            {
                return actual.Equals(expected);
            }
            else if (actual is decimal || expected is decimal)
            {
                var decimals = new List<decimal>();
                foreach (object op in new object[] { actual, expected })
                {
                    if (op is decimal)
                    {
                        decimals.Add((decimal)op);
                    }
                    else if (op is BigInteger)
                    {
                        decimals.Add((decimal)((BigInteger) op));
                    }
                    else
                    {
                        decimals.Add(ConvertToDecimal(op));
                    }
                }
                return decimals[0] == decimals[1];
            }
            else
            {
                var integers = new List<BigInteger>();
                foreach (object op in new object[]{ actual, expected }) 
                {
                    if (op is BigInteger)
                    {
                        integers.Add((BigInteger) op);
                    }
                    else
                    {
                        integers.Add(ConvertToInteger(op));
                    }
                }
                return integers[0] == integers[1];
            }
        }

        protected bool AreFloatingPointNumbersCloseEnough(double actual, double expected)
        {
            double diff = Math.Abs(actual - expected);
            return diff <= 1e-6;
        }

        protected virtual void ReportError(string message, string pathToActual,
                Dictionary<string, string> pathExpectations)
        {
            message = WrapAssertionError(message, pathToActual, pathExpectations);
            throw new Exception(message);
        }

        public static string WrapAssertionError(string message, string pathToActual,
                Dictionary<string, string> pathExpectations)
        {
            var fullMessage = new StringBuilder();
            fullMessage.Append("at {").Append(pathToActual).Append("}: ");
            fullMessage.Append(message);
            if (pathExpectations.Count > 0)
            {
                fullMessage.Append("\n\n");
                var title = "Match Attempt Details";
                fullMessage.Append(title).Append("\n");
                for (int i = 0; i < title.Length; i++)
                {
                    fullMessage.Append("-");
                }
                fullMessage.Append("\n");
                var expKeys = new List<string>(pathExpectations.Keys)
                    .OrderBy(x => x.Length);
                foreach (string expPath in expKeys)
                {
                    fullMessage.Append("  at {").Append(expPath).Append("}: ");
                    fullMessage.Append(pathExpectations[expPath]).Append("\n");
                }
            }
            return fullMessage.ToString();
        }

        private void AssertEquivalentTo(object actual, string pathToActual,
                Dictionary<string, string> pathExpectations, int recursionDepthRemaining)
        {
            if (recursionDepthRemaining <= 0)
            {
                ReportError("Maximum recursion depth reached", pathToActual, pathExpectations);
                return;
            }
            string expectation = GetExpectedDescription();
            if (expectation != null && expectation.Length > 0)
            {
                // For correctness throughout recursive calls, recreate rather
                // than modify in place.
                pathExpectations = new Dictionary<string, string>(pathExpectations);
                string previous = "";
                if (pathExpectations.ContainsKey(pathToActual))
                {
                    previous = pathExpectations[pathToActual] + "; ";
                }
                pathExpectations[pathToActual] = previous + "expected: " + expectation;
            }
            WorkOnEquivalenceAssertion(_expected, actual, pathToActual, pathExpectations, recursionDepthRemaining);
        }

        private string GetExpectedDescription()
        {
            if (_expectedDescription != null)
            {
                return _expectedDescription;
            }
            return SerializeTreeNode(_expected);
        }

        internal static bool IsNumber(object value)
        {
            return value is sbyte
                    || value is byte
                    || value is short
                    || value is ushort
                    || value is int
                    || value is uint
                    || value is long
                    || value is ulong
                    || value is float
                    || value is double
                    || value is decimal
                    || value is BigInteger;
        }

        private static bool IsFloatingPoint(object number)
        {
            return number is double || number is float;
        }

        private static double ConvertToFloatingPoint(object number)
        {
            double ans;
            if (number is sbyte)
            {
                ans = (sbyte)number;
            }
            else if (number is byte)
            {
                ans = (byte)number;
            }
            else if (number is short)
            {
                ans = (short)number;
            }
            else if (number is ushort)
            {
                ans = (ushort)number;
            }
            else if (number is int)
            {
                ans = (int)number;
            }
            else if (number is uint)
            {
                ans = (uint)number;
            }
            else if (number is long)
            {
                ans = (long)number;
            }
            else if (number is ulong)
            {
                ans = (ulong)number;
            }
            else if (number is float)
            {
                ans = (float)number;
            }
            else if (number is double)
            {
                ans = (double)number;
            }
            else if (number is decimal)
            {
                ans = (double)((decimal)number);
            }
            else
            {
                ans = (double)((BigInteger)number);
            }
            return ans;
        }

        private static decimal ConvertToDecimal(object number)
        {
            var instance = (decimal)Activator.CreateInstance(typeof(decimal), number);
            return instance;
        }

        private static BigInteger ConvertToInteger(object number)
        {
            var instance = (BigInteger)Activator.CreateInstance(typeof(BigInteger), number);
            return instance;
        }
    }
}
