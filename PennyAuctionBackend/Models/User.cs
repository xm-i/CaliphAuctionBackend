using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace PennyAuctionBackend.Models;

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
	} = false;

	public bool IsDeleted {
		get;
		set;
	} = false;
}