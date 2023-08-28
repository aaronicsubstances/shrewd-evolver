# Data Modelling

## Database-Independent Versatile Model

Property-graph model

## Last Resort Options for achieving ACID

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

## Last Resort for Querying Regardless of Database or Model

Break querying into stages, consider involving application code, and consider involving saving to temporary objects of a database for repeat querying.
  - An example is to save dataset from a data source into embedded SQL databases (such as SQLite and DuckDB) or temporary SQL tables of popular RDBMses, query them in SQL, save the result set to SQL tables, and repeat the querying and saving until the desired results are obtained.

More on model-independent querying: 
  1. This approach can serve as a last resort for traversing relationships
  1. Dynamically-generated SQL can be tested with permutations and combinations,
 as long as variations are kept below a limit, say 7 permutations. Then further variations can be dealt with by involving application code
  1. querying can be seen as limited to "partial stored procedures" - partly database, partly application
  2. application-code side of querying resembles MapReduce, ie is based on map, filter, reduce, sort, union, equi-join, etc
  1. May require easy import or export of large datasets into/from temporary databases or tables, and the automatic deletion of such temporary objects after some timeouts.
  2. May have to deal with expectation of SQL table schema, by dealing with issues like
     - automatically identifiying column types (and even names) in result sets of queries
     - creating temporary tables based on schema suggested by column types to store such query results
     - converting result sets into typed record list

## Minimal ORM Requirements for both SQL and NoSQL Databases

  1. Institute custom code generator for abstracting names and types of SQL table columns, and names of other database objects (i.e. tables, sequences, stored procedures, etc).
  1. Implement function for converting between trees and record list
     - split function into (1) mapping record list into tuples of classes and (2) map tuples of classes to trees (e.g. using groupByAdjacent operator on sorted record list, such as can be found in morelinq library) and vice versa (e.g. using selectMany operator such as can be found in Reactive Extensions).
     - main job then is about mapping result set to list of tuples of "field/column sets", where each column set doesn't duplicate field names, but a name may be present in two or more column sets. 
     - So given a row like {name:A, value:1}, {name:B, value:2}, {name:A, value: 0}, and a list of column sets like class C1 {A,B}, class C2 {A}, the row should be mapped to something like C1(A=1,B=2), C2(A=0).
     - an application-defined mapper function can then convert the database classes used in the tuples, to classes with property names which are more convenient to higher layers  of an application.
  2. Implement function for loading targets of many to many relationships, given id
     - implement efficiently in document db, sql or graph db using knowledge of all distinct ordered ids whose targets are to be loaded. By having many to many table sorted by source ids, fetch target ids, and use WHERE id in (target ids)
  3. Mappers need to be maintained with production data
     - store redacted interim and final query results (ie all inputs to mappers) in JSON (ie tree format)
     - redacted means we only occur to know about fields and their types: a union (in Typescript sense) of the JSON types. For arrays, union will be done to make the arrays all have the same item type. Or can simply make use of JSON schema definitions.
     - once this data is made available to mapper tests, all they have to check for is whether their fields of interest are found, and are of the expected types.
     - SQL makes this considerably easier than the NoSQL solutions due to its schema, but in general it will production data too (unless an empty query result set can sufficiently indicate the possible types for all its columns).

Learn from the following before attempting to implement advanced ORM features, such as abstracting names of SQL table columns, and fetching target of relationships.
  - https://blog.codinghorror.com/object-relational-mapping-is-the-vietnam-of-computer-science/
  - https://scala-slick.org/doc/3.0.0/orm-to-slick.html
