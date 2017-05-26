namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class MediaDeleted : IDeleteEvent
    {
        public int Id { get; set; }

        public ResourceType GetEntityType() => ResourceType.Media;
    }
}
