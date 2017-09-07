using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Events
{
    /// <summary>
    /// Excludes an event type derived from <see cref="ICrudEvent"/> from the timestamp cascade mechanism.
    /// This attribute cannot be inherited.
    /// </summary>
    /// <remarks>
    /// By default, events derived from <see cref="ICrudEvent"/> propagate their timestamp to referencing entities.
    /// For example, if a page is updated, the timestamp from the corresponding update event is propagated to all
    /// exhibits and pages referencing that updated page. Annotating the page update event with this attribute would
    /// prevent that propagation.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class DoNotPropagateTimestampToReferencersAttribute : Attribute
    {
    }
}
