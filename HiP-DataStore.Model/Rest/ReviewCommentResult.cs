using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class ReviewCommentResult
    {
        public int Id { get; set; }

        public int ReviewId { get; set; }

        public string Text { get; set; }

        public bool Approved { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public string UserId { get; set; }

        public ReviewCommentResult(ReviewComment reviewComment)
        {
            Id = reviewComment.Id;
            Text = reviewComment.Text;
            Timestamp = reviewComment.Timestamp;
            UserId = reviewComment.UserId;
            ReviewId = reviewComment.ReviewId;
            Approved = reviewComment.Approved;
        }
    }
}
