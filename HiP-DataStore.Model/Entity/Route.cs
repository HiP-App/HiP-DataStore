using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using System.Collections.Generic;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class Route : ContentBase
    {
        // TODO: What about waypoints?

        public string Title { get; set; }

        public string Description { get; set; }

        public int Duration { get; set; }

        public double Distance { get; set; }

        [ResourceReference(nameof(ResourceTypes.Media))]
        public int? Image { get; set; }

        [ResourceReference(nameof(ResourceTypes.Media))]
        public int? Audio { get; set; }

        [ResourceReference(nameof(ResourceTypes.Exhibit))]
        public List<int> Exhibits { get; set; }

        [ResourceReference(nameof(ResourceTypes.Tag))]
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
            args.Image = Image;
            args.Audio = Audio;
            args.Exhibits = Exhibits;
            args.Tags = Tags;
            args.Status = Status;
            return args;
        }
    }
}
