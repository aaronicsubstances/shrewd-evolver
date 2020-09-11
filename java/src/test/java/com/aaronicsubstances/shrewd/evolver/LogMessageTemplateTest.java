package com.aaronicsubstances.shrewd.evolver;

import java.util.List;
import java.util.stream.Collectors;

import com.aaronicsubstances.shrewd.evolver.LogMessageTemplateParser.PartDescriptor;

import com.google.gson.Gson;

import static java.util.Arrays.asList;
import static com.aaronicsubstances.shrewd.evolver.TestUtils.toMap;

import org.testng.annotations.DataProvider;
import org.testng.annotations.Test;

import static org.testng.Assert.*;

public class LogMessageTemplateTest {

    public static class EmbeddableLogRecordImpl extends LogMessageTemplate {

        public EmbeddableLogRecordImpl(String formatString, Object treeData, List<Object> positionalArgs) {
            super(formatString, treeData, positionalArgs);
        }
		
		@Override
		protected String serializeData(Object treeDataSlice) {
			return new Gson().toJson(treeDataSlice);
		}

        @Override
        protected String escapeLiteral(String literal, boolean forLogger) {
            if (forLogger) {
				// Assume SLF4J
                return literal.replace("{", "\\{");
            }
            return super.escapeLiteral(literal, forLogger);
        }

        @Override
        protected String generatePositionIndicator(int position, boolean forLogger) {
            if (forLogger) {
				// Assume SLF4J
                return "{}";
            }
            return super.generatePositionIndicator(position, forLogger);
        }
    }
    
    @Test(dataProvider = "creatTestGetTreeDataSliceData")
    public void testGetTreeDataSlice(Object treeData, List<Object> treeDataKey, Object expected) {
        LogMessageTemplate instance = new EmbeddableLogRecordImpl("", treeData, null) {
            
            @Override
            protected Object handleNonExistentTreeDataSlice(PartDescriptor part, int nonExistentIndex) {
                return null;
            }
        };
        PartDescriptor part = new PartDescriptor(0, 0, treeDataKey);
        Object actual = instance.getTreeDataSlice(treeData, part);
        assertEquals(actual, expected);
    }

    @DataProvider
    public Object[][] creatTestGetTreeDataSliceData() {
        return new Object[][]{
            { null, asList(), null },
            { "", asList(), "" },
            { 2, asList(), 2 },
            { asList(2), asList("f"), null },
            { asList(2), asList(0), 2 },
            { asList(21, asList()), asList(10), null },
            { asList(21, asList()), asList(0), 21 },
            { asList(21, asList()), asList(1), asList() },
            { asList(21, asList()), asList(-1), asList() },
            { asList(21, asList()), asList(-2), null },
            { toMap("a", 1), asList(), toMap("a", 1) },
            { toMap("a", 1), asList(0), null },
            { toMap("a", 1), asList("a"), 1 },
            { toMap("a", 1, "b", 2), asList("b"), 2 },
            { toMap("a", 1, "b", 2), asList("c"), null },
            { toMap("a", 1, "b", asList("e", true)), asList("b", 0), "e" },
            { toMap("a", 1, "b", asList("e", true)), asList("b", 1), true },
            { toMap("a", 1, "b", toMap("e", true)), asList("b"), toMap("e", true) },
            { toMap("a", 1, "b", toMap("e", true)), asList("b", "e"), true }
        };
    }

    @Test(dataProvider = "createTestGetPositionalArgData")
    public void testGetPositionalArg(List<Object> args, int index, Object expected) {
        LogMessageTemplate instance = new EmbeddableLogRecordImpl("", null, args) {
            
            @Override
            protected Object handleNonExistentPositionalArg(PartDescriptor part) {
                return null;
            }
        };
        PartDescriptor part = new PartDescriptor(0, 0, index);
        Object actual = instance.getPositionalArg(args, part);
        assertEquals(actual, expected);
    }

