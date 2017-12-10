using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class ReviewUpdated : UserActivityBaseEvent
    {
        public int EntityId { get; set; }

        public bool ApprovedComment { get; set; }

        public bool Approved { get; set; }

        public string Comment { get; set; }

        public int StudentsToApprove { get; set; }

        public bool ReviewableByStudents { get; set; }

        public List<string> Reviewers { get; set; }

        public ResourceType ReviewType { get; set; }

        public override ResourceType GetEntityType() => ResourceType.Review;
    }
}
