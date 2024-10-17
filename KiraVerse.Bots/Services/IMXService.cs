using System.Net;
using KiraVerse.Bots.Models.IMX;
using Newtonsoft.Json;

namespace KiraVerse.Bots.Services;

public class IMXService : IIMXService
{
    private readonly HttpClient _client;

    public IMXService(HttpClient client)
    {
        _client = client;
    }

    public async Task<RootobjectImxBuyOrder?> GetBuyerOrderByAssetIdAsync(string assetId, DateTime updated_at, CancellationToken cancellationToken = default)
    {
        try
        {
            var test = updated_at.ToUniversalTime().ToString("O");
            var response = await _client.GetAsync($"/v3/orders?buy_asset_id={assetId}&status=filled&direction=desc&order_by=updated_at&updated_min_timestamp={test}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                RootobjectImxBuyOrder result = JsonConvert.DeserializeObject<RootobjectImxBuyOrder>(json)!;

                return result;
            }
            else
            {
                return null;
            }

        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        return null;
    }

    public async Task<RootobjectImxSellOrder?> GetSellerOrderByCollectionAsync(string collectionId, DateTime updated_at, CancellationToken cancellationToken = default)
    {
        try
        {
            var test = updated_at.ToUniversalTime().ToString("O");
            var response = await _client.GetAsync($"/v3/orders?sell_token_address={collectionId}&status=filled&order_by=updated_at&updated_min_timestamp={test}&direction=asc", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                RootobjectImxSellOrder result = JsonConvert.DeserializeObject<RootobjectImxSellOrder>(json)!;

                return result;
            }
            else
            {
                return null;
            }

        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        return null;
    }
}
