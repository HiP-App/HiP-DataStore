using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class ExhibitQueryArgs : QueryArgs
    {
        [JsonProperty("onlyRoute")]
        public IList<string> RouteIds { get; set; }

        [RegularExpression("^(id|name|timestamp)$")]
        public override string OrderBy
        {
            get => base.OrderBy;
            set => base.OrderBy = value;
        }
    }
}
