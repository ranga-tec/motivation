# POMS Agent Handover

Status date: 2026-09-02 (Asia/Colombo)

## 1. Read this first

This file replaces the 2026-08-14 handover, which described a Render demonstration and a planned
Azure pilot. **Both are gone.** Render is unreachable, the two older Railway services return 404,
and no Azure resource was ever created. POMS now runs on one Railway service with a managed
PostgreSQL database.

If you are picking this up cold, read this file, then `DEPLOY_RAILWAY.md`. Treat
`DEPLOYMENT_HANDOVER.md` and `AGENT_NOTES.md` as historical.

## 2. Executive status

- Live at <https://poms-motivation-production.up.railway.app>, deployed from GitHub `main`.
- `/health` returned `Healthy` on 2026-09-02.
- Storage is durable: managed PostgreSQL plus a Railway volume for uploads and data-protection keys.
- Deployment is automatic — a push to `main` builds and releases.
- **The seeded demonstration passwords are live and unrotated.** See Section 6.
- The full test suite passes: 67 tests.

## 3. Repository state

| Item | Value |
| --- | --- |
| Workspace | `X:\Developments\motivation` |
| Repository | <https://github.com/ranga-tec/motivation> |
| Branch | `main` |
| HEAD and `origin/main` | `3f3ad71` |
| Other branch | `production-backup` — backs no live service |

`main` and `origin/main` are level. The working tree is clean apart from a stray `image.png` at
the repository root (a Kamatera pricing screenshot) that was deliberately left untracked.

## 4. Application overview

ASP.NET Core 8 MVC. Entry point `POMS/src/Poms.Web/Program.cs`; persistence through
`PomsDbContext` (EF Core).

Provider selection:

- `DATABASE_URL` present → parsed into an Npgsql connection string → PostgreSQL.
- Otherwise `UsePostgreSQL` / `UseSQLite` decide. Development defaults to SQLite
  (`appsettings.Development.json`, `poms-local.db`).

Schema is created by `EnsureCreatedAsync()` plus `PostgresSchemaUpgrader` / `SqliteSchemaUpgrader`.
**There are no EF Core migrations on the PostgreSQL path.** Additive changes are handled; renames
and drops are not.

Seeding on every start: roles and users (`DbInitializer`), then locations, referral sources, main
problem types, cause/reason types, nationalities, and the device catalogue (`SampleDataSeeder`).
Each seeder is a no-op when its table already has rows.

## 5. Live environment

See `DEPLOY_RAILWAY.md` for the complete configuration. Summary:

| Item | Value |
| --- | --- |
| Railway project | `kind-presence` (`b9fcd418-f37a-4980-97e9-bc2c1b2e3161`) |
| Services | `poms-motivation` (app), `Postgres` (database) |
| Volumes | `poms-motivation-volume` → `/app/storage`; `postgres-volume` → PostgreSQL data |
| Region | Amsterdam (`ams`) |

Latency note: users are in Sri Lanka and the service is in Amsterdam. A Singapore region would cut
round-trip time substantially. This has not been changed because it means re-provisioning.

## 6. Credentials — open risk

`POMS/src/Poms.Infrastructure/Data/DbInitializer.cs` seeds fixed accounts and passwords, and they
are committed to the repository:

```text
admin@poms.lk / Admin@123           (ADMIN)
clinician@poms.lk / Clinic@123      (CLINICIAN)
registrar@poms.lk / Data@123        (DATA_ENTRY)
viewer@poms.lk / View@123           (VIEWER)
management@poms.lk / Manage@123     (MANAGEMENT)
```

**These credentials work on the live site right now.** Older documentation claimed the live
passwords had been rotated; that was true of the retired services, not this one. Anyone who reads
this repository can sign in as an administrator.

This is the single largest blocker before any real patient data is entered. Rotating a password in
the running app is also not durable on its own: a fresh database re-seeds the original values.

Fix properly by making first-administrator creation secret-driven and removing the hard-coded
passwords, then rotating.

