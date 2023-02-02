1. What happens with the passage of time after first release: e.g. as requests per second from outside world (humans and systems),  data volumes, bug fix requests and feature requests from stakeholders increase without bound?
2. integration testing,
3. beta testing,
4. regression testing
5. tradeoff between callback and polling
6. difficulty in changing architecture (code and data),
7. difficulty in radically changing interface to outside world (GUI and APIs),
8. code generation with output modification
9. modular programming with enforcement of commuication boundaries
10. generalized docker/unix init, generalized OS shell jobs
11. data modelling,
12. try-test-cancel, as alternative to distributed transactions
13. precursor to mapreduce, requerying query results
14. precursor to log-based message brokers, cron jobs, job queues
15. precursor to redis, in memory db like SQLite in-memory
16. ORMs,
17. dependency of maintenance on ease of software life cycle steps after coding, i.e. testing, integration, deployment, monitoring, debugging.
18. Need for deployed software to be capable of being replicated on development machines, for ease of debugging 
19. need of software to satisfy future stakeholders, esp maintainers
20. importance of human and time factors, seeking and streaming, and distributed systems in scaling data,
21. tradeoff between code optimisation and readability,
22. need to apply security fixes to deployed technologies,
23. need to deal with changes to deployed technologies (e.g. OS versions, library versions),
24. changes in the technologies in vogue. Discipline required not to change old code that works.
25. property graph model and approach of viewing relationship primarily as a graph navigation tool rather than a consistency constraint, ie abstract over foreign keys and hide them from end users, and make them nullable, ignore delete/update cascade. Also do not create a relationship until it is necessary, and even then prefer double many sided to one sided.
26. Value of unravelling entity relational diagram. Can have a reverse engineering tool which can report the ERD when run against the database. Can av a convention of annotating tables/nodes/documents as entities or relationships (aka join tables). And have a special way to indicate foreign keys of one-sided relationships (like "fk_" prefix). And have a db scan function. Then a generic tool can be built or purchased, which can even fetch field types and ranges. NB: omit nested documents.
27. value of escalating business rule decisions (e.g. uniqueness, immutability, cardinality, minimum, maximum, etc) to external world usage (e.g. GUI);
28. beware of excessive and unwarranted validation which prohibits manual interventions, or which prohibits temporary suspension of business rules during manual intervention. Can create a validation tool (aka data integrity check, db crash recovery) against the database which occasionally runs in production, and is internal/external to app. E.g. generates a report, and can be redacted and sent back to dev. Such a tool checks integrity of entity relationship diagram, esp for enforcing prescence in one side relationships, and verifying data types and ranges of entity and relationship fields.
29. Need for data security (including possibility of barring access to programmer or DBA);
30. record input requests from external world, and record database changes with CDC (take advantage of cheap secondary storage), to counteract the cost of incomplete data architecture (what about security concerns of me not seeing the data? Redaction needed?)
31. Never assume any data item received from or sent to external world cannot change, ie prepare for situation where programner was wrong about whether data items have identities, and what uniquely identifies it (e.g. only use autoinc ints as ids internally, and uuids as ids externally, and assume every other data item may be abandoned);
32. identify time context of every data item,
33. assume that the only way to change data schema is to add new tables and columns, copy over, and to stop using certain tables and columns. Rather than update or delete.
34. Discipline of not changing code and data architecture simultaneously.
35. Need for a code architecture which can be run as a distributed system. Can limit architectural choices in order to simplify design. Can provide unexpected benefits as well.
36. Need for rolling upgrade, by having a data architecture which can be horizontally partitioned to support a distributed system. Since data is the part that stakeholders can readily observe, then the takeaway here is horizontal partitionability of data, to reduce cost of data architectural changes. Can support approach of add new code to old code just as new schema is added to old schema via different sets of tables or dbs, migrate horizontal partitions to new code and schema, delete old code and schema
37. Need for tool which can copy a graph of data from one horizontal partition to the other.


