using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace RepairPlannerAgent.Models;

public sealed class DiagnosedFault
{
    [JsonPropertyName("faultType")]
    [JsonProperty("faultType")]
    public string FaultType { get; set; } = string.Empty;

    [JsonPropertyName("machineId")]
    [JsonProperty("machineId")]
    public string MachineId { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("severity")]
    [JsonProperty("severity")]
    public string Severity { get; set; } = string.Empty;

    [JsonPropertyName("detectedAt")]
    [JsonProperty("detectedAt")]
    public DateTimeOffset DetectedAt { get; set; }

    [JsonPropertyName("symptoms")]
    [JsonProperty("symptoms")]
    public List<string> Symptoms { get; set; } = [];
}
