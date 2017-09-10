namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class ExhibitDeleted : UserActivityBaseEvent, IDeleteEvent
    {
        public override ResourceType GetEntityType() => ResourceType.Exhibit;
    }
}
