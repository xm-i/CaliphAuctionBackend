using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CaliphAuctionBackend.Models;

[Index(nameof(AuctionItemId), IsUnique = true)]
[Index(nameof(DepositTokenJti), IsUnique = true)]
[Index(nameof(UserId))]
public class AuctionItemPurchase : BaseEntity {
	[Key]
	public long Id {
		get;
		set;
	}

	[Required]
	public int UserId {
		get;
		set;
	}

	[ForeignKey(nameof(UserId))]
	[DeleteBehavior(DeleteBehavior.Restrict)]
	public User User {
		get;
		set;
	} = null!;

	[Required]
	public int AuctionItemId {
		get;
		set;
	}

	[ForeignKey(nameof(AuctionItemId))]
	[DeleteBehavior(DeleteBehavior.Restrict)]
	public AuctionItem AuctionItem {
		get;
		set;
	} = null!;

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
	public int DeliveryTimeSlot {
		get;
		set;
	}

	[Required]
	public int ShippingCarrier {
		get;
		set;
	}

	[Required]
	[MaxLength(64)]
	public required string DepositTokenJti {
		get;
		set;
	}

	[Required]
	public int DepositAmount {
		get;
		set;
	}
}