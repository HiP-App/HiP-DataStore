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

        public static bool TryGetReferenceType(this PropertyChangedEvent e, out ResourceType type)
        {
            type = null;
            var resourceType = e.GetEntityType();
            var propertyInfo = resourceType.Type.GetProperty(e.PropertyName);
            var hasAttribute = propertyInfo.CustomAttributes.Any(attr => attr.AttributeType == typeof(ReferenceAttribute));
            if (hasAttribute)
            {
                var referenceAttribute = propertyInfo.GetCustomAttribute<ReferenceAttribute>();
                var valueType = Type.GetType(e.ValueTypeName);
                type = ResourceType.ResourceTypeDictionary[referenceAttribute.ResourceTypeName];
                return true;
            }

            return false;
        }

        public static (IEnumerable<EntityId> addedReferences, IEnumerable<EntityId> removedReferences) DetermineReferences(this PropertyChangedEvent e, object oldValue)
        {
            var resourceType = e.GetEntityType();
            var propertyInfo = resourceType.Type.GetProperty(e.PropertyName);
            var hasAttribute = propertyInfo.CustomAttributes.Any(attr => attr.AttributeType == typeof(ReferenceAttribute));
            if (hasAttribute)
            {
                var referenceAttribute = propertyInfo.GetCustomAttribute<ReferenceAttribute>();
                var valueType = Type.GetType(e.ValueTypeName);
                var referenceResourceType = ResourceType.ResourceTypeDictionary[referenceAttribute.ResourceTypeName];

                if (!Equals(oldValue, e.Value))
                {
                    if (oldValue == null)
                    {
                        return (GetEntityIdsFromObject(e.Value, referenceResourceType), new List<EntityId>());
                    }
                    else if (e.Value == null)
                    {
                        return (new List<EntityId>(), GetEntityIdsFromObject(oldValue, referenceResourceType));
                    }
                    else if (typeof(IEnumerable).IsAssignableFrom(valueType))
                    {
                        if (typeof(IEnumerable<int>).IsAssignableFrom(valueType))
                        {
                            var enumValue = (IEnumerable<int>)oldValue;
                            var eventValue = (IEnumerable<int>)e.Value;
                            return (eventValue.Except(enumValue).Select(r => new EntityId(referenceResourceType, r)), enumValue.Except(eventValue).Select(r => new EntityId(referenceResourceType, r)));
                        }
                        else if (typeof(IEnumerable<IReference>).IsAssignableFrom(valueType))
                        {
                            var enumValue = (IEnumerable<IReference>)oldValue;
                            var eventValue = (IEnumerable<IReference>)e.Value;
                            return (eventValue.Except(enumValue).Select(r => new EntityId(referenceResourceType, r.ReferenceId)), enumValue.Except(eventValue).Select(r => new EntityId(referenceResourceType, r.ReferenceId)));
                        }
                    }
                    else if (typeof(int).IsAssignableFrom(valueType))
                    {
                        return (new[] { new EntityId(referenceResourceType, (int)e.Value) }, new[] { new EntityId(referenceResourceType, (int)oldValue) });
                    }
                    else throw new NotSupportedException("No supported type as an identifier for a reference could be found");

                }
            }

            return (new List<EntityId>(), new List<EntityId>());
        }

        private static IEnumerable<EntityId> GetEntityIdsFromObject(object obj, ResourceType resourceType)
        {
            var valueType = obj.GetType();
            if (typeof(IEnumerable).IsAssignableFrom(valueType))
            {
                if (typeof(IEnumerable<int>).IsAssignableFrom(valueType))
                {
                    var enumValue = (IEnumerable<int>)obj;
                    return enumValue.Select(r => new EntityId(resourceType, r));
                }
                else if (typeof(IEnumerable<IReference>).IsAssignableFrom(valueType))
                {
                    var enumValue = (IEnumerable<IReference>)obj;
                    return enumValue.Select(r => new EntityId(resourceType, r.ReferenceId));
                }
            }
            else if (typeof(int).IsAssignableFrom(valueType))
            {
                return new[] { new EntityId(resourceType, (int)obj) };
            }
            else throw new NotSupportedException("No supported type as an identifier for a reference could be found");
            
            return new List<EntityId>();
        }
    }
}
