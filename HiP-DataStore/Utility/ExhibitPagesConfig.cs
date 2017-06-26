using System;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Utility
{
    public class ExhibitPagesConfig
    {
        /// <summary>
        /// The font families that are valid for exhibit pages.
        /// </summary>
        public string[] FontFamilies { get; set; }

        /// <summary>
        /// The default font family to be used if no font family is specified during creation or update
        /// of an exhibit page. Should be contained in <see cref="FontFamilies"/>.
        /// </summary>
        public string DefaultFontFamily { get; set; }

        public bool IsFontFamilyValid(string fontFamily) =>
            fontFamily != null && FontFamilies.Contains(fontFamily, StringComparer.OrdinalIgnoreCase);
    }
}
