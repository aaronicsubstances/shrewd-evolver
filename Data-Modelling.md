# Data Modelling

## Last Resort for conceptualizing Data Model regardless of Database Vendor

*Use the property-graph model*.

By property-graph model, I am referring to the near equivalent of entity-relational model in which both nodes and edges are organised into bags/multisets, distinction exists between nodes and edges/relationships, and distinction exists between one-sided relationships which cannot have properties, and all other kind of relationships.
  1. have a way to identify the type of each node, and each edge (ie either one-sided relationship which cannot have properties, or not). And in the case of edges, also have a way to identify the targets of the relationship (NB: not a big issue for SQL thanks to its foreign keys concept).
  1. abstract all relationships as potentially many-to-many to the public, i.e. hide one-sided relationship implementation details from the public.
  2. differentiate between the means of distinguishing entities for the purpose of establishing relationships in the property-graph model, from all other criteria for distinguishing entities, including criteria known to the public
     - E.g. always use internally-generated ids to identify entities for all programming purposes, including for forming relationships and for presenting to the public.

## Last Resort Options for achieving ACID

"ACID" here refers to Atomicity, Consistency, Isolation, Durability

  1. "ACID" databases
     - best at achieving durability.
     - best at achieving atomicty by abortability.
     - best at achieving snapshot isolation for readonly queries.
     - best at preventing access to results from uncommitted transactions.
     - insufficient and inflexible in achieving consistency by themselves.
  2. Applications and business rules
     - last resort for consistency.
  1. Try-Confirm-Cancel, mentioned by Pat Helland in 2007.
     - achieves isolation without forcing sequential processing.
  2. Log-based message broker
     - last resort for isolation, in which messages are processed one at a time, effectively forcing sequential processing, and hence achieving isolation.
     - last resort for atomicity by endless retrying

## Last Resort for Querying regardless of Database or Model

*Break querying into stages, consider involving application code to query the SQL/NoSQL query results, and consider involving "repeat querying".*
  - By "repeat querying", I mean: save either query result set or dataset from a data source, into embedded SQL databases (such as SQLite and DuckDB) or temporary SQL tables of popular RDBMses, query them in SQL, consider involving application code to query the SQL query results, save the result set to SQL/NoSQL storage, and repeat the querying and saving until the desired results are obtained.
  - The application-code side of querying should resemble MapReduce, ie is based on reduce (e.g. map, filter, selectMany, groupByAdjacent, application of window functions), sort, 
  merge-join (e.g. union, intersection, except, equi-join), and functions common to most databases, spreadsheet applications and programming languages (e.g. LIKE, uppercase, arithmetic).

This approach provides other related benefits:
  - Can serve as a last resort for traversing relationships in any database model, since traversing relationships is arguably equivalent to performing merge-joins.
  - Provides one solution to maintaining many-to-many relationships in document databases or hierarchical/tree data models, since traversing many-to-many relationships in relational data models is about performing multiple merge-join operations.
     - E.g. one can have the ids of two document collections in a relational database (either on demand at query time, or maintained in sync with document database at write time), perform many-to-many joins, and use the results to perform further queries in the document database.
  - Provides one solution to the problem of having too many filtering conditions in an SQL query, such that the SQL execution engine is forced to do table scans and dispense with indices. And that solution is to apply few filtering conditions which are most likely to exclude the highest possible
  number of non-matching rows. Then further filtering can be dealt with by involving application code querying.

Some possible dependencies:
  1. May require easy import or export of large datasets into/from temporary databases or tables or files, and the automatic deletion of such temporary objects after some timeouts.
     - requires taking advantage of large cheap secondary storage space
  3. May have to deal with expectation of SQL table schema, by dealing with issues like
     - automatically identifiying column types (and even names) in result sets of queries
     - creating temporary tables based on schema suggested by column types to store such query results
     - converting SQL query result sets to higher-level objects "on the fly". This is given the fact that ORMs typically require a prespecified schema declaration for such conversions.

## Last Resort ORM strategy for both SQL and NoSQL Databases

*Leverage existing micro-ORM solutions (ie ORM without caching),
store all database information in a data storage format (e.g. XML, JSON, YAML),
use a code generator to generate code artifacts required by the chosen micro-ORM solution,
and generate any custom helping code artifacts purely based on the database information*

[Code augmentor](https://github.com/aaronicsubstances/code-augmentor) library is perfect use case for this kind of code generation, since it allows for breaking updates to be easily detected.

Learn from the following and avoid attempting to implement full-blown ORM solution.
  - https://blog.codinghorror.com/object-relational-mapping-is-the-vietnam-of-computer-science/
  - https://scala-slick.org/doc/3.0.0/orm-to-slick.html
  - https://martinfowler.com/articles/evodb.html

JPA's persistence.xml file is an example of the kind of database information that has to be stored. In contrast, a micro-ORM will definitely not need all that information, will not need to use XML, and will make the file readily available to application code for introspection. Also it should be possible to indicate prescence of a relationship without implying the existence of a database foreign key contraint.

Represent any helping code artifacts to perform a read and write database operation as an "internal stored procedure" which generates something like prepared SQL statements, so that it can be tested
independently of the application employing it.

  1. To make it easier to test variations in generated query code snippets, the query generation part can be made equivalent to a pure function with these characteristics:
     - Contains only sequential statements and non-nested (ie top-level) if-else statements
     - All the boolean conditions for the if statements constitute the beginning code of the function, and are determined only by the procedure parameters.
     - Each if condition clause is trivially exactly one boolean condition
   (e.g "if (condition1)").

Can create helper functions or code generation scripts which access database information stored in XML/JSON/YAML.
Notable examples are
  1. Functions for generating SQL join clauses
  2. Functions for mapping tabular query results (typically from SQL) to list of tuples of database classes from code generation, as seen in https://scala-slick.org/doc/3.0.0/orm-to-slick.html#relationships


Some possible dependencies:
  1. Deserialization logic of read-only query results (e.g. mappers between query results and database classes resulting from code generation) may need to be maintained with production data.
     - SQL makes this considerably easier than NoSQL, because even an empty query result set can sufficiently indicate the columns/fields of the query result.
     - Generally for SQL and NoSQL however, existing production database contents and optionally schema define the schema of database contents. This suggests having a last resort of testing mappers against the production database (e.g. during runtime initialization) with test parameter values specified in deployment configuration per readonly query (so as to make readonly queries return non-empty data by default).
     - Tests can also include verifying "optionality" of query results. For tabular results, "optionality" refers to whether the "one" in a one-sided relationship is exactly one or at
     most one, and whether the "many" in a many-sided relationship is at least one or can be empty.
     For tree results equivalent to a JSON document, "optionality" refers to whether an object can
     be null or not, and whether an array can be empty nor not.
  2. Additional second-stage mapper functions may have to be defined to convert the database classes resulting from code generation, to data transfer objects for use by higher layers of an application.
     - Further impetus for this can from a situation where query result items are always of one generic type  (e.g. list or tuple).
     - This also provides the stage of mapping list to trees (e.g. using groupByAdjacent operator on sorted record list, such as can be found in morelinq .NET library) and vice versa (e.g. using selectMany operator such as can be found in Reactive Extensions libraries).
     - NoSQL makes this considerably easier by nearly eliminating this stage entirely, since query results usually already are trees of high level structures known to the application code.
