using System;
using System.Collections.Generic;
using System.Text;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.EventArgs
{
    public class ExhibitTransferArgs
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public int? Image { get; set; }

        public float Latitude { get; set; }

        public float Longitude { get; set; }

        public ContentStatus Status { get; set; }

        public List<int> Tags { get; set; }

        public List<int> Pages { get; set; }

        /// <summary>
        /// The radius (in km) in which the exhibit can be accessed.
        /// </summary>
        public float AccessRadius { get; set; }

        public List<int> Questions { get; set; }
    }
}
