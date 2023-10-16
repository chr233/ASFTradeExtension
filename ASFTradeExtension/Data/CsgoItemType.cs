namespace ASFTradeExtension.Data;

/// <summary>
/// 物品类型
/// </summary>
[Flags]
public enum CsgoItemType : byte
{
    /// <summary>
    /// 其他
    /// </summary>
    Other = 1,
    /// <summary>
    /// 武器箱
    /// </summary>
    WeaponCase = 2,
    /// <summary>
    /// 武器皮肤
    /// </summary>
    Weapon = 4,
    /// <summary>
    /// 音乐盒
    /// </summary>
    MusicKit = 8,
    /// <summary>
    /// 收藏品
    /// </summary>
    Collectible = 16,
    /// <summary>
    /// 工具
    /// </summary>
    Tool = 32,
    /// <summary>
    /// 干员
    /// </summary>
    Player = 64,

    /// <summary>
    /// 全部
    /// </summary>
    All = WeaponCase | Weapon | MusicKit | Collectible | Tool,
}

