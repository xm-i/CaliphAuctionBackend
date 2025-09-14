namespace CaliphAuctionBackend.Dtos.AuctionItem;

public class BidHistoryDto {
	public int UserId {
		get;
		set;
	}

	public required string Username {
		get;
		set;
	}

	public long BidAmount {
		get;
		set;
	}

	public DateTime BidTime {
		get;
		set;
	}
}