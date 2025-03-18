namespace KpVotes;

public interface IKpClient
{
    Task<string> GetPage(Uri uri);
}