using Shared.Infrastructure.DTO;

namespace Shared.Infrastructure.Services;

/// <summary>
/// Lightweight service for capturing and attaching explainability metadata to domain entities.
/// This is the single future entry point for LLM integration.
/// 
/// Responsibilities:
/// - Accept Explainability metadata as input
/// - Attach it to a given domain entity
/// - Set GeneratedAt if not provided
/// - Do NOT generate reasoning
/// - Do NOT infer logic
/// - Do NOT call any AI or external services
/// </summary>
public interface IReasoningCapture
{
    /// <summary>
    /// Attaches explainability metadata to an entity before persistence.
    /// Sets GeneratedAt to UtcNow if not provided.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <param name="entity">The entity to attach explainability to</param>
    /// <param name="explainability">The explainability metadata to attach (must not be null)</param>
    /// <exception cref="ArgumentNullException">Thrown when entity or explainability is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when entity does not support Explainability property</exception>
    void Attach<TEntity>(TEntity entity, ExplainabilityDto explainability) where TEntity : class;

    /// <summary>
    /// Attaches explainability metadata to an entity, or clears it if null is provided.
    /// Sets GeneratedAt to UtcNow if not provided.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <param name="entity">The entity to attach explainability to</param>
    /// <param name="explainability">The explainability metadata to attach, or null to clear</param>
    /// <exception cref="ArgumentNullException">Thrown when entity is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when entity does not support Explainability property</exception>
    void AttachOrClear<TEntity>(TEntity entity, ExplainabilityDto? explainability) where TEntity : class;
}
