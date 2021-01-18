# Shrewd Evolver

This projects seeks to provide tools helpful for automated testing, maintenance and evolution of software projects.

Currently the project contains source files written Java 8 and C# 6, and are meant to be copied over into projects, and even ported into other programming languages. Groups of one or two source files consitute independent modules of functionality for helping the software engineer with this project's goals.

The following provide information on the implemented modules.

## TreeDataMatcher

This module consists of a sole **TreeDataMatcher** class for comparing two tree data structures in automated tests, in which expectations are defined with anonymous objects, and actual values are deserialized from file, database or network. It is similar in intent to C#.NET Fluent Assertion's [object graph comparison](https://fluentassertions.com/objectgraphs/) facility, but is designed to be independent of any language programming language, by requiring clients to think in terms of tree data structures representable in JSON.

Intended use case is integration testing, particularly cases in which the software under test runs in a process separate from that of the test cases. Then testing can be done in two main ways:

  * In an HTTP API scenario (or equivalent networked system), the tests makes network requests to the process, obtains responses, and then use *TreeDataMatcher* to compare obtained actual responses against predefined expectations.
  * In an Android phone scenario (or equivalent IOS or embedded system), logs are inserted in program and are appended to SQLite database during manual testing of mobile application process by human user. After end of testing, the automated tests are run and predefined expectations are compared with actual results recorded in the database.

In the typical test case, one has to write data classes to represent expected and actual results. If using statically typed languages like Java or C#, then one has to implement equals(), hashCode() and toString() methods. And then if a test case fails, one has to take the toString() output generated for the expected and actual results, and use the eye or a diff tool to hunt down the differences. 

*TreeDataMatcher* eliminates all these inconveniences, and only requires that tree data structures be represented (or representable) with JSON data types: null, string, number, boolean, list, dictionary (or objects with properties in languages which have such a concept). Test cases become a delight to write, especially in languages like C# in which anonymous objects are so easy to create, such as

```cs
var expected = new
{
    price = 40m,
    books = new List<object>
    {
        new
        {
            id = 2,
            title = "Beavers"
        },
        new
        {
            id = 4,
            title = "Shakers"
        }
    }
};
```

And if a test case fails, the error message will include the exact part of the tree structure responsible: E.g.

```
java.lang.AssertionError: at {books[1].id}: expected [4] but found [5]
```

Another convenience provided by *TreeDataMatcher* is that it ignores extra object fields in actual results. So for example, the above defined expectation will also match an actual result like:

```cs
var actual = new
{
    dateCreated = "2020-09-01T09:00",
    price = 40m,
    books = new List<object>
    {
        new
        {
            id = 2,
            title = "Beavers",
            inStock = false
        },
        new
        {
            id = 4,
            title = "Shakers",
            language = "en"
        }
    }
};
```

Finally subclass method hooks exist in *TreeDataMatcher* to extend its functionality. Such subclasses can even be conveniently hooked into a larger expected tree data structure at any point. Thus subclasses can be used at specific points only, and developer can still take advantage of existing *TreeDataMatcher* functionality every where else. 

As an example of a simple subclass, one can create a subclass to match all actual values aside nulls.

```cs
class AnyNonNullMatcher : TreeDataMatcher
{
    public AnyNonNullMatcher():
        base(null, "not null")
    { }

    protected override void WorkOnEquivalenceAssertion(object expected, object actual, string pathToActual,
            Dictionary<string, string> pathExpectations, int maxRecursionDepthRemaining)
    {
        if (actual == null)
        {
            ReportError("is null", pathToActual, pathExpectations);
        }
    }
}
```

This subclass can then be used at specific points where needed, so that expectation can be defined as

```cs
var expected = new
{
    dateCreated = "2020-09-01T09:00",
    price = new AnyNonNullMatcher()
};
```

and this expectation will match a tree data structure such as
```cs
var actual = new
{
    dateCreated = "2020-09-01T09:00",
    price = 40m
};
```

but not this

```cs
var actual = new
{
    dateCreated = "2020-09-01T09:00",
    price = null
};
```

