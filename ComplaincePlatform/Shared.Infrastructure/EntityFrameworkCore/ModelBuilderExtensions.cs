using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Shared.Infrastructure.DTO;
using System.Text.Json;

namespace Shared.Infrastructure.EntityFrameworkCore;

/// <summary>
/// Extension methods for configuring Explainability metadata in EF Core.
/// Uses JSON serialization to store ExplainabilityDto as nvarchar(max) for minimal migration impact.
/// </summary>
public static class ModelBuilderExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Configures the Explainability property to be stored as JSON in a nullable nvarchar(max) column.
    /// 
    /// Why JSON serialization was chosen:
    /// - Minimal migration impact: Uses existing nvarchar(max) column type, no schema changes required
    /// - No separate tables: Avoids creating additional tables or owned entity relationships
    /// - Flexible schema: Easy to add/remove fields in ExplainabilityDto without database migrations
    /// - Simple persistence: Single column per entity, no joins required
    /// - Nullable by default: Property is nullable, column remains nullable (no required constraint)
    /// 
    /// Alternative approaches considered:
    /// - Owned entity: Would require multiple columns per entity, more complex migrations
    /// - Separate table: Would require joins and foreign keys, more complex queries
    /// - Native JSON type: Not all databases support native JSON, nvarchar(max) is universal
    /// </summary>
    public static PropertyBuilder<ExplainabilityDto?> ConfigureExplainability(
        this PropertyBuilder<ExplainabilityDto?> propertyBuilder)
    {
        // Value converter: Serialize ExplainabilityDto to JSON string for storage
        // Deserialize JSON string back to ExplainabilityDto when reading
        var converter = new ValueConverter<ExplainabilityDto?, string>(
            v => v == null ? null : JsonSerializer.Serialize(v, JsonOptions),
            v => string.IsNullOrEmpty(v) ? null : JsonSerializer.Deserialize<ExplainabilityDto>(v, JsonOptions));

        propertyBuilder.HasConversion(converter);
        
        // Configure as nullable nvarchar(max) column - no separate table, minimal migration impact
        propertyBuilder.Metadata.SetAnnotation("Relational:ColumnType", "nvarchar(max)");

        return propertyBuilder;
    }
}
