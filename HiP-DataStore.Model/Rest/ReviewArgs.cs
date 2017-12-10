using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class ReviewArgs
    {
        public string Description { get; set; }

        public List<string> Reviewers { get; set; }

        public int StudentsToApprove { get; set; }

        public bool ReviewableByStudents { get; set; }
    }
}
