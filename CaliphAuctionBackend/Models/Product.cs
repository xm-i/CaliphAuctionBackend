using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CaliphAuctionBackend.Models;

[Index(nameof(IsActive))]
[Index(nameof(CategoryId))]
public class Product : BaseEntity {
	[Key]
	public int Id {
		get;
		set;
	}

	[Required]
	[MaxLength(255)]
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
	public long StartingBid {
		get;
		set;
	}

	[Required]
	public long MinimumPrice {
		get;
		set;
	}

	[Required]
	public long BidIncrement {
		get;
		set;
	}

	[Required]
	public long OriginalPrice {
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

	[Required]
	public int StockQuantity {
		get;
		set;
	}

	[Required]
	public bool IsActive {
		get;
		set;
	} = true;

	[Required]
	public int DurationMinutes {
		get;
		set;
	}

	[Required]
	public int BidPointCost {
		get;
		set;
	}

	public ICollection<AuctionItem> AuctionItems {
		get;
		set;
	} = [];
}