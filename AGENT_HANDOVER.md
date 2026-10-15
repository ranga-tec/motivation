# POMS engineering handover

Status date: 2026-10-15 (Asia/Colombo)

## 1. Read this first

POMS runs on **one Contabo VPS** at <https://169-58-173-191.sslip.io>. There is no other
environment. Everything about hosting, deploying, backups and restore is in
[`DEPLOY_CONTABO.md`](DEPLOY_CONTABO.md). Product scope is [`Requirement/PRD`](Requirement/PRD);
client-reported issues are in [`BUGS/bugs.md`](BUGS/bugs.md).

## 2. Current state

| Item | Value |
| --- | --- |
| Repository | <https://github.com/ranga-tec/motivation>, branch `main` |
| Production | Contabo VPS `169.58.173.191`, Docker Compose in `POMS/deploy/contabo` |
| Health | `/health` returned `Healthy` on 2026-10-15 |
| Data | Fresh production database (reference data + one administrator, no patients yet) |
| Administrator | `ranga@neuralsedge.com` (password held by the system owner) |
| Tests | 108 passing (`dotnet test POMS/POMS.sln -c Release`) |
| Frontend | lint and production build clean |
| Deployment | Manual: `git pull` + `docker compose up -d --build` on the server (no CI/CD) |

## 3. Architecture summary

React SPA (`POMS/frontend`) → Caddy → `Poms.Api` (ASP.NET Core 8, Identity bearer tokens) →
PostgreSQL 17. `Poms.Api` compiles the controllers in `POMS/src/Poms.Web/Api/V1`. The Razor MVC
app in `Poms.Web` is legacy and not deployed. See [`POMS/API_HOST.md`](POMS/API_HOST.md) for the
endpoint map and policies, and [`POMS/frontend/README.md`](POMS/frontend/README.md) for the
client structure.

Schema management: `EnsureCreated` plus `PostgresSchemaUpgrader` (additive changes only). There
are no EF Core migrations on PostgreSQL.

## 4. Revision history

### 2026-10-13 — UI redesign and PRD gap closure (`bc5b6f8`)

- New design system (`styles/theme.css`, `styles/app.css`) with Light, Dark, Classic (original
  navy) and System themes and a density setting; consistent shell and aligned footer.
- Dashboard per PRD §5: title, Total patients / Active records / Active patients / Today's
  activities with drill-down, six quick actions, recent activities, pending schedule.
- Registration wizard per PRD §6 (referral source, travel time, multiple confirmed telephone
  numbers, guardian phone and mobile, nationality list, photo review); reused for editing.
- Patient folder per PRD §7: Overview, Clinical records with quick entries, Clinical history with
  print buttons, Documents, Print forms.
- New Clinical Records list, grouped Reports with extra filters and CSV export, redesigned Users &
  access and Reference data pages, Settings page.
- API: `dashboard/activity`, `dashboard/records`, `patients/filter-options`, nationalities in
  registration options, folder details (guardian, referral, travel) on patient detail.
- New `PatientRead` policy: clinical and management users can open the patient folder (was 403).
- Fixed: editing a patient linked to a staff account failed ("selected staff account is
  unavailable"); adding a city sent an empty code that the API rejected.

### 2026-10-14 — Contabo hosting (`ea2e2f3`, `cc5d3e0`, `1f780c3`)

- `POMS/deploy/contabo`: Compose stack with Caddy HTTPS, setup script, nightly backups.
- API trusts `X-Forwarded-*` headers so rate limiting sees real client IPs behind the proxy.
- API honours `?sslmode=` in `DATABASE_URL`; the private Docker database uses `sslmode=disable`.
- HTTP/3 disabled in Caddy after browsers showed blank pages with `ERR_QUIC_PROTOCOL_ERROR`.

### 2026-10-15 — Hosting cleanup

- Removed all configuration, scripts and guides for previous cloud hosts, the MVC Dockerfiles,
  the Keycloak Docker files, the version-switch controller, and `SeedUsers.csx` / `seed-users.sql`.
- `SeedUsers.csx` contained a former cloud PostgreSQL connection string with a password. That database
  is retired, but the string remains in git history; treat it as compromised.
- Documentation rewritten for Contabo only.

Earlier history (phases 1–13: API extraction, React migration, Identity replacing Keycloak) is in
[`PLAN.md`](PLAN.md) and `git log`.

## 5. Known issues and open work

| Item | Detail |
| --- | --- |
| No CI/CD | Deploys are manual on the server. A GitHub Actions job could build and SSH-deploy. |
| Backups are on the same provider | Nightly dumps and Contabo snapshots both live at Contabo. Copy `/var/backups/poms` off-site regularly. Restore has been documented but not rehearsed. |
| No EF migrations on PostgreSQL | Destructive schema changes need `PostgresSchemaUpgrader` updated by hand. |
| sslip.io address | Works, but an organisation domain is better for staff. See DEPLOY_CONTABO.md §8. |
| Server in Europe | Users are in Sri Lanka; latency is ~350 ms. An Asia region would be faster. |
| Password self-service | Users cannot change their own password; an administrator resets it. |
| PRD confirmations | Exact PNO format, Colombo flag, yearly reset rule, final problem/cause/spinal lists (PRD §25). |
| Prescription-type report filter | PRD §17.3 lists it; not yet exposed in the Reports filters. |
| `ComponentCatalog` | Table exists with no admin screen. |
| Legacy `Poms.Web` MVC | Still in the solution and tests; not deployed. Remove when no longer needed for reference. |

## 6. Working rules

1. Read this file and `DEPLOY_CONTABO.md` before changing anything.
2. Run `git status` first and keep user-owned files (for example `hosting aproach.md`).
3. Before pushing: `dotnet build` and `dotnet test` in Release, then `npm run lint` and
   `npm run build` in `POMS/frontend`.
4. Pushing to `main` does **not** deploy. Deploy on the server and then check `/health` and a
   browser sign-in.
5. Never commit `.env`, passwords, tokens or connection strings.
6. Do not reintroduce type-ahead on the patient folder search button flow without asking; the
   client reverted it once. Type-ahead is expected in pickers (appointments, quick actions).
7. Keep UI changes inside the design tokens so all themes keep working.
