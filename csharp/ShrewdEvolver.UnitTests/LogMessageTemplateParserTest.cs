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
        public class TokenPart
        {
            internal PartDescriptor part;
            internal readonly bool isReplacementField;
            internal readonly int startPos;
            internal readonly int endPos;

            public TokenPart(PartDescriptor part, bool isReplacementField, int startPos, int endPos)
            {
                this.part = part;
                this.isReplacementField = isReplacementField;
                this.startPos = startPos;
                this.endPos = endPos;
            }
            
            // override object.Equals
            public override bool Equals(object obj)
            {
                if (obj == null || GetType() != obj.GetType())
                {
                    return false;
                }

                var other = (TokenPart)obj;
                if (!Equals(other.part, part))
                {
                    return false;
                }
                return other.isReplacementField == isReplacementField &&
                    other.startPos == startPos &&
                    other.endPos == endPos;
            }

            // override object.GetHashCode
            public override int GetHashCode()
            {
                return 1; 
            }

            public override string ToString()
            {
                return "TokenPart{part=" + part + ", isReplacementField=" + isReplacementField +
                    ", startPos=" + startPos + ", endPos=" + endPos + "}";
            }
        }

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
        public void TestParseOnePart(string source, List<TokenPart> expected)
        {
            LogMessageTemplateParser tokenizer = new LogMessageTemplateParser(source);
            PartDescriptor part;
            var actual = new List<TokenPart>();
            while ((part = tokenizer.ParseOnePart()) != null)
            {
                Assert.Contains(tokenizer.tokenType, ToList(FormatTokenType.LITERAL_STRING_SECTION,
                    FormatTokenType.END_REPLACEMENT));
                TokenPart partDescriptor = new TokenPart(part,
                    tokenizer.tokenType == FormatTokenType.END_REPLACEMENT,
                    tokenizer.partStart, tokenizer.endPos);
                actual.Add(partDescriptor);
            }
            Assert.Equal(new CollectionWrapper(expected), new CollectionWrapper(actual));
        }

        public static List<object[]> CreateTestParseOnePartData()
        {
            return new List<object[]>
            {
                new object[]{ "", ToGenericList<TokenPart>() },
                new object[]{ " ", ToGenericList(
                    new TokenPart(new PartDescriptor(" "), false, 0, 1)
                    ) },
                new object[]{ "xyz", ToGenericList(
                    new TokenPart(new PartDescriptor("xyz"), false, 0, 3)
                    ) },
                new object[]{ "x{{}}{{yz", ToGenericList(
                    new TokenPart(new PartDescriptor("x{}{yz"), false, 0, 9)
                    ) },
                new object[]{ "x{}{{yz", ToGenericList(
                    new TokenPart(new PartDescriptor("x"), false, 0, 1),
                    new TokenPart(new PartDescriptor(ToList()), true, 1, 3),
                    new TokenPart(new PartDescriptor("{yz"), false, 3, 7)
                    ) },
                new object[]{ "x{0}{{yz", ToGenericList(
                    new TokenPart(new PartDescriptor("x"), false, 0, 1),
                    new TokenPart(new PartDescriptor(0), true, 1, 4),
                    new TokenPart(new PartDescriptor("{yz"), false, 4, 8)
                    ) },
                new object[]{ "x{$0}{{yz", ToGenericList(
                    new TokenPart(new PartDescriptor("x"), false, 0, 1),
                    new TokenPart(new PartDescriptor(0), true, 1, 5),
                    new TokenPart(new PartDescriptor("{yz"), false, 5, 9)
                    ) },
                new object[]{ "x{a}{{yz", ToGenericList(
                    new TokenPart(new PartDescriptor("x"), false, 0, 1),
                    new TokenPart(new PartDescriptor(ToList("a")), true, 1, 4),
                    new TokenPart(new PartDescriptor("{yz"), false, 4, 8)
                    ) },
                new object[]{ "x{$a}{{yz", ToGenericList(
                    new TokenPart(new PartDescriptor("x"), false, 0, 1),
                    new TokenPart(new PartDescriptor(ToList("a"), false), true, 1, 5),
                    new TokenPart(new PartDescriptor("{yz"), false, 5, 9)
                    ) },
                new object[]{ "x{.0}{{yz", ToGenericList(
                    new TokenPart(new PartDescriptor("x"), false, 0, 1),
                    new TokenPart(new PartDescriptor(ToList("0")), true, 1, 5),
                    new TokenPart(new PartDescriptor("{yz"), false, 5, 9)
                    ) },
                new object[]{ "x{$.0}{{yz", ToGenericList(
                    new TokenPart(new PartDescriptor("x"), false, 0, 1),
                    new TokenPart(new PartDescriptor(ToList("0"), false), true, 1, 6),
                    new TokenPart(new PartDescriptor("{yz"), false, 6, 10)
                    ) },

                // test previous 4 again, but with whitespace tolerance.
                new object[]{ "x{ }{{yz", ToGenericList(
                    new TokenPart(new PartDescriptor("x"), false, 0, 1),
                    new TokenPart(new PartDescriptor(ToList()), true, 1, 4),
                    new TokenPart(new PartDescriptor("{yz"), false, 4, 8)
                    ) },
                new object[]{ "x{ 0 }{{yz", ToGenericList(
                    new TokenPart(new PartDescriptor("x"), false, 0, 1),
                    new TokenPart(new PartDescriptor(0), true, 1, 6),
                    new TokenPart(new PartDescriptor("{yz"), false, 6, 10)
                    ) },
                new object[]{ "x{a  }{{yz", ToGenericList(
                    new TokenPart(new PartDescriptor("x"), false, 0, 1),
                    new TokenPart(new PartDescriptor(ToList("a")), true, 1, 6),
                    new TokenPart(new PartDescriptor("{yz"), false, 6, 10)
                    ) },
                new object[]{ "x{ . 0 }{{yz", ToGenericList(
                    new TokenPart(new PartDescriptor("x"), false, 0, 1),
                    new TokenPart(new PartDescriptor(ToList("0")), true, 1, 8),
                    new TokenPart(new PartDescriptor("{yz"), false, 8, 12)
                    ) },

                // handle longer replacement fields.
                new object[]{ "{bag .prices[0 ]}", ToGenericList(
                    new TokenPart(new PartDescriptor(ToList("bag", "prices", 0)), true, 0, 17)
                    ) },
                new object[]{ "{ -12 }{ [10][-2] }{ y.z }", ToGenericList(
                    new TokenPart(new PartDescriptor(-12), true, 0, 7),
                    new TokenPart(new PartDescriptor(ToList(10, -2)), true, 7, 19),
                    new TokenPart(new PartDescriptor(ToList("y", "z")), true, 19, 26)
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
                new object[]{ index++, "{a}", ToList(new PartDescriptor(ToList("a"))) },
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
                new object[]{ index++, "a{[200]}b", ToList(new PartDescriptor("a"), 
                    new PartDescriptor(ToList(200)), new PartDescriptor("b")) },
                new object[]{ index++, "a{ $ [ 200]}b", null },
                new object[]{ index++, "a{$ [ 200]}b", ToList(new PartDescriptor("a"),
                    new PartDescriptor(ToList(200), false), new PartDescriptor("b")) },
                new object[]{ index++, " a { x.data [ 200 ] [ 300 ] . q . z } b", ToList(new PartDescriptor(" a "),
                    new PartDescriptor(ToList("x", "data", 200, 300, "q", "z")), 
                    new PartDescriptor(" b")) },
                new object[]{ index++, "", ToList() },
                new object[]{ index++, "{}", ToList(new PartDescriptor(ToList())) },
                new object[]{ index++, "{$}", ToList(new PartDescriptor(ToList(), false)) },
                new object[]{ index++, ".[]", ToList(new PartDescriptor(".[]")) },
                new object[]{ index++, ".{{{{x}}}}", ToList(new PartDescriptor(".{{x}}")) },
                new object[]{ index++, "0", ToList(new PartDescriptor("0")) },
                new object[]{ index++, "{{0}}", ToList(new PartDescriptor("{0}")) },
                new object[]{ index++, "{0}", ToList(new PartDescriptor(0)) },
                new object[]{ index++, "{$0}", ToList(new PartDescriptor(0)) },
                new object[]{ index++, "{[0]}", ToList(new PartDescriptor(ToList(0))) },
                new object[]{ index++, "{$[0]}", ToList(new PartDescriptor(ToList(0), false)) },

                // test with newlines
                new object[]{ index++, "{[\n0]\n}", ToList(new PartDescriptor(ToList(0))) },
                new object[]{ index++, "\nThere is plenty{\n}\n of peace\n", 
                    ToList(new PartDescriptor("\nThere is plenty"), new PartDescriptor(ToList()),
                        new PartDescriptor("\n of peace\n")) },
                new object[]{ index++, "{[0]}\nfirst\nsecond{a}}r{y}", null }
            };
        }
    }
}
