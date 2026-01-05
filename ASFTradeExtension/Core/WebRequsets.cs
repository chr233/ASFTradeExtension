using ArchiSteamFarm.Steam;
using ASFTradeExtension.Data.Core;

namespace ASFTradeExtension.Core;

internal static class WebRequsets
{
    /// <summary>
    /// 获取徽章信息
    /// </summary>
    /// <param name="steamId"></param>
    /// <returns></returns>
    public static async Task<GetBadgesResponse?> GetUserBadges(Bot bot, ulong steamId)
    {
        var token = bot.AccessToken ?? throw new Exception("Bot access token is null");
        var request = new Uri(SteamApiURL, $"/IPlayerService/GetBadges/v1/?steamid={steamId}&access_token={token}");

        var response = await bot.ArchiWebHandler
            .UrlGetToJsonObjectWithSession<GetBadgesResponse>(request).ConfigureAwait(false);

        return response?.Content;
    }
}
