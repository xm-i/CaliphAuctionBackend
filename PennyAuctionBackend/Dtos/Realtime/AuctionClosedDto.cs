namespace PennyAuctionBackend.Dtos.Realtime;

public class AuctionClosedDto {
	public int AuctionItemId {
		get;
		set;
	}

	public long FinalPrice {
		get;
		set;
	}

	public DateTime EndTime {
		get;
		set;
	}

	public int Status {
		get;
		set;
	}

	public int WinnerUserId {
		get;
		set;
	}
}