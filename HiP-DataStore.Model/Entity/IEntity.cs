namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public interface IEntity<TKey>
    {
        TKey Id { get; }
    }
}
