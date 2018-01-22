using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.EventSourcing;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    public class ExhibitQuizCreated : UserActivityBaseEvent, ICreateEvent
    {
        public int ExhibitId { get; set; }

        public ExhibitQuizArgs Properties { get; set; }

        public override ResourceType GetEntityType() => ResourceTypes.Quiz;

        public ContentStatus GetStatus() => Properties.Status;
    }
}
