using Newtonsoft.Json;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class MediaCreated : ICreateEvent
    {
        public int Id { get; set; }

        public MediaArgs Properties { get; set; }

        [JsonIgnore]
        public ResourceType EntityType => ResourceType.Media;

        [JsonIgnore]
        public ContentStatus Status => Properties.Status;
    }
}
