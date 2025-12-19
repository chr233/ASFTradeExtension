using ArchiSteamFarm.Core;
using ArchiSteamFarm.Helpers.Json;
using ArchiSteamFarm.Plugins.Interfaces;
using ArchiSteamFarm.Steam;
using ASFTradeExtension.Core;
using ASFTradeExtension.Data.Plugin;
using System.ComponentModel;
using System.Composition;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace ASFTradeExtension;

[Export(typeof(IPlugin))]
internal sealed class ASFTradeExtension : IASF, IBot, IBotCommand2, IGitHubPluginUpdates
{
    private const string ShortName = "ATE";

    private bool ASFEBridge;

    private Timer? StatisticTimer;
    public static PluginConfig Config => Utils.Config;

    /// <summary>
    /// 获取插件信息
    /// </summary>
    private string PluginInfo => $"{Name} ({ShortName}) {Version}";

    public string Name => "ASF Trade Extension";
    public Version Version => MyVersion;

    public bool CanUpdate => true;
    public string RepositoryName => "chr233/ASFTradeExtension";

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
                if (configProperty == "ASFEnhance" && configValue.ValueKind == JsonValueKind.Object)
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

        Utils.Config = config ?? new PluginConfig();

        var sb = new StringBuilder();

        //使用协议
        if (!Config.EULA)
        {
            sb.AppendLine();
            sb.AppendLine(Langs.Line);
            sb.AppendLineFormat(Langs.EulaWarning, Name);
            sb.AppendLine(Langs.Line);
        }

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
                async void (_) =>
                {
                    try
                    {
                        await ASF.WebBrowser!.UrlGetToHtmlDocument(request).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        ASFLogger.LogGenericException(ex);
                    }
                },
                null,
                TimeSpan.FromSeconds(180),
                TimeSpan.FromHours(24)
            );
        }

        return CardSetCache.SaveCacheFile();
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

        return CardSetCache.LoadCacheFile();
    }

    public Task OnBotDestroy(Bot bot)
    {
        CoreHandlers.TryRemove(bot, out _);
        return Task.CompletedTask;
    }

    public Task OnBotInit(Bot bot)
    {
        var botHandler = new InventoryHandler(bot);
        CoreHandlers.TryAdd(bot, botHandler);
        return Task.CompletedTask;
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

            if (cmd.StartsWith($"{ShortName}."))
            {
                cmd = cmd[4..];
            }

            var task = ResponseCommand(bot, access, cmd, args);
            if (task != null)
            {
                return await task.ConfigureAwait(false);
            }

            return null;
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

    /// <summary>
    /// 处理命令
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="access"></param>
    /// <param name="cmd"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private Task<string>? ResponseCommand(Bot bot, EAccess access, string cmd, string[] args)
    {
        var argLength = args.Length;

        var autoConfirm = false;
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
                "ASFTRADEEXTENSION" or
                "ATE" when access >= EAccess.FamilySharing =>
                    Task.FromResult(PluginInfo),

                "GETMASTERBOT" or
                "GETMASTER" or
                "GM" when access >= EAccess.Master =>
                    Task.FromResult(Command.ResponseGetMasterBot()),

                "SETMASTERBOT" or
                "SETMASTER" or
                "SM" when access >= EAccess.Master =>
                    Command.ResponseSetMasterBot(bot),


                //获取卡牌信息
                "FULLSETLIST" or
                "FSL" when access >= EAccess.Master =>
                    Command.ResponseFullSetList(null, false),

                "FULLSETLISTFOIL" or
                "FSLF" when access >= EAccess.Master =>
                    Command.ResponseFullSetList(null, true),

                "FULLSETLISTSALE" or
                "FSLS" when access >= EAccess.Master =>
                    Command.ResponseFullSetListSaleEvent(),

                //获取宝珠信息
                "GEMSINFO" or
                "GI" when access >= EAccess.Master =>
                    Command.ResponseGemsInfo(),

                //重新加载库存
                "RELOADCACHE" when access >= EAccess.Master =>
                    Command.ResponseReloadCache(),

                //清除库存缓存
                "CLEARCACHE" when access >= EAccess.Master =>
                    Command.ResponseClearCache(),

                _ => null
            },
            _ => cmd switch //带参数
            {
                "SETMASTERBOT" or
                "SETMASTER" or
                "SM" when access >= EAccess.Master =>
                   Command.ResponseSetMasterBot(Utilities.GetArgsAsText(args, 1, ",")),

                //获取卡牌信息
                "FULLSETLIST" or
                "FSL" when access >= EAccess.Master && argLength % 2 != 0 =>
                    Command.ResponseFullSetList(Utilities.GetArgsAsText(args, 1, ","), false),

                "FULLSETLISTFOIL" or
                "FSLF" when access >= EAccess.Master && argLength % 2 != 0 =>
                    Command.ResponseFullSetList(Utilities.GetArgsAsText(args, 1, ","), true),

                //获取指定游戏卡牌套数
                "FULLSET" or
                "FS" when access >= EAccess.Master =>
                    Command.ResponseFullSetCountOfGame(Utilities.GetArgsAsText(args, 1, ","), false),

                "FULLSETFOIL" or
                "FSF" when access >= EAccess.Master =>
                    Command.ResponseFullSetCountOfGame(Utilities.GetArgsAsText(args, 1, ","), true),

                //发送套卡给机器人
                "SENDCARDSETBOT" or
                "SCSB" when access >= EAccess.Master && argLength == 4 =>
                    Command.ResponseSendCardSetBot(args[1], args[2], args[3], autoConfirm, false),

                //发送套卡给交易链接
                "SENDCARDSET" or
                "SCS" when access >= EAccess.Master && argLength == 4 =>
                    Command.ResponseSendCardSet(args[1], args[2], args[3], autoConfirm, false),

                //发送闪卡套卡给机器人
                "SENDCARDSETBOTFOIL" or
                "SCSBF" when access >= EAccess.Master && argLength == 4 =>
                    Command.ResponseSendCardSetBot(args[1], args[2], args[3], autoConfirm, true),

                //发送闪卡套卡给交易链接
                "SENDCARDSETFOIL" or
                "SCSF" when access >= EAccess.Master && argLength == 4 =>
                    Command.ResponseSendCardSet(args[1], args[2], args[3], autoConfirm, true),

                //发送宝珠给机器人
                "SENDGEMSBOT" or
                "SGB" when access >= EAccess.Master && argLength == 3 =>
                    Command.ResponseSendGemsBot(args[1], args[2], autoConfirm),

                //发送宝珠给交易链接
                "SENDGEMS" or
                "SG" when access >= EAccess.Master && argLength == 3 =>
                    Command.ResponseSendGems(args[1], args[2], autoConfirm),

                //发送成套卡牌
                "SENDLEVELUP" or
                "SLU" when argLength == 3 && access >= EAccess.Master =>
                    Command.ResponseSendLevelUpTrade(args[1], args[2], autoConfirm),

                "SENDLEVELUPSET" or
                "SLUS" when argLength == 3 && access >= EAccess.Master =>
                    Command.ResponseSendLevelUpTradeSet(args[1], args[2], autoConfirm),

                //重新加载库存
                "RELOADCACHE" when access >= EAccess.Operator =>
                    Command.ResponseReloadCache(),

                //清除库存缓存
                "CLEARCACHE" when access >= EAccess.Master =>
                    Command.ResponseClearCache(),

                //转移库存
                "TRANSFEREX" when access >= EAccess.Operator && argLength == 3 =>
                    Command.ResponseTransferEx(bot, args[1], args[2], autoConfirm),

                "TRANSFEREX^" when access >= EAccess.Operator && argLength == 4 =>
                    Command.ResponseTransferEx(bot, args[1], args[2], args[3], autoConfirm),

                _ => null
            }
        };
    }
}