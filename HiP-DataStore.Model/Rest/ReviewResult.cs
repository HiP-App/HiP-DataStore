using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using System;
using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class ReviewResult
    {
        public int Id { get; set; }

        public string Description { get; set; }

        public bool Approved { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public List<string> Reviewers { get; set; }

        public List<int> Comments { get; set; }

        public string UserId { get; set; }

        public ReviewResult(Review review)
        {
            Id = review.Id;
            Description = review.Description;
            Approved = review.Approved;
            Timestamp = review.Timestamp;
            Reviewers = review.Reviewers;
            Comments = review.Comments;
            UserId = review.UserId;
        }

    }
}
