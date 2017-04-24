namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    class Exhibit
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public MediaElement Image { get; set; }
    }
}
