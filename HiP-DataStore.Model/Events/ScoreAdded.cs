using PaderbornUniversity.SILab.Hip.EventSourcing;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class ScoreAdded : UserActivityBaseEvent
    {
        public int Score { get; set; }

        public override ResourceType GetEntityType() => ResourceTypes.ScoreRecord;
    }
}
