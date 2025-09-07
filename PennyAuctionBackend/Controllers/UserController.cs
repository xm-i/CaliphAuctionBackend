using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PennyAuctionBackend.Dtos.User;
using PennyAuctionBackend.Services.Interfaces;
using System.Security.Claims;

namespace PennyAuctionBackend.Controllers;

[ApiController]
[Route("users")]
public class UserController(IUserService userService) : ControllerBase {
	private readonly IUserService _userService = userService;

	[HttpPost("register")]
	[AllowAnonymous]
	public async Task<ActionResult> RegisterAsync([FromBody] RegisterUserDto registerUserDto) {
		if (!this.ModelState.IsValid) {
			return this.BadRequest(this.ModelState);
		}

		await this._userService.RegisterAsync(registerUserDto);
		return this.Ok();
	}

	[HttpPost("login")]
	[AllowAnonymous]
	public async Task<ActionResult> LoginAsync([FromBody] LoginDto loginDto) {
		var ipAddress = this.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
		if (!this.ModelState.IsValid) {
			return this.BadRequest(this.ModelState);
		}

		var result = await this._userService.LoginAsync(loginDto, ipAddress);
		return this.Ok(result);
	}

	[HttpGet("me")]
	[Authorize]
	public async Task<ActionResult<UserSummaryDto>> MeAsync() {
		var userIdStr = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (!int.TryParse(userIdStr, out var userId)) {
			return this.Unauthorized();
		}
		var user = await this._userService.GetByIdAsync(userId);
		if (user == null) {
			return this.NotFound();
		}
		return this.Ok(user);
	}
}