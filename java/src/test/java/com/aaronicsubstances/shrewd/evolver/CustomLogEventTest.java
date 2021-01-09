package com.aaronicsubstances.shrewd.evolver;

import java.util.List;

import static java.util.Arrays.asList;
import static com.aaronicsubstances.shrewd.evolver.TestUtils.toMap;

import org.testng.annotations.DataProvider;
import org.testng.annotations.Test;

import static org.testng.Assert.*;

public class CustomLogEventTest {
    
    public void testAddProperty() {
        CustomLogEvent instance = new CustomLogEvent();
        assertEquals(instance.getData(), null);

        instance.addProperty("age", 20);
        assertEquals(instance.getData(), toMap("age", 20));

        instance.addProperty("name", "Kofi");
        assertEquals(instance.getData(), toMap("age", 20, "name", "Kofi"));
    }

    public void testGenerateMessage() {
        CustomLogEvent instance = new CustomLogEvent();
        instance.setData(toMap("person", toMap("name", "Kofi"),
            "age", "twenty"));

        instance.generateMessage((j, s) -> j.apply("person/name") + " is " +
            s.apply("age") + " years old.");
        assertEquals(instance.getMessage(), "\"Kofi\" is twenty years old.");
    }
    
    
    @Test(dataProvider = "creatTestGetTreeDataSliceData")
    public void testGetTreeDataSlice(Object treeData, String path, Object expected) {
        CustomLogEvent instance = new CustomLogEvent();
        instance.setData(treeData);
        Object actual = instance.fetchDataSlice(path);
        assertEquals(actual, expected);
    }

    @DataProvider
    public Object[][] creatTestGetTreeDataSliceData() {
        return new Object[][]{
            { null, "", null },
            { "", "", "" },
            { 2, "", 2 },
            { asList(2), "f", null },
            { asList(2), "0", 2 },
            { asList(21, asList()), "10", null },
            { asList(21, asList()), "0", 21 },
            { asList(21, asList()), "1", asList() },
            { asList(21, asList()), "-1", asList() },
            { asList(21, asList()), "-2", null },
            { toMap("a", 1), "", toMap("a", 1) },
            { toMap("a", 1), "0", null },
            { toMap("a", 1), "a", 1 },
            { toMap("a", 1, "b", 2), "b", 2 },
            { toMap("a", 1, "b", 2), "c", null },
            { toMap("a", 1, "b", asList("e", true)), "b/0", "e" },
            { toMap("a", 1, "b", asList("e", true)), "b/1", true },
            { toMap("a", 1, "b", toMap("e", true)), "b", toMap("e", true) },
            { toMap("a", 1, "b", toMap("e", true)), "b/e", true }
        };
    }
}