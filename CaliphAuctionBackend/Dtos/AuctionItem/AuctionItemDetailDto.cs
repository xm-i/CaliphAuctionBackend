using CaliphAuctionBackend.Models;

namespace CaliphAuctionBackend.Dtos.AuctionItem;

public class AuctionItemDetailDto {
	public int Id {
		get;
		set;
	}

	public required string Name {
		get;
		set;
	}

	public required string Description {
		get;
		set;
	}

	public required string ThumbnailImageUrl {
		get;
		set;
	}

	public required string ImageUrl {
		get;
		set;
	}

	public long OriginalPrice {
		get;
		set;
	}

	public long StartingBid {
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

	public long CurrentPrice {
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

	public required string CategoryName {
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

	public int BidCount {
		get;
		set;
	}

	public AuctionStatus Status {
		get;
		set;
	}

	public DateTime ServerTimeUtc {
		get;
		set;
	}

	public IReadOnlyList<BidHistoryDto> BidHistories {
		get;
		set;
	} = [];
}