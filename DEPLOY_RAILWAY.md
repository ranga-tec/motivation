# Railway Deployment

This repository currently runs two live Railway services: a prototype service and a production service.

## Live Topology

- Prototype service: `motivation`
- Prototype URL: `https://motivation-production-f454.up.railway.app`
- Production service: `motivation-production`
- Production URL: `https://motivation-production-production.up.railway.app`

Both services use persistent storage for uploads and ASP.NET data-protection keys. They must not share the same application database.

## Required Configuration

Set these variables per service:

- `ASPNETCORE_ENVIRONMENT=Production`
- `DATABASE_URL=...`
- `FileStorage__RootPath=/app/storage`
- `DataProtection__KeysPath=/app/storage/data-protection-keys`
- `Security__ForceHttps=false`
- `SWITCH_SECRET=<same value on both services>`
- `VersionSwitch__Current=prototype` or `production`
- `VersionSwitch__PrototypeUrl=https://motivation-production-f454.up.railway.app`
- `VersionSwitch__ProductionUrl=https://motivation-production-production.up.railway.app`

## Database Rules

- Prototype and production must use separate PostgreSQL databases.
- The production deployment was fixed by moving it onto its own PostgreSQL database after an earlier shared-schema attempt caused identity and user-table conflicts.
- If you reuse one Railway PostgreSQL service for both variants, create a dedicated second database for production instead of reusing the prototype database.

## Deploy Prototype

From the `main` worktree:

```bash
railway up . --path-as-root --service motivation --environment production --detach
```

## Deploy Production

From a `production-backup` worktree:

```bash
railway up . --path-as-root --service motivation-production --environment production --detach
```

## Smoke Checks

```bash
curl -I https://motivation-production-f454.up.railway.app/
curl https://motivation-production-f454.up.railway.app/health
curl -I https://motivation-production-production.up.railway.app/
curl https://motivation-production-production.up.railway.app/health
```

## Notes

- The prototype branch contains the environment-driven switch configuration.
- The production branch contains the full production UI/features and the Railway-safe PostgreSQL startup fix.
- GitHub is the source of truth for code, but Railway should be redeployed from the correct branch/worktree so each live service stays aligned with its intended variant.
