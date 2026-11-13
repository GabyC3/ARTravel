# Backend

The backend follows Clean Architecture:

```text
API -> Application -> Domain
API -> Infrastructure -> Application -> Domain
Infrastructure -> Domain
```

`API` hosts HTTP endpoints, `Application` contains identity use cases and ports, `Domain` contains entities, and `Infrastructure` implements PostgreSQL persistence, password hashing, and JWT creation.

## Requirements and local configuration

- .NET 8 SDK.
- PostgreSQL.
- EF CLI 8.0.15 (`dotnet tool install --global dotnet-ef --version 8.0.15`).

From the repository root, configure the database connection and a private JWT signing key using User Secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=artravel;Username=postgres;Password=YOUR_DATABASE_PASSWORD" --project .\Backend\ARTravel.API\ARTravel.API.csproj
$signingKey = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
dotnet user-secrets set "Jwt:SigningKey" $signingKey --project .\Backend\ARTravel.API\ARTravel.API.csproj
Remove-Variable signingKey
```

The JWT issuer, audience, and token lifetimes are configured in `appsettings.json`. Keep the signing key and database credentials out of configuration files; use User Secrets or the environment variables `ConnectionStrings__DefaultConnection` and `Jwt__SigningKey`.

## Database

The initial migration creates `Users`, `Roles`, `UserRoles`, and `RevokedAccessTokens`, and seeds the `User` and `Admin` roles. `IdentityTokenLifecycle` adds refresh/reset token storage and `TokenVersion`; apply pending migrations after configuring PostgreSQL:

```powershell
dotnet ef database update --project .\Backend\ARTravel.Infrastructure\ARTravel.Infrastructure.csproj --startup-project .\Backend\ARTravel.API\ARTravel.API.csproj
```

To start the API:

```powershell
dotnet run --project .\Backend\ARTravel.API\ARTravel.API.csproj
```

`GET /health` checks the API process; `GET /health/database` checks PostgreSQL connectivity.

## Identity API

| Method | Route | Access |
|---|---|---|
| `POST` | `/api/auth/register` | Public |
| `POST` | `/api/auth/login` | Public |
| `POST` | `/api/auth/refresh-token` | Public (refresh token required) |
| `POST` | `/api/auth/forgot-password` | Public |
| `POST` | `/api/auth/reset-password` | Public (one-time reset token required) |
| `POST` | `/api/auth/logout` | Authenticated |
| `PUT` | `/api/auth/change-password` | Authenticated |
| `GET` | `/api/auth/me` | Authenticated |
| `GET` | `/api/users?page=1&pageSize=20` | Admin |
| `GET` | `/api/users/{userId}` | Admin |
| `POST` | `/api/users` | Admin |
| `PUT` | `/api/users/{userId}` | Admin |
| `PATCH` | `/api/users/{userId}/status` | Admin |
| `GET` | `/api/users/{userId}/roles` | Admin |
| `POST` | `/api/users/{userId}/roles/{roleId}` | Admin |
| `DELETE` | `/api/users/{userId}/roles/{roleId}` | Admin |
| `GET` | `/api/roles` | Admin |
| `GET` | `/api/roles/{roleId}` | Admin |
| `POST` | `/api/roles` | Admin |
| `PUT` | `/api/roles/{roleId}` | Admin |
| `PATCH` | `/api/roles/{roleId}/status` | Admin |
| `DELETE` | `/api/roles/{roleId}` | Admin |

Registration requires a password with at least 12 characters, including uppercase, lowercase, a number, and a symbol. Passwords are stored as salted PBKDF2-SHA256 hashes. Access tokens are signed JWTs and refresh tokens are opaque, stored hashed, single-use, and rotated. Logout, password changes, password resets, and account status changes invalidate sessions. Authorization reloads current active roles for each request. Built-in `User` and `Admin` roles cannot be renamed, deactivated, or deleted; a role assigned to a user cannot be deleted.

Password-reset token storage and the endpoint are implemented, but no email provider has been selected/configured. Until an `IPasswordResetNotifier` is configured, `POST /api/auth/forgot-password` returns `503` without looking up the submitted email or exposing a reset token. The reset email must deliver the one-time token to the account owner; tokens are stored only as hashes.

### First administrator

Public registration only assigns the standard `User` role. After registering the account that should administer the system, promote that account once using PostgreSQL (replace `1` with its user ID):

```sql
INSERT INTO "UserRoles" ("UserId", "RoleId")
VALUES (1, 2)
ON CONFLICT DO NOTHING;
```

The seed assigns role ID `2` to `Admin`. Administrator role removal/deactivation is blocked when it would leave no active administrators.
