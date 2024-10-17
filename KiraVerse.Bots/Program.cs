using KiraVerse.Bots.Services;
using KiraVerse.Bots.StorageTable;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddSingleton<IStorageService<ImxTrade>>(provider =>
        {
            var connectionString = context.Configuration.GetSection("StorageConnectionString").Value;
            const string tableName = "ImxSales";
            return new StorageService<ImxTrade>(connectionString!, tableName);
        });
        services.AddHttpClient<ITwitterService, TwitterService>(client =>
        {
            client.BaseAddress = new Uri(context.Configuration.GetSection("twitterBaseUrl").Value!);
        });
        services.AddHttpClient<ICryptoCompareService, CryptoCompareService>(client =>
        {
            client.BaseAddress = new Uri(context.Configuration.GetSection("cryptoCompareBaseUrl").Value!);
        });
        services.AddHttpClient<IIMXService, IMXService>(client =>
        {
            client.BaseAddress = new Uri(context.Configuration.GetSection("imxBaseUrl").Value!);
        });
    })
    .Build();

host.Run();