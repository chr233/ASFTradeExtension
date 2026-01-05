using ArchiSteamFarm.IPC.Responses;
using ArchiSteamFarm.Steam.Data;
using ASFAwardTool.IPC.Responses;
using ASFTradeExtension.IPC.Requests;
using ASFTradeExtension.IPC.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SteamKit2;

namespace ASFTradeExtension.IPC.Controllers;

/// <summary>
/// 用户接口
/// </summary>
public sealed class TradeController : AbstractController
{
    /// <summary>
    /// 往指定链接发货
    /// </summary>
    /// <param name="targetLevel">目标等级</param>
    /// <param name="tradeLink">交易链接</param>
    /// <param name="autoConfirm">自动确认</param>
    /// <returns></returns>
    [HttpPost]
    [EndpointSummary("往指定链接发货")]
    public async Task<ActionResult<GenericResponse<string>>> DeliveryLevelUpCardSets(
        [FromBody] DeliveryLevelUpCardSetsRequest payload)
    {
        if (payload.TargetLevel < 1 || payload.TargetLevel > 1000)
        {
            return Ok(new GenericResponse(false, "目标等级无效, 有效范围 1~1000"));
        }

        var (tradeLinkValid, targetSteamId, tradeToken) = ParseTradeLink(payload.TradeLink);
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

            var badgeInfo = await handler.GetUserBadgeSummary(targetSteamId).ConfigureAwait(false);
            if (badgeInfo == null)
            {
                return Ok(new GenericResponse(false, $"读取用户 {targetSteamId} 的徽章信息失败"));
            }

            if (payload.TargetLevel <= badgeInfo.Level)
            {
                return Ok(new GenericResponse(false,
                    $"用户 {badgeInfo.Nickname} {targetSteamId} 的等级 {badgeInfo.Level} 大于等于目标等级"));
            }

            var needExp = CalcExpToLevel(badgeInfo.Level, payload.TargetLevel, badgeInfo.Experience);
            var needSet = (needExp / 100) + 1;

            var avilableSets = await handler.SelectFullSetCards(badgeInfo.Badges, needSet)
                .ConfigureAwait(false);

            var offer = avilableSets.TradeItems;
            if (needSet != avilableSets.CardSet || offer.Count == 0)
            {
                return Ok(new GenericResponse(false, "可用卡牌库存不足"));
            }

            await handler.AddInTradeItems(avilableSets.TradeItems).ConfigureAwait(false);

            var tradeMsg = $"共发货 {needSet} 套 {offer.Count} 张卡牌, 可以从 {badgeInfo.Level} 级升到 {payload.TargetLevel} 级";

            var (success, _, mobileTradeOfferIDs) = await bot.ArchiWebHandler
                .SendTradeOffer(targetSteamId, offer, null, tradeToken, tradeMsg, false, Config.MaxItemPerTrade)
                .ConfigureAwait(false);

            if (!success)
            {
                return Ok(new GenericResponse(false, "发送报价失败, 可能网络问题"));
            }

            if (payload.AutoConfirm && mobileTradeOfferIDs?.Count > 0 && bot.HasMobileAuthenticator)
            {
                var (twoFactorSuccess, _, _) = await bot.Actions
                    .HandleTwoFactorAuthenticationConfirmations(true, Confirmation.EConfirmationType.Trade,
                        mobileTradeOfferIDs, true).ConfigureAwait(false);
            }

            var response = new SendLevelUpSetResponse(badgeInfo.Nickname, badgeInfo.Level, badgeInfo.Experience,
               payload.TargetLevel, avilableSets.CardSet, avilableSets.CardCount, avilableSets.CardType, payload.AutoConfirm);

            return Ok(new GenericResponse<SendLevelUpSetResponse>(true,
                $"发送报价成功, 从 {badgeInfo.Level} 级升级到 {payload.TargetLevel} 级还需要 {needExp} 点经验, 需要合成 {needSet} 套卡牌, 总计发送了 {response.CardSet} 套 {response.CardCount} 张卡牌",
                response));
        }
        catch (Exception ex)
        {
            ASFLogger.LogGenericError("发货失败");
            ASFLogger.LogGenericException(ex);
            return Ok(new GenericResponse(false, "执行发货失败"));
        }
    }

    [HttpPost]
    [EndpointDescription("往交易链接发送报价")]
    [EndpointSummary("往交易链接发送报价")]
    public async Task<ActionResult<BaseResponse<SendCardSetResponse>>> DeliveryCardSets(
        [FromBody] DeliveryCardSetsRequest payload)
    {
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

        if (string.IsNullOrEmpty(payload.TradeLink) || payload.CardSets == null || payload.CardSets.Count == 0)
        {
            return Ok(new BaseResponse(false, "请指定交易链接和卡牌套数信息"));
        }

        var match = RegexUtils.MatchTradeLink.Match(payload.TradeLink);

        if (!match.Success || !ulong.TryParse(match.Groups[1].Value, out var targetSteamId))
        {
            return Ok(new BaseResponse(false, "交易链接似乎无效"));
        }

        var tradeToken = match.Groups[2].Value;
        targetSteamId = Steam322SteamId(targetSteamId);

        if (!new SteamID(targetSteamId).IsIndividualAccount)
        {
            return Ok(new BaseResponse(false, "交易链接无效"));
        }

        var inventory = await (
            payload.FoilCard ? handler.GetFoilCardSetCache(false) : handler.GetCardSetCache(false)
        ).ConfigureAwait(false);

        List<Asset> offer = [];

        uint setCount = 0;
        foreach (var (appId, targetSetCount) in payload.CardSets)
        {
            if (!inventory.TryGetValue(appId, out var bundle) || bundle.Assets == null ||
                bundle.TradableSetCount < targetSetCount)
            {
                return Ok(new BaseResponse(false, $"发货库存缺少 AppId={appId} 的物品"));
            }

            var flag = bundle.Assets.Select(static x => x.ClassID).Distinct()
                .ToDictionary(static x => x, _ => targetSetCount);

            foreach (var asset in bundle.Assets)
            {
                var clsId = asset.ClassID;
                if (flag[clsId] > 0)
                {
                    offer.Add(asset);
                    flag[clsId]--;
                }
            }

            if (flag.Values.Any(static x => x > 0))
            {
                return Ok(new BaseResponse(false, $"发货库存缺少 AppId={appId} 的物品"));
            }

            setCount += targetSetCount;
        }

        if (offer.Count == 0)
        {
            return Ok(new BaseResponse(false, "发货失败, 无待交易物品"));
        }

        await handler.AddInTradeItems(offer).ConfigureAwait(false);

        var tradeMsg = $"共发货 {setCount} 套 {offer.Count} 张卡牌";

        var (success, _, mobileTradeOfferIDs) = await bot.ArchiWebHandler
            .SendTradeOffer(targetSteamId, offer, null, tradeToken, tradeMsg, false, Config.MaxItemPerTrade)
            .ConfigureAwait(false);

        if (!success)
        {
            return Ok(new BaseResponse(false, "发送报价失败, 可能网络问题"));
        }

        if (payload.AutoConfirm && mobileTradeOfferIDs?.Count > 0 && bot.HasMobileAuthenticator)
        {
            var (twoFactorSuccess, _, _) = await bot.Actions
                .HandleTwoFactorAuthenticationConfirmations(true, Confirmation.EConfirmationType.Trade,
                    mobileTradeOfferIDs, true).ConfigureAwait(false);
        }

        var response = new SendCardSetResponse
        {
            AutoConfirm = payload.AutoConfirm,
            CardCount = (uint)offer.Count,
            SetCount = setCount,
            FoilCard = payload.FoilCard
        };

        return Ok(new BaseResponse<SendCardSetResponse>(true,
            $"发送报价成功, 发送了 {response.SetCount} 套 {response.CardCount} 张卡牌", response));
    }
}