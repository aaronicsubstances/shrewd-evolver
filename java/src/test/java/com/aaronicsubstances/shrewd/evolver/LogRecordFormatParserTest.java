package com.aaronicsubstances.shrewd.evolver;

import java.util.Arrays;
import java.util.ArrayList;
import java.util.List;
import java.util.Objects;

import static java.util.Arrays.asList;
import static com.aaronicsubstances.shrewd.evolver.TestUtils.toMap;

import com.aaronicsubstances.shrewd.evolver.LogRecordFormatParser.FormatTokenType;
import com.aaronicsubstances.shrewd.evolver.LogRecordFormatParser.PartDescriptor;
import static org.testng.Assert.*;

import org.testng.annotations.DataProvider;
import org.testng.annotations.Test;

public class LogRecordFormatParserTest {

    public static class TokenPart {
        final PartDescriptor part;
        final boolean isReplacementField;
        final int startPos;
        final int endPos;

        public TokenPart(PartDescriptor part, boolean isReplacementField, int startPos, int endPos) {
            this.part = part;
            this.isReplacementField = isReplacementField;
            this.startPos = startPos;
            this.endPos = endPos;
        }

        @Override
        public boolean equals(Object obj) {
            if (!(obj instanceof TokenPart)) return false;
            TokenPart other = (TokenPart) obj;
            return other.part.equals(part) &&
                other.isReplacementField == isReplacementField &&
                other.startPos == startPos &&
                other.endPos == endPos;
        }

        @Override
        public int hashCode() {
            return Objects.hash(part, isReplacementField, startPos, endPos);
        }

        @Override
        public String toString() {
            return "TokenPart{part=" + part + ", isReplacementField=" + isReplacementField + 
                ", startPos=" + startPos + ", endPos=" + endPos + "}";
        }
    }

    @Test(dataProvider = "createTestCalculateLineAndColumnNumbersData")
    public void testCalculateLineAndColumnNumbers(String s, int pos,
            int expLineNumber, int expColumnNumber) {
        int[] expected = new int[]{ expLineNumber, expColumnNumber };
        int[] actual = LogRecordFormatParser.calculateLineAndColumnNumbers(s, pos);
        assertEquals(actual, expected);
    }

    @DataProvider
    public Object[][] createTestCalculateLineAndColumnNumbersData() {
        String inputWin32 = "This this is \r\nthe GOD we \r\nadore\r\n.";
        String inputUnix = "This this is \nthe GOD we \nadore\n.";
        String inputMac = "This this is \rthe GOD we \radore\r.";

        return new Object[][]{
            { "", 0, 1, 1 },
            { "\n", 1, 2, 1 },
            { "abc", 3, 1, 4 },
            { "ab\nc", 4, 2, 2 },
            { "ab\nc\r\n", 6, 3, 1 },

            new Object[]{ inputWin32, 1, 1, 2 },
            new Object[]{ inputWin32, 4, 1, 5 },
            new Object[]{ inputWin32, 5, 1, 6 },
            new Object[]{ inputWin32, 9, 1, 10 },
            new Object[]{ inputWin32, 10, 1, 11 },
            new Object[]{ inputWin32, 12, 1, 13 },
            new Object[]{ inputWin32, 13, 1, 14 },
            new Object[]{ inputWin32, 14, 1, 15 }, // test that encountering \r does not lead to newline
            new Object[]{ inputWin32, 15, 2, 1 },  // until \n here on this line.
            new Object[]{ inputWin32, 18, 2, 4 },

            new Object[]{ inputUnix, 1, 1, 2 },
            new Object[]{ inputUnix, 4, 1, 5 },
            new Object[]{ inputUnix, 5, 1, 6 },
            new Object[]{ inputUnix, 9, 1, 10 },
            new Object[]{ inputUnix, 10, 1, 11 },
            new Object[]{ inputUnix, 12, 1, 13 },
            new Object[]{ inputUnix, 13, 1, 14 },
            new Object[]{ inputUnix, 14, 2, 1 },
            new Object[]{ inputUnix, 17, 2, 4 },

            new Object[]{ inputMac, 1, 1, 2 },
            new Object[]{ inputMac, 4, 1, 5 },
            new Object[]{ inputMac, 5, 1, 6 },
            new Object[]{ inputMac, 9, 1, 10 },
            new Object[]{ inputMac, 10, 1, 11 },
            new Object[]{ inputMac, 12, 1, 13 },
            new Object[]{ inputMac, 13, 1, 14 },
            new Object[]{ inputMac, 14, 2, 1 },
            new Object[]{ inputMac, 17, 2, 4 },
        };
    }

