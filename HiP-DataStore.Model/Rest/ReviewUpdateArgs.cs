using Newtonsoft.Json;
using System.Collections.Generic;
using static PaderbornUniversity.SILab.Hip.DataStore.Model.Entity.Review;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class ReviewUpdateArgs
    {
        public bool Approved { get; set; }

        public string Description { get; set; }

        public string Comment { get; set; }

        /// <summary>
        /// Amount of students, that need to approve, in order to approve the review
        /// </summary>
        public int? StudentsToApprove { get; set; }

        public bool? ReviewableByStudents { get; set; }

        public List<string> Reviewers { get; set; }

        [JsonIgnore]
        public string EntityType { get; set; }

        [JsonIgnore]
        public int EntityId { get; set; }

        [JsonIgnore]
        public List<Comment> Comments { get; set; } = new List<Comment>();
    }
}
