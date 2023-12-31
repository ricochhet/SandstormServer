using System.Text.Json.Serialization;

namespace Sandstorm.Core.Models;

public class ModObjectModel
{
    [JsonPropertyName("data")]
    public object[] Data { get; set; }

    [JsonPropertyName("result_count")]
    public int ResultCount { get; set; }

    [JsonPropertyName("result_offset")]
    public int ResultOffset { get; set; }

    [JsonPropertyName("result_limit")]
    public int ResultLimit { get; set; }

    [JsonPropertyName("result_total")]
    public int ResultTotal { get; set; }
}
