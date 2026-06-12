using System.Security.Cryptography;

using Flowist.AuthService.Data;
using Flowist.AuthService.DTOs;
using Flowist.AuthService.Entities;
using Flowist.AuthService.Options;
using Flowist.Shared.DTOs;
using Flowist.Shared.Exceptions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Flowist.AuthService.Services;

public sealed class AuthService : IAuthService
{
    private readonly AuthDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        AuthDbContext dbContext,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IOptions<JwtOptions> jwtOptions)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, string? deviceInfo, CancellationToken cancellationToken = default)
    {
        string normalizedEmail = NormalizeEmail(request.Email);

        bool emailExists = await _dbContext.Users
            .AnyAsync(user => user.Email == normalizedEmail, cancellationToken);

        if (emailExists)
        {
            throw new ConflictException("Email", normalizedEmail);
        }

        User user = new()
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            FullName = request.FullName.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        RefreshToken refreshToken = CreateRefreshToken(user.Id, deviceInfo);

        user.RefreshTokens.Add(refreshToken);

        _dbContext.Users.Add(user);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreateAuthResponse(user, refreshToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? deviceInfo, CancellationToken cancellationToken = default)
    {
        string normalizedEmail = NormalizeEmail(request.Email);

        User user = await _dbContext.Users
            .Include(user => user.RefreshTokens)
            .FirstOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken)
            ?? throw new NotFoundException(nameof(user), normalizedEmail);

        bool passwordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);

        if(!passwordValid) throw new ForbiddenAccessException("Invalid email or password.");

        RefreshToken refreshToken = CreateRefreshToken(user.Id, deviceInfo);

        user.RefreshTokens.Add(refreshToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return CreateAuthResponse(user, refreshToken);
    }

    public Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, string? deviceInfo, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task RevokeTokenAsync(RevokeTokenRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        User user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userId);

        return ToUserDto(user);
    }

    private AuthResponse CreateAuthResponse(User user, RefreshToken refreshToken)
    {
        JwtTokenResult accessToken = _jwtTokenService.GenerateAccessToken(user);

        return new AuthResponse(
            accessToken.AccessToken,
            accessToken.ExpiresAt,
            refreshToken.Token,
            refreshToken.ExpiresAt,
            ToUserDto(user));
    }

    private RefreshToken CreateRefreshToken(Guid userId, string? deviceInfo)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = GenerateRefreshToken(),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays),
            CreatedAt = DateTimeOffset.UtcNow,
            DeviceInfo = deviceInfo
        };
    }

    private static string GenerateRefreshToken()
    {
        byte[] randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static UserDto ToUserDto(User user)
    {
        return new UserDto(
            user.Id,
            user.Email,
            user.FullName,
            user.CreatedAt);
    }
}