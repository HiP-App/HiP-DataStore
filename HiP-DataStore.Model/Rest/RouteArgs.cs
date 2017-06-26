using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class RouteArgs
    {
        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        [Range(0, int.MaxValue)]
        public int Duration { get; set; }

        [Range(0, double.PositiveInfinity)]
        public double Distance { get; set; }

        public int? Image { get; set; }

        public int? Audio { get; set; }

        public List<int> Exhibits { get; set; }

        public ContentStatus Status { get; set; }

        public List<int> Tags { get; set; }
    }
}
