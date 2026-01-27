namespace VehicleSearch.Core.Models;

/// <summary>
/// Configuration settings for Azure OpenAI.
/// </summary>
public class AzureOpenAIConfig
{
    /// <summary>
    /// Gets or sets the Azure OpenAI service endpoint.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Azure OpenAI API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the deployment name for the embedding model.
    /// </summary>
    public string EmbeddingDeploymentName { get; set; } = "text-embedding-ada-002";

    /// <summary>
    /// Gets or sets the maximum number of concurrent requests.
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum number of retries for failed requests.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the batch size for embedding generation.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the deployment name for the chat model.
    /// </summary>
    public string? ChatDeploymentName { get; set; }
}
