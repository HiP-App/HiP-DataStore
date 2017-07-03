﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class Exhibit : ContentBase
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public float Latitude { get; set; }

        public float Longitude { get; set; }

        [BsonElement]
        public DocRef<MediaElement> Image { get; private set; } =
            new DocRef<MediaElement>(ResourceType.Media.Name);

        [BsonElement]
        public DocRefList<Tag> Tags { get; private set; } =
            new DocRefList<Tag>(ResourceType.Tag.Name);

        /// <remarks>
        /// This property and the references from pages to exhibit should always be in sync.
        /// Thus, when pages are created/deleted for a specific exhibit, that exhibit's Pages property must be updated.
        /// </remarks>
        [BsonElement]
        public DocRefList<ExhibitPage> Pages { get; private set; } =
            new DocRefList<ExhibitPage>(ResourceType.ExhibitPage.Name);

        public Exhibit()
        {
        }

        public Exhibit(ExhibitArgs args)
        {
            Name = args.Name;
            Description = args.Description;
            Image.Id = args.Image;
            Latitude = args.Latitude;
            Longitude = args.Longitude;
            Status = args.Status;
            Tags.Add(args.Tags?.Select(id => (BsonValue)id));
        }

        public Exhibit(ExhibitUpdateArgs args) : this((ExhibitArgs)args)
        {
            Pages.Add(args.Pages?.Select(id => (BsonValue)id));
        }
    }
}
