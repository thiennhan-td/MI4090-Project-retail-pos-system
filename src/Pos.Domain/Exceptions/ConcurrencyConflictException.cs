namespace Pos.Domain.Exceptions;

public sealed class ConcurrencyConflictException(string message) : PosDomainException(message);
