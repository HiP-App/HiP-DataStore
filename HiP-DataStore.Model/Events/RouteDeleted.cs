using PaderbornUniversity.SILab.Hip.EventSourcing;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class RouteDeleted : UserActivityBaseEvent, IDeleteEvent
    {
         public override ResourceType GetEntityType() => ResourceTypes.Route;
    }
}
