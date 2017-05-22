using System;
using System.Collections.Generic;
using System.Text;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class ExhibitUpdated : IUpdateEvent
    {
        public int Id { get; set; }

        public ExhibitArgs Properties { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public ContentStatus GetStatus() => Properties.Status;

        public ResourceType GetEntityType() => ResourceType.Exhibit;
    }
}
