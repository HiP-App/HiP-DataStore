using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.EventSourcing.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel
{
    public class HighScoreIndex : IDomainIndex
    {
        private readonly Dictionary<string, HighScoreEntityInfo> _highScoresDict = new Dictionary<string, HighScoreEntityInfo>();       //"key" is the user id, and "value" is the highscore class
        private readonly object _lockObject = new object();

        private int _maximumId = -1;

        public int NextId()
        {
            lock (_lockObject)
            {
                return ++_maximumId;
            }
        }

        public void ApplyEvent(IEvent e)
        {
            switch (e)
            {
                case CreatedEvent ev:
                    if (ev.GetEntityType() == ResourceTypes.Highscore)
                    {
                        lock (_lockObject)
                        {
                            _maximumId = Math.Max(_maximumId, ev.Id);
                            
                        }
                    }
                    break;

                case PropertyChangedEvent ev:
                    if (ev.GetEntityType() == ResourceTypes.Highscore)
                    {
                        lock (_lockObject)
                        {

                        }
                    }
                    break;

                case DeletedEvent ev:
                    if (ev.GetEntityType() == ResourceTypes.Highscore)
                    {
                        lock (_lockObject)
                        {

                        }
                    }
                    break;
            }
        }

        private class HighScoreEntityInfo
        {
            public List<int> ExhibitId { get; } = new List<int>();
            public List<double> HighScore { get; } = new List<double>();
        }
    }
}
