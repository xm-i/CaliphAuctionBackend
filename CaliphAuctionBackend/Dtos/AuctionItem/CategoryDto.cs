namespace CaliphAuctionBackend.Dtos.AuctionItem;

public class CategoryDto {
	public int Id {
		get;
		set;
	}

	public required string Name {
		get;
		set;
	}

	public required string Description {
		get;
		set;
	}

	public int ActiveItemCount {
		get;
		set;
	}
}