# POMS professional UI redesign

1. Establish the shared design system and application shell.
   - Prove: active navigation, consistent page widths, typography, buttons, cards, forms, tables, badges, empty states, and responsive containers work in light and dark themes.
2. Redesign the dashboard around daily operational priorities.
   - Prove: metrics, today's appointments, quick actions, and recent activity have a clear hierarchy on desktop and mobile.
3. Redesign the highest-traffic record screens.
   - Prove: patient list/folder, clinical records, and appointments use standard headers, filters, responsive tables, and restrained action hierarchy.
4. Standardize reports, administration lists, and remaining create/edit forms.
   - Prove: all Razor views use the shared visual language without changing routes, field names, permissions, or server behavior.
5. Verify the system and document remaining risk.
   - Prove: build/tests pass; desktop/mobile UI is inspected; validation, dialogs, empty states, and dark theme are checked.

# POMS production hardening (MVC architecture retained)

Status: complete on 2026-10-07. Release verification: 71 tests passed; local `/health` returned HTTP 200 with database connectivity.

1. Audit the existing production boundaries before editing.
   - Prove: document the current database, file storage, Identity key, health-check, proxy, and container behavior from code and deployment files.
2. Make startup and health reporting fail safely.
   - Prove: an unavailable database prevents a false-ready application start, while `/health` verifies database connectivity.
3. Validate persistent storage and account bootstrap configuration in production.
   - Prove: production rejects temporary storage unless explicitly marked as a disposable demo; fixed demo passwords are never seeded in production; a fresh production database requires secret-provided administrator credentials.
4. Align container configuration and deployment documentation.
   - Prove: documented environment variables and mounted paths match the application configuration and Docker definitions.
5. Verify the hardened application.
   - Prove: Release solution build and automated tests pass; local startup and `/health` return successfully with the development database.
   - Verification correction: Debug output is locked by the already-running local POMS process and the sandbox cannot stop its owner process. Use Release output for build/tests instead of retrying Debug.

# Phase 1 API extraction (MVC remains operational)

Status: complete on 2026-10-07. Verification: 4 focused API tests and 75 full-suite tests passed; HTTP smoke tests returned API 401 without redirect, authenticated API 200, MVC 302 to login, and health 200.

1. Define the versioned API boundary and response contracts.
   - Prove: `/api/v1` uses API controllers, stable DTOs, bounded pagination, and ProblemDetails-compatible errors without exposing EF entities.
2. Add authenticated patient read endpoints.
   - Prove: authorized callers can search/page patients and fetch one patient; missing records return 404 and invalid filters return 400.
3. Add authenticated appointment read endpoints.
   - Prove: authorized callers can filter/page appointments and fetch one appointment using the existing authorization policy.
4. Make cookie authentication API-safe and document the contract.
   - Prove: unauthenticated `/api` requests return 401/403 instead of HTML login redirects; the migration boundary and authentication limitation are documented.
5. Verify API and MVC compatibility.
   - Prove: focused API tests and the full Release suite pass; existing MVC routes remain mapped and compile unchanged.
   - Verification correction: direct controller tests do not run MVC's result executor, so `ValidationProblem()` leaves status metadata unset. Return an explicit 400 `ValidationProblemDetails` contract and verify that instead.

# Phase 2 API authentication boundary

Status: complete on 2026-10-07. Verification: bearer/cookie scheme-selection tests and the full 78-test Release suite passed. Bearer mode remains disabled until an OIDC authority is configured.

1. Add a configurable OIDC bearer-token handler without replacing MVC cookies.
   - Prove: API authentication selects bearer tokens when an Authorization header is present and retains cookies for current same-origin users.
2. Require the combined API authentication policy on `/api/v1` controllers.
   - Prove: existing role/policy checks still apply after authentication scheme selection.
3. Validate OIDC configuration early.
   - Prove: production cannot enable bearer authentication without authority and audience values; development can run cookie-only.
4. Document provider-neutral mobile and external-client configuration.
   - Prove: configuration keys, token expectations, and remaining OAuth client-registration work are explicit.
