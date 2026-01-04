using AngleSharp;
using AngleSharp.Dom;
using ASFTradeExtension.Data.Core;
using System.Text;

namespace ASFTradeExtension.Core;

internal static class WebRequsets
{
    /// <summary>
    /// 查找用户
    /// </summary>
    /// <param name="steamId"></param>
    /// <param name="cancel"></param>
    /// <returns></returns>
    public static async Task<FindUserData?> FindSteamUser(ulong steamId, CancellationToken cancel = default)
    {
        try
        {
            var request = $"/profiles/{steamId}?xml=1";
            var response = await MakeRequest(HttpMethod.Get, request).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var xml = await response.Content.ReadAsStringAsync(cancel).ConfigureAwait(false);
            if (string.IsNullOrEmpty(xml))
            {
                _logger.LogWarning("Response is null");
                return null;
            }

            var parser = new XmlParser();
            var document = parser.ParseDocument(xml);

            var root = document.QuerySelector("profile");

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

            var avatar = root.QuerySelector("avatarFull")?.TextContent;
            var nickname = root.QuerySelector("steamID")?.TextContent;

            var result = new FindUserData(steamId64, avatar, nickname, profilePath);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FetchBadgePage Error");
            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// 获取提货人徽章信息
    /// </summary>
    /// <param name="profilePath"></param>
    /// <param name="page"></param>
    /// <param name="cancel"></param>
    /// <returns></returns>
    private static async Task<IDocument?> FetchBadgePage(string profilePath, int page = 1, CancellationToken cancel = default)
    {
        await _semaphore.WaitAsync(cancel).ConfigureAwait(false);

        try
        {
            var request = $"/{profilePath}/badges/?sort=c&p={page}&l=schinese";
            var response = await MakeRequest(HttpMethod.Get, request).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cancel).ConfigureAwait(false);
            var html = Encoding.UTF8.GetString(bytes);

            if (string.IsNullOrEmpty(html))
            {
                _logger.LogWarning("Response is null");
                return null;
            }

            var context = BrowsingContext.New(Configuration.Default);
            var document = await context.OpenAsync(req => req.Content(html), cancel: cancel).ConfigureAwait(false);

            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FetchBadgePage Error");
            await Task.Delay(500, cancel).ConfigureAwait(false);
            return null;
        }
        finally
        {
            _semaphore.Release();
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

            var match = MatchGameCards.Match(url);
            if (!match.Success || !uint.TryParse(match.Groups[1].Value, out var appId))
            {
                continue;
            }

            var strLevel = ele.QuerySelector(".badge_info_description>div:nth-child(2)")?.TextContent.Trim() ?? "";
            match = MatchBadgeLevel.Match(strLevel);

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
    public static async Task<UserBadgeInfo?> GetUserBadgeSummary(ulong steamId, bool fullLoad = false, CancellationToken cancellation = default)
    {
        var document = await FetchBadgePage(profilePath, 1, cancellation).ConfigureAwait(false);

        if (document == null)
        {
            return null;
        }

        var nickname = document.QuerySelector("span.profile_small_header_name > a")?.TextContent.Trim();
        var strLevel = document.QuerySelector("span.profile_xp_block_level span.friendPlayerLevelNum")?.TextContent.Trim();
        var strExp = document.QuerySelector("span.profile_xp_block_xp")?.TextContent.Trim().Replace(",", "");

        if (string.IsNullOrEmpty(nickname) || string.IsNullOrEmpty(strLevel) || string.IsNullOrEmpty(strExp))
        {
            return null;
        }

        if (!int.TryParse(strLevel, out var level))
        {
            level = -1;
        }

        var match = MatchLevelExp.Match(strExp);
        if (!match.Success || !long.TryParse(match.Groups[1].Value, out var exp))
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

                var badgeDocuments = await Task.WhenAll(tasks).ConfigureAwait(false);
                foreach (var bd in badgeDocuments)
                {
                    if (bd != null)
                    {
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
        }

        var result = new UserBadgeSummary(nickname, level, exp, totalPage, fullLoad, badges);
        return result;
    }
}
