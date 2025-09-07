using System.Net;
using System.Text.Json;
using CarInsurance.Api.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace CarInsurance.Api.Handlers;

public class ExceptionHandler: IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        HttpStatusCode statusCode;
        string message;

        switch (exception)
        {
            case KeyNotFoundException:
                statusCode = HttpStatusCode.NotFound;
                message = exception.Message;
                break;
            case NotUniqueVinException:
                statusCode = HttpStatusCode.Conflict;
                message = exception.Message;
                break;
            case InvalidOperationException: 
                statusCode = HttpStatusCode.BadRequest;
                message = exception.Message;
                break;
            default:
                statusCode = HttpStatusCode.InternalServerError;
                message = "An unexpected error occurred. Please try again later.";
                break;
        }

        httpContext.Response.ContentType = "application/json";
        httpContext.Response.StatusCode = (int)statusCode;
        
        var result = JsonSerializer.Serialize(new { error = message });
        await httpContext.Response.WriteAsync(result, cancellationToken);
        
        return true;
    }
}