using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel
{
    public class ScoreBoardIndex : IDomainIndex
    {
        private readonly ScoreBoard _board = new ScoreBoard();
        private readonly object _lockObject = new object();

        private int _maximumId;

        public int NewId()
        {
            lock (_lockObject)
                return ++_maximumId;
        }

        //All records of ScoreBoard (Sorted from min to max)
        public IReadOnlyCollection<ScoreRecord> AllRecords()
        {
            lock (_lockObject)
                return _board.ToList();
        }

        public bool Exists(int userId)
        {
            lock (_lockObject)
                return _board.Any(x => x.UserId == userId);
        }

        public void ApplyEvent(IEvent e)
        {
            switch (e)
            {
                case ScoreAdded ev:
                    lock (_lockObject)
                    {
                        _maximumId = Math.Max(_maximumId, ev.Id);
                        _board.RemoveWhere(x => x.UserId == ev.UserId);
                        _board.Add(new ScoreRecord
                        {
                            Id = ev.Id,
                            UserId = ev.UserId,
                            Score = ev.Score,
                            Timestamp = ev.Timestamp
                        });
                    }
                    break;
            }
        }
    }
}
