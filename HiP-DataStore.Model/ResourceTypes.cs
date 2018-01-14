using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.EventSourcing;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model
{
    public static class ResourceTypes
    {
        public static ResourceType Rating { get; private set; }
        public static ResourceType Exhibit { get; private set; }
        public static ResourceType ExhibitPage { get; private set; }
        public static ResourceType Route { get; private set; }
        public static ResourceType Media { get; private set; }
        public static ResourceType Tag { get; private set; }
        public static ResourceType ScoreRecord { get; private set; }
        public static ResourceType ExhibitReview { get; private set; }
        public static ResourceType ExhibitPageReview { get; private set; }
        public static ResourceType RouteReview { get; private set; }


        /// <summary>
        /// Initializes the fields
        /// </summary>
        public static void Initialize()
        {
            Exhibit = ResourceType.Register(nameof(Exhibit), typeof(ExhibitArgs));
            ExhibitPage = ResourceType.Register(nameof(ExhibitPage), typeof(ExhibitPageArgs2));
            Route = ResourceType.Register(nameof(Route), typeof(RouteArgs));
            Media = ResourceType.Register(nameof(Media), typeof(MediaArgs));
            Tag = ResourceType.Register(nameof(Tag), typeof(TagArgs));
            ScoreRecord = ResourceType.Register(nameof(ScoreRecord), typeof(ScoreBoardArgs));
            Rating = ResourceType.Register(nameof(Rating), typeof(RatingArgs));
            ExhibitReview = ResourceType.Register(nameof(ExhibitReview), typeof(ReviewArgs));
            ExhibitPageReview = ResourceType.Register(nameof(ExhibitPageReview), typeof(ReviewArgs));
            RouteReview = ResourceType.Register(nameof(RouteReview), typeof(ReviewArgs));
        }
    }
}
