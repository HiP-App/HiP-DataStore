using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class Exhibit : ContentBase
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public float Latitude { get; set; }

        public float Longitude { get; set; }

        public float AccessRadius { get; set; }

        public int? Image { get; set; }

        public List<int> Tags { get; set; }

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
                Image = Image,
                Latitude = Latitude,
                Longitude = Longitude,
                Status = Status,
                Tags = Tags,
                Pages = Pages,
                AccessRadius = AccessRadius
            };
            return args;
        }
    }
}
