using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Flowist.AuthService.DTOs;
using Flowist.AuthService.Services;
using Flowist.Shared.DTOs;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Flowist.AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth-fixed")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ITokenBlacklistService _tokenBlacklistService;

    public AuthController(IAuthService authService, ITokenBlacklistService tokenBlacklistService)
    {
        _authService = authService;
        _tokenBlacklistService = tokenBlacklistService;
    }

    /// <summary>
    /// Registers a new user account and returns access and refresh tokens.
    /// </summary>
    /// <param name="request">The registration request.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The authentication response containing tokens and user information.</returns>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        AuthResponse response = await _authService.RegisterAsync(
            request,
            GetDeviceInfo(),
            GetIpAddress(),
            cancellationToken);

        return CreatedAtAction(nameof(Me), response.User, response);
    }

    /// <summary>
    /// Authenticates a user and returns access and refresh tokens.
    /// </summary>
    /// <param name="request">The login request.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The authentication response containing tokens and user information.</returns>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        AuthResponse response = await _authService.LoginAsync(
            request,
            GetDeviceInfo(),
            GetIpAddress(),
            cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Rotates a valid refresh token and returns a new access token and refresh token.
    /// </summary>
    /// <param name="request">The refresh token request.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The authentication response containing new tokens and user information.</returns>
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        AuthResponse response = await _authService.RefreshTokenAsync(
            request,
            GetDeviceInfo(),
            GetIpAddress(),
            cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Revokes a single refresh token.
    /// </summary>
    /// <param name="request">The revoke token request.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>No content when the token is revoked.</returns>
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke(
        RevokeTokenRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.RevokeTokenAsync(request, cancellationToken);
        await BlacklistCurrentAccessTokenAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Revokes all active refresh tokens for the authenticated user.
    /// </summary>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>No content when all active tokens are revoked.</returns>
    [Authorize]
    [HttpPost("revoke-all")]
    public async Task<IActionResult> RevokeAll(CancellationToken cancellationToken)
    {
        string? userIdClaim = User.FindFirstValue(Flowist.Shared.Constants.ClaimTypes.UserId);

        if (!Guid.TryParse(userIdClaim, out Guid userId))
        {
            return Unauthorized();
        }

        await _authService.RevokeAllTokensAsync(userId, cancellationToken);
        await BlacklistCurrentAccessTokenAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Gets the authenticated user's profile.
    /// </summary>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The current authenticated user.</returns>
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

    private string? GetIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private async Task BlacklistCurrentAccessTokenAsync(CancellationToken cancellationToken)
    {
        string? tokenId = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        string? expirationValue = User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;

        if (string.IsNullOrWhiteSpace(tokenId) ||
            string.IsNullOrWhiteSpace(expirationValue) ||
            !long.TryParse(expirationValue, out long expirationUnixSeconds))
        {
            return;
        }

        DateTimeOffset expiresAt = DateTimeOffset.FromUnixTimeSeconds(expirationUnixSeconds);

        await _tokenBlacklistService.BlacklistAsync(tokenId, expiresAt, cancellationToken);
    }
}