## LogNavigator

This module comprises the **LogNavigator** class. It is meant to automate some aspects of manual system testing by leveraging logging libraries.

Till date, in spite of the noteworthy computer science advances, when all else fails to discover the cause of a bug, the last resort is "to insert a log statement" and rerun the program. Why then don't we take this a step further, and build a white box system testing philosophy on top of it? 

That is what *LogNavigator* module is meant for, to demonstrate and promote a white box system testing philosophy based on logging. This approach to testing should practically solve the problem of testing a codebase not designed for automated testability. To do this, 

   1. The software logging system should be configured to save these logging statements in a database (seen by most logging libraries as an appender or sink or target).
   1. The software system should be configurable enough to detach the database appender during runtime when testing is not being done, and to re-attach when test logs have to be gathered.
   1. There must be a logger dedicated to test log appender whose level is binary. That level can be used at runtime to skip unnecessary emissions. Ideally, test log appender should receive only test logs.
   1. Logging system or library must always write logs in order (i.e. first in first out policy).
   1. Optionally the logging system should be configurable to support both immediate and async saving of test logs. It may be of help depending on testing context.
   1. The programmer has to insert logging statements meant for test cases with **properties or key-value pairs attached**. It should be possible to serialize test data to the underlying database appender as key value pairs, for deserialization and use by test cases.
   
So for example, supposing we wanted to test linear search in Java (this example is contrived for illustration purposes, but is very practical for testing in the midst of information hiding design practices, multithreading, single thread concurrency, graphical user interface, database access, and "out-of-band" background processing).

```java
public int linearSearch(List<String> items, String searchItem) {
    for (int i = 0; i < items.size(); i++) {
        if (searchItem.equals(items.get(i))) {
            return i;        
        }
    }
    return -1;
}
```

The black box way to test this (actually the best for this example) is to use a library like *TestNG* to write testing code like this:

```java
@Test(dataProvider = "createTestLinearSearchData")
public void testLinearSearch(List<String> items, String searchItem, int expected) {
    int actual = linearSearch(items, searchItem);
    assertEquals(actual, expected);
}

@DataProvider
public Object[][] createTestLinearSearchData() {
    return new Object[][]{
        { Arrays.asList(), "2", -1 },
        { Arrays.asList("2", "4"), "2", 0 },
        { Arrays.asList("2", "4"), "4", 1 },
        { Arrays.asList("2", "4", "8"), "8", 2 },
        { Arrays.asList("8"), "2", -1 }
    };
}
```

Using **LogNavigation** with a library like *SLF4J 2.0.0*, one can alternatively write test code, first by modifying linearSearch with logging statements to obtain something like this:

```java
public int linearSearch(List<String> items, String searchItem) {
    logger.atDebug()
        .addKeyValue("positionId", "5d06d267-afce-4c8b-801f-f5a516c66774")
        .log("Linear search started.");
    for (int i = 0; i < items.size(); i++) {
        if (searchItem.equals(items.get(i))) {
            logger.atDebug()
                .addKeyValue("positionId", "022d478c-e3ae-4faa-bd7f-67e5b6aa3c7e")
                .addArgument(i)
                .log("Search item found at: {}");
            return i;        
        }
        logger.atDebug()
            .addKeyValue("positionId", "7e0c9e2c-8fb1-4695-b6ad-b94a1c70f96f")
            .addArgument(i + 1)
            .log("Search item not found after {} iteration(s).");
    }
        
    logger.atDebug()
        .addKeyValue("positionId", "1fc11a08-b85f-45dc-9f2c-579aa6c1cc12")
        .addArgument(-1)
        .log("Search item not found. Returning {}.");
    return -1;
}
```

Then white box based system testing will look like this:

