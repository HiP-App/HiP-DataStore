using PaderbornUniversity.SILab.Hip.EventSourcing;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class ExhibitDeleted : UserActivityBaseEvent, IDeleteEvent
    {
        public override ResourceType GetEntityType() => ResourceTypes.Exhibit;
    }
}
