using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.EventSourcing;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class RatingAdded : UserActivityBaseEvent
    {

        public RatingAdded()
        {
        }

        public int EntityId { get; set; }

        // User Rating Value
        public byte Value { get; set; }

        public ResourceType RatedType { get; set; }

        public override ResourceType GetEntityType() => ResourceTypes.Rating;

    }
}
