using MongoDB.Bson.Serialization.Attributes;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    /// <remarks>
    /// An <see cref="ExhibitPage"/> is part of an <see cref="Exhibit"/> document.
    /// There is no need to store pages in their own Mongo collection, since a page is only ever
    /// referenced by a single exhibit.
    /// </remarks>
    public class ExhibitPage : ContentBase
    {
        // TODO: What about the page type? (AppetizerPage, ImagePage, SliderPage)

        [BsonElement(nameof(Image))]
        private DocRef<Image> _image = new DocRef<Image>(Entity.Image.CollectionName);

        [BsonElement(nameof(Audio))]
        private DocRef<Audio> _audio = new DocRef<Audio>(Entity.Audio.CollectionName);

        [BsonElement(nameof(Images))]
        private DocRefList<Image> _images = new DocRefList<Image>(Entity.Image.CollectionName);

        public int Id { get; set; }

        public string Text { get; set; }

        public bool HideYearNumbers { get; set; }

        public DocRef<Audio> Audio => _audio;

        public DocRef<Image> Image => _image;

        public DocRefList<Image> Images => _images;
    }
}
