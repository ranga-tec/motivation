# POMS Agent Handover

Status date: 2026-08-14 (Asia/Colombo)

## 1. Purpose

This document gives the next engineering or operations agent enough context to continue the POMS hosting work without repeating discovery. It distinguishes the currently live Render demonstration, repository deployment options, the planned Azure pilot, known risks, and the exact point where the previous session stopped.

## 2. Executive status

- POMS is currently reachable at <https://poms-motivation.onrender.com>.
- The health endpoint <https://poms-motivation.onrender.com/health> returned `Healthy` with HTTP `200` on 2026-08-14.
- The Render service is a demonstration environment. It uses SQLite, uploaded-file storage, and ASP.NET data-protection keys under `/tmp`; all can be lost on restart, redeployment, or instance replacement.
- Render deploys automatically from GitHub branch `main`.
- The preferred one-month Azure pilot has been designed but not provisioned.
- Recommended Azure pilot: App Service for Linux B1 plus Azure Database for PostgreSQL Flexible Server B1MS with 32 GB storage.
- Azure App Service F1 was rejected for dependable use because it is limited to 60 CPU minutes per day, has no SLA, and is intended for trials and learning.
- Azure CLI is not installed on this workstation.
- The prior Azure session stopped because no controllable Browser Use session was connected. No Azure resource group, App Service, PostgreSQL server, storage account, or Key Vault was created.

## 3. Repository and Git state

| Item | Value |
| --- | --- |
| Workspace | `X:\Developments\motivation` |
| Repository | `https://github.com/ranga-tec/motivation` |
| Current branch | `main` |
| Local HEAD | `bbc6a6cb72d79d2aeb78eb2a97137add47cd6f84` |
| `origin/main` | `459e2217e6b40f85a8b7ecea0b36ba70229552f6` |
| Live Render commit last verified during deployment | `459e2217e6b40f85a8b7ecea0b36ba70229552f6` |
| Local-only handover commit | `bbc6a6c` (`Document deployment and Azure handover`) |
| Additional worktree branch | `production-backup` at `8039b62` |

The local `main` branch contains the deployment handover commit that is not on `origin/main`. Do not push blindly: Render watches `main`, so a push can trigger a redeployment and erase the demo's temporary SQLite database, uploads, and login-cookie keys.

The workspace was already dirty before this document was created. Preserve these user-owned changes and artifacts:

- Modified: `POMS/tests/Poms.Tests/SchedulingWorkflowTests.cs`
- Modified: `POMS_Proposal_Exceed_Clinics.html`
- Untracked: `BUGS/`, `TODO.md`, `graphify-out/`, screenshots, and `output/`

Inspect `git status --short` again before committing. Do not include unrelated files in a deployment commit.

## 4. Application overview

POMS is an ASP.NET Core 8 MVC application. The web entry point is `POMS/src/Poms.Web/Program.cs`; persistence is implemented with Entity Framework Core through `PomsDbContext`.

Relevant hosting behavior:

- If `DATABASE_URL` exists, `Program.cs` converts the URL into an Npgsql connection string and uses PostgreSQL.
- Otherwise, `UsePostgreSQL` and `UseSQLite` select the configured provider.
- Development defaults to SQLite through `appsettings.Development.json`.
- The root `Dockerfile` builds and publishes `Poms.Web` using .NET 8 and listens on the platform-provided `PORT` (default `8080`).
- `/health` is a process-level health endpoint. It does not currently prove that database initialization, storage access, or seeding succeeded.
- PostgreSQL and SQLite are initialized with `EnsureCreatedAsync()` and provider-specific schema-upgrader classes.
- `FileStorageService` writes patient files to a local filesystem; no Azure Blob implementation exists yet.
- ASP.NET data-protection keys are persisted to a configured filesystem directory.
- Uploaded extensions are limited by configuration to PDF, JPG/JPEG, PNG, and DOCX; the configured maximum is 10 MB.

Key files:

