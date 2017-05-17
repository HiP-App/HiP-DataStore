using Newtonsoft.Json;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class ExhibitDeleted : IDeleteEvent
    {
        public int Id { get; set; }

        [JsonIgnore]
        public ResourceType EntityType => ResourceType.Exhibit;
    }
}
