using ArchiSteamFarm.IPC.Controllers.Api;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ASFTradeExtension.IPC.Controllers;

/// <summary>
/// 基础控制器
/// </summary>
[Route("/Api/[controller]/[action]")]
[SwaggerTag("Award Tool")]
public abstract class AbstractController : ArchiController { }
