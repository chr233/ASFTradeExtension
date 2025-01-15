using ArchiSteamFarm.IPC.Responses;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;
using ASFTradeExtension.Data.Core;
using ASFTradeExtension.IPC.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace ASFTradeExtension.IPC.Controllers;

/// <summary>
/// 测试接口
/// </summary>
[Route("/Api/[controller]/[action]")]
public sealed class AluController : AbstractController
{
    /// <summary>
    /// 获取发货机器人
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [EndpointSummary("获取发货机器人")]
    public ActionResult<GenericResponse<string>> GetMasterBot()
    {
        var (bot, handler) = Utils.GetMasterBot();
        if (bot == null || handler == null)
        {
            return Ok(new GenericResponse(false, "未设置发货机器人"));
        }

        return Ok(new GenericResponse<string>(true, "Ok", bot.BotName));
    }

    /// <summary>
    /// 设置发货机器人
    /// </summary>
    /// <param name="botName">机器人名称</param>
    /// <returns></returns>
    [HttpPost]
    [EndpointSummary("设置发货机器人")]
    public async Task<ActionResult<GenericResponse<string>>> SetMasterBot(
        [Description("机器人名称")] string botName)
    {
        var bot = Bot.GetBot(botName);
        if (bot == null)
        {
            return Ok(new GenericResponse(false, "找不到机器人"));
        }

        await CardSetCache.SetMasterBotName(botName).ConfigureAwait(false);

        return Ok(new GenericResponse<string>(true, "Ok", bot.BotName));
    }

    /// <summary>
    /// 获取发货机器人库存
    /// </summary>
    /// <param name="forceReload">强制刷新</param>
    /// <returns></returns>
    [HttpPost]
    [EndpointSummary("获取发货机器人库存")]
    public async Task<ActionResult<GenericResponse<string>>> GetBotStock(
        [Description("强制刷新")] bool forceReload = false)
    {
        var (bot, handler) = Utils.GetMasterBot();
        if (bot == null || handler == null)
        {
            return Ok(new GenericResponse(false, "未设置发货机器人"));
        }

        var inv = await handler.GetCardSetCache(forceReload).ConfigureAwait(false);
        await handler.FullLoadAppCardGroup(inv).ConfigureAwait(false);

        var setCount = inv.Values.Where(static b => b.CardCountPerSet > 0).Sum(static b => b.CardCountPerSet);
        var notLoaded = inv.Values.Count(static b => !b.Loaded);

        return Ok(new GenericResponse<Dictionary<uint, AssetBundle>>(true, "Ok", inv));
    }

    /// <summary>
    /// 往指定链接发货
    /// </summary>
    /// <param name="targetLevel">目标等级</param>
    /// <param name="tradeLink">交易链接</param>
    /// <param name="autoConfirm">自动确认</param>
    /// <returns></returns>
    [HttpPost]
    [EndpointSummary("往指定链接发货")]
    public async Task<ActionResult<GenericResponse<string>>> DeliveryCardsByLevel(
        [Description("目标等级")] int targetLevel,
        [Description("交易链接")] string tradeLink,
        [Description("自动确认")] bool autoConfirm = true)
    {
        if (targetLevel < 1 || targetLevel > 1000)
        {
            return Ok(new GenericResponse(false, "目标等级无效, 有效范围 1~1000"));
        }

        var (tradeLinkValid, targetSteamId, tradeToken) = ParseTradeLink(tradeLink);
        if (!tradeLinkValid || targetSteamId == 0 || string.IsNullOrEmpty(tradeToken))
        {
            return Ok(new GenericResponse(false, "交易链接无效"));
        }

        var (bot, handler) = GetRandomBot();
        if (bot == null || handler == null)
        {
            return Ok(new GenericResponse(false, "未设置发货机器人"));
        }

        if (!bot.IsConnectedAndLoggedOn)
        {
            return Ok(new GenericResponse(false, "发货机器人当前离线, 请稍后再试"));
        }

        if (bot.IsAccountLimited || bot.IsAccountLocked)
        {
            return Ok(new GenericResponse(false, "发货机器人受限或者被锁定, 请更换机器人, 用法 SETMASTERBOT [Bot] 设置发货机器人"));
        }

        try
        {
            var userInfo = await handler.GetUserBasicInfo(targetSteamId).ConfigureAwait(false);
            if (userInfo == null || !userInfo.IsValid)
            {
                return Ok(new GenericResponse(false, $"交易链接似乎无效, 找不到用户 {targetSteamId}"));
            }

            var badgeInfo = await handler.GetUserBadgeSummary(userInfo.ProfilePath!, true).ConfigureAwait(false);
            if (badgeInfo == null)
            {
                return Ok(new GenericResponse(false, $"读取用户 {targetSteamId} 的徽章信息失败"));
            }

            if (targetLevel <= badgeInfo.Level)
            {
                return Ok(new GenericResponse(false,
                    $"用户 {badgeInfo.Nickname} {targetSteamId} 的等级 {badgeInfo.Level} 大于等于目标等级"));
            }

            var needExp = CalcExpToLevel(badgeInfo.Level, targetLevel, badgeInfo.Experience);
            var needSet = (needExp / 100) + 1;

            var avilableSets = await handler.SelectFullSetCards(badgeInfo.Badges, needSet)
                .ConfigureAwait(false);

            var offer = avilableSets.TradeItems;
            if (needSet != avilableSets.CardSet || offer.Count == 0)
            {
                return Ok(new GenericResponse(false, "可用卡牌库存不足"));
            }

            await handler.AddInTradeItems(avilableSets.TradeItems).ConfigureAwait(false);

            var (success, _, mobileTradeOfferIDs) = await bot.ArchiWebHandler
                .SendTradeOffer(targetSteamId, offer, null, tradeToken, null, false, Config.MaxItemPerTrade)
                .ConfigureAwait(false);

            if (!success)
            {
                return Ok(new GenericResponse(false, "发送报价失败, 可能网络问题"));
            }

            if (autoConfirm && mobileTradeOfferIDs?.Count > 0 && bot.HasMobileAuthenticator)
            {
                var (twoFactorSuccess, _, _) = await bot.Actions
                    .HandleTwoFactorAuthenticationConfirmations(true, Confirmation.EConfirmationType.Trade,
                        mobileTradeOfferIDs, true).ConfigureAwait(false);
            }

            var response = new SendLevelUpSetResponse(badgeInfo.Nickname, badgeInfo.Level, badgeInfo.Experience,
                targetLevel, avilableSets.CardSet, avilableSets.CardCount, avilableSets.CardType, autoConfirm);

            return Ok(new GenericResponse<SendLevelUpSetResponse>(true,
                $"发送报价成功, 从 {badgeInfo.Level} 级升级到 {targetLevel} 级还需要 {needExp} 点经验, 需要合成 {needSet} 套卡牌, 总计发送了 {response.CardSet} 套 {response.CardCount} 张卡牌",
                response));
        }
        catch (Exception ex)
        {
            ASFLogger.LogGenericError("发货失败");
            ASFLogger.LogGenericException(ex);
            return Ok(new GenericResponse(false, "执行发货失败"));
        }
    }
}