5. Verify both authentication paths and regression safety.
   - Prove: API auth-selection tests and the full Release suite pass; unauthenticated API stays 401 and MVC login redirect stays intact.

# Phase 3 separately deployable API host

Status: complete on 2026-10-07. Verification: independent API host built and ran on port 5015; database health returned 200 against the development database, protected API returned 401, and configured-origin CORS preflight returned 204 with the expected origin.

1. Create a dedicated `Poms.Api` ASP.NET Core host targeting .NET 8.
   - Prove: it builds and runs independently from `Poms.Web` while sharing the current API controller source during migration.
2. Configure bearer-only authentication and existing authorization policies.
   - Prove: startup requires OIDC authority/audience and API endpoints never use MVC cookies.
3. Add explicit frontend CORS and database configuration.
   - Prove: only configured origins are allowed; PostgreSQL, SQLite development, and dependency health checks are supported.
4. Add an API-specific container definition and operations documentation.
   - Prove: the API can be published/deployed without Razor assets or the MVC process.
5. Verify independent and combined builds.
   - Prove: API host tests/smoke checks and the full solution Release suite pass while MVC remains operational.

# Phase 4 separate React frontend

Status: complete on 2026-10-07. Verification: frontend lint and production build passed; Playwright inspected desktop dashboard/patients, mobile appointments, direct-route fallback, and production auth guard; the full .NET Release suite passed 78/78 tests.

1. Scaffold a React, TypeScript, and Vite application with the POMS design shell.
   - Prove: the frontend installs and builds independently from both ASP.NET Core hosts.
2. Add a typed client for the versioned patient and appointment APIs.
   - Prove: requests use the configured API base URL, bearer token, bounded query parameters, and typed error handling.
3. Add provider-neutral OIDC authorization-code-with-PKCE authentication.
   - Prove: sign-in, callback, silent session restoration, sign-out, and protected routes are configurable without provider-specific code.
4. Build professional responsive patient and appointment screens.
   - Prove: desktop and mobile layouts include loading, error, empty, search/filter, and pagination states; local demo mode is visibly identified and cannot activate implicitly in production.
5. Verify, document, commit, and push the phase.
   - Prove: lint/build pass, Playwright browser checks cover desktop and mobile, deployment variables are documented, and the commit reaches `origin/main`.

# Phase 5 React patient registration

Status: complete on 2026-10-07. Verification: frontend lint/build passed; the separate API built; 83/83 .NET tests passed; Playwright verified required-field blocking, a complete registration, completion feedback, direct-route refresh, responsive mobile layout, and zero console warnings.

1. Define versioned registration and reference-data API contracts.
   - Prove: request DTO validation matches the existing patient rules, responses do not expose EF entities, and dropdown values come from the database.
2. Add the secured patient creation endpoint.
   - Prove: the DataEntry policy protects creation; location, assignee, duplicate, patient-number, contact, and audit fields use existing domain services and rules.
3. Add focused API tests for successful and rejected registrations.
   - Prove: tests cover persistence, exact duplicates, possible-duplicate confirmation, and invalid location relationships.
   - Verification correction: adding registration dependencies changed the controller constructor and initially broke existing direct-controller tests. Route all patient API tests through one real-service factory before adding new scenarios.
4. Replace the React placeholder with a professional registration workflow.
   - Prove: the form loads reference data, validates required and conditional fields, handles duplicate confirmation, prevents double-submit, and shows the created patient number.
5. Verify, document, commit, and push the phase.
   - Prove: frontend lint/build, .NET tests, and Playwright desktop/mobile/invalid/submit flows pass; the commit reaches `origin/main`.

# Phase 6 React appointment management

Status: complete on 2026-10-08. Verification: frontend lint/build and the separate API build passed; 87/87 .NET tests passed; Playwright verified required-field blocking, create/reschedule/cancel feedback, responsive mobile cards, and zero console errors or warnings.

1. Document the existing appointment workflow and define stable write contracts.
   - Prove: create, reschedule, cancel, and completion rules are mapped without carrying legacy-data compatibility requirements.
2. Add bearer-only appointment write endpoints and reference data.
   - Prove: patient selection, assignee validation, status transitions, audit fields, and ProblemDetails responses use existing domain rules.
