using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    /// <summary>
    /// Model for updating exhibits.
    /// </summary>
    public class ExhibitUpdateArgs : ExhibitArgs
    {
        public List<int> Pages { get; set; }
    }
}
