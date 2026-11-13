using ARTravel.Application.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace ARTravel.API.Middleware;

public sealed class IdentityExceptionMiddleware(RequestDelegate next, ILogger<IdentityExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (IdentityNotFoundException exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, exception.Message);
        }
        catch (IdentityConflictException exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status409Conflict, exception.Message);
        }
        catch (IdentityUnauthorizedException)
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status401Unauthorized,
                "The email or password is invalid.");
        }
        catch (PasswordResetDeliveryUnavailableException exception)
        {
            logger.LogError(exception, "Password reset email delivery is not configured.");
            await WriteProblemAsync(
                context,
                StatusCodes.Status503ServiceUnavailable,
                "Password reset is temporarily unavailable because email delivery is not configured.");
        }
        catch (IdentityConfigurationException exception)
        {
            logger.LogError("Identity configuration error: {Message}", exception.Message);
            await WriteProblemAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "The identity service is not configured correctly.");
        }
        catch (ArgumentException exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, exception.Message);
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        int statusCode,
        string detail)
    {
        if (context.Response.HasStarted)
        {
            throw new InvalidOperationException("The response has already started.");
        }

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = ReasonPhrases.GetReasonPhrase(statusCode),
            Detail = detail
        });
    }
}
