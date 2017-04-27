namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    /// <summary>
    /// Model for creating new exhibits.
    /// </summary>
    public class ExhibitArgs
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public string Image { get; set; }
    }
}
