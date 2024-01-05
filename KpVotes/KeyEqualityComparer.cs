namespace KpVotes;

public record KeyEqualityComparer<T, TK>(Func<T, TK> GetKey) : IEqualityComparer<T>
{
    public bool Equals(T x, T y) => GetKey(x).Equals(GetKey(y));
    public int GetHashCode(T obj) => GetKey(obj).GetHashCode();
}