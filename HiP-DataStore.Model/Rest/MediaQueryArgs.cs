using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
   public class MediaQueryArgs : QueryArgs
    {
        public bool? Used { get; set; }
        
       
        public MediaType? Type { get; set; }
    }
}
