namespace VehicleSearch.Core.Models;

/// <summary>
/// Configuration settings for Azure AI Search.
/// </summary>
public class AzureSearchConfig
{
    /// <summary>
    /// Gets or sets the Azure Search service endpoint.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Azure Search API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the index name.
    /// </summary>
    public string IndexName { get; set; } = "vehicles-index";

    /// <summary>
    /// Gets or sets the vector dimensions for embeddings.
    /// Default is 1536 for OpenAI ada-002 embeddings.
    /// </summary>
    public int VectorDimensions { get; set; } = 1536;
}
