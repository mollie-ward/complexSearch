using VehicleSearch.Core.Models;

namespace VehicleSearch.Core.Interfaces;

/// <summary>
/// Service for resolving references and refining queries in conversation context.
/// </summary>
public interface IReferenceResolverService
{
    /// <summary>
    /// Resolves pronouns and references in a query using conversation context.
    /// </summary>
    /// <param name="query">The user query containing references.</param>
    /// <param name="session">The conversation session with history and state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A resolved query with references replaced.</returns>
    Task<ResolvedQuery> ResolveReferencesAsync(
        string query, 
        ConversationSession session, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves comparative terms against previous search constraints.
    /// </summary>
    /// <param name="parsedQuery">The parsed query containing comparative terms.</param>
    /// <param name="searchState">The search state with active filters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated search constraints with resolved comparatives.</returns>
    Task<Dictionary<string, SearchConstraint>> ResolveComparativesAsync(
        ParsedQuery parsedQuery, 
        SearchState searchState, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts reference patterns from a query.
    /// </summary>
    /// <param name="query">The user query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of references found in the query.</returns>
    Task<List<Reference>> ExtractReferentsAsync(
        string query, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refines a query by combining new constraints with previous search state.
    /// </summary>
    /// <param name="newQuery">The new parsed query with constraints.</param>
    /// <param name="searchState">The search state with active filters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A composed query with merged constraints.</returns>
    Task<ComposedQuery> RefineQueryAsync(
        ParsedQuery newQuery, 
        SearchState searchState, 
        CancellationToken cancellationToken = default);
}
