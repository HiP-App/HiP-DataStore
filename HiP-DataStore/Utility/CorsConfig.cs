using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PaderbornUniversity.SILab.Hip.DataStore.Utility
{
    public class CorsConfig
    {
        public Dictionary<string, CorsEnvironmentConfig> CORS { get; set; }
    }
    public class CorsEnvironmentConfig
    {
       public string[] Origins { get; set; }
       public string[] Headers { get; set; }
       public string[] Methods { get; set; }
       public string[] ExposedHeaders { get; set; }
    }
}
