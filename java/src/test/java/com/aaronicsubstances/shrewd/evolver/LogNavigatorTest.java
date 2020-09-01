package com.aaronicsubstances.shrewd.evolver;

import static org.testng.Assert.*;

import java.util.Arrays;
import java.util.List;
import org.testng.annotations.Test;

public class LogNavigatorTest {
    
    static class LogPositionHolderImpl implements LogPositionHolder {
        private final String positionId;
    
        public LogPositionHolderImpl(String positionId) {
            this.positionId = positionId;
        }
    
        @Override
        public String loadPositionId() {
            return positionId;
        }
    }

    @Test
    public void testEmptiness() {
        LogNavigator<LogPositionHolderImpl> instance = new LogNavigator<>(Arrays.asList());
        assertEquals(instance.hasNext(), false);
        assertEquals(instance.nextIndex(), 0);
    }

    @Test
    public  void testNext() {
        List<LogPositionHolderImpl> testLogs = Arrays.asList(new LogPositionHolderImpl("a"), 
            new LogPositionHolderImpl("b"), new LogPositionHolderImpl("c"), 
            new LogPositionHolderImpl("c"));
        LogNavigator<LogPositionHolderImpl> instance = new LogNavigator<>(testLogs);
        
        for (int i = 0; i < testLogs.size(); i++) {
            assertEquals(instance.hasNext(), true);
            assertEquals(instance.nextIndex(), i);
            assertEquals(instance.next(), testLogs.get(i));
        }
        
        assertEquals(instance.hasNext(), false);
        assertEquals(instance.nextIndex(), 4);
    }
    
    @Test
    public void testNextWithSearchIds() {
        List<LogPositionHolderImpl> testLogs = Arrays.asList(new LogPositionHolderImpl("a"), 
            new LogPositionHolderImpl("b"), new LogPositionHolderImpl("c"),
            new LogPositionHolderImpl("c"));
        LogNavigator<LogPositionHolderImpl> instance = new LogNavigator<>(testLogs);
        
        assertEquals(instance.hasNext(), true);
        assertEquals(instance.nextIndex(), 0);
        assertEquals(instance.next(Arrays.asList("d")), null);
        
        assertEquals(instance.hasNext(), true);
        assertEquals(instance.nextIndex(), 0);
        assertEquals(instance.next(Arrays.asList("c", "b")), testLogs.get(1));
        
        assertEquals(instance.hasNext(), true);
        assertEquals(instance.nextIndex(), 2);
        assertEquals(instance.next(Arrays.asList("c")), testLogs.get(2));
        
        assertEquals(instance.hasNext(), true);
        assertEquals(instance.nextIndex(), 3);
        assertEquals(instance.next(Arrays.asList("a")), null);
        
        assertEquals(instance.hasNext(), true);
        assertEquals(instance.nextIndex(), 3);
        assertEquals(instance.next(Arrays.asList("c")), testLogs.get(3));
        
        assertEquals(instance.hasNext(), false);
        assertEquals(instance.nextIndex(), 4);
    }
    
    @Test
    public void testNextWithSearchAndLimitIds() {
        List<LogPositionHolderImpl> testLogs = Arrays.asList(new LogPositionHolderImpl("a"),
            new LogPositionHolderImpl("b"), new LogPositionHolderImpl("c"), 
            new LogPositionHolderImpl("c"));
        LogNavigator<LogPositionHolderImpl> instance = new LogNavigator<>(testLogs);
        
        assertEquals(instance.hasNext(), true);
        assertEquals(instance.nextIndex(), 0);
        assertEquals(instance.next(Arrays.asList("d"), null), null);
        
        assertEquals(instance.hasNext(), true);
        assertEquals(instance.nextIndex(), 0);
        assertEquals(instance.next(Arrays.asList("a"), Arrays.asList("b")), testLogs.get(0));
        
        assertEquals(instance.hasNext(), true);
        assertEquals(instance.nextIndex(), 1);
        assertEquals(instance.next(Arrays.asList("c"), Arrays.asList("b")), null);
        
        assertEquals(instance.hasNext(), true);
        assertEquals(instance.nextIndex(), 1);
        assertEquals(instance.next(Arrays.asList("b"), Arrays.asList("b")), null);
        
        assertEquals(instance.hasNext(), true);
        assertEquals(instance.nextIndex(), 1);
        assertEquals(instance.next(Arrays.asList("b"), Arrays.asList("c")), testLogs.get(1));
        
        assertEquals(instance.hasNext(), true);
        assertEquals(instance.nextIndex(), 2);
        assertEquals(instance.next(Arrays.asList("c"), Arrays.asList("c")), null);
        
        assertEquals(instance.hasNext(), true);
        assertEquals(instance.nextIndex(), 2);
        assertEquals(instance.next(Arrays.asList("c"), null), testLogs.get(2));
        
        assertEquals(instance.hasNext(), true);
        assertEquals(instance.nextIndex(), 3);
    }
}