using System.Net;
using System.Net.Http.Json;

using Flowist.AuthService.DTOs;
using Flowist.AuthService.IntegrationTests.TestSupport;

using FluentAssertions;

namespace Flowist.AuthService.IntegrationTests.Auth;

public sealed class AuthFlowIntegrationTests : IClassFixture<AuthServiceWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthFlowIntegrationTests(AuthServiceWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ShouldCreateUserAndReturnTokens()
    {
        RegisterRequest request = CreateRegisterRequest();

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        AuthResponse? authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();

        authResponse.Should().NotBeNull();
        authResponse!.AccessToken.Should().NotBeNullOrWhiteSpace();
        authResponse.RefreshToken.Should().NotBeNullOrWhiteSpace();
        authResponse.User.Email.Should().Be(request.Email.ToLowerInvariant());
        authResponse.User.FullName.Should().Be(request.FullName);
    }

    [Fact]
    public async Task Login_ShouldReturnTokensForRegisteredUser()
    {
        RegisterRequest registerRequest = CreateRegisterRequest();
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        LoginRequest loginRequest = new(registerRequest.Email, registerRequest.Password);

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        AuthResponse? authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();

        authResponse.Should().NotBeNull();
        authResponse!.AccessToken.Should().NotBeNullOrWhiteSpace();
        authResponse.RefreshToken.Should().NotBeNullOrWhiteSpace();
        authResponse.User.Email.Should().Be(registerRequest.Email.ToLowerInvariant());
    }

    [Fact]
    public async Task Refresh_ShouldRotateRefreshTokenAndRejectOldToken()
    {
        RegisterRequest registerRequest = CreateRegisterRequest();
        HttpResponseMessage registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        AuthResponse authResponse = (await registerResponse.Content.ReadFromJsonAsync<AuthResponse>())!;

        RefreshTokenRequest refreshRequest = new(authResponse.RefreshToken);

        HttpResponseMessage refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        AuthResponse? rotatedResponse = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>();

        rotatedResponse.Should().NotBeNull();
        rotatedResponse!.RefreshToken.Should().NotBe(authResponse.RefreshToken);
        rotatedResponse.AccessToken.Should().NotBeNullOrWhiteSpace();

        HttpResponseMessage oldTokenResponse = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        oldTokenResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static RegisterRequest CreateRegisterRequest()
    {
        string uniqueValue = Guid.NewGuid().ToString("N");

        return new RegisterRequest(
            $"integration-{uniqueValue}@flowist.local",
            "Test123!",
            "Integration Test User");
    }
}