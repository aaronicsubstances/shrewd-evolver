package com.aaronicsubstances.shrewd.evolver;

import static org.testng.Assert.*;

import java.util.Arrays;
import java.util.List;
import org.testng.annotations.Test;

public class LogNavigatorTest {
    
    static class LogPositionHolderImpl {
        private final String positionId;
    
        public LogPositionHolderImpl(String positionId) {
            this.positionId = positionId;
        }
    
        public String getPositionId() {
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
        assertEquals(instance.next(x -> x.getPositionId() == "d"), null);
        
        assertEquals(instance.hasNext(), true);
        assertEquals(instance.nextIndex(), 0);
        assertEquals(instance.next(x -> Arrays.asList("c", "b").contains(x.getPositionId())), testLogs.get(1));
        
        assertEquals(instance.hasNext(), true);
        assertEquals(instance.nextIndex(), 2);
        assertEquals(instance.next(x -> x.getPositionId() == "c"), testLogs.get(2));
        
        assertEquals(instance.hasNext(), true);
        assertEquals(instance.nextIndex(), 3);
        assertEquals(instance.next(x -> x.getPositionId() == "a"), null);
        
        assertEquals(instance.hasNext(), true);
        assertEquals(instance.nextIndex(), 3);
        assertEquals(instance.next(x -> x.getPositionId() == "c"), testLogs.get(3));
        
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
        assertEquals(instance.next(x -> x.getPositionId() == "d", null), null);
        
        assertEquals(instance.hasNext(), true);
        assertEquals(instance.nextIndex(), 0);
        assertEquals(instance.next(x -> x.getPositionId() == "a", x -> x.getPositionId() == "b"), testLogs.get(0));
        
        assertEquals(instance.hasNext(), true);
        assertEquals(instance.nextIndex(), 1);
        assertEquals(instance.next(x -> x.getPositionId() == "c", x -> x.getPositionId() == "b"), null);
        
        assertEquals(instance.hasNext(), true);
        assertEquals(instance.nextIndex(), 1);
        assertEquals(instance.next(x -> x.getPositionId() == "b", x -> x.getPositionId() == "b"), null);
        
        assertEquals(instance.hasNext(), true);
        assertEquals(instance.nextIndex(), 1);
        assertEquals(instance.next(x -> x.getPositionId() == "b", x -> x.getPositionId() == "c"), testLogs.get(1));
        
        assertEquals(instance.hasNext(), true);
        assertEquals(instance.nextIndex(), 2);
        assertEquals(instance.next(x -> x.getPositionId() == "c", x -> x.getPositionId() == "c"), null);
        
        assertEquals(instance.hasNext(), true);
        assertEquals(instance.nextIndex(), 2);
        assertEquals(instance.next(x -> x.getPositionId() == "c", null), testLogs.get(2));
        
        assertEquals(instance.hasNext(), true);
        assertEquals(instance.nextIndex(), 3);
    }
}