using AngleSharp.Dom;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Web.Responses;

namespace CardTradeExtension.CSGO
{
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
    }
}
