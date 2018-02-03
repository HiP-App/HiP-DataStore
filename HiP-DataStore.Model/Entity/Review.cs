using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using System;
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
            Description = args.Description;
            StudentsToApprove = args.StudentsToApprove;
            ReviewableByStudents = args.ReviewableByStudents;
            Reviewers = args.Reviewers;
        }

        public Review(ReviewUpdateArgs args)
        {
            Approved = args.Approved;
            Description = args.Description;
            StudentsToApprove = args.StudentsToApprove ?? 0;
            ReviewableByStudents = args.ReviewableByStudents ?? false;
            Reviewers = args.Reviewers;
            Comments = args.Comments;
            EntityId = args.EntityId;
            EntityType = args.EntityType;
        }

        public ReviewUpdateArgs CreateReviewUpdateArgs()
        {
            var args = new ReviewUpdateArgs()
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

        // Type of the entity the review belongs tos
        public string EntityType { get; set; }

        // Id of the entity the review belongs to
        public int EntityId { get; set; }

        public string Description { get; set; }

        public int StudentsToApprove { get; set; }

        public bool ReviewableByStudents { get; set; }

        public List<string> Reviewers { get; set; }

        public List<Comment> Comments { get; set; } = new List<Comment>();

        public class Comment
        {
            public string Text { get; set; }

            public DateTimeOffset Timestamp { get; set; }

            public string UserId { get; set; }

            public bool Approved { get; set; }

            public Comment(string text, DateTimeOffset timestamp, string userId, bool approved)
            {
                Text = text;
                Timestamp = timestamp;
                UserId = userId;
                Approved = approved;
            }
        }
    }
}
