namespace CaliphAuctionBackend.Dtos.User;

public class LoginResultDto {
	public required string AccessToken {
		get;
		set;
	}

	public required UserSummaryDto User {
		get;
		set;
	}
}

public class UserSummaryDto {
	public int Id {
		get;
		set;
	}

	public required string Email {
		get;
		set;
	}

	public required string Username {
		get;
		set;
	}
}