namespace PennyAuctionBackend.Dtos.AuctionItem;

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

	public DateTime EndTime {
		get;
		set;
	}

	public int CategoryId {
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
}