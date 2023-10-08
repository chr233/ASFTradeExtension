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
using System.Text;

namespace ASFTradeExtension;

[Export(typeof(IPlugin))]
internal sealed class ASFTradeExtension : IASF, IBotCommand2, IBotTradeOffer, IBotTradeOfferResults
{
    public string Name => nameof(ASFTradeExtension);
    public Version Version => Utils.MyVersion;

    [JsonProperty]
    public static PluginConfig Config => Utils.Config;
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
                if ((configProperty == "ASFTradeExtension" || configProperty == "CardTradeExtension") && configValue.Type == JTokenType.Object)
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
                        Utils.ASFLogger.LogGenericException(ex);
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
            sb.AppendLine(Static.Line);
            sb.AppendLine(Langs.EulaWarning);
            sb.AppendLine(Static.Line);
        }

        if (sb.Length > 0)
        {
            Utils.ASFLogger.LogGenericWarning(sb.ToString());
        }
        //统计
        if (Config.Statistic)
        {
            var request = new Uri("https://asfe.chrxw.com/asftradeextension");
            _ = new Timer(
                async (_) =>
                {
                    await ASF.WebBrowser!.UrlGetToHtmlDocument(request).ConfigureAwait(false);
                },
                null,
                TimeSpan.FromSeconds(30),
                TimeSpan.FromHours(24)
            );
        }
        //禁用命令
        if (Config.DisabledCmds == null)
        {
            Config.DisabledCmds = new();
        }
        else
        {
            for (int i = 0; i < Config.DisabledCmds.Count; i++)
            {
                Config.DisabledCmds[i] = Config.DisabledCmds[i].ToUpperInvariant();
            }
        }
        if (Config.MaxItemPerTrade < byte.MaxValue)
        {
            Config.MaxItemPerTrade = byte.MaxValue;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 插件加载事件
    /// </summary>
    /// <returns></returns>
    public Task OnLoaded()
    {
        var message = new StringBuilder("\n");
        message.AppendLine(Static.Line);
        message.AppendLine(Static.Logo);
        message.AppendLine(Static.Line);
        message.AppendLine(string.Format(Langs.PluginVer, nameof(ASFTradeExtension), Utils.MyVersion.ToString()));
        message.AppendLine(Langs.PluginContact);
        message.AppendLine(Langs.PluginInfo);
        message.AppendLine(Static.Line);

        string pluginFolder = Path.GetDirectoryName(Utils.MyLocation) ?? ".";
        string backupPath = Path.Combine(pluginFolder, $"{nameof(ASFTradeExtension)}.bak");
        bool existsBackup = File.Exists(backupPath);
        if (existsBackup)
        {
            try
            {
                File.Delete(backupPath);
                message.AppendLine(Langs.CleanUpOldBackup);
            }
            catch (Exception e)
            {
                Utils.ASFLogger.LogGenericException(e);
                message.AppendLine(Langs.CleanUpOldBackupFailed);
            }
        }
        else
        {
            message.AppendLine(Langs.ASFEVersionTips);
            message.AppendLine(Langs.ASFEUpdateTips);
        }

        message.AppendLine(Static.Line);

        Utils.ASFLogger.LogGenericInfo(message.ToString());

        return Task.CompletedTask;
    }

    /// <summary>
    /// 处理命令
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="access"></param>
    /// <param name="message"></param>
    /// <param name="args"></param>
    /// <param name="steamId"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private static Task<string?>? ResponseCommand(Bot bot, EAccess access, string message, string[] args, ulong steamId)
    {
        string cmd = args[0].ToUpperInvariant();

        if (cmd.StartsWith("ATE."))
        {
            cmd = cmd.Substring(4);
        }
        else
        {
            //跳过禁用命令
            if (Config.DisabledCmds?.Contains(cmd) == true)
            {
                ASFLogger.LogGenericInfo("Command {0} is disabled!");
                return null;
            }
        }

        int argLength = args.Length;
        return argLength switch
        {
            0 => throw new InvalidOperationException(nameof(args)),
            1 => cmd switch //不带参数
            {
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


                //Update
                "ASFTRADEXTENSION" or
                "ATE" when access >= EAccess.FamilySharing =>
                    Task.FromResult(Update.Command.ResponseASFTradeExtensionVersion()),

                "ATEVERSION" or
                "ATEV" when access >= EAccess.Operator =>
                    Update.Command.ResponseCheckLatestVersion(),

                "ATEUPDATE" or
                "ATEU" when access >= EAccess.Owner =>
                    Update.Command.ResponseUpdatePlugin(),

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
                "FSL" when access >= EAccess.Operator && argLength % 2 == 1 =>
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
                "CIL" when access >= EAccess.Operator && argLength % 2 == 1 =>
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
        if (!Enum.IsDefined(access))
        {
            throw new InvalidEnumArgumentException(nameof(access), (int)access, typeof(EAccess));
        }

        try
        {
            var task = ResponseCommand(bot, access, message, args, steamId);
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
            string version = await bot.Commands.Response(EAccess.Owner, "VERSION").ConfigureAwait(false) ?? "Unknown";
            var i = version.LastIndexOf('V');
            if (i >= 0)
            {
                version = version[++i..];
            }
            string cfg = JsonConvert.SerializeObject(Config, Formatting.Indented);

            var sb = new StringBuilder();
            sb.AppendLine(Langs.ErrorLogTitle);
            sb.AppendLine(Static.Line);
            sb.AppendLineFormat(Langs.ErrorLogOriginMessage, message);
            sb.AppendLineFormat(Langs.ErrorLogAccess, access.ToString());
            sb.AppendLineFormat(Langs.ErrorLogASFVersion, version);
            sb.AppendLineFormat(Langs.ErrorLogPluginVersion, MyVersion);
            sb.AppendLine(Static.Line);
            sb.AppendLine(cfg);
            sb.AppendLine(Static.Line);
            sb.AppendLineFormat(Langs.ErrorLogErrorName, ex.GetType());
            sb.AppendLineFormat(Langs.ErrorLogErrorMessage, ex.Message);
            sb.AppendLine(ex.StackTrace);

            _ = Task.Run(async () =>
            {
                await Task.Delay(500).ConfigureAwait(false);
                sb.Insert(0, '\n');
                ASFLogger.LogGenericError(sb.ToString());
            }).ConfigureAwait(false);

            return sb.ToString();
        }
    }

    public Task<bool> OnBotTradeOffer(Bot bot, TradeOffer tradeOffer)
    {
        bool accept = Csgo.Handler.IsMyTrade(tradeOffer.TradeOfferID, tradeOffer.OtherSteamID64);
        Utils.ASFLogger.LogGenericWarning(string.Format("交易Id: {0}, {1}", tradeOffer.TradeOfferID, accept));
        return Task.FromResult(accept);
    }

    public Task OnBotTradeOfferResults(Bot bot, IReadOnlyCollection<ParseTradeResult> tradeResults)
    {
        foreach (var tradeResult in tradeResults)
        {
            Csgo.Handler.RemoveMyTrade(tradeResult.TradeOfferID);
            Utils.ASFLogger.LogGenericWarning(string.Format("交易Id: {0}, {1}", tradeResult.TradeOfferID, tradeResult.Result));
        }
        return Task.CompletedTask;
    }
}
