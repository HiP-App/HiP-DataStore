using System.Collections.Generic;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel
{
    /// <summary>
    /// Caches the list of media elements (images, audio) and their publication status.
    /// </summary>
    public class MediaIndex : IDomainIndex
    {
        private readonly Dictionary<int, MediaInfo> _media = new Dictionary<int, MediaInfo>();
        private readonly object _lockObject = new object();

        public bool IsPublishedImage(int id)
        {
            lock (_lockObject)
            {
                return _media.TryGetValue(id, out var info) &&
                info.Status == ContentStatus.Published &&
                info.Type == MediaType.Image;
            }
        }

        public bool IsPublishedAudio(int id)
        {
            lock (_lockObject)
            {
                return _media.TryGetValue(id, out var info) &&
                info.Status == ContentStatus.Published &&
                info.Type == MediaType.Audio;
            }
        }
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
                return _media.ContainsKey(id) && _media[id].Type == MediaType.Image;
            }
        }

        public bool IsAudio(int id)
        {
            lock (_lockObject)
            {
                return _media.ContainsKey(id) && _media[id].Type == MediaType.Audio;
            }
        }

        public bool ContainsId(int id)
        {
            lock (_lockObject)
                return  _media.ContainsKey(id); 
        }

        public void ApplyEvent(IEvent e)
        {
            switch (e)
            {
                case MediaCreated ev:
                    lock (_lockObject)
                        _media.Add(ev.Id, new MediaInfo { Status = ev.GetStatus(), Type = ev.Properties.Type }); 
                    break;

                case MediaDeleted ev:
                    lock (_lockObject)
                        _media.Remove(ev.Id); 
                    break;

                case MediaUpdate ev:
                    lock (_lockObject)
                        _media[ev.Id].Status = ev.GetStatus(); 
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
