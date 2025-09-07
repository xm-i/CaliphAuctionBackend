using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using PennyAuctionBackend.Dtos.Payments;
using PennyAuctionBackend.Exceptions;

namespace PennyAuctionBackend.Controllers.External;

[ApiController]
[Route("external/credit-card")]
[AllowAnonymous]
public class CreditCardController(IConfiguration configuration) : ControllerBase {
	private readonly IConfiguration _configuration = configuration;

	[HttpPost("deposit")] // POST /external/credit-card/deposit
	public ActionResult<CreditCardDepositResponse> CreateDepositToken([FromBody] CreditCardDepositRequest request) {
		if (string.IsNullOrWhiteSpace(request.CardNumber) || string.IsNullOrWhiteSpace(request.CardHolder) ||
		    string.IsNullOrWhiteSpace(request.ExpiryMonth) || string.IsNullOrWhiteSpace(request.ExpiryYear) ||
		    string.IsNullOrWhiteSpace(request.Cvv)) {
			return this.BadRequest("CardNumber, CardHolder, ExpiryMonth, ExpiryYear and Cvv are required");
		}

		if (request.Amount <= 0) {
			return this.BadRequest("Amount must be positive");
		}

		var key = this._configuration["ExternalPaymentJwt:Key"] ?? throw new ConfigurationPennyException("ExternalPaymentJwt:Key configuration is missing");
		var issuer = this._configuration["ExternalPaymentJwt:Issuer"] ?? throw new ConfigurationPennyException("ExternalPaymentJwt:Issuer configuration is missing");
		var expireMinutesStr = this._configuration["ExternalPaymentJwt:ExpireMinutes"] ?? throw new ConfigurationPennyException("ExternalPaymentJwt:ExpireMinutes is missing");
		var expireMinutes = int.Parse(expireMinutesStr);

		var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
		var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
		var jti = Guid.NewGuid().ToString("N");
		var expires = DateTime.UtcNow.AddMinutes(expireMinutes);
		var claims = new List<Claim> { new("tokenType", "deposit"), new("provider", "credit_card"), new("amount", request.Amount.ToString()), new(JwtRegisteredClaimNames.Jti, jti) };
		var token = new JwtSecurityToken(
			issuer,
			null,
			claims,
			expires: expires,
			signingCredentials: credentials
		);
		var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
		return new CreditCardDepositResponse { DepositToken = tokenString, Amount = request.Amount, ExpiresAtUtc = expires };
	}
}