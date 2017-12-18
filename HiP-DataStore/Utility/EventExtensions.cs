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
            if (!hasAttribute)
                return Enumerable.Empty<EntityId>();

            var referenceAttribute = propertyInfo.GetCustomAttribute<ReferenceAttribute>();
            var referenceResourceType = ResourceType.ResourceTypeDictionary[referenceAttribute.ResourceTypeName];
            return GetEntityIdsFromObject(e.Value, referenceResourceType);
        }

        /// <summary>
        /// Returns the added and removed references by comparing the references from <paramref name="oldValue"/> and the value of <paramref name="e"/>.
        /// </summary>
        public static (IEnumerable<EntityId> addedReferences, IEnumerable<EntityId> removedReferences) GetReferenceDifferences(this PropertyChangedEvent e, object oldValue)
        {
            var resourceType = e.GetEntityType();
            var propertyInfo = resourceType.Type.GetProperty(e.PropertyName);
            var hasAttribute = propertyInfo.CustomAttributes.Any(attr => attr.AttributeType == typeof(ReferenceAttribute));
            if (hasAttribute)
            {
                var referenceAttribute = propertyInfo.GetCustomAttribute<ReferenceAttribute>();
                var referenceResourceType = ResourceType.ResourceTypeDictionary[referenceAttribute.ResourceTypeName];

                if (!Equals(oldValue, e.Value))
                {
                    if (oldValue == null || e.Value == null)
                        return (GetEntityIdsFromObject(e.Value, referenceResourceType), GetEntityIdsFromObject(oldValue, referenceResourceType));

                    switch (oldValue)
                    {
                        case IEnumerable<int> oldIds:
                            var newIds = (IEnumerable<int>)e.Value;
                            return (newIds.Except(oldIds).Select(r => new EntityId(referenceResourceType, r)), oldIds.Except(newIds).Select(r => new EntityId(referenceResourceType, r)));

                        case IEnumerable<IReference> oldList:
                            var newList = (IEnumerable<IReference>)e.Value;
                            return (newList.Except(oldList).Select(r => new EntityId(referenceResourceType, r.ReferenceId)), oldList.Except(newList).Select(r => new EntityId(referenceResourceType, r.ReferenceId)));

                        case int i:
                            return (new[] { new EntityId(referenceResourceType, (int)e.Value) }, new[] { new EntityId(referenceResourceType, i) });

                        case IReference r:
                            return (new[] { new EntityId(referenceResourceType, ((IReference)e.Value).ReferenceId) }, new[] { new EntityId(referenceResourceType, r.ReferenceId) });

                        default:
                            throw new NotSupportedException("No supported type as an identifier for a reference could be found");

                    }
                }
            }

            return (new List<EntityId>(), new List<EntityId>());
        }

        private static IEnumerable<EntityId> GetEntityIdsFromObject(object obj, ResourceType resourceType)
        {
            if (obj == null) return Enumerable.Empty<EntityId>();

            switch (obj)
            {
                case IEnumerable<int> e:
                    return e.Select(r => new EntityId(resourceType, r));

                case IEnumerable<IReference> e:
                    return e.Select(r => new EntityId(resourceType, r.ReferenceId));

                case int i:
                    return new[] { new EntityId(resourceType, i) };

                case IReference r:
                    return new[] { new EntityId(resourceType, r.ReferenceId) };

                default:
                    throw new NotSupportedException("No supported type as an identifier for a reference could be found");
            }
        }
    }
}
