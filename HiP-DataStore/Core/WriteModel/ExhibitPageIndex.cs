using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
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

        /// <summary>
        /// Gets the ID of the exhibit the page with the specified ID belong to.
        /// </summary>
        public int? ExhibitId(int pageId) => _pageType.TryGetValue(pageId, out var t) ? t.ExhibitId : default(int?);

        public void ApplyEvent(IEvent e)
        {
            switch (e)
            {
                case ExhibitPageCreated2 ev:
                    _pageType[ev.Id] = new PageInfo
                    {
                        Type = ev.Properties.Type,
                        ExhibitId = ev.ExhibitId
                    };
                    break;

                case ExhibitPageDeleted2 ev:
                    _pageType.Remove(ev.Id);
                    break;
            }
        }

        public class PageInfo
        {
            public PageType Type { get; set; }
            public int ExhibitId { get; set; }
        }
    }
}
