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