    @Test(dataProvider = "createTestParseOnePartData")
    public void testParseOnePart(String source, List<TokenPart> expected) {
        LogRecordFormatParser tokenizer = new LogRecordFormatParser(source);
        PartDescriptor part;
        List<TokenPart> actual = new ArrayList<>();
        while ((part = tokenizer.parseOnePart()) != null) {
            assertTrue(asList(FormatTokenType.LITERAL_STRING_SECTION, 
                FormatTokenType.END_REPLACEMENT).contains(tokenizer.tokenType));
            TokenPart partDescriptor = new TokenPart(part,
                tokenizer.tokenType == FormatTokenType.END_REPLACEMENT,
                tokenizer.partStart, tokenizer.endPos);
            actual.add(partDescriptor);
        }
        assertEquals(actual, expected);
    }

    @DataProvider
    public Object[][] createTestParseOnePartData() {
        return new Object[][]{
            { "", asList() },
            { " ", asList(
                new TokenPart(new PartDescriptor(" "), false, 0, 1)
                ) },
            { "xyz", asList(
                new TokenPart(new PartDescriptor("xyz"), false, 0, 3)
                ) },
            { "x{{}}{{yz", asList(
                new TokenPart(new PartDescriptor("x{}{yz"), false, 0, 9)
                ) },
            { "x{}{{yz", asList(
                new TokenPart(new PartDescriptor("x"), false, 0, 1),
                new TokenPart(new PartDescriptor(asList()), true, 1, 3),
                new TokenPart(new PartDescriptor("{yz"), false, 3, 7)
                ) },
            { "x{0}{{yz", asList(
                new TokenPart(new PartDescriptor("x"), false, 0, 1),
                new TokenPart(new PartDescriptor(0), true, 1, 4),
                new TokenPart(new PartDescriptor("{yz"), false, 4, 8)
                ) },
            { "x{$0}{{yz", asList(
                new TokenPart(new PartDescriptor("x"), false, 0, 1),
                new TokenPart(new PartDescriptor(0), true, 1, 5),
                new TokenPart(new PartDescriptor("{yz"), false, 5, 9)
                ) },
            { "x{a}{{yz", asList(
                new TokenPart(new PartDescriptor("x"), false, 0, 1),
                new TokenPart(new PartDescriptor(asList("a")), true, 1, 4),
                new TokenPart(new PartDescriptor("{yz"), false, 4, 8)
                ) },
            { "x{$a}{{yz", asList(
                new TokenPart(new PartDescriptor("x"), false, 0, 1),
                new TokenPart(new PartDescriptor(asList("a"), false), true, 1, 5),
                new TokenPart(new PartDescriptor("{yz"), false, 5, 9)
                ) },
            { "x{.0}{{yz", asList(
                new TokenPart(new PartDescriptor("x"), false, 0, 1),
                new TokenPart(new PartDescriptor(asList("0")), true, 1, 5),
                new TokenPart(new PartDescriptor("{yz"), false, 5, 9)
                ) },
            { "x{$.0}{{yz", asList(
                new TokenPart(new PartDescriptor("x"), false, 0, 1),
                new TokenPart(new PartDescriptor(asList("0"), false), true, 1, 6),
                new TokenPart(new PartDescriptor("{yz"), false, 6, 10)
                ) },

            // test previous 4 again, but with whitespace tolerance.
            { "x{ }{{yz", asList(
                new TokenPart(new PartDescriptor("x"), false, 0, 1),
                new TokenPart(new PartDescriptor(asList()), true, 1, 4),
                new TokenPart(new PartDescriptor("{yz"), false, 4, 8)
                ) },
            { "x{ 0 }{{yz", asList(
                new TokenPart(new PartDescriptor("x"), false, 0, 1),
                new TokenPart(new PartDescriptor(0), true, 1, 6),
                new TokenPart(new PartDescriptor("{yz"), false, 6, 10)
                ) },
            { "x{a  }{{yz", asList(
                new TokenPart(new PartDescriptor("x"), false, 0, 1),
                new TokenPart(new PartDescriptor(asList("a")), true, 1, 6),
                new TokenPart(new PartDescriptor("{yz"), false, 6, 10)
                ) },
            { "x{ . 0 }{{yz", asList(
                new TokenPart(new PartDescriptor("x"), false, 0, 1),
                new TokenPart(new PartDescriptor(asList("0")), true, 1, 8),
                new TokenPart(new PartDescriptor("{yz"), false, 8, 12)
                ) },

            // handle longer replacement fields.
            { "{bag .prices[0 ]}", asList(
                new TokenPart(new PartDescriptor(asList("bag", "prices", 0)), true, 0, 17)
                ) },                
            { "{ -12 }{ [10][-2] }{ y.z }", asList(
                new TokenPart(new PartDescriptor(-12), true, 0, 7),
                new TokenPart(new PartDescriptor(asList(10, -2)), true, 7, 19),
                new TokenPart(new PartDescriptor(asList("y", "z")), true, 19, 26)
                ) }
        };
    }    

