namespace KpVotes.Kinopoisk;

public interface IKpLoader
{
    public Task<string> Load(Uri uri, CancellationToken cancellation);
}