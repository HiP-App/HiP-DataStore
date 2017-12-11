using PaderbornUniversity.SILab.Hip.DataStore.Model.Utility;
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

        [Reference(nameof(ResourceTypes.Media))]
        public int? Image { get; set; }

        [Range(-90, 90)]
        public float Latitude { get; set; }

        [Range(-180, 180)]
        public float Longitude { get; set; }

        [AllowedStatuses]
        public ContentStatus Status { get; set; }

        [Reference(nameof(ResourceTypes.Tag))]
        public List<int> Tags { get; set; }

        [Reference(nameof(ResourceTypes.ExhibitPage))]
        public List<int> Pages { get; set; }

        /// <summary>
        /// The radius (in km) in which the exhibit can be accessed.
        /// </summary>
        [Range(0.001, 1000)]
        public float AccessRadius { get; set; }
        
    }
}
