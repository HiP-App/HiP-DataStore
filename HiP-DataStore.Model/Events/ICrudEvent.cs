using PaderbornUniversity.SILab.Hip.EventSourcing.Events;
using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    /// <summary>
    /// An event for simple create, update and delete operations.
    /// </summary>
    public interface ICrudEvent : IEvent
    {
        /// <summary>
        /// The type of the created, updated or deleted entity.
        /// </summary>
        ResourceType GetEntityType();

        /// <summary>
        /// The ID of the created, updated or deleted entity.
        /// </summary>
        int Id { get; }
    }

    public interface ICreateEvent : ICrudEvent
    {
        ContentStatus GetStatus();
        DateTimeOffset Timestamp { get; }
    }

    public interface IUpdateEvent : ICrudEvent
    {
        ContentStatus GetStatus();
        DateTimeOffset Timestamp { get; }
    }

    public interface IDeleteEvent : ICrudEvent
    {
    }

    public interface IUpdateFileEvent : ICrudEvent
    {
    }

    public interface IUserActivityEvent : ICrudEvent
    {
        int UserId { get; }
        DateTimeOffset Timestamp { get; }
    }
}
