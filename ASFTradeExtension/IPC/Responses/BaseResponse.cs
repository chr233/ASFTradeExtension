using ArchiSteamFarm.Localization;

namespace ASFAwardTool.IPC.Responses;

/// <summary>
/// </summary>
/// <typeparam name="T"></typeparam>
public record BaseResponse<T> : BaseResponse
{
    /// <summary>
    /// </summary>
    public BaseResponse()
    {
    }

    /// <summary>
    /// </summary>
    /// <param name="result"></param>
    public BaseResponse(T? result) : base(result is not null)
    {
        Result = result;
    }

    /// <summary>
    /// </summary>
    /// <param name="success"></param>
    /// <param name="message"></param>
    public BaseResponse(bool success, string? message) : base(success, message) { }

    /// <summary>
    /// </summary>
    /// <param name="success"></param>
    /// <param name="result"></param>
    public BaseResponse(bool success, T? result) : base(success)
    {
        Result = result;
    }

    /// <summary>
    /// </summary>
    /// <param name="success"></param>
    /// <param name="message"></param>
    /// <param name="result"></param>
    public BaseResponse(bool success, string? message, T? result) : base(success, message)
    {
        Result = result;
    }

    /// <summary>
    /// </summary>
    public T? Result { get; private set; }
}

/// <summary>
/// </summary>
public record BaseResponse
{
    /// <summary>
    /// </summary>
    public BaseResponse() { }

    /// <summary>
    /// </summary>
    /// <param name="success"></param>
    /// <param name="message"></param>
    public BaseResponse(bool success, string? message = null)
    {
        Success = success;
        Message = !string.IsNullOrEmpty(message) ? message : success ? "OK" : Strings.WarningFailed;
    }

    /// <summary>
    /// A message that describes what happened with the request, if available.
    /// </summary>
    /// <remarks>
    /// This property will provide exact reason for majority of expected failures.
    /// </remarks>
    public string? Message { get; private set; }

    /// <summary>
    /// Boolean type that specifies if the request has succeeded.
    /// </summary>
    public bool Success { get; private set; }
}