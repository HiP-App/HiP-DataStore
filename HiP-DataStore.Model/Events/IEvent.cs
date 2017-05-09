using System;

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
        /// The type of the created, updated or deleted entity.
        /// </summary>
        Type EntityType { get; }

        /// <summary>
        /// The ID of the created, updated or deleted entity.
        /// </summary>
        int Id { get; }
    }

    public interface ICreateEvent : ICrudEvent
    {
        ContentStatus Status { get; }
    }

    public interface IUpdateEvent : ICrudEvent
    {
        ContentStatus Status { get; }
    }

    public interface IDeleteEvent : ICrudEvent
    {
    }
}
