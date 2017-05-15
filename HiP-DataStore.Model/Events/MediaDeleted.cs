﻿using Newtonsoft.Json;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class MediaDeleted : IDeleteEvent
    {
        public int Id { get; set; }
        
        [JsonIgnore]
        public Type EntityType => typeof(MediaElement);
    }
}