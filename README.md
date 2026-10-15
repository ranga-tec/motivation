# POMS — Exceed Lanka Prosthetic & Orthotic Data Management System

POMS manages the complete patient pathway for Exceed Lanka's prosthetic and orthotic services:
registration, patient number (PNO) generation, the patient folder, assessments and prescriptions,
fittings, deliveries, follow-ups, documents, printable forms, appointments and reports. Every
record is filed under the patient number. Requirements: [`Requirement/PRD`](Requirement/PRD).

**Live:** <https://169-58-173-191.sslip.io> — hosted on one Contabo VPS.
Hosting, operations, backups and restore: [`DEPLOY_CONTABO.md`](DEPLOY_CONTABO.md).

## Architecture

| Part | Technology | Location |
| --- | --- | --- |
| Web client | React 19, TypeScript, Vite, lucide icons | `POMS/frontend` |
| API | ASP.NET Core 8, EF Core, ASP.NET Core Identity bearer tokens | `POMS/src/Poms.Api` (controllers in `POMS/src/Poms.Web/Api/V1`) |
| Domain / data | Entities, EF Core context, services, QuestPDF forms and reports | `POMS/src/Poms.Domain`, `Poms.Infrastructure`, `Poms.Reporting` |
| Database | PostgreSQL 17 (production), SQLite (local development) | — |
| Hosting | Docker Compose: Caddy + frontend + API + PostgreSQL | `POMS/deploy/contabo` |
| Tests | xUnit, FluentAssertions (108 tests) | `POMS/tests/Poms.Tests` |

`POMS/src/Poms.Web` is the original Razor MVC application. It is not deployed; its `Api/V1` folder
holds the API controllers that `Poms.Api` compiles in.

## Features

- **Dashboard** — PRD title; Total patients, Active records, Active patients and Today's
  activities cards with drill-down; six quick actions; recent activities feed; today's pending
  assessments and fittings (deliveries and follow-ups are never shown as pending).
- **Patient registration** — five-step wizard (Identity, Contact, Guardian/carer, Registration,
  Review); local/foreign patients; NIC, passport, driving licence or N/A; duplicate checks;
  province → district → city; multiple telephone numbers with date confirmed and person checked;
  referral source; travel time; optional photo. The PNO is generated automatically, with a centre
  flag such as `C/2026/0001` for Colombo.
- **Patient folder** — search by PNO, names, ID number or phone, filter by location, province and
  district; tabs for Overview, Clinical records, Clinical history, Documents and Print forms.
- **Clinical records** — records (episodes) per patient; prosthetic and orthotic assessments with
  the hardcoded PRD prescription lists, sub-types and bilateral left/right prescriptions; unlimited
  fittings, deliveries and follow-ups; stage tracker; restricted entries.
- **Appointments** — assessment, fitting and delivery visits; day and 7-day views; complete,
  reschedule, cancel; type-ahead patient search and searchable "Handled by".
- **Documents and printing** — uploads per patient or record; registration form (Form 101 layout),
  assessment, prescription, delivery note and follow-up note PDFs.
- **Reports** — all 13 PRD reports with location, date, PNO, province, local/foreign, assessment
  type, prosthetic/orthotic type and year filters; PDF and CSV export.
- **Administration** — users and roles, password reset, lock/unlock, restricted-data access;
  reference data for locations (with PNO flag), provinces, districts, cities, referral sources,
  nationalities, main problem types, causes, device types and devices.
- **Settings** — Light, Dark, Classic (original navy) and System themes; comfortable or compact
  density.

## Roles

| Role | Can use |
| --- | --- |
| Administrator (`ADMIN`) | Everything, including users and reference data |
| Registration user (`DATA_ENTRY`) | Register and edit patients, patient folder, appointments |
| Clinical user (`CLINICIAN`) | Patient folder (read), clinical records, appointments |
| Management user (`MANAGEMENT`) | Dashboard, patient folder (read), reports |
| Report user (`VIEWER`) | Dashboard and reports |

## Local development

Requirements: .NET 8 SDK, Node.js 22. Development uses SQLite, so no database server is needed,
and seeds demonstration accounts (`admin@poms.lk` / `Admin@123`, `clinician@poms.lk` /
`Clinic@123`, `registrar@poms.lk` / `Data@123`, `management@poms.lk` / `Manage@123`,
`viewer@poms.lk` / `View@123`). These accounts are created only in Development.

```bash
# API on http://localhost:5015
cd POMS/src/Poms.Api
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5015 dotnet run --no-launch-profile

# Frontend on http://localhost:5173 (second terminal)
cd POMS/frontend
npm install
npm run dev
```

The whole stack in Docker, with PostgreSQL: `docker compose -f POMS/compose.development.yaml up -d --build`,
then open <http://localhost:8080> and sign in as `admin@poms.local` / `Local-Identity-8374!`.

### Checks before every deployment

```bash
dotnet build POMS/POMS.sln -c Release
dotnet test POMS/POMS.sln -c Release
cd POMS/frontend && npm run lint && npm run build
```

## Documentation

| File | Contents |
| --- | --- |
| [`DEPLOY_CONTABO.md`](DEPLOY_CONTABO.md) | Hosting, deploy, logs, backups, restore, domain, troubleshooting |
| [`AGENT_HANDOVER.md`](AGENT_HANDOVER.md) | Engineering status, revision history, known issues, working rules |
| [`POMS/API_HOST.md`](POMS/API_HOST.md) | API configuration, authentication and endpoint map |
| [`POMS/frontend/README.md`](POMS/frontend/README.md) | Frontend structure and conventions |
| [`BUGS/bugs.md`](BUGS/bugs.md) | Client-reported issues and their status |
| [`TODO.md`](TODO.md) | Open items |
| [`PLAN.md`](PLAN.md) | Phase-by-phase implementation history |
| [`Requirement/PRD`](Requirement/PRD) | Product requirements |
