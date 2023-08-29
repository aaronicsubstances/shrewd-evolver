# Shrewd Evolver

Contains documents intended to record insights and opinions on software evolution and architecture.

Sometimes the opinions take the form of source code files implementing a proposal.


## My Last Resort for Dealing with Fragile Software Architectures

  1. Code generation
  2. https://github.com/aaronicsubstances/code-augmentor


## My Ideal Software Architecture

My ideal software architecture (which comprises code and data architectures)  is one that meets the following criteria (in addition to all the excellent advice on [Wikipedia](https://en.wikipedia.org/wiki/Software_architecture)):

  1. It has been designed with the necessary abstractions to ensure that its implementation can be changed incrementally, such that stakeholders and the public do not have to make radical changes to cope with any implementation changes.
  2. It can be explicitly recorded into the implementation, such that engineers can discover the architecture on their own by reading the implementation.
  3. It implements measures to make easier the transition to a distributed system if necessary.


## My Proposals for Designing Flexible Software Architectures

About abstractions that enables programmers to make incremental changes within the constraints of the laws of conservation of familiarity and organisational stability:
  1. build data architecture on the property-graph model (near equivalent of entity-relational model).
     1. This approach seeks to leverage the success story of SQL databases, whose flexibility come from multisets and the entity-relationship model.
  2. build code architecture on the assumption that all processing occurs similar to how Apache/PHP and AWS Lambda functions process HTTP requests: a single process/thread is created to handle an incoming HTTP request representing the input of I/O, and the output of I/O will be contained in the corresponding HTTP response.
     1. This approach seeks to leverage the fact that all I/O can be converted into network requests, ie from PCI Express to the Internet.
     3. Another takeaway is that if an architecture limits its use of memory to local variables, serializable memory, scheduled timeouts and I/O callbacks, then it will result in codebases which are structured in similar ways.
  3. present software to stakeholders and public as a single abstraction, regardless of the perspective programmers have of the software from its architecture.
     1. separate concerns of number of modules (aka microservices) from number of deployments, by deploying with single OS process (possibly supervisor process over child processes) for as long as possible.
     3. separate concerns of number of modules from number of databases, by managing data with ACID transactions inside single homogenous database for as long as possible.

About explicitly recording an architecture in its implementation:
  1. enforce code architecture by implementing modular boundaries with serializable, HTTP-like communication protocols. See https://github.com/aaronicsubstances/cskabomu for details.
  4. encode database schema and entity-relational or property-graph model into data storage in such a way that it can be extracted by database reverse-engineering tools.
     1. This is especially important for NoSQL databases which are usually without a database schema.
     2. For simple SQL designs, the database schema may approximate the entity-relational model.

About measures which make transition to distributed systems easier:
  1. deploy the code as two nearly-identical processes (or process groups).
  1. manage the data as two nearly-identical horizontal data partitions, but such that local ACID transactions can be conducted across them.
