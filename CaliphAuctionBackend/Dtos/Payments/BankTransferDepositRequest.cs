namespace CaliphAuctionBackend.Dtos.Payments;

public class BankTransferDepositRequest {
	public required string BankName {
		get;
		set;
	}

	public required string BranchName {
		get;
		set;
	}

	public required string AccountNumber {
		get;
		set;
	}

	public required string AccountHolder {
		get;
		set;
	}

	public int Amount {
		get;
		set;
	}
}

public class BankTransferDepositResponse {
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
	} = "bank_transfer";

	public DateTime ExpiresAtUtc {
		get;
		set;
	}
}