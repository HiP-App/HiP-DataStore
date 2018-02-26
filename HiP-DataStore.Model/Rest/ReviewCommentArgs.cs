using Newtonsoft.Json;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class ReviewCommentArgs
    {
        public string Text { get; set; }

        public bool Approved { get; set; }

        [JsonIgnore]
        public int ReviewId { get; set; }
    }
}
