using System.ComponentModel.DataAnnotations;

namespace PennyAuctionBackend.Models;

public class BaseEntity {
	[Required]
	public DateTime CreatedAt {
		get;
		set;
	}

	[Required]
	public DateTime UpdatedAt {
		get;
		set;
	}
}