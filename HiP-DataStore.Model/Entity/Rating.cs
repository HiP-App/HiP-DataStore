using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class Rating
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int EntityId { get; set; }

        public ResourceType RatedType { get; set; }

        public byte Value { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public Rating() { }

        public Rating(RatingAdded e)
        {
            Id = e.Id;
            UserId = e.UserId;
            EntityId = e.EntityId;
            RatedType = e.RatedType;
            Value = e.Value;
            Timestamp = e.Timestamp;            
        }
    }
}
