using System.ComponentModel.DataAnnotations;

namespace PennyAuctionBackend.Models;

/// <summary>
///     キャンペーン
/// </summary>
public class Campaign : BaseEntity {
	[Key]
	public int Id {
		get;
		set;
	}

	/// <summary>表示名</summary>
	[Required]
	[MaxLength(100)]
	public string Name {
		get;
		set;
	} = null!;

	/// <summary>説明</summary>
	[MaxLength(255)]
	public string? Description {
		get;
		set;
	}

	/// <summary>開始日時 (null なら即時)</summary>
	public DateTime? StartsAt {
		get;
		set;
	}

	/// <summary>終了日時 (null なら期限なし)</summary>
	public DateTime? EndsAt {
		get;
		set;
	}

	/// <summary>有効フラグ</summary>
	public bool IsActive {
		get;
		set;
	} = true;
}