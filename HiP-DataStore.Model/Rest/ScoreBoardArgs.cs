using System.ComponentModel.DataAnnotations;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class ScoreBoardArgs
    {
        [Range(1,int.MaxValue)]
        public int? Length { get; set; }
    }
}
