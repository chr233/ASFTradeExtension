using Newtonsoft.Json;

namespace CardTradeExtension.Data
{
    public sealed record PluginConfig
    {

        [JsonProperty(Required = Required.DisallowNull)]
        public bool EULA { get; set; } = true;

        [JsonProperty(Required = Required.DisallowNull)]
        public bool Statistic { get; set; } = true;
        
        [JsonProperty(Required = Required.Default)]
        public List<string>? DisabledCmds { get; set; }
    }
}
