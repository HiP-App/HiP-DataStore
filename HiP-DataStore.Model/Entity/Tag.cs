using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.EventSourcing;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class Tag : ContentBase
    {
        public string Title { get; set; }

        public string Description { get; set; }

        [ResourceReference(nameof(ResourceType.Media))]
        public int? Image { get; set; }

        public Tag()
        {
        }

        public Tag(TagArgs args)
        {
            Title = args.Title;
            Description = args.Description;
            Image = args.Image;
            Status = args.Status;
        }
    }
}
