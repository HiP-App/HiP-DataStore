using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class ExhibitQueryArgs : QueryArgs
    {
        public IList<int> OnlyRoutes { get; set; }

        public float? Latitude { get; set; }

        public float? Longitude { get; set; }
    }
}
