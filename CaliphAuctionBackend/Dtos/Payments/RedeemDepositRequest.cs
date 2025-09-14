namespace CaliphAuctionBackend.Dtos.Payments;

public class RedeemDepositRequest {
	public required string DepositToken {
		get;
		set;
	}

	public int PointPlanId {
		get;
		set;
	}
}