using System.Text.Json.Serialization;

namespace ClaudeCodeWrapper.Models;

/// <summary>
/// Token usage information from a Claude API response.
/// </summary>
public record TokenUsage
{
    /// <summary>
    /// Number of input tokens consumed.
    /// </summary>
    [JsonPropertyName("input_tokens")]
    public int InputTokens { get; init; }

    /// <summary>
    /// Number of output tokens generated.
    /// </summary>
    [JsonPropertyName("output_tokens")]
    public int OutputTokens { get; init; }

    /// <summary>
    /// Tokens used to create cache entries.
    /// </summary>
    [JsonPropertyName("cache_creation_input_tokens")]
    public int CacheCreationInputTokens { get; init; }

    /// <summary>
    /// Tokens read from cache (cost savings).
    /// </summary>
    [JsonPropertyName("cache_read_input_tokens")]
    public int CacheReadInputTokens { get; init; }

    /// <summary>
    /// Service tier used (e.g., "standard").
    /// </summary>
    [JsonPropertyName("service_tier")]
    public string? ServiceTier { get; init; }

    /// <summary>
    /// Ephemeral cache creation details.
    /// </summary>
    [JsonPropertyName("cache_creation")]
    public CacheCreation? CacheCreation { get; init; }

    /// <summary>
    /// Server-side tool usage (web search, fetch).
    /// </summary>
    [JsonPropertyName("server_tool_use")]
    public ServerToolUse? ServerToolUse { get; init; }

    /// <summary>
    /// Total tokens (input + output).
    /// </summary>
    [JsonIgnore]
    public int TotalTokens => InputTokens + OutputTokens;

    /// <summary>
    /// Effective input tokens (excluding cache reads).
    /// </summary>
    [JsonIgnore]
    public int EffectiveInputTokens => InputTokens - CacheReadInputTokens;

    /// <summary>
    /// Cache hit rate (0-1).
    /// </summary>
    [JsonIgnore]
    public double CacheHitRate => InputTokens > 0
        ? (double)CacheReadInputTokens / InputTokens
        : 0;
}

/// <summary>
/// Ephemeral cache creation information.
/// </summary>
public record CacheCreation
{
    /// <summary>
    /// Tokens in 5-minute ephemeral cache.
    /// </summary>
    [JsonPropertyName("ephemeral_5m_input_tokens")]
    public int Ephemeral5mInputTokens { get; init; }

    /// <summary>
    /// Tokens in 1-hour ephemeral cache.
    /// </summary>
    [JsonPropertyName("ephemeral_1h_input_tokens")]
    public int Ephemeral1hInputTokens { get; init; }
}

/// <summary>
/// Server-side tool usage information.
/// </summary>
public record ServerToolUse
{
    /// <summary>
    /// Number of web search requests made.
    /// </summary>
    [JsonPropertyName("web_search_requests")]
    public int WebSearchRequests { get; init; }

    /// <summary>
    /// Number of web fetch requests made.
    /// </summary>
    [JsonPropertyName("web_fetch_requests")]
    public int WebFetchRequests { get; init; }
}
