using System;
using System.ComponentModel.DataAnnotations;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class RatingArgs
    {
        public const byte MinRateValue = 1;
        public const byte MaxRateValue = 5;

        [Required]
        [Range(MinRateValue, MaxRateValue, ErrorMessage = "Has to be in a range from 1 to 5")]
        public byte? Rating { get; set; }
   }
}
