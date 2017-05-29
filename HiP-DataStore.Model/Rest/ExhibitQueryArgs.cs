using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class ExhibitQueryArgs : QueryArgs
    {
        public IList<int> OnlyRoute { get; set; }
    }
}
