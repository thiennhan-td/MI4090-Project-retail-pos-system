namespace Pos.Domain.Exceptions;

public sealed class EntityNotFoundException(string message) : PosDomainException(message);
