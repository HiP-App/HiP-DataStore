﻿using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using System.ComponentModel.DataAnnotations;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class MediaArgs
    {
        [Required]
        public string Title { get; set; }

        public string Description { get; set; }


        public MediaType Type { get; set; }
        public ContentStatus Status { get; set; }
    }
}