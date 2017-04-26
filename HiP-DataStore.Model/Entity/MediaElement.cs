namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class MediaElement
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public MediaType Type { get; set; }
    }

    public enum MediaType
    {
        Image, Audio
    }
}
