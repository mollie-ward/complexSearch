namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents the type of reference in a query.
/// </summary>
public enum ReferenceType
{
    /// <summary>
    /// Pronoun reference (e.g., "it", "them", "those").
    /// </summary>
    Pronoun,

    /// <summary>
    /// Demonstrative reference (e.g., "this", "that", "these").
    /// </summary>
    Demonstrative,

    /// <summary>
    /// Comparative reference (e.g., "cheaper", "bigger", "newer").
    /// </summary>
    Comparative,

    /// <summary>
    /// Anaphoric reference (e.g., "the BMW", "the previous one").
    /// </summary>
    Anaphoric,

    /// <summary>
    /// Implicit reference (implied references).
    /// </summary>
    Implicit
}
