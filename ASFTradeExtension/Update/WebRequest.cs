using ArchiSteamFarm.Core;
using ArchiSteamFarm.Web.Responses;
using ASFTradeExtension.Data;

namespace ASFTradeExtension.Update;

internal static class WebRequest
{
    /// <summary>
    /// 获取最新的发行版
    /// </summary>
    /// <returns></returns>
    internal static async Task<GitHubReleaseResponse?> GetLatestRelease(bool useMirror = true)
    {
        var request = new Uri(useMirror ? "https://hub.chrxw.com/ASFTradeExtension/releases/latest" : "https://api.github.com/repos/chr233/ASFTradeExtension/releases/latest");
        var response = await ASF.WebBrowser!.UrlGetToJsonObject<GitHubReleaseResponse>(request).ConfigureAwait(false);

        if (response == null && useMirror)
        {
            return await GetLatestRelease(false).ConfigureAwait(false);
        }

        return response?.Content;
    }

    /// <summary>
    /// 下载发行版
    /// </summary>
    /// <param name="downloadUrl"></param>
    /// <returns></returns>
    internal static async Task<BinaryResponse?> DownloadRelease(string? downloadUrl)
    {
        if (string.IsNullOrEmpty(downloadUrl))
        {
            return null;
        }

        var request = new Uri(downloadUrl);
        var response = await ASF.WebBrowser!.UrlGetToBinary(request).ConfigureAwait(false);
        return response;
    }
}
