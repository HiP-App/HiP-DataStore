using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    /// <summary>
    /// Specifies the name of the MongoDB collection that stores the entities of the annotated type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class MongoCollectionNameAttribute : Attribute
    {
        public string CollectionName { get; }

        public MongoCollectionNameAttribute(string collectionName)
        {
            if (string.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException($"collectionName is null or whitespace", nameof(collectionName));

            CollectionName = collectionName;
        }
    }

    public static class CollectionNameAttributeExtensions
    {
        /// <summary>
        /// Gets the name of the MongoDB collection that stores the entities of the annotated type.
        /// If the type is annotated with a <see cref="MongoCollectionNameAttribute"/>, the name specified
        /// in the attribute is returned. Otherwise, the type name is returned.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetMongoCollectionName(this Type type)
        {
            var attr = type.GetTypeInfo().GetCustomAttribute<MongoCollectionNameAttribute>();
            return attr?.CollectionName ?? type.Name;
        }
    }
}
