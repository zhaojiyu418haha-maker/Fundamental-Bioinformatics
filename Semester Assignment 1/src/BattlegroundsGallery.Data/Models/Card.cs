using BattlegroundsGallery.Orm;
using BattlegroundsGallery.Orm.Attributes;

namespace BattlegroundsGallery.Data.Models;

[Table("cards")]
public sealed class Card : Entity
{
    [Column("slug")]
    public string Slug { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("attack")]
    public int Attack { get; set; }

    [Column("health")]
    public int Health { get; set; }

    [Column("tier")]
    public int Tier { get; set; }

    [Column("image_path")]
    public string ImagePath { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Tribe> Tribes { get; set; } = new List<Tribe>();
}
