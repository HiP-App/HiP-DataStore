using MongoDB.Driver;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using System;
using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core
{
    public static class DocRefExtensions
    {
        /// <summary>
        /// Resolves a reference to a document.
        /// Both, database name and collection name of the referenced document, must be specified.
        /// </summary>
        public static T Load<T>(this DocRef<T> docRef, IMongoClient client)
        {
            if (string.IsNullOrEmpty(docRef.Database))
                throw new InvalidOperationException($"The DocRef does not specify the database of the referenced document. Try Load(IMongoDatabase)");

            return docRef.Load(client.GetDatabase(docRef.Database));
        }

        /// <summary>
        /// Resolves a reference to a document.
        /// Collection name of the referenced document must be specified, database name is ignored.
        /// </summary>
        public static T Load<T>(this DocRef<T> docRef, IMongoDatabase database)
        {
            if (string.IsNullOrEmpty(docRef.Collection))
                throw new InvalidOperationException($"The DocRef does not specify the collection of the referenced document. Try Load(IMongoCollection).");

            return docRef.Load(database.GetCollection<T>(docRef.Collection));
        }

        /// <summary>
        /// Resolves a reference to a document. Collection name and database name are ignored.
        /// </summary>
        public static T Load<T>(this DocRef<T> docRef, IMongoCollection<T> collection)
        {
            return collection
                .Find(Builders<T>.Filter.Eq("_id", docRef.Id))
                .SingleOrDefault();
        }

        /// <summary>
        /// Resolves a collection of document references.
        /// Both, database name and collection name of the referenced document, must be specified.
        /// </summary>
        public static IReadOnlyCollection<T> LoadAll<T>(this DocRefList<T> docRef, IMongoClient client)
        {
            if (string.IsNullOrEmpty(docRef.Database))
                throw new InvalidOperationException($"The DocRef does not specify the database of the referenced document. Try Load(IMongoDatabase)");

            return docRef.LoadAll(client.GetDatabase(docRef.Database));
        }

        /// <summary>
        /// Resolves a collection of document references.
        /// Collection name of the referenced documents must be specified, database name is ignored.
        /// </summary>
        public static IReadOnlyCollection<T> LoadAll<T>(this DocRefList<T> docRef, IMongoDatabase database)
        {
            if (string.IsNullOrEmpty(docRef.Collection))
                throw new InvalidOperationException($"The DocRef does not specify the collection of the referenced document. Try Load(IMongoCollection).");

            return docRef.LoadAll(database.GetCollection<T>(docRef.Collection));
        }

        /// <summary>
        /// Resolves a collection of document references. Collection name and database name are ignored.
        /// </summary>
        public static IReadOnlyCollection<T> LoadAll<T>(this DocRefList<T> docRef, IMongoCollection<T> collection)
        {
            return collection
                .Find(Builders<T>.Filter.In("_id", docRef))
                .ToList();
        }
    }
}
