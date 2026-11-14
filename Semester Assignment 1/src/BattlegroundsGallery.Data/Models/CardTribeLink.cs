using BattlegroundsGallery.Orm;
using BattlegroundsGallery.Orm.Attributes;

namespace BattlegroundsGallery.Data.Models;

[Table("cards_tribes")]
public sealed class CardTribeLink : Entity
{
    [Column("card_id")]
    public int CardId { get; set; }

    [Column("tribe_id")]
    public int TribeId { get; set; }

    [Column("is_primary")]
    public bool IsPrimary { get; set; }
}
