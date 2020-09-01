﻿using System;
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
        internal enum TreeNodeType
        {
            OBJECT, ARRAY, NUMBER, BOOLEAN, STRING, NULL
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

        public double RealNumberComparisonTolerance { get; set; } = 1e-6;

        public void AssertEquivalentTo(object actual)
        {
            var pathExpectations = new Dictionary<string, string>();
            string actualDescription = SerializeTreeNode(actual);
            string startPath = "";
            pathExpectations[startPath] = "actual: " + actualDescription;
            AssertEquivalentTo(actual, startPath, pathExpectations);
        }

        protected virtual string SerializeTreeNode(object node)
        {
            return node != null ? node.ToString() : "null";
        }

        protected virtual void WorkOnEquivalenceAssertion(object expected, object actual,
                string pathToActual, Dictionary<string, string> pathExpectations)
        {
            if (expected is TreeDataMatcher)
            {
                // It is up to nested matcher to include full node path and expectations
                // encountered along the way in any assertion error it raises.
                var nestedMatcher = (TreeDataMatcher)expected;
                nestedMatcher.AssertEquivalentTo(actual, pathToActual, pathExpectations);
                return;
            }
            /* Possible errors:
                - types not the same
                - non object, non array and non equal.
                - arrays, but different lengths
                - objects, key not found
            */
            expected = NormalizeTreeNode(expected);
            actual = NormalizeTreeNode(actual);
            TreeNodeType expectedType = GetTreeNodeType(expected);
            TreeNodeType actualType = GetTreeNodeType(actual);
            if (expectedType != actualType)
            {
                // mismatch found
                string message = string.Format("expected type {0} but found {1}",
                    expectedType, actualType);
                ReportMismatch(message, pathToActual, pathExpectations);
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
                        ReportMismatch(message, pathToActual, pathExpectations);
                        continue;
                    }
                    object correspondingExpected = expectedEntry.Value;
                    object correspondingActual = actualMap[expectedEntry.Key];
                    WorkOnEquivalenceAssertion(correspondingExpected, correspondingActual,
                        string.Format("{0}{1}{2}", pathToActual,
                            (pathToActual.Length == 0 ? "" : "."), expectedEntry.Key),
                        pathExpectations);
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
                    ReportMismatch(message, pathToActual, pathExpectations);
                }
                int commonSectionLength = Math.Min(expectedList.Count, actualList.Count);
                for (int i = 0; i < commonSectionLength; i++)
                {
                    object correspondingExpected = expectedList[i];
                    object correspondingActual = actualList[i];
                    WorkOnEquivalenceAssertion(correspondingExpected, correspondingActual,
                        string.Format("{0}[{1}]", pathToActual, i), pathExpectations);
                }
            }
            else
            {
                if (!AreLeafNodesEqual(actual, expected))
                {
                    // mismatch found
                    string message = string.Format("expected [{0}] but found [{1}]",
                        expected, actual);
                    ReportMismatch(message, pathToActual, pathExpectations);
                }
            }
        }

        protected virtual object NormalizeTreeNode(object node)
        {
            return node;
        }

        protected virtual bool AreLeafNodesEqual(object actual, object expected)
        {
            if (actual == null || expected == null)
            {
                return actual == expected;
            }
            else
            {
                if (actual is double)
                {
                    double diff = ((double)actual) - ((double)expected);
                    return Math.Abs(diff) <= RealNumberComparisonTolerance;
                }
                else if (actual is float)
                {
                    double diff = ((float)actual) - ((float)expected);
                    return Math.Abs(diff) <= RealNumberComparisonTolerance;
                }
                else
                {
                    return actual.Equals(expected);
                }
            }
        }

        protected virtual void ReportMismatch(string message, string pathToActual,
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
                Dictionary<string, string> pathExpectations)
        {
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
            WorkOnEquivalenceAssertion(_expected, actual, pathToActual, pathExpectations);
        }

        private string GetExpectedDescription()
        {
            if (_expectedDescription != null)
            {
                return _expectedDescription;
            }
            return SerializeTreeNode(_expected);
        }

        private TreeNodeType GetTreeNodeType(object node)
        {
            if (node == null)
            {
                return TreeNodeType.NULL;
            }
            if (IsNumber(node))
            {
                // double, long, decimal
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
            throw new Exception("Unsupported node type: " + node.GetType());
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
    }
}