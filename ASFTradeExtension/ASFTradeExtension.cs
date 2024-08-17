using ArchiSteamFarm.Core;
using ArchiSteamFarm.Helpers.Json;
using ArchiSteamFarm.Plugins.Interfaces;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Web.GitHub.Data;
using ASFTradeExtension.Core;
using ASFTradeExtension.Data;
using System.ComponentModel;
using System.Composition;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace ASFTradeExtension;

[Export(typeof(IPlugin))]
internal sealed class ASFTradeExtension : IASF, IBot, IBotCommand2, IGitHubPluginUpdates
{
    public string Name => "ASF Trade Extension";
    public Version Version => MyVersion;
    public bool CanUpdate => true;
    public string RepositoryName => "chr233/ASFEnhance";

    private bool ASFEBridge;
    public static PluginConfig Config => Utils.Config;

    private Timer? StatisticTimer;

    /// <summary>
    /// ASF启动事件
    /// </summary>
    /// <param name="additionalConfigProperties"></param>
    /// <returns></returns>
    public Task OnASFInit(IReadOnlyDictionary<string, JsonElement>? additionalConfigProperties = null)
    {

        PluginConfig? config = null;

        if (additionalConfigProperties != null)
        {
            foreach (var (configProperty, configValue) in additionalConfigProperties)
            {
                if ((configProperty == "ASFEnhance") && configValue.ValueKind == JsonValueKind.Object)
                {
                    try
                    {
                        config = configValue.ToJsonObject<PluginConfig>();
                        if (config != null)
                        {
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        ASFLogger.LogGenericException(ex);
                    }
                }
            }
        }

        Utils.Config = config ?? new();

        var sb = new StringBuilder();

        if (Config.MaxItemPerTrade < byte.MaxValue)
        {
            Config.MaxItemPerTrade = byte.MaxValue;
        }

        if (Config.CacheTTL < 300)
        {
            Config.CacheTTL = 300;
        }

        if (sb.Length > 0)
        {
            ASFLogger.LogGenericWarning(sb.ToString());
        }

        //统计
        if (Config.Statistic && !ASFEBridge)
        {
            var request = new Uri("https://asfe.chrxw.com/asftradeextension");
            StatisticTimer = new Timer(
                async (_) =>
                {
                    await ASF.WebBrowser!.UrlGetToHtmlDocument(request).ConfigureAwait(false);
                },
                null,
                TimeSpan.FromSeconds(30),
                TimeSpan.FromHours(24)
            );
        }


        return Task.CompletedTask;
    }

    /// <summary>
    /// 插件加载事件
    /// </summary>
    /// <returns></returns>
    public Task OnLoaded()
    {
        ASFLogger.LogGenericInfo(Langs.PluginContact);
        ASFLogger.LogGenericInfo(Langs.PluginInfo);

        var flag = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        var handler = typeof(ASFTradeExtension).GetMethod(nameof(ResponseCommand), flag);

        const string pluginId = nameof(ASFTradeExtension);
        const string cmdPrefix = "ATE";
        const string repoName = "ASFTradeExtension";

        ASFEBridge = AdapterBridge.InitAdapter(Name, pluginId, cmdPrefix, repoName, handler);

        if (ASFEBridge)
        {
            ASFLogger.LogGenericDebug(Langs.ASFEnhanceRegisterSuccess);
        }
        else
        {
            ASFLogger.LogGenericInfo(Langs.ASFEnhanceRegisterFailed);
            ASFLogger.LogGenericWarning(Langs.PluginStandalongMode);
        }

        return Utils.CardSetCache.LoadCacheFile();
    }

    /// <summary>
    /// 获取插件信息
    /// </summary>
    private static string? PluginInfo => string.Format("{0} {1}", nameof(ASFTradeExtension), MyVersion);

    /// <summary>
    /// 处理命令
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="access"></param>
    /// <param name="cmd"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private static Task<string?>? ResponseCommand(Bot bot, EAccess access, string cmd, string[] args)
    {
        int argLength = args.Length;

        bool autoConfirm = false;
        if (cmd.StartsWith('2'))
        {
            autoConfirm = true;
            cmd = cmd[1..];
        }

        return argLength switch
        {
            0 => throw new InvalidOperationException(nameof(args)),
            1 => cmd switch //不带参数
            {
                //插件信息
                "ASFTradeExtension" or
                "ATE" when access >= EAccess.FamilySharing =>
                    Task.FromResult(PluginInfo),

                //获取卡牌信息
                "FULLSETLIST" or
                "FSL" when access >= EAccess.Operator =>
                    Card.Command.ResponseFullSetList(bot, null, false),

                "FULLSETLISTFOIL" or
                "FSLF" when access >= EAccess.Operator =>
                    Card.Command.ResponseFullSetList(bot, null, true),

                "FULLSETLISTSALE" or
                "FSLS" when access >= EAccess.Operator =>
                    Card.Command.ResponseFullSetListSaleEvent(bot),

                //获取宝珠信息
                "GEMSINFO" or
                "GI" when access >= EAccess.Operator =>
                    Card.Command.ResponseGemsInfo(bot),

                //重新加载库存
                "RELOADCACHE" when access >= EAccess.Operator =>
                    Card.Command.ResponseReloadCache(bot),

                _ => null,
            },
            _ => cmd switch //带参数
            {
                //获取卡牌信息
                "FULLSETLIST" or
                "FSL" when access >= EAccess.Operator && argLength == 2 =>
                    Card.Command.ResponseFullSetList(args[1], null, false),
                "FULLSETLIST" or
                "FSL" when access >= EAccess.Operator && argLength % 2 == 0 =>
                    Card.Command.ResponseFullSetList(args[1], Utilities.GetArgsAsText(args, 2, ","), false),
                "FULLSETLIST" or
                "FSL" when access >= EAccess.Operator && argLength % 2 != 0 =>
                    Card.Command.ResponseFullSetList(bot, Utilities.GetArgsAsText(args, 1, ","), false),

                "FULLSETLISTFOIL" or
                "FSLF" when access >= EAccess.Operator && argLength == 2 =>
                    Card.Command.ResponseFullSetList(args[1], null, true),
                "FULLSETLISTFOIL" or
                "FSLF" when access >= EAccess.Operator && argLength % 2 == 0 =>
                    Card.Command.ResponseFullSetList(args[1], Utilities.GetArgsAsText(args, 2, ","), true),
                "FULLSETLISTFOIL" or
                "FSLF" when access >= EAccess.Operator && argLength % 2 != 0 =>
                    Card.Command.ResponseFullSetList(bot, Utilities.GetArgsAsText(args, 1, ","), true),

                "FULLSETLISTSALE" or
                "FSLS" when access >= EAccess.Operator =>
                    Card.Command.ResponseFullSetListSaleEvent(Utilities.GetArgsAsText(args, 1, ",")),

                //获取指定游戏卡牌套数
                "FULLSET" or
                "FS" when argLength >= 3 && access >= EAccess.Operator =>
                    Card.Command.ResponseFullSetCountOfGame(args[1], Utilities.GetArgsAsText(args, 2, ","), false),
                "FULLSET" or
                "FS" when access >= EAccess.Operator =>
                    Card.Command.ResponseFullSetCountOfGame(bot, args[1], false),

                "FULLSETFOIL" or
                "FSF" when argLength >= 3 && access >= EAccess.Operator =>
                    Card.Command.ResponseFullSetCountOfGame(args[1], Utilities.GetArgsAsText(args, 2, ","), true),
                "FULLSETFOIL" or
                "FSF" when access >= EAccess.Operator =>
                    Card.Command.ResponseFullSetCountOfGame(bot, args[1], true),

                //获取宝珠信息
                "GEMSINFO" or
                "GI" when access >= EAccess.Operator =>
                    Card.Command.ResponseGemsInfo(Utilities.GetArgsAsText(args, 1, ",")),

                //发送套卡给机器人
                "SENDCARDSETBOT" or
                "SCSB" when access >= EAccess.Master && argLength == 5 =>
                    Card.Command.ResponseSendCardSet(args[1], args[2], args[3], args[4], autoConfirm, false),
                "SENDCARDSETBOT" or
                "SCSB" when access >= EAccess.Master && argLength == 4 =>
                    Card.Command.ResponseSendCardSetBot(bot, args[1], args[2], args[3], autoConfirm, false),

                //发送套卡给交易链接
                "SENDCARDSET" or
                "SCS" when access >= EAccess.Master && argLength == 5 =>
                    Card.Command.ResponseSendCardSet(args[1], args[2], args[3], args[4], autoConfirm, false),
                "SENDCARDSET" or
                "SCS" when access >= EAccess.Master && argLength == 4 =>
                    Card.Command.ResponseSendCardSet(bot, args[1], args[2], args[3], autoConfirm, false),

                //发送闪卡套卡给机器人
                "SENDCARDSETBOTFOIL" or
                "SCSBF" when access >= EAccess.Master && argLength == 5 =>
                    Card.Command.ResponseSendCardSetBot(args[1], args[2], args[3], args[4], autoConfirm, true),
                "SENDCARDSETBOTFOIL" or
                "SCSBF" when access >= EAccess.Master && argLength == 4 =>
                    Card.Command.ResponseSendCardSetBot(bot, args[1], args[2], args[3], autoConfirm, true),

                //发送闪卡套卡给交易链接
                "SENDCARDSETFOIL" or
                "SCSF" when access >= EAccess.Master && argLength == 5 =>
                    Card.Command.ResponseSendCardSet(args[1], args[2], args[3], args[4], autoConfirm, true),
                "SENDCARDSETFOIL" or
                "SCSF" when access >= EAccess.Master && argLength == 4 =>
                    Card.Command.ResponseSendCardSet(bot, args[1], args[2], args[3], autoConfirm, true),

                //发送宝珠给机器人
                "SENDGEMSBOT" or
                "SGB" when access >= EAccess.Master && argLength == 4 =>
                    Card.Command.ResponseSendGemsBot(args[1], args[2], args[3], autoConfirm),
                "SENDGEMSBOT" or
                "SGB" when access >= EAccess.Master && argLength == 3 =>
                    Card.Command.ResponseSendGemsBot(bot, args[1], args[2], autoConfirm),

                //发送宝珠给交易链接
                "SENDGEMS" or
                "SG" when access >= EAccess.Master && argLength == 4 =>
                    Card.Command.ResponseSendGems(args[1], args[2], args[3], autoConfirm),
                "SENDGEMS" or
                "SG" when access >= EAccess.Master && argLength == 3 =>
                    Card.Command.ResponseSendGems(bot, args[1], args[2], autoConfirm),

                //重新加载库存
                "RELOADCACHE" when access >= EAccess.Operator =>
                    Card.Command.ResponseReloadCache(Utilities.GetArgsAsText(args, 1, ",")),

                _ => null,
            }
        };
    }

    /// <summary>
    /// 处理命令事件
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="access"></param>
    /// <param name="message"></param>
    /// <param name="args"></param>
    /// <param name="steamId"></param>
    /// <returns></returns>
    /// <exception cref="InvalidEnumArgumentException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<string?> OnBotCommand(Bot bot, EAccess access, string message, string[] args, ulong steamId = 0)
    {
        if (ASFEBridge)
        {
            return null;
        }

        if (!Enum.IsDefined(access))
        {
            throw new InvalidEnumArgumentException(nameof(access), (int)access, typeof(EAccess));
        }

        try
        {
            var cmd = args[0].ToUpperInvariant();

            if (cmd.StartsWith("ATE."))
            {
                cmd = cmd[4..];
            }

            var task = ResponseCommand(bot, access, cmd, args);
            if (task != null)
            {
                return await task.ConfigureAwait(false);
            }
            else
            {
                return null;
            }
        }
        catch (Exception ex)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(500).ConfigureAwait(false);
                ASFLogger.LogGenericException(ex);
            }).ConfigureAwait(false);

            return ex.StackTrace;
        }
    }

    public Task OnBotDestroy(Bot bot)
    {
        Card.Command.Handlers.TryRemove(bot, out var _);
        return Task.CompletedTask;
    }

    public Task OnBotInit(Bot bot)
    {
        var botHandler = new InventoryHandler(bot);
        Card.Command.Handlers.TryAdd(bot, botHandler);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<ReleaseAsset?> GetTargetReleaseAsset(Version asfVersion, string asfVariant, Version newPluginVersion, IReadOnlyCollection<ReleaseAsset> releaseAssets)
    {
        var result = releaseAssets.Count switch
        {
            0 => null,
            1 => //如果找到一个文件，则第一个
                releaseAssets.First(),
            _ => //优先下载当前语言的版本
                releaseAssets.FirstOrDefault(static x => x.Name.Contains(Langs.CurrentLanguage)) ??
                releaseAssets.FirstOrDefault(static x => x.Name.Contains("en-US"))
        };

        return Task.FromResult(result);
    }
}
