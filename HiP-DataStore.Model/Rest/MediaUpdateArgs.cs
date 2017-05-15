using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class MediaUpdateArgs
    {
        [BsonIgnoreIfNull]
        public string Title { get; set; }

        [BsonIgnoreIfNull]
        public string Description { get; set; }

        [BsonIgnoreIfNull]
        public MediaType? Type { get; set; }

        [BsonIgnoreIfNull]
        public ContentStatus? Status { get; set; }

              
    }
}
