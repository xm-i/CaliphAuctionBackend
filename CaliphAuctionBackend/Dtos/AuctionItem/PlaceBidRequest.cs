namespace CaliphAuctionBackend.Dtos.AuctionItem;

public class PlaceBidRequest {
	public int AuctionItemId {
		get;
		set;
	}

	public long BidAmount {
		get;
		set;
	}
}