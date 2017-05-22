using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
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
            lock(_lockObject)
               return _tag.Any(x => x.Value.Title == title);
        }

        public void ApplyEvent(IEvent e)
        {
            switch (e)
            {
                case TagCreated ev:
                    lock (_lockObject)
                        _tag.Add(ev.Id, new TagInfo() { Title = ev.Properties.Title });
                    break;
                case TagUpdated ev:
                    lock (_lockObject)
                        if (_tag.TryGetValue(ev.Id, out var tagInfo))
                            tagInfo.Title = ev.Properties.Title;
                    break;
                case TagDeleted ev:
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
