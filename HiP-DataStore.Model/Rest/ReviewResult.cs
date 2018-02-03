using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using System;
using System.Collections.Generic;
using static PaderbornUniversity.SILab.Hip.DataStore.Model.Entity.Review;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class ReviewResult
    {
        public int Id { get; set; }

        public string Description { get; set; }

        public bool Approved { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public List<string> Reviewers { get; set; }

        public List<Comment> Comments { get; set; }

        public ReviewResult(Review review)
        {
            Id = review.Id;
            Description = review.Description;
            Approved = review.Approved;
            Timestamp = review.Timestamp;
            Reviewers = review.Reviewers;
            Comments = review.Comments;
        }

    }
}
