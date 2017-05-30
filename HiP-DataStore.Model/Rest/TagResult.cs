using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class TagResult
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int? Image { get; set; }
        public bool Used { get; set; }
        public DateTimeOffset Timestamp { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ContentStatus Status { get; set; }

        public static TagResult ConvertFromTag(Tag tag)
        {
            return new TagResult
            {
                Id = tag.Id,
                Title = tag.Title,
                Description = tag.Description,
                Image = tag.Image.Id.AsNullableInt32,
                Used = tag.Referencees.Count > 0,
                Status = tag.Status,
                Timestamp = tag.Timestamp,
            };
        }
    }
}
