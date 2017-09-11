using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Utility
{
    /// <summary>
    /// This validation is for class ContentStatus. 
    /// By default allowed DRAFT, IN_REVIEW, PUBLISHED
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    class AllowedStatuses : ValidationAttribute
    {
        private ContentStatus[] Default = new ContentStatus[] { ContentStatus.Draft,
                                                                ContentStatus.In_Review,
                                                                ContentStatus.Published };

        public ContentStatus[] Allowed { get; set; }

        public AllowedStatuses(params ContentStatus[] statuses)
        {
            Allowed = (statuses.Length == 0) ? Default : statuses;
        }
        
        public override bool IsValid(object value)
        {
            ContentStatus status = (ContentStatus) value;
            return Array.Exists(Allowed, x => x == status);
        }

        public override string FormatErrorMessage(string name)
        {
            return String.Format(CultureInfo.CurrentCulture,
              "Field can only take : {0}", String.Join(",", Allowed.Select(x => x.ToString())));
        }
    }
}
