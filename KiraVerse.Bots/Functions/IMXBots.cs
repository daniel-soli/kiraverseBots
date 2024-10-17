using Discord;
using Discord.WebSocket;
using KiraVerse.Bots.Configuration;
using KiraVerse.Bots.Exceptions;
using KiraVerse.Bots.Helpers;
using KiraVerse.Bots.Models.IMX;
using KiraVerse.Bots.Services;
using KiraVerse.Bots.StorageTable;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KiraVerse.Bots.Functions;

public class IMXBots
{
    private readonly IIMXService _imxService;
    private readonly IStorageService<ImxTrade> _storageService;
    private readonly ITwitterService _twitterService;
    private readonly ICryptoCompareService _cryptoCompare;
    private readonly IConfiguration _config;
    private readonly ILogger<IMXBots> _logger;

    public IMXBots(IIMXService ImxService, 
        IStorageService<ImxTrade> storageService, 
        ITwitterService twitterService, 
        ICryptoCompareService cryptoCompare, 
        IConfiguration config,
        ILogger<IMXBots> logger)
    {
        _imxService = ImxService;
        _storageService = storageService;
        _twitterService = twitterService;
        _cryptoCompare = cryptoCompare;
        _config = config;
        _logger = logger;
    }

    [Function("ImxBotGenesis")]
    public async Task ImxBotGenesis([TimerTrigger("0 */10 * * * *", RunOnStartup = true)] TimerInfo myTimer)
    {
        if (!_config.GetValue<bool>("imxActive"))
        {
            return;
        }

        _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

        var latestSaleForCollection = await _storageService.GetLatestByCollectionAddressAsync("0xe2c921ed59f5a4011b4ffc6a4747015dcb5b804f");

        _logger.LogInformation($"Latest sale in db for collection '0xe2c921ed59f5a4011b4ffc6a4747015dcb5b804f' {latestSaleForCollection?.OrderId}");

        if (latestSaleForCollection?.TokenAddress == null)
        {
            latestSaleForCollection!.UpdatedAt = DateTime.UtcNow.AddDays(-1);
        }

        if (latestSaleForCollection.UpdatedAt < DateTime.UtcNow.AddHours(-2))
        {
            latestSaleForCollection.UpdatedAt = DateTime.UtcNow.AddMinutes(-10);
        }

        var sellerOrders = await _imxService.GetSellerOrderByCollectionAsync("0xe2c921ed59f5a4011b4ffc6a4747015dcb5b804f", latestSaleForCollection.UpdatedAt);

        if (sellerOrders == null || sellerOrders.result.Length == 0)
        {
            _logger.LogInformation("No new seller registered on IMX.");
            return;
        }

        _logger.LogInformation($"{sellerOrders.result.Length} new sales for collection '0xe2c921ed59f5a4011b4ffc6a4747015dcb5b804f'");

        foreach (var sale in sellerOrders.result!)
        {
            sale.sell.data.properties.image_url = sale.sell.data.properties.image_url.Replace("https://kiraverse.mypinata.cloud/ipfs/", "https://hcvk6az2pdimb3dbicfqken6xggdqkxb3bnwkoenou53fqm54zka.arweave.net/");

            sale.buy.type = sale.buy.type.ToLower() == "erc20" ? "IMX" : sale.buy.type;
            ImxTrade trade = new()
            {
                Id = Guid.NewGuid(),
                OrderId = sale.order_id,
                UpdatedAt = sale.updated_timestamp,
                TokenAddress = sale.sell.data.token_address,
                SellerAddress = sale.user
            };
            var buyerOrder = await _imxService.GetBuyerOrderByAssetIdAsync(sale.sell.data.id, latestSaleForCollection.UpdatedAt);

            if (buyerOrder?.result == null || buyerOrder.result.Count() == 0)
            {
                _logger.LogInformation("No new buyer registered on IMX.");
                continue;
            }

            var buyer = buyerOrder.result.FirstOrDefault(x => x.buy.data.id == sale.sell.data.id);

            trade.BuyerAddress = buyer?.user;

            var imageId = await _twitterService.PostMediaAsync(sale.sell.data.properties.image_url);

            // Generate tweet to send
            var amount = TokenAmountConverter.ToString(Convert.ToDecimal(sale.buy.data.quantity), sale.buy.data.decimals);
            //var amountDecimalString = amount.Replace(".", ",");
            string price = $"{amount} {sale.buy.type}";

            int usd = 0;

            try
            {
                var cryptoValue = await _cryptoCompare.GetRatesAsync(sale.buy.type);
                usd = (int)(cryptoValue!.USD * Convert.ToDecimal(amount));
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Could not fetch value");
            }

            Models.Twitter.TwitterRequest request = new()
            {
                Text = $"",
                Media = new()
            };

            var amountOfNfts = "";

            if (int.Parse(sale.amount_sold) > 1)
            {
                amountOfNfts = $"x {sale.amount_sold}";
            }

            request.Text = $"{sale.sell.data.properties.name} {amountOfNfts} \n💰 SOLD FOR {price} (${usd}) \n➡️ Seller: {trade.SellerAddress} \n⬅️ Buyer: {trade.BuyerAddress} \n\n🐦 #Kiraverse #IMX $PARAM \n\U0001f6d2 https://market.immutable.com/collections/0xe2c921ed59f5a4011b4ffc6a4747015dcb5b804f";
            request.Media.Media_ids = new string[] { imageId! };

            // Post to twitter
            try
            {
                await _twitterService.PostTweetAsync(request);

            }
            catch (Exception ex) 
            {
                _logger.LogInformation("Could not post to twitter. {ex}", ex);
            }

            // Post to Kira discord
            try
            {
                await PublishToDiscord(sale, trade, price, usd);
            }
            catch (NotFoundException nf)
            {
                _logger.LogError(nf.Message);
            }
            catch (Exception e)
            {
                _logger.LogError($"Error: {e}");
            }
            
            await _storageService.CreateTradeAsync(trade);
        }
    }

