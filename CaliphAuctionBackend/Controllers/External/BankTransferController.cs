using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using CaliphAuctionBackend.Dtos.Payments;
using CaliphAuctionBackend.Exceptions;

namespace CaliphAuctionBackend.Controllers.External;

[ApiController]
[Route("external/bank-transfer")]
[AllowAnonymous]
public class BankTransferController(IConfiguration configuration) : ControllerBase {
	private readonly IConfiguration _configuration = configuration;

	[HttpPost("deposit")] // POST /external/bank-transfer/deposit
	public ActionResult<BankTransferDepositResponse> CreateDepositToken([FromBody] BankTransferDepositRequest request) {
		if (string.IsNullOrWhiteSpace(request.BankName) || string.IsNullOrWhiteSpace(request.BranchName) ||
		    string.IsNullOrWhiteSpace(request.AccountNumber) || string.IsNullOrWhiteSpace(request.AccountHolder)) {
			return this.BadRequest("BankName, BranchName, AccountNumber and AccountHolder are required");
		}

		if (request.Amount <= 0) {
			return this.BadRequest("Amount must be positive");
		}

		var key = this._configuration["ExternalPaymentJwt:Key"] ?? throw new ConfigurationCaliphException("ExternalPaymentJwt:Key configuration is missing");
		var issuer = this._configuration["ExternalPaymentJwt:Issuer"] ?? throw new ConfigurationCaliphException("ExternalPaymentJwt:Issuer configuration is missing");
		var expireMinutesStr = this._configuration["ExternalPaymentJwt:ExpireMinutes"] ?? throw new ConfigurationCaliphException("ExternalPaymentJwt:ExpireMinutes is missing");
		var expireMinutes = int.Parse(expireMinutesStr);

		var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
		var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
		var jti = Guid.NewGuid().ToString("N");
		var expires = DateTime.UtcNow.AddMinutes(expireMinutes);
		var claims = new List<Claim> {
			new("tokenType", "deposit"),
			new("provider", "bank_transfer"),
			new("amount", request.Amount.ToString()),
			new(JwtRegisteredClaimNames.Jti, jti)
		};
		var token = new JwtSecurityToken(
			issuer,
			null,
			claims,
			expires: expires,
			signingCredentials: credentials
		);
		var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
		return new BankTransferDepositResponse { DepositToken = tokenString, Amount = request.Amount, ExpiresAtUtc = expires };
	}
}