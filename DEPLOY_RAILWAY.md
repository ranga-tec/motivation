# Railway Deployment

Status date: 2026-09-02 (Asia/Colombo)

POMS runs as a single Railway service backed by a managed PostgreSQL database. The earlier
prototype/production split described in older revisions of this file no longer exists.

## Live Topology

| Item | Value |
| --- | --- |
| Railway project | `kind-presence` |
| Project ID | `b9fcd418-f37a-4980-97e9-bc2c1b2e3161` |
| Environment | `production` |
| Application service | `poms-motivation` |
| Application URL | <https://poms-motivation-production.up.railway.app> |
| Health URL | <https://poms-motivation-production.up.railway.app/health> |
| Database service | `Postgres` (Railway managed PostgreSQL) |
| Region | `ams` (Amsterdam) |
| Source | GitHub `ranga-tec/motivation`, branch `main` |
| Build | Root `Dockerfile` (.NET 8, Tesseract installed) |

## Deployment Model

Railway watches GitHub `main` and builds automatically on every push. There is no manual
deploy step and no `railway up` in the normal workflow.

```bash
git push origin main    # GitHub -> Railway builds and deploys
```

Redeploy the current commit without a code change:

```bash
railway redeploy --service poms-motivation --yes
```

## Persistence

Two Railway volumes hold everything that must survive a redeploy.

| Volume | Service | Mount path | Holds |
| --- | --- | --- | --- |
| `postgres-volume` | `Postgres` | `/var/lib/postgresql/data` | The application database |
| `poms-motivation-volume` | `poms-motivation` | `/app/storage` | Uploaded patient files and data-protection keys |

Without the application volume, uploaded files vanish on redeploy and every login cookie is
invalidated, because ASP.NET data-protection keys are regenerated.

## Service Variables

Set on `poms-motivation`:

```text
ASPNETCORE_ENVIRONMENT=Production
DATABASE_URL=${{Postgres.DATABASE_URL}}
UsePostgreSQL=true
FileStorage__RootPath=/app/storage/uploads
DataProtection__KeysPath=/app/storage/data-protection-keys
Security__ForceHttps=false
DOTNET_USE_POLLING_FILE_WATCHER=1
```

`DATABASE_URL` must stay a Railway reference (`${{Postgres.DATABASE_URL}}`), not a pasted
connection string, so it follows any credential rotation on the database service.

`Security__ForceHttps=false` is deliberate: Railway terminates TLS at its proxy, and enabling
in-app HTTPS redirection there causes a redirect loop. The public URL is still HTTPS.

`DOTNET_USE_POLLING_FILE_WATCHER=1` avoids Linux inotify watcher limits on the build host.

There must be no `ConnectionStrings__DefaultConnection` variable. An earlier deployment carried
`Data Source=/tmp/poms-local.db`, which put the whole system on an ephemeral SQLite file.

## Schema Creation

`Program.cs` detects `DATABASE_URL`, converts it to an Npgsql connection string, then calls
`EnsureCreatedAsync()` followed by `PostgresSchemaUpgrader.ApplyAsync`. There are no EF Core
migrations on the PostgreSQL path.

This means an additive model change (a new column, a new enum member) is applied by the
upgrader, but a destructive change is not. Review `PostgresSchemaUpgrader` before shipping any
schema change that renames or drops something.

## Smoke Checks

```bash
curl https://poms-motivation-production.up.railway.app/health      # expects: Healthy
curl -I https://poms-motivation-production.up.railway.app/         # expects: 302 to /Identity/Account/Login
railway logs --service poms-motivation | tail -20                  # expects: "Database seeded successfully"
railway status                                                     # expects both services Online
```

## Common Operations

```bash
railway link --project b9fcd418-f37a-4980-97e9-bc2c1b2e3161
railway status
railway logs --service poms-motivation
railway variables --service poms-motivation
railway variables --service poms-motivation --set 'KEY=value'
railway volume list
```

The CLI on Windows/Git Bash rewrites container paths such as `/app/storage` into Windows paths.
Prefix those commands with `MSYS_NO_PATHCONV=1`.

## Not Configured

`OPENAI_API_KEY` is not set on the service, so **Patient Folder → Import old form** falls back to
offline Tesseract OCR. The OpenAI vision option shows as *Unavailable* until the key is added.

## Retired Environments

These appear in older documentation and are all dead. Do not restore them without a decision.

| Environment | URL | State |
| --- | --- | --- |
| Railway prototype | `motivation-production-f454.up.railway.app` | 404 |
| Railway production | `motivation-production-production.up.railway.app` | 404 |
| Render demo | `poms-motivation.onrender.com` | Unreachable |

The `production-backup` branch still exists in the repository but backs no live service.

## Fallback Path

`DEPLOY_CONTABO.md` and `docker-compose.contabo.yml` describe the same application stack on a
plain Ubuntu VPS with Docker. That path is generic and applies to any VPS provider, including
Kamatera. Note that a 1 GB instance cannot run the in-place Docker build — the .NET SDK stage
needs roughly 2 GB — so either build off-box or use a 2 GB instance.
