namespace CarPlates.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
    protected DomainException(string message, Exception innerException) : base(message, innerException) { }
}

public class PlateRecognitionException : DomainException
{
    public PlateRecognitionException(string message) : base(message) { }
}

public class AuthenticationException : DomainException
{
    public AuthenticationException(string message) : base(message) { }
}

public class VehicleLookupException : DomainException
{
    public VehicleLookupException(string message) : base(message) { }
}

public class SyncException : DomainException
{
    public SyncException(string message) : base(message) { }
}
