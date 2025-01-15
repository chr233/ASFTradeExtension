using System.Text.Json.Serialization;

namespace ASFTradeExtension.Data.Steam;

public sealed record SellItemResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("requires_confirmation")]
    public byte RequiresConfirmation { get; set; }

    [JsonPropertyName("needs_mobile_confirmation")]
    public bool NeedsMobileConfirmation { get; set; }

    [JsonPropertyName("needs_email_confirmation")]
    public bool NeedsEmailConfirmation { get; set; }

    [JsonPropertyName("email_domain")]
    public string EmailDomain { get; set; } = "";
}