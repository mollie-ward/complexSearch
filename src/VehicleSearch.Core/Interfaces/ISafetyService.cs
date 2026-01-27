namespace VehicleSearch.Core.Interfaces;

/// <summary>
/// Interface for safety service operations.
/// </summary>
public interface ISafetyService
{
    /// <summary>
    /// Validates and sanitizes user input for safety.
    /// </summary>
    /// <param name="input">The user input to validate.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>True if the input is safe, otherwise false.</returns>
    Task<bool> ValidateInputAsync(string input, CancellationToken cancellationToken = default);
}
