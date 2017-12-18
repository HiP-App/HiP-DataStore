using System;
using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model
{
    /// <summary>
    /// This attribute can be used to mark a property as a reference to other entities. 
    /// This info can then used to build a list of references/referencers for an entity, which is important e.g. for the deletion of entities.
    /// Supported property types are <see cref="int"/>, <see cref="IEnumerable{Int32}"/> and <see cref="IEnumerable{IReference}"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class ReferenceAttribute : Attribute
    {
        public string ResourceTypeName { get; }

        /// <param name="resourceTypeName">Resource Type of the referenced entities</param>
        public ReferenceAttribute(string resourceTypeName)
        {
            ResourceTypeName = resourceTypeName;
        }
    }

    /// <summary>
    /// This interface can be used as a type for a reference instead of just <see cref="int"/>
    /// </summary>
    public interface IReference
    {
        int ReferenceId { get; }
    }

}