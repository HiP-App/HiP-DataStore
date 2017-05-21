namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class ExhibitDeleted : IDeleteEvent
    {
        public int Id { get; set; }

        public ResourceType GetEntityType() => ResourceType.Exhibit;
    }
}
