namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class TagDeleted : IDeleteEvent
    {
        public int Id { get; set; }

        public ResourceType GetEntityType() => ResourceType.Tag;

    }
}
