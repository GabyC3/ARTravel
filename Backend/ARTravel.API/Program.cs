using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ARTravel.API.Configuration;
using ARTravel.API.Middleware;
using ARTravel.Application.Interfaces.Identity;
using ARTravel.Application.DTOs.Identity;
using ARTravel.Application.UseCases.Identity;
using ARTravel.Application.Interfaces.Contacts;
using ARTravel.Application.UseCases.Contacts;
using ARTravel.Application.Interfaces.Destinations;
using ARTravel.Application.UseCases.Destinations;
using ARTravel.Application.Interfaces.Products;
using ARTravel.Application.UseCases.Products;
using ARTravel.Application.Interfaces.Itineraries;
using ARTravel.Application.UseCases.Itineraries;
using ARTravel.Application.Interfaces.Departures;
using ARTravel.Application.UseCases.Departures;
using ARTravel.Application.Interfaces.Reservations;
using ARTravel.Application.UseCases.Reservations;
using ARTravel.Infrastructure;
using ARTravel.Infrastructure.Data;
using ARTravel.Infrastructure.Services.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

DotEnvLoader.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IRoleManagementService, RoleManagementService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<IDestinationService, DestinationService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IItineraryService, ItineraryService>();
builder.Services.AddScoped<IDepartureService, DepartureService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddControllers();

var jwtSettings = JwtSettings.FromConfiguration(builder.Configuration);
builder.Services.AddSingleton(new IdentityTokenLifetimes(
    TimeSpan.FromDays(jwtSettings.RefreshTokenDays),
    TimeSpan.FromMinutes(jwtSettings.PasswordResetMinutes)));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var principal = context.Principal;
                var tokenId = principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);
                var sessionId = principal?.FindFirstValue("sid");
                var userIdValue =
                    principal?.FindFirstValue(ClaimTypes.NameIdentifier) ??
                    principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
                var tokenVersionValue = principal?.FindFirstValue("ver");
                if (string.IsNullOrWhiteSpace(tokenId) ||
                    string.IsNullOrWhiteSpace(sessionId) ||
                    !int.TryParse(tokenVersionValue, out var tokenVersion) ||
                    !int.TryParse(userIdValue, out var userId))
                {
                    context.Fail("The access token is missing required claims.");
                    return;
                }

                var validator = context.HttpContext.RequestServices
                    .GetRequiredService<IAccessTokenSessionValidator>();
                var roles = await validator.GetActiveRolesAsync(
                    tokenId,
                    sessionId,
                    userId,
                    tokenVersion,
                    context.HttpContext.RequestAborted);
                if (roles is null)
                {
                    context.Fail("The access token session is no longer valid.");
                    return;
                }

                if (principal?.Identity is ClaimsIdentity identity)
                {
                    foreach (var claim in identity.FindAll(identity.RoleClaimType).ToArray())
                    {
                        identity.RemoveClaim(claim);
                    }

                    foreach (var role in roles)
                    {
                        identity.AddClaim(new Claim(identity.RoleClaimType, role));
                    }
                }
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        "Administrator",
        policy => policy.RequireAuthenticatedUser().RequireRole("Admin"));
    options.AddPolicy(
        "Manager",
        policy => policy.RequireAuthenticatedUser().RequireRole("Admin", "Manager"));
    options.AddPolicy(
        "Commercial",
        policy => policy.RequireAuthenticatedUser().RequireRole("Admin", "Manager", "Agent", "Agente"));
    options.AddPolicy(
        "StaffRead",
        policy => policy.RequireAuthenticatedUser().RequireRole(
            "Admin", "Manager", "Agent", "Agente", "Operator", "Operador", "Guide", "Guia"));
    options.AddPolicy(
        "CustomerRead",
        policy => policy.RequireAuthenticatedUser().RequireRole(
            "Admin", "Manager", "Agent", "Agente", "Operator", "Operador"));
    options.AddPolicy(
        "CustomerManagement",
        policy => policy.RequireAuthenticatedUser().RequireRole(
            "Admin", "Manager", "Agent", "Agente"));
    options.AddPolicy(
        "PassengerManagement",
        policy => policy.RequireAuthenticatedUser().RequireRole(
            "Admin", "Manager", "Agent", "Agente", "Operator", "Operador"));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ARTravel API",
        Version = "v1"
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter the access token returned by register or login."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        }] = Array.Empty<string>()
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()                     
              .AllowAnyMethod();                    
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { service = "ARTravel API", status = "running" }));
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
app.MapGet(
    "/health/database",
    async (AppDbContext dbContext, CancellationToken cancellationToken) =>
        await dbContext.Database.CanConnectAsync(cancellationToken)
            ? Results.Ok(new { status = "Healthy", database = "PostgreSQL" })
            : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));
app.MapControllers();

app.Run();
