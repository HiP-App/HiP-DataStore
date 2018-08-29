using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.EventSourcing.Events;
using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel
{
    public class HighScoreIndex : IDomainIndex
    {
        //"key" is the user id, and "value" is the HighScoreEntityInfo class
        private readonly Dictionary<string, HighScoreEntityInfo> _highScoresDict = new Dictionary<string, HighScoreEntityInfo>();
        private readonly object _lockObject = new object();

        /// <summary>
        /// Checks the in-memory index to detect whether there is already a recoreded highscore for the specified exhibit and user
        /// </summary>
        /// <param name="exhibitId">Exhibit ID</param>
        /// <param name="userId">User's ID</param>
        /// <returns>The entity Id of the highscore for the specified exhibit and user; null otherwise</returns>
        public int? CheckHighScoreExistence(int exhibitId, string userId)
        {
            lock (_lockObject)
            {
                if (_highScoresDict.TryGetValue(userId, out var value))
                {
                    if (value.ExhibitIds.Contains(exhibitId))
                    {
                        int index = value.ExhibitIds.IndexOf(exhibitId);
                        return value.EntityIds[index];
                    }
                }
                return null;
            }
        }

        /// <summary>
        ///This implemented interface method is used to populate the in-memory highscore index to be used for validiation purposes when adding a highscore for specified exhibit and user
        /// </summary>
        /// <param name="e">Generic Event</param>
        public void ApplyEvent(IEvent e)
        {
            switch (e)
            {
                case PropertyChangedEvent ev:
                    if (ev.GetEntityType() == ResourceTypes.Highscore)
                    {
                        lock (_lockObject)
                        {
                            if (ev.PropertyName == nameof(ExhibitHighScoreArgs.ExhibitId) && ev.Value is int exhibitId)
                            {
                                HighScoreEntityInfo value;
                                if (_highScoresDict.ContainsKey(ev.UserId))
                                {
                                    //update the entry in the dictionary
                                    if (_highScoresDict.TryGetValue(ev.UserId, out value))
                                    {
                                        value.ExhibitIds.Add(exhibitId);
                                        value.EntityIds.Add(ev.Id);
                                        _highScoresDict[ev.UserId] = value;
                                    }
                                }
                                else
                                {
                                    //create new pair where the key is the UserId
                                    value = new HighScoreEntityInfo();
                                    value.ExhibitIds.Add(exhibitId);
                                    value.EntityIds.Add(ev.Id);
                                    _highScoresDict.Add(ev.UserId, value);
                                }
                            }
                        }
                    }
                    break;
            }
        }

        private class HighScoreEntityInfo
        {
            public List<int> ExhibitIds { get; } = new List<int>();
            public List<int> EntityIds { get; } = new List<int>();
        }
    }
}
