# Data Modelling

## Entity-Relational Diagramming/Property-Graph Modelling

Develop best practices for working with ERD or property-graph model that supports abstraction of things like
  - ACID transactions - this will work best with database technologies in which strings can be used to do everything from DDL
  to DML, ie like SQL. In that case a transaction orchestrator can be in a separate process from its clients, and create and store transaction handles,
  and lease them out by GUID reference.
  - SQL joins
  - adding/deleting relationships
  - loading, adding, updating or deleting single row using primary key (simple or composite)
  - names of tables, table columns, views, functions, stored procedures and query result columns used in SQL builders (probably not required for dynamically typed languages, and requires code generation)

Can make use of SQL scripts for almost static SQL (notably this works for NoSQL too)
  - for static sql, use strategy of storing SQL in file scripts, which can be tested independently of the program against the database
 by a quality assurance tool
  - for dynamic sql with a small number of possible variations (e.g. max of 10), store common SQL in file script and define the variations in terms
 of diff patches to the common script. Then let independent QA tool test each variation against database.
  - can use code generation to generate result set row class, or tuple of classes, by annotating result columns and their data types in the SQL scripts.

Can make use of libraries for SQL builders available in every major web programming language (notably this works for NoSQL too)
  - especially for very dynamic SQL, that is, SQL statements with a large number of variations (e.g. more than 20).
  - only when SQL statement requires native features unsupported by particular SQL builder library and is very dynamic, do we have to generate SQL without builders and also forego the ability to independently test.
  
Can encode entity-relational diagram into data storage.
  - That is, have names and properties which communicate the kind of an entity or relationship, which communicate whether a node in a property-graph model is an entity or a relationship, and which communicate the kind of the target entity of a relationship.
  - Could even build a ERD discoverer and integrity checker tool which if fed with nodes from scanning a database, can determine ERD, and even validate the existence of the lone target of one-sided relationships, and even validate property types as well. 

Learn from the following before attempting to implement ORM-like features (like abstracting names of SQL table columns)
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
  2. Applications and business rules
     - last resort for consistency.
  1. Try-Confirm-Cancel (aka Try, Commit, and Cancel model)
     - achieves isolation without forcing sequential processing.
  2. Log-based message broker
     - last resort for isolation, in which messages are processed one at a time, effectively forcing sequential processing, and hence achieving isolation.
     - last resort for atomicity by endless retrying

## Model-Independent Query Strategy

Goal is to have a last resort for traversing relationships between entities (e.g. performing SQL joins) regardless of the data model. And that is by saving dataset in SQL tables, querying them in SQL, and repeating the saving and querying until the desired results are obtained.

  1. Assume use of SQLite, or temporary table mechanism supported by mainstream RDBMSes for model independent query support.
  2. Make it easy to import large datasets into temporary SQLite databases or temporary tables, and manage automatic deletion of such objects after some timeouts.
  3. May have to deal with expectation of schema by RDBMSes, by dealing with issues like
     - automatically identifiying column types (and even names) in result sets of queries
     - creating temporary tables based on schema suggested by column types to store such query results
     - loading data from such temporary tables into typed record list
     - transforming data in such temporary tables and mapping the results into typed record list as query results or as data to store in main RDBMS tables.

### Implementation

  1. Delete temporary tables and files after some time
     - this need is the same as the need to evict append-only log events in memory or disk after processing by log-based message brokers.
  2. Take advantage of views, materialized views, and as a last resort, insert into ... select
     - in sql, we can create regular or temporary tables directly from query results
     - in sqlite, we can leverage ephemeral file storage and dynamically-typed columns to have a template database file with precreated tables, which can be cloned and populated with query results.
  3. Implement function for converting between trees and record list so as to extend model-independent querying to non-RDBMSes.
     - split into mapping record list into tuples of classes and using selectMany and groupByAdjacent operators (available in libraries such as Reactive Extensions, morelinq) to map tuples of classes to trees
     - main job then is about mapping sql table/result set to list/tuple of field sets. That a list of field collection, where each collection doesn't duplicate field names, but a name may be present in two or more collections. 
     - So given a row like {name:A, value:1}, {name:B, value:2}, {name:A, value: 0}, and a list of field sets like class C1 {A,B}, class C2 {A}, the row should be mapped to something like new C1(A=1,B=2), new C2(A=0).
  4. Implement function for loading targets of many to many relationships, given id, for non-RDBMSes.
     - implement efficiently in document db, sql or graph db using knowledge of all distinct ordered ids whose targets are to be loaded. By having many to many table sorted by source ids, fetch target ids, and use WHERE id in (target ids)
