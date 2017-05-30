using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    /// <remarks>
    /// An <see cref="ExhibitPage"/> is part of an <see cref="Exhibit"/> document.
    /// There is no need to store pages in their own Mongo collection, since a page is only ever
    /// referenced by a single exhibit.
    /// </remarks>
    public class ExhibitPage : ContentBase
    {
        public PageType Type { get; set; }

        public string Title { get; set; }

        public string Text { get; set; }

        public string Description { get; set; }

        public string FontFamily { get; set; }

        [BsonElement]
        public DocRef<Exhibit> Exhibit { get; private set; } =
            new DocRef<Exhibit>(ResourceType.Exhibit.Name);

        [BsonElement]
        public DocRef<MediaElement> Audio { get; private set; } =
            new DocRef<MediaElement>(ResourceType.Media.Name);

        [BsonElement]
        public DocRef<MediaElement> Image { get; private set; } =
            new DocRef<MediaElement>(ResourceType.Media.Name);

        [BsonElement]
        public DocRefList<MediaElement> Images { get; private set; } =
            new DocRefList<MediaElement>(ResourceType.Media.Name);

        public bool HideYearNumbers { get; set; }

        [BsonElement]
        public DocRefList<ExhibitPage> AdditionalInformationPages { get; private set; } =
            new DocRefList<ExhibitPage>(ResourceType.ExhibitPage.Name);

        public ExhibitPage()
        {
        }

        public ExhibitPage(ExhibitPageArgs args)
        {
            Type = args.Type;
            Title = args.Title;
            Text = args.Text;
            Description = args.Description;
            FontFamily = args.FontFamily;
            Audio.Id = args.Audio;
            Image.Id = args.Image;
            Images.Add(args.Images?.Select(id => (BsonValue)id));
            HideYearNumbers = args.HideYearNumbers ?? false;
            AdditionalInformationPages.Add(args.AdditionalInformationPages?.Select(id => (BsonValue)id));
        }
    }
}