    [Function("ImxBotForgotten")]
    public async Task ImxBotForgotten([TimerTrigger("0 */10 * * * *", RunOnStartup = true)] TimerInfo myTimer, ILogger log)
    {
        if (!_config.GetValue<bool>("imxActive"))
        {
            return;
        }
    
        _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
    
        var latestSaleForCollection = await _storageService.GetLatestByCollectionAddressAsync("0x4101fb43f4b37c3030d03f4f181b374c099466f5");
    
        _logger.LogInformation($"Latest sale in db for collection '0x4101fb43f4b37c3030d03f4f181b374c099466f5' {latestSaleForCollection?.OrderId}");
    
        if (latestSaleForCollection!.TokenAddress == null)
        {
            latestSaleForCollection.UpdatedAt = DateTime.UtcNow.AddDays(-1);
        }
    
        var sellerOrders = await _imxService.GetSellerOrderByCollectionAsync("0x4101fb43f4b37c3030d03f4f181b374c099466f5", latestSaleForCollection.UpdatedAt);
    
        if (sellerOrders == null || sellerOrders.result.Count() == 0)
        {
            _logger.LogInformation("No new seller registered on IMX.");
            return;
        }
    
        _logger.LogInformation($"{sellerOrders.result.Count()} new sales for collection '0x4101fb43f4b37c3030d03f4f181b374c099466f5'");
    
        foreach (var sale in sellerOrders.result!)
        {
            sale.buy.type = sale.buy.type.ToLower() == "erc20" ? "IMX" : sale.buy.type;
            ImxTrade trade = new()
            {
                Id = Guid.NewGuid(),
                OrderId = sale.order_id,
                UpdatedAt = sale.updated_timestamp,
                TokenAddress = sale.sell.data.token_address,
                SellerAddress = sale.user
            };
            var buyerOrder = await _imxService.GetBuyerOrderByAssetIdAsync(sale.sell.data.id, latestSaleForCollection.UpdatedAt);
    
            if (buyerOrder?.result == null || buyerOrder.result.Count() == 0)
            {
                _logger.LogInformation("No new buyer registered on IMX.");
                continue;
            }
    
            var buyer = buyerOrder.result.FirstOrDefault(x => x.buy.data.id == sale.sell.data.id);
    
            trade.BuyerAddress = buyer?.user;
    
            var imageId = await _twitterService.PostMediaAsync(sale.sell.data.properties.image_url);
    
            // Generate tweet to send
            var amount = TokenAmountConverter.ToString(Convert.ToDecimal(sale.buy.data.quantity), sale.buy.data.decimals);
            //var amountDecimalString = amount.Replace(".", ",");
            string price = $"{amount} {sale.buy.type}";
    
            int usd = 0;
    
            try
            {
                var cryptoValue = await _cryptoCompare.GetRatesAsync(sale.buy.type);
                usd = (int)(cryptoValue!.USD * Convert.ToDecimal(amount));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
    
            Models.Twitter.TwitterRequest request = new()
            {
                Text = $"",
                Media = new()
            };
    
            var amountOfNfts = "";
    
            if (int.Parse(sale.amount_sold) > 1)
            {
                amountOfNfts = $"x {sale.amount_sold}";
            }
    
            request.Text = $"{sale.sell.data.properties.name} {amountOfNfts} \n💰 SOLD FOR {price} (${usd}) \n➡️ Seller: {trade.SellerAddress} \n⬅️ Buyer: {trade.BuyerAddress} \n\n🐦 #Kiraverse #IMX $PARAM \n\U0001f6d2 https://market.immutable.com/collections/0x4101fb43f4b37c3030d03f4f181b374c099466f5";
            request.Media.Media_ids = new string[] { imageId! };
    
    
            await _twitterService.PostTweetAsync(request);
    
            // Post to Kira discord
            try
            {
                await PublishToDiscord(sale, trade, price, usd);
            }
            catch (NotFoundException nf)
            {
                _logger.LogError(nf.Message);
            }
            catch (Exception e)
            {
                _logger.LogError($"Error: {e}");
            }
    
            await _storageService.CreateTradeAsync(trade);
    
        }
    }
    
    [Function("ImxBotHadoFusion")]
    public async Task ImxBotHadoFusion([TimerTrigger("0 */10 * * * *", RunOnStartup = true)] TimerInfo myTimer, ILogger log)
    {
        if (!_config.GetValue<bool>("imxActive"))
        {
            return;
        }
    
        _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
    
        var latestSaleForCollection = await _storageService.GetLatestByCollectionAddressAsync("0x78ddd57e1fabc1d0373cf28e8dd02d038e636353");
    
        _logger.LogInformation($"Latest sale in db for collection '0x78ddd57e1fabc1d0373cf28e8dd02d038e636353' {latestSaleForCollection?.OrderId}");
    
        if (latestSaleForCollection!.TokenAddress == null)
        {
            latestSaleForCollection.UpdatedAt = DateTime.UtcNow.AddDays(-1);
        }
    
        var sellerOrders = await _imxService.GetSellerOrderByCollectionAsync("0x78ddd57e1fabc1d0373cf28e8dd02d038e636353", latestSaleForCollection.UpdatedAt);
    
        if (sellerOrders == null || sellerOrders.result.Count() == 0)
        {
            _logger.LogInformation("No new seller registered on IMX.");
            return;
        }
    
        _logger.LogInformation($"{sellerOrders.result.Count()} new sales for collection '0x78ddd57e1fabc1d0373cf28e8dd02d038e636353'");
    
        foreach (var sale in sellerOrders.result!)
        {
            sale.buy.type = sale.buy.type.ToLower() == "erc20" ? "IMX" : sale.buy.type;
            
            ImxTrade trade = new()
            {
                Id = Guid.NewGuid(),
                OrderId = sale.order_id,
                UpdatedAt = sale.updated_timestamp,
                TokenAddress = sale.sell.data.token_address,
                SellerAddress = sale.user
            };
            var buyerOrder = await _imxService.GetBuyerOrderByAssetIdAsync(sale.sell.data.id, latestSaleForCollection.UpdatedAt);
    
            if (buyerOrder?.result == null || buyerOrder.result.Count() == 0)
            {
                _logger.LogInformation("No new buyer registered on IMX.");
                continue;
            }
    
            var buyer = buyerOrder.result.FirstOrDefault(x => x.buy.data.id == sale.sell.data.id);
    
            trade.BuyerAddress = buyer?.user;
            var imageUrl = "https://arweave.net/5dS1bgmt53fFMKluYues7Slc3zY5XNmFresO9hGKbGU/HADO_COVER.jpg";
            var imageId = await _twitterService.PostMediaAsync(imageUrl);
    
            // Generate tweet to send
            var amount = TokenAmountConverter.ToString(Convert.ToDecimal(sale.buy.data.quantity), sale.buy.data.decimals);
            //var amountDecimalString = amount.Replace(".", ",");
            string price = $"{amount} {sale.buy.type}";
    
            int usd = 0;
    
            try
            {
                var cryptoValue = await _cryptoCompare.GetRatesAsync(sale.buy.type);
                usd = (int)(cryptoValue!.USD * Convert.ToDecimal(amount));
            }
            catch (Exception ex)
            {
    
            }
    
            Models.Twitter.TwitterRequest request = new()
            {
                Text = $"",
                Media = new()
            };
    
            string amountOfNfts = "";
    
            if (int.Parse(sale.amount_sold) > 1)
            {
                amountOfNfts = $"x {sale.amount_sold}";
            }
    
            request.Text = $"{sale.sell.data.properties.name} {amountOfNfts} \n💰 SOLD FOR {price} (${usd}) \n➡️ Seller: {trade.SellerAddress} \n⬅️ Buyer: {trade.BuyerAddress} \n\n🐦 #Kiraverse #IMX $PARAM \n\U0001f6d2 https://market.immutable.com/collections/0x78ddd57e1fabc1d0373cf28e8dd02d038e636353";
            request.Media.Media_ids = new string[] { imageId! };
    
            await _twitterService.PostTweetAsync(request);
    
            // Post to Kira discord
            try
            {
                await PublishToDiscord(sale, trade, price, usd);
            }
            catch (NotFoundException nf)
            {
                _logger.LogError(nf.Message);
            }
            catch (Exception e)
            {
                _logger.LogError($"Error: {e}");
            }
    
            await _storageService.CreateTradeAsync(trade);
        }
    }
    
    [Function("ImxBotAeternals")]
    public async Task ImxBotAeternals([TimerTrigger("0 */10 * * * *", RunOnStartup = true)] TimerInfo myTimer, ILogger log)
    {
        if (!_config.GetValue<bool>("imxActive"))
        {
            return;
        }
    
        _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
    
        var latestSaleForCollection = await _storageService.GetLatestByCollectionAddressAsync("0x8b4eb39340d52efabd87973ca96d6035a91b1d7c");
    
        _logger.LogInformation($"Latest sale in db for collection '0x8b4eb39340d52efabd87973ca96d6035a91b1d7c' {latestSaleForCollection?.OrderId}");
    
        if (latestSaleForCollection!.TokenAddress == null)
        {
            latestSaleForCollection.UpdatedAt = DateTime.UtcNow.AddDays(-1);
        }
    
    
    
        var sellerOrders = await _imxService.GetSellerOrderByCollectionAsync("0x8b4eb39340d52efabd87973ca96d6035a91b1d7c", latestSaleForCollection.UpdatedAt);
    
        //sellerOrders.result.Reverse();
    
        if (sellerOrders == null || sellerOrders.result.Length == 0)
        {
            _logger.LogInformation("No new seller registered on IMX.");
            return;
        }
    
        _logger.LogInformation($"{sellerOrders.result.Count()} new sales for collection '0x8b4eb39340d52efabd87973ca96d6035a91b1d7c'");
    
        foreach (var sale in sellerOrders.result!)
        {
            sale.buy.type = sale.buy.type.ToLower() == "erc20" ? "IMX" : sale.buy.type;
            ImxTrade trade = new()
            {
                Id = Guid.NewGuid(),
                OrderId = sale.order_id,
                UpdatedAt = sale.updated_timestamp,
                TokenAddress = sale.sell.data.token_address,
                SellerAddress = sale.user
            };
            var buyerOrder = await _imxService.GetBuyerOrderByAssetIdAsync(sale.sell.data.id, latestSaleForCollection.UpdatedAt);
    
            if (buyerOrder?.result == null || buyerOrder?.result.Count() == 0)
            {
                _logger.LogInformation("No new buyer registered on IMX.");
                continue;
            }
    
            var buyer = buyerOrder?.result.FirstOrDefault(x => x.buy.data.id == sale.sell.data.id);
    
            trade.BuyerAddress = buyer?.user;
    
            var imageId = await _twitterService.PostMediaAsync(sale.sell.data.properties.image_url);
    
            // Generate tweet to send
            var amount = TokenAmountConverter.ToString(Convert.ToDecimal(sale.buy.data.quantity), sale.buy.data.decimals);
            //var amountDecimalString = amount.Replace(".", ",");
            string price = $"{amount} {sale.buy.type}";
    
            int usd = 0;
    
            try
            {
                var cryptoValue = await _cryptoCompare.GetRatesAsync(sale.buy.type);
                usd = (int)(cryptoValue!.USD * Convert.ToDecimal(amount));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
    
            Models.Twitter.TwitterRequest request = new()
            {
                Text = $"",
                Media = new()
            };
    
            string amountOfNfts = "";
    
            if (int.Parse(sale.amount_sold) > 1)
            {
                amountOfNfts = $"x {sale.amount_sold}";
            }
    
            request.Text = $"{sale.sell.data.properties.name} {amountOfNfts} \n💰 SOLD FOR {price} (${usd}) \n➡️ Seller: {trade.SellerAddress} \n⬅️ Buyer: {trade.BuyerAddress} \n\n🐦 #Kiraverse #IMX $PARAM \n\U0001f6d2 https://market.immutable.com/collections/0x8b4eb39340d52efabd87973ca96d6035a91b1d7c";
            request.Media.Media_ids = new string[] { imageId! };
    
            await _twitterService.PostTweetAsync(request);
    
            // Post to Kira discord
            try
            {
                await PublishToDiscord(sale, trade, price, usd);
            }
            catch (NotFoundException nf)
            {
                _logger.LogError(nf.Message );
            }
            catch (Exception e)
            {
                _logger.LogError($"Error: {e}");
            }
    
            await _storageService.CreateTradeAsync(trade);
        }
    }

    public async Task PublishToDiscord(ImxSellOrderDto sale, ImxTrade trade, string price, int usd)
    {
        var settings = _config.GetSection("Discord").Get<DiscordSettings>()!;

        // fetch the sub discord info
        string botToken = settings.SalesBotToken!;
        ulong channelId = Convert.ToUInt64(settings.SalesBotChannel);

        var client = new DiscordSocketClient();
        await client.LoginAsync(TokenType.Bot, botToken);
        await client.StartAsync();
        var channel = await client.GetChannelAsync(channelId) as IMessageChannel;

        var title = "An item has been sold";
        var amountNftsField = new EmbedFieldBuilder()
            .WithName("# Of Items")
            .WithValue(sale.amount_sold);

        var nftField = new EmbedFieldBuilder()
            .WithName("NFT")
            .WithValue(sale.sell.data.properties.name);

        var priceField = new EmbedFieldBuilder()
            .WithName("Price")
            .WithValue($"{price} (${usd})");

        var buyerField = new EmbedFieldBuilder()
            .WithName("Buyer")
            .WithValue(trade.BuyerAddress);

        var sellerField = new EmbedFieldBuilder()
            .WithName("Seller")
            .WithValue(trade.SellerAddress);

        var footer = new EmbedFooterBuilder()
            .WithText("Powered by LayerLoot.io");

        var emb = new EmbedBuilder()
            .WithThumbnailUrl(sale.sell.data.properties.image_url!)
            .WithTitle(title)
            .WithColor(Discord.Color.Green)
            .AddField(nftField)
            .AddField(amountNftsField)
            .AddField(priceField)
            .AddField(buyerField)
            .AddField(sellerField)
            .WithFooter(footer)
            .Build();

        try
        {
            await channel!.SendMessageAsync(null, false, emb);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }

        client.Dispose();
    }
}
