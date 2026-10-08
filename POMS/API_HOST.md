# Separate POMS API Host

`Poms.Api` is the production backend for the React, mobile, and external clients. `Poms.Web` is a
rollback-only legacy host and must not be deployed for the React/API production cutover.

## Local run

The API creates or upgrades its database and seeds local demo accounts in Development:

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
ApiAuthentication__RoleClaimType=poms_role
Cors__AllowedOrigins__0=https://app.example.com
FileStorage__RootPath=/app/storage
SeedDemoUsers=false
BootstrapAdmin__Email=admin@example.com
BootstrapAdmin__Password=USE_A_SECRET_VALUE
```

Production startup fails when OIDC, CORS, persistent storage, database, or bootstrap-administrator
configuration is absent. The host accepts bearer tokens only; it has no Razor UI or cookie fallback.
The OIDC `sub` for the administrator must match the local Identity user ID used by administration
safeguards. Never commit any value shown as a secret.

## Container build

Run from the `POMS` directory:

```powershell
docker build -f src/Poms.Api/Dockerfile -t poms-api .
docker build -f frontend/Dockerfile -t poms-frontend frontend `
  --build-arg VITE_API_URL=https://api.example.com `
  --build-arg VITE_OIDC_AUTHORITY=https://identity.example.com `
  --build-arg VITE_OIDC_CLIENT_ID=poms-web `
  --build-arg VITE_OIDC_REDIRECT_URI=https://app.example.com/auth/callback `
  --build-arg VITE_OIDC_POST_LOGOUT_REDIRECT_URI=https://app.example.com/signin
```

For the complete PostgreSQL/API/frontend stack, copy `.env.example` to `.env`, replace every
placeholder, and run `docker compose up -d --build`. Do not commit `.env`. The frontend image uses
Nginx with SPA route fallback and immutable caching only for fingerprinted assets.

Both application images run as non-root users. Compose persists PostgreSQL data, uploaded files,
and ASP.NET Core data-protection keys in separate named volumes. Back up all three volumes before
an infrastructure move or restore.

For the complete local production-development stack, run
`docker compose -f compose.development.yaml up -d --build`, then open `http://localhost:8080`.
It uses PostgreSQL, Keycloak OIDC with authorization-code/PKCE, the bearer API, and the production
Nginx frontend. Sign in as `admin@poms.local` with `Admin@123!`; these credentials are local-only.
Keycloak administration is at `http://localhost:8082/admin` using `keycloak-admin` and
`Keycloak-Local-8374!`.

Production staff credentials remain owned by the configured identity provider. POMS stores the
matching staff profile and authorization roles by email; it does not create or reset OIDC passwords.

Mount `/app/storage` as persistent storage. Patient photos and documents are lost if this path is
ephemeral. The API applies a global limit of 120 requests per minute per authenticated name or IP.

## Migration ownership

`Poms.Api.csproj` links the versioned API source under `Poms.Web/Api`; this is a source-sharing
boundary only and does not make the production API depend on the MVC process.

## Release checks

```powershell
dotnet restore Poms.sln
dotnet build Poms.sln -c Release --no-restore
dotnet test Poms.sln -c Release --no-build
cd frontend
npm ci
npm run lint
npm run build
```

Deploy the API image, verify `/health`, then deploy `frontend/dist` with its production OIDC and API
environment variables. Keep the previous API image available for rollback; never roll back the
database by deleting it.
