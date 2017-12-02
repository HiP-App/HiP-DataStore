using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class ReviewCreated : UserActivityBaseEvent
    {
        public int EntityId { get; set; }

        public bool Approved { get; set; }

        public string Description { get; set; }

        public List<string> Reviewers { get; set; }

        public ResourceType ReviewType { get; set; }

        public override ResourceType GetEntityType() => ResourceType.Review;

    }
}
