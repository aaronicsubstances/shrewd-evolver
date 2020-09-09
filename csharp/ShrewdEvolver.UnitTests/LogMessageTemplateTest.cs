using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using static AaronicSubstances.ShrewdEvolver.LogMessageTemplateParser;
using static AaronicSubstances.ShrewdEvolver.UnitTests.TestUtils;

namespace AaronicSubstances.ShrewdEvolver.UnitTests
{
    public class LogMessageTemplateTest
    {
        public class EmbeddableLogRecordImpl: LogMessageTemplate
        {

            public EmbeddableLogRecordImpl(string formatString, object treeData, List<object> positionalArgs):
                base(formatString, treeData, positionalArgs)
            { }

            protected override string SerializeData(object treeDataSlice)
            {
                return JsonConvert.SerializeObject(treeDataSlice);
            }
        }

        public class TestLogRecordImp2 : LogMessageTemplate
        {

            public TestLogRecordImp2(string formatString, object treeData, List<object> positionalArgs) :
                base(formatString, treeData, positionalArgs)
            { }

            protected override object HandleNonExistentTreeDataSlice(PartDescriptor part, int nonExistentIndex)
            {
                return null;
            }

            protected override object HandleNonExistentPositionalArg(PartDescriptor part)
            {
                return null;
            }
        }

        [Theory]
        [MemberData(nameof(CreatTestGetTreeDataSliceData))]
        public void TestGetTreeDataSlice(object treeData, List<object> treeDataKey, object expected)
        {
            LogMessageTemplate instance = new TestLogRecordImp2("", treeData, null);
            var part = new PartDescriptor(0, 0, treeDataKey);
            object actual = instance.GetTreeDataSlice(treeData, part);
            Assert.Equal(actual, expected);
        }

        public static List<object[]> CreatTestGetTreeDataSliceData()
        {
            return new List<object[]>
            {
                new object[]{ null, ToList(), null },
                new object[]{ "", ToList(), "" },
                new object[]{ 2, ToList(), 2 },
                new object[]{ ToList(2), ToList("f"), null },
                new object[]{ ToList(2), ToList(0), 2 },
                new object[]{ ToList(21, ToList()), ToList(10), null },
                new object[]{ ToList(21, ToList()), ToList(0), 21 },
                new object[]{ ToList(21, ToList()), ToList(1), ToList() },
                new object[]{ ToList(21, ToList()), ToList(-1), ToList() },
                new object[]{ ToList(21, ToList()), ToList(-2), null },
                new object[]{ ToDict("a", 1), ToList(), ToDict("a", 1) },
                new object[]{ ToDict("a", 1), ToList(0), null },
                new object[]{ ToDict("a", 1), ToList("a"), 1 },
                new object[]{ ToDict("a", 1, "b", 2), ToList("b"), 2 },
                new object[]{ ToDict("a", 1, "b", 2), ToList("c"), null },
                new object[]{ ToDict("a", 1, "b", ToList("e", true)), ToList("b", 0), "e" },
                new object[]{ ToDict("a", 1, "b", ToList("e", true)), ToList("b", 1), true },
                new object[]{ ToDict("a", 1, "b", ToDict("e", true)), ToList("b"), ToDict("e", true) },
                new object[]{ ToDict("a", 1, "b", ToDict("e", true)), ToList("b", "e"), true }
            };
        }

        [Theory]
        [MemberData(nameof(CreateTestGetPositionalArgData))]
        public void TestGetPositionalArg(List<object> args, int index, object expected)
        {
            LogMessageTemplate instance = new TestLogRecordImp2("", null, args);
            var part = new PartDescriptor(0, 0, index);
            object actual = instance.GetPositionalArg(args, part);
            Assert.Equal(actual, expected);
        }

        public static List<object[]> CreateTestGetPositionalArgData()
        {
            return new List<object[]>
            {
                new object[]{ null, 0, null },
                new object[]{ ToList(), 0, null },
                new object[]{ ToList(), -1, null },
                new object[]{ ToList(), 1, null },
                new object[]{ ToList(1), 0,  1 },
                new object[]{ ToList(1), 1,  null },
                new object[]{ ToList(1), -1, null },
                new object[]{ ToList(1), -2, null },
                new object[]{ ToList(1, 2), 0,  1 },
                new object[]{ ToList(1, 2), 1,  2 },
                new object[]{ ToList(1, 2), -1,  2 },
                new object[]{ ToList(1, 2), 2,  null },
                new object[]{ ToList(1, 2), -2,  null },
                new object[]{ ToList(1, 2), -20,  null }
            };
        }

        [Theory]
        [MemberData(nameof(CreateTestToUnstructuredLogRecordData))]
        public void TestToUnstructuredLogRecord(string messageTemplate, object keywordArgs,
            List<object> positionalArgs,
            string expectedFormat, List<string> expectedFormatArgs)
        {
            LogMessageTemplate instance = new EmbeddableLogRecordImpl(messageTemplate, keywordArgs,
                positionalArgs);
            LogMessageTemplate.Unstructured output = instance.ToUnstructuredLogRecord();
            string actualFormat = output.FormatString;
            Assert.Equal(actualFormat, expectedFormat);
            List<string> actualFormatArgs = output.FormatArgs.Select(x => x.ToString()).ToList();
            Assert.Equal(new CollectionWrapper(actualFormatArgs), new CollectionWrapper(expectedFormatArgs));
        }

        public static List<object[]> CreateTestToUnstructuredLogRecordData()
        {
            return new List<object[]>
            {
                new object[]{ "", ToDict(), ToList(), "", ToGenericList<string>() },
                new object[]{ "{a}{0}", ToDict("a", "yes"), ToList(1), "{0}{1}", ToGenericList("\"yes\"", "1") },
                new object[]{ "{}{0}", ToDict("a", "yes"), ToList("1"), "{0}{1}", ToGenericList("{\"a\":\"yes\"}", "1") },
                new object[]{ "{$}{$0}", new CollectionWrapper(ToDict("a", "yes")), ToList("1"), "{0}{1}", ToGenericList("{a=yes}", "1") },
                new object[]{ "{@}{@0}", ToDict("a", "yes"), ToList("1"), "{0}{1}", ToGenericList("{\"a\":\"yes\"}", "\"1\"") }
            };
        }

        [Theory]
        [MemberData(nameof(CreateTestToStringData))]
        public void TestToString(string messageTemplate, object keywordArgs,
            List<object> positionalArgs, string expected)
        {
            LogMessageTemplate instance = new EmbeddableLogRecordImpl(messageTemplate, keywordArgs,
                positionalArgs);
            string actual = instance.ToString();
            Assert.Equal(actual, expected);
        }

        public static List<object[]> CreateTestToStringData()
        {
            return new List<object[]>
            {
                new object[]{ "", ToDict(), ToList(), "" },
                new object[]{ "{a}{0}", ToDict("a", "yes"), ToList(1), "\"yes\"1" },
                new object[]{ "{}{0}", ToDict("a", "yes"), ToList("1"), "{\"a\":\"yes\"}1" },
                new object[]{ "{$}{$0}", new CollectionWrapper(ToDict("a", "yes")), ToList("1"), "{a=yes}1" },
                new object[]{ "{@}{@0}", ToDict("a", "yes"), ToList("1"), "{\"a\":\"yes\"}\"1\"" },
                new object[]{ "{@}{@0}{8}", ToDict("a", "yes"), ToList("1"), "{\"a\":\"yes\"}\"1\"{8}" }
            };
        }
    }
}
