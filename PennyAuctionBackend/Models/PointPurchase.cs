using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PennyAuctionBackend.Models;

/// <summary>
///     ポイント購入申込/確定レコード。外部決済とユーザー付与を紐づける。
/// </summary>
[Index(nameof(UserId), nameof(CreatedAt))]
[Index(nameof(ExternalTokenJti), IsUnique = true)]
public class PointPurchase : BaseEntity {
	[Key]
	public int Id {
		get;
		set;
	}

	[Required]
	public int UserId {
		get;
		set;
	}

	[ForeignKey(nameof(UserId))]
	[DeleteBehavior(DeleteBehavior.Restrict)]
	public User User {
		get;
		set;
	} = null!;

	/// <summary>購入ポイント数量</summary>
	[Required]
	public int Points {
		get;
		set;
	}

	/// <summary>支払総額 (円, 2桁)</summary>
	[Required]
	public int AmountPaid {
		get;
		set;
	}

	/// <summary>メモ</summary>
	[MaxLength(255)]
	public string? Note {
		get;
		set;
	}

	/// <summary>外部決済トークンJTI (再利用防止) - 外部経路以外は null</summary>
	[MaxLength(64)]
	public string? ExternalTokenJti {
		get;
		set;
	}
}