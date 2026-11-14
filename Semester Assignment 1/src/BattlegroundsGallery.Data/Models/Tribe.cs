using BattlegroundsGallery.Orm;
using BattlegroundsGallery.Orm.Attributes;

namespace BattlegroundsGallery.Data.Models;

[Table("tribes")]
public sealed class Tribe : Entity
{
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("color")]
    public string Color { get; set; } = "#8a6a3f";

    public ICollection<Card> Cards { get; set; } = new List<Card>();
}
