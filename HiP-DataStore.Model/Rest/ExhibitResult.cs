﻿using Newtonsoft.Json;
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
            Used = x.Referencees.Any(r => r.Collection == ResourceType.Route.Name); // an exhibit is in use if it is contained in a route
            Pages = x.Referencees.Where(r => r.Collection == ResourceType.ExhibitPage.Name).Select(r => (int)r.Id).ToArray();
            Status = x.Status;
            Tags = x.Tags.Select(id => (int)id).ToArray();
            Timestamp = x.Timestamp;
        }
    }
}
