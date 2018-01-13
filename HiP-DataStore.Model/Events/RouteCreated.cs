using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.EventSourcing;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class RouteCreated : UserActivityBaseEvent, ICreateEvent
    {
        public RouteArgs Properties { get; set; }

        public override ResourceType GetEntityType() => ResourceTypes.Route;

        public ContentStatus GetStatus() => Properties.Status;
        
    }
}
