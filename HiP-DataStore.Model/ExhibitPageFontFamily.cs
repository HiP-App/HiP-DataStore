using System;
using System.Collections.Generic;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model
{
    public static class ExhibitPageFontFamily
    {
        public const string Default = "DEFAULT";

        public static readonly IReadOnlyCollection<string> All = new[] { Default, "AlteSchwabacher" };

        public static bool IsValid(string fontFamily) =>
            fontFamily != null && All.Contains(fontFamily, StringComparer.OrdinalIgnoreCase);
    }
}
