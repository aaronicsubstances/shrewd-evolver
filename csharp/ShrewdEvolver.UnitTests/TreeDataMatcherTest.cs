using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using static AaronicSubstances.ShrewdEvolver.UnitTests.TestUtils;

namespace AaronicSubstances.ShrewdEvolver.UnitTests
{
    public class TreeDataMatcherTest
    {
        class ExtendedTreeDataMatcher : TreeDataMatcher
        {
            public ExtendedTreeDataMatcher(object expected) :
                base(expected)
            { }

            public ExtendedTreeDataMatcher(object expected, bool generateDescription) :
                base(expected, generateDescription)
            { }

            public ExtendedTreeDataMatcher(object expected, string expectedDescription) :
               base(expected, expectedDescription)
            { }

            protected override string SerializeTreeNode(object node)
            {
                return JsonConvert.SerializeObject(node);
            }

            protected override void ReportMismatch(string message, string pathToActual, Dictionary<string, string> pathExpectations)
            {
                throw new XunitException(WrapAssertionError(message, pathToActual, pathExpectations));
            }
        }

        class AnyNonNullMatcher : ExtendedTreeDataMatcher
        {
            public AnyNonNullMatcher():
                base(null, "not null")
            { }

            protected override void WorkOnEquivalenceAssertion(object expected, object actual, string pathToActual,
                    Dictionary<string, string> pathExpectations)
            {
                if (actual == null)
                {
                    ReportMismatch("is null", pathToActual, pathExpectations);
                }
            }
        }

        private readonly ITestOutputHelper _output;

        public TreeDataMatcherTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestIsNumber()
        {
            Assert.True(TreeDataMatcher.IsNumber(6));
            Assert.True(TreeDataMatcher.IsNumber(6f));
            Assert.True(TreeDataMatcher.IsNumber(6L));
            Assert.True(TreeDataMatcher.IsNumber(6m));
            Assert.True(TreeDataMatcher.IsNumber(6d));
            Assert.True(TreeDataMatcher.IsNumber(new BigInteger(6)));
            Assert.False(TreeDataMatcher.IsNumber("6"));
        }

        [Theory]
        [MemberData(nameof(CreateTestEquivalenceData))]
        public void TestEquivalence(object expected, object actual)
        {
            var instance = new ExtendedTreeDataMatcher(expected);
            instance.AssertEquivalentTo(actual);
        }

        public static List<object[]> CreateTestEquivalenceData()
        {
            return new List<object[]>
            {
                new object[]{ null, null },
                new object[]{ true, true },
                new object[]{ 7, 7 },
                new object[]{ BigInteger.Parse("400"), BigInteger.Parse("400") },
                new object[]{ decimal.Parse("400.89"), decimal.Parse("400.89") },
                new object[]{ 9.8818d, 9.8818d },
                new object[]{ 9.2f, 9.2f },
                new object[]{ 8, 8L },
                new object[]{ 8, 8f },
                new object[]{ 8.2f, 8.2d },
                new object[]{ BigInteger.Parse("400"), 400 },
                new object[]{ decimal.Parse("400.89"), 400.89 },
                new object[]{ "str", "str" },
                new object[]{ ToList(2, "three"), ToList(2, "three") },
                new object[]
                {
                    ToList(ToList(2), ToList("three")),
                    ToList(ToList(2), ToList("three"))
                },
                new object[]{ ToDict("three", 2), ToDict("three", 2) },
                new object[]{ ToDict("three", 3), ToDict("three", 3, "four", 4) },
                new object[]
                {
                    ToDict("three", ToDict("three", 3, "four", 4), "four", ToDict("one", 3, "four", 4)),
                    ToDict("three", ToDict("three", 3, "four", 4), "four", ToDict("one", 3, "four", 4),
                        "sum", true)
                },
                new object[]{ new ExtendedTreeDataMatcher(ToList(1, 2)), ToList(1, 2) },
                new object[]{ new AnyNonNullMatcher(), ToList(2, true, "sum", ToDict()) },
                new object[]{ new AnyNonNullMatcher(), new StringBuilder() },
                new object[]{ ToList(2, true, "sum", new AnyNonNullMatcher()), ToList(2, true, "sum", ToDict()) }
            };
        }

        [Theory]
        [MemberData(nameof(CreateTestNonEquivalenceData))]
        public void TestNonEquivalence(int index, object expected, object actual)
        {
            bool testPassed;
            try
            {
                var instance = new ExtendedTreeDataMatcher(expected);
                instance.AssertEquivalentTo(actual);
                testPassed = false;
            }
            catch (XunitException ex)
            {
                _output.WriteLine((index + 1) + ". " + ex);
                testPassed = true;
            }
            Assert.True(testPassed, "Expected non equivalence");
        }

        public static List<object[]> CreateTestNonEquivalenceData()
        {
            int index = 0;
            return new List<object[]>
            {
                new object[]{ index++, true, false },
                new object[]{ index++, 8, 7 },
                new object[]{ index++, 8, "8" },
                new object[]{ index++, 8, null },
                new object[]{ index++, "street", "str" },
                new object[]{ index++, 9.8818d, 9.8819d },
                new object[]{ index++, new BigInteger(400), false },
                new object[]{ index++, new BigInteger(400), new BigInteger(401) },
                new object[]{ index++, 400.89m, 400.19m },
                new object[]{ index++, ToList(1, 3, "three"), ToList(2, "three") },
                new object[]{ index++, ToList(3, "three"), ToList(2, "three") },
                new object[]{ index++, ToList(ToList(2), ToList("three")), ToList(ToList(2), ToList(false)) },
                new object[]{ index++, ToList(ToList(2), ToList("three")), ToList(ToList(2), ToList("three", false)) },
                new object[]{ index++, ToDict("three", 2), ToDict("four", 2) },
                new object[]{ index++, ToDict("three", 2, "it", "seen"), ToDict("three", 3, "four", 4) },
                new object[]{ index++, ToDict(), ToList() },
                new object[]{ index++, new ExtendedTreeDataMatcher(ToList(), "empty list"), ToDict() },
                new object[]{ index++, new ExtendedTreeDataMatcher(ToList(), true), ToList(ToDict()) },
                new object[]{
                    index++,
                    ToDict("three", ToDict("three", 3, "four", 4), "four", ToDict("one", 3, "four", 4)),
                    ToDict("three", ToDict("three", 3, "four", 5), "four", ToDict("one", 3, "four", 4),
                        "sum", true)
                },
                new object[]{ index++, ToList(2, true, "sum", new AnyNonNullMatcher()), ToList(2, true, "sum", null) },
                new object[]{ index++, ToDict("2", true, "sum", new AnyNonNullMatcher()), ToDict("2", true, "sum", null) },
                new object[]{ index++, new AnyNonNullMatcher(), null },
            };
        }
    }
}
