package com.aaronicsubstances.shrewd.evolver;

import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Objects;

public class TreeDataMatcher {
    enum TreeNodeType {
        OBJECT, ARRAY, NUMBER, BOOLEAN, STRING, NULL
    }
    
    private final Object expected;
    private final String expectedDescription;
    private double realNumberComparisonTolerance = 1e-6;

    public TreeDataMatcher(Object expected) {
        this(expected, "");
    }

    public TreeDataMatcher(Object expected, String expectedDescription) {
        this.expected = expected;
        this.expectedDescription = expectedDescription;
    }

    public double getRealNumberComparisonTolerance() {
        return realNumberComparisonTolerance;
    }

    public void setRealNumberComparisonTolerance(double realNumberComparisonTolerance) {
        this.realNumberComparisonTolerance = realNumberComparisonTolerance;
    }

    public void assertEquivalentTo(Object actual) {
        assertEquivalentTo(actual, "", new LinkedHashMap<>());
    }

    protected void workOnEquivalenceAssertion(Object expected, Object actual,
            String pathToActual, Map<String, String> pathExpectations) {
        /* Possible errors:
            - types not the same
            - non object, non array and non equal.
            - arrays, but different lengths
            - objects, key not found
        */
        expected = normalizeTreeNode(expected);
        actual = normalizeTreeNode(actual);
        TreeNodeType expectedType = getTreeNodeType(expected);
        TreeNodeType actualType = getTreeNodeType(actual);
        if (expectedType != actualType) {
            // mismatch found
        }
        if (expectedType == TreeNodeType.OBJECT) {
            Map<String, Object> expectedMap = (Map) expected;
            Map<String, Object> actualMap = (Map) actual;
            for (Map.Entry<String, Object> expectedEntry : expectedMap.entrySet()) {
                if (!actualMap.containsKey(expectedEntry.getKey())) {
                    // mismatch found
                }
                Object correspondingExpected = expectedEntry.getValue();
                Object correspondingActual = actualMap.get(expectedEntry.getKey());
                workOnEquivalenceAssertion(correspondingExpected, correspondingActual, 
                    String.format("%s.%s", pathToActual, expectedEntry.getKey()), pathExpectations);
            }
        }
        else if (expectedType == TreeNodeType.ARRAY) {
            List<Object> expectedList = (List) expected;
            List<Object> actualList = (List) actual;
            if (expectedList.size() != actualList.size()) {
                // mismatch found
            }
            for (int i = 0; i < expectedList.size(); i++) {
                Object correspondingExpected = expectedList.get(i);
                Object correspondingActual = actualList.get(i);
                workOnEquivalenceAssertion(correspondingExpected, correspondingActual, 
                    String.format("%s[%d]", pathToActual, i), pathExpectations);
            }
        }
        else {
            if (!areLeafNodesEqual(actual, expected)) {
                // mismatch found
            }
        }
    }

    protected Object normalizeTreeNode(Object node) {
        return node;
    }

    protected boolean areLeafNodesEqual(Object actual, Object expected) {
        if (actual == null || expected == null) {
            return actual == expected;
        }
        else {
            if (actual instanceof Double) {
                double diff = ((Double) actual).doubleValue() - ((Double) expected).doubleValue();
                return Math.abs(diff) <= realNumberComparisonTolerance;
            }
            else if (actual instanceof Float) {
                double diff = ((Float) actual).doubleValue() - ((Float) expected).doubleValue();
                return Math.abs(diff) <= realNumberComparisonTolerance;
            }
            else {
                return actual.equals(expected);
            }
        }
    }

    static String wrapAssertionError(String message, String pathToActual,
            Map<String, String> pathExpectations) {
        return message;
    }

    private void assertEquivalentTo(Object actual, String pathToActual,
            Map<String, String> pathExpectations) {
        String expectation = getExpectedDescription();
        if (expectation != null && !expectation.isEmpty()) {
            // For correctness throughout nested calls, recreate rather
            // than modify in place.
            pathExpectations = new LinkedHashMap<>(pathExpectations);
            pathExpectations.put(pathToActual, expectation);
        }
        if (expected instanceof TreeDataMatcher) {
            // It is up to nested matcher to include full node path and expectations
            // encountered along the way in any assertion error it raises.
            TreeDataMatcher nestedMatcher = (TreeDataMatcher) expected;
            nestedMatcher.assertEquivalentTo(actual, pathToActual, pathExpectations);
        }
        else {
            workOnEquivalenceAssertion(expected, actual, pathToActual, pathExpectations);
        }
    }

    private String getExpectedDescription() {
        if (expectedDescription != null) {
            return expectedDescription;
        }
        return Objects.toString(expected);
    }

    private TreeNodeType getTreeNodeType(Object node) {
        if (node == null) {
            return TreeNodeType.NULL;
        }
        if (node instanceof Number) {
            // double, long, decimal
            return TreeNodeType.NUMBER;
        }
        if (node instanceof Boolean) {
            return TreeNodeType.BOOLEAN;
        }
        if (node instanceof String) {
            return TreeNodeType.STRING;
        }
        if (node instanceof List) {
            return TreeNodeType.ARRAY;
        }
        if (node instanceof Map) {
            return TreeNodeType.OBJECT;
        }
        throw new RuntimeException("Unsupported node type: " + node.getClass());
    }
}