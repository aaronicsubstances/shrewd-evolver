using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using static AaronicSubstances.ShrewdEvolver.LogMessageTemplateParser;
using static AaronicSubstances.ShrewdEvolver.UnitTests.TestUtils;

namespace AaronicSubstances.ShrewdEvolver.UnitTests
{
    public class LogMessageTemplateParserTest
    {
        private readonly ITestOutputHelper _output;

        public LogMessageTemplateParserTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [MemberData(nameof(CreateTestCalculateLineAndColumnNumbersData))]
        public void TestCalculateLineAndColumnNumbers(string s, int pos,
            int expLineNumber, int expColumnNumber)
        {
            int[] expected = new int[] { expLineNumber, expColumnNumber };
            int[] actual = LogMessageTemplateParser.CalculateLineAndColumnNumbers(s, pos);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestCalculateLineAndColumnNumbersData()
        {
            string inputWin32 = "This this is \r\nthe GOD we \r\nadore\r\n.";
            string inputUnix = "This this is \nthe GOD we \nadore\n.";
            string inputMac = "This this is \rthe GOD we \radore\r.";

            return new List<object []>
            {
                new object[]{ "", 0, 1, 1 },
                new object[]{ "\n", 1, 2, 1 },
                new object[]{ "abc", 3, 1, 4 },
                new object[]{ "ab\nc", 4, 2, 2 },
                new object[]{ "ab\nc\r\n", 6, 3, 1 },

                new object[]{ inputWin32, 1, 1, 2 },
                new object[]{ inputWin32, 4, 1, 5 },
                new object[]{ inputWin32, 5, 1, 6 },
                new object[]{ inputWin32, 9, 1, 10 },
                new object[]{ inputWin32, 10, 1, 11 },
                new object[]{ inputWin32, 12, 1, 13 },
                new object[]{ inputWin32, 13, 1, 14 },
                new object[]{ inputWin32, 14, 1, 15 }, // test that encountering \r does not lead to newline
                new object[]{ inputWin32, 15, 2, 1 },  // until \n here on this line.
                new object[]{ inputWin32, 18, 2, 4 },

                new object[]{ inputUnix, 1, 1, 2 },
                new object[]{ inputUnix, 4, 1, 5 },
                new object[]{ inputUnix, 5, 1, 6 },
                new object[]{ inputUnix, 9, 1, 10 },
                new object[]{ inputUnix, 10, 1, 11 },
                new object[]{ inputUnix, 12, 1, 13 },
                new object[]{ inputUnix, 13, 1, 14 },
                new object[]{ inputUnix, 14, 2, 1 },
                new object[]{ inputUnix, 17, 2, 4 },

                new object[]{ inputMac, 1, 1, 2 },
                new object[]{ inputMac, 4, 1, 5 },
                new object[]{ inputMac, 5, 1, 6 },
                new object[]{ inputMac, 9, 1, 10 },
                new object[]{ inputMac, 10, 1, 11 },
                new object[]{ inputMac, 12, 1, 13 },
                new object[]{ inputMac, 13, 1, 14 },
                new object[]{ inputMac, 14, 2, 1 },
                new object[]{ inputMac, 17, 2, 4 },
            };
        }

        [Theory]
        [MemberData(nameof(CreateTestParseOnePartData))]
        public void TestParseOnePart(string source, List<PartDescriptor> expected)
        {
            LogMessageTemplateParser tokenizer = new LogMessageTemplateParser(source);
            PartDescriptor part;
            var actual = new List<PartDescriptor>();
            while ((part = tokenizer.ParseOnePart()) != null)
            {
                Assert.Contains(tokenizer.tokenType, ToList(FormatTokenType.LITERAL_STRING_SECTION,
                    FormatTokenType.END_REPLACEMENT));
                Assert.Equal(tokenizer.tokenType == FormatTokenType.END_REPLACEMENT,
                    part.literalSection == null);
                actual.Add(part);
            }
            Assert.Equal(new CollectionWrapper(expected), new CollectionWrapper(actual));
        }

        public static List<object[]> CreateTestParseOnePartData()
        {
            return new List<object[]>
            {
                new object[]{ "", ToGenericList<PartDescriptor>() },
                new object[]{ " ", ToGenericList(
                    new PartDescriptor(0, 1, " ")
                    ) },
                new object[]{ "xyz", ToGenericList(
                    new PartDescriptor(0, 3, "xyz")
                    ) },
                new object[]{ "x{{}}{{yz", ToGenericList(
                    new PartDescriptor(0, 9, "x{}{yz")
                    ) },
                new object[]{ "x{}{{yz", ToGenericList(
                    new PartDescriptor(0, 1, "x"),
                    new PartDescriptor(1, 3, ToList()),
                    new PartDescriptor(3, 7, "{yz")
                    ) },
                new object[]{ "x{0}{{yz", ToGenericList(
                    new PartDescriptor(0, 1, "x"),
                    new PartDescriptor(1, 4, 0),
                    new PartDescriptor(4, 8, "{yz")
                    ) },
                new object[]{ "x{$0}{{yz", ToGenericList(
                    new PartDescriptor(0, 1, "x"),
                    new PartDescriptor(1, 5, 0),
                    new PartDescriptor(5, 9, "{yz")
                    ) },
                new object[]{ "x{@0}{{yz", ToGenericList(
                    new PartDescriptor(0, 1, "x"),
                    new PartDescriptor(1, 5, 0, true),
                    new PartDescriptor(5, 9, "{yz")
                    ) },
                new object[]{ "x{a}{{yz", ToGenericList(
                    new PartDescriptor(0, 1, "x"),
                    new PartDescriptor(1, 4, ToList("a")),
                    new PartDescriptor(4, 8, "{yz")
                    ) },
                new object[]{ "x{$a}{{yz", ToGenericList(
                    new PartDescriptor(0, 1, "x"),
                    new PartDescriptor(1, 5, ToList("a"), false),
                    new PartDescriptor(5, 9, "{yz")
                    ) },
                new object[]{ "x{.0}{{yz", ToGenericList(
                    new PartDescriptor(0, 1, "x"),
                    new PartDescriptor(1, 5, ToList("0")),
                    new PartDescriptor(5, 9, "{yz")
                    ) },
                new object[]{ "x{$.0}{{yz", ToGenericList(
                    new PartDescriptor(0, 1, "x"),
                    new PartDescriptor(1, 6, ToList("0"), false),
                    new PartDescriptor(6, 10, "{yz")
                    ) },

                // test previous 4 again, but with whitespace tolerance.
                new object[]{ "x{ }{{yz", ToGenericList(
                    new PartDescriptor(0, 1, "x"),
                    new PartDescriptor(1, 4, ToList()),
                    new PartDescriptor(4, 8, "{yz")
                    ) },
                new object[]{ "x{ 0 }{{yz", ToGenericList(
                    new PartDescriptor(0, 1, "x"),
                    new PartDescriptor(1, 6, 0),
                    new PartDescriptor(6, 10, "{yz")
                    ) },
                new object[]{ "x{a  }{{yz", ToGenericList(
                    new PartDescriptor(0, 1, "x"),
                    new PartDescriptor(1, 6, ToList("a")),
                    new PartDescriptor(6, 10, "{yz")
                    ) },
                new object[]{ "x{ . 0 }{{yz", ToGenericList(
                    new PartDescriptor(0, 1, "x"),
                    new PartDescriptor(1, 8, ToList("0")),
                    new PartDescriptor(8, 12, "{yz")
                    ) },

                // handle longer replacement fields.
                new object[]{ "{bag .prices[0 ]}", ToGenericList(
                    new PartDescriptor(0, 17, ToList("bag", "prices", 0))
                    ) },
                new object[]{ "{ -12 }{ [10][-2] }{ y.z }", ToGenericList(
                    new PartDescriptor(0, 7, -12),
                    new PartDescriptor(7, 19, ToList(10, -2)),
                    new PartDescriptor(19, 26, ToList("y", "z"))
                    ) }
            };
        }


        [Theory]
        [MemberData(nameof(CreateTestParseData))]
        public void TestParse(int index, string source, List<object> expected)
        {
            List<PartDescriptor> actual = null;
            try
            {
                var instance = new LogMessageTemplateParser(source);
                actual = instance.Parse();
            }
            catch (Exception ex)
            {
                _output.WriteLine((index + 1) + ". " + ex);
            }
            Assert.Equal(new CollectionWrapper(actual), new CollectionWrapper(expected));
        }

        public static List<object[]> CreateTestParseData()
        {
            int index = 0;
            return new List<object[]>
            {
                new object[]{ index++, "{", null },
                new object[]{ index++, "}", null },
                new object[]{ index++, "{0", null },
                new object[]{ index++, "{a}", ToList(new PartDescriptor(0, 3, ToList("a"))) },
                new object[]{ index++, "a{.0.[2]}b", null },
                new object[]{ index++, "a{.}b", null },
                new object[]{ index++, "a{.a$}b", null },
                new object[]{ index++, "a{.{b", null },
                new object[]{ index++, "a{.x{b", null },
                new object[]{ index++, "a{[0}b", null },
                new object[]{ index++, "a{0]}b", null },
                new object[]{ index++, "a{.0]}b", null },
                new object[]{ index++, "a{[]}b", null },
                new object[]{ index++, "a{[x]}b", null },
                new object[]{ index++, "a{[200]}b", ToList(new PartDescriptor(0, 1, "a"), 
                    new PartDescriptor(1, 8, ToList(200)), new PartDescriptor(8, 9, "b")) },
                new object[]{ index++, "a{ $ [ 200]}b", null },
                new object[]{ index++, "a{$ [ 200]}b", ToList(new PartDescriptor(0, 1, "a"),
                    new PartDescriptor(1, 11, ToList(200), false), new PartDescriptor(11, 12, "b")) },
                new object[]{ index++, " a { x.data [ 200 ] [ 300 ] . q . z } b", ToList(
                    new PartDescriptor(0, 3, " a "),
                    new PartDescriptor(3, 37, ToList("x", "data", 200, 300, "q", "z")), 
                    new PartDescriptor(37, 39, " b")) },
                new object[]{ index++, "", ToList() },
                new object[]{ index++, "{}", ToList(new PartDescriptor(0, 2, ToList())) },
                new object[]{ index++, "{$}", ToList(new PartDescriptor(0, 3, ToList(), false)) },
                new object[]{ index++, ".[]", ToList(new PartDescriptor(0, 3, ".[]")) },
                new object[]{ index++, ".{{{{x}}}}", ToList(new PartDescriptor(0, 10, ".{{x}}")) },
                new object[]{ index++, "0", ToList(new PartDescriptor(0, 1, "0")) },
                new object[]{ index++, "{{0}}", ToList(new PartDescriptor(0, 5, "{0}")) },
                new object[]{ index++, "{0}", ToList(new PartDescriptor(0, 3, 0)) },
                new object[]{ index++, "{$0}", ToList(new PartDescriptor(0, 4, 0)) },
                new object[]{ index++, "{@2}", ToList(new PartDescriptor(0, 4, 2, true)) },
                new object[]{ index++, "{[0]}", ToList(new PartDescriptor(0, 5, ToList(0))) },
                new object[]{ index++, "{$[0]}", ToList(new PartDescriptor(0, 6, ToList(0), false)) },

                // test with newlines
                new object[]{ index++, "{[\n0]\n}", ToList(new PartDescriptor(0, 7, ToList(0))) },
                new object[]{ index++, "\nThere is plenty{\n}\n of peace\n", 
                    ToList(new PartDescriptor(0, 16, "\nThere is plenty"), 
                        new PartDescriptor(16, 19, ToList()),
                        new PartDescriptor(19, 30, "\n of peace\n")) },
                new object[]{ index++, "{[0]}\nfirst\nsecond{a}}r{y}", null }
            };
        }
    }
}