## 7. Revision history — 2026-08-30 to 2026-09-02

Five commits on top of `459e221`.

| Commit | Summary |
| --- | --- |
| `bbc6a6c` | Deployment/Azure handover documentation (written earlier, pushed in this window) |
| `8f87609` | Patient form OCR import |
| `3aca409` | Type-ahead patient search and the device catalogue admin |
| `6ac9d7d` | Reverted the type-ahead patient search |
| `3f3ad71` | Patient and appointment UX backlog |

### Infrastructure

- Provisioned the `Postgres` service and pointed the app at it with
  `DATABASE_URL=${{Postgres.DATABASE_URL}}`.
- Removed `ConnectionStrings__DefaultConnection`, which had been `Data Source=/tmp/poms-local.db`.
  The service had been running on an ephemeral SQLite file that was discarded on every redeploy.
- Attached `poms-motivation-volume` at `/app/storage` and moved `FileStorage__RootPath` and
  `DataProtection__KeysPath` onto it, so uploads and login cookies survive redeploys.

### Features

- **OCR import** (`8f87609`): OpenAI vision engine with a Tesseract offline fallback, image
  validation, an `ImportLegacy` upload view, and a hosted cleanup service for staged scans.
  Both Dockerfiles now install `tesseract-ocr`.
- **Device catalogue** (`3aca409`): `DeviceCatalog` had a table and a delivery-form dropdown but no
  UI at all, so the dropdown was always empty. Added **Administration → Devices** with device and
  device-type CRUD, and seeded 17 standard prosthetic, orthotic, and spinal devices.

### Patient search — added, then reverted

`3aca409` replaced the Search button with type-as-you-go search; `6ac9d7d` reverted it at the
user's request. The patient folder uses the Search button. Do not reintroduce type-ahead there
without asking. Note the distinction: type-ahead **was** wanted on the appointment patient picker,
which is a dropdown rather than a results table.

### UX backlog (`3f3ad71`)

Nine items from `TODO.md`:

1. **Density.** The registration wizard announced the current step three times — modal header,
   sticky stepper, and a per-step icon plus "STEP N" eyebrow. Roughly 715px of chrome sat above
   the first input, so one field was visible. Removed the duplicated layer and compacted the
   optional photo card; about 175px reclaimed. An admin font-size setting was considered and
   rejected — it would shrink labels and inputs too, and browsers already offer zoom.
2. **Focus.** Step changes and modal opens focused a heading. Both now focus the first real
   field, skipping the visually-hidden photo input. `Enter` advances a step, `Alt`+arrow moves
   between steps.
3. **Review photo.** The review step shows the patient photo with click-to-enlarge.
4. **Email accepts N/A** (also `NA`, `n/a`), still rejecting malformed addresses.
5. **`IdentificationType.NotApplicable`** added. Selecting it clears and disables the number
   field. Duplicate detection now ignores blank identification numbers, which would otherwise
   match every such patient to every other.
6. **"Records" renamed "Clinical Records"** in navigation, page titles, and the folder tab.
7. **Prescription subtype** label no longer lingers after its select is hidden.
8. **Appointments**: patient type-ahead via a new `SearchPatients` endpoint; the record dropdown
   now shows centre and assessment/fitting/delivery counts.
9. **Handled By** is a searchable clinician list that still accepts a typed name for staff who
   have no account.

A generic combobox in `site.js` (`select[data-combobox]`, optional `data-combobox-endpoint`)
backs items 8 and 9 and shares styling with the existing city combobox.

## 8. Known issues and unfinished work

