namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class RatingAdded : UserActivityBaseEvent
    {
        public int EntityId { get; set; }

        // User Rating Value
        public int Value { get; set; }

        public int? OldValue { get; set; }

        public ResourceType RatedType { get; set; }

        public override ResourceType GetEntityType() => ResourceType.Rating;
        
    }
}
