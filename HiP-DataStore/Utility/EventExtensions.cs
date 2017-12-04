using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.EventSourcing.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PaderbornUniversity.SILab.Hip.DataStore.Utility
{
    public static class EventExtensions
    {
        public static IEnumerable<EntityId> GetReferences(this PropertyChangedEvent e)
        {
            var resourceType = e.GetEntityType();
            var propertyInfo = resourceType.Type.GetProperty(e.PropertyName);
            var hasAttribute = propertyInfo.CustomAttributes.Any(attr => attr.AttributeType == typeof(ReferenceAttribute));
            if (hasAttribute)
            {
                var referenceAttribute = propertyInfo.GetCustomAttribute<ReferenceAttribute>();
                var valueType = Type.GetType(e.ValueTypeName);
                var referenceResourceType = ResourceType.ResourceTypeDictionary[referenceAttribute.ResourceTypeName];
                if (typeof(IEnumerable).IsAssignableFrom(valueType))
                {
                    if (typeof(IEnumerable<int>).IsAssignableFrom(valueType))
                    {
                        return ((IEnumerable<int>)e.Value).Select(v => new EntityId(referenceResourceType, v));
                    }
                    else if (typeof(IEnumerable<IReference>).IsAssignableFrom(valueType))
                    {
                        return ((IEnumerable<IReference>)e.Value).Select(v => new EntityId(referenceResourceType, v.ReferenceId));
                    }
                    else throw new NotSupportedException("No supported type as an identifier for a reference could be found");
                }
                else if (e.Value != null)
                {
                    return new[] { new EntityId(referenceResourceType, (int)e.Value) };
                }
            }

            return new List<EntityId>();
        }

    }
}
