using System;
using PaderbornUniversity.SILab.Hip.EventSourcing;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class HighscoreAdded : UserActivityBaseEvent
    {
        public int ExhibitId { get; set; }
        public double HighScore { get; set; }
        public override ResourceType GetEntityType() => ResourceTypes.Highscore;
    }
}
