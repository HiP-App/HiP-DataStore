using Newtonsoft.Json;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class TagUpdated : IUpdateEvent
    {
        public int Id { get; set; }
        public TagUpdateArgs Properties { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        [JsonIgnore]
        public DocRef<MediaElement> Image => new DocRef<MediaElement>(Properties.Image,ResourceType.Media.Name);

        [JsonIgnore]
        public ContentStatus Status { get; set; }

        [JsonIgnore]
        public ResourceType EntityType => ResourceType.Tag;


    }
}
