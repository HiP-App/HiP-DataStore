using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    /// <summary>
    /// Interface for arguments that are provided via REST
    /// </summary>
    public interface IContentArgs
    {
        ContentStatus Status { get; }
    }
}
