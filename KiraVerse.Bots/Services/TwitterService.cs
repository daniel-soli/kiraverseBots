using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Tweetinvi;
using Tweetinvi.Models;

namespace KiraVerse.Bots.Services;

public class TwitterService : ITwitterService
{
    private const string TwitterApiBaseUrl = "https://api.twitter.com";
    readonly string _consumerKey, _consumerKeySecret, _accessToken, _accessTokenSecret;
    readonly HMACSHA1 _sigHasher;
    readonly DateTime _epochUtc = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private readonly HttpClient _client;
    private readonly IConfiguration _config;
    private readonly ICryptoCompareService _cryptoCompare;
    private bool _postToTwitter;

    public TwitterService(HttpClient client, IConfiguration config, ICryptoCompareService cryptoCompare)
    {
        _client = client;
        _config = config;
        _cryptoCompare = cryptoCompare;
        _consumerKey = _config.GetSection("kiraConsumerKey").Value!;
        _consumerKeySecret = _config.GetSection("kiraConsumerSecret").Value!;
        _accessToken = _config.GetSection("kiraAccessToken").Value!;
        _accessTokenSecret = _config.GetSection("kiraAccessSecret").Value!;
        _postToTwitter = _config.GetValue<bool>("postToTwitter");

        _sigHasher = new HMACSHA1(new ASCIIEncoding().GetBytes(string.Format("{0}&{1}", _consumerKeySecret, _accessTokenSecret)));
    }
    public async Task<string?> PostMediaAsync(string imageUrl = "https://loopring.mypinata.cloud/ipfs/QmYKzNAfUhtggNM5RduLwn1G1pd1X4YqNuLQYUhHgxdPPe", CancellationToken cancellationToken = default)
    {
        if (!_postToTwitter)
            return null;
        //byte[] data;
        string tempFile = "";
        try
        {
            using (WebClient client = new WebClient())
            {
                string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }
                tempFile = Path.Combine(tempDir, "temp.png");

                client.DownloadFile(imageUrl, tempFile);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Not working");
        }

        byte[] imageArray = await File.ReadAllBytesAsync(tempFile, cancellationToken);

        TwitterClient tClient = new TwitterClient(_consumerKey, _consumerKeySecret, _accessToken, _accessTokenSecret);
        IMedia imageMedia = await tClient.Upload.UploadTweetImageAsync(imageArray);

        var imageId = imageMedia.Id.ToString();

        return imageId;
    }

    public async Task<string?> PostTweetAsync(Models.Twitter.TwitterRequest tweet, CancellationToken cancellationToken = default)
    {
        if (!_postToTwitter)
            return null;
        
        var data = new Dictionary<string, string>();

        var fullUrl = TwitterApiBaseUrl + "/2/tweets";

        var timestamp = (int)((DateTime.UtcNow - _epochUtc).TotalSeconds);
        var timestampString = ((int)((DateTime.UtcNow - _epochUtc).TotalSeconds)).ToString();
        var nonce = Convert.ToBase64String(Encoding.UTF8.GetBytes(timestampString));
        // Add all the OAuth headers we'll need to use when constructing the hash.
        data.Add("oauth_consumer_key", _config.GetSection("kiraConsumerKey").Value!);
        data.Add("oauth_signature_method", "HMAC-SHA1");
        data.Add("oauth_timestamp", timestamp.ToString());
        data.Add("oauth_nonce", nonce); // Required, but Twitter doesn't appear to use it, so "a" will do.
        data.Add("oauth_token", _config.GetSection("kiraAccessToken").Value!);
        data.Add("oauth_version", "1.0");

        // Generate the OAuth signature and add it to our payload.
        data.Add("oauth_signature", GenerateSignature(fullUrl, data));

        // Build the OAuth HTTP Header from the data.
        string oAuthHeader = GenerateOAuthHeader(data);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("OAuth", oAuthHeader);


        var response = await HttpClientJsonExtensions.PostAsJsonAsync(_client, $"/2/tweets", tweet, CancellationToken.None);

        var result = await response.Content.ReadAsStringAsync(CancellationToken.None);

        return result;
    }

    /// <summary>
	/// Generate an OAuth signature from OAuth header values.
	/// </summary>
    private string GenerateSignature(string url, Dictionary<string, string> data)
    {
        var sigString = string.Join(
            "&",
            data
                .Union(data)
                .Select(kvp => string.Format("{0}={1}", Uri.EscapeDataString(kvp.Key), Uri.EscapeDataString(kvp.Value)))
                .OrderBy(s => s)
        );

        var fullSigData = string.Format(
            "{0}&{1}&{2}",
            "POST",
            Uri.EscapeDataString(url),
            Uri.EscapeDataString(sigString.ToString())
        );

        return Convert.ToBase64String(_sigHasher.ComputeHash(new ASCIIEncoding().GetBytes(fullSigData.ToString())));
    }

    private string GenerateSignatureMedia(string url, Dictionary<string, string> data)
    {
        var sigString = string.Join(
            "&",
            data
                .Union(data)
                .Select(kvp => string.Format("{0}={1}", Uri.EscapeDataString(kvp.Key), Uri.EscapeDataString(kvp.Value)))
                .OrderBy(s => s)
        );

        var fullSigData = string.Format(
            "{0}&{1}",
            Uri.EscapeDataString(url),
            Uri.EscapeDataString(sigString.ToString())
        );

        return Convert.ToBase64String(_sigHasher.ComputeHash(new ASCIIEncoding().GetBytes(fullSigData.ToString())));
    }

    /// <summary>
	/// Generate the raw OAuth HTML header from the values (including signature).
	/// </summary>
	string GenerateOAuthHeader(Dictionary<string, string> data)
    {
        return string.Join(
            ", ",
            data
                .Where(kvp => kvp.Key.StartsWith("oauth_"))
                .Select(kvp => string.Format("{0}=\"{1}\"", Uri.EscapeDataString(kvp.Key), Uri.EscapeDataString(kvp.Value)))
                .OrderBy(s => s)
        );
    }

    //Convert file to byte array
    public static byte[] AuthGetFileData(string fileUrl)
    {
        using (FileStream fs = new FileStream(fileUrl, FileMode.OpenOrCreate, FileAccess.ReadWrite))
        {
            byte[] buffur = new byte[fs.Length];
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                bw.Write(buffur);
                bw.Close();
            }
            return buffur;
        }
    }
}