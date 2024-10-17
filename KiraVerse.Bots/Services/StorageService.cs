using Azure;
using Azure.Data.Tables;
using KiraVerse.Bots.StorageTable;

namespace KiraVerse.Bots.Services;

public class StorageService<T> : IStorageService<T> where T : class, ITableEntity, new()
{
    private readonly TableClient _tableClient;
    public StorageService(string connectionString, string tableName)
    {
        var serviceClient = new TableServiceClient(connectionString);
        _tableClient = serviceClient.GetTableClient(tableName);
        _tableClient.CreateIfNotExists();
    }

    public async Task<ImxTrade?> GetLatestByCollectionAddressAsync(string collectionAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            // Query the table for entities matching the partitionKey, ordering by Timestamp in descending order
            var queryResult = _tableClient.QueryAsync<ImxTrade>(entity => entity.PartitionKey == collectionAddress)
                .OrderByDescending(e => e.UpdatedAt);

            // Get the first result (latest entity)
            return await queryResult.FirstOrDefaultAsync(cancellationToken: cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<Guid?> CreateTradeAsync(ImxTrade trade, CancellationToken cancellationToken = default)
    {
        trade.PartitionKey = trade.TokenAddress;
        trade.RowKey = Guid.NewGuid().ToString();
        await _tableClient.AddEntityAsync(trade, cancellationToken);
        return trade.Id;
    }
}