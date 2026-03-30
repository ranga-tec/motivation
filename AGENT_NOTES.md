# Agent Handoff Notes — POMS Project

**Last Updated:** 2026-03-30

## IMPORTANT: Current Branch State

| Branch | Commit | What It Contains |
|--------|--------|-----------------|
| `main` (currently deployed) | `6fe2e74` | **PROTOTYPE** — basic POMS, Railway deployment config only |
| `production-backup` | `5eea145` | **FULL PRODUCTION** — all Phase 1 features, restore this when ready |

## What Happened & Why

The client wanted to review the original prototype UI to describe what changes are needed before finalizing the production system.

**Steps taken (2026-03-30):**
1. `production-backup` branch created from `main` HEAD (`5eea145`) — full production state saved
2. `main` was reset back to `6fe2e74` (last commit before "Production system enhancements - Phase 1")
3. Both branches pushed to GitHub
4. Railway (https://popms.up.railway.app) deploys from `main` → now shows prototype

## What To Do Next

After the client reviews the prototype and describes the needed changes:

### Step 1 — Restore production
```bash
git checkout main
git reset --hard production-backup
git push origin main   # Railway will redeploy with production version
```

### Step 2 — Apply client feedback
Make the requested UI/feature changes on top of the restored production code, then commit and push.

## Key Difference: Prototype vs Production

The **prototype** (`6fe2e74`) has:
- Basic patient, episode, fitting management
- Simple views with Bootstrap 5
- Railway deployment configs (Dockerfile, nixpacks.toml)

The **production** (`production-backup` / `5eea145`) added on top:
- Dashboard with KPIs and Chart.js visualizations
- Reports module (PDF/Excel export via QuestPDF + ClosedXML)
- Admin module (user management, system settings, audit logs)
- Light professional UI theme (`wwwroot/css/poms-theme.css`)
- Enhanced Fitting module (status, adjustments, feedback, FittingNumber)
- Document management with OCR (Tesseract, Sinhala support)
- Patient photo upload and IsActive toggle
- Complete ViewModels, DashboardService, ReportService
- DEVELOPMENT.md developer guide

## Credentials (unchanged in both versions)

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@poms.lk | Admin@123 |
| Clinician | clinician@poms.lk | Clinic@123 |
| Data Entry | registrar@poms.lk | Data@123 |
| Viewer | viewer@poms.lk | View@123 |

## Railway Deployment

- **URL:** https://popms.up.railway.app
- **Deploys from:** `main` branch on GitHub (ranga-tec/motivation)
- **Build:** Nixpacks using `nixpacks.toml`
- **DB:** PostgreSQL (auto-provisioned by Railway, uses `DATABASE_URL` env var)
- **Port:** 8080

## Authentication Note for Future Agents

Git push requires authentication via Windows Credential Manager (GUI). If push fails silently, ask the user to run the push commands manually:
```bash
git push origin production-backup    # save production state
git push --force origin main         # deploy prototype to Railway
```
Or after client feedback:
```bash
git push origin main                 # redeploy production+changes
```
