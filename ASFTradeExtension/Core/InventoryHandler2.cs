using AngleSharp.Dom;
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
    /// 获取提货人徽章信息
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="profilePath"></param>
    /// <param name="page"></param>
    /// <param name="cancel"></param>
    /// <returns></returns>
    private async Task<IDocument?> FetchBadgePage(string profilePath, int page = 1, CancellationToken cancel = default)
    {
        try
        {
            var request = new Uri(SteamCommunityURL, $"/{profilePath}/badges/?sort=c&p={page}&l=schinese");
            var response = await _bot.ArchiWebHandler
                .UrlGetToHtmlDocumentWithSession(request, cancellationToken: cancel).ConfigureAwait(false);

            return response?.Content;
        }
        catch (Exception ex)
        {
            ASFLogger.LogGenericException(ex);
            return null;
        }
    }

    /// <summary>
    /// 提取徽章信息
    /// </summary>
    /// <param name="document"></param>
    /// <returns></returns>
    private static Dictionary<uint, byte> ParseToBadges(IDocument document)
    {
        Dictionary<uint, byte> badges = [];

        var badgeEles = document.QuerySelectorAll("div.badge_row.is_link");
        foreach (var ele in badgeEles)
        {
            var url = ele.QuerySelector("a.badge_row_overlay")?.GetAttribute("href");
            if (url == null)
            {
                continue;
            }

            var match = RegexUtils.MatchGameCards.Match(url);
            if (!match.Success || !uint.TryParse(match.Groups[1].Value, out var appId))
            {
                continue;
            }

            var strLevel = ele.QuerySelector(".badge_info_description>div:nth-child(2)")?.TextContent.Trim() ?? "";
            match = RegexUtils.MatchBadgeLevel.Match(strLevel);

            if (!match.Success || !byte.TryParse(match.Groups[1].Value, out var level))
            {
                continue;
            }

            badges.TryAdd(appId, level);
        }

        return badges;
    }

    /// <summary>
    /// 获取提货人徽章信息
    /// </summary>
    /// <param name="profilePath"></param>
    /// <param name="fullLoad"></param>
    /// <param name="cancellation"></param>
    /// <returns></returns>
    public async Task<UserBadgeInfo?> GetUserBadgeSummary(string profilePath, bool fullLoad = false,
        CancellationToken cancellation = default)
    {
        var document = await FetchBadgePage(profilePath, 1, cancellation).ConfigureAwait(false);

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

        var badges = ParseToBadges(document);

        var strMaxPage = document.QuerySelector("div.pageLinks>a.pagelink:nth-last-child(2)")?.TextContent.Trim();
        if (!int.TryParse(strMaxPage, out var totalPage))
        {
            totalPage = 1;
        }

        // 加载后续页面
        if (fullLoad)
        {
            if (totalPage > 1)
            {
                List<Task<IDocument?>> tasks = [];
                for (var page = 2; page <= totalPage; page++)
                {
                    tasks.Add(FetchBadgePage(profilePath, page, cancellation));
                }

                var badgeDocuments = await Utilities.InParallel(tasks).ConfigureAwait(false);
                foreach (var bd in badgeDocuments)
                {
                    if (bd == null)
                    {
                        continue;
                    }

                    var bs = ParseToBadges(bd);
                    if (bs?.Count > 0)
                    {
                        foreach (var (k, v) in bs)
                        {
                            badges.TryAdd(k, v);
                        }
                    }
                }
            }
        }

        var result = new UserBadgeInfo(nickname, level, exp, totalPage, fullLoad, badges);
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
        var invCache = await GetCardSetCache(false).ConfigureAwait(false);
        await FullLoadAppCardGroup(invCache).ConfigureAwait(false);

        var sortedInv = invCache
            .Select(static x => x.Value)
            .OrderByDescending(static x => x.TradableSetCount);

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