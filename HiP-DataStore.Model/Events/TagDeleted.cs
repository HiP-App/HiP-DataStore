using Newtonsoft.Json;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class TagDeleted : IDeleteEvent
    {
        public int Id { get; set; }

        [JsonIgnore]
        public ResourceType EntityType => ResourceType.Tag;
    }
}
