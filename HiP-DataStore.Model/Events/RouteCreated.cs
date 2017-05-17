using Newtonsoft.Json;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class RouteCreated : ICreateEvent
    {
        public int Id { get; set; }

        public RouteArgs Properties { get; set; }

        [JsonIgnore]
        public ResourceType EntityType => ResourceType.Route;

        [JsonIgnore]
        public ContentStatus Status => Properties.Status;
    }
}
