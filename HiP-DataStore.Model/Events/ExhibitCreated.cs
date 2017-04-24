namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class ExhibitCreated : IEvent
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public int ImageId { get; set; }
    }
}
