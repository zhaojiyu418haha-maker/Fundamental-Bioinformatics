using System.Collections.Concurrent;
using System.Reflection;
using BattlegroundsGallery.Orm.Attributes;

namespace BattlegroundsGallery.Orm;

internal sealed class EntityMetadata
{
    private static readonly ConcurrentDictionary<Type, EntityMetadata> Cache = new();

    private EntityMetadata(Type entityType)
    {
        EntityType = entityType;
        TableName = entityType.GetCustomAttribute<TableAttribute>()?.Name
                    ?? entityType.Name.ToLowerInvariant();

        var properties = entityType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanRead && property.CanWrite)
            .Select(property => new
            {
                Property = property,
                Column = property.GetCustomAttribute<ColumnAttribute>()?.Name,
            })
            .Where(item => item.Column is not null)
            .Select(item => new EntityProperty(item.Property, item.Column!))
            .ToList();

        Properties = properties;
        IdProperty = properties.FirstOrDefault(item =>
                         item.Property.GetCustomAttribute<PrimaryKeyAttribute>() is not null)
                     ?? properties.FirstOrDefault(item =>
                         item.Property.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                     ?? throw new InvalidOperationException(
                         $"Entity {entityType.Name} has no primary key property.");
    }

    public Type EntityType { get; }

    public string TableName { get; }

    public IReadOnlyList<EntityProperty> Properties { get; }

    public EntityProperty IdProperty { get; }

    public static EntityMetadata For<TEntity>() => For(typeof(TEntity));

    public static EntityMetadata For(Type entityType)
        => Cache.GetOrAdd(entityType, static type => new EntityMetadata(type));
}

internal sealed record EntityProperty(PropertyInfo Property, string ColumnName);
