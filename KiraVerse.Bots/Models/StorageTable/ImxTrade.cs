using Azure;
using Azure.Data.Tables;
using Newtonsoft.Json;

namespace KiraVerse.Bots.StorageTable;

public class ImxTrade : ITableEntity
{
    public string? PartitionKey { get; set; }
    public string? RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    
    [JsonProperty("id")]
    public Guid Id { get; set; }
    [JsonProperty("tokenAddress")]
    public string? TokenAddress { get; set; }
    [JsonProperty("updatedAt")]
    public DateTime UpdatedAt { get; set; }
    [JsonProperty("orderId")]
    public long OrderId { get; set; }
    [JsonProperty("sellerAddress")]
    public string? SellerAddress { get; set; }
    [JsonProperty("buyerAddress")]
    public string? BuyerAddress { get; set; }
}
