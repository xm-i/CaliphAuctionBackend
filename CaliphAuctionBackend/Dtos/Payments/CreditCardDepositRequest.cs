namespace CaliphAuctionBackend.Dtos.Payments;

public class CreditCardDepositRequest {
	public required string CardNumber {
		get;
		set;
	}

	public required string CardHolder {
		get;
		set;
	}

	public required string ExpiryMonth {
		get;
		set;
	}

	public required string ExpiryYear {
		get;
		set;
	}

	public required string Cvv {
		get;
		set;
	}

	public int Amount {
		get;
		set;
	}
}

public class CreditCardDepositResponse {
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
	} = "credit_card";

	public DateTime ExpiresAtUtc {
		get;
		set;
	}
}