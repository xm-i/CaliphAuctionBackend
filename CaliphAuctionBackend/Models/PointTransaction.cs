using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CaliphAuctionBackend.Models;

/// <summary>
///     ポイントトランザクション(ヘッダ)。
///     単一のビジネス操作(購入/消費/キャンペーン付与)を表し、複数の明細(PointTransactionEntry)を持つ。
///     Entries の合計が TotalAmount と一致する前提。
/// </summary>
[Index(nameof(UserId), nameof(CreatedAt))]
public class PointTransaction : BaseEntity {
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

	/// <summary>取引種別</summary>
	[Required]
	public PointTransactionType Type {
		get;
		set;
	}

	/// <summary>合計ポイント変動量 (購入/付与=正, 消費=負)</summary>
	[Required]
	public int TotalAmount {
		get;
		set;
	}

	/// <summary>この取引適用後の残高</summary>
	[Required]
	public int BalanceAfter {
		get;
		set;
	}

	/// <summary>キャンペーンID (キャンペーン付与時)</summary>
	public int? CampaignId {
		get;
		set;
	}

	[ForeignKey(nameof(CampaignId))]
	[DeleteBehavior(DeleteBehavior.Restrict)]
	public Campaign? Campaign {
		get;
		set;
	}

	/// <summary>ポイント購入ID (購入時)</summary>
	public int? PointPurchaseId {
		get;
		set;
	}

	[ForeignKey(nameof(PointPurchaseId))]
	[DeleteBehavior(DeleteBehavior.Restrict)]
	public PointPurchase? PointPurchase {
		get;
		set;
	}

	/// <summary>入札ID (消費時)</summary>
	public int? BidId {
		get;
		set;
	}

	[ForeignKey(nameof(BidId))]
	[DeleteBehavior(DeleteBehavior.Restrict)]
	public Bid? Bid {
		get;
		set;
	}

	/// <summary>補足メモ</summary>
	[MaxLength(255)]
	public string? Note {
		get;
		set;
	}

	/// <summary>明細行コレクション</summary>
	public ICollection<PointTransactionEntry> Entries {
		get;
		set;
	} = [];
}