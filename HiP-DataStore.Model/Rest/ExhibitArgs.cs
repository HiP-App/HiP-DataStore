using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    /// <summary>
    /// Model for creating new exhibits.
    /// </summary>
    public class ExhibitArgs
    {
        [Required]
        public string Name { get; set; }
        public string Description { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public int? Image { get; set; }
        public List<int> Tags { get; set; }
    }
}
