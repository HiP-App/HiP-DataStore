using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

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

        public int? Image { get; set; }

        [Range(-90, 90)]
        public float Latitude { get; set; }

        [Range(-180, 180)]
        public float Longitude { get; set; }

        public ContentStatus Status { get; set; }

        public List<int> Tags { get; set; }

        public List<int> Pages { get; set; }

        public IEnumerable<EntityId> GetReferences()
        {
            if (Image != null)
                yield return (ResourceType.Media, Image.Value);

            foreach (var pageId in Pages?.Distinct() ?? Enumerable.Empty<int>())
                yield return (ResourceType.ExhibitPage, pageId);

            foreach (var tagId in Tags?.Distinct() ?? Enumerable.Empty<int>())
                yield return (ResourceType.Tag, tagId);
        }
    }
}
