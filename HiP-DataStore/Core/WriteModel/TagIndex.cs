using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.EventSourcing.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel
{
    public class TagIndex : IDomainIndex
    {
        private readonly Dictionary<int, TagInfo> _tag = new Dictionary<int, TagInfo>();
        private readonly Object _lockObject = new object();

        public bool IsTitleExist(string title)
        {
            lock (_lockObject)
                return _tag.Any(x => x.Value.Title == title);
        }

        /// <summary>
        /// Gets the ID of the tag having the specified title, or null if no tag with that title exists.
        /// </summary>
        public int? GetIdByTagTitle(string title)
        {
            lock (_lockObject)
            {
                var matches = _tag.Where(x => x.Value.Title == title);
                return matches.Any() ? matches.First().Key : default(int?);
            }
        }

        public void ApplyEvent(IEvent e)
        {
            if (e is EventBase baseEvent && baseEvent.GetEntityType() == ResourceTypes.Tag)
                switch (e)
                {
                    case CreatedEvent ev:
                        lock (_lockObject)
                            _tag.Add(ev.Id, new TagInfo());
                        break;
                    case PropertyChangedEvent ev:
                        lock (_lockObject)
                            if (_tag.TryGetValue(ev.Id, out var tagInfo) && ev.PropertyName == nameof(TagArgs.Title))
                                tagInfo.Title = ev.Value.ToString();
                        break;
                    case DeletedEvent ev:
                        lock (_lockObject)
                            _tag.Remove(ev.Id);
                        break;
                }

        }
    }
    public class TagInfo
    {
        public string Title { get; set; }
    }
}
