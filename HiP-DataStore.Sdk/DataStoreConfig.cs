namespace PaderbornUniversity.SILab.Hip.DataStore
{
    /// <summary>
    /// Configuration properties for clients using the DataStore SDK.
    /// </summary>
    public sealed class DataStoreConfig
    {
        /// <summary>
        /// URL pointing to a running instance of the DataStore service.
        /// Example: "https://docker-hip.cs.upb.de/develop/datastore"
        /// </summary>
        public string DataStoreHost { get; set; }
    }
}
