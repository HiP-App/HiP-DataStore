using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.EventSourcing.Events;
using PaderbornUniversity.SILab.Hip.EventSourcing.Migrations;

namespace PaderbornUniversity.SILab.Hip.DataStore.Migrations
{
    [StreamMigration(from: 10, to: 11)]
    public class Migration11DefaultValues : IStreamMigration
    {
        private Dictionary<EntityId, List<PropertyInfo>> _dict = new Dictionary<EntityId, List<PropertyInfo>>();
        public async Task MigrateAsync(IStreamMigrationArgs e)
        {
            var events = e.GetExistingEvents();
            while (await events.MoveNextAsync())
            {
                var current = events.Current;
                ResourceType resourceType;

                //there are events in the event stream with ResourceType "Quiz" these need to ignored here
                try
                {
                    resourceType = (current as BaseEvent)?.GetEntityType();
                    if (resourceType == null) continue;
                }
                catch { continue; }
                switch (current)
                {
                    case CreatedEvent ev:
                        Type type = resourceType.Type;
                        var properties = type.GetProperties().Where(p => p.CanRead).ToList();
                        _dict.Add((resourceType, ev.Id), properties);
                        break;

                    case PropertyChangedEvent ev:
                        properties = _dict[(resourceType, ev.Id)];
                        var property = properties.FirstOrDefault(p => p.Name == ev.PropertyName);
                        if (property != null)
                        {
                            properties.Remove(property);
                        }
                        break;
                }
            }

            events = e.GetExistingEvents();
            while (await events.MoveNextAsync())
            {
                var current = events.Current;

                switch (current)
                {
                    case BaseEvent ev:
                        ResourceType resourceType;

                        //there are events in the event stream with ResourceType "Quiz" these need to be deleted since a new solution with ResourceType "QuizQuestion" has been implemented
                        try
                        {
                            resourceType = (current as BaseEvent)?.GetEntityType();
                            if (resourceType == null) continue;
                        }
                        catch { continue; }

                        if (ev is CreatedEvent)
                        {
                            var properties = _dict[(resourceType, ev.Id)];
                            e.AppendEvent(ev);
                            foreach (var prop in properties)
                            {
                                if (prop.PropertyType.IsValueType && !(prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)))
                                {
                                    var value = Activator.CreateInstance(prop.PropertyType);
                                    e.AppendEvent(new PropertyChangedEvent(prop.Name, ev.ResourceTypeName, ev.Id, ev.UserId, value));
                                }
                            }
                        }
                        else
                        {
                            e.AppendEvent(ev);
                        }
                        break;
                    default:
                        e.AppendEvent(current);
                        break;
                }
            }


        }
    }
}
