﻿using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel
{
    public class ScoreBoard : SortedSet<ScoreRecord>
    {
       public ScoreBoard() : base(new UsersScoreComparer()) { }
    }

    //Making sorting by Descending first by scores. Then by Timestamp
    class UsersScoreComparer : IComparer<ScoreRecord>
    {
        //From IComparer.Compare:
        //Comparing null with any type is allowed and does not generate an exception when using IComparable.
        //When sorting, null is considered to be less than any other object.
        public int Compare(ScoreRecord pair1, ScoreRecord pair2)
        {
            if (pair1 == null)
                return 1;

            if (pair2 == null)
                return -1;
            
            int compareResult = pair2.Score.CompareTo(pair1.Score);
            if (compareResult == 0) {
                compareResult = pair2.Timestamp.CompareTo(pair1.Timestamp);
            }
            return compareResult;
        }
    }
}
