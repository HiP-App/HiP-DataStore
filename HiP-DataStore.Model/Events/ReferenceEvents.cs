using PaderbornUniversity.SILab.Hip.EventSourcing;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public abstract class ReferenceEventBase : IEvent
    {
        /// <summary>
        /// Collection where the referencing entity is in.
        /// </summary>
        public ResourceType SourceType { get; set; }

        /// <summary>
        /// ID of the referencing entity.
        /// </summary>
        public int SourceId { get; set; }

        /// <summary>
        /// Collection where the referenced entity is in.
        /// </summary>
        public ResourceType TargetType { get; set; }

        /// <summary>
        /// ID of the referenced entity.
        /// </summary>
        public int TargetId { get; set; }

        public ReferenceEventBase()
        {
        }

        public ReferenceEventBase(ResourceType sourceCollectionName, int sourceId, ResourceType targetCollectionName, int targetId)
        {
            SourceType = sourceCollectionName;
            SourceId = sourceId;
            TargetType = targetCollectionName;
            TargetId = targetId;
        }
    }

    /// <summary>
    /// The event that is raised when a reference from one entity to another entity is established.
    /// </summary>
    public class ReferenceAdded : ReferenceEventBase
    {
        public ReferenceAdded()
        {
        }

        public ReferenceAdded(ResourceType sourceCollectionName, int sourceId, ResourceType targetCollectionName, int targetId)
            : base(sourceCollectionName, sourceId, targetCollectionName, targetId)
        {
        }
    }

    /// <summary>
    /// The event that is raised when a reference from one entity to another entity is cleared.
    /// </summary>
    public class ReferenceRemoved : ReferenceEventBase
    {
        public ReferenceRemoved()
        {
        }

        public ReferenceRemoved(ResourceType sourceCollectionName, int sourceId, ResourceType targetCollectionName, int targetId)
            : base(sourceCollectionName, sourceId, targetCollectionName, targetId)
        {
        }
    }
}
