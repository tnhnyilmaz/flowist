using System.Security.Cryptography;
using System.Text;

using Flowist.AuthService.Data;
using Flowist.AuthService.DTOs;
using Flowist.AuthService.Entities;
using Flowist.AuthService.Options;
using Flowist.Shared.Caching;
using Flowist.Shared.DTOs;
using Flowist.Shared.Events;
using Flowist.Shared.Exceptions;

using MassTransit;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Flowist.AuthService.Services;

public sealed class AuthService : IAuthService
{
    private readonly AuthDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly JwtOptions _jwtOptions;
    private const int MaxFailedLoginAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IRefreshTokenCacheService _refreshTokenCacheService;
    private readonly IDistributedLockService _distributedLockService;

    public AuthService(
        AuthDbContext dbContext,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IOptions<JwtOptions> jwtOptions,
        IPublishEndpoint publishEndpoint,
        IRefreshTokenCacheService refreshTokenCacheService,
        IDistributedLockService distributedLockService)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _jwtOptions = jwtOptions.Value;
        _publishEndpoint = publishEndpoint;
        _refreshTokenCacheService = refreshTokenCacheService;
        _distributedLockService = distributedLockService;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, string? deviceInfo, string? ipAddress, CancellationToken cancellationToken = default)
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

        RefreshToken refreshToken = CreateRefreshToken(user.Id, deviceInfo, ipAddress);

        user.RefreshTokens.Add(refreshToken);

        _dbContext.Users.Add(user);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _refreshTokenCacheService.CacheAsync(
            refreshToken.Token,
            user.Id,
            refreshToken.ExpiresAt,
            cancellationToken);


        UserRegisteredEvent userRegisteredEvent = new(
            user.Id,
            user.Email,
            user.FullName,
            user.CreatedAt,
            Guid.NewGuid());

        await _publishEndpoint.Publish(userRegisteredEvent, cancellationToken);

        return CreateAuthResponse(user, refreshToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? deviceInfo, string? ipAddress, CancellationToken cancellationToken = default)
    {
        string normalizedEmail = NormalizeEmail(request.Email);

        User user = await _dbContext.Users
            .Include(user => user.RefreshTokens)
            .FirstOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken)
            ?? throw new NotFoundException(nameof(user), normalizedEmail);


        if (user.LockedUntil.HasValue && user.LockedUntil > DateTimeOffset.UtcNow)
        {
            throw new ForbiddenAccessException("Account is temporarily locked. Please try again later.");
        }

        bool passwordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);

        if (!passwordValid)
        {
            user.FailedLoginAttempts += 1;
            if (user.FailedLoginAttempts >= MaxFailedLoginAttempts)
            {
                user.LockedUntil = DateTimeOffset.UtcNow.Add(LockoutDuration);
                user.FailedLoginAttempts = 0;
            }
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new ForbiddenAccessException("Invalid email or password.");
        }

        bool userLockoutStateChanged = user.FailedLoginAttempts != 0 || user.LockedUntil is not null;

        if (userLockoutStateChanged)
        {
            user.FailedLoginAttempts = 0;
            user.LockedUntil = null;
            user.UpdatedAt = DateTimeOffset.UtcNow;
        }

        RefreshToken refreshToken = CreateRefreshToken(user.Id, deviceInfo, ipAddress);

        _dbContext.RefreshTokens.Add(refreshToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _refreshTokenCacheService.CacheAsync(
            refreshToken.Token,
            user.Id,
            refreshToken.ExpiresAt,
            cancellationToken);
        return CreateAuthResponse(user, refreshToken);
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, string? deviceInfo, string? ipAddress, CancellationToken cancellationToken = default)
    {
        string refreshTokenLockKey = $"lock:auth:refresh-token:{ComputeSha256(request.RefreshToken)}";

        await using IDistributedLock? refreshTokenLock =
            await _distributedLockService.TryAcquireAsync(
                refreshTokenLockKey,
                TimeSpan.FromSeconds(30),
                cancellationToken);

        if (refreshTokenLock is null)
        {
            throw new ConflictException("Refresh token operation is already in progress.");
        }

        RefreshToken existingRefreshToken = await _dbContext.RefreshTokens
            .Include(refreshToken => refreshToken.User)
            .FirstOrDefaultAsync(refreshToken => refreshToken.Token == request.RefreshToken, cancellationToken)
            ?? throw new NotFoundException(nameof(RefreshToken), "refresh token");

        if (!existingRefreshToken.IsActive)
        {
            throw new ForbiddenAccessException("Refresh token is expired or revoked.");
        }
        existingRefreshToken.RevokedAt = DateTimeOffset.UtcNow;

        RefreshToken newRefreshToken = CreateRefreshToken(existingRefreshToken.UserId, deviceInfo, ipAddress);
        _dbContext.RefreshTokens.Add(newRefreshToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _refreshTokenCacheService.RemoveAsync(
            existingRefreshToken.Token,
            cancellationToken);

        await _refreshTokenCacheService.CacheAsync(
            newRefreshToken.Token,
            existingRefreshToken.UserId,
            newRefreshToken.ExpiresAt,
            cancellationToken);

        return CreateAuthResponse(existingRefreshToken.User, newRefreshToken);

    }

    public async Task RevokeTokenAsync(RevokeTokenRequest request, CancellationToken cancellationToken = default)
    {
        RefreshToken refreshToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(refreshToken => refreshToken.Token == request.RefreshToken, cancellationToken)
            ?? throw new NotFoundException(nameof(RefreshToken), "refresh token");

        if (refreshToken.IsRevoked) return;
        refreshToken.RevokedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _refreshTokenCacheService.RemoveAsync(
            refreshToken.Token,
            cancellationToken);
    }


    public async Task RevokeAllTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {

        List<RefreshToken> activeTokens = await _dbContext.RefreshTokens
            .Where(
                refreshToken => refreshToken.UserId == userId &&
                refreshToken.RevokedAt == null &&
                refreshToken.ExpiresAt > DateTimeOffset.UtcNow
            ).ToListAsync(cancellationToken);

        if (activeTokens.Count == 0) return;

        DateTimeOffset revokedAt = DateTimeOffset.UtcNow;

        foreach (RefreshToken token in activeTokens)
        {
            token.RevokedAt = revokedAt;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        foreach (RefreshToken token in activeTokens)
        {
            await _refreshTokenCacheService.RemoveAsync(
                token.Token,
                cancellationToken);
        }
    }


    public async Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        User user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userId);

        return ToUserDto(user);
    }
    private static string ComputeSha256(string value)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));

        return Convert.ToHexString(bytes).ToLowerInvariant();
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

    private RefreshToken CreateRefreshToken(Guid userId, string? deviceInfo, string? ipAddress)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = GenerateRefreshToken(),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays),
            CreatedAt = DateTimeOffset.UtcNow,
            DeviceInfo = deviceInfo,
            IpAddress = ipAddress
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