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

        public bool IsPublishedImage(int id)
        {
            return _media.TryGetValue(id, out var info) &&
                info.Status == ContentStatus.Published &&
                info.Type == MediaType.Image;
        }

        public bool IsPublishedAudio(int id)
        {
            return _media.TryGetValue(id, out var info) &&
                info.Status == ContentStatus.Published &&
                info.Type == MediaType.Audio;
        }
       
        public bool IsImage(int id)
        {
            return (_media.ContainsKey(id) && _media[id].Type == MediaType.Image) ? true : false;
        }

        public bool IsAudio(int id)
        {
            return (_media.ContainsKey(id) && _media[id].Type == MediaType.Audio) ? true : false;
        }

        public bool ContainsId(int id)
        {
            return _media.ContainsKey(id);
        }

        public void ApplyEvent(IEvent e)
        {
            switch (e)
            {
                case MediaCreated ev:
                    _media.Add(ev.Id, new MediaInfo { Status = ev.Status, Type = ev.Properties.Type });
                    break;

                case MediaDeleted ev:
                    _media.Remove(ev.Id);
                    break;

                case MediaUpdate ev:
                    _media[ev.Id].Status = ev.Status;
                    break;
            }
        }

        public class MediaInfo
        {
            public ContentStatus Status { get; set; }
            public MediaType Type { get; set; }
        }
    }
}