| Item | Detail |
| --- | --- |
| Seeded passwords live | Section 6. Highest priority before real data. |
| No EF migrations on PostgreSQL | `EnsureCreated` plus a hand-written upgrader. A destructive schema change will not apply. |
| `SearchPatients` untested with data | The endpoint returns 200 and valid JSON, but the local dev database has no patients, so matching is unexercised. |
| `AutoMapper` 12.0.1 advisory | `GHSA-rvv3-g6hj-g44x`, high severity. Surfaces as `NU1903` on every build. |
| `/health` is shallow | Process-level only. It does not prove database or storage access. |
| No backups | Nothing dumps PostgreSQL on a schedule. Restore has never been tested. |
| `ComponentCatalog` has no UI | Same gap the device catalogue had before `3aca409`. |
| OpenAI OCR disabled in production | `OPENAI_API_KEY` is unset, so only offline Tesseract OCR runs. |
| Amsterdam region | ~150ms+ from Sri Lanka. Singapore would be materially faster. |

## 9. Local development

Development uses SQLite, so no database server is needed.

```bash
ASPNETCORE_ENVIRONMENT=Development \
ASPNETCORE_URLS=http://localhost:5011 \
dotnet run --project POMS/src/Poms.Web --no-launch-profile
```

For offline OCR on Windows, Tesseract must be resolvable. It is commonly installed at
`C:\Program Files\Tesseract-OCR\tesseract.exe` but not added to `PATH`, in which case the engine
reports *Unavailable*. Either add it to `PATH` or set:

```bash
TESSERACT_EXECUTABLE_PATH="C:\Program Files\Tesseract-OCR\tesseract.exe"
```

The local database (`POMS/src/Poms.Web/poms-local.db`, gitignored) is empty of patients, which
makes any search feature look broken locally. Seed a patient before judging search behaviour.

```bash
dotnet build POMS/POMS.sln -c Release
dotnet test POMS/POMS.sln -c Release      # 67 tests
```

## 10. Hosting alternatives considered

`DEPLOY_CONTABO.md` plus `docker-compose.contabo.yml` describe the same stack on any Ubuntu VPS
with Docker, which covers Kamatera and similar providers.

If moving to a VPS, note:

- A 1 GB instance cannot run `docker compose up --build`; the .NET SDK build stage needs roughly
  2 GB. Either use a 2 GB instance or build images in CI and pull them.
- Railway provides TLS automatically. On a VPS you supply a domain plus Nginx and Let's Encrypt,
  then set `Security__ForceHttps=true`.
- Railway's managed volume is not a backup. On a VPS, `pg_dump` on a schedule is your
  responsibility from day one.

Azure was previously proposed (App Service B1 plus PostgreSQL Flexible Server B1MS, roughly
US$37/month). No Azure resource was ever created and the Azure CLI is not installed. That plan is
dormant, not in progress.

## 11. Agent operating rules

1. Read this file and `DEPLOY_RAILWAY.md` before changing anything.
2. Run `git status` first and preserve user-owned changes.
3. Verify live state with `railway status` and `/health` rather than trusting this document's date.
4. A push to `main` deploys to production. Confirm scope before pushing.
5. Never commit secrets. `DATABASE_URL` stays a Railway reference, never a pasted connection string.
6. Run `dotnet build` and `dotnet test` before pushing; the suite is fast.
7. Do not reintroduce type-ahead on the patient folder search — it was explicitly reverted.
8. Do not describe POMS as production-ready while Section 6 is open.

## 12. Suggested next actions

1. Remove hard-coded seed passwords; make the first administrator secret-driven; rotate.
2. Add a scheduled `pg_dump` and test a restore.
3. Replace `EnsureCreated` with versioned EF Core migrations before the schema changes again.
4. Resolve the AutoMapper advisory.
5. Decide on region — staying in Amsterdam or re-provisioning in Singapore.
6. Deepen `/health` into a readiness check covering database and storage.

## 13. References

- `DEPLOY_RAILWAY.md` — current live deployment
- `DEPLOY_CONTABO.md` — VPS fallback path
- `README.md` — application and feature overview
- `TODO.md` — outstanding requests from the system owner
- `DEPLOYMENT_HANDOVER.md`, `AGENT_NOTES.md` — historical, describe retired environments
