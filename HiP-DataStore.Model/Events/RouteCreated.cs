using Newtonsoft.Json;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class RouteCreated : ICreateEvent
    {
        public int Id { get; set; }

        public RouteArgs Properties { get; set; }

        [JsonIgnore]
        public Type EntityType => typeof(Exhibit);

        [JsonIgnore]
        public ContentStatus Status => Properties.Status;
    }
}