- `POMS/src/Poms.Web/Program.cs`
- `POMS/src/Poms.Web/appsettings.json`
- `POMS/src/Poms.Web/appsettings.Production.json`
- `POMS/src/Poms.Infrastructure/Data/PomsDbContext.cs`
- `POMS/src/Poms.Infrastructure/Data/DbInitializer.cs`
- `POMS/src/Poms.Infrastructure/Data/PostgresSchemaUpgrader.cs`
- `POMS/src/Poms.Infrastructure/Data/SqliteSchemaUpgrader.cs`
- `POMS/src/Poms.Infrastructure/Services/FileStorageService.cs`
- `Dockerfile`
- `.dockerignore`
- `render.yaml`
- `docker-compose.contabo.yml`
- `DEPLOYMENT_HANDOVER.md`
- `DEPLOY_CONTABO.md`

## 5. Current Render demonstration

| Item | Value |
| --- | --- |
| Application URL | <https://poms-motivation.onrender.com> |
| Health URL | <https://poms-motivation.onrender.com/health> |
| Dashboard | <https://dashboard.render.com/web/srv-d9td6qrncjis7390ldhg> |
| Service ID | `srv-d9td6qrncjis7390ldhg` |
| Service name | `poms-motivation` |
| Runtime | Docker / .NET 8 |
| Plan | Render free web service |
| Region | Singapore |
| Source | GitHub `main` |
| Automatic deployment | On commit |
| Database | SQLite |
| SQLite path | `/tmp/poms-local.db` |
| Upload path | `/tmp/poms-storage` |
| Data-protection key path | `/tmp/poms-data-protection-keys` |

Effective settings from `render.yaml`:

```text
ASPNETCORE_ENVIRONMENT=Production
DOTNET_USE_POLLING_FILE_WATCHER=1
UsePostgreSQL=false
UseSQLite=true
ConnectionStrings__DefaultConnection=Data Source=/tmp/poms-local.db
FileStorage__RootPath=/tmp/poms-storage
DataProtection__KeysPath=/tmp/poms-data-protection-keys
Security__ForceHttps=false
```

`DOTNET_USE_POLLING_FILE_WATCHER=1` was added after Render hit its Linux inotify watcher limit. `Security__ForceHttps=false` avoids an internal redirect loop behind Render's TLS-terminating proxy; the public Render URL still uses HTTPS.

### Why PostgreSQL is not used on Render

The connected Render workspace already used its free PostgreSQL allocation for `neuedge-erp-db`. Render rejected another free PostgreSQL database, so this POMS demo was switched to temporary SQLite rather than creating a paid service.

### Render deployment history

| Commit | Purpose |
| --- | --- |
| `967f045` | Add Render blueprint and Docker deployment files |
| `93930f7` | Switch the free Render demo to SQLite under `/tmp` |
| `459e221` | Use polling file watchers to avoid Render host limits |
| `bbc6a6c` | Local-only deployment/Azure handover documentation |

### Render warning

Do not enter real patient information. There is no dependable backup or restore point for `/tmp/poms-local.db` or `/tmp/poms-storage`. A pushed commit can redeploy the service and reset records, files, seeded-user state, and cookies.

## 6. Demonstration credentials and security

The application seeds predictable demonstration accounts in `POMS/src/Poms.Infrastructure/Data/DbInitializer.cs`.

Administrator requested for the demo:

```text
URL: https://poms-motivation.onrender.com
Email: admin@poms.lk
Temporary password: Admin@123
Role: ADMIN
```

Treat this credential as compromised by design because it exists in source and handover material. It is acceptable only for the disposable demo. Changing it inside the Render instance is not durable: a fresh SQLite database can restore the original seeded password.

Before any pilot containing real data:

1. Remove all hard-coded passwords and unnecessary default accounts from `DbInitializer`.
2. Seed the first administrator from a deployment secret or one-time administrative command.
3. Rotate the administrator password immediately after first login.
4. Add MFA or an approved external identity provider.
5. Review role permissions and disable unused accounts.
6. Never commit Azure, PostgreSQL, Blob Storage, or Key Vault credentials.

## 7. Render operating procedure

### Read-only health check

```powershell
curl.exe --fail --show-error https://poms-motivation.onrender.com/health
curl.exe --head https://poms-motivation.onrender.com/
```

The free service may cold-start. Allow up to roughly 90 seconds before treating the first request as failed.

### Authenticated smoke test

