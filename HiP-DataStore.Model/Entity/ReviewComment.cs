using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class ReviewComment : ContentBase
    {
        public string Text { get; set; }

        public bool Approved { get; set; }

        public ReviewComment(ReviewCommentArgs args)
        {
            Text = args.Text;
            Approved = args.Approved;
        }

        public ReviewComment()
        {
        }

        public ReviewCommentArgs CreateReviewCommentArgs()
        {
            var args = new ReviewCommentArgs()
            {
                Text = Text,
                Approved = Approved,
            };
            return args;
        }
    }
}
