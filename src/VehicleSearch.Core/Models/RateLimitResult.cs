namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents the result of a rate limit check.
/// </summary>
public class RateLimitResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the request is allowed.
    /// </summary>
    public bool IsAllowed { get; set; }

    /// <summary>
    /// Gets or sets the number of remaining requests in the current window.
    /// </summary>
    public int RemainingRequests { get; set; }

    /// <summary>
    /// Gets or sets the time to wait before retrying if rate limit is exceeded.
    /// </summary>
    public TimeSpan RetryAfter { get; set; }
}
