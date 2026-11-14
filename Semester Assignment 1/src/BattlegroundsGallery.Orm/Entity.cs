using BattlegroundsGallery.Orm.Attributes;

namespace BattlegroundsGallery.Orm;

/// <summary>
/// Base entity for the custom ORM. The database layer maps this Id to the
/// primary key of the corresponding table.
/// </summary>
public abstract class Entity
{
    [PrimaryKey]
    [Column("id")]
    public int Id { get; set; }
}
