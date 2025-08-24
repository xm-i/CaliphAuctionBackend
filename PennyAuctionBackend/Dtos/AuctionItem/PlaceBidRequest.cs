namespace PennyAuctionBackend.Dtos.AuctionItem;

public class PlaceBidRequest
{
    public int AuctionItemId { get; set; }
    public int BidAmount { get; set; }
}
