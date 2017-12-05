using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.EventSourcing.Events;
using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel
{
    public class ExhibitPageIndex : IDomainIndex
    {
        private readonly Dictionary<int, PageInfo> _pageType = new Dictionary<int, PageInfo>();

        /// <summary>
        /// Gets the type of the page with the specified ID.
        /// </summary>
        public PageType? PageType(int pageId) => _pageType.TryGetValue(pageId, out var t) ? t.Type : default(PageType?);

        public void ApplyEvent(IEvent e)
        {
            if (e is BaseEvent baseEvent && baseEvent.GetEntityType() == ResourceTypes.ExhibitPage)
                switch (e)
                {
                    case CreatedEvent ev:
                        _pageType[ev.Id] = new PageInfo();
                        break;

                    case PropertyChangedEvent ev:
                        if (ev.Value is PageType type)
                        {
                            _pageType[ev.Id].Type = type;
                        }
                        break;
                    case DeletedEvent ev:
                        _pageType.Remove(ev.Id);
                        break;
                }
        }

        public class PageInfo
        {
            public PageType Type { get; set; }
        }
    }
}
