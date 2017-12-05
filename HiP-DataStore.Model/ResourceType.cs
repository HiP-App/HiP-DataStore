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


        /// <summary>
        /// Initalizes the fields
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
        }
    }
}
