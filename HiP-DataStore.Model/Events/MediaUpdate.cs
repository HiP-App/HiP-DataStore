using Newtonsoft.Json;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using System;
using System.Collections.Generic;
using System.Text;

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
        public Type EntityType => typeof(MediaElement);        
    }
}
