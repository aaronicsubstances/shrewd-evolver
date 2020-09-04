package com.aaronicsubstances.shrewd.evolver;

import java.util.List;

import static java.util.Arrays.asList;
import static com.aaronicsubstances.shrewd.evolver.TestUtils.toMap;

import com.google.gson.Gson;

import org.testng.annotations.DataProvider;
import org.testng.annotations.Test;

import static org.testng.Assert.*;

public class EmbeddableLogRecordTest {

    public static class EmbeddableLogRecordImpl extends EmbeddableLogRecord {
        private static final Gson JSON = new Gson();

        public EmbeddableLogRecordImpl(String formatString, Object treeData,
                List<Object> positionalArgs) {
            super(formatString, treeData, positionalArgs);
        }

        protected String serializeData(Object treeDataSlice) {
            return JSON.toJson(treeDataSlice);
        }
    }
    
    @Test(dataProvider = "creatTestGetTreeDataSliceData")
    public void testGetTreeDataSlice(Object treeData, List<Object> treeDataKey, Object expected) {
        EmbeddableLogRecord instance = new EmbeddableLogRecordImpl("", treeData, null);
        Object actual = instance.getTreeDataSlice(treeData, treeDataKey);
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
        EmbeddableLogRecord instance = new EmbeddableLogRecordImpl("", null, args);
        Object actual = instance.getPositionalArg(args, index);
        assertEquals(actual, expected);
    }

    @DataProvider
    public Object[][] createTestGetPositionalArgData() {
        return new Object[][]{
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
}