# TreeDataMatcher

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
