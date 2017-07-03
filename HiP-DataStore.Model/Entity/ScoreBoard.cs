using System;
using System.Collections.Generic;
using System.Text;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    class ScoreBoard
    {
        public SortedSet<User> ScoreList { get; } = new SortedSet<User>(new UserComparer());

        public ScoreBoard() { }
    }

    //Making sorting first by scores. If Scores are Equal then by Names,
    //if names are equal then by email ( they can`t be equal)
    class UserComparer : IComparer<User>
    {
        public int Compare(User user1, User user2)
        {
            int compareResult = user1.Scores.CompareTo(user2.Scores);
            if (compareResult == 0) {
                compareResult = user1.Name.CompareTo(user2.Name);
                    if (compareResult == 0)
                    compareResult = user1.Email.CompareTo(user2.Email);
            }
            return compareResult;
        }
    }
}
