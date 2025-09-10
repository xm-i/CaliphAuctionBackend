using System.ComponentModel.DataAnnotations;

namespace PennyAuctionBackend.Dtos.Purchases;

public class PurchaseWonProductRequest {
	[Required]
	public int AuctionId {
		get;
		set;
	}

	[Required]
	[MaxLength(32)]
	public required string Prefecture {
		get;
		set;
	}

	[Required]
	[MaxLength(64)]
	public required string City {
		get;
		set;
	}

	[Required]
	[MaxLength(128)]
	public required string AddressLine1 {
		get;
		set;
	}

	[MaxLength(128)]
	public string? AddressLine2 {
		get;
		set;
	}

	[Required]
	public DateOnly DeliveryDate {
		get;
		set;
	}

	[Required]
	[Range(1, 7)]
	public int DeliveryTimeSlot {
		get;
		set;
	}

	[Required]
	[Range(1, 5)]
	public int ShippingCarrier {
		get;
		set;
	}

	[Required]
	public required string DepositToken {
		get;
		set;
	}
}