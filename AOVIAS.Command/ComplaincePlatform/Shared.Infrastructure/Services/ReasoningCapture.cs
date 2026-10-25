using System.Reflection;
using Shared.Infrastructure.DTO;

namespace Shared.Infrastructure.Services;

/// <summary>
/// Lightweight service for capturing and attaching explainability metadata to domain entities.
/// This implementation does not call AI or infer logic - it simply attaches the provided metadata.
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
public class ReasoningCapture : IReasoningCapture
{
    /// <summary>
    /// Attaches explainability metadata to an entity before persistence.
    /// Uses reflection to find and set the Explainability property on the entity.
    /// Sets GeneratedAt to UtcNow if not provided.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <param name="entity">The entity to attach explainability to</param>
    /// <param name="explainability">The explainability metadata to attach (must not be null)</param>
    /// <exception cref="ArgumentNullException">Thrown when entity or explainability is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when entity does not support Explainability property</exception>
    public void Attach<TEntity>(TEntity entity, ExplainabilityDto explainability) where TEntity : class
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        if (explainability == null)
        {
            throw new ArgumentNullException(nameof(explainability));
        }

        // Set GeneratedAt if not provided (default DateTime value)
        var explainabilityToAttach = explainability;
        if (explainability.GeneratedAt == default(DateTime))
        {
            explainabilityToAttach = explainability with { GeneratedAt = DateTime.UtcNow };
        }

        var property = GetExplainabilityProperty(entity.GetType());
        property.SetValue(entity, explainabilityToAttach);
    }

    /// <summary>
    /// Attaches explainability metadata to an entity, or clears it if null is provided.
    /// Sets GeneratedAt to UtcNow if not provided.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <param name="entity">The entity to attach explainability to</param>
    /// <param name="explainability">The explainability metadata to attach, or null to clear</param>
    /// <exception cref="ArgumentNullException">Thrown when entity is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when entity does not support Explainability property</exception>
    public void AttachOrClear<TEntity>(TEntity entity, ExplainabilityDto? explainability) where TEntity : class
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        // If explainability is null, clear it; otherwise set GeneratedAt if not provided
        ExplainabilityDto? explainabilityToAttach = null;
        if (explainability != null)
        {
            explainabilityToAttach = explainability.GeneratedAt == default(DateTime)
                ? explainability with { GeneratedAt = DateTime.UtcNow }
                : explainability;
        }

        var property = GetExplainabilityProperty(entity.GetType());
        property.SetValue(entity, explainabilityToAttach);
    }

    /// <summary>
    /// Gets the Explainability property from the entity type, with validation.
    /// </summary>
    private static PropertyInfo GetExplainabilityProperty(Type entityType)
    {
        var explainabilityProperty = entityType.GetProperty(
            "Explainability",
            BindingFlags.Public | BindingFlags.Instance);

        if (explainabilityProperty == null)
        {
            throw new InvalidOperationException(
                $"Entity type {entityType.Name} does not have an Explainability property. " +
                $"Ensure the entity has a property named 'Explainability' of type 'ExplainabilityDto?'.");
        }

        var propertyType = explainabilityProperty.PropertyType;
        var isNullableExplainability = Nullable.GetUnderlyingType(propertyType) == typeof(ExplainabilityDto);
        var isExplainability = propertyType == typeof(ExplainabilityDto);

        if (!isExplainability && !isNullableExplainability)
        {
            throw new InvalidOperationException(
                $"Entity type {entityType.Name} has an Explainability property but it is not of type " +
                $"ExplainabilityDto or ExplainabilityDto?. Found type: {propertyType.Name}.");
        }

        return explainabilityProperty;
    }
}
