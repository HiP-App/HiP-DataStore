using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PaderbornUniversity.SILab.Hip.DataStore.Utility
{
    public class UploadFilesConfig
    {
        public string Path { get; }


        public Dictionary<string, List<string>> Formats { get; }

        public UploadFilesConfig(IConfiguration config)
        {
            Path = config.GetValue<string>("Path");
            Formats = new Dictionary<string, List<string>>();

            var extensions = config.GetSection("SupportedFormats");

            foreach(var format in extensions.GetChildren())
                Formats.Add(format.Key, (from c in format.GetChildren() select c.Value).ToList());
            
        }
    }
}
