namespace KpVotes;

public static class HashSetExtensions
{
    public static HashSet<T> ToHashSet<T, TK>(this IEnumerable<T> items, Func<T, TK> toKey) =>
        items.ToHashSet(new KeyEqualityComparer<T, TK>(toKey));
}