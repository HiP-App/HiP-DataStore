using PaderbornUniversity.SILab.Hip.EventSourcing;
using System;
using System.Collections.Generic;
using static PaderbornUniversity.SILab.Hip.DataStore.Model.Rest.ReviewResult;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class ReviewArgs2
    {
        public string UserId { get; set; }

        public bool Approved { get; set; }

        public string Description { get; set; }

        public int StudentsToApprove { get; set; }

        public bool ReviewableByStudents { get; set; }

        public Comment Comment { get; set; }

        public ResourceType EntityType { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public List<string> Reviewers { get; set; }
    }
}
