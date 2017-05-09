using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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
        public int? Image { get; set; }
        public int? Audio { get; set; }
        public int[] Exhibits { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public ContentStatus Status { get; set; }
        public int[] Tags { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}
