using KiraVerse.Bots.Models.CryptoCompare;

namespace KiraVerse.Bots.Services;

public interface ICryptoCompareService
{
    Task<FiatUsd?> GetRatesAsync(string token, CancellationToken cancellationToken = default);
}