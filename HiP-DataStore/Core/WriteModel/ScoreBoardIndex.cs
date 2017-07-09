using System;
using System.Collections.Generic;
using System.Linq;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel
{
    public class ScoreBoardIndex : IDomainIndex
    {
        private readonly ScoreBoard _board = new ScoreBoard();
        private readonly Object _lockObject = new object();

        private int MaximumId=0;
        public int NewId()
        {
            return ++MaximumId;
        }

        //All records of ScoreBoard (Sorted)
        public IReadOnlyCollection<ScoreRecord> AllRecords()
        {
            return _board.ToList();
        }
        public bool Exists(int userId)
        {
            return _board.Any(x => x.UserId == userId);
        }


        public void ApplyEvent(IEvent e)
        {
            switch (e)
            {
                case ScoreAdded ev:
                    lock (_lockObject)
                    {
                        MaximumId = Math.Max(MaximumId, ev.Id);
                        _board.RemoveWhere(x => x.UserId == ev.UserId);
                        _board.Add(new ScoreRecord() { Id=ev.Id, UserId= ev.UserId, Score = ev.Score , Timestamp = ev.Timestamp });
                    }
                    break;
            }
        }
    }
}
