# About Migration and Versioning
HiP-DataStore uses two kinds of "migrations" to make changes to the data model without loosing existing data.

## Event Migration
A light way of migration that is useful if small modifications to event types need to be made. For example, adding a property to an `ExhibitCreated`-event or changing the type of a property are good candidates for event migrations. Event migrations are applied "on the fly", i.e. whenever an event of an outdated type is read from the Event Store, it is transformed into an event of the new type.

How to implement an event migration:
1. Create a copy of the event type you want to modify, e.g. `FooCreated`, and append a version number to the type's name, e.g. `FooCreated2` (future versions of the event type should be called `FooCreated3`, `FooCreated4` and so on).
1. Make changes to `FooCreated2`, e.g. add/remove properties, change property types etc. If `FooCreated` contained a property of a custom type, such as `FooArgs`, and you need to make changes to `FooArgs`, then you also need to create a copy of that and name it `FooArgs2`.
1. Make `FooCreated` implement `IMigratable<FooCreated2>` so that events of the old type can be converted into events of the new type
1. Adapt `CacheDatabaseManager` to handle the new event type
1. Where necessary, adapt `IDomainIndex`-classes to handle the new event type
1. Mark the old event type (`FooCreated`) as `[Obsolete]` so that you are warned if your code still uses it

## Stream Migration
If the desired model change cannot be achieved with event migration, stream migration must be used. A stream migration basically recreates the whole event stream from scratch by reading through all the existing events and emitting new events based on the old ones. For example, if you have a type of event which you want to split into two events, a stream migration is a good way to do that. In contrast to event migrations, a stream migration is only applied once on application startup.

A positive side-effect of stream migrations is that they persist all the event migrations. So after a stream migration has been applied, all obsolete event types up to this point in time can be removed, since the stream no longer contains events of obsolete types (but remember to keep the event types' names such as `FooCreated2` when deleting `FooCreated`).