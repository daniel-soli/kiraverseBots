using System.Net.Http.Headers;
using KiraVerse.Bots.Models.CryptoCompare;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace KiraVerse.Bots.Services;

public class CryptoCompareService : ICryptoCompareService
{
    private readonly HttpClient _client;
    private readonly IConfiguration _configuration;

    public CryptoCompareService(HttpClient client, IConfiguration configuration)
    {
        _client = client;
        _configuration = configuration;
        var apiKey = _configuration.GetValue<string>("CryptoCompareApiKey");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("ApiKey", apiKey);
    }

    public async Task<FiatUsd?> GetRatesAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: Add your username to the extraParameter
            var rates = await _client.GetAsync($"/data/price?fsym={token}&tsyms=USD&extraParams=<YourName>", cancellationToken);

            if (rates.IsSuccessStatusCode)
            {
                var json = await rates.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonConvert.DeserializeObject<FiatUsd>(json)!;

                return result;
            }
            //return rates;
            return null;
        }
        catch (Exception e)
        {
            return null;
        }
    }
}