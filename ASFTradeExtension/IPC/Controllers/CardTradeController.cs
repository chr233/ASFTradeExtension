using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;
using ASFAwardTool.IPC.Responses;
using ASFTradeExtension.Core;
using ASFTradeExtension.Data.Core;
using ASFTradeExtension.IPC.Requests;
using ASFTradeExtension.IPC.Responses;
using Microsoft.AspNetCore.Mvc;
using SteamKit2;
using Swashbuckle.AspNetCore.Annotations;
using System.Globalization;
using System.Net;

namespace ASFTradeExtension.IPC.Controllers;

/// <summary>
/// 用户接口
/// </summary>
[Route("/Api/[controller]/[action]")]
public sealed class CardTradeController : AbstractController
{
    /// <summary>
    /// 获取机器人点数信息
    /// </summary>
    /// <returns></returns>
    [HttpGet("{botName:required}")]
    public async Task<ActionResult<BaseResponse<Dictionary<uint, AssetBundle>>>> GetBotStock(
        string botName, bool forceReload)
    {
        if (string.IsNullOrEmpty(botName))
        {
            throw new ArgumentNullException(nameof(botName));
        }

        var bot = Bot.GetBot(botName);
        if (bot == null)
        {
            return Ok(new BaseResponse(false,
                string.Format(CultureInfo.CurrentCulture, Strings.BotNotFound, botName)));
        }

        Dictionary<uint, AssetBundle>? result = null;

        var handler = Command.Handlers.GetValueOrDefault(bot);
        if (handler != null)
        {
            result = await handler.GetCardSetCache(forceReload).ConfigureAwait(false);
        }

        var response = new BaseResponse<Dictionary<uint, AssetBundle>?>(true, "Ok", result);
        return Ok(response);
    }

    /// <summary>
    /// 获取机器人点数信息
    /// </summary>
    /// <returns></returns>
    [HttpGet("{botNames:required}")]
    public async Task<ActionResult<BaseResponse<Dictionary<string, Dictionary<uint, AssetBundle>>>>> GetBotsStock(
        string botNames, bool forceReload)
    {
        if (string.IsNullOrEmpty(botNames))
        {
            throw new ArgumentNullException(nameof(botNames));
        }

        var bots = Bot.GetBots(botNames);
        if (bots == null || bots.Count == 0)
        {
            return Ok(new BaseResponse(false,
                string.Format(CultureInfo.CurrentCulture, Strings.BotNotFound, botNames)));
        }

        Dictionary<string, Dictionary<uint, AssetBundle>?> result = [];
        foreach (var bot in bots)
        {
            var botname = bot.BotName;

            var handler = Command.Handlers.GetValueOrDefault(bot);
            if (handler == null)
            {
                result[botname] = null;
            }
            else
            {
                result[botname] = await handler.GetCardSetCache(forceReload).ConfigureAwait(false);
            }
        }

        var response = new BaseResponse<Dictionary<string, Dictionary<uint, AssetBundle>?>>(true, "Ok", result);
        return Ok(response);
    }

    [HttpPost("{botName:required}")]
    [SwaggerOperation(Summary = "往交易链接发送报价", Description = "往交易链接发送报价")]
    [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(BaseResponse), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<BaseResponse<SendCardSetResponse>>> SendCardSetToTradeUrl(string botName,
        [FromBody] SendCardSetRequest payload)
    {
        if (string.IsNullOrEmpty(botName))
        {
            throw new ArgumentNullException(nameof(botName));
        }

        var bot = Bot.GetBot(botName);
        if (bot == null)
        {
            return Ok(new BaseResponse(false,
                string.Format(CultureInfo.CurrentCulture, Strings.BotNotFound, botName)));
        }

        if (!bot.IsConnectedAndLoggedOn)
        {
            return Ok(new BaseResponse(false, "机器人离线, 无法发送报价"));
        }

        var handler = Command.Handlers.GetValueOrDefault(bot);
        if (handler == null)
        {
            return Ok(new BaseResponse(false, "内部异常, 无法发送报价"));
        }

        if (string.IsNullOrEmpty(payload.TradeLink) || payload.CardSets == null || payload.CardSets.Count == 0)
        {
            return Ok(new BaseResponse(false, "请指定交易链接和卡牌套数信息"));
        }

        var match = RegexUtils.MatchTradeLink().Match(payload.TradeLink);

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

        var (success, _, mobileTradeOfferIDs) = await bot.ArchiWebHandler
            .SendTradeOffer(targetSteamId, offer, null, tradeToken, null, false, Config.MaxItemPerTrade)
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