```java
static class MyLogRecord {
    public String positionId;
    public Integer indexValue;
}

static class TestDB {
    public static void clear() {
        throw new UnsupportedOperationException("Implement database truncation before every test method runs");    
    }
    
    public static List<MyLogRecord> load() {
        throw new UnsupportedOperationException("Implement loading of logs to be navigated");
    }
}
    
public static String getPositionId(LogRecord instance) {
    return instance == null ? null : instance.positionId;
}
    
public static Integer getIndexValue(LogRecord instance) {
    return instance == null ? null : instance.indexValue;
}

@BeforeTest
public void setUpTest() {
    TestDB.clear();
}

@Test
public void testLinearSearch1() {
    List<String> items = Arrays.asList();
    String searchItem = "2";
    int expected = -1;
    linearSearch(items, searchItem);
    List<MyLogRecord> logs = TestDB.load();
    LogNavigator<MyLogRecord> logNavigator = new LogNavigator<>(logs);
    assertNotNull(getPositionId(logNavigator.next(
        x -> x.positionId == "5d06d267-afce-4c8b-801f-f5a516c66774")));
    MyLogRecord last = logNavigator.next(x -> x.positionId == "1fc11a08-b85f-45dc-9f2c-579aa6c1cc12");
    assertFalse(logNavigator.hasNext());
    assertEquals(last.indexValue, expected);
}

@Test(dataProvider = "createTestLinearSearch2Data")
public void testLinearSearch2(List<String> items, String searchItem, int expected) {
    linearSearch(items, searchItem);
    List<MyLogRecord> logs = TestDB.load();
    LogNavigator<MyLogRecord> logNavigator = new LogNavigator<>(logs);
    assertNotNull(getPositionId(logNavigator.next(
        x -> x.positionId == "5d06d267-afce-4c8b-801f-f5a516c66774")));
    MyLogRecord last = logNavigator.next(x -> x.positionId == "022d478c-e3ae-4faa-bd7f-67e5b6aa3c7e");
    assertFalse(logNavigator.hasNext());
    assertEquals(last.indexValue, expected);
}

@DataProvider
public Object[][] createTestLinearSearch2Data() {
    return new Object[][]{
        { Arrays.asList("2", "4"), "2", 0 },
        { Arrays.asList("2", "4"), "4", 1 },
        { Arrays.asList("2", "4", "8"), "8", 2 }
    };
}

@Test
public void testLinearSearch3() {
    List<String> items = Arrays.asList("8");
    String searchItem = "2";
    int expected = -1;
    linearSearch(items, searchItem);
    List<MyLogRecord> logs = TestDB.load();
    LogNavigator<MyLogRecord> logNavigator = new LogNavigator<>(logs);
    assertNotNull(getPositionId(logNavigator.next(
        x -> x.positionId == "5d06d267-afce-4c8b-801f-f5a516c66774")));
    for (int i = 0; i < 1; i++) {
        assertTrue(logNavigator.hasNext());
        MyLogRecord next = logNavigator.next();
        assertEquals(next.positionId, "7e0c9e2c-8fb1-4695-b6ad-b94a1c70f96f");
        assertEquals(next.indexValue, i + 1);
    }
    MyLogRecord last = logNavigator.next(x -> x.positionId == "1fc11a08-b85f-45dc-9f2c-579aa6c1cc12");
    assertFalse(logNavigator.hasNext());
    assertEquals(last.indexValue, expected);
}

```

## CustomLogEvent

This module comprises **CustomLogEvent** class. Its main goal is to encourage the generation of structured logs without the need for a structured logging library.

For example, given the tree data structure below:
```json
{
    "person": {
        "name": "Kofi"
    },
    "age": "twenty"
}
```

the following code snippet demonstrates use of the class to fetch parts of the tree data structure above to generate a log message:

```cs
var instance = new CustomLogEvent(GetType());
dynamic properties = new ExpandoObject();
instance.Data = properties;
properties.person = new Dictionary<string, string>
{
    { "name", "Kofi" }
};
properties.age = "twenty";

instance.GenerateMessage((j, s) => $"{j($"person/name")} is " +
    $"{s($"age")} years old.");
Assert.Equal("\"Kofi\" is twenty years old.", instance.Message);
```

*NB: The j() function serializes its argument as JSON; the s() function calls ToString().*
