using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.Migrations
{
    /// <summary>
    /// Specifies the version for which a migration is applicable and the version the migration transforms the stream to.
    /// </summary>
    /// <remarks>
    /// Migrations are applied using the <see cref="StreamMigrator"/> class. In order for <see cref="StreamMigrator"/>
    /// to recognize a migration class, the class must implement <see cref="IStreamMigration"/> and must be annotated
    /// with the <see cref="StreamMigrationAttribute"/>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class StreamMigrationAttribute : Attribute
    {
        /// <summary>
        /// The stream version this migration is able to process.
        /// </summary>
        public int FromVersion { get; }

        /// <summary>
        /// The version the stream is migrated to.
        /// </summary>
        public int ToVersion { get; }

        public StreamMigrationAttribute(int from, int to)
        {
            FromVersion = from;
            ToVersion = to;
        }
    }
}
