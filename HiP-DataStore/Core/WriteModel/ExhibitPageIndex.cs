using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel
{
    public class ExhibitPageIndex : IDomainIndex
    {
        private readonly Dictionary<int, PageType> _pageType = new Dictionary<int, PageType>();

        public PageType? PageType(int id) => _pageType.TryGetValue(id, out var t) ? t : default(PageType?);

        public void ApplyEvent(IEvent e)
        {
            switch (e)
            {
                case ExhibitPageCreated ev: _pageType[ev.Id] = ev.Properties.Type; break;
                case ExhibitPageDeleted ev: _pageType.Remove(ev.Id); break;
            }
        }
    }
}
