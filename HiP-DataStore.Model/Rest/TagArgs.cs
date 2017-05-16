using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class TagArgs
    {
        [Required]
        public string Tille { get; set; }

        public string Description { get; set; }

        public int? Image { get; set; }

        public ContentStatus Status { get; set; }
        
    }
}
