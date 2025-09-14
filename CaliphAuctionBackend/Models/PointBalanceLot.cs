using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CaliphAuctionBackend.Models;

/// <summary>
///     単価付き残高ロット (購入 / ボーナスで生成され、入札消費で減少)。
/// </summary>
[Index(nameof(UserId), nameof(CreatedAt))]
public class PointBalanceLot : BaseEntity {
	/// <summary>ID</summary>
	[Key]
	public long Id {
		get;
		set;
	}

	/// <summary>ユーザーID</summary>
	[Required]
	public int UserId {
		get;
		set;
	}

	/// <summary>ユーザー</summary>
	[ForeignKey(nameof(UserId))]
	[DeleteBehavior(DeleteBehavior.Restrict)]
	public User User {
		get;
		set;
	} = null!;

	/// <summary>付与時点の単価</summary>
	[Precision(18, 2)]
	[Required]
	public decimal UnitPrice {
		get;
		set;
	}

	/// <summary>未消化ポイント数量 (0 で枯渇)</summary>
	[Required]
	public int QuantityRemaining {
		get;
		set;
	}

	/// <summary>このロットを参照するトランザクション明細一覧</summary>
	public ICollection<PointTransactionEntry> PointTransactionEntries {
		get;
		set;
	} = new List<PointTransactionEntry>();
}