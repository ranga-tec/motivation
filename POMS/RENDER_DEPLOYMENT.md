# POMS deployment: Render + Neon

POMS deploys as two independent services: the React frontend and ASP.NET Core API. Authentication is provided by ASP.NET Core Identity inside the API; Keycloak is not required.

## Render services

- `poms-motivation`: React static frontend.
- `poms-api`: ASP.NET Core API, Identity endpoints, and application authorization.
- Neon PostgreSQL: application data, users, password hashes, and roles.

## Required API environment variables

| Variable | Value |
|---|---|
| `DATABASE_URL` | Neon pooled PostgreSQL URL |
| `Cors__AllowedOrigins__0` | `https://poms-motivation.onrender.com` |
| `BootstrapAdmin__Email` | Initial administrator email |
| `BootstrapAdmin__Password` | Generated password of at least 12 characters |
| `FileStorage__RootPath` | `/tmp/poms-storage` for demo use only |
| `SeedDemoUsers` | `false` |

The frontend requires `VITE_API_URL=https://poms-api.onrender.com` at build time.

## Verification

1. Open `https://poms-api.onrender.com/health`; expect HTTP 200.
2. Sign in through `https://poms-motivation.onrender.com/signin` with the bootstrap administrator.
3. Confirm the dashboard loads and authenticated API calls return HTTP 200.
4. Confirm `POST /auth/register` returns HTTP 404; public registration is disabled.

## Storage limitation

Render Free has an ephemeral filesystem. Uploaded documents in `/tmp/poms-storage` disappear after a restart, spin-down, or redeploy. Do not upload real patient documents until object storage is configured.

Render Free also sleeps after inactivity. The first frontend or API request can take about one minute. This tier is suitable for development and demonstrations, not live clinical use.
