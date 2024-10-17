# Setup

## Discord Application
You need to set up a discord application to be able to post to Discord.
This can be done here: https://discord.com/developers/applications
Register the application and setup OAuth and the bot itself.

Remember permissions to send messages and links and images. 

## Azure resources

### Function App
You need to set up an Azure Function in order for this to run. 

#### Environment Variables needed to run
* Discord__SalesBotToken
  * This is the Discord Application Token
* Discord__SalesBotChannel
  * Discord Channel to send messages to
* StorageConnectionString
  * ConnectionString to the Storage Account
* twitterBaseUrl
  * https://api.twitter.com
* cryptoCompareBaseUrl
  * https://min-api.cryptocompare.com
* imxBaseUrl
  * https://api.x.immutable.com
* postToTwitter
  * Set this to true or false to be able to activate/deactivate the twitter post directly from the config
* imxActive
  * True/False in config to be able to easily turn off notifications in general (discord and twitter)
* CryptoCompareApiKey
  * Your apikey for CC

### Storage Account
Create an Azure Storage Account with at least a storage table

## Crypto Compare
Set ut a cryptocompare account with an API KEY

## GitHub Action
Create a GitHub Action to automatically deploy the Function to Azure Function.
Get the publishing profile from the azure function and store it in a GitHub Secret

See example [HERE](./.github/workflows/deploy.yml)
