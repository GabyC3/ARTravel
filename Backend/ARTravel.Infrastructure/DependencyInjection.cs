using ARTravel.Application.Interfaces.Identity;
using ARTravel.Application.Interfaces.Contacts;
using ARTravel.Application.Interfaces.Destinations;
using ARTravel.Application.Interfaces.Products;
using ARTravel.Application.Interfaces.Itineraries;
using ARTravel.Application.Interfaces.Departures;
using ARTravel.Application.Interfaces.Reservations;
using ARTravel.Infrastructure.Data;
using ARTravel.Infrastructure.Repositories.Contacts;
using ARTravel.Infrastructure.Repositories.Destinations;
using ARTravel.Infrastructure.Repositories.Products;
using ARTravel.Infrastructure.Repositories.Itineraries;
using ARTravel.Infrastructure.Repositories.Departures;
using ARTravel.Infrastructure.Repositories.Reservations;
using ARTravel.Infrastructure.Repositories.Identity;
using ARTravel.Infrastructure.Services.Identity;
using ARTravel.Infrastructure.Services.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ARTravel.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "The PostgreSQL connection string 'DefaultConnection' is missing. " +
                "Configure it with user-secrets or ConnectionStrings__DefaultConnection.");
        }

        var jwtSettings = JwtSettings.FromConfiguration(configuration);
        services.AddSingleton(jwtSettings);
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IContactStore, EfContactStore>();
        services.AddScoped<IDestinationStore, EfDestinationStore>();
        services.AddScoped<IProductStore, EfProductStore>();
        services.AddScoped<IItineraryStore, EfItineraryStore>();
        services.AddScoped<IDepartureStore, EfDepartureStore>();
        services.AddScoped<IReservationStore, EfReservationStore>();
        services.AddScoped<IIdentityStore, EfIdentityStore>();
        services.AddScoped<IAccessTokenSessionValidator, EfAccessTokenSessionValidator>();
        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IAccessTokenService, JwtAccessTokenService>();
        services.AddSingleton<ISecureTokenService, SecureTokenService>();
        services.AddSingleton<IPasswordResetNotifier, UnconfiguredPasswordResetNotifier>();

        return services;
    }
}
