using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CaliphAuctionBackend.Models;

/// <summary>
///     ポイントトランザクション明細。単一の残高ロット/単価に対する入出庫行。
/// </summary>
[Index(nameof(PointTransactionId))]
public class PointTransactionEntry : BaseEntity {
	/// <summary>ID</summary>
	[Key]
	public long Id {
		get;
		set;
	}

	/// <summary>親トランザクションID</summary>
	[Required]
	public long PointTransactionId {
		get;
		set;
	}

	/// <summary>親トランザクション navigation</summary>
	[ForeignKey(nameof(PointTransactionId))]
	[DeleteBehavior(DeleteBehavior.Cascade)]
	public PointTransaction Transaction {
		get;
		set;
	} = null!;

	/// <summary>数量</summary>
	[Required]
	public int Quantity {
		get;
		set;
	}

	/// <summary>残高ロットID</summary>
	[Required]
	public long PointBalanceLotId {
		get;
		set;
	}

	/// <summary>残高ロット</summary>
	[ForeignKey(nameof(PointBalanceLotId))]
	[DeleteBehavior(DeleteBehavior.Restrict)]
	public PointBalanceLot? PointBalanceLot {
		get;
		set;
	}

	/// <summary>単価</summary>
	[Precision(18, 2)]
	[Required]
	public decimal UnitPrice {
		get;
		set;
	}

	/// <summary>合計金額</summary>
	[Required]
	public int TotalPrice {
		get;
		set;
	}
}