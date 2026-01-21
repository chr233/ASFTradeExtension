using ArchiSteamFarm.Core;
using ArchiSteamFarm.Steam.Data;
using ASFTradeExtension.Data.Core;

namespace ASFTradeExtension.Core;

public partial class InventoryHandler
{
    private string? TradeLink { get; set; }

    /// <summary>
    /// 读取交易链接
    /// </summary>
    /// <returns></returns>
    public async Task<string?> GetTradeLink()
    {
        if (!string.IsNullOrEmpty(TradeLink))
        {
            return TradeLink;
        }

        var request = new Uri(SteamCommunityURL, $"/profiles/{_bot.SteamID}/tradeoffers/privacy");
        var response = await _bot.ArchiWebHandler.UrlGetToHtmlDocumentWithSession(request, referer: SteamStoreURL)
            .ConfigureAwait(false);
        if (response?.Content == null)
        {
            return null;
        }
        var inputEle = response.Content.QuerySelector("#trade_offer_access_url");

        TradeLink = inputEle?.GetAttribute("value");
        return TradeLink;
    }

    /// <summary>
    /// 查找用户
    /// </summary>
    /// <param name="steamId"></param>
    /// <returns></returns>
    public async Task<FindUserData?> GetUserBasicInfo(ulong steamId)
    {
        var request = new Uri(SteamCommunityURL, $"/profiles/{steamId}?xml=1");
        var response = await _bot.ArchiWebHandler.UrlGetToHtmlDocumentWithSession(request).ConfigureAwait(false);

        var root = response?.Content;
        if (root == null)
        {
            return null;
        }

        var strSteamId64 = root.QuerySelector("steamID64")?.TextContent;
        string? profilePath = null;
        if (strSteamId64 == null || !ulong.TryParse(strSteamId64, out var steamId64))
        {
            steamId64 = 0;
        }
        else
        {
            var customUrl = root.QuerySelector("customURL")?.TextContent;
            profilePath = string.IsNullOrEmpty(customUrl) ? $"profiles/{steamId64}" : $"id/{customUrl}";
        }

        var result = new FindUserData(steamId64, profilePath);
        return result;
    }

    /// <summary>
    /// 获取徽章信息
    /// </summary>
    /// <param name="steamId"></param>
    /// <returns></returns>
    private async Task<GetBadgesResponse?> GetUserBadges(ulong steamId, CancellationToken cancellation)
    {
        var token = _bot.AccessToken ?? throw new Exception("Bot access token is null");
        var request = new Uri(SteamApiURL, $"/IPlayerService/GetBadges/v1/?steamid={steamId}&access_token={token}");

        var response = await _bot.ArchiWebHandler
            .UrlGetToJsonObjectWithSession<GetBadgesResponse>(request, cancellationToken: cancellation).ConfigureAwait(false);

        return response?.Content;
    }

    /// <summary>
    /// 获取提货人徽章信息
    /// </summary>
    /// <param name="cancellation"></param>
    /// <returns></returns>
    public async Task<UserBadgeInfo?> GetUserBadgeSummary(ulong steamId,
         CancellationToken cancellation = default)
    {
        var request = new Uri(SteamCommunityURL, $"/profiles/{steamId}/badges/?l=schinese");
        var response = await _bot.ArchiWebHandler
            .UrlGetToHtmlDocumentWithSession(request, cancellationToken: cancellation).ConfigureAwait(false);

        var document = response?.Content;
        if (document == null)
        {
            return null;
        }

        var nickname = document.QuerySelector("span.profile_small_header_name > a")?.TextContent.Trim();
        var strLevel = document.QuerySelector("span.profile_xp_block_level span.friendPlayerLevelNum")?.TextContent
            .Trim();
        var strExp = document.QuerySelector("span.profile_xp_block_xp")?.TextContent.Trim().Replace(",", "");

        if (string.IsNullOrEmpty(nickname) || string.IsNullOrEmpty(strLevel) || string.IsNullOrEmpty(strExp))
        {
            return null;
        }

        if (!int.TryParse(strLevel, out var level))
        {
            level = -1;
        }

        var match = RegexUtils.MatchLevelExp.Match(strExp);
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out var exp))
        {
            exp = -1;
        }

        var badges = await GetUserBadges(steamId, cancellation).ConfigureAwait(false);

        var result = new UserBadgeInfo(nickname, level, exp, badges?.Response?.Badges);
        return result;
    }

    /// <summary>
    /// 选择可用套卡
    /// </summary>
    /// <param name="badgeInfo"></param>
    /// <param name="targetSet"></param>
    /// <returns></returns>
    public async Task<LevelCardSetData> SelectFullSetCards(Dictionary<uint, byte> badgeInfo, int targetSet)
    {
        var invCache = await GetCardSetCache(true).ConfigureAwait(false);
        await FullLoadAppCardGroup(invCache).ConfigureAwait(false);

        var sortedInv = invCache
            .Select(static x => x.Value)
            .OrderByDescending(static x => x.TradableSetCount)
            .ToList();

        List<Asset> offer = [];
        var currentSet = 0;
        var cardType = 0;

        foreach (var bundle in sortedInv)
        {
            if (bundle.TradableSetCount == 0)
            {
                continue;
            }

            if (!badgeInfo.TryGetValue(bundle.AppId, out var currentLevel))
            {
                currentLevel = 0;
            }

            var setCount = Math.Min(5 - currentLevel, bundle.TradableSetCount);
            setCount = Math.Min(targetSet - currentSet, setCount);

            if (setCount > 0)
            {
                var flag = bundle.Assets
                    .Select(static x => x.ClassID)
                    .Distinct()
                    .ToDictionary(static x => x, _ => setCount);

                foreach (var asset in bundle.Assets)
                {
                    var clsId = asset.ClassID;
                    if (flag[clsId] > 0)
                    {
                        offer.Add(asset);
                        flag[clsId]--;
                    }
                }

                cardType++;
                currentSet += setCount;

                if (targetSet == currentSet)
                {
                }
            }
        }

        return new LevelCardSetData(currentSet, cardType, offer);
    }
}