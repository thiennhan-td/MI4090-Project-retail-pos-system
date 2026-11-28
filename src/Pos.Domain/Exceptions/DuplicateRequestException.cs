namespace Pos.Domain.Exceptions;

public sealed class DuplicateRequestException(string message) : PosDomainException(message);
