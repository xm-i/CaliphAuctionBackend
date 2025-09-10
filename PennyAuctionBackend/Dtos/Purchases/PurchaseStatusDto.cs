namespace PennyAuctionBackend.Dtos.Purchases;

public class PurchaseStatusDto {
	public int AuctionItemId {
		get;
		set;
	}

	public bool Purchased {
		get;
		set;
	}

	public DateOnly? DeliveryDate {
		get;
		set;
	}

	public int? DeliveryTimeSlot {
		get;
		set;
	}

	public int? ShippingCarrier {
		get;
		set;
	}

	public string? Prefecture {
		get;
		set;
	}

	public string? City {
		get;
		set;
	}

	public string? AddressLine1 {
		get;
		set;
	}

	public string? AddressLine2 {
		get;
		set;
	}
}