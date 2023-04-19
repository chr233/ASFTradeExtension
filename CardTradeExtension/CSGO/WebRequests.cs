using AngleSharp.Dom;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;
using ArchiSteamFarm.Web.Responses;
using CardTradeExtension.Data;

namespace CardTradeExtension.CSGO;

internal static class WebRequests
{
    /// <summary>
    /// 读取交易链接
    /// </summary>
    /// <param name="bot"></param>
    /// <returns></returns>
    internal static async Task<string?> GetTradeToken(Bot bot)
    {
        Uri request = new(SteamCommunityURL, $"/profiles/{bot.SteamID}/tradeoffers/privacy");
        HtmlDocumentResponse? response = await bot.ArchiWebHandler.UrlGetToHtmlDocumentWithSession(request, referer: SteamStoreURL).ConfigureAwait(false);

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
    /// 出售指定物品
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="asset"></param>
    /// <param name="price"></param>
    /// <returns></returns>
    internal static async Task<SellItemResponse?> SellItem(Bot bot, Asset asset, ulong price)
    {
        Uri request = new(SteamCommunityURL, "/market/sellitem/");
        Dictionary<string, string> data = new(6) {
            { "appid", asset.AppID.ToString() },
            { "contextid", asset.ContextID.ToString() },
            { "assetid", asset.AssetID.ToString() },
            { "amount", asset.Amount.ToString() },
            { "price", price.ToString() },
        };

        var response = await bot.ArchiWebHandler.UrlPostToJsonObjectWithSession<SellItemResponse>(request, data: data).ConfigureAwait(false);

        return response?.Content;
    }
}
