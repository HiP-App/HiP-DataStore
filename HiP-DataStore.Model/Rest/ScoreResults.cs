using System;
using System.Collections.Generic;
using System.Text;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class ScoreResults : AllItemsResult<ScoreResult>
    {
        public int Rank { get; set; }

        public ScoreResults(AllItemsResult<ScoreResult> all)  {
            this.Items = all.Items;
            this.Total = all.Total;
        }
     }
}
