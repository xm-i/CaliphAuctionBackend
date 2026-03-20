using System.ComponentModel.DataAnnotations;

namespace CaliphAuctionBackend.Dtos.User;

public class LoginDto : IValidatableObject {
	[EmailAddress(ErrorMessage = "Invalid email format.")]
	[StringLength(255, MinimumLength = 5, ErrorMessage = "Email must be 5-255 characters.")]
	public string? Email {
		get;
		set;
	}

	public int? UserId {
		get;
		set;
	}

	[Required(ErrorMessage = "Password is required.")]
	[StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be 8-100 characters.")]
	public required string Password {
		get;
		set;
	}

	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
		if (string.IsNullOrWhiteSpace(this.Email) && !this.UserId.HasValue) {
			yield return new ValidationResult("Either Email or UserId is required.", [nameof(this.Email), nameof(this.UserId)]);
		}
	}
}