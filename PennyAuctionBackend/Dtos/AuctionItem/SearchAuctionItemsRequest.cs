namespace PennyAuctionBackend.Dtos.AuctionItem;

public class SearchAuctionItemsRequest {
	public int CategoryId {
		get;
		set;
	}

	public int Page {
		get;
		set;
	} = 1;
}