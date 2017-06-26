namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class RouteDeleted : IDeleteEvent
    {
        public int Id { get; set; }

        public ResourceType GetEntityType() => ResourceType.Route;
    }
}
