namespace BetterCollections.Memories;

public interface IRef<T>
{
    public ref T GetRef();
}

public interface IReadOnlyRef<T>
{
    public ref readonly T GetReadOnlyRef();
}
