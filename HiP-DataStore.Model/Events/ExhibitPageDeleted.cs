namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class ExhibitPageDeleted : IDeleteEvent
    {
        public int Id { get; set; }

        public ResourceType GetEntityType() => ResourceType.ExhibitPage;
    }
}
