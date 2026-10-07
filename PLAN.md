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
