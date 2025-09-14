using CaliphAuctionBackend.Models;

namespace CaliphAuctionBackend.Dtos.AuctionItem;

public class AuctionItemSummaryDto {
	public int Id {
		get;
		set;
	}

	public required string Name {
		get;
		set;
	}

	public required string ThumbnailImageUrl {
		get;
		set;
	}

	public long CurrentPrice {
		get;
		set;
	}

	public long BidIncrement {
		get;
		set;
	}

	public int BidPointCost {
		get;
		set;
	}

	public DateTime EndTime {
		get;
		set;
	}

	public int CategoryId {
		get;
		set;
	}

	public AuctionStatus Status {
		get;
		set;
	}

	public int? CurrentHighestBidUserId {
		get;
		set;
	}

	public string? CurrentHighestBidUserName {
		get;
		set;
	}

	public bool Purchased {
		get;
		set;
	}
}