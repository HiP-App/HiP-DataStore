using MongoDB.Bson;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class Exhibit
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public DocRef<MediaElement> Image { get; set; }
    }
}
