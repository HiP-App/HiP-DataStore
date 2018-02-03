﻿using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class ReviewArgs
    {

        public string Description { get; set; }

        public List<string> Reviewers { get; set; }

        /// <summary>
        /// Amount of students, that need to approve, in order to approve the review
        /// </summary>
        public int StudentsToApprove { get; set; }

        public bool ReviewableByStudents { get; set; }
    }
}
