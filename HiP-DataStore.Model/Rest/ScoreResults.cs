namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class ScoreResults : AllItemsResult<ScoreResult>
    {
        public int Rank { get; set; }

        public ScoreResults(AllItemsResult<ScoreResult> all)  {
           Items = all.Items;
           Total = all.Total;
        }
     }
}
