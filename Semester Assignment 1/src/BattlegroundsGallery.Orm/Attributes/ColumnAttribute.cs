namespace BattlegroundsGallery.Orm.Attributes;

[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public sealed class ColumnAttribute : Attribute
{
    public ColumnAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    public string Name { get; }
}