    @Test(dataProvider = "createTestParseData")
    public void testParse(int index, String source, List<PartDescriptor> expected) {
        List<PartDescriptor> actual = null;
        try {
            LogRecordFormatParser instance = new LogRecordFormatParser(source);
            actual = instance.parse();
        }
        catch (Throwable ex) {
            System.err.println((index + 1) + ". " + ex);
        }
        assertEquals(actual, expected);
    }

    @DataProvider
    public Object[][] createTestParseData() {
        int index = 0;
        return new Object[][]{
            { index++, "{", null },
            { index++, "}", null },
            { index++, "{0", null },
            { index++, "{a}", asList(new PartDescriptor(asList("a"))) },
            { index++, "a{.0.[2]}b", null },
            { index++, "a{.}b", null },
            { index++, "a{.a$}b", null },
            { index++, "a{.{b", null },
            { index++, "a{.x{b", null },
            { index++, "a{[0}b", null },
            { index++, "a{0]}b", null },
            { index++, "a{.0]}b", null },
            { index++, "a{[]}b", null },
            { index++, "a{[x]}b", null },
            { index++, "a{[200]}b", asList(new PartDescriptor("a"),
                new PartDescriptor(asList(200)), new PartDescriptor("b")) },
            { index++, "a{ $ [ 200]}b", null },
            { index++, "a{$ [ 200]}b", asList(new PartDescriptor("a"),
                new PartDescriptor(asList(200), false), new PartDescriptor("b")) },
            { index++, " a { x.data [ 200 ] [ 300 ] . q . z } b", asList(new PartDescriptor(" a "), 
                new PartDescriptor(asList("x", "data", 200, 300, "q", "z")), 
                new PartDescriptor(" b")) },
            { index++, "", asList() },
            { index++, "{}", asList(new PartDescriptor(asList())) },
            { index++, "{$}", asList(new PartDescriptor(asList(), false)) },
            { index++, ".[]", asList(new PartDescriptor(".[]")) },
            { index++, ".{{{{x}}}}", asList(new PartDescriptor(".{{x}}")) },
            { index++, "0", asList(new PartDescriptor("0")) },
            { index++, "{{0}}", asList(new PartDescriptor("{0}")) },
            { index++, "{0}", asList(new PartDescriptor(0)) },
            { index++, "{$0}", asList(new PartDescriptor(0)) },
            { index++, "{[0]}", asList(new PartDescriptor(asList(0))) },
            { index++, "{$[0]}", asList(new PartDescriptor(asList(0), false)) },

            // test with newlines
            { index++, "{[\n0]\n}", asList(new PartDescriptor(asList(0))) },
            { index++, "\nThere is plenty{\n}\n of peace\n", 
                asList(new PartDescriptor("\nThere is plenty"), new PartDescriptor(asList()),
                    new PartDescriptor("\n of peace\n")) },
            { index++, "{[0]}\nfirst\nsecond{a}}r{y}", null }
        };
    }
}