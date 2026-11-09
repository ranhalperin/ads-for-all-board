using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BulletinBoard.Api.Errors;

/// <summary>
/// Turns exceptions into ProblemDetails JSON. Known errors keep their (user-safe) message;
/// anything else becomes a plain 500 and the details go to the log only.
/// </summary>
public class ErrorHandler(IProblemDetailsService problemDetails, ILogger<ErrorHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        int status;
        string title;
        switch (exception)
        {
            case AppException app:
                status = app.StatusCode;
                title = app.Message;
                if (app is StorageBusyException)
                    context.Response.Headers.RetryAfter = "2";
                break;
            case OperationCanceledException when context.RequestAborted.IsCancellationRequested:
                return true; // the client went away; nobody to answer
            default:
                logger.LogError(exception, "Unhandled error on {Method} {Path}", context.Request.Method, context.Request.Path);
                status = StatusCodes.Status500InternalServerError;
                title = "Something went wrong. Try again.";
                break;
        }

        context.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = title },
        });
    }
}
