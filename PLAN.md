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
