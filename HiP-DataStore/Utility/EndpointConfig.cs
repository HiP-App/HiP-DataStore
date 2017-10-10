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
        /// URL that points to the "swagger.json" file. If set, this URL is entered by default
        /// when accessing the Swagger UI page. If not set, we will try to construct the URL
        /// automatically which might result in an invalid URL.
        /// </summary>
        public string SwaggerEndpoint { get; set; }
    }
}
