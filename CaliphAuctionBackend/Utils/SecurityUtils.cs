using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace CaliphAuctionBackend.Utils;

public static class SecurityUtils {
	public static string HashPassword(string password, string salt, string pepper) {
		var combined = password + pepper;
		var saltBytes = Convert.FromBase64String(salt);
		var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
			combined,
			saltBytes,
			100_000,
			HashAlgorithmName.SHA256,
			32
		);
		return Convert.ToBase64String(hashBytes);
	}

	public static string GenerateSalt(int size = 32) {
		var saltBytes = RandomNumberGenerator.GetBytes(size);
		return Convert.ToBase64String(saltBytes);
	}

	/// <summary>
	///     指定した情報をもとに JWT トークンを生成する
	/// </summary>
	/// <param name="userId">ユーザーの一意な ID</param>
	/// <param name="email">ユーザーのメールアドレス</param>
	/// <param name="secretKey">署名用の秘密鍵（対称鍵）</param>
	/// <param name="issuer">トークン発行者</param>
	/// <param name="expireMinutes">トークンの有効期限（分）</param>
	/// <returns>生成された JWT トークン文字列</returns>
	public static string GenerateJwtToken(string userId, string email, string secretKey, string issuer, int expireMinutes) {
		var tokenHandler = new JwtSecurityTokenHandler();
		var key = Encoding.UTF8.GetBytes(secretKey);

		var tokenDescriptor = new SecurityTokenDescriptor {
			Subject =
				new([
					new(ClaimTypes.NameIdentifier, userId), new(ClaimTypes.Email, email)
				]),
			Expires = DateTime.UtcNow.AddMinutes(expireMinutes),
			Issuer = issuer,
			SigningCredentials = new(
				new SymmetricSecurityKey(key),
				SecurityAlgorithms.HmacSha256Signature
			)
		};

		var token = tokenHandler.CreateToken(tokenDescriptor);
		return tokenHandler.WriteToken(token);
	}
}