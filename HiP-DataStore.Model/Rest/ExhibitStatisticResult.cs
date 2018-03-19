namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    /// <summary>
    /// Represent the information about the event.
    /// How many events happened per year/month/day
    /// </summary>
    public class ExhibitStatisticResult
    {
        public int Year { get; set; }

        public int Month { get; set; }

        public int Day { get; set; }
    }
}
