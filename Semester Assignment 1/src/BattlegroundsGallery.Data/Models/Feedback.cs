using BattlegroundsGallery.Orm;
using BattlegroundsGallery.Orm.Attributes;

namespace BattlegroundsGallery.Data.Models;

[Table("feedback")]
public sealed class Feedback : Entity
{
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("message")]
    public string Message { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
