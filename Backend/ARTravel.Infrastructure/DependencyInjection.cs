using ARTravel.Application.Identity.Abstractions;
using ARTravel.Infrastructure.Data;
using ARTravel.Infrastructure.Identity;
using ARTravel.Infrastructure.Security;
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
        services.AddScoped<IIdentityStore, EfIdentityStore>();
        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IAccessTokenService, JwtAccessTokenService>();
        services.AddSingleton<ISecureTokenService, SecureTokenService>();
        services.AddSingleton<IPasswordResetNotifier, UnconfiguredPasswordResetNotifier>();

        return services;
    }
}
