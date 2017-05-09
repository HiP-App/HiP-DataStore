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
        private DocRef<MediaElement> _image = new DocRef<MediaElement>(MediaElement.CollectionName);

        [BsonElement(nameof(Audio))]
        private DocRef<MediaElement> _audio = new DocRef<MediaElement>(MediaElement.CollectionName);

        [BsonElement(nameof(Images))]
        private DocRefList<MediaElement> _images = new DocRefList<MediaElement>(MediaElement.CollectionName);

        public string Text { get; set; }

        public bool HideYearNumbers { get; set; }

        public DocRef<MediaElement> Audio => _audio;

        public DocRef<MediaElement> Image => _image;

        public DocRefList<MediaElement> Images => _images;
    }
}
