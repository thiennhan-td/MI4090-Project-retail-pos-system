namespace Pos.Domain.Exceptions;

public sealed class BusinessRuleViolationException(string message) : PosDomainException(message);
