using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PennyAuctionBackend.Models;

public class AuctionItem : BaseEntity {
	[Key]
	public int Id {
		get;
		set;
	}

	[Required]
	[MaxLength(50)]
	public required string Name {
		get;
		set;
	}

	[Required]
	[MaxLength(1000)]
	public required string Description {
		get;
		set;
	}

	[Required]
	[MaxLength(512)]
	public required string ThumbnailImageUrl {
		get;
		set;
	}

	[Required]
	[MaxLength(512)]
	public required string ImageUrl {
		get;
		set;
	}

	[Required]
	public long OriginalPrice {
		get;
		set;
	}

	[Required]
	public long StartingBid {
		get;
		set;
	}

	[Required]
	public long CurrentPrice {
		get;
		set;
	}

	[Required]
	public int CategoryId {
		get;
		set;
	}

	[ForeignKey(nameof(CategoryId))]
	[DeleteBehavior(DeleteBehavior.Restrict)]
	public required AuctionItemCategory Category {
		get;
		set;
	}

	public int? CurrentHighestBidUserId {
		get;
		set;
	}

	[ForeignKey(nameof(CurrentHighestBidUserId))]
	[DeleteBehavior(DeleteBehavior.Restrict)]
	public User? CurrentHighestBidUser {
		get;
		set;
	}

	[Required]
	public DateTime EndTime {
		get;
		set;
	}

	[Required]
	public AuctionStatus Status {
		get;
		set;
	}

	public ICollection<Bid> Bids {
		get;
		set;
	} = [];
}

public enum AuctionStatus {
	Preparing,
	Active,
	Ended
}