namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class MediaDeleted : UserActivityBaseEvent, IDeleteEvent
    {
        public override ResourceType GetEntityType() => ResourceType.Media;
    }
}