1. Sign in with an authorized demonstration account.
2. Register a clearly disposable test patient through all five steps.
3. Confirm the patient appears in search and opens successfully.
4. Verify any required upload/download workflow.
5. Soft-delete the disposable record.
6. Review Render logs for schema, file-storage, and authentication errors.

The patient-registration workflow and admin login were verified on 2026-08-11. The health endpoint was rechecked on 2026-08-14 and returned HTTP `200 Healthy`.

### Before any Render push/redeploy

1. Confirm whether the demo contains data that must be retained.
2. Export the SQLite database and `/tmp/poms-storage` before triggering a deploy if retention matters.
3. Review the exact commit scope.
4. Expect existing authentication cookies to become invalid if data-protection keys are replaced.
5. Re-run the health and authenticated smoke tests after deployment.

## 8. Other repository deployment path: Contabo

The repository also contains a Docker Compose deployment for a Contabo VPS:

- `poms-web`: application container built from the root `Dockerfile`
- `poms-db`: `postgres:16-alpine`
- Persistent named volumes for PostgreSQL data, patient files, and data-protection keys
- Internal `DATABASE_URL` pointing from the web container to `poms-db`
- Host port defaults to `8081`

This path is documented in `DEPLOY_CONTABO.md` and `docker-compose.contabo.yml`. It is not the currently verified public deployment described in this handover. Never use the Compose default PostgreSQL password (`change-this-now`); supply a strong secret through the deployment environment.

## 9. Azure decision and target architecture

The chosen direction is an Azure pilot using paid-capability tiers funded by the eligible free-account credit—not App Service F1.

| Concern | Azure target | Initial configuration |
| --- | --- | --- |
| Web application | Azure App Service for Linux | B1 for the pilot |
| Relational database | Azure Database for PostgreSQL Flexible Server | Burstable B1MS, 32 GB, TLS required |
| Temporary pilot file persistence | App Service `/home` | Single instance only |
| Production patient files | Azure Storage / private Blob container | Standard GPv2, Hot LRS initially |
| Secrets | App Service settings and Key Vault | Managed identity and least privilege |
| ASP.NET data-protection keys | Blob-backed key ring protected by Key Vault | Separate path/container per environment |
| Monitoring | Application Insights / Azure Monitor | 5xx, restart, database, storage, and budget alerts |

Choose one region for the app, database, and storage. South India is geographically closer to Sri Lanka; Southeast Asia matches the current Render region. Confirm service availability, latency, data residency, and the current price before provisioning.

### Azure free-account facts checked on 2026-08-14

- Eligible new Azure customers can receive US$200 credit usable within 30 days.
- The account must move to pay-as-you-go after 30 days or after the credit is exhausted to keep resources running and continue applicable free allowances; otherwise services are disabled.
- The published 12-month PostgreSQL allowance includes up to 750 B1MS Flexible Server hours per month, 32 GB storage, and 32 GB backup storage for eligible new customers.
- App Service F1 provides shared compute with 60 CPU minutes per day, 1 GB RAM, and 1 GB storage, with no SLA; Microsoft does not support it for production workloads.

The earlier retail estimate, calculated on 2026-08-11, was approximately US$36.74/month in Southeast Asia or US$37.55/month in South India for B1 App Service, B1MS PostgreSQL with 32 GB storage, and 10 GB Hot LRS Blob storage. This is a planning estimate only; use the Azure Pricing Calculator or Retail Prices API immediately before provisioning.

## 10. Exact Azure continuation point

No Azure resources exist from the previous session. The last attempt stopped at account/browser preparation:

- A Microsoft account picker was visible.
- Normal Browser Use was not connected to the agent.
- Azure CLI was not installed and remains unavailable as of 2026-08-14.
- The user was told not to enable Full CDP access because it is unnecessary and elevated-risk.
- Phone verification, card verification, legal acceptance, subscription upgrade, and pay-as-you-go authorization must be completed or approved by the user, never by the agent without explicit authorization.

Two safe continuation routes are available.

### Route A: Azure portal through connected Browser Use

1. Connect the normal browser session in Codex/ChatGPT settings.
2. Open the Azure free-account page and sign in to the intended Microsoft account.
3. The user completes phone/card verification and any legal or billing acceptance.
4. Confirm subscription name, credit status, region availability, and budget controls.
5. Provision only after the user authorizes the named subscription and expected spend.

