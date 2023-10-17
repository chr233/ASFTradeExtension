using ArchiSteamFarm.Core;
using ArchiSteamFarm.Plugins.Interfaces;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;
using ArchiSteamFarm.Steam.Exchange;
using ASFTradeExtension.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Composition;
using System.Reflection;
using System.Text;

namespace ASFTradeExtension;

[Export(typeof(IPlugin))]
internal sealed class ASFTradeExtension : IASF, IBotCommand2, IBotTradeOffer, IBotTradeOfferResults
{
    public string Name => "ASF Trade Extension";
    public Version Version => MyVersion;

    private bool ASFEBridge;

    [JsonProperty]
    public static PluginConfig Config => Utils.Config;

    private Timer? StatisticTimer;

    /// <summary>
    /// ASF启动事件
    /// </summary>
    /// <param name="additionalConfigProperties"></param>
    /// <returns></returns>
    public Task OnASFInit(IReadOnlyDictionary<string, JToken>? additionalConfigProperties = null)
    {

        PluginConfig? config = null;

        if (additionalConfigProperties != null)
        {
            foreach ((string configProperty, JToken configValue) in additionalConfigProperties)
            {
                if ((configProperty == "ASFEnhance") && configValue.Type == JTokenType.Object)
                {
                    try
                    {
                        config = configValue.ToObject<PluginConfig>();
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

        //使用协议
        if (!Config.EULA)
        {
            sb.AppendLine();
            sb.AppendLine(Langs.Line);
            sb.AppendLine(Langs.EulaWarning);
            sb.AppendLine(Langs.Line);
        }

        if (sb.Length > 0)
        {
            ASFLogger.LogGenericWarning(sb.ToString());
        }
        //统计
        if (Config.Statistic)
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

        ASFEBridge = AdapterBtidge.InitAdapter(Name, pluginId, cmdPrefix, repoName, handler);

        if (ASFEBridge)
        {
            ASFLogger.LogGenericDebug(Langs.ASFEnhanceRegisterSuccess);
        }
        else
        {
            ASFLogger.LogGenericInfo(Langs.ASFEnhanceRegisterFailed);
            ASFLogger.LogGenericWarning(Langs.PluginStandalongMode);
        }

        return Task.CompletedTask;
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
        return argLength switch
        {
            0 => throw new InvalidOperationException(nameof(args)),
            1 => cmd switch //不带参数
            {
                //Plugin Info
                "ASFTRADEXTENSION" or
                "ATE" when access >= EAccess.FamilySharing =>
                    Task.FromResult(PluginInfo),

                //Card
                "FULLSETLIST" or
                "FSL" when access >= EAccess.Operator =>
                    Card.Command.ResponseFullSetList(bot, null),

                //CSGO
                "CSITEMLIST" or
                "CIL" when access >= EAccess.Operator =>
                    Csgo.Command.ResponseCsItemList(bot, null),

                "CSSENDITEM" or
                "CSI" when access >= EAccess.Master =>
                    Csgo.Command.ResponseSendCsItem(bot, null, null, false),
                "2CSSENDITEM" or
                "2CSI" when access >= EAccess.Master =>
                    Csgo.Command.ResponseSendCsItem(bot, null, null, true),

                "CSMARKETHISTORY" or
                "CMH" when access >= EAccess.Operator =>
                    Csgo.Command.ResponseGetCsMarketInfo(bot, null),

                "CSDELISTING" or
                "CDL" when access >= EAccess.Master =>
                    Csgo.Command.ResponseCsRemoveListing(bot, null),

                _ => null,
            },
            _ => cmd switch //带参数
            {
                //Card
                "FULLSETLIST" or
                "FSL" when access >= EAccess.Operator && argLength == 2 =>
                    Card.Command.ResponseFullSetList(args[1], null),
                "FULLSETLIST" or
                "FSL" when access >= EAccess.Operator && argLength % 2 == 0 =>
                    Card.Command.ResponseFullSetList(args[1], Utilities.GetArgsAsText(args, 2, ",")),
                "FULLSETLIST" or
                "FSL" when access >= EAccess.Operator && argLength % 2 != 0 =>
                    Card.Command.ResponseFullSetList(bot, Utilities.GetArgsAsText(args, 1, ",")),

                "FULLSET" or
                "FS" when argLength >= 3 && access >= EAccess.Operator =>
                    Card.Command.ResponseFullSetCountOfGame(args[1], Utilities.GetArgsAsText(args, 1, ",")),
                "FULLSET" or
                "FS" when access >= EAccess.Operator =>
                    Card.Command.ResponseFullSetCountOfGame(bot, args[1]),

                "SENDCARDSET" or
                "SCS" when access >= EAccess.Master && argLength == 5 =>
                    Card.Command.ResponseSendCardSet(args[1], args[2], args[3], args[4], false),
                "SENDCARDSET" or
                "SCS" when access >= EAccess.Master && argLength == 4 =>
                    Card.Command.ResponseSendCardSet(bot, args[1], args[2], args[3], false),

                "2SENDCARDSET" or
                "2SCS" when access >= EAccess.Master && argLength == 5 =>
                    Card.Command.ResponseSendCardSet(args[1], args[2], args[3], args[4], true),
                "2SENDCARDSET" or
                "2SCS" when access >= EAccess.Master && argLength == 4 =>
                    Card.Command.ResponseSendCardSet(bot, args[1], args[2], args[3], true),

                //CSGO
                "CSITEMLIST" or
                "CIL" when access >= EAccess.Operator && argLength == 2 =>
                    Csgo.Command.ResponseCsItemList(args[1], null),
                "CSITEMLIST" or
                "CIL" when access >= EAccess.Operator && argLength % 2 == 0 =>
                    Csgo.Command.ResponseCsItemList(args[1], Utilities.GetArgsAsText(args, 2, ",")),
                "CSITEMLIST" or
                "CIL" when access >= EAccess.Operator && argLength % 2 != 0 =>
                    Csgo.Command.ResponseCsItemList(bot, Utilities.GetArgsAsText(args, 1, ",")),


                "CSSENDITEM" or
                "CSI" when access >= EAccess.Master && argLength == 4 =>
                    Csgo.Command.ResponseSendCsItem(args[1], args[2], args[3], false),
                "CSSENDITEM" or
                "CSI" when access >= EAccess.Master && argLength == 3 =>
                    Csgo.Command.ResponseSendCsItem(bot, args[1], args[2], false),
                "CSSENDITEM" or
                "CSI" when access >= EAccess.Master && argLength == 2 =>
                    Csgo.Command.ResponseSendCsItem(args[1], null, null, false),


                "2CSSENDITEM" or
                "2CSI" when access >= EAccess.Master && argLength == 4 =>
                    Csgo.Command.ResponseSendCsItem(args[1], args[2], args[3], true),
                "2CSSENDITEM" or
                "2CSI" when access >= EAccess.Master && argLength == 3 =>
                    Csgo.Command.ResponseSendCsItem(bot, args[1], args[2], true),
                "2CSSENDITEM" or
                "2CSI" when access >= EAccess.Master && argLength == 2 =>
                    Csgo.Command.ResponseSendCsItem(args[1], null, null, true),


                "CSSELLITEM" or
                "CEI" when access >= EAccess.Master && argLength == 5 =>
                    Csgo.Command.ResponseSellCsItem(args[1], args[2], args[3], args[4], false),
                "CSSELLITEM" or
                "CEI" when access >= EAccess.Master && argLength == 4 =>
                    Csgo.Command.ResponseSellCsItem(bot, args[1], args[2], args[3], false),

                "2CSSELLITEM" or
                "2CEI" when access >= EAccess.Master && argLength == 5 =>
                    Csgo.Command.ResponseSellCsItem(args[1], args[2], args[3], args[4], true),
                "2CSSELLITEM" or
                "2CEI" when access >= EAccess.Master && argLength == 4 =>
                    Csgo.Command.ResponseSellCsItem(bot, args[1], args[2], args[3], true),

                "CSMARKETHISTORY" or
                "CMH" when access >= EAccess.Operator =>
                    Csgo.Command.ResponseGetCsMarketInfo(SkipBotNames(args, 1, 1), args.Last()),


                "CSDELISTING" or
                "CDL" when access >= EAccess.Master && argLength >= 3 =>
                    Csgo.Command.ResponseCsRemoveListing(SkipBotNames(args, 1, 1), args.Last()),
                "CSDELISTING" or
                  "CDL" when access >= EAccess.Master =>
                    Csgo.Command.ResponseCsRemoveListing(bot, args[1]),

                "TRANSFERCSGO" or
                "TRC" when argLength == 3 && access >= EAccess.Master =>
                    Csgo.Command.ResponseBotStatus(args[1], args[2], null),
                "TRANSFERCSGO" or
                "TRC" when argLength == 4 && access >= EAccess.Master =>
                    Csgo.Command.ResponseBotStatus(args[1], args[2], args[3]),

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
                Utils.ASFLogger.LogGenericException(ex);
            }).ConfigureAwait(false);

            return ex.StackTrace;
        }
    }

    public Task<bool> OnBotTradeOffer(Bot bot, TradeOffer tradeOffer)
    {
        bool accept = Csgo.Handler.IsMyTrade(tradeOffer.TradeOfferID, tradeOffer.OtherSteamID64);
        ASFLogger.LogGenericWarning(string.Format("交易Id: {0}, {1}", tradeOffer.TradeOfferID, accept));
        return Task.FromResult(accept);
    }

    public Task OnBotTradeOfferResults(Bot bot, IReadOnlyCollection<ParseTradeResult> tradeResults)
    {
        foreach (var tradeResult in tradeResults)
        {
            Csgo.Handler.RemoveMyTrade(tradeResult.TradeOfferID);
            ASFLogger.LogGenericWarning(string.Format("交易Id: {0}, {1}", tradeResult.TradeOfferID, tradeResult.Result));
        }
        return Task.CompletedTask;
    }
}
