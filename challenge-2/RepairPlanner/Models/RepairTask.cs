using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace RepairPlannerAgent.Models;

public sealed class RepairTask
{
    [JsonPropertyName("sequence")]
    [JsonProperty("sequence")]
    public int Sequence { get; set; }

    [JsonPropertyName("title")]
    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("estimatedDurationMinutes")]
    [JsonProperty("estimatedDurationMinutes")]
    public int EstimatedDurationMinutes { get; set; }

    [JsonPropertyName("requiredSkills")]
    [JsonProperty("requiredSkills")]
    public List<string> RequiredSkills { get; set; } = [];

    [JsonPropertyName("safetyNotes")]
    [JsonProperty("safetyNotes")]
    [System.Text.Json.Serialization.JsonConverter(typeof(StringOrStringArrayJsonConverter))]
    public List<string> SafetyNotes { get; set; } = [];
}

public sealed class StringOrStringArrayJsonConverter : System.Text.Json.Serialization.JsonConverter<List<string>>
{
    public override List<string> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == System.Text.Json.JsonTokenType.String)
        {
            return [reader.GetString() ?? string.Empty];
        }

        if (reader.TokenType == System.Text.Json.JsonTokenType.Null)
        {
            return [];
        }

        if (reader.TokenType != System.Text.Json.JsonTokenType.StartArray)
        {
            throw new System.Text.Json.JsonException(
                "Expected safetyNotes to be a string or an array of strings.");
        }

        var notes = new List<string>();
        while (reader.Read() && reader.TokenType != System.Text.Json.JsonTokenType.EndArray)
        {
            if (reader.TokenType == System.Text.Json.JsonTokenType.String)
            {
                notes.Add(reader.GetString() ?? string.Empty);
            }
            else if (reader.TokenType != System.Text.Json.JsonTokenType.Null)
            {
                throw new System.Text.Json.JsonException(
                    "Expected each safetyNotes array entry to be a string.");
            }
        }

        if (reader.TokenType != System.Text.Json.JsonTokenType.EndArray)
        {
            throw new System.Text.Json.JsonException("The safetyNotes array was incomplete.");
        }

        return notes;
    }

    public override void Write(
        Utf8JsonWriter writer,
        List<string> value,
        JsonSerializerOptions options) =>
        System.Text.Json.JsonSerializer.Serialize(writer, value, options);
}
