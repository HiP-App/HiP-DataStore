using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.EventSourcing.Mongo;
using System.Collections.Generic;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class Exhibit : ContentBase
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public float Latitude { get; set; }

        public float Longitude { get; set; }

        public float AccessRadius { get; set; }

        [ResourceReference(nameof(ResourceType.Media))]
        public int? Image { get; set; }

        [ResourceReference(nameof(ResourceType.Tag))]
        public List<int> Tags { get; set; }

        [ResourceReference(nameof(ResourceType.ExhibitPage))]
        public List<int> Pages { get; private set; }

        public Exhibit()
        {
        }

        public Exhibit(ExhibitArgs args)
        {
            Name = args.Name;
            Description = args.Description;
            Image = args.Image;
            Latitude = args.Latitude;
            Longitude = args.Longitude;
            Status = args.Status;
            Tags = args.Tags;
            Pages = args.Pages;
            AccessRadius = args.AccessRadius;
        }

        public ExhibitArgs CreateExhibitArgs()
        {
            var args = new ExhibitArgs
            {
                Name = Name,
                Description = Description,
                Image = Image.Id.AsNullableInt32,
                Latitude = Latitude,
                Longitude = Longitude,
                Status = Status,
                Tags = Tags?.Ids.Select(id => id.AsInt32).ToList(),
                Pages = Pages?.Ids.Select(id => id.AsInt32).ToList(),
                AccessRadius = AccessRadius
            };
            return args;
        }
    }
}
