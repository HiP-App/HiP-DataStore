using PaderbornUniversity.SILab.Hip.DataStore.Model.Utility;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

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

        [AllowedStatuses]
        public ContentStatus Status { get; set; }

        public List<int> Tags { get; set; }

        public IEnumerable<EntityId> GetReferences()
        {
            if (Image != null)
                yield return (ResourceType.Media, Image.Value);

            if (Audio != null)
                yield return (ResourceType.Media, Audio.Value);

            foreach (var exhibitId in Exhibits?.Distinct() ?? Enumerable.Empty<int>())
                yield return (ResourceType.Exhibit, exhibitId);

            foreach (var tagId in Tags?.Distinct() ?? Enumerable.Empty<int>())
                yield return (ResourceType.Tag, tagId);
        }
    }
}
