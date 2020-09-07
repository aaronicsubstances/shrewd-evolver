# Shrewd Evolver

This projects seeks to provide tools helpful for automated testing, maintenance and evolution of software projects.

Currently the project contains source files written Java 8 and C# 6, and are meant to be copied over into projects, and even ported into other programming languages. Groups of one or two source files consitute independent modules of functionality for helping the software engineer with this project's goals.

The following provide information on the implemented modules.

## TreeDataMatcher

This module consists of a sole **TreeDataMatcher** class for comparing two tree data structures in automated tests, in which expectations are defined with anonymous objects, and actual values are deserialized from file, database or network. It is similar in intent to C#.NET Fluent Assertion's [object graph comparison](https://fluentassertions.com/objectgraphs/) facility, but is designed to be independent of any language programming language, by requiring clients to think in terms of tree data structures representable in JSON.

This module intended use case is integration testing, and particularly applies to cases in which the software under test runs in a process separate from that of the test cases. Then testing can be done in two main ways:

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

Finally subclass method hooks exist in *TreeDataMatcher* to extend its functionality.

## LogNavigator

This module comprises the **LogNavigator** and **LogPositionHolder** classes. It is meant to automate some aspects of manual system testing by leveraging structured logging libraries.

Till date, in spite of the noteworthy computer science advances, when all else fails to discover the cause of a bug, the last resort is "to insert a log statement" and rerun the program. Why then don't we take this a step further, and build a white box system testing philosophy on top of it? 

That is what *LogNavigator* module is meant for, to demonstrate and promote a white box system testing philosophy based on logging. This approach to testing should practically solve the problem of testing a codebase not designed for automated testability. To do this, 

   1. The software logging system should be configured to save these logging statements in a database (seen by most logging libraries as an appender or sink).
   1. The software system should be configurable enough to detach the database appender or sink during runtime, when testing is not being done; and to re-attach when testing has to be done. A configuration value indicating whether or not "test log generation" is enabled should be available to code emitting such logs, so as to skip unnecessary emissions.
   1. Logging system or library must always write logs in order (ie first in first out policy). 
   1. Optionally the logging system should be configurable to require immediate saving of test logs (possibly blocking a thread) or allow async test logging. It may be of help depending on testing context.
   1. The programmer has to insert logging statements meant for test cases, and with **log position identification** and **embedded structured logs**.
   
       1. Every test log section should have a UUID/GUID in it to identify it to future maintainers of the software. By so doing, every log at runtime can be traced to the source code point responsible.
     
       1. It should be possible to serialize test data to the underlying database  appender or sink for deserialization and use by test cases. Structured logging libraries like *SLF4J 2.0.0* and *Serilog* provide almost all that is need to embed serialized test data in a database. All that should be left for software developer is to configure the system to saving data from a particular key in a log event. 
   
So for example, supposing we wanted to test linear search in Java (this example is contrived for illustration purposes, but is very practical for testing in the midst of encapsulation, multithreading, single thread concurrency, graphical user interface, database access, external API callbacks, or offline background processing).

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

Using **LogNavigation** with a library like *SLF4J*, one can alternatively write test code, first by modifying linearSearch with logging statements to obtain something like this:

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
static class MyLogRecord implements LogPositionHolder {
    public String positionId;
    public Integer indexValue;
    
    @Override
    public String loadPositionId() {
        return positionId;    
    }
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
        Arrays.asList("5d06d267-afce-4c8b-801f-f5a516c66774"))));
    MyLogRecord last = logNavigator.next(Arrays.asList("1fc11a08-b85f-45dc-9f2c-579aa6c1cc12"));
    assertFalse(logNavigator.hasNext());
    assertEquals(last.indexValue, expected);
}

@Test(dataProvider = "createTestLinearSearch2Data")
public void testLinearSearch2(List<String> items, String searchItem, int expected) {
    linearSearch(items, searchItem);
    List<MyLogRecord> logs = TestDB.load();
    LogNavigator<MyLogRecord> logNavigator = new LogNavigator<>(logs);
    assertNotNull(getPositionId(logNavigator.next(
        Arrays.asList("5d06d267-afce-4c8b-801f-f5a516c66774"))));
    MyLogRecord last = logNavigator.next(Arrays.asList("022d478c-e3ae-4faa-bd7f-67e5b6aa3c7e"));
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
        Arrays.asList("5d06d267-afce-4c8b-801f-f5a516c66774"))));
    for (int i = 0; i < 1; i++) {
        assertTrue(logNavigator.hasNext());
        MyLogRecord next = logNavigator.next();
        assertEquals(next.positionId, "7e0c9e2c-8fb1-4695-b6ad-b94a1c70f96f");
        assertEquals(next.indexValue, i + 1);
    }
    MyLogRecord last = logNavigator.next(Arrays.asList("1fc11a08-b85f-45dc-9f2c-579aa6c1cc12"));
    assertFalse(logNavigator.hasNext());
    assertEquals(last.indexValue, expected);
}

```

## LogMessageTemplate

This module comprises **LogMessageTemplate** and **LogMessageTemplateParser** classes. Its purpose is to implement a message template as described on [messagetemplates.org](https://messagetemplates.org/). The main goal is to encourage the generation of human readable logs in addition to the generation of structured logs. 

It works by receiving a log message template in the same syntax as that described on the Message Templates site, using *LogMessageTemplateParser* class. It also requires a tree data structure such as the ones used by **TreeDataMatcher** module, and a list of positional arguments. Using this three inputs, **LogMessageTemplate** class can generate both structured and unstructured strings, where the structured ones require a serialization implementation (e.g to JSON).

*Key differences in semantics with those on the Message Templates site are that positional arguments are distinct from keyword arguments; positional and keyword arguments can be present in a template at the same time; and a keyword can be a complex path referring to any node in the supplied tree data structure. A "hole" which is an integral index refers to a positional argument; otherwise it refers to a node in the tree data structure input. Also capturing rules and alignment and format options are not supported.*

The unstructured string is generated by referring to parts of the tree data structure in the message template with keyword arguments. Then upon request, the unstructured string can be generated. Any other data which needs to be present in unstructured string, but is not present in tree data can be supplied with positional arguments.

For example, given the tree data structure below

```cs
new
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
}
```

and the positional arguments

```cs
new List<object>{ 2 }
```

we can use the template string

```cs
@"
New record created on {$dateCreated} with {0} books. Price is {$price}.
The books are: {books[0].title}, {books[1].title}
"
```

to generate a single *LogMessageTemplate* subclass instance. Assuming the subclass was implemented to yield JSON, then calling toStructuredLogRecord() on the instance yields an object whose toString() in turn gives

```json
{
  "dateCreated": "2020-09-01T09:00",
  "price": 40,
  "books": [
    {
      "id": 2,
      "title": "Beavers",
      "inStock": false
    },
    {
      "id": 4,
      "title": "Shakers",
      "language": "en"
    }
  ]
}
```

On the other hand, calling toUnstructuredLogRecord() yields an object with this format string, assuming subclass was implemented to use SLF4J string format syntax:
```
New record created on {} with {} books. Price is {}.
The books are: {}, {}
```

and this list of format arguments:
```cs
new List<object>{ "2020-09-01T09:00", 2, 40, "\"Beavers\"", "\"Shakers\""}
```

Finally, calling toString() directly on *LogMessageTemplate* instance yields the string
```
New record created on 2020-09-01T09:00 with 2 books. Price is 40.
The books are: "Beavers", "Shakers"
```
