namespace PennyAuctionBackend.Dtos.Payments;

public class PayPalDepositRequest {
	public required string LoginId {
		get;
		set;
	}

	public required string Password {
		get;
		set;
	}

	public int Amount {
		get;
		set;
	}
}

public class PayPalDepositResponse {
	public required string DepositToken {
		get;
		set;
	}

	public int Amount {
		get;
		set;
	}

	public string Provider {
		get;
		set;
	} = "paypal";

	public DateTime ExpiresAtUtc {
		get;
		set;
	}
}