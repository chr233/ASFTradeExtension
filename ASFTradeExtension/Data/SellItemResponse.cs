using Newtonsoft.Json;

namespace ASFTradeExtension.Data;

internal sealed record SellItemResponse
{
    [JsonProperty(PropertyName = "success", Required = Required.Always)]
    public bool Success { get; set; }

    [JsonProperty(PropertyName = "requires_confirmation", Required = Required.Always)]
    public byte RequiresConfirmation { get; set; }

    [JsonProperty(PropertyName = "needs_mobile_confirmation", Required = Required.Always)]
    public bool NeedsMobileConfirmation { get; set; }

    [JsonProperty(PropertyName = "needs_email_confirmation", Required = Required.Always)]
    public bool NeedsEmailConfirmation { get; set; }

    [JsonProperty(PropertyName = "email_domain", Required = Required.Always)]
    public string EmailDomain { get; set; } = "";
}
