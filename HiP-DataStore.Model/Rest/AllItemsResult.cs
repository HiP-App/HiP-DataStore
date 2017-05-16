using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class AllItemsResult<T>
    {
        public int Total { get; set; }

        public List<T> Items { get; set; }
    }
}
