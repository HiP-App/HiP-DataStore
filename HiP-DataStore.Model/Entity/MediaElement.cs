namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    class MediaElement
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public MediaType Type { get; set; }
    }

    enum MediaType
    {
        Image, Audio
    }
}
