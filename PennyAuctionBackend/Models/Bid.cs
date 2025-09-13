using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PennyAuctionBackend.Models;

[Index(nameof(AuctionItemId), nameof(BidAmount), IsUnique = true)]
public class Bid : BaseEntity {
	[Key]
	public int Id {
		get;
		set;
	}

	[Required]
	public int AuctionItemId {
		get;
		set;
	}

	[ForeignKey(nameof(AuctionItemId))]
	[DeleteBehavior(DeleteBehavior.Restrict)]
	public required AuctionItem AuctionItem {
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

	[Required]
	public long BidAmount {
		get;
		set;
	}

	[Required]
	public DateTime BidTime {
		get;
		set;
	}

	[MaxLength(45)]
	[Required]
	public string? IpAddress {
		get;
		set;
	}
}