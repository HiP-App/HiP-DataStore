using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Utility
{
    public class UploadFilesConfig
    {
        public string Path { get; set; }

        public Dictionary<string, List<string>> SupportedFormats { get; set; }
    }
}
