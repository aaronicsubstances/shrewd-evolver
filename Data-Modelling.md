# Data Modelling

## Last Resort for conceptualizing Data Model regardless of Database Vendor

*Use the property-graph model*.

"Property-graph model" as used here refers to the near equivalent of entity-relational model in which both nodes and edges are organised into either bags/multisets in the case of SQL, or objects (map of key-value pairs) in the case of NoSQL. Also in this model, distinction exists between nodes and edges/relationships, and distinction exists between one-sided relationships which cannot have properties, and all other kind of relationships.
  1. have a way to identify the type of each node, and each edge (ie either one-sided relationship which cannot have properties, or not). And in the case of edges, also have a way to identify the type of the targets of the relationship.
  1. abstract all relationships as potentially many-to-many to the public, i.e. hide one-sided relationship implementation details from the public.
  2. differentiate between the means of distinguishing entities for the purpose of establishing relationships in the property-graph model, from all other criteria for distinguishing entities, including criteria known to the public
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

This approach is based on the assumption that SQL is better than NoSQL for generating reports and queries that was not anticipated by the original database creators.
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

*Leverage existing micro-ORM solutions (ie ORM without caching),
store all database information in a data storage format (e.g. XML, JSON, YAML),
use a code generator to generate code artifacts required by the chosen micro-ORM solution,
and generate any custom helping code artifacts purely based on the database information*

JPA's persistence.xml file is an example of the kind of database information that has to be stored. In contrast, a micro-ORM will definitely not need all that information, will not need to use XML, and will make the file readily available to application code for introspection. Also it should be possible to indicate prescence of a relationship without implying the existence of a database foreign key contraint.

Can create helper functions or code generation scripts which access database information stored in XML/JSON/YAML.
Notable examples are
  1. Functions which dynamically select a prepared SQL statement to execute from a specific subset of canned SQL statements, depending on the parameters.
  2. Functions for mapping SQL query results to list of tuples of database classes from code generation, as seen in https://scala-slick.org/doc/3.0.0/orm-to-slick.html#relationships (this is meant to precede conversion to data transfer objects).

Replace dynamic construction of SQL in application code with dynamic selection from a list of canned SQL statements. The canned statements can then be tested independently of the application code employing them. This seeks to leverage the fact that increasing variation in SQL snippets (typically with variation in WHERE clauses) decrease opportunities for optimizations to leverage indices. And so generating all possible canned SQL statements (likely with the help of a code generator) is feasible.

Learn from the following and avoid attempting to implement full-blown ORM solution.
  - https://blog.codinghorror.com/object-relational-mapping-is-the-vietnam-of-computer-science/
  - https://scala-slick.org/doc/3.0.0/orm-to-slick.html
  - https://martinfowler.com/articles/evodb.html
