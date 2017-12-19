using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class RouteResult
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int Duration { get; set; }
        public double Distance { get; set; }
        public string UserId { get; set; }
        public int? Image { get; set; }
        public int? Audio { get; set; }
        public int[] Exhibits { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public ContentStatus Status { get; set; }
        public int[] Tags { get; set; }
        public DateTimeOffset Timestamp { get; set; }

        public RouteResult()
        {
        }

        public RouteResult(Route route)
        {
            Id = route.Id;
            Title = route.Title;
            Description = route.Description;
            Duration = route.Duration;
            Distance = route.Distance;
            UserId = route.UserId;
            Image = route.Image;
            Audio = route.Audio;
            Exhibits = route.Exhibits?.ToArray();
            Status = route.Status;
            Tags = route.Tags.ToArray();
            Timestamp = route.Timestamp;
        }
    }
}
