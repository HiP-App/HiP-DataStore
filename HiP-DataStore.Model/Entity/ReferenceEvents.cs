using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public abstract class ReferenceEventBase : IEvent
    {
        /// <summary>
        /// Collection where the referencing entity is in.
        /// </summary>
        public string SourceCollectionName { get; set; }

        /// <summary>
        /// ID of the referencing entity.
        /// </summary>
        public int SourceId { get; set; }

        /// <summary>
        /// Collection where the referenced entity is in.
        /// </summary>
        public string TargetCollectionName { get; set; }

        /// <summary>
        /// ID of the referenced entity.
        /// </summary>
        public int TargetId { get; set; }

        public ReferenceEventBase()
        {
        }

        public ReferenceEventBase(string sourceCollectionName, int sourceId, string targetCollectionName, int targetId)
        {
            SourceCollectionName = sourceCollectionName;
            SourceId = sourceId;
            TargetCollectionName = targetCollectionName;
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

        public ReferenceAdded(string sourceCollectionName, int sourceId, string targetCollectionName, int targetId)
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

        public ReferenceRemoved(string sourceCollectionName, int sourceId, string targetCollectionName, int targetId)
            : base(sourceCollectionName, sourceId, targetCollectionName, targetId)
        {
        }
    }
}
