# Data Modelling

## Last Resort for conceptualizing Data Model regardless of Database Vendor

*Use the property-graph model*.

"Property-graph model" as used here refers to the near equivalent of entity-relational model in which both nodes and edges are organised into either bags/multisets in the case of SQL, or objects/associative arrays in the case of NoSQL.
  1. have a way to identify the type of each node
  1. have a way to identify the type of each of the targets of each edge.
  1. abstract all relationships as potentially many-to-many to the public, i.e. hide one-sided relationship implementation details from the public.
     - For SQL, in which one to many relationship is implemented without a join table, the table containing the foreign key can be viewed as playing an additional role of join table, in which any new properties of the relationship can be added as columns in that table.
     - When fetching the single end of one to many relationship, just fetch first row in result set, and skip validating that there is only a single row in the result set. With this, expanding to a many to many relationship will cause no or few runtime errors while giving developers time to make updates.
  3. differentiate between the means of distinguishing entities for the purpose of establishing relationships in the property-graph model, from all other criteria for distinguishing entities, including criteria known to the public
     - E.g. always use internally-generated ids to identify entities for all programming purposes, including for forming relationships and for presenting to the public.

## Last Resort Options for achieving ACID transactions

"ACID transactions" refers to database transactions ideally characterized by atomicity, consistency, isolation and durability.

  1. "ACID databases", i.e. databases which promise full or partial support for ACID transactions.
     - last resort for achieving durability.
     - best at achieving atomicty by abortability.
     - best at achieving snapshot isolation for readonly queries.
     - best at preventing access to results from uncommitted transactions.
     - insufficient and inflexible in achieving consistency by themselves.
  2. Applications and business rules
     - last resort for consistency.
  1. Try-Confirm-Cancel, mentioned by Pat Helland in 2007.
     - achieves isolation without forcing sequential processing.
  2. Log-based message broker
     - last resort for isolation
     - last resort for atomicity by endless retrying

## Last Resort for Querying regardless of Database or Model

*Break querying into stages, consider involving application code to query the SQL/NoSQL query results, and consider involving "repeat querying".*

  - "Repeat querying" here means: save either query result set or dataset from a data source, into embedded SQL databases (such as SQLite and DuckDB) or temporary SQL tables of popular RDBMses, query them in SQL, consider involving application code to query the SQL query results, save the result set to SQL/NoSQL storage, and repeat the querying and saving until the desired results are obtained.
  - The application-code side of querying should resemble MapReduce, ie is based on reduce (e.g. map, filter, selectMany, groupByAdjacent, application of window functions), sort, 
  merge-join (e.g. union, intersection, except, equi-join), and functions common to most databases, spreadsheet applications and programming languages (e.g. LIKE, uppercase, arithmetic).

This approach is based on the assumption that SQL is better than NoSQL for generating reports and queries that were not anticipated by the original database creators.
This approach provides other related benefits:
  - Can serve as a last resort for traversing relationships in any database model, since traversing relationships is arguably equivalent to performing merge-joins.
  - Provides one solution to maintaining many-to-many relationships in document databases or hierarchical/tree data models, since traversing many-to-many relationships in relational data models is about performing multiple merge-join operations.
  - Provides one solution to the problem of having too many filtering conditions in an SQL query, such that the SQL execution engine is forced to do table scans and dispense with indices. And that solution is to apply few filtering conditions which are most likely to exclude the highest possible
  number of non-matching rows. Then further filtering can be dealt with by involving application code querying.

Some possible dependencies:
  1. May require easy import or export of large datasets into/from temporary databases or tables or files, and the automatic deletion of such temporary objects after some timeouts.
     - requires taking advantage of large cheap secondary storage space
  3. May have to deal with expectation of SQL table schema, by dealing with issues like
     - automatically identifiying column types (and even names) in result sets of queries
     - creating temporary tables based on schema suggested by column types to store such query results
     - converting SQL query result sets to data transfer objects "on the fly". This is given the fact that ORMs typically require a prespecified schema declaration for such conversions.

## Last Resort ORM strategy for both SQL and NoSQL Databases

*Prefer ORM solutions with support for cache busting to those which lack such support. Then if needed, augment with an in-house micro-ORM for running both canned queries and native queries, that stores mapping information between types and names of database objects and application objects  in serializable storage format (e.g. JSON, YAML).*

Learn from the following and avoid attempting to create fully-featured ORM solution.
  - https://blog.codinghorror.com/object-relational-mapping-is-the-vietnam-of-computer-science/
  - https://scala-slick.org/doc/3.0.0/orm-to-slick.html
  - https://martinfowler.com/articles/evodb.html
  - https://github.com/aaberg/sql2o/blob/master/core/src/main/java/org/sql2o/quirks/Quirks.java

Leverage code generation if needed.

Replace all dynamic construction of queries in application code with canned queries (whether native or not). *Canned statements refer to queries that can be tested independently of the application code employing them, and hence independently of runtime of production environment.*

A custom micro-ORM should support the following:

  1. Mapping query results to tuple of database types, indexed arrays of database types, associative arrays of database types, or plain old objects whose properties are all of database types.
  1. Getting column names of SQL query results.
  1. Converting application objects to database query parameters or query results of types expected by a given database driver.
     - This usually depends on identifying the appropriate database driver function to call, and additional custom type converter functions in cases where database driver is falling behind.


Some ideas for mapping column names in SQL query results to tuple of plain old objects or associative arrays are:
  - just use the database column names as names in the application objects, and let column results with the same column name overwrite each other. It is then the responsiblity of the developer to ensure uniqueness of query result column names. This seems to be the approach favoured by dynamically typed languages, and should be supported by any custom micro-ORM at a minimum.
  - another approach is needed when it is desirable to be able to separate database object names from other parts of application code with the use of mappers, and also be able to rename database objects and have it automatically reflect in SQL queries. This is needed especially in statically typed languages. And that approach is to have a special syntactical convention used within SQL queries, in which column names of query results and tables (and other database objects) are specified as a pair of class name (or index in tuple) and property name (or key in associative array). Syntax should differentiate between column names of query results and the rest. At build time, a linting program can be used to validate the syntactical constructs. And then at build time or runtime, a preprocessor can be used to replace the syntactical constructs with auto-generated column names.

