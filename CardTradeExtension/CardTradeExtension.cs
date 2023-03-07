using ArchiSteamFarm.Core;
using ArchiSteamFarm.Plugins.Interfaces;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Integration.Callbacks;
using CardTradeExtension.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Composition;
using System.Text;

namespace CardTradeExtension
{
    [Export(typeof(IPlugin))]
    internal sealed class CardTradeExtension : IASF, IBotCommand2, IBotUserNotifications
    {
        public string Name => nameof(CardTradeExtension);
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
                    if (configProperty == "CardTradeExtension" && configValue.Type == JTokenType.Object)
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
                Uri request = new("https://asfe.chrxw.com/");
                _ = new Timer(
                    async (_) => {
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
            message.AppendLine(string.Format(Langs.PluginVer, nameof(CardTradeExtension), MyVersion.ToString()));
            message.AppendLine(Langs.PluginContact);
            message.AppendLine(Langs.PluginInfo);
            message.AppendLine(Static.Line);

            string pluginFolder = Path.GetDirectoryName(MyLocation) ?? ".";
            string backupPath = Path.Combine(pluginFolder, $"{nameof(CardTradeExtension)}.bak");
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
                        //Core
                        case "FULLSETLIST" when access >= EAccess.Operator:
                        case "FSL" when access >= EAccess.Operator:
                            return await Core.Command.ResponseFullSetList(bot, null).ConfigureAwait(false);

                        //Update
                        case "CARDTRADEXTENSION" when access >= EAccess.FamilySharing:
                        case "CTE" when access >= EAccess.FamilySharing:
                            return Update.Command.ResponseCardTradeExtensionVersion();

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
                        case "FULLSETLIST" when access >= EAccess.Operator && argLength % 2 == 0:
                        case "FSL" when access >= EAccess.Operator && argLength % 2 == 0:
                            return await Core.Command.ResponseFullSetList(args[1], Utilities.GetArgsAsText(args, 2, ",")).ConfigureAwait(false);
                        case "FULLSETLIST" when access >= EAccess.Operator && argLength % 2 == 1:
                        case "FSL" when access >= EAccess.Operator && argLength % 2 == 1:
                            return await Core.Command.ResponseFullSetList(bot, Utilities.GetArgsAsText(args, 1, ",")).ConfigureAwait(false);

                        case "FULLSET" when argLength >= 3 && access >= EAccess.Operator:
                        case "FS" when argLength >= 3 && access >= EAccess.Operator:
                            return await Core.Command.ResponseFullSetCountOfGame(args[1], Utilities.GetArgsAsText(args, 1, ",")).ConfigureAwait(false);

                        case "FULLSET" when access >= EAccess.Operator:
                        case "FS" when access >= EAccess.Operator:
                            return await Core.Command.ResponseFullSetCountOfGame(bot, args[1]).ConfigureAwait(false);

                        case "SENDCARDSET" when access >= EAccess.Master && argLength == 5:
                        case "SCS" when access >= EAccess.Master && argLength == 5:
                            return await Core.Command.ResponseSendCardSet(args[1], args[2], args[3], args[4]).ConfigureAwait(false);
                        case "SENDCARDSET" when access >= EAccess.Master && argLength == 4:
                        case "SCS" when access >= EAccess.Master && argLength == 4:
                            return await Core.Command.ResponseSendCardSet(bot, args[1], args[2], args[3]).ConfigureAwait(false);

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

                _ = Task.Run(async () => {
                    await Task.Delay(500).ConfigureAwait(false);
                    sb.Insert(0, '\n');
                    ASFLogger.LogGenericError(sb.ToString());
                }).ConfigureAwait(false);

                return sb.ToString();
            }
        }

        public Task OnBotUserNotifications(Bot bot, IReadOnlyCollection<UserNotificationsCallback.EUserNotification> newNotifications)
        {
            return Task.CompletedTask;
        }
    }
}
