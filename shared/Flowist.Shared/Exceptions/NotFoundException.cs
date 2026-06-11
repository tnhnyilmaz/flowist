namespace Flowist.Shared.Exceptions;

public sealed class NotFoundException : DomainException
{
    public NotFoundException(string entity, object id)
        : base($"{entity} with id '{id}' was not found.")
    {
        Entity = entity;
        Id = id;
    }

    public string Entity { get; }

    public object Id { get; }
}