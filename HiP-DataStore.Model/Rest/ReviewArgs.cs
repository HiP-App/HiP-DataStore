using Newtonsoft.Json;
using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class ReviewArgs
    {

        public string Description { get; set; }

        public List<string> Reviewers { get; set; }

        /// <summary>
        /// Amount of students, that need to approve, in order to approve the review
        /// </summary>
        public int? StudentsToApprove { get; set; }

        public bool? ReviewableByStudents { get; set; }

        [JsonIgnore]
        public bool Approved { get; set; }

        [JsonIgnore]
        public List<int> Comments { get; set; } = new List<int>();

        [JsonIgnore]
        public string EntityType { get; set; }

        [JsonIgnore]
        public int EntityId { get; set; }

        public ReviewArgs CloneArgs()
        {
            var copy = (ReviewArgs) MemberwiseClone();
            copy.Comments = new List<int>(Comments);
            return copy;
        }
    }
}
