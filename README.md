# Shrewd Evolver

Contains documents intended to record insights and opinions on software evolution and architecture.

Sometimes the opinions take the form of source code files implementing a proposal.


## My Last Resort for Dealing with Fragile Software Architectures

  1. Code generation
  2. [Code Augmentor](https://github.com/aaronicsubstances/code-augmentor)


## My Ideal Software Architecture

My ideal software architecture (which comprises code and data architectures)  is one that meets the following criteria (in addition to all the excellent advice on [Wikipedia](https://en.wikipedia.org/wiki/Software_architecture)):

  1. It has been designed with the necessary abstractions to ensure that its implementation can be changed incrementally, such that stakeholders and the public do not have to make radical changes to cope with any implementation changes.
  2. It can be explicitly recorded into the implementation, such that engineers can discover the architecture on their own by reading the implementation.
  3. It implements measures to make easier the transition to a distributed system if necessary.


## My Proposals for Designing Flexible Software Architectures

About abstractions that enables programmers to make incremental changes:
  1. build data architecture on the entity-relational diagram (ERD). This is equivalent to building on property-graph model, provided distinction can be made between one-sided relationships which cannot have properties, and all other relationships.
     1. abstract all relationships as "zero-to-many", i.e. hide one-sided relationship implementation details from the public.
     2. differentiate between the means of distinguishing entities for the purpose of establishing relationships in the property-graph model, from all other criteria for distinguishing entities, including criteria known to the public, i.e. always  use internally-generated ids to identify entities for all programming purposes, including for forming relationships and for presenting to the public.
  2. build code architecture on the assumption that all processing occurs similar to how Apache/PHP processes HTTP requests: a single process/thread is created to handle an incoming HTTP request representing the input of I/O, and the output of I/O will be contained in the corresponding HTTP response.
     1. This model is very flexible because barring efficiency concerns, all I/O can be converted into network requests.
     3. Another takeaway here is that if an architecture limits its use of memory to local variables, serializable memory, scheduled timeouts and I/O callbacks, then it will likely result in code which is simpler to understand.
  3. present software to stakeholders and public as a single abstraction, regardless of the perspective programmers have of the software from its architecture.
     1. separate concerns of number of modules (aka microservices) from number of deployments, by deploying with single OS process (possibly supervisor process over child processes) for as long as possible.
     3. separate concerns of number of modules from number of databases, by managing data with ACID transactions inside single homogenous database for as long as possible.

About explicitly recording an architecture in its implementation:
  1. enforce code architecture by abstracting modular boundaries with serializable, HTTP-like communication protocols. See [Kabomu](https://github.com/aaronicsubstances/cskabomu) and https://rfc.zeromq.org/spec/33/.
  4. encode entity-relational diagram into data storage. That is, have names and properties which communicate the kind of an entity or relationship, which communicate whether a node in a property-graph model is an entity or a relationship, and which communicate the kind of the target entity of a relationship.

About measures which make transition to distributed systems easier:
  1. deploy the code as two nearly-identical process groups.
  1. manage the data as two nearly-identical horizontal data partitions.
