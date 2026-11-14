using BattlegroundsGallery.Orm;
using BattlegroundsGallery.Orm.Attributes;

namespace BattlegroundsGallery.Data.Models;

[Table("users")]
public sealed class User : Entity
{
    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("role")]
    public string Role { get; set; } = "user";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
