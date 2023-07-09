# Data Modelling

## Entity-Relational Diagramming/Property-Graph Modelling

Learn from the following before attempting to implement ORM-like features, especially abstracting names of SQL table columns, and fetching target of relationships.
  - https://blog.codinghorror.com/object-relational-mapping-is-the-vietnam-of-computer-science/
  - https://scala-slick.org/doc/3.0.0/orm-to-slick.html
  - https://rdo.devzest.com
  - https://www.datanucleus.org/products/accessplatform_4_2/jdo/metadata_api.html
  - https://www.datanucleus.org/products/accessplatform_4_2/jdo/metadata_xml.html
  - https://openjpa.apache.org/builds/1.2.3/apache-openjpa/docs/jpa_langref.html
  - https://docs.oracle.com/javaee/7/api/javax/persistence/package-summary.html
  - https://docs.oracle.com/javaee/7/api/javax/persistence/metamodel/package-summary.html
  - https://docs.oracle.com/javaee/7/api/javax/persistence/criteria/package-summary.html
  - https://docs.spring.io/spring-framework/docs/current/javadoc-api/org/springframework/jdbc/core/JdbcTemplate.html
  - https://github.com/querydsl/querydsl (looks like JPA Criteria API makes it easier than JDOQL API to throw in native SQL)


## Last Resort Options for achieving ACID

  1. "ACID" databases
     - best at achieving durability.
     - best at achieving atomicty by abortability.
     - best at achieving snapshot isolation for readonly queries.
     - best at preventing access to results from uncommitted transactions.
     - insufficient and inflexible in achieving consistency.
  2. Applications and business rules
     - last resort for consistency.
  1. Try-Confirm-Cancel (aka Try, Commit, and Cancel model), first mentioned by Pat Helland in 2007.
     - achieves isolation without forcing sequential processing.
  2. Log-based message broker
     - last resort for isolation, in which messages are processed one at a time, effectively forcing sequential processing, and hence achieving isolation.
     - last resort for atomicity by endless retrying

## Model-Independent Querying

Goal is to have a last resort for traversing relationships between entities (e.g. performing SQL joins) regardless of the data model. Thus this can be seen as precursor to distributed filesystems and large-scale data processing engines.

My proposal for achieving this goal is to save dataset from any data source in SQL tables, query them in SQL, and repeat the saving and querying until the desired results are obtained.

  1. Assume use of SQLite, or temporary table mechanism supported by mainstream RDBMSes for model independent query support.
  2. Make it easy to import large datasets into temporary SQLite databases or temporary tables, and manage automatic deletion of such objects after some timeouts.
  3. May have to deal with expectation of schema by RDBMSes, by dealing with issues like
     - automatically identifiying column types (and even names) in result sets of queries
     - creating temporary tables based on schema suggested by column types to store such query results
     - loading data from such temporary tables into typed record list
     - transforming data in such temporary tables and mapping the results into typed record list as query results or as data to store in main RDBMS tables.
4. Helps to have data records have human/organisation identifiers and timestamp information, as this can be used to limit requerying to some kind of latest records.

### Implementation

  1. Delete temporary tables and files after some time
     - this need is the same as the need to evict append-only log events in memory or disk after processing by log-based message brokers.
  2. Take advantage of views, materialized views, and as a last resort, insert into ... select
     - in sql, we can create regular or temporary tables directly from query results
     - in sqlite, we can leverage ephemeral file storage and dynamically-typed columns to have a template database file with precreated tables, which can be cloned and populated with query results.
  3. Implement function for converting between trees and record list so as to extend model-independent querying to non-RDBMSes.
     - split function into (1) mapping record list into tuples of classes and (2) map tuples of classes to trees (e.g. using groupByAdjacent operator in morelinq library) and vice versa (e.g. using selectMany operator in Reactive Extensions).
     - main job then is about mapping sql table/result set to list/tuple of field sets. That a list of field collection, where each collection doesn't duplicate field names, but a name may be present in two or more collections. 
     - So given a row like {name:A, value:1}, {name:B, value:2}, {name:A, value: 0}, and a list of field sets like class C1 {A,B}, class C2 {A}, the row should be mapped to something like C1(A=1,B=2), C2(A=0).
     - an ORM-like mapper function can then convert the database classes used in the tuples, to classes with property names which are more convenient to higher layers  of an application.
  4. Implement function for loading targets of many to many relationships, given id, for non-RDBMSes.
     - implement efficiently in document db, sql or graph db using knowledge of all distinct ordered ids whose targets are to be loaded. By having many to many table sorted by source ids, fetch target ids, and use WHERE id in (target ids)
