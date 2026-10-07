# POMS

POMS is an ASP.NET Core MVC application for prosthetic, orthotic, and spinal patient management.

## Live Service

- <https://poms-motivation-production.up.railway.app>

One Railway service (`poms-motivation` in project `kind-presence`) backed by a managed PostgreSQL
database. It exposes a database-aware `/health` endpoint and deploys automatically from GitHub `main`.

The earlier prototype/production split, the two `motivation-production-*` URLs, and the Render
demo are all retired. `production-backup` still exists as a branch but backs no live service.

## Database And Storage

- PostgreSQL on Railway, reached through `DATABASE_URL`.
- Uploaded patient files and ASP.NET data-protection keys live on a Railway volume mounted at
  `/app/storage`, so they survive redeploys.
- Production rejects `/tmp` storage unless `Storage__AllowEphemeral=true` explicitly marks a
  disposable demo. This exception must never be used for real patient data.
- Schema is created with `EnsureCreatedAsync()` plus `PostgresSchemaUpgrader`. There are no EF Core
  migrations on the PostgreSQL path, so a destructive schema change needs the upgrader updated.

## Clinical Reference Data

Fresh databases seed locations, referral sources, problem and cause types, nationalities, and a
starter catalogue of 17 prosthetic, orthotic, and spinal devices. Administrators maintain these
under **Admin → Devices** and the other reference-data screens.

## Legacy Patient Form OCR

Authorized data-entry staff can open **Patient Folder → Import old form** to read a photographed
Exceed Lanka registration form. OCR prefills the normal registration wizard for human review; it
does not create a patient until staff complete the required fields and press **Register patient**.
On save, POMS generates the normal patient number and database GUIDs, then attaches the original
scan as a restricted registration document. Unfinished scans expire from staging after 24 hours.

Users can select **OpenAI vision** (recommended for handwriting) or **Offline OCR** (Tesseract,
no API charge). The Docker images install Tesseract automatically. For local Windows development,
install Tesseract and set `TESSERACT_EXECUTABLE_PATH` to `tesseract.exe` when it is not on `PATH`.

Configure OpenAI with `OPENAI_API_KEY` (or `PatientFormOcr__ApiKey`). Optional overrides are
`PatientFormOcr__Model`, `PatientFormOcr__Endpoint`, `PatientFormOcr__Enabled`, and nested
`PatientFormOcr__Offline__*` settings. Do not place an API key in `appsettings.json` or commit it.

## Documentation

- Current deployment: [`DEPLOY_RAILWAY.md`](DEPLOY_RAILWAY.md)
- Engineering handover, revision history, known issues: [`AGENT_HANDOVER.md`](AGENT_HANDOVER.md)
- VPS fallback (Contabo, Kamatera, any Ubuntu host): [`DEPLOY_CONTABO.md`](DEPLOY_CONTABO.md)
- Outstanding requests from the system owner: [`TODO.md`](TODO.md)
- Historical, describing retired environments: [`DEPLOYMENT_HANDOVER.md`](DEPLOYMENT_HANDOVER.md),
  [`AGENT_NOTES.md`](AGENT_NOTES.md)

## Local Development

Development uses SQLite, so no database server is required.

```bash
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5011   dotnet run --project POMS/src/Poms.Web --no-launch-profile

dotnet test POMS/POMS.sln -c Release
```

## Seed Users

Development databases seed fixed demonstration accounts. Production never seeds those passwords.
A fresh production database requires `BootstrapAdmin__Email` and
`BootstrapAdmin__Password`; store both as deployment secrets. Once the administrator exists, the
variables may be removed because subsequent users are managed from the Admin screen. Production
also refuses to start if an existing demonstration account still uses its public development
password.
