using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.EventSourcing.Events;
using System.Collections.Generic;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel
{
    public class QuizIndex : IDomainIndex
    {
        /// <summary>
        /// Key: ExhibitId Value:QuizId
        /// </summary>
        private readonly Dictionary<int, int> _exhibitQuizDict = new Dictionary<int, int>();
        private readonly object _lockObject = new object();

        /// <summary>
        /// Get Quiz id From Exhit id
        /// </summary>
        /// <returns></returns>
        public int? GetQuizId(int exhibitId)
        {
            lock (_lockObject)
            {
                if (_exhibitQuizDict.TryGetValue(exhibitId, out var quizId))
                    return quizId;
                return null;
            }
        }

        public void ApplyEvent(IEvent e)
        {
            switch (e)
            {
                case DeletedEvent ev when ev.GetEntityType() == ResourceTypes.Quiz:
                    int exhibitId=-1;
                    lock (_lockObject)
                    {
                        _exhibitQuizDict.ToList().ForEach(x => { if (x.Value == ev.Id) exhibitId = x.Key; });
                        if (exhibitId != -1)
                            _exhibitQuizDict.Remove(exhibitId);
                    }
                    break;

                case PropertyChangedEvent ev when ev.GetEntityType() == ResourceTypes.Quiz:
                    if (ev.PropertyName == nameof(ExhibitQuizArgs.ExhibitId))
                    {
                        lock (_lockObject)
                        {
                            var oldExhibitId = -1;
                            _exhibitQuizDict.ToList().ForEach(x => { if (x.Value == ev.Id) oldExhibitId = x.Key; });
                            if (oldExhibitId != -1)
                                _exhibitQuizDict.Remove(oldExhibitId);
                            _exhibitQuizDict[(int)ev.Value] = ev.Id;
                        }
                    }
                    break;
            }
        }
    }
}