Do not enable Full CDP access merely for this task.

### Route B: Azure CLI

1. Install Azure CLI using an approved installation method.
2. Run `az login` and have the user complete interactive authentication.
3. Run `az account show` and `az account list --output table`.
4. Ask the user to confirm the exact subscription before creating resources.
5. Set a budget alert before provisioning billable resources.

## 11. Recommended Azure implementation sequence

### Phase 0: approval and safety

1. Confirm the Azure tenant, subscription, region, naming convention, owner, and monthly budget.
2. Confirm that the account is eligible for the intended free credit and PostgreSQL allowance.
3. Decide whether this is a disposable pilot or a clinical production project.
4. Do not copy real patient data from Render into a pilot without governance approval.

### Phase 1: application hardening

1. Remove predictable seed credentials.
2. Make first-admin creation secret-driven and optional.
3. Replace the `EnsureCreated` PostgreSQL lifecycle with tested, versioned EF Core migrations.
4. Make fatal schema or required-seeding failures fail startup/readiness.
5. Add a readiness check that verifies database and required storage access.
6. Review logs to ensure they do not expose credentials or patient data.

### Phase 2: Azure foundation

1. Create a resource group in the approved region.
2. Create PostgreSQL Flexible Server B1MS with 32 GB storage, TLS enforced, backups configured, and public/network access minimized.
3. Create a Linux B1 App Service plan and web app.
4. Enable managed identity.
5. Create the storage account and private Blob containers if the Blob work is included.
6. Create Key Vault or use protected App Service settings for initial pilot secrets.
7. Configure Application Insights, budget alerts, and operational alerts.

### Phase 3: application configuration

At minimum:

```text
ASPNETCORE_ENVIRONMENT=Production
UsePostgreSQL=true
UseSQLite=false
ConnectionStrings__DefaultConnection=<protected Npgsql connection string>
Security__ForceHttps=<value tested for the App Service proxy configuration>
```

For the short single-instance pilot before Blob support:

```text
FileStorage__RootPath=/home/poms-storage
DataProtection__KeysPath=/home/poms-data-protection-keys
```

Do not use `/tmp` on Azure for data that must survive. Do not scale the filesystem-based pilot beyond one instance. Blob Storage is required before multi-instance or production use.

### Phase 4: deployment

1. Deploy to a non-production/staging web app first.
2. Prefer a controlled GitHub Actions or Azure deployment workflow over an undocumented manual upload.
3. Apply and verify database migrations against a fresh test database.
4. Keep secrets out of workflow logs and repository files.
5. Record resource IDs, public URLs, configuration names, deployment commit, and rollback target.

### Phase 5: acceptance testing

Verify all of the following:

1. `/health` and database/storage-aware readiness.
2. Administrator login and authorization.
3. Full five-step patient registration.
4. Duplicate-patient detection.
5. Patient search and folder access.
6. Appointment scheduling workflow.
7. File upload, download, authorization, and deletion.
8. Reports and print views.
9. Soft deletion and audit behavior.
10. Restart persistence for database, files, and cookies.
11. Backup restore into a separate test database.
12. Logging, monitoring, budget, and failure alerts.

### Phase 6: cutover

1. Obtain acceptance from the system owner.
2. Confirm backup and rollback steps.
3. If Render contains data that must be retained, freeze writes and export SQLite plus files before any restart.
4. Change the approved hostname only after acceptance tests pass.
5. Rotate all temporary credentials.
6. Monitor the new deployment closely during the initial operating period.

## 12. Production blockers and go-live gates

Do not call POMS production-ready until all gates are complete:

- Persistent PostgreSQL is in use and a restore has been tested.
- PostgreSQL schema changes use versioned migrations.
- Patient files are stored durably in a private container and end-to-end tests pass.
- Data-protection keys survive restart and are protected appropriately.
- Predictable seed credentials and unused default accounts are removed.
- Privileged credentials are rotated and MFA/external identity is addressed.
- Health/readiness verifies database and required storage dependencies.
- Logs exclude patient data and secrets and have an approved retention policy.
- Backup frequency, retention, recovery time objective, recovery point objective, and incident ownership are documented.
- Monitoring and budget alerts are active and tested.
- Data residency, privacy, security, and clinical-governance reviews are approved.
- A staging deployment and rollback procedure have been exercised.
- A named operator owns routine updates, backup review, access review, and incident response.

