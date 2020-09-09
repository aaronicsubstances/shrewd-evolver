package com.aaronicsubstances.shrewd.evolver;

import java.util.ArrayList;
import java.util.List;

import static java.util.Arrays.asList;

import com.aaronicsubstances.shrewd.evolver.LogMessageTemplateParser.FormatTokenType;
import com.aaronicsubstances.shrewd.evolver.LogMessageTemplateParser.PartDescriptor;
import static org.testng.Assert.*;

import org.testng.annotations.DataProvider;
import org.testng.annotations.Test;

public class LogMessageTemplateParserTest {

    @Test(dataProvider = "createTestCalculateLineAndColumnNumbersData")
    public void testCalculateLineAndColumnNumbers(String s, int pos,
            int expLineNumber, int expColumnNumber) {
        int[] expected = new int[]{ expLineNumber, expColumnNumber };
        int[] actual = LogMessageTemplateParser.calculateLineAndColumnNumbers(s, pos);
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
    public void testParseOnePart(String source, List<PartDescriptor> expected) {
        LogMessageTemplateParser tokenizer = new LogMessageTemplateParser(source);
        PartDescriptor part;
        List<PartDescriptor> actual = new ArrayList<>();
        while ((part = tokenizer.parseOnePart()) != null) {
            assertTrue(asList(FormatTokenType.LITERAL_STRING_SECTION, 
                FormatTokenType.END_REPLACEMENT).contains(tokenizer.tokenType));
            assertEquals(tokenizer.tokenType == FormatTokenType.END_REPLACEMENT,
                part.literalSection == null);
            actual.add(part);
        }
        assertEquals(actual, expected);
    }

    @DataProvider
    public Object[][] createTestParseOnePartData() {
        return new Object[][]{
            { "", asList() },
            { " ", asList(
                new PartDescriptor(0, 1, " ")
                ) },
            { "xyz", asList(
                new PartDescriptor(0, 3, "xyz")
                ) },
            { "x{{}}{{yz", asList(
                new PartDescriptor(0, 9, "x{}{yz")
                ) },
            { "x{}{{yz", asList(
                new PartDescriptor(0, 1, "x"),
                new PartDescriptor(1, 3, asList()),
                new PartDescriptor(3, 7, "{yz")
                ) },
            { "x{0}{{yz", asList(
                new PartDescriptor(0, 1, "x"),
                new PartDescriptor(1, 4, 0),
                new PartDescriptor(4, 8, "{yz")
                ) },
            { "x{$0}{{yz", asList(
                new PartDescriptor(0, 1, "x"),
                new PartDescriptor(1, 5, 0),
                new PartDescriptor(5, 9, "{yz")
                ) },
            { "x{@0}{{yz", asList(
                new PartDescriptor(0, 1, "x"),
                new PartDescriptor(1, 5, 0, true),
                new PartDescriptor(5, 9, "{yz")
                ) },
            { "x{a}{{yz", asList(
                new PartDescriptor(0, 1, "x"),
                new PartDescriptor(1, 4, asList("a")),
                new PartDescriptor(4, 8, "{yz")
                ) },
            { "x{$a}{{yz", asList(
                new PartDescriptor(0, 1, "x"),
                new PartDescriptor(1, 5, asList("a"), false),
                new PartDescriptor(5, 9, "{yz")
                ) },
            { "x{.0}{{yz", asList(
                new PartDescriptor(0, 1, "x"),
                new PartDescriptor(1, 5, asList("0")),
                new PartDescriptor(5, 9, "{yz")
                ) },
            { "x{$.0}{{yz", asList(
                new PartDescriptor(0, 1, "x"),
                new PartDescriptor(1, 6, asList("0"), false),
                new PartDescriptor(6, 10, "{yz")
                ) },

            // test previous 4 again, but with whitespace tolerance.
            { "x{ }{{yz", asList(
                new PartDescriptor(0, 1, "x"),
                new PartDescriptor(1, 4, asList()),
                new PartDescriptor(4, 8, "{yz")
                ) },
            { "x{ 0 }{{yz", asList(
                new PartDescriptor(0, 1, "x"),
                new PartDescriptor(1, 6, 0),
                new PartDescriptor(6, 10, "{yz")
                ) },
            { "x{a  }{{yz", asList(
                new PartDescriptor(0, 1, "x"),
                new PartDescriptor(1, 6, asList("a")),
                new PartDescriptor(6, 10, "{yz")
                ) },
            { "x{ . 0 }{{yz", asList(
                new PartDescriptor(0, 1, "x"),
                new PartDescriptor(1, 8, asList("0")),
                new PartDescriptor(8, 12, "{yz")
                ) },

            // handle longer replacement fields.
            { "{bag .prices[0 ]}", asList(
                new PartDescriptor(0, 17, asList("bag", "prices", 0))
                ) },                
            { "{ -12 }{ [10][-2] }{ y.z }", asList(
                new PartDescriptor(0, 7, -12),
                new PartDescriptor(7, 19, asList(10, -2)),
                new PartDescriptor(19, 26, asList("y", "z"))
                ) }
        };
    }    

    @Test(dataProvider = "createTestParseData")
    public void testParse(int index, String source, List<PartDescriptor> expected) {
        List<PartDescriptor> actual = null;
        try {
            LogMessageTemplateParser instance = new LogMessageTemplateParser(source);
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
            { index++, "{a}", asList(new PartDescriptor(0, 3, asList("a"))) },
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
            { index++, "a{[200]}b", asList(new PartDescriptor(0, 1, "a"),
                new PartDescriptor(1, 8, asList(200)), new PartDescriptor(8, 9, "b")) },
            { index++, "a{ $ [ 200]}b", null },
            { index++, "a{$ [ 200]}b", asList(new PartDescriptor(0, 1, "a"),
                new PartDescriptor(1, 11, asList(200), false), new PartDescriptor(11, 12, "b")) },
            { index++, " a { x.data [ 200 ] [ 300 ] . q . z } b", asList(new PartDescriptor(0, 3, " a "), 
                new PartDescriptor(3, 37, asList("x", "data", 200, 300, "q", "z")), 
                new PartDescriptor(37, 39, " b")) },
            { index++, "", asList() },
            { index++, "{}", asList(new PartDescriptor(0, 2, asList())) },
            { index++, "{$}", asList(new PartDescriptor(0, 3, asList(), false)) },
            { index++, ".[]", asList(new PartDescriptor(0, 3, ".[]")) },
            { index++, ".{{{{x}}}}", asList(new PartDescriptor(0, 10, ".{{x}}")) },
            { index++, "0", asList(new PartDescriptor(0, 1, "0")) },
            { index++, "{{0}}", asList(new PartDescriptor(0, 5, "{0}")) },
            { index++, "{0}", asList(new PartDescriptor(0, 3, 0)) },
            { index++, "{$0}", asList(new PartDescriptor(0, 4, 0)) },
            { index++, "{@2}", asList(new PartDescriptor(0, 4, 2, true)) },
            { index++, "{[0]}", asList(new PartDescriptor(0, 5, asList(0))) },
            { index++, "{$[0]}", asList(new PartDescriptor(0, 6, asList(0), false)) },

            // test with newlines
            { index++, "{[\n0]\n}", asList(new PartDescriptor(0, 7, asList(0))) },
            { index++, "\nThere is plenty{\n}\n of peace\n", 
                asList(new PartDescriptor(0, 16, "\nThere is plenty"),
                    new PartDescriptor(16, 19, asList()),
                    new PartDescriptor(19, 30, "\n of peace\n")) },
            { index++, "{[0]}\nfirst\nsecond{a}}r{y}", null }
        };
    }
}