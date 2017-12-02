using System;
using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class ReviewUpdated : UserActivityBaseEvent
    {
        public int EntityId { get; set; }

        public bool Approved { get; set; }

        public string Comment { get; set; }

        public List<string> Reviewers { get; set; }

        public ResourceType ReviewType { get; set; }

        public override ResourceType GetEntityType() => ResourceType.Review;
    }
}
