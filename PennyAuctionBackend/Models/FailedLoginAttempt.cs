using System.ComponentModel.DataAnnotations;

namespace PennyAuctionBackend.Models {
	public class FailedLoginAttempt : BaseEntity {
		[Key]
		public long Id {
			get;
			set;
		}

		[Required]
		[MaxLength(45)]
		public required string IpAddress {
			get;
			set;
		}

		[MaxLength(256)]
		public required string Email {
			get;
			set;
		}

		[Required]
		public DateTime AttemptedAt {
			get;
			set;
		}
	}
}