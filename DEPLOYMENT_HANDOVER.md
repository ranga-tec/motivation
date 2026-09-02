# POMS Deployment Handover

> **Historical document — superseded on 2026-09-02.**
> The environments described below are retired: the two `motivation-production-*` Railway
> services return 404 and the Render demo is unreachable. POMS now runs as a single Railway
> service with managed PostgreSQL. See [`AGENT_HANDOVER.md`](AGENT_HANDOVER.md) and
> [`DEPLOY_RAILWAY.md`](DEPLOY_RAILWAY.md). Kept for background only.

Last updated: 2026-08-11  
System: Patient Outcome Management System (POMS)  
Repository: `https://github.com/ranga-tec/motivation`  
Current branch: `main`

## 1. Executive summary

POMS is live on Render at:

- Application: <https://poms-motivation.onrender.com>
- Health check: <https://poms-motivation.onrender.com/health>
- Render service: <https://dashboard.render.com/web/srv-d9td6qrncjis7390ldhg>

The current Render deployment is a **demonstration environment only**. It runs the application on Render's free web tier with SQLite, uploaded files, and ASP.NET data-protection keys under `/tmp`. Render can erase that storage when the service restarts, redeploys, or is replaced. Patient records, uploaded files, seeded users, and existing login cookies can therefore disappear.

The latest recommended route is:

1. Keep Render only for short-lived demonstrations.
2. Move the database to Azure Database for PostgreSQL Flexible Server.
3. Run the .NET 8 application on Azure App Service for Linux.
4. Move patient documents to a private Azure Blob Storage container.
5. Remove hard-coded seed passwords and store deployment secrets in Azure configuration/Key Vault.

