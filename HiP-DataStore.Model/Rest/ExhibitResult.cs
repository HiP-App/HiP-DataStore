using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class ExhibitResult
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int? Image { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public bool Used { get; set; }
        public ContentStatus Status { get; set; }
        public int[] Tags { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}
