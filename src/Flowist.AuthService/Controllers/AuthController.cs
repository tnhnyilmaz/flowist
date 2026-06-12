

using System.Security.Claims;

using Flowist.AuthService.DTOs;
using Flowist.AuthService.Services;
using Flowist.Shared.DTOs;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flowist.AuthService.Controllers;


[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(
    RegisterRequest request,
    CancellationToken cancellationToken)
    {
        AuthResponse response = await _authService.RegisterAsync(
            request,
            GetDeviceInfo(),
            cancellationToken);

        return CreatedAtAction(nameof(Me), response.User, response);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {

        AuthResponse response = await _authService.LoginAsync(
            request,
            GetDeviceInfo(),
            cancellationToken
        );

        return Ok(response);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me(CancellationToken cancellationToken)
    {

        string? userIdClaim = User.FindFirstValue(Flowist.Shared.Constants.ClaimTypes.UserId);

        if (!Guid.TryParse(userIdClaim, out Guid userId))
        {
            return Unauthorized();
        }

        UserDto user = await _authService.GetCurrentUserAsync(userId, cancellationToken);
        return Ok(user);
    }

    private string? GetDeviceInfo()
    {
        return Request.Headers.UserAgent.ToString();
    }



}