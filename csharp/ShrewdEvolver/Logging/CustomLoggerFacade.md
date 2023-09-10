# CustomLoggerFacade

This module comprises the classes in the *logging* subdirectories of the language implementation directories. They are meant for use during library creation, to abstract away any logging library which will eventually be used by library consumers.

At a point during application start up, **CustomLoggerFacade** has to be supplied with a function that creates **CustomLogger** instances. 

Inside library, call CustomLoggerFacade.getLogger() and supply calling class name or class/type instance. And then invoke the methods of the CustomLogger returned. By default the logger returned from CustomLoggerFacade does nothing.
