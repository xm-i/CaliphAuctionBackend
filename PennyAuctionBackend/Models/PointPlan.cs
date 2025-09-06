using System.ComponentModel.DataAnnotations;

namespace PennyAuctionBackend.Models;

/// <summary>
///     ポイント購入プラン定義テーブル。
/// </summary>
public class PointPlan : BaseEntity {
	[Key]
	public int Id {
		get;
		set;
	}

	/// <summary>プラン名</summary>
	[Required]
	[MaxLength(100)]
	public required string Name {
		get;
		set;
	}

	/// <summary>付与ポイント数</summary>
	[Required]
	public int Points {
		get;
		set;
	}

	/// <summary>価格</summary>
	[Required]
	public int Price {
		get;
		set;
	}
}