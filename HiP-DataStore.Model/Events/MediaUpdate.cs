using Newtonsoft.Json;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public  class MediaUpdate : IUpdateEvent
    {
        public int Id { get; set; }
        public MediaUpdateArgs Properties { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        [JsonIgnore]
        public ContentStatus Status { get; set; }

        [JsonIgnore]
        public ResourceType EntityType => ResourceType.Media;
    }
}
