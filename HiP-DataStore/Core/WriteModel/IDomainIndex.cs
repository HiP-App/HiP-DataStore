using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel
{
    /// <summary>
    /// An "index" caches specific parts of the domain model for efficient access during validation.
    /// </summary>
    public interface IDomainIndex
    {
        void ApplyEvent(IEvent e);
    }
}
