using Newtonsoft.Json;
using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class ExhibitQueryArgs : QueryArgs
    {
        [JsonProperty("onlyRoute")]
        public IList<string> RouteIds { get; set; }
    }
}
