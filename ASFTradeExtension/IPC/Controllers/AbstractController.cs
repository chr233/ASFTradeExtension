using Microsoft.AspNetCore.Mvc;

namespace ASFTradeExtension.IPC.Controllers;

/// <summary>
/// 基础控制器
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class AbstractController : ControllerBase
{
}