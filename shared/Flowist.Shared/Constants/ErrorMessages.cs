namespace Flowist.Shared.Constants;

public static class ErrorMessages
{
    public const string ResourceNotFound = "The requested resource was not found.";
    public const string Unauthorized = "Authentication is required to access this resource.";
    public const string Forbidden = "You do not have permission to access this resource.";
    public const string ValidationFailed = "One or more validation errors occurred.";
    public const string Conflict = "The requested operation conflicts with the current state.";
    public const string InternalServerError = "An unexpected error occurred.";
}