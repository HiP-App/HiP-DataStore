using Newtonsoft.Json;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class MediaCreated : ICreateEvent
    {
        public int Id { get; set; }

        public MediaArgs Properties { get; set; }

        [JsonIgnore]
        public Type EntityType => typeof(MediaElement);

        [JsonIgnore]
        public ContentStatus Status => Properties.Status;
    }
}
