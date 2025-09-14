using System.ComponentModel.DataAnnotations;

namespace CaliphAuctionBackend.Dtos.User;

public class RegisterUserDto {
	[Required(ErrorMessage = "Email is required.")]
	[EmailAddress(ErrorMessage = "Invalid email format.")]
	[StringLength(255, MinimumLength = 5, ErrorMessage = "Email must be 5-255 characters.")]
	public required string Email {
		get;
		set;
	}

	[Required(ErrorMessage = "Password is required.")]
	[StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be 8-100 characters.")]
	public required string Password {
		get;
		set;
	}


	[Required(ErrorMessage = "Username is required.")]
	[StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be 3-50 characters.")]
	public required string Username {
		get;
		set;
	}
}