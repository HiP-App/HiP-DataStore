using System.Collections.Generic;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel
{
    /// <summary>
    /// Caches the list of media elements (images, audio) and their publication status.
    /// </summary>
    public class MediaIndex : IDomainIndex
    {
        private readonly Dictionary<string, ContentStatus> _media = new Dictionary<string, ContentStatus>();

        public ContentStatus? GetMediaStatus(string id)
        {
            return _media.TryGetValue(id, out var status) ? status : default(ContentStatus?);
        }

        public void ApplyEvent(IEvent e)
        {
            // TODO: Basically handle MediaCreated/-Removed events
        }
    }
}
