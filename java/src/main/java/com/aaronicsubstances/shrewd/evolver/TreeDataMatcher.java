package com.aaronicsubstances.shrewd.evolver;

import java.util.ArrayList;
import java.util.Collections;
import java.util.HashMap;
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
        this(expected, false);
    }

    public TreeDataMatcher(Object expected, boolean generateDescription) {
        this(expected, generateDescription ? null: "");
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
        Map<String, String> pathExpectations = new HashMap<>();
        String actualDescription = serializeTreeNode(actual);
        String startPath = "";
        pathExpectations.put(startPath, "actual: " + actualDescription);
        assertEquivalentTo(actual, startPath, pathExpectations);
    }

    protected String serializeTreeNode(Object node) {
        return Objects.toString(node);
    }

    protected void workOnEquivalenceAssertion(Object expected, Object actual,
            String pathToActual, Map<String, String> pathExpectations) {                
        if (expected instanceof TreeDataMatcher) {
            // It is up to nested matcher to include full node path and expectations
            // encountered along the way in any assertion error it raises.
            TreeDataMatcher nestedMatcher = (TreeDataMatcher) expected;
            nestedMatcher.assertEquivalentTo(actual, pathToActual, pathExpectations);
            return;
        }
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
            String message = String.format("expected type %s but found %s",
                expectedType, actualType);
            reportMismatch(message, pathToActual, pathExpectations);
        }
        else if (expectedType == TreeNodeType.OBJECT) {
            Map<String, Object> expectedMap = (Map) expected;
            Map<String, Object> actualMap = (Map) actual;
            for (Map.Entry<String, Object> expectedEntry : expectedMap.entrySet()) {
                if (!actualMap.containsKey(expectedEntry.getKey())) {
                    // mismatch found
                    String message = String.format("expected object property [%s] but was not found",
                        expectedEntry.getKey());
                    reportMismatch(message, pathToActual, pathExpectations);
                    continue;
                }
                Object correspondingExpected = expectedEntry.getValue();
                Object correspondingActual = actualMap.get(expectedEntry.getKey());
                workOnEquivalenceAssertion(correspondingExpected, correspondingActual, 
                    String.format("%s%s%s", pathToActual, 
                        (pathToActual.isEmpty() ? "": "."), expectedEntry.getKey()), 
                    pathExpectations);
            }
        }
        else if (expectedType == TreeNodeType.ARRAY) {
            List<Object> expectedList = (List) expected;
            List<Object> actualList = (List) actual;
            if (expectedList.size() != actualList.size()) {
                // mismatch found
                String message = String.format("expected array length %d but found %d",
                    expectedList.size(), actualList.size());
                reportMismatch(message, pathToActual, pathExpectations);
            }
            int commonSectionLength = Math.min(expectedList.size(), actualList.size());
            for (int i = 0; i < commonSectionLength; i++) {
                Object correspondingExpected = expectedList.get(i);
                Object correspondingActual = actualList.get(i);
                workOnEquivalenceAssertion(correspondingExpected, correspondingActual, 
                    String.format("%s[%d]", pathToActual, i), pathExpectations);
            }
        }
        else {
            if (!areLeafNodesEqual(actual, expected)) {
                // mismatch found
                String message = String.format("expected [%s] but found [%s]",
                    expected, actual);
                reportMismatch(message, pathToActual, pathExpectations);
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

    protected void reportMismatch(String message, String pathToActual,
            Map<String, String> pathExpectations) {
        message = wrapAssertionError(message, pathToActual, pathExpectations);
        throw new AssertionError(message);
    }

    static String wrapAssertionError(String message, String pathToActual,
            Map<String, String> pathExpectations) {
        StringBuilder fullMessage = new StringBuilder();
        fullMessage.append("at {").append(pathToActual).append("}: ");
        fullMessage.append(message);
        if (!pathExpectations.isEmpty()) {
            fullMessage.append("\n\n");
            String title = "Match Attempt Details";
            fullMessage.append(title).append("\n");
            for (int i = 0; i < title.length(); i++) {
                fullMessage.append("-");
            }
            fullMessage.append("\n");
            List<String> expKeys = new ArrayList<>(pathExpectations.keySet());
            Collections.sort(expKeys, (a, b)-> Integer.compare(a.length(), b.length()));
            for (String expPath: expKeys) {
                fullMessage.append("  at {").append(expPath).append("}: ");
                fullMessage.append(pathExpectations.get(expPath)).append("\n");
            }
        }
        return fullMessage.toString();
    }

    private void assertEquivalentTo(Object actual, String pathToActual,
            Map<String, String> pathExpectations) {
        String expectation = getExpectedDescription();
        if (expectation != null && !expectation.isEmpty()) {
            // For correctness throughout recursive calls, recreate rather
            // than modify in place.
            pathExpectations = new HashMap<>(pathExpectations);
            String previous = "";
            if (pathExpectations.containsKey(pathToActual)) {
                previous = pathExpectations.get(pathToActual) + "; ";
            }
            pathExpectations.put(pathToActual, previous + "expected: " + expectation);
        }
        workOnEquivalenceAssertion(expected, actual, pathToActual, pathExpectations);
    }

    private String getExpectedDescription() {
        if (expectedDescription != null) {
            return expectedDescription;
        }
        return serializeTreeNode(expected);
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