using System.Collections.Generic;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.EventSourcing.Events;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel
{
    /// <summary>
    /// Caches the list of media elements (images, audio) and their publication status.
    /// </summary>
    public class MediaIndex : IDomainIndex
    {
        private readonly Dictionary<int, MediaInfo> _media = new Dictionary<int, MediaInfo>();
        private readonly object _lockObject = new object();

        public MediaType? GetMediaType(int id)
        {
            lock (_lockObject)
            {
                if (_media.TryGetValue(id, out var mediaInfo))
                    return mediaInfo.Type;
                return null;
            }
        }
        public string GetFilePath(int id)
        {
            lock (_lockObject)
            {
                if (_media.TryGetValue(id, out var mediaInfo))
                    return mediaInfo.FilePath;
                return null;
            }
        }

        public bool IsImage(int id)
        {
            lock (_lockObject)
            {
                return _media.TryGetValue(id, out var info) && info.Type == MediaType.Image;
            }
        }

        public bool IsAudio(int id)
        {
            lock (_lockObject)
            {
                return _media.TryGetValue(id, out var info) && info.Type == MediaType.Audio;
            }
        }

        public bool ContainsId(int id)
        {
            lock (_lockObject)
                return _media.ContainsKey(id);
        }

        public void ApplyEvent(IEvent e)
        {
            switch (e)
            {
                case CreatedEvent ev when ev.GetEntityType() == ResourceTypes.Media:

                    lock (_lockObject)
                        _media.Add(ev.Id, new MediaInfo());

                    break;

                case DeletedEvent ev when ev.GetEntityType() == ResourceTypes.Media:

                    lock (_lockObject)
                        _media.Remove(ev.Id);

                    break;

                case PropertyChangedEvent ev when ev.GetEntityType() == ResourceTypes.Media:
                    if (ev.PropertyName == nameof(MediaArgs.Status))
                    {
                        lock (_lockObject)
                            _media[ev.Id].Status = (ContentStatus)ev.Value;
                    }
                    else if (ev.PropertyName == nameof(MediaArgs.Type))
                    {
                        lock (_lockObject)
                            _media[ev.Id].Type = (MediaType)ev.Value;
                    }

                    break;

                case MediaFileUpdated ev:
                    lock (_lockObject)
                        _media[ev.Id].FilePath = ev.File;
                    break;
            }
        }

        public class MediaInfo
        {
            public ContentStatus Status { get; set; }
            public MediaType Type { get; set; }
            public string FilePath { get; set; }
        }
    }
}
