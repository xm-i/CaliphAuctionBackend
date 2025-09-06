using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PennyAuctionBackend.Models;

[Index(nameof(UserId), nameof(CreatedAt))]
public class Notification : BaseEntity {
	[Key]
	public long Id {
		get;
		set;
	}

	/// <summary>null -> 全体通知</summary>
	public int? UserId {
		get;
		set;
	}

	[ForeignKey(nameof(UserId))]
	[DeleteBehavior(DeleteBehavior.Restrict)]
	public User? User {
		get;
		set;
	}

	[MaxLength(50)]
	public string? Category {
		get;
		set;
	}

	[Required]
	[MaxLength(120)]
	public required string Title {
		get;
		set;
	}

	[Required]
	[MaxLength(500)]
	public required string Message {
		get;
		set;
	}

	public bool IsRead {
		get;
		set;
	}
}