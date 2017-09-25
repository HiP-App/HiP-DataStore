namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class TagDeleted : UserActivityBaseEvent, IDeleteEvent
    {
        public override ResourceType GetEntityType() => ResourceType.Tag;
    }
}
