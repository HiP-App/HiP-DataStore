using PaderbornUniversity.SILab.Hip.DataStore.Model.Utility;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class TagArgs
    {
        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        public int? Image { get; set; }

        [AllowedStatuses]
        public ContentStatus Status { get; set; }
        
        public IEnumerable<EntityId> GetReferences()
        {
            if (Image != null)
                yield return (ResourceType.Media, Image.Value);
        }
    }
}
