using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PennyAuctionBackend.Models;

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
	public required User User {
		get;
		set;
	}

	[Required]
	public int BidAmount {
		get;
		set;
	}

	[Required]
	public DateTime BidTime {
		get;
		set;
	}
}