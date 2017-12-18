using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Utility
{
    public class UploadFilesConfig
    {
        /// <summary>
        /// Path to the directory where media files (images, audio) are stored.
        /// Default value: "Media"
        /// </summary>
        public string Path { get; set; } = "Media";

        /// <summary>
        /// A list of supported file extensions (without leading dot) for each <see cref="MediaType"/>.
        /// </summary>
        public Dictionary<string, List<string>> SupportedFormats { get; set; }
    }
}
