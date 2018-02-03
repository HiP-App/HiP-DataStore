using System;
using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Utility
{
    /// <summary>
    /// This validation is for class List. 
    /// Check if number of objects in list is in the specified range
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    class ListRange : ValidationAttribute
    {
        private int _min;
        private int _max = int.MaxValue;
        private string _errorMessage;
        private bool _errorMessageWasReplaced;

        public int Minimum { get { return _min; } set { _min = value; if (!_errorMessageWasReplaced) GenNewErrorMessage(); } }
        public int Maximum { get { return _max; } set { _max = value; if (!_errorMessageWasReplaced) GenNewErrorMessage(); } }

        public new string ErrorMessage
        {
            get { return _errorMessage; }
            set { _errorMessage = value; _errorMessageWasReplaced = true;}
        }
  
        private void GenNewErrorMessage()
        {
            ErrorMessage = $"List can have from {Minimum} to {Maximum} number of objects"; 
        }

        public override bool IsValid(object value)
        {
            var list = value as IList;
            if (list != null)
            {
                return list.Count >= Minimum && list.Count <= Maximum;
            }
            return false;
        }

        public override string FormatErrorMessage(string name)
        {
            return ErrorMessage;
        }
    }
}
