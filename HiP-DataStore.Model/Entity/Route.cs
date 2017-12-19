using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.EventSourcing.Mongo;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class Route : ContentBase
    {
        // TODO: What about waypoints?

        public string Title { get; set; }

        public string Description { get; set; }

        public int Duration { get; set; }

        public double Distance { get; set; }

        [ResourceReference(nameof(ResourceType.Media))]
        public int? Image { get; set; }

        [ResourceReference(nameof(ResourceType.Media))]
        public int? Audio { get; set; }

        [ResourceReference(nameof(ResourceType.Exhibit))]
        public List<int> Exhibits { get; set; }

        [ResourceReference(nameof(ResourceType.Tag))]
        public List<int> Tags { get; set; }

        public Route()
        {
        }

        public Route(RouteArgs args)
        {
            Title = args.Title;
            Description = args.Description;
            Duration = args.Duration;
            Distance = args.Distance;
            Image = args.Image;
            Audio = args.Audio;
            Exhibits = args.Exhibits;
            Status = args.Status;
            Tags = args.Tags;
        }

        public RouteArgs CreateRouteArgs()
        {
            var args = new RouteArgs();
            args.Title = Title;
            args.Description = Description;
            args.Duration = Duration;
            args.Distance = Distance;
            args.Image = Image.Id.AsNullableInt32;
            args.Audio = Audio.Id.AsNullableInt32;
            args.Exhibits = Exhibits.Ids.Select(i => i.AsInt32).ToList();
            args.Tags = Tags.Ids.Select(i => i.AsInt32).ToList();
            args.Status = Status;
            return args;
        }
    }
}
