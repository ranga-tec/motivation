# POMS API (`Poms.Api`)

`Poms.Api` is the only backend in production. It serves the React client under `/api/v1`, signs
users in through ASP.NET Core Identity under `/auth`, and reports readiness at `/health`. It
compiles the controllers and contracts from `src/Poms.Web/Api/V1`; it does not depend on the MVC
process. Hosting is described in [`../DEPLOY_CONTABO.md`](../DEPLOY_CONTABO.md).

## Authentication

- `POST /auth/login?useCookies=false` with `{ "email", "password" }` returns a bearer access token
  and a refresh token. `POST /auth/refresh` renews it. The frontend keeps tokens in session storage.
- `GET /api/v1/session` returns the signed-in user and roles.
- `POST /auth/register` is blocked (404). Accounts are created by administrators.
- Token keys are ASP.NET Core data-protection keys; persist
  `/home/app/.aspnet/DataProtection-Keys` or every user is signed out on restart.

## Authorization policies

| Policy | Roles | Used for |
| --- | --- | --- |
| `AnyAuthenticatedUser` | any signed-in user | dashboard, appointments, documents, printing, session |
| `PatientRead` | DATA_ENTRY, CLINICIAN, MANAGEMENT, ADMIN | patient list/detail, records list |
| `DataEntry` | DATA_ENTRY, ADMIN | registration options, create and edit patients |
| `ClinicianOrAdmin` | CLINICIAN, ADMIN | records (episodes) and clinical entries |
| `ReportOrAdmin` | VIEWER, MANAGEMENT, ADMIN | reports |
| `AdminOnly` | ADMIN | users and reference data |
| `ApiWrite` | bearer-token requests | every write endpoint |

The React client mirrors these rules in `frontend/src/lib/access.ts`.

## Endpoint map (`/api/v1`)

| Area | Endpoints |
| --- | --- |
| Dashboard | `GET dashboard`, `GET dashboard/activity`, `GET dashboard/records` |
| Patients | `GET patients`, `GET patients/{id}`, `GET patients/{id}/edit`, `GET patients/filter-options`, `GET patients/registration-options`, `POST patients`, `PUT patients/{id}` |
| Records | `GET episodes?patientId=`, `GET episodes/{id}`, `GET episodes/options`, `POST episodes`, `PUT episodes/{id}` |
| Clinical | `GET clinical-records/options`, `GET clinical-records/prescription-options`, `GET clinical-records/episodes/{id}`, `POST/PUT` for `assessments`, `fittings`, `deliveries`, `follow-ups` |
| Appointments | `GET appointments`, `GET appointments/{id}`, `GET appointments/options`, `POST appointments`, `POST appointments/{id}/complete`, `/cancel`, `/reschedule` |
| Documents | `GET documents/options`, `GET documents?patientId=` or `?episodeId=`, `POST documents`, `GET documents/{id}`, `DELETE documents/{id}`, `GET documents/patient-photo/{patientId}` |
| Printing | `GET print/patients/{id}/registration`, `print/assessments/{id}`, `print/assessments/{id}/prescription`, `print/deliveries/{id}`, `print/follow-ups/{id}` |
| Reports | `GET reports/options`, `GET reports/{key}`, `GET reports/{key}/pdf` (13 keys) |
| Administration | `GET admin/catalog`, CRUD for `provinces`, `districts`, `cities`, `centers`, `device-types`, `devices`, `lookups/{type}`; `GET/POST admin/users`, `PUT admin/users/{id}/profile`, `/roles`, `/lock`, `/password` |

Errors use RFC 7807 problem details. Validation errors return 400 with an `errors` dictionary.

## Configuration

| Setting | Production value |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `DATABASE_URL` | `postgresql://user:password@host:5432/db`. Add `?sslmode=disable` for a private Docker database; without `sslmode`, Production requires SSL. |
| `Cors__AllowedOrigins__0` | The site origin, e.g. `https://169-58-173-191.sslip.io` |
| `FileStorage__RootPath` | `/app/storage` on a persistent volume |
| `SeedDemoUsers` | `false` |
| `BootstrapAdmin__Email`, `BootstrapAdmin__Password` | First administrator for an empty database |

Production refuses to start if the database, CORS origin, persistent storage or bootstrap
administrator (on an empty database) is missing, and if a demonstration account still uses its
published password. The schema is created with `EnsureCreated` plus `PostgresSchemaUpgrader`;
additive changes apply automatically on start, destructive ones need the upgrader updated.

Behind a reverse proxy the API trusts `X-Forwarded-For` / `X-Forwarded-Proto`, so rate limiting
(120 requests per minute per user or IP) sees real client addresses. Never publish the API port
directly when this is enabled.

## Local run

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://localhost:5015"
dotnet run --project POMS/src/Poms.Api --no-launch-profile
```

Development uses SQLite (`poms-api-local.db`) and seeds demonstration accounts.
`GET /health` returns 200; `GET /api/v1/patients` returns 401 without a token.
