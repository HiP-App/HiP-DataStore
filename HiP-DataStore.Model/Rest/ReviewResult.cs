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

        public List<Comment> Comments { get; set; }

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
