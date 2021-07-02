// version: 1.0.0
package com.aaronicsubstances.shrewd.evolver;

import java.lang.reflect.Method;
import java.math.BigDecimal;
import java.math.BigInteger;
import java.util.ArrayList;
import java.util.Collections;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Objects;

public class TreeDataMatcher {

    public enum TreeNodeType {
        OBJECT, ARRAY, NUMBER, BOOLEAN, STRING, NULL
    }

    public static TreeNodeType getTreeNodeType(Object node, boolean validate) {
        if (node == null) {
            return TreeNodeType.NULL;
        }
        if (node instanceof Number) {
            // integer, floating point, decimal
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
        if (!validate) {
            return null;
        }
        throw new RuntimeException("Unsupported node type: " + node.getClass());
    }
    
    private final Object expected;
    private final String expectedDescription;

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

    public void assertEquivalentTo(Object actual) {
        assertEquivalentTo(actual, 10);
    }

    public void assertEquivalentTo(Object actual, int maxRecursionDepth) {
        Map<String, String> pathExpectations = new HashMap<>();
        String actualDescription = serializeTreeNode(actual);
        String startPath = "";
        pathExpectations.put(startPath, "actual: " + actualDescription);
        assertEquivalentTo(actual, startPath, pathExpectations, maxRecursionDepth);
    }

    protected String serializeTreeNode(Object node) {
        return Objects.toString(node);
    }

    @SuppressWarnings("unchecked")
    protected void workOnEquivalenceAssertion(Object expected, Object actual,
            String pathToActual, Map<String, String> pathExpectations,
            int recursionDepthRemaining) {                
        if (expected instanceof TreeDataMatcher) {
            // It is up to nested matcher to include full node path and expectations
            // encountered along the way in any assertion error it raises.
            TreeDataMatcher nestedMatcher = (TreeDataMatcher) expected;
            nestedMatcher.assertEquivalentTo(actual, pathToActual, pathExpectations, 
                recursionDepthRemaining - 1);
            return;
        }
        /* Possible errors:
            - types not the same
            - non object, non array and non equal.
            - arrays, but different lengths
            - objects, key not found
        */
        TreeNodeType expectedType = getTreeNodeType(expected, false);
        if (expectedType == null) {
            expected = normalizeTreeNode(expected);
            expectedType = getTreeNodeType(expected, true);
        }
        TreeNodeType actualType = getTreeNodeType(actual, false);
        if (actualType == null) {
            actual = normalizeTreeNode(actual);
            actualType = getTreeNodeType(actual, true);
        }
        if (expectedType != actualType) {
            // mismatch found
            String message = String.format("expected type %s but found %s",
                expectedType, actualType);
            reportError(message, pathToActual, pathExpectations);
        }
        else if (expectedType == TreeNodeType.OBJECT) {
            Map<Object, Object> expectedMap = (Map<Object, Object>) expected;
            Map<Object, Object> actualMap = (Map<Object, Object>) actual;
            for (Map.Entry<Object, Object> expectedEntry : expectedMap.entrySet()) {
                if (!actualMap.containsKey(expectedEntry.getKey())) {
                    // mismatch found
                    String message = String.format("expected object property [%s] but was not found",
                        expectedEntry.getKey());
                    reportError(message, pathToActual, pathExpectations);
                    continue;
                }
                Object correspondingExpected = expectedEntry.getValue();
                Object correspondingActual = actualMap.get(expectedEntry.getKey());
                workOnEquivalenceAssertion(correspondingExpected, correspondingActual, 
                    String.format("%s%s%s", pathToActual, 
                        (pathToActual.isEmpty() ? "": "."), expectedEntry.getKey()), 
                    pathExpectations, recursionDepthRemaining - 1);
            }
        }
        else if (expectedType == TreeNodeType.ARRAY) {
            List<Object> expectedList = (List<Object>) expected;
            List<Object> actualList = (List<Object>) actual;
            if (expectedList.size() != actualList.size()) {
                // mismatch found
                String message = String.format("expected array length %d but found %d",
                    expectedList.size(), actualList.size());
                reportError(message, pathToActual, pathExpectations);
            }
            int commonSectionLength = Math.min(expectedList.size(), actualList.size());
            for (int i = 0; i < commonSectionLength; i++) {
                Object correspondingExpected = expectedList.get(i);
                Object correspondingActual = actualList.get(i);
                workOnEquivalenceAssertion(correspondingExpected, correspondingActual, 
                    String.format("%s[%d]", pathToActual, i), pathExpectations,
                    recursionDepthRemaining - 1);
            }
        }
        else {
            if (!areLeafNodesEqual(actual, expected)) {
                // mismatch found
                String message = String.format("expected [%s] but found [%s]",
                    expected, actual);
                reportError(message, pathToActual, pathExpectations);
            }
        }
    }

    protected Object normalizeTreeNode(Object node) {
        try {
            Class<?> propAccessorType = Class.forName("org.apache.commons.beanutils.PropertyUtils");
            Method propAccessor = propAccessorType.getMethod("describe", Object.class);
            return propAccessor.invoke(null, node);
        }
        catch (ClassNotFoundException ex) {
            return node;
        }
        catch (Exception ex) {
            if (ex instanceof RuntimeException) {
                throw (RuntimeException)ex;
            }
            throw new RuntimeException(ex);
        }
    }

    private boolean areLeafNodesEqual(Object actual, Object expected) {
        if (actual == null || expected == null) {
            return actual == expected;
        }
        if (!(actual instanceof Number)) {
            return actual.equals(expected);
        }

        // At this stage leaf nodes are numbers convertible to the following:
        // integer, floating point, decimal.
        // To check, 
        //   a. if floating point is involved, convert to double and use tolerance to compare.
        //   b. if actual and expected are of the same type, then compare directly.
        //   c. else if big decimal is involved, convert to big decimal and compare.
        //   d. else convert to big integer and compare
        if (isFloatingPoint(actual) || isFloatingPoint(expected)) {
            double actualFloat = convertToFloatingPoint(actual);
            double expectedFloat = convertToFloatingPoint(expected);
            return areFloatingPointNumbersCloseEnough(actualFloat, expectedFloat);
        }
        else if (actual.getClass().equals(expected.getClass())) {
            return actual.equals(expected);
        }
        else if (actual instanceof BigDecimal || expected instanceof BigDecimal) {
            List<BigDecimal> decimals = new ArrayList<>();
            for (Object op : new Object[]{actual, expected}) {
                if (op instanceof BigDecimal) {
                    decimals.add((BigDecimal) op);
                }
                else if (op instanceof BigInteger) {
                    decimals.add(new BigDecimal((BigInteger) op));
                }
                else {
                    decimals.add(new BigDecimal(convertToInteger(op)));
                }
            }
            return decimals.get(0).equals(decimals.get(1));
        }
        else {
            List<BigInteger> integers = new ArrayList<>();
            for (Object op : new Object[]{actual, expected}) {
                if (op instanceof BigInteger) {
                    integers.add((BigInteger) op);
                }
                else {
                    integers.add(BigInteger.valueOf(convertToInteger(op)));
                }
            }
            return integers.get(0).equals(integers.get(1));
        }
    }

    protected boolean areFloatingPointNumbersCloseEnough(double actual, double expected) {
        double diff = Math.abs(actual - expected);
        return diff <= 1e-6;
    }

    protected void reportError(String message, String pathToActual,
            Map<String, String> pathExpectations) {
        message = wrapAssertionError(message, pathToActual, pathExpectations);
        throw new AssertionError(message);
    }

    public static String wrapAssertionError(String message, String pathToActual,
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
            Map<String, String> pathExpectations, int recursionDepthRemaining) {
        if (recursionDepthRemaining <= 0) {
            reportError("Maximum recursion depth reached", pathToActual, pathExpectations);
            return;
        }
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
        workOnEquivalenceAssertion(expected, actual, pathToActual, pathExpectations,
            recursionDepthRemaining);
    }

    private String getExpectedDescription() {
        if (expectedDescription != null) {
            return expectedDescription;
        }
        return serializeTreeNode(expected);
    }

    private static boolean isFloatingPoint(Object number) {
        return number instanceof Double || number instanceof Float;
    }

    private static double convertToFloatingPoint(Object number) {
        return ((Number) number).doubleValue();
    }

    private static long convertToInteger(Object number) {
        return ((Number) number).longValue();
    }
}