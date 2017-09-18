namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class RouteDeleted : UserActivityBaseEvent, IDeleteEvent
    {
         public override ResourceType GetEntityType() => ResourceType.Route;
    }
}
