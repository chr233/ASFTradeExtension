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
    public Version Version => MyVersion;

    [JsonProperty]
    public static PluginConfig Config => Utils.Config;
    /// <summary>
    /// ASF启动事件
    /// </summary>
    /// <param name="additionalConfigProperties"></param>
    /// <returns></returns>
    public Task OnASFInit(IReadOnlyDictionary<string, JToken>? additionalConfigProperties = null)
    {
        StringBuilder sb = new();

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
                        ASFLogger.LogGenericException(ex);
                    }
                }
            }
        }

        Utils.Config = config ?? new();

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
            ASFLogger.LogGenericWarning(sb.ToString());
        }
        //统计
        if (Config.Statistic)
        {
            Uri request = new("https://asfe.chrxw.com/asftradeextension");
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
        StringBuilder message = new("\n");
        message.AppendLine(Static.Line);
        message.AppendLine(Static.Logo);
        message.AppendLine(Static.Line);
        message.AppendLine(string.Format(Langs.PluginVer, nameof(ASFTradeExtension), MyVersion.ToString()));
        message.AppendLine(Langs.PluginContact);
        message.AppendLine(Langs.PluginInfo);
        message.AppendLine(Static.Line);

        string pluginFolder = Path.GetDirectoryName(MyLocation) ?? ".";
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
                ASFLogger.LogGenericException(e);
                message.AppendLine(Langs.CleanUpOldBackupFailed);
            }
        }
        else
        {
            message.AppendLine(Langs.ASFEVersionTips);
            message.AppendLine(Langs.ASFEUpdateTips);
        }

        message.AppendLine(Static.Line);

        ASFLogger.LogGenericInfo(message.ToString());

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
    private static async Task<string?> ResponseCommand(Bot bot, EAccess access, string message, string[] args, ulong steamId)
    {
        string cmd = args[0].ToUpperInvariant();

        if (cmd.StartsWith("CTE."))
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
        switch (argLength)
        {
            case 0:
                throw new InvalidOperationException(nameof(args));
            case 1: //不带参数
                switch (cmd)
                {
                    //Card
                    case "FULLSETLIST" when access >= EAccess.Operator:
                    case "FSL" when access >= EAccess.Operator:
                        return await Card.Command.ResponseFullSetList(bot, null).ConfigureAwait(false);

                    //CSGO
                    case "CSITEMLIST" when access >= EAccess.Operator:
                    case "CIL" when access >= EAccess.Operator:
                        return await Csgo.Command.ResponseCsItemList(bot, null).ConfigureAwait(false);

                    case "CSSENDITEM" when access >= EAccess.Master:
                    case "CSI" when access >= EAccess.Master:
                        return await Csgo.Command.ResponseSendCsItem(bot, null, null, false).ConfigureAwait(false);
                    case "2CSSENDITEM" when access >= EAccess.Master:
                    case "2CSI" when access >= EAccess.Master:
                        return await Csgo.Command.ResponseSendCsItem(bot, null, null, true).ConfigureAwait(false);

                    case "CSMARKETHISTORY" when access >= EAccess.Operator:
                    case "CMH" when access >= EAccess.Operator:
                        return await Csgo.Command.ResponseGetCsMarketInfo(bot, null).ConfigureAwait(false);

                    case "CSDELISTING" when access >= EAccess.Master:
                    case "CDL" when access >= EAccess.Master:
                        return await Csgo.Command.ResponseCsRemoveListing(bot, null).ConfigureAwait(false);


                    //Update
                    case "CARDTRADEXTENSION" when access >= EAccess.FamilySharing:
                    case "CTE" when access >= EAccess.FamilySharing:
                        return Update.Command.ResponseASFTradeExtensionVersion();

                    case "CTEVERSION" when access >= EAccess.Operator:
                    case "CTEV" when access >= EAccess.Operator:
                        return await Update.Command.ResponseCheckLatestVersion().ConfigureAwait(false);

                    case "CTEUPDATE" when access >= EAccess.Owner:
                    case "CTEU" when access >= EAccess.Owner:
                        return await Update.Command.ResponseUpdatePlugin().ConfigureAwait(false);


                    default:
                        return null;
                }
            default: //带参数
                switch (cmd)
                {
                    //Card
                    case "FULLSETLIST" when access >= EAccess.Operator && argLength == 2:
                    case "FSL" when access >= EAccess.Operator && argLength == 2:
                        return await Card.Command.ResponseFullSetList(args[1], null).ConfigureAwait(false);
                    case "FULLSETLIST" when access >= EAccess.Operator && argLength % 2 == 0:
                    case "FSL" when access >= EAccess.Operator && argLength % 2 == 0:
                        return await Card.Command.ResponseFullSetList(args[1], Utilities.GetArgsAsText(args, 2, ",")).ConfigureAwait(false);
                    case "FULLSETLIST" when access >= EAccess.Operator && argLength % 2 == 1:
                    case "FSL" when access >= EAccess.Operator && argLength % 2 == 1:
                        return await Card.Command.ResponseFullSetList(bot, Utilities.GetArgsAsText(args, 1, ",")).ConfigureAwait(false);

                    case "FULLSET" when argLength >= 3 && access >= EAccess.Operator:
                    case "FS" when argLength >= 3 && access >= EAccess.Operator:
                        return await Card.Command.ResponseFullSetCountOfGame(args[1], Utilities.GetArgsAsText(args, 1, ",")).ConfigureAwait(false);
                    case "FULLSET" when access >= EAccess.Operator:
                    case "FS" when access >= EAccess.Operator:
                        return await Card.Command.ResponseFullSetCountOfGame(bot, args[1]).ConfigureAwait(false);

                    case "SENDCARDSET" when access >= EAccess.Master && argLength == 5:
                    case "SCS" when access >= EAccess.Master && argLength == 5:
                        return await Card.Command.ResponseSendCardSet(args[1], args[2], args[3], args[4], false).ConfigureAwait(false);
                    case "SENDCARDSET" when access >= EAccess.Master && argLength == 4:
                    case "SCS" when access >= EAccess.Master && argLength == 4:
                        return await Card.Command.ResponseSendCardSet(bot, args[1], args[2], args[3], false).ConfigureAwait(false);

                    case "2SENDCARDSET" when access >= EAccess.Master && argLength == 5:
                    case "2SCS" when access >= EAccess.Master && argLength == 5:
                        return await Card.Command.ResponseSendCardSet(args[1], args[2], args[3], args[4], true).ConfigureAwait(false);
                    case "2SENDCARDSET" when access >= EAccess.Master && argLength == 4:
                    case "2SCS" when access >= EAccess.Master && argLength == 4:
                        return await Card.Command.ResponseSendCardSet(bot, args[1], args[2], args[3], true).ConfigureAwait(false);

                    //CSGO
                    case "CSITEMLIST" when access >= EAccess.Operator && argLength == 2:
                    case "CIL" when access >= EAccess.Operator && argLength == 2:
                        return await Csgo.Command.ResponseCsItemList(args[1], null).ConfigureAwait(false);
                    case "CSITEMLIST" when access >= EAccess.Operator && argLength % 2 == 0:
                    case "CIL" when access >= EAccess.Operator && argLength % 2 == 0:
                        return await Csgo.Command.ResponseCsItemList(args[1], Utilities.GetArgsAsText(args, 2, ",")).ConfigureAwait(false);
                    case "CSITEMLIST" when access >= EAccess.Operator && argLength % 2 == 1:
                    case "CIL" when access >= EAccess.Operator && argLength % 2 == 1:
                        return await Csgo.Command.ResponseCsItemList(bot, Utilities.GetArgsAsText(args, 1, ",")).ConfigureAwait(false);


                    case "CSSENDITEM" when access >= EAccess.Master && argLength == 4:
                    case "CSI" when access >= EAccess.Master && argLength == 4:
                        return await Csgo.Command.ResponseSendCsItem(args[1], args[2], args[3], false).ConfigureAwait(false);
                    case "CSSENDITEM" when access >= EAccess.Master && argLength == 3:
                    case "CSI" when access >= EAccess.Master && argLength == 3:
                        return await Csgo.Command.ResponseSendCsItem(bot, args[1], args[2], false).ConfigureAwait(false);
                    case "CSSENDITEM" when access >= EAccess.Master && argLength == 2:
                    case "CSI" when access >= EAccess.Master && argLength == 2:
                        return await Csgo.Command.ResponseSendCsItem(args[1], null, null, false).ConfigureAwait(false);


                    case "2CSSENDITEM" when access >= EAccess.Master && argLength == 4:
                    case "2CSI" when access >= EAccess.Master && argLength == 4:
                        return await Csgo.Command.ResponseSendCsItem(args[1], args[2], args[3], true).ConfigureAwait(false);
                    case "2CSSENDITEM" when access >= EAccess.Master && argLength == 3:
                    case "2CSI" when access >= EAccess.Master && argLength == 3:
                        return await Csgo.Command.ResponseSendCsItem(bot, args[1], args[2], true).ConfigureAwait(false);
                    case "2CSSENDITEM" when access >= EAccess.Master && argLength == 2:
                    case "2CSI" when access >= EAccess.Master && argLength == 2:
                        return await Csgo.Command.ResponseSendCsItem(args[1], null, null, true).ConfigureAwait(false);


                    case "CSSELLITEM" when access >= EAccess.Master && argLength == 5:
                    case "CEI" when access >= EAccess.Master && argLength == 5:
                        return await Csgo.Command.ResponseSellCsItem(args[1], args[2], args[3], args[4], false).ConfigureAwait(false);
                    case "CSSELLITEM" when access >= EAccess.Master && argLength == 4:
                    case "CEI" when access >= EAccess.Master && argLength == 4:
                        return await Csgo.Command.ResponseSellCsItem(bot, args[1], args[2], args[3], false).ConfigureAwait(false);


                    case "2CSSELLITEM" when access >= EAccess.Master && argLength == 5:
                    case "2CEI" when access >= EAccess.Master && argLength == 5:
                        return await Csgo.Command.ResponseSellCsItem(args[1], args[2], args[3], args[4], true).ConfigureAwait(false);
                    case "2CSSELLITEM" when access >= EAccess.Master && argLength == 4:
                    case "2CEI" when access >= EAccess.Master && argLength == 4:
                        return await Csgo.Command.ResponseSellCsItem(bot, args[1], args[2], args[3], true).ConfigureAwait(false);


                    case "CSMARKETHISTORY" when access >= EAccess.Operator:
                    case "CMH" when access >= EAccess.Operator:
                        {
                            string botNames = string.Join(',', args[1..(argLength - 1)]);
                            return await Csgo.Command.ResponseGetCsMarketInfo(botNames, args.Last()).ConfigureAwait(false);
                        }


                    case "CSDELISTING" when access >= EAccess.Master && argLength >= 3:
                    case "CDL" when access >= EAccess.Master && argLength >= 3:
                        {
                            string botNames = string.Join(',', args[1..(argLength - 1)]);
                            return await Csgo.Command.ResponseCsRemoveListing(botNames, args.Last()).ConfigureAwait(false);
                        }
                    case "CSDELISTING" when access >= EAccess.Master:
                    case "CDL" when access >= EAccess.Master:
                        return await Csgo.Command.ResponseCsRemoveListing(bot, args[1]).ConfigureAwait(false);

                    default:
                        return null;
                }
        }
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
            return await ResponseCommand(bot, access, message, args, steamId).ConfigureAwait(false);
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

            StringBuilder sb = new();
            sb.AppendLine(Langs.ErrorLogTitle);
            sb.AppendLine(Static.Line);
            sb.AppendLine(string.Format(Langs.ErrorLogOriginMessage, message));
            sb.AppendLine(string.Format(Langs.ErrorLogAccess, access.ToString()));
            sb.AppendLine(string.Format(Langs.ErrorLogASFVersion, version));
            sb.AppendLine(string.Format(Langs.ErrorLogPluginVersion, MyVersion));
            sb.AppendLine(Static.Line);
            sb.AppendLine(cfg);
            sb.AppendLine(Static.Line);
            sb.AppendLine(string.Format(Langs.ErrorLogErrorName, ex.GetType()));
            sb.AppendLine(string.Format(Langs.ErrorLogErrorMessage, ex.Message));
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
