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

    public static class MigratableExtensions
    {
        /// <summary>
        /// Applies all possible migrations in order to update an object to the latest version of its type.
        /// If the specified object does not support migration, it is returned as is.
        /// </summary>
        /// <typeparam name="TBase">The base class or interface all versions of the type have in common</typeparam>
        public static TBase MigrateToLatestVersion<TBase>(this TBase obj)
        {
            while (obj is IMigratable<TBase> o)
                obj = o.Migrate();

            return obj;
        }
    }
}
