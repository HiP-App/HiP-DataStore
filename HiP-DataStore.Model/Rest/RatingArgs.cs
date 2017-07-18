using System;
using System.ComponentModel.DataAnnotations;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class RatingArgs
    {
        [Required]
        [Range(1, 5, ErrorMessage = "Has to be in a range from 1 to 5")]
        public byte? Rating { get; set; }

        [Required]
        [Range(0, Int32.MaxValue, ErrorMessage = "Has to be a positive value")]
        public int? UserId { get; set; }
    }
}
