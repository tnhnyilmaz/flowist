namespace Flowist.Shared.Exceptions;

public sealed class ConflictException : DomainException
{
    public ConflictException(string message)
        : base(message)
    {
    }

    public ConflictException(string resourceName, string conflictingValue)
        : base($"{resourceName} '{conflictingValue}' already exists.")
    {
        ResourceName = resourceName;
        ConflictingValue = conflictingValue;
    }

    public string? ResourceName { get; }

    public string? ConflictingValue { get; }
}