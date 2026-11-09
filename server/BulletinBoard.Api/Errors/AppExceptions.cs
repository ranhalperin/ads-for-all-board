namespace BulletinBoard.Api.Errors;

/// <summary>An error whose message is safe to show to the user.</summary>
public abstract class AppException(string message) : Exception(message)
{
    public abstract int StatusCode { get; }
}

public sealed class NotFoundException(string message) : AppException(message)
{
    public override int StatusCode => StatusCodes.Status404NotFound;
}

/// <summary>The board reached Storage:MaxAds.</summary>
public sealed class StorageFullException(string message) : AppException(message)
{
    public override int StatusCode => StatusCodes.Status409Conflict;
}

/// <summary>The file lock wasn't free within the timeout. The client should try again.</summary>
public sealed class StorageBusyException() : AppException("The board is busy right now. Try again in a moment.")
{
    public override int StatusCode => StatusCodes.Status503ServiceUnavailable;
}
