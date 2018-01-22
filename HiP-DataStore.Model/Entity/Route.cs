using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class Route : ContentBase
    {
        // TODO: What about waypoints?

        public string Title { get; set; }

        public string Description { get; set; }

        public int Duration { get; set; }

        public double Distance { get; set; }

        public int? Image { get; set; }

        public int? Audio { get; set; }

        public List<int> Exhibits { get; set; } = new List<int>();

        public List<int> Tags { get; set; } = new List<int>();

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
            Exhibits = args.Exhibits ?? new List<int>();
            Status = args.Status;
            Tags = args.Tags ?? new List<int>();
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
