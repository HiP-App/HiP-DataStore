using Newtonsoft.Json;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class ExhibitCreated : ICreateEvent
    {
        public int Id { get; set; }

        public ExhibitArgs Properties { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        [JsonIgnore]
        public ResourceType EntityType => ResourceType.Exhibit;

        [JsonIgnore]
        public ContentStatus Status => Properties.Status;
    }
}
