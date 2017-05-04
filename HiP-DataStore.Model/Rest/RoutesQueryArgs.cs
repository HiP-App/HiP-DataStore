using System.ComponentModel.DataAnnotations;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class RoutesQueryArgs : QueryArgs
    {
        [RegularExpression("^(id|title|timestamp)$")]
        public override string OrderBy
        {
            get => base.OrderBy;
            set => base.OrderBy = value;
        }
    }
}
