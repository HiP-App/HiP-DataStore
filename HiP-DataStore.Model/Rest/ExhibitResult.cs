using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using System;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class ExhibitResult
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public int? Image { get; set; }

        public float Latitude { get; set; }

        public float Longitude { get; set; }

        public bool Used { get; set; }

        public int[] Pages { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ContentStatus Status { get; set; }

        public int[] Tags { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public ExhibitResult()
        {
        }

        public ExhibitResult(Exhibit x)
        {
            Id = x.Id;
            Name = x.Name;
            Description = x.Description;
            Image = (int?)x.Image.Id;
            Latitude = x.Latitude;
            Longitude = x.Longitude;
            Used = x.Referencers.Count > 0; // an exhibit is in use if it is contained in (i.e. referenced by) a route
            Pages = x.Pages.Ids.Select(id => (int)id).ToArray();
            Status = x.Status;
            Tags = x.Tags.Ids.Select(id => (int)id).ToArray();
            Timestamp = x.Timestamp;
        }
    }
}
