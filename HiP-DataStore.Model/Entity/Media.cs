namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class MediaElement : ContentBase
    {
        public const string CollectionName = "media";

        public string Title { get; set; }

        public string Description { get; set; }

        /// <summary>
        /// The path to the actual file.
        /// </summary>
        public string File { get; set; }

        public MediaType Type { get; set; }
    }

    public enum MediaType
    {
        Image, Audio
    }
}
