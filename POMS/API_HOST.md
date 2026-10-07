# Separate POMS API Host

`Poms.Api` is the independently runnable backend for future React, mobile, and external clients.
The existing `Poms.Web` MVC application remains operational during migration.

## Local run

The API does not create or upgrade databases. Point it at an existing POMS development database:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://localhost:5015"
$env:ConnectionStrings__DefaultConnection = "Data Source=X:\Developments\motivation\POMS\src\Poms.Web\poms-local.db"
dotnet run --project POMS/src/Poms.Api --no-launch-profile
```

Expected checks:

```text
GET /health                 -> 200 when the database is reachable
GET /api/v1/patients        -> 401 without a bearer token
GET /api/v1/appointments    -> 401 without a bearer token
```

## Required production configuration

```text
ASPNETCORE_ENVIRONMENT=Production
DATABASE_URL=postgresql://USER:PASSWORD@HOST:5432/DATABASE
ApiAuthentication__Authority=https://identity.example.com
ApiAuthentication__Audience=poms-api
ApiAuthentication__RequireHttpsMetadata=true
ApiAuthentication__NameClaimType=name
ApiAuthentication__RoleClaimType=role
Cors__AllowedOrigins__0=https://app.example.com
```

Production startup fails when the OIDC authority, audience, or allowed frontend origins are absent.
The host accepts bearer tokens only; it has no Razor UI, login page, or Identity-cookie fallback.

## Container build

Run from the `POMS` directory:

```powershell
docker build -f src/Poms.Api/Dockerfile -t poms-api .
```

The API host does not need persistent file storage for the current JSON patient-registration and
read contracts. Add object-storage integration before moving patient photos or document uploads.

## Migration ownership

During migration, `Poms.Api.csproj` links the versioned API source under `Poms.Web/Api`. This keeps
one contract/controller implementation while both hosts run. The next extraction step is to move
those files into a shared API feature library after write commands and file endpoints establish the
final dependency boundary.
