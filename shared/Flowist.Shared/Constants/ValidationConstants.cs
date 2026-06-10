namespace Flowist.Shared.Constants;

public static class ValidationConstants
{
    public const int EmailMaxLength = 256;
    public const int FullNameMaxLength = 150;
    public const int WorkspaceNameMaxLength = 120;
    public const int WorkspaceDescriptionMaxLength = 1_000;
    public const int ProjectNameMaxLength = 120;
    public const int ProjectDescriptionMaxLength = 1_000;
    public const int TaskTitleMaxLength = 200;
    public const int TaskDescriptionMaxLength = 4_000;
    public const int PasswordMinLength = 8;

    public const string EmailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
}