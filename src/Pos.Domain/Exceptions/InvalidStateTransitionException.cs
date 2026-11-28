namespace Pos.Domain.Exceptions;

public sealed class InvalidStateTransitionException(string message) : PosDomainException(message);
