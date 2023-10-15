using System.IO.Compression;
using System.Text;


namespace ASFTradeExtension.Update;

internal static class Command
{
    /// <summary>
    /// 查看插件版本
    /// </summary>
    /// <returns></returns>
    internal static string? ResponseASFTradeExtensionVersion()
    {
        return FormatStaticResponse(string.Format(Langs.PluginVer, nameof(ASFTradeExtension), Utils.MyVersion.ToString()));
    }
}
