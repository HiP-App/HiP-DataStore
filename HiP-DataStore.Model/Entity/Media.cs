using MongoDB.Bson.Serialization.Attributes;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public abstract class MediaElement : ContentBase
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public bool IsUsed { get; set; }

        /// <summary>
        /// The path to the actual file.
        /// </summary>
        public string File { get; set; }
    }

    public class Image : MediaElement
    {
        public const string CollectionName = "images";

    }

    public class Audio : MediaElement
    {
        public const string CollectionName = "audio";
    }
}
