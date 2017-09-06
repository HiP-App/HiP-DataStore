using System.Collections.Generic;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using System;
using System.Reflection;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.Migrations
{
    /// <summary>
    /// Provides methods to update an Event Store stream to the latest version by applying one or multiple
    /// migrations defined in the HiP-DataStore assembly.
    /// </summary>
    public static class StreamMigrator
    {
        public const string StreamVersionMetadataKey = "StreamVersion";

        public static async Task<(int fromVersion, int toVersion)> MigrateAsync(IEventStoreConnection connection, string streamName)
        {
            // Get current stream version from metadata
            var initialVersion = await GetStreamVersionAsync(connection, streamName) ?? 0;
            var currentVersion = initialVersion;
            
            // Find all applicable migrations in the current assembly
            var availableMigrations = GetAvailableMigrations()
                .Where(t => t.Properties.FromVersion >= currentVersion)
                .OrderBy(t => t.Properties.FromVersion)
                .ToList();

            // Check for ambiguities (i.e. are there multiple migrations for the same source version?)
            if (availableMigrations.GroupBy(t => t.Properties.FromVersion).Any(g => g.Count() > 1))
                throw new InvalidOperationException("The assembly defines multiple migrations for the same source version");

            // Repeatedly apply the migration with FromVersion == currentVersion and maximum ToVersion,
            // until no more migrations are applicable
            MigrationTypeInfo chosenMigration;

            while ((chosenMigration = availableMigrations.FirstOrDefault(t => t.Properties.FromVersion == currentVersion)) != null)
            {
                // from the group of matching migrations, choose the one with maximum ToVersion
                await ExecuteMigrationAsync(chosenMigration, connection, streamName);
                currentVersion = chosenMigration.Properties.ToVersion;
            }

            return (initialVersion, currentVersion);
        }

        private static async Task<int?> GetStreamVersionAsync(IEventStoreConnection connection, string streamName)
        {
            var metadata = await connection.GetStreamMetadataAsync(streamName);
            return metadata.StreamMetadata.TryGetValue(StreamVersionMetadataKey, out int version)
                ? version
                : default(int?);
        }

        private static IEnumerable<MigrationTypeInfo> GetAvailableMigrations()
        {
            return typeof(StreamMigrator).GetTypeInfo().Assembly.DefinedTypes
                .Where(t => t.GetInterface(nameof(IStreamMigration)) != null)
                .Select(t => new MigrationTypeInfo
                {
                    Type = t,
                    Properties = t.GetCustomAttribute<StreamMigrationAttribute>()
                })
                .Where(t =>
                    t.Properties != null && // type must have the [StreamMigration]-attribute
                    t.Properties.ToVersion > t.Properties.FromVersion);
        }

        private static async Task ExecuteMigrationAsync(MigrationTypeInfo migrationType, IEventStoreConnection connection, string streamName)
        {
            var migration = (IStreamMigration)Activator.CreateInstance(migrationType.Type.AsType());
            var args = new StreamMigrationArgs(connection, streamName);
            await migration.MigrateAsync(args);

            using (var transaction = await connection.StartTransactionAsync(streamName, ExpectedVersion.Any))
            {
                // Soft-delete the stream and recreate it by appending all new events
                await connection.DeleteStreamAsync(streamName, ExpectedVersion.Any, hardDelete: false);
                await connection.AppendToStreamAsync(streamName, ExpectedVersion.Any, args.EventsToAppend);

                // Write new version to the stream's metadata
                var metadata = await connection.GetStreamMetadataAsync(streamName);
                var updatedMetadata = metadata.StreamMetadata.Copy()
                    .SetCustomProperty(StreamVersionMetadataKey, migrationType.Properties.ToVersion);
                await connection.SetStreamMetadataAsync(streamName, ExpectedVersion.Any, updatedMetadata);

                await transaction.CommitAsync();
            }
        }

        class MigrationTypeInfo
        {
            public TypeInfo Type { get; set; }
            public StreamMigrationAttribute Properties { get; set; }
        }
    }
}
