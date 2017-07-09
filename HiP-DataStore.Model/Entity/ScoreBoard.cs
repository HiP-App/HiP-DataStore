using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class ScoreBoard : SortedSet<ScoreRecord>
    {
       public ScoreBoard() : base(new UsersScoreComparer()) { }
    }

    //Making sorting first by scores. Then by Timestamp
    class UsersScoreComparer : IComparer<ScoreRecord>
    {
        public int Compare(ScoreRecord pair1, ScoreRecord pair2)
        {
            int compareResult = pair1.Score.CompareTo(pair2.Score);
            if (compareResult == 0) {
                compareResult = pair1.Timestamp.CompareTo(pair2.Timestamp);
            }
            return compareResult;
        }
    }
}
