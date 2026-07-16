using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Flowist.NotificationService.IntegrationTests.TestSupport;

public sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";
    public const string UserIdHeaderName = "X-Test-UserId";

    public TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserIdHeaderName, out Microsoft.Extensions.Primitives.StringValues headerValue) ||
            !Guid.TryParse(headerValue.ToString(), out Guid userId))
        {
            return Task.FromResult(AuthenticateResult.Fail("Test user id is missing."));
        }

        Claim[] claims =
        [
            new(Flowist.Shared.Constants.ClaimTypes.UserId, userId.ToString()),
            new(Flowist.Shared.Constants.ClaimTypes.Email, $"{userId:N}@flowist.local"),
            new(Flowist.Shared.Constants.ClaimTypes.FullName, "Integration Test User")
        ];

        ClaimsIdentity identity = new(claims, SchemeName);
        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}