## 13. Verification history

### Verified on 2026-08-11

- All 45 tests passed at that point in repository history.
- Render `/health` returned `200 Healthy`.
- Admin login worked.
- Full five-step patient registration saved successfully.
- The disposable test patient was soft-deleted.
- Render automatic deployment from `main` was enabled.

### Rechecked on 2026-08-14

- Render `/health` returned body `Healthy` and HTTP `200`.
- Local HEAD is `bbc6a6c`; `origin/main` is `459e221`.
- Azure CLI is not installed.
- A fresh Debug test run did not complete because the shared C# compiler locked `Poms.Web/obj/Debug/net8.0/Poms.Web.dll` (`CS2012`). This is a workstation/process lock, not a test assertion failure.
- A second Release run with `UseSharedCompilation=false` exceeded the 180-second command limit without returning a conclusive result.
- The build output reported that `AutoMapper` 12.0.1 has known high-severity advisory `GHSA-rvv3-g6hj-g44x`. Upgrade or otherwise resolve the advisory before production deployment.
- Therefore, the last conclusive suite result remains the 45-test pass from 2026-08-11. Rerun the suite in a clean process/environment before deployment and record the result here.

## 14. Useful commands

### Repository inspection

```powershell
git status --short --branch
git log --oneline --decorate -10
git diff --check
git diff --stat
```

### Build and test

```powershell
dotnet restore POMS\POMS.sln
dotnet build POMS\POMS.sln --no-restore
dotnet test POMS\POMS.sln --no-restore --verbosity minimal
```

### Local Docker build

```powershell
docker build --tag poms:handover .
docker run --rm --publish 8080:8080 --env PORT=8080 poms:handover
```

Do not run the image against real data without persistent database/file/key configuration.

### Render health

```powershell
curl.exe --fail --show-error --location --max-time 90 https://poms-motivation.onrender.com/health
```

## 15. Agent operating rules

1. Start by reading this file and `DEPLOYMENT_HANDOVER.md`.
2. Check `git status` before modifying anything; preserve user-owned changes.
3. Confirm current external service state rather than relying on historical status.
4. Never push `main` casually because Render auto-deploy can erase temporary data.
5. Never expose or commit cloud/database secrets.
6. Stop for user action at phone/card verification, legal acceptance, subscription selection, pay-as-you-go authorization, or any unexpected spend.
7. Resolve the exact Azure subscription and resource targets before creating or deleting anything.
8. Use a staging environment and reversible deployment path.
9. Record every created Azure resource, configuration decision, deployment commit, validation result, cost assumption, and rollback step.
10. Do not claim production readiness until every go-live gate is satisfied.

## 16. Immediate next action

The next agent should not start by changing application code or creating Azure resources. First:

1. Ask the user to connect normal Browser Use or approve installation/authentication of Azure CLI.
2. Verify the intended Azure account and subscription.
3. Confirm free-credit eligibility, region, and a monthly budget cap.
4. Obtain explicit authorization for the expected resources and spend.
5. Then execute the Azure pilot sequence in Sections 10 and 11.

Until that happens, Render remains the only verified live environment and must be treated as disposable.

## 17. References

- `DEPLOYMENT_HANDOVER.md` — earlier hosting and pricing handover
- `DEPLOY_CONTABO.md` — Contabo VPS operating path
- `render.yaml` — current Render blueprint
- `docker-compose.contabo.yml` — VPS PostgreSQL/application stack
- Azure account terms: <https://azure.microsoft.com/en-us/pricing/purchase-options/azure-account>
- Azure free services: <https://azure.microsoft.com/en-us/pricing/free-services/>
- Azure App Service Linux pricing: <https://azure.microsoft.com/en-us/pricing/details/app-service/linux/>
- Azure PostgreSQL: <https://azure.microsoft.com/en-us/products/postgresql/>
- Azure pricing calculator: <https://azure.microsoft.com/en-us/pricing/calculator/>
