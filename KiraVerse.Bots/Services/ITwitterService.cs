using KiraVerse.Bots.Models.Twitter;

namespace KiraVerse.Bots.Services;

public interface ITwitterService
{
    Task<string?> PostTweetAsync(TwitterRequest tweet, CancellationToken cancellationToken = default);
    Task<string?> PostMediaAsync(string imageUrl = "https://loopring.mypinata.cloud/ipfs/QmYKzNAfUhtggNM5RduLwn1G1pd1X4YqNuLQYUhHgxdPPe", CancellationToken cancellationToken = default);
}