3. Add API tests for successful and rejected state transitions.
   - Prove: tests cover create, reschedule, cancel, invalid patient, and invalid transition cases.
4. Add responsive React appointment actions.
   - Prove: users can create, reschedule, and cancel from the schedule with validation, double-submit protection, and clear success/error feedback.
5. Verify, document, commit, and push the phase.
   - Prove: frontend lint/build, API build, full .NET tests, and Playwright desktop/mobile workflows pass; the commit reaches `origin/main`.

# Full React/API migration completion

Scope confirmed on 2026-10-08: migrate every production workflow currently exposed by MVC to the
standalone versioned API and React frontend. Preserve only administrator login data at cutover;
legacy business data compatibility and import are out of scope.

## Phase 7 patient workspace and episodes

Status: complete on 2026-10-08. Verification: patient list-to-detail, record creation/editing,
restricted-record feedback, required browser fields, direct routing, and responsive mobile layout
passed; frontend lint/build, standalone API build, and the full .NET suite passed.

1. Define patient-detail and episode API contracts from current domain rules.
   - Prove: contracts expose contacts, episode summaries, clinical activity, and reference options without serializing EF entities.
2. Add bearer-secured episode create/edit/detail endpoints.
   - Prove: location, centre, patient, status, audit, and restricted-access rules are enforced with ProblemDetails responses.
3. Add focused patient-workspace and episode API tests.
   - Prove: tests cover reads, creation, invalid relationships, editing, and authorization-sensitive visibility.
4. Build responsive React patient detail and episode workflows.
   - Prove: list-to-detail navigation, episode create/edit dialogs, empty/error/loading states, and mobile layouts work without legacy pages.
5. Verify, document, commit, and push.
   - Prove: lint/build, API build, full tests, and Playwright patient-to-episode flows pass.

## Phase 8 clinical records

Status: complete on 2026-10-08. Verification: assessment/prescription, fitting, delivery, and
follow-up create/edit workflows passed focused API tests and Playwright desktop/mobile checks;
bilateral prescription requirements, restricted entries, invalid follow-up times, and catalog
validation were exercised; frontend lint/build, standalone API build, and the full suite passed.

1. Extract assessment and prescription contracts/options.
2. Extract fitting, delivery, and follow-up contracts/options.
3. Implement secured APIs and state/audit validation.
4. Build responsive React create/edit clinical workflows.
5. Verify all clinical chains, commit, and push.

## Phase 9 documents and printable records

Status: complete on 2026-10-08. Verification: patient and record upload/list/download/delete flows,
restricted access, latest patient-photo delivery, and all five PDF outputs are implemented; focused
API tests and Playwright desktop/mobile upload/delete/download checks passed with no console errors.

1. Introduce storage-neutral patient/episode document APIs.
2. Add upload, download, delete, and patient-photo authorization.
3. Expose registration, assessment, prescription, delivery, and follow-up print outputs.
4. Build React document and print actions.
5. Verify file security and print rendering, commit, and push.

## Phase 10 administration

Status: queued.

1. Extract locations, geography, lookups, devices, and device types.
2. Extract user/profile/role/lock/password-reset administration while preserving admin login.
3. Build role-protected admin APIs.
4. Build responsive React administration screens.
5. Verify authorization and CRUD workflows, commit, and push.

## Phase 11 reports and dashboard

Status: queued.

1. Define dashboard metrics and drill-down APIs.
2. Define all report filters, row contracts, summaries, and exports.
3. Implement the thirteen existing report families against current query rules.
4. Build responsive React report/dashboard screens and export controls.
5. Verify report totals/exports against controlled fixtures, commit, and push.

## Phase 12 production cutover

Status: queued.

1. Remove legacy-data/import dependencies and seed production-safe reference data.
2. Retire migrated Razor routes while retaining administrator authentication/recovery.
3. Harden dependencies, CORS, rate limits, health checks, logs, and deployment configuration.
4. Run full automated, browser, accessibility, security, and clean-database acceptance checks.
5. Publish final deployment/runbook documentation, commit, push, and tag the release candidate.
