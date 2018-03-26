using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class Review : ContentBase
    {
        public Review()
        {
        }

        public Review(ReviewArgs args)
        {
            Approved = args.Approved;
            Description = args.Description;
            StudentsToApprove = args.StudentsToApprove ?? 0;
            ReviewableByStudents = args.ReviewableByStudents ?? false;
            Reviewers = args.Reviewers;
            Comments = args.Comments;
            EntityType = args.EntityType;
            EntityId = args.EntityId;
        }

        public ReviewArgs CreateReviewArgs()
        {
            var args = new ReviewArgs()
            {
                Approved = Approved,
                Description = Description,
                ReviewableByStudents = ReviewableByStudents,
                Reviewers = Reviewers,
                StudentsToApprove = StudentsToApprove,
                Comments = Comments,
                EntityType = EntityType,
                EntityId = EntityId
            };
            return args;
        }

        public bool Approved { get; set; }

        // Type of the entity the review belongs to
        public string EntityType { get; set; }

        // ID of the entity the review belongs to
        public int EntityId { get; set; }

        public string Description { get; set; }

        public int StudentsToApprove { get; set; }

        public bool ReviewableByStudents { get; set; }

        public List<string> Reviewers { get; set; }

        public List<int> Comments { get; set; } = new List<int>();
    }
}
