using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class Rating
    {
        public int Id;

        public int UserId;

        public int EntityId;

        public ResourceType RatedType;

        public int Value;

        public DateTimeOffset Timestamp;

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
