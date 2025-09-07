using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace PennyAuctionBackend.Models;

[Index(nameof(Name), IsUnique = true)]
public class AuctionItemCategory : BaseEntity {
	[Key]
	public int Id {
		get;
		set;
	}

	[Required]
	[MaxLength(50)]
	public required string Name {
		get;
		set;
	}

	[MaxLength(255)]
	[Required]
	public required string Description {
		get;
		set;
	}

	public ICollection<AuctionItem> AuctionItems {
		get;
		set;
	} = [];
}