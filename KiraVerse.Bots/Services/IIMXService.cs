using KiraVerse.Bots.Models.IMX;

namespace KiraVerse.Bots.Services;

public interface IIMXService
{
    Task<RootobjectImxSellOrder?> GetSellerOrderByCollectionAsync(string collectionId, DateTime updated_at, CancellationToken cancellationToken = default);
    Task<RootobjectImxBuyOrder?> GetBuyerOrderByAssetIdAsync(string assetId, DateTime updated_at, CancellationToken cancellationToken = default);
}