using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class ReferenceAttribute : Attribute
    {

        public string ResourceTypeName { get; }

        public ReferenceAttribute(string resourceTypeName)
        {
            ResourceTypeName = resourceTypeName;
        }
    }

    public interface IReference
    {
        int ReferenceId { get; }
    }

}