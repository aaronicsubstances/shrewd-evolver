package com.aaronicsubstances.shrewd.evolver;

import static org.testng.Assert.*;

import java.math.BigDecimal;
import java.math.BigInteger;
import java.util.Map;

import static java.util.Arrays.asList;
import static com.aaronicsubstances.shrewd.evolver.TestUtils.toMap;

import org.testng.annotations.DataProvider;
import org.testng.annotations.Test;

public class TreeDataMatcherTest {
    static class AnyNonNullMatcher extends TreeDataMatcher {
        public AnyNonNullMatcher() {
            super(null, "not null");
        }

        @Override
        protected void workOnEquivalenceAssertion(Object expected, Object actual, String pathToActual,
                Map<String, String> pathExpectations) {
            if (actual == null) {
                reportMismatch("is null", pathToActual, pathExpectations);
            }
        }
    }

    @Test(dataProvider = "createTestEquivalenceData")
    public void testEquivalence(Object expected, Object actual) {
        TreeDataMatcher instance = new TreeDataMatcher(expected);
        instance.assertEquivalentTo(actual);
    }

    @DataProvider
    public Object[][] createTestEquivalenceData() {
        return new Object[][]{
            { null, null },
            { true, true },
            { 8, 8 },
            { 8.2f, 8.2f },
            { 9.8818d, 9.8818d },
            { new BigInteger("400"), new BigInteger("400") },
            { new BigDecimal("400.89"), new BigDecimal("400.89") },
            { "str", "str" },
            { asList(2, "three"), asList(2, "three") },
            { asList(asList(2), asList("three")), asList(asList(2), asList("three")) },
            { toMap("three", 2), toMap("three", 2) },
            { toMap("three", 3), toMap("three", 3, "four", 4) },
            { 
                toMap("three", toMap("three", 3, "four", 4), "four", toMap("one", 3, "four", 4)),
                toMap("three", toMap("three", 3, "four", 4), "four", toMap("one", 3, "four", 4),
                    "sum", true)
            },
            { new TreeDataMatcher(asList(1, 2)), asList(1, 2) },
            { new AnyNonNullMatcher(), asList(2, true, "sum", toMap()) },
            { new AnyNonNullMatcher(), new StringBuilder() },
            { asList(2, true, "sum", new AnyNonNullMatcher()), asList(2, true, "sum", toMap()) }
        };
    }

    @Test(dataProvider = "createTestNonEquivalenceData")
    public void testNonEquivalence(int index, Object expected, Object actual) {
        boolean testPassed;
        try {
            TreeDataMatcher instance = new TreeDataMatcher(expected);
            instance.assertEquivalentTo(actual);
            testPassed = false;
        }
        catch (AssertionError ex) {
            System.err.println((index + 1) + ". " + ex);
            testPassed = true;
        }
        assertTrue(testPassed, "Expected non equivalence");
    }

    @DataProvider
    public Object[][] createTestNonEquivalenceData() {
        int index = 0;
        return new Object[][]{
            { index++, true, false },
            { index++, 8, 7 },
            { index++, 8, "8" },
            { index++, 8, null },
            { index++, "street", "str" },
            { index++, 9.8818d, 9.8819d },
            { index++, new BigInteger("400"), false },
            { index++, new BigInteger("400"), new BigInteger("401") },
            { index++, new BigDecimal("400.89"), new BigDecimal("400.19") },
            { index++, asList(1, 3, "three"), asList(2, "three") },
            { index++, asList(3, "three"), asList(2, "three") },
            { index++, asList(asList(2), asList("three")), asList(asList(2), asList(false)) },
            { index++, asList(asList(2), asList("three")), asList(asList(2), asList("three", false)) },
            { index++, toMap("three", 2), toMap("four", 2) },
            { index++, toMap("three", 2, "it", "seen"), toMap("three", 3, "four", 4) },
            { index++, toMap(), asList() },
            { index++, new TreeDataMatcher(asList(), "empty list"), toMap() },
            { index++, new TreeDataMatcher(asList(), true), asList(toMap()) },
            { 
                index++,
                toMap("three", toMap("three", 3, "four", 4), "four", toMap("one", 3, "four", 4)),
                toMap("three", toMap("three", 3, "four", 5), "four", toMap("one", 3, "four", 4),
                    "sum", true)
            },
            { index++, asList(2, true, "sum", new AnyNonNullMatcher()), asList(2, true, "sum", null) },
            { index++, toMap("2", true, "sum", new AnyNonNullMatcher()), toMap("2", true, "sum", null) },
            { index++, new AnyNonNullMatcher(), null },
        };
    }
}