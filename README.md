# POMS

POMS is an ASP.NET Core MVC application for prosthetic, orthotic, and spinal patient management.

## Live Services

- Prototype: `https://motivation-production-f454.up.railway.app`
- Production: `https://motivation-production-production.up.railway.app`

Both Railway services expose `/health` and support cross-app auto-login switching through `/Switch/Go`.

## Branch Model

- `main`: prototype line, Contabo fallback assets, and environment-driven version switching
- `production-backup`: restored production line with the fuller dashboard/reports/admin feature set

Do not replace `main` with `production-backup` anymore. The project now runs the prototype and production variants as separate Railway services.

## Switching Between Prototype And Production

Prototype and production switching is driven by:

- `VersionSwitch__Current`
- `VersionSwitch__PrototypeUrl`
- `VersionSwitch__ProductionUrl`
- `SWITCH_SECRET`

The prototype branch resolves switch targets from configuration in `POMS/src/Poms.Web/Models/VersionSwitchOptions.cs` and `POMS/src/Poms.Web/Controllers/SwitchController.cs`.

## Databases

- Prototype uses the standard Railway PostgreSQL database for the `motivation` service.
- Production must use a separate PostgreSQL database from prototype. Reusing the same identity tables across both variants causes schema conflicts.

If both apps share a PostgreSQL cluster, keep them in separate databases.

## Deployment Docs

- Railway deployment: [`DEPLOY_RAILWAY.md`](DEPLOY_RAILWAY.md)
- Contabo fallback deployment: [`DEPLOY_CONTABO.md`](DEPLOY_CONTABO.md)
- Internal branch/deployment handoff notes: [`AGENT_NOTES.md`](AGENT_NOTES.md)

## Seed Users

Fresh databases seed the standard role emails from `POMS/src/Poms.Infrastructure/Data/DbInitializer.cs`.

The live Railway passwords were rotated after deployment and are intentionally not documented in the repository. After any fresh deploy, rotate the seeded passwords immediately.
