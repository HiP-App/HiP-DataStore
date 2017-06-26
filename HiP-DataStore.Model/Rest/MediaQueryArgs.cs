using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class MediaQueryArgs : QueryArgs
    {
        /// <summary>
        /// If null, all objects are returned. If set to true (resp. false), only the objects referenced
        /// (resp. not referenced) by another object are returned.
        /// </summary>
        public bool? Used { get; set; }

        public MediaType? Type { get; set; }
    }
}
