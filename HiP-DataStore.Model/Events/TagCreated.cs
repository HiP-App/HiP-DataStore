using Newtonsoft.Json;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class TagCreated : ICreateEvent
    {
        public int Id { get; set; }

        public TagArgs Properties { get; set; }

        [JsonIgnore]
        public ResourceType EntityType => ResourceType.Tag;

        [JsonIgnore]
        public ContentStatus Status => Properties.Status;

    }
}
