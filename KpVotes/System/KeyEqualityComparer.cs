namespace KpVotes.System;

public record KeyEqualityComparer<T, TK>(Func<T, TK> GetKey) : IEqualityComparer<T>
{
    public bool Equals(T x, T y)
    {
        return GetKey(x).Equals(GetKey(y));
    }

    public int GetHashCode(T obj)
    {
        return GetKey(obj).GetHashCode();
    }
}