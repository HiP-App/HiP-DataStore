using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using System;
using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel
{
    public class ExhibitPageIndex : IDomainIndex
    {
        private readonly Dictionary<int, List<int>> _exhibitPages = new Dictionary<int, List<int>>();
        private readonly Dictionary<int, PageInfo> _pageType = new Dictionary<int, PageInfo>();

        /// <summary>
        /// Gets the type of the page with the specified ID.
        /// </summary>
        public PageType? PageType(int pageId) => _pageType.TryGetValue(pageId, out var t) ? t.Type : default(PageType?);

        /// <summary>
        /// Gets the ID of the exhibit the page with the specified ID belong to.
        /// </summary>
        public int? ExhibitId(int pageId) => _pageType.TryGetValue(pageId, out var t) ? t.ExhibitId : default(int?);

        /// <summary>
        /// Gets the ordered list of page IDs for a specific exhibit.
        /// </summary>
        public IReadOnlyList<int> PageIds(int exhibitId) => GetPageListForExhibitOrNull(exhibitId)?.ToArray() ?? Array.Empty<int>();

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

                    GetOrCreatePageListForExhibit(ev.ExhibitId).Add(ev.Id);
                    break;

                case ExhibitPageDeleted ev:
                    _pageType.Remove(ev.Id);
                    GetPageListForExhibitOrNull(ev.ExhibitId)?.Remove(ev.Id);
                    break;

                case ExhibitUpdated ev:
                    _exhibitPages[ev.Id] = ev.Properties.Pages ?? new List<int>();
                    break;

                case ExhibitDeleted ev:
                    _exhibitPages.Remove(ev.Id);
                    break;
            }
        }

        private List<int> GetPageListForExhibitOrNull(int exhibitId) =>
            _exhibitPages.TryGetValue(exhibitId, out var list) ? list : null;

        private List<int> GetOrCreatePageListForExhibit(int exhibitId)
        {
            return _exhibitPages.TryGetValue(exhibitId, out var list)
                ? list
                : (_exhibitPages[exhibitId] = new List<int>());
        }

        public class PageInfo
        {
            public PageType Type { get; set; }
            public int ExhibitId { get; set; }
        }
    }
}
