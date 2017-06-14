namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    /// <summary>
    /// Types implementing this interface can migrate objects to a newer version of the type.
    /// This is used for example to transform old events from the event log to the latest version.
    /// </summary>
    public interface IMigratable<out T>
    {
        /// <summary>
        /// Transforms this object to an object of a newer version of the (logically) same type.
        /// </summary>
        /// <returns></returns>
        T Migrate();
    }
}
