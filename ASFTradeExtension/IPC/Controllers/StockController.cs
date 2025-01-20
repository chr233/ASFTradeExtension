using ArchiSteamFarm.IPC.Responses;
using ArchiSteamFarm.Steam;
using ASFTradeExtension.Data.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace ASFTradeExtension.IPC.Controllers;

/// <summary>
/// 测试接口
/// </summary>
public sealed class StockController : AbstractController
{
    /// <summary>
    /// 获取发货机器人
    /// </summary>
    /// <returns></returns>
    [HttpGet]
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
    [HttpGet]
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
}