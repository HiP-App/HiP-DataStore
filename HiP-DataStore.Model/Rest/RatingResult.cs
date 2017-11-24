using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class RatingResult
    {
        public int Id { get; set; }

        public double Average { get; set; }

        public int Count { get; set; }

        public Dictionary<int,int> RatingTable { get; set; }
    }
}
