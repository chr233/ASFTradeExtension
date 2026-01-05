namespace ASFTradeExtension.Data.Core;

/// <summary>
/// 个人资料数据
/// </summary>
public sealed record UserBadgeInfo
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="nickname"></param>
    /// <param name="level"></param>
    /// <param name="experience"></param>
    /// <param name="totalPages"></param>
    /// <param name="fullLoaded"></param>
    /// <param name="badges"></param>
    public UserBadgeInfo(string? nickname, int level, int experience, List<BadgeData>? badges)
    {
        Nickname = nickname;
        Level = level;
        Experience = experience;

        Badges = [];
        if (badges != null)
        {
            foreach (var badge in badges)
            {
                if (badge.AppId > 0 && badge.BorderColor == 0 && badge.Level < byte.MaxValue)
                {
                    Badges[badge.AppId] = (byte)badge.Level;
                }
            }
        }
    }

    /// <summary>
    /// 昵称
    /// </summary>
    public string? Nickname { get; init; }

    /// <summary>
    /// 等级
    /// </summary>
    public int Level { get; init; }

    /// <summary>
    /// 经验
    /// </summary>
    public int Experience { get; init; }

    /// <summary>
    /// 全部徽章页数
    /// </summary>
    public int TotalPages { get; init; }

    /// <summary>
    /// 已加载徽章页数
    /// </summary>
    public bool FullLoaded { get; init; }

    /// <summary>
    /// 徽章等级
    /// </summary>
    public Dictionary<uint, byte> Badges { get; init; }
}