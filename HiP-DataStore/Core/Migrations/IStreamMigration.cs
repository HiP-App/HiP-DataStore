using System.Threading.Tasks;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.Migrations
{
    /// <summary>
    /// The interface for Event Store stream migrations.
    /// </summary>
    /// <remarks>
    /// Migrations are applied using the <see cref="StreamMigrator"/> class. In order for <see cref="StreamMigrator"/>
    /// to recognize a migration class, the class must implement <see cref="IStreamMigration"/> and must be annotated
    /// with the <see cref="StreamMigrationAttribute"/>.
    /// </remarks>
    public interface IStreamMigration
    {
        Task MigrateAsync(IStreamMigrationArgs e);
    }
}
