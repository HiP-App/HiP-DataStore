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

        /// <summary>
        /// Name of the event stream to read from and write to.
        /// For example, you can use different streams for develop and production environments.
        /// </summary>
        public string EventStoreStream { get; set; }

        /// <summary>
        /// URL that points to the "swagger.json" file. If set, this URL is entered by default
        /// when accessing the Swagger UI page. If not set, we will try to construct the URL
        /// automatically which might result in an invalid URL.
        /// </summary>
        public string SwaggerEndpoint { get; set; }

        /// <summary>
        /// URL pattern for generating thumbnail URLs. Should contain a placeholder "{0}" that is replaced with the
        /// ID of the requested media at runtime. The endpoint should support GET and DELETE requests. Example:
        /// "https://docker-hip.cs.upb.de/develop/thumbnailservice/api/Thumbnails?Url=datastore/api/Media/{0}/File"
        /// </summary>
        /// <remarks>
        /// This property is optional: If no value is provided, no thumbnail URLs are generated - instead, direct URLs
        /// to the original image files are then returned.
        /// </remarks>
        public string ThumbnailUrlPattern { get; set; }
    }
}
