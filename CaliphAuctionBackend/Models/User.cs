using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace CaliphAuctionBackend.Models;

[Index(nameof(Email), IsUnique = true)]
[Index(nameof(Username), IsUnique = true)]
public class User : BaseEntity {
	[Key]
	public int Id {
		get;
		set;
	}

	[Required]
	[MaxLength(255)]
	public required string Email {
		get;
		set;
	}

	[Required]
	[MaxLength(64)]
	public required string PasswordHash {
		get;
		set;
	}

	[Required]
	[MaxLength(64)]
	public required string PasswordSalt {
		get;
		set;
	}

	[Required]
	[MaxLength(50)]
	public required string Username {
		get;
		set;
	}

	public DateTime? LastLoginAt {
		get;
		set;
	}

	public DateTime? LastFailedLoginAt {
		get;
		set;
	}

	public int FailedLoginCount {
		get;
		set;
	} = 0;

	public bool EmailConfirmed {
		get;
		set;
	}

	public bool IsDeleted {
		get;
		set;
	}

	public bool IsBotUser {
		get;
		set;
	}

	/// <summary>冗長: 現在のポイント残高。破損時は取引履歴から再計算可</summary>
	public int PointBalance {
		get;
		set;
	}

	/// <summary>ポイント取引ヘッダ一覧</summary>
	public ICollection<PointTransaction> PointTransactions {
		get;
		set;
	} = [];

	/// <summary>ポイント原価一覧</summary>
	public ICollection<PointBalanceLot> PointBalanceLots {
		get;
		set;
	} = [];

	/// <summary>ポイント購入履歴</summary>
	public ICollection<PointPurchase> PointPurchases {
		get;
		set;
	} = [];
}