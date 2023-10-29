General Notes
-------------
- Code improvements (esp. database access) is far likely the solution to efficiency problems than vertical scaling, which in turn is far likely the solution than horizontal scaling.
- All concerns of efficiency should be investigated by profiler on production deployment, in order to avoid wasting time with guess work and premature optimizations.
- Optimized code can be made more maintainable by keeping the unoptimized code around, and having a
  random boolean switch which almost always (but not always) turns in favour of the optimized code.

I/O Tasks
---------
- Database caching - applied to frequently used horizontal partititions. Sort of in-memory document database.
- Helps to leverage denormalization in data modelling to make data caching easier. For SQL this means something like maintaining replica copies of source table columns in other tables' columns, and using log-based message broker (or precursor) to maintain consistency across replicas.
- "Conditional acid" as equivalent to eventual consistency - ie different people can see different views (ie different read consistencies) of the data depending on destination and time of writes, but writes which cannot be lost, must be acidly stored. For nosql this means database durability and use of log-based message broker (or precursor) to achieve eventual consistency.
- Atomic file renaming for support file rollover and deletion (used by databases behind the scenes), to support some denormalization and leverage fact that the more data in a table, the more expensive full scans become.
- Support crashing the system if a "heterogenous commit" (ie via log-based message broker or precursor) fails after writing to some nodes, and support completing all pending heterogenous commits at application startup (similar to RDBMS startup recovery routine).

Compute Tasks
--------------
- Log-linear algorithms in worse case through sorting, better still and usually linear through one-time scanning, best and ideally logarithmic through binary search
- Non-blocking synchronization can introduce significant efficiency gains for compute tasks
- Atomic reads and writes of immutable values
