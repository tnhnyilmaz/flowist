namespace Flowist.AuthService.DTOs;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string FullName);