Azure does offer a free-account promotion. It is not simply “everything free for 30 days”: eligible new customers receive **US$200 credit that expires after 30 days**, selected services have monthly free allowances, and PostgreSQL has a separate 12-month allowance. See [Section 7](#7-azure-free-account-finding).

## 2. Current Render deployment

| Item | Current value |
| --- | --- |
| Service name | `poms-motivation` |
| Service ID | `srv-d9td6qrncjis7390ldhg` |
| Runtime | Docker / .NET 8 |
| Render plan | Free web service |
| Render region | Singapore |
| Source | GitHub `main` branch |
| Automatic deployment | On commit |
| Health path | `/health` |
| Database | SQLite |
| SQLite file | `/tmp/poms-local.db` |
| Uploaded files | `/tmp/poms-storage` |
| Data-protection keys | `/tmp/poms-data-protection-keys` |

The health endpoint returned HTTP 200 with `Healthy` on 2026-08-11.

### Why Render is using SQLite

The connected Render workspace already contains an active free PostgreSQL database (`neuedge-erp-db`). Render rejected creation of another free PostgreSQL database because the workspace permits only one active free-tier database. POMS was therefore deployed using its existing SQLite support so that the UI and registration workflow could be demonstrated without adding a paid database.

This does **not** mean PostgreSQL is installed locally inside the Render container. The live demo is using one SQLite file stored on the container's temporary filesystem.

### Render configuration

The deployment definition is in `render.yaml`. Its effective application settings are:

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

`Security__ForceHttps=false` prevents an internal redirect loop behind Render's TLS-terminating reverse proxy. Public traffic is still served through Render's HTTPS URL.

`DOTNET_USE_POLLING_FILE_WATCHER=1` avoids the Linux inotify watcher limit that caused the first container startup to fail.

### Deployment commits

| Commit | Purpose |
| --- | --- |
| `967f045` | Add Render blueprint and Docker deployment files |
| `93930f7` | Configure the free Render demo to use temporary SQLite storage |
| `459e221` | Avoid Render's inotify watcher limit |

The live service was verified against commit `459e2217e6b40f85a8b7ecea0b36ba70229552f6`.

## 3. Accounts and immediate security action

The application currently seeds five roles and five predictable demonstration users in `POMS/src/Poms.Infrastructure/Data/DbInitializer.cs`. The requested administrator is:

```text
Email: admin@poms.lk
Temporary password: Admin@123
Role: ADMIN
```

This credential is intentionally simple for the demo and is already present in source code. **Do not use it for real patient data.** Before a pilot or production deployment:

1. Remove all hard-coded passwords from `DbInitializer`.
2. Seed only the first administrator from protected deployment settings, or create it through a one-time administrative command.
3. Use a unique long password and rotate it immediately after first login.
4. Add MFA or an external identity provider before clinical use.
5. Review and disable unused default clinician, registrar, viewer, and management accounts.

Because the Render database is temporary, changing the password only in the running demo is not durable; the simple seeded password can return after the SQLite database is recreated.

## 4. Current application behavior relevant to hosting

- `Program.cs` selects PostgreSQL when `DATABASE_URL` exists, SQLite when `UseSQLite=true`, and SQL Server otherwise.
- A fresh PostgreSQL database is built with `EnsureCreatedAsync()` and then adjusted by `PostgresSchemaUpgrader`.
- SQLite is built with `EnsureCreatedAsync()` and `SqliteSchemaUpgrader`.
- Startup seeding errors are logged, but the application continues starting. A healthy `/health` response therefore confirms the process is running; it does not by itself prove every database schema/seeding action succeeded.
- `FileStorageService` writes directly to a filesystem. It does not currently support Azure Blob Storage.
- ASP.NET data-protection keys are persisted to a configured filesystem directory.
- The root `Dockerfile` builds and publishes `Poms.Web` on .NET 8 and listens on the platform-provided `PORT`.

Before production, replace the `EnsureCreated`-based PostgreSQL lifecycle with tested, versioned PostgreSQL migrations. A schema failure should also fail deployment or readiness instead of only being logged.

## 5. Render operating procedure

### Redeploy

Render watches GitHub `main`. A pushed commit triggers a deployment automatically. The blueprint and build entry points are:

- `render.yaml`
- `Dockerfile`
- Docker context: repository root

### Smoke check

```powershell
curl.exe --fail --show-error https://poms-motivation.onrender.com/health
curl.exe --head https://poms-motivation.onrender.com/
```

Then perform an authenticated workflow check:

1. Sign in as an authorized demo user.
2. Register a disposable patient through all five steps.
3. Confirm the patient appears in search and can be opened.
4. Delete the disposable record.
5. Review Render logs for database or file-storage exceptions.

### Recovery expectation

There is no dependable recovery point for `/tmp/poms-local.db` or `/tmp/poms-storage`. If the instance is recreated, allow startup to create a clean database and reseed reference data/users. If any important records have been entered, export them **before** restarting or redeploying the service.

## 6. Recommended Azure target architecture

| Concern | Azure service | Initial size/configuration |
| --- | --- | --- |
| Web application | Azure App Service for Linux | B1 for a pilot; F1 only for development/demo |
| Relational database | Azure Database for PostgreSQL Flexible Server | Burstable B1MS, 32 GB |
| Patient files | Azure Storage account / private Blob container | Standard GPv2, Hot LRS initially |
| Secrets | App Service settings backed by Key Vault | Connection string and initial-admin secret |
| Application keys | Blob-backed ASP.NET data-protection key ring, protected by Key Vault | One private container/path per environment |
| Monitoring | Application Insights / Azure Monitor | Alerts on 5xx, restart, storage, and DB failures |

Place the app, database, and storage account in the same Azure region. South India is geographically closer to Sri Lanka; Southeast Asia (Singapore) matches the current Render region and is slightly cheaper for the small configuration priced below. Benchmark latency and confirm the organization's data-residency requirements before choosing.

### Migration sequence

1. **Security preparation**
   - Remove predictable production seed users/passwords.
   - Make the first-admin seed optional and secret-driven.
   - Ensure startup fails when required schema initialization fails.
2. **Persistent file support**
   - Add an Azure Blob implementation of `IFileStorageService`.
   - Use a private container, managed identity, encryption, and least-privilege access.
   - Persist ASP.NET data-protection keys outside the app instance.
3. **Azure foundation**
   - Create separate resource groups and databases for test and production.
   - Create PostgreSQL Flexible Server with 32 GB storage and TLS enforced.
   - Create the storage account and private containers.
   - Create App Service and enable managed identity.
4. **Application configuration**
   - Set `ASPNETCORE_ENVIRONMENT=Production`.
   - Set `UsePostgreSQL=true` and `UseSQLite=false`.
   - Supply an Npgsql-format `ConnectionStrings__DefaultConnection` through protected settings.
   - Configure Blob Storage and data-protection settings.
   - Keep application/database/storage in the same region.
5. **Deployment and validation**
   - Deploy from GitHub to a staging slot or test App Service first.
   - Validate schema creation/migrations in a fresh test database.
   - Run login, patient registration, duplicate detection, upload/download, reporting, and soft-delete tests.
   - Configure backups, budget alerts, monitoring alerts, and a restore test.
6. **Cutover**
   - Do not migrate disposable Render demo data.
   - If real Render data exists, stop writes and export SQLite plus files before any Render restart.
   - Switch the approved hostname only after acceptance testing and rollback preparation.

### Short pilot without the Blob code change

For a time-limited Azure pilot, App Service's persistent `/home` storage can temporarily host uploads and data-protection keys. Configure paths under `/home`, not `/tmp`, and do not scale beyond one instance. This is an interim bridge; Blob Storage is the target for production and multi-instance deployment.

## 7. Azure free-account finding

As checked on 2026-08-11:

- The Azure free account is available only to eligible new Azure customers.
- It includes **US$200 credit usable within 30 days**.
- Azure does not charge the card unless the customer explicitly moves to pay-as-you-go. A temporary card verification hold may appear.
- At 30 days, or when the credit is exhausted, the customer must move to pay-as-you-go to keep resources running and continue receiving applicable free-service allowances. Otherwise, the account/services are disabled.
- Unused promotional credit cannot be carried beyond its original 30-day lifetime.
- Azure Database for PostgreSQL includes, for the first 12 months, up to **750 hours/month of Flexible Server Burstable B1MS compute, 32 GB storage, and 32 GB backup storage**.
- App Service F1 is always free up to its limits: shared compute, 60 CPU minutes/day, 1 GB RAM, and 1 GB storage. Microsoft says F1 is for trials/learning, has no SLA, and is not supported for production workloads.

Practical interpretation for POMS:

- **30-day evaluation:** the US$200 credit is enough for this small stack, but it expires after 30 days even if money remains.
- **Months 2–12:** after opting into pay-as-you-go, an eligible account can keep the PostgreSQL B1MS/32 GB server within its monthly free allowance. F1 can make the app free too, but its one-hour-per-day CPU quota makes it unsuitable for dependable clinical use.
- **After month 12:** PostgreSQL becomes billable at normal rates. F1 can remain free within its limits, but a paid App Service plan is recommended.

Official references:

- [Azure account and US$200/30-day terms](https://azure.microsoft.com/en-us/pricing/purchase-options/azure-account)
- [Azure free-service list](https://azure.microsoft.com/en-us/pricing/free-services/)
- [Azure Database for PostgreSQL free allowance](https://azure.microsoft.com/en-us/products/postgresql/)
- [Azure App Service for Linux pricing and F1 limitations](https://azure.microsoft.com/en-us/pricing/details/app-service/linux/)

## 8. Azure pricing estimate

The following is a retail estimate in USD, checked on 2026-08-11 using Microsoft's [Azure Retail Prices API](https://learn.microsoft.com/en-us/rest/api/cost-management/retail-prices/azure-retail-prices). It assumes 730 hours/month, PostgreSQL B1MS, the minimum 32 GB database storage, 10 GB of Hot LRS Blob storage, and no discount/reservation.

### Paid pilot baseline

| Component | Southeast Asia | South India |
| --- | ---: | ---: |
| App Service Linux B1 | $0.018/hour = **$13.14/month** | $0.019/hour = **$13.87/month** |
| PostgreSQL B1MS compute | $0.026/hour = **$18.98/month** | $0.0245/hour = **$17.89/month** |
| PostgreSQL 32 GB storage | $0.138/GB = **$4.42/month** | $0.1737/GB = **$5.56/month** |
| Blob Hot LRS, 10 GB | $0.020/GB = **$0.20/month** | $0.0238/GB = **$0.24/month** |
| **Estimated baseline total** | **$36.74/month** | **$37.55/month** |

The totals exclude Blob operations, outbound bandwidth, logs above free allowances, a custom domain, taxes, support, Key Vault operations, backups above the included PostgreSQL amount, and high availability. Actual invoices depend on usage, contract, exchange rate, and current regional availability.

### Expected cost by phase

| Phase | Approximate infrastructure cost |
| --- | --- |
| First 30 days, eligible new account | Covered by the US$200 promotional credit if configured within it |
| Months 2–12, F1 app + eligible PostgreSQL allowance | Near $0 plus Blob operations/storage beyond free amounts; demo only |
| Months 2–12, B1 app + eligible PostgreSQL allowance | About $13.34/month in Southeast Asia, plus usage-dependent items |
| After month 12, B1 app + paid PostgreSQL baseline | About $36.74/month in Southeast Asia |

A production clinical deployment may require a higher App Service tier, PostgreSQL high availability (which is not the B1MS burstable baseline), zone redundancy, private networking, longer backup retention, stronger monitoring, and formal security/compliance controls. Price that design separately in the [Azure Pricing Calculator](https://azure.microsoft.com/en-us/pricing/calculator/) after workload and recovery requirements are approved.

## 9. Go-live gates

Do not label the system production-ready until all of the following are complete:

- Persistent PostgreSQL is in use and a restore has been tested.
- Patient files are in durable private storage and upload/download/delete tests pass.
- Predictable seed passwords are removed and all privileged credentials are rotated.
- MFA/external identity and least-privilege access are addressed.
- PostgreSQL schema changes use a tested migration process.
- Health/readiness checks verify database and required storage dependencies.
- Logs contain no patient data or secrets and have an approved retention policy.
- Backup, recovery time, recovery point, alerting, and incident ownership are documented.
- Data residency, privacy, security, and organizational clinical-governance reviews are approved.
- A staging deployment and rollback procedure have been exercised.

## 10. Handover decision

The Render deployment is suitable for demonstrating the repaired patient-registration workflow today. It is not suitable for retaining real patient records. The lowest-friction durable next step is an Azure pilot using App Service B1 plus PostgreSQL Flexible Server B1MS, with the PostgreSQL 12-month allowance used if the Azure account is eligible. Complete the security and Blob Storage work before any clinical production cutover.
