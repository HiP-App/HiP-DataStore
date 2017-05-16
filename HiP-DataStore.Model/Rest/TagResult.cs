﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

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
    }
}
