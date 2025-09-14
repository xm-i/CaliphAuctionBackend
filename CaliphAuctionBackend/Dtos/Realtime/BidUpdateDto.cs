namespace CaliphAuctionBackend.Dtos.Realtime;

public class BidUpdateDto {
	public int AuctionItemId {
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

	public DateTime BidTime {
		get;
		set;
	}

	public int BidId {
		get;
		set;
	}

	public int CurrentHighestBidUserId {
		get;
		set;
	}

	public required string CurrentHighestBidUserName {
		get;
		set;
	}
}