using System;
using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    /// <summary>
    /// Marker interface for events.
    /// </summary>
    public interface IEvent
    {
    }

    /// <summary>
    /// An event for simple create, update and delete operations.
    /// </summary>
    public interface ICrudEvent : IEvent
    {
        /// <summary>
        /// Gets the type of the created, updated or deleted entity.
        /// </summary>
        ResourceType GetEntityType();

        /// <summary>
        /// The ID of the created, updated or deleted entity.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// The date and time when the entity was created, updated or deleted.
        /// </summary>
        DateTimeOffset Timestamp { get; }
    }

    public interface ICreateEvent : ICrudEvent
    {
        ContentStatus GetStatus();

        /// <summary>
        /// Gets a list of entities that are referenced by the created entity.
        /// </summary>
        IEnumerable<EntityId> GetReferences();
    }

    public interface IUpdateEvent : ICrudEvent
    {
        ContentStatus GetStatus();

        /// <summary>
        /// Gets a list of entities that are referenced by the updated entity.
        /// </summary>
        IEnumerable<EntityId> GetReferences();
    }

    public interface IDeleteEvent : ICrudEvent
    {
        new DateTimeOffset Timestamp { get; set; } // setter required for a migration
    }

    public interface IUpdateFileEvent : ICrudEvent
    {
    }

    public interface IUserActivityEvent : ICrudEvent
    {
        string UserId { get; }
    }
}
