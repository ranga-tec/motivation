# Agent Handoff Notes - POMS Project

**Last Updated:** 2026-04-27

## Current Live Topology

The project no longer relies on a single Railway app that flips between prototype and production by resetting `main`.

Current live services:

- Prototype: `https://motivation-production-f454.up.railway.app`
- Production: `https://motivation-production-production.up.railway.app`

Each service has its own role:

- `main` backs the prototype line
- `production-backup` backs the production line

## Branch Roles

| Branch | Purpose | Live Service |
|--------|---------|--------------|
| `main` | Prototype branch with env-driven switch URLs, Railway hardening, Contabo fallback files | `motivation` |
| `production-backup` | Full production branch with dashboard/reports/admin modules | `motivation-production` |

## Important Change From The Old Workflow

The older handoff advised resetting `main` to `production-backup` and redeploying a single Railway app. That guidance is obsolete.

Do this instead:

- keep `main` as prototype
- keep `production-backup` as production
- deploy each branch to its own Railway service

## Version Switching

Prototype and production switching now depends on:

- `/Switch/Go`
- `VersionSwitch__Current`
- `VersionSwitch__PrototypeUrl`
- `VersionSwitch__ProductionUrl`
- `SWITCH_SECRET`

On the prototype branch, the switching configuration lives in:

- `POMS/src/Poms.Web/Models/VersionSwitchOptions.cs`
- `POMS/src/Poms.Web/Controllers/SwitchController.cs`
- `POMS/src/Poms.Web/Views/Shared/_Layout.cshtml`

On the production branch, the switch controller was updated with the live Railway URLs and a fixed-time signature comparison.

## Database Notes

- Prototype and production must not share the same application database.
- A previous production deployment pointed at a database that already contained unrelated UUID-based identity tables, which broke login and switching.
- The production app was corrected by moving it to its own PostgreSQL database and keeping prototype and production separated.

## Deployment Notes

Prototype deploy command from the `main` worktree:

```bash
railway up . --path-as-root --service motivation --environment production --detach
```

Production deploy command from a `production-backup` worktree:

```bash
railway up . --path-as-root --service motivation-production --environment production --detach
```

See also:

- `DEPLOY_RAILWAY.md`
- `DEPLOY_CONTABO.md`

## Credentials

Fresh databases still seed the standard four user emails from `DbInitializer.cs`.

Do not assume the seeded passwords are valid on live systems:

- the live Railway environments had their seeded passwords rotated
- those live passwords are intentionally not stored in the repository
- after any fresh deployment, rotate passwords immediately