    @DataProvider
    public Object[][] createTestGetPositionalArgData() {
        return new Object[][]{
            { null, 0, null },
            { asList(), 0, null },
            { asList(), -1, null },
            { asList(), 1, null },
            { asList(1), 0,  1 },
            { asList(1), 1,  null },
            { asList(1), -1, null },
            { asList(1), -2, null },
            { asList(1, 2), 0,  1 },
            { asList(1, 2), 1,  2 },
            { asList(1, 2), -1,  2 },
            { asList(1, 2), 2,  null },
            { asList(1, 2), -2,  null },
            { asList(1, 2), -20,  null }
        };
    }

    @Test(dataProvider = "createTestToUnstructuredLogRecordData")
    public void testToUnstructuredLogRecord(String messageTemplate, Object keywordArgs,
            List<Object> positionalArgs,
            String expectedFormat, List<String> expectedFormatArgs) {
        LogMessageTemplate instance = new EmbeddableLogRecordImpl(messageTemplate, keywordArgs,
            positionalArgs);
        LogMessageTemplate.Unstructured output = instance.toUnstructuredLogRecord();
        String actualFormat = output.getFormatString();
        assertEquals(actualFormat, expectedFormat);
        List<String> actualFormatArgs = output.getFormatArgs().stream()
            .map(x -> x.toString()).collect(Collectors.toList());
        assertEquals(actualFormatArgs, expectedFormatArgs);
    }

    @DataProvider
    public Object[][] createTestToUnstructuredLogRecordData() {
        return new Object[][]{
            { "", toMap(), asList(), "", asList() },
            { "{a}{0}", toMap("a", "yes"), asList(1), "{}{}", asList("\"yes\"", "1") },
            { "{}{0}", toMap("a", "yes"), asList("1"), "{}{}", asList("{\"a\":\"yes\"}", "1") },
            { "{$}{$0}", toMap("a", "yes"), asList("1"), "{}{}", asList("{a=yes}", "1") },
            { "{@}{@0}", toMap("a", "yes"), asList("1"), "{}{}", asList("{\"a\":\"yes\"}", "\"1\"") }
        };
    }

    @Test(dataProvider = "createTestToStructuredLogRecordData")
    public void testToStructuredLogRecord(String messageTemplate, Object keywordArgs,
            List<Object> positionalArgs, String expected) {
        LogMessageTemplate instance = new EmbeddableLogRecordImpl(messageTemplate, keywordArgs,
            positionalArgs);
        Object actual = instance.toStructuredLogRecord().toString();
        assertEquals(actual, expected);
    }

    @DataProvider
    public Object[][] createTestToStructuredLogRecordData() {
        return new Object[][]{
            { "", toMap(), asList(), "{}" },
            { "{a}{0}", toMap("a", "yes"), null, "{\"a\":\"yes\"}" },
            { "{}{0}", toMap("a", "yes", "b", 2), null, "{\"a\":\"yes\",\"b\":2}" },
            { "{$}{$0}", asList("a", "yes"), null, "[\"a\",\"yes\"]" },
            { "{@}{@0}", "yes", null, "\"yes\"" }
        };
    }

    @Test(dataProvider = "createTestToStringData")
    public void testToString(String messageTemplate, Object keywordArgs,
            List<Object> positionalArgs, String expected) {
        LogMessageTemplate instance = new EmbeddableLogRecordImpl(messageTemplate, keywordArgs,
            positionalArgs);
        String actual = instance.toString();
        assertEquals(actual, expected);
    }

    @DataProvider
    public Object[][] createTestToStringData() {
        return new Object[][]{
            { "", toMap(), asList(), "" },
            { "{a}{0}", toMap("a", "yes"), asList(1), "\"yes\"1" },
            { "{}{0}", toMap("a", "yes"), asList("1"), "{\"a\":\"yes\"}1" },
            { "{$}{$0}", toMap("a", "yes"), asList("1"), "{a=yes}1" },
            { "{@}{@0}", toMap("a", "yes"), asList("1"), "{\"a\":\"yes\"}\"1\"" },
            { "{@}{@0}{8}", toMap("a", "yes"), asList("1"), "{\"a\":\"yes\"}\"1\"{8}" }
        };
    }
}