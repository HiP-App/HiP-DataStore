using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class MediaResult
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public bool Used { get; set; }

        /// <summary>
        /// The URL from which the file can be downloaded.
        /// For images, a URL pointing to the thumbnail service.
        /// For audio, a URL pointing to "GET /api/Media/{id}/File".
        /// </summary>
        public string File { get; set; }

        public string UserId { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public MediaType Type { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ContentStatus Status { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public MediaResult()
        {
        }

        public MediaResult(MediaElement x)
        {
            Id = x.Id;
            Title = x.Title;
            Description = x.Description;
            Used = x.Referencers.Count > 0;
            UserId = x.UserId;
            Type = x.Type;
            Status = x.Status;
            Timestamp = x.Timestamp;
        }
    }
}
