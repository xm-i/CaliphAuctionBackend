namespace CaliphAuctionBackend.Dtos.Points;

public class PointPlanDto {
	public int Id {
		get;
		set;
	}

	public required string Name {
		get;
		set;
	}

	public int Points {
		get;
		set;
	}

	public int Price {
		get;
		set;
	}
}