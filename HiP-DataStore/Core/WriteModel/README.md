# Write Model (the C in CQRS)
This README documents design decisions that have been made during implementation.

## Validation
There are basically two kinds of validation that should happen server-side when issuing requests (think: "commands") against a controller class:
- Superficial validation (e.g. check whether a value is provided for required fields, check if string length constraints are met, ...): This can be checked  easily using `ModelState.IsValid` since it is independent from the domain state
- Domain Validation: This is more difficult as it involves checks against the current domain state (which is usually unknown due to the nature of Event Sourcing). There are multiple solutions as presented below.

Consider the following validation requirement:
"When creating a new exhibit, the referenced image must exist and its state must be 'published'"

How can this requirement be met?

1. query the read model (i.e. Mongo database) for the image and check the status
   - bad idea, since the read model might not be up-to-date (keyword: eventual consistency)

1. play through the whole event stream (especially events like "image created", "image deleted") to find out whether the image currently exists and has the correct state
   - might have a negative performance impact (required time grows linear to the number of stored events)

1. design a sophisticated domain model with aggregates etc. that stores a representation of the current domain state in memory, then query this model to check requirements
   - certainly overkill, lots of code duplication between read model and domain model, and the question remains: which data can/should be held in memory?

1. create light-weight "indices" that hold only the minimum domain state in memory that is required to validate requests/commands. When events are created and sent to the event store, immediately send them to the indices so they can synchronously update their state
   - not too difficult to implement, not too much overlapping with the read model.
   - THIS IS THE SOLUTION THAT IS IMPLEMENTED
   - idea from [http://stackoverflow.com/a/34449639](http://stackoverflow.com/a/34449639)
   - to implement an index, create a class implementing `IDomainIndex` and register it as a singleton in `Startup.cs`