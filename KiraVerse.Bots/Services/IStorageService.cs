using KiraVerse.Bots.StorageTable;

namespace KiraVerse.Bots.Services;

public interface IStorageService<T>
{
    public Task<ImxTrade?> GetLatestByCollectionAddressAsync(string collectionAddress,
        CancellationToken cancellationToken = default);

    public Task<Guid?> CreateTradeAsync(ImxTrade trade, CancellationToken cancellationToken = default);
}