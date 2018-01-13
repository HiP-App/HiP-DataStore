using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Utility;
using System.ComponentModel.DataAnnotations;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class MediaArgs : IContentArgs
    {
        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        public MediaType Type { get; set; }

        [AllowedStatuses]
        public ContentStatus Status { get; set; }
    }
}
