using ARTravel.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace ARTravel.API.Middleware;

public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger)
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
        catch (ContactNotFoundException exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, exception.Message);
        }
        catch (DestinationNotFoundException exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, exception.Message);
        }
        catch (DestinationConflictException exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status409Conflict, exception.Message);
        }
        catch (ProductNotFoundException exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, exception.Message);
        }
        catch (IdentityConflictException exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status409Conflict, exception.Message);
        }
        catch (ProductConflictException exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status409Conflict, exception.Message);
        }
        catch (ItineraryNotFoundException exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, exception.Message);
        }
        catch (ItineraryConflictException exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status409Conflict, exception.Message);
        }
        catch (DepartureNotFoundException exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, exception.Message);
        }
        catch (DepartureConflictException exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status409Conflict, exception.Message);
        }
        catch (ReservationNotFoundException exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, exception.Message);
        }
        catch (ReservationConflictException exception)
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
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Unhandled exception processing {Method} {Path}.",
                context.Request.Method,
                context.Request.Path);
            await WriteProblemAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.");
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
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = ReasonPhrases.GetReasonPhrase(statusCode),
            Detail = detail,
            Instance = context.Request.Path
        };
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;
        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}
