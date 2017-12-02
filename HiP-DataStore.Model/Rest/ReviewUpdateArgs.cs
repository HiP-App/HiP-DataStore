using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class ReviewUpdateArgs
    {
        public bool Approved { get; set; }

        public string Comment { get; set; }

        public List<string> Reviewers { get; set; }
    }
}
