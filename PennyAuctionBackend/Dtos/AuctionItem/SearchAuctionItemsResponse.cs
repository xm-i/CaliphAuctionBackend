namespace PennyAuctionBackend.Dtos.AuctionItem;

public class SearchAuctionItemsResponse {
	public required IReadOnlyList<AuctionItemSummaryDto> Items {
		get;
		set;
	}

	public int TotalCount {
		get;
		set;
	}

	public DateTime ServerTimeUtc {
		get;
		set;
	}
}