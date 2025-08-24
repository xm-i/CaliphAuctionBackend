namespace PennyAuctionBackend.Dtos.AuctionItem;

public class BidHistoryDto {
	public int UserId {
		get;
		set;
	}

	public required string Username {
		get;
		set;
	}

	public int BidAmount {
		get;
		set;
	}

	public DateTime BidTime {
		get;
		set;
	}
}