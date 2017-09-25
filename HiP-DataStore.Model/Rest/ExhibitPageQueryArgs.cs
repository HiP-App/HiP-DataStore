namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class ExhibitPageQueryArgs : QueryArgs
    {
        /// <summary>
        /// If not null, only pages of the specified type are returned.
        /// </summary>
        public PageType? Type { get; set; }
    }
}
