using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ARTravel.API.Middleware;
using ARTravel.Application.Identity.Abstractions;
using ARTravel.Application.Identity.Contracts;
using ARTravel.Application.Identity.Services;
using ARTravel.Infrastructure;
using ARTravel.Infrastructure.Data;
using ARTravel.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IRoleManagementService, RoleManagementService>();
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

                var dbContext = context.HttpContext.RequestServices
                    .GetRequiredService<AppDbContext>();
                var isRevoked = await dbContext.RevokedAccessTokens
                    .AnyAsync(token =>
                        token.TokenId == tokenId && token.ExpiresAt > DateTime.UtcNow);
                if (isRevoked)
                {
                    context.Fail("The access token has been revoked.");
                    return;
                }

                var activeSessionExists = await dbContext.RefreshTokens
                    .AnyAsync(token =>
                        token.SessionId == sessionId &&
                        token.RevokedAt == null &&
                        token.ExpiresAt > DateTime.UtcNow);
                if (!activeSessionExists)
                {
                    context.Fail("The access token session has ended.");
                    return;
                }

                var user = await dbContext.Users
                    .Include(entity => entity.UserRoles)
                    .ThenInclude(userRole => userRole.Role)
                    .SingleOrDefaultAsync(entity => entity.Id == userId && entity.IsActive);
                if (user is null || user.TokenVersion != tokenVersion)
                {
                    context.Fail("The user is inactive or no longer exists.");
                    return;
                }

                if (principal?.Identity is ClaimsIdentity identity)
                {
                    foreach (var claim in identity.FindAll(identity.RoleClaimType).ToArray())
                    {
                        identity.RemoveClaim(claim);
                    }

                    foreach (var role in user.UserRoles
                                 .Select(userRole => userRole.Role)
                                 .Where(role => role.IsActive))
                    {
                        identity.AddClaim(new Claim(identity.RoleClaimType, role.Name));
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseMiddleware<IdentityExceptionMiddleware>();
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
