using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.Migrations
{
    public interface IStreamMigrationArgs
    {
        IAsyncEnumerator<IEvent> GetExistingEvents();
        void AppendEvent(IEvent ev);
    }
}
