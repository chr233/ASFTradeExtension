using AngleSharp.Dom;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;
using ArchiSteamFarm.Steam.Integration;
using ASFTradeExtension.Data;

namespace ASFTradeExtension.Csgo;

internal static class WebRequests
{
    /// <summary>
    /// 读取交易链接
    /// </summary>
    /// <param name="bot"></param>
    /// <returns></returns>
    internal static async Task<string?> GetTradeToken(Bot bot)
    {
        var request = new Uri(Utils.SteamCommunityURL, $"/profiles/{bot.SteamID}/tradeoffers/privacy");
        var response = await bot.ArchiWebHandler.UrlGetToHtmlDocumentWithSession(request, referer: Utils.SteamStoreURL).ConfigureAwait(false);

        if (response?.Content == null)
        {
            return null;
        }

        var inputEle = response.Content.SelectSingleNode<IElement>("//input[@id='trade_offer_access_url']");

        string? tradeLink = inputEle?.GetAttribute("value");
        if (string.IsNullOrEmpty(tradeLink))
        {
            return null;
        }

        var match = RegexUtils.MatchTradeLink().Match(tradeLink);
        return match.Success ? match.Groups[2].Value : null;
    }

    /// <summary>
    /// 上架物品
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="asset"></param>
    /// <param name="price"></param>
    /// <returns></returns>
    internal static async Task<SellItemResponse?> SellItem(Bot bot, Asset asset, decimal price)
    {
        var request = new Uri(Utils.SteamCommunityURL, "/market/sellitem/");
        var data = new Dictionary<string, string>(6) {
            { "appid", asset.AppID.ToString() },
            { "contextid", asset.ContextID.ToString() },
            { "assetid", asset.AssetID.ToString() },
            { "amount", asset.Amount.ToString() },
            { "price", (price*100).ToString("N0") },
        };

        var response = await bot.ArchiWebHandler.UrlPostToJsonObjectWithSession<SellItemResponse>(request, data: data).ConfigureAwait(false);

        return response?.Content;
    }

    /// <summary>
    /// 获取市场历史
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="count"></param>
    /// <param name="start"></param>
    /// <returns></returns>
    internal static async Task<MarketHistoryResponse?> GetMarketHistory(Bot bot, uint count = 50, uint start = 0)
    {
        var request = new Uri(Utils.SteamCommunityURL, $"/market/myhistory/render/?count={count}&start={start}");
        var referer = new Uri(Utils.SteamCommunityURL, "/market/");

        var response = await bot.ArchiWebHandler.UrlGetToJsonObjectWithSession<MarketHistoryResponse>(request, referer: referer).ConfigureAwait(false);

        return response?.Content;
    }

    /// <summary>
    /// 下架物品
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="itemId"></param>
    /// <returns></returns>
    internal static async Task<bool> RemoveMarketListing(Bot bot, string itemId)
    {
        var request = new Uri(Utils.SteamCommunityURL, $"/market/removelisting/{itemId}");
        var referer = new Uri(Utils.SteamCommunityURL, "/market/");

        var response = await bot.ArchiWebHandler.UrlPostWithSession(request, referer: referer, session: ArchiWebHandler.ESession.Lowercase).ConfigureAwait(false);

        return response;
    }
}
