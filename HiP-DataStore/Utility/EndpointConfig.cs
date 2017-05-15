namespace PaderbornUniversity.SILab.Hip.DataStore.Utility
{
    public class EndpointConfig
    {
        /// <summary>
        /// Connection string for the Mongo DB cache database.
        /// Example: "mongodb://localhost:27017"
        /// </summary>
        public string MongoDbHost { get; set; }

        /// <summary>
        /// Name of the database to use.
        /// Example: "main"
        /// </summary>
        public string MongoDbName { get; set; }

        /// <summary>
        /// Endpoint of the Event Store.
        /// 
        /// Examples:
        /// "tcp://localhost:1113",
        /// "tcp://user:password@myserver:11234",
        /// "discover://user:password@myserver:1234"
        /// 
        /// See also: http://docs.geteventstore.com/dotnet-api/4.0.0/connecting-to-a-server/
        /// </summary>
        public string EventStoreHost { get; set; }
    }
}
