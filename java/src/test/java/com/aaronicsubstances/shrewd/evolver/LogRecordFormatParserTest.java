package com.aaronicsubstances.shrewd.evolver;

import static org.testng.Assert.*;

import org.testng.annotations.DataProvider;
import org.testng.annotations.Test;

public class LogRecordFormatParserTest {